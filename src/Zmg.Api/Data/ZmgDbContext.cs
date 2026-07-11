using Microsoft.EntityFrameworkCore;
using Zmg.Domain;

namespace Zmg.Api.Data;

public class ZmgDbContext : DbContext
{
    public ZmgDbContext(DbContextOptions<ZmgDbContext> options) : base(options) { }

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<ReleaseArtist> ReleaseArtists => Set<ReleaseArtist>();
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
        });

        b.Entity<ReleaseArtist>(e =>
        {
            e.HasKey(x => new { x.ReleaseId, x.ArtistId });
            e.HasOne(x => x.Release)
                .WithMany(r => r.FeaturedArtists)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Artist)
                .WithMany(a => a.ReleaseCredits)
                .HasForeignKey(x => x.ArtistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Track>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
            e.HasOne(x => x.Release)
                .WithMany(r => r.Tracks)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
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
            });
        }
    }
}
