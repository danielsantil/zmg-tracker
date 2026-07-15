using Microsoft.EntityFrameworkCore;
using Zmg.Domain;
using Zmg.Domain.Entities;

namespace Zmg.Infra.Data;

public class ZmgDbContext(DbContextOptions<ZmgDbContext> options) : DbContext(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongArtist> SongArtists => Set<SongArtist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<ReleaseTask> ReleaseTasks => Set<ReleaseTask>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<TemplateTask> TemplateTasks => Set<TemplateTask>();

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
            // Soft-delete (v1.2): removed releases are never hard-deleted, just hidden everywhere.
            e.HasQueryFilter(x => x.DeletedAt == null);
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
            // Soft-delete (v2.0): removed songs are hidden everywhere. Stale join rows are hidden by
            // the Track query filter below.
            e.HasQueryFilter(x => x.DeletedAt == null);
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
            // A join between two soft-filtered entities must vanish with either parent; this also
            // silences EF's required-nav-to-filtered-entity warning.
            e.HasQueryFilter(x => x.Release!.DeletedAt == null && x.Song!.DeletedAt == null);
        });

        b.Entity<ReleaseTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
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
            e.Property(x => x.Title).IsRequired();
        });

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
                Title = task.Title,
                Phase = task.Phase,
                SortOrder = task.SortOrder,
                MinDaysBefore = task.MinDaysBefore,
                MaxDaysBefore = task.MaxDaysBefore,
            });
        }
    }
}
