using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// M54 — the auth schema exists and seeds, exercised through the real startup path.
///
/// A *database* test rather than a pure one, because what needs pinning is the seed and the
/// relational constraints, which no unit test can reach. Per the standing v2.5/M30 split these run
/// SQLite while the migration is Postgres-specific, so this asserts schema *shape* and behaviour —
/// column types are proven against real Postgres in M59's live verification, and Testcontainers
/// remain deferred to Phase 2.
///
/// Every test shares one in-memory database (IClassFixture), so none may disturb the seeded row:
/// the cascade case creates and destroys its own user.
/// </summary>
public class AuthSchemaApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private ZmgDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

    [Fact]
    public async Task The_bootstrap_user_is_seeded_and_normalized()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var expected = SeedData.AllowedUsers().Single();

        var user = await db.AllowedUsers.SingleOrDefaultAsync(u => u.Id == expected.Id);

        // Without this row a fresh database is locked out of its own login screen — nobody can sign
        // in, including the person who would add the first entry.
        Assert.NotNull(user);
        Assert.Equal(expected.Email, user!.Email);
        Assert.Equal(EmailNormalization.Normalize(user.Email), user.Email);
        Assert.Null(user.DisabledAt);
        Assert.True(AccessControl.IsAllowed(user));
    }

    [Fact]
    public async Task The_seeded_user_is_findable_by_a_normalized_lookup()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var seeded = SeedData.AllowedUsers().Single();

        // The exact lookup M55 performs: normalize whatever the provider sent, then ordinal-compare.
        // Casing or padding from the identity provider must not miss the row.
        var probe = EmailNormalization.Normalize($"  {seeded.Email.ToUpperInvariant()} ");
        var found = await db.AllowedUsers.SingleOrDefaultAsync(u => u.Email == probe);

        Assert.NotNull(found);
        Assert.Equal(seeded.Id, found!.Id);
    }

    [Fact]
    public async Task A_session_round_trips_its_ticket_bytes()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "roundtrip@example.com");
        var sessionId = Guid.NewGuid().ToString("N");

        db.AuthSessions.Add(NewSession(sessionId, user));
        await db.SaveChangesAsync();

        // bytea → BLOB is the ticket store's entire contract; a lossy round trip would corrupt every
        // session in a way that only shows up as "randomly signed out".
        var stored = await db.AuthSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, stored.TicketData);
        Assert.Equal(user.Email, stored.Email);
    }

    [Fact]
    public async Task Deleting_a_user_cascades_to_their_sessions()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var user = await AddUserAsync(db, "cascade@example.com");
        var sessionId = Guid.NewGuid().ToString("N");
        db.AuthSessions.Add(NewSession(sessionId, user));
        await db.SaveChangesAsync();

        db.AllowedUsers.Remove(user);
        await db.SaveChangesAsync();

        // Hard revocation. Contrast DisabledAt, which denies access but keeps both the row and the
        // record that the person was ever here — the two are different tools on purpose.
        Assert.Null(await db.AuthSessions.SingleOrDefaultAsync(s => s.Id == sessionId));
    }

    [Fact]
    public async Task Two_users_cannot_share_an_address()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        var seeded = SeedData.AllowedUsers().Single();

        db.AllowedUsers.Add(new AllowedUser
        {
            Id = Guid.NewGuid(),
            Email = seeded.Email,
            CreatedAt = DateTime.UtcNow,
        });

        // The unique index is the last line of defence behind EmailNormalization: two rows for one
        // person would mean access that survives revoking "the" row.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task The_data_protection_key_ring_is_persisted_to_the_database()
    {
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);

        // Exercise the key ring the way issuing a session cookie would.
        scope.ServiceProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("zmg.test")
            .Protect("payload");

        var keys = await db.DataProtectionKeys.ToListAsync();

        // The assertion that matters in production: keys land in Postgres rather than on the
        // container's ephemeral filesystem. Without this, ACA's min_replicas=0 mints a fresh key ring
        // on every scale-from-zero — roughly every five idle minutes — silently invalidating every
        // session cookie. It is the failure that would present as "the app randomly logs me out".
        Assert.NotEmpty(keys);
        Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(k.Xml)));
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

    private static AuthSession NewSession(string id, AllowedUser user) => new()
    {
        Id = id,
        AllowedUserId = user.Id,
        Email = user.Email,
        TicketData = [1, 2, 3, 4],
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
    };
}
