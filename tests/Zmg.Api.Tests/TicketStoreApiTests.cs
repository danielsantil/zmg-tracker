using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// M55 — "7 days unless invalidated", proven.
///
/// The revocation requirement is the reason sessions are database rows instead of self-contained
/// cookies, and it cannot be exercised through HTTP without a real Google round trip. So it is tested
/// where the behaviour lives: <see cref="Zmg.Api.Services.PostgresTicketStore"/> resolved from the
/// running host, against the real schema.
/// </summary>
public class TicketStoreApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private ITicketStore Store(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ITicketStore>();
    private static ZmgDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

    private static AuthenticationTicket TicketFor(AllowedUser user, DateTimeOffset? expires = null)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = expires ?? DateTimeOffset.UtcNow.AddDays(7),
        };

        return new AuthenticationTicket(new ClaimsPrincipal(identity), props, CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static async Task<AllowedUser> AddUserAsync(ZmgDbContext db, string email)
    {
        var user = new AllowedUser
        {
            Id = Guid.NewGuid(),
            Email = EmailNormalization.Normalize(email),
            CreatedAt = DateTime.UtcNow,
        };
        db.AllowedUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task A_stored_ticket_comes_back_with_its_identity_intact()
    {
        using var scope = factory.Services.CreateScope();
        var user = await AddUserAsync(Db(scope), "roundtrip-store@example.com");

        var key = await Store(scope).StoreAsync(TicketFor(user));
        var retrieved = await Store(scope).RetrieveAsync(key);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Email, retrieved!.Principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public async Task The_cookie_key_is_opaque_and_carries_no_identity()
    {
        using var scope = factory.Services.CreateScope();
        var user = await AddUserAsync(Db(scope), "opaque@example.com");

        var key = await Store(scope).StoreAsync(TicketFor(user));

        // The whole point of a server-side session: the browser holds a meaningless handle, so a stolen
        // cookie discloses nothing and is worthless the moment the row is gone.
        Assert.DoesNotContain("opaque@example.com", key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(user.Id.ToString(), key, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deleting_the_row_revokes_the_session_immediately()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "revoke-row@example.com");
        var key = await Store(scope).StoreAsync(TicketFor(user));
        Assert.NotNull(await Store(scope).RetrieveAsync(key));

        // The "unless invalidated" requirement, exactly as it would be exercised in Neon at 2am.
        await db.AuthSessions.Where(s => s.Id == key).ExecuteDeleteAsync();

        Assert.Null(await Store(scope).RetrieveAsync(key));
    }

    [Fact]
    public async Task Signing_out_removes_the_row_rather_than_only_the_cookie()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "signout@example.com");
        var key = await Store(scope).StoreAsync(TicketFor(user));

        await Store(scope).RemoveAsync(key);

        Assert.Null(await db.AuthSessions.SingleOrDefaultAsync(s => s.Id == key));
    }

    [Fact]
    public async Task Disabling_a_user_kills_their_existing_session_on_the_next_request()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "disabled-later@example.com");
        var key = await Store(scope).StoreAsync(TicketFor(user));
        Assert.NotNull(await Store(scope).RetrieveAsync(key));

        user.DisabledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Without re-checking authorization on retrieve, revoking someone would leave them signed in
        // for up to seven more days — which is not what anyone editing that column expects.
        Assert.Null(await Store(scope).RetrieveAsync(key));
        Assert.Null(await db.AuthSessions.SingleOrDefaultAsync(s => s.Id == key));
    }

    [Fact]
    public async Task An_expired_session_is_refused_and_cleaned_up()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "expired@example.com");

        var key = await Store(scope).StoreAsync(TicketFor(user, DateTimeOffset.UtcNow.AddSeconds(-1)));

        Assert.Null(await Store(scope).RetrieveAsync(key));
        Assert.Null(await db.AuthSessions.SingleOrDefaultAsync(s => s.Id == key));
    }

    [Fact]
    public async Task Renewing_does_not_extend_an_absolute_expiry()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "absolute@example.com");
        var expires = DateTimeOffset.UtcNow.AddDays(7);
        var key = await Store(scope).StoreAsync(TicketFor(user, expires));

        // Renew with the same ticket expiry, as a normal request would.
        await Store(scope).RenewAsync(key, TicketFor(user, expires));

        var stored = await db.AuthSessions.SingleAsync(s => s.Id == key);
        Assert.Equal(expires.UtcDateTime, stored.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Retrieving_an_unknown_key_returns_null_rather_than_throwing()
    {
        using var scope = factory.Services.CreateScope();

        // A forged or long-swept cookie must be an ordinary "not signed in", not a 500.
        Assert.Null(await Store(scope).RetrieveAsync(Guid.NewGuid().ToString("N")));
    }
}
