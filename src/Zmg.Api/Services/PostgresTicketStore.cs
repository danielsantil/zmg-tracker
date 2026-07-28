using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // ITicketStore
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zmg.Api.Logging;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Keeps sessions in the database instead of inside the cookie (v2.10/M55).
///
/// A default ASP.NET cookie is a self-contained encrypted ticket: valid until it expires, with no way
/// to take it back. The requirement is "7 days <em>unless invalidated</em>", so the ticket lives in a
/// row and the cookie carries only its key. Revoking is a <c>DELETE</c>, and it bites on the very next
/// request — no key rotation, no waiting out an expiry.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scope handling is not incidental.</b> The cookie options that hold this store are resolved once
/// and effectively live for the process, while <see cref="ZmgDbContext"/> is scoped per request.
/// Injecting the context directly would capture the first request's instance and then throw on every
/// subsequent one, so each call opens its own scope.
/// </para>
/// <para>
/// <b><see cref="RetrieveAsync"/> re-checks authorization on every request</b>, not just at sign-in.
/// Without that, disabling someone would leave their existing session working for up to seven days —
/// which is not what "revoked" means to anyone reading the table. It costs one indexed join per
/// request, which at this scale is the right trade.
/// </para>
/// </remarks>
public sealed class PostgresTicketStore(
    IServiceScopeFactory scopeFactory,
    IOptions<AuthOptions> options,
    ILogger<PostgresTicketStore> logger) : ITicketStore
{
    /// <summary>
    /// How stale <c>LastSeenAt</c> may get before a request refreshes it. Without a throttle this
    /// would be a database write on literally every request, to maintain a column nothing gates on.
    /// </summary>
    private static readonly TimeSpan LastSeenPrecision = TimeSpan.FromMinutes(5);

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

        var now = DateTime.UtcNow;
        var email = ticket.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var userId = Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

        db.AuthSessions.Add(new AuthSession
        {
            Id = key,
            AllowedUserId = userId,
            Email = email,
            TicketData = SerializeTicket(ticket),
            CreatedAt = now,
            ExpiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime ?? now.AddDays(options.Value.SessionDays),
            LastSeenAt = now,
        });

        // Opportunistic sweep. Sign-in is rare, so this is the cheapest possible place to put it — a
        // hosted timer would barely run anyway on a container that scales to zero after 5 idle minutes.
        var removed = await db.AuthSessions.Where(s => s.ExpiresAt < now).ExecuteDeleteAsync();
        if (removed > 0) Log.AuthSessionsSwept(logger, removed);

        await db.SaveChangesAsync();

        // The successful-login line lives here rather than in the OIDC event that authorized it: this
        // is the moment the session exists, and the only one that knows the id to revoke it by.
        Log.AuthLoginOk(logger, email, key);

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

        var session = await db.AuthSessions.FirstOrDefaultAsync(s => s.Id == key);
        if (session is null) return; // already revoked — nothing to renew, and recreating it would undo that

        session.TicketData = SerializeTicket(ticket);
        // Expiry is only ever moved by an explicit ticket expiry, never extended by the act of renewing:
        // sessions are absolute, so a stolen cookie cannot be kept alive by using it.
        if (ticket.Properties.ExpiresUtc is { } expires) session.ExpiresAt = expires.UtcDateTime;
        await db.SaveChangesAsync();
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

        var session = await db.AuthSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == key);
        if (session is null) return null; // revoked, swept, or a forged key

        var now = DateTime.UtcNow;
        if (session.ExpiresAt <= now)
        {
            db.AuthSessions.Remove(session);
            await db.SaveChangesAsync();
            return null;
        }

        // The row is the authority on access, not the ticket that was minted days ago. Disabling a user
        // therefore takes effect on their next request rather than at their next sign-in.
        if (!AccessControl.IsAllowed(session.User))
        {
            db.AuthSessions.Remove(session);
            await db.SaveChangesAsync();
            return null;
        }

        if (session.LastSeenAt is null || now - session.LastSeenAt.Value > LastSeenPrecision)
        {
            session.LastSeenAt = now;
            await db.SaveChangesAsync();
        }

        return DeserializeTicket(session.TicketData);
    }

    public async Task RemoveAsync(string key)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

        // ExecuteDelete rather than load-then-remove: sign-out should not care whether the row is still
        // there, and this is one statement instead of two round trips.
        await db.AuthSessions.Where(s => s.Id == key).ExecuteDeleteAsync();
    }

    private static byte[] SerializeTicket(AuthenticationTicket ticket) =>
        TicketSerializer.Default.Serialize(ticket);

    private static AuthenticationTicket? DeserializeTicket(byte[] data) =>
        TicketSerializer.Default.Deserialize(data);
}
