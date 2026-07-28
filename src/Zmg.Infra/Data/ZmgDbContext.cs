using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zmg.Domain;
using Zmg.Domain.Entities;

namespace Zmg.Infra.Data;

/// <summary>
/// Also the Data Protection key store (v2.10/M54) — see <see cref="DataProtectionKeys"/>.
/// </summary>
public class ZmgDbContext(DbContextOptions<ZmgDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongArtist> SongArtists => Set<SongArtist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<ReleaseTask> ReleaseTasks => Set<ReleaseTask>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<TemplateTask> TemplateTasks => Set<TemplateTask>();

    // ---- Authentication (v2.10/M54) ----

    public DbSet<AllowedUser> AllowedUsers => Set<AllowedUser>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    /// <summary>
    /// The ASP.NET Data Protection key ring, satisfying <see cref="IDataProtectionKeyContext"/>.
    /// Nothing in this codebase reads it — the framework does, via <c>PersistKeysToDbContext</c>.
    /// It lives in the database because the container filesystem is ephemeral on ACA, and keys lost
    /// on a scale-from-zero would silently sign every user out (see the note in Zmg.Infra.csproj).
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Artist>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            // Case-insensitive uniqueness is enforced in app logic (SQLite NOCASE is opt-in);
            // an index keeps lookups cheap and marks intent.
            e.HasIndex(x => x.Name);
        });

        b.Entity<Release>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
            e.HasOne(x => x.MainArtist)
                .WithMany(a => a.Releases)
                .HasForeignKey(x => x.MainArtistId)
                .OnDelete(DeleteBehavior.Restrict); // artist with releases can't be deleted
        });

        b.Entity<Song>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
            e.HasOne(x => x.MainArtist)
                .WithMany(a => a.Songs)
                .HasForeignKey(x => x.MainArtistId)
                .OnDelete(DeleteBehavior.Restrict); // artist who's a song's main artist can't be deleted
            e.HasIndex(x => x.Title);
        });

        b.Entity<SongArtist>(e =>
        {
            e.HasKey(x => new { x.SongId, x.ArtistId });
            e.HasOne(x => x.Song)
                .WithMany(s => s.Artists)
                .HasForeignKey(x => x.SongId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Artist)
                .WithMany(a => a.SongCredits)
                .HasForeignKey(x => x.ArtistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Track>(e =>
        {
            // Composite PK: structurally prevents the same song appearing twice on one release.
            e.HasKey(x => new { x.ReleaseId, x.SongId });
            e.HasOne(x => x.Release)
                .WithMany(r => r.Tracks)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Song)
                .WithMany(s => s.ReleaseLinks)
                .HasForeignKey(x => x.SongId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReleaseTask>(e =>
        {
            e.HasKey(x => x.Id);
            // English is required and is the fallback; Spanish is nullable, and null legitimately means
            // "reads the same in both languages" rather than "not translated yet".
            e.Property(x => x.TitleEn).IsRequired();
            e.HasOne(x => x.Release)
                .WithMany(r => r.Tasks)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChecklistTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Tasks)
                .WithOne(t => t.ChecklistTemplate!)
                .HasForeignKey(t => t.ChecklistTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TemplateTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TitleEn).IsRequired();
            // Code is the stable identity of a seeded task, null for user-added ones. Unique per
            // template — a filtered index would exclude the nulls, but SQLite and Postgres disagree on
            // filtered-index syntax and the tests run SQLite, so uniqueness stays an app-level invariant
            // of SeedData and the index exists for lookup.
            e.HasIndex(x => new { x.ChecklistTemplateId, x.Code });
        });

        b.Entity<AllowedUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(320); // RFC 5321 max: 64 local + @ + 255 domain
            // Unique, not merely indexed — unlike Artist.Name, whose case-insensitive uniqueness is an
            // app-level rule. Here the value is already normalized by EmailNormalization before it is
            // ever written, so ordinal uniqueness in the database is exactly the invariant we want, and
            // it is the last line of defence against two rows granting access to the same person.
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<AuthSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Email).IsRequired().HasMaxLength(320);
            e.Property(x => x.TicketData).IsRequired();
            // Sessions die with the user. Contrast AllowedUser.DisabledAt, which is the *reversible*
            // revocation that keeps the row; a delete is the deliberate hard one.
            e.HasOne(x => x.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(x => x.AllowedUserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Drives the expired-row sweep the ticket store runs on sign-in.
            e.HasIndex(x => x.ExpiresAt);
        });

        // The bootstrap whitelist entry, so a fresh database isn't locked out of its own login screen.
        foreach (var user in SeedData.AllowedUsers())
        {
            b.Entity<AllowedUser>().HasData(new AllowedUser
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                DisabledAt = user.DisabledAt,
            });
        }

        // Seed both templates and their tasks (build-plan.md section 5.4).
        foreach (var template in SeedData.Templates())
        {
            b.Entity<ChecklistTemplate>().HasData(new ChecklistTemplate { Id = template.Id, Type = template.Type });
        }
        foreach (var task in SeedData.AllTemplateTasks())
        {
            b.Entity<TemplateTask>().HasData(new TemplateTask
            {
                Id = task.Id,
                ChecklistTemplateId = task.ChecklistTemplateId,
                Code = task.Code,
                TitleEn = task.TitleEn,
                TitleEs = task.TitleEs,
                Phase = task.Phase,
                SortOrder = task.SortOrder,
                MinDaysBefore = task.MinDaysBefore,
                MaxDaysBefore = task.MaxDaysBefore,
            });
        }
    }
}
