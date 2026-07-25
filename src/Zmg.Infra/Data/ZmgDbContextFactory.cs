using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zmg.Infra.Data;

// Takes precedence over EF's default "boot the startup project to find the DbContext" discovery. Without
// it, `dotnet ef` runs Program.cs — which calls Configuration.Validate() and demands every R2:* setting —
// so building the migration bundle would throw in CI, where only the connection string exists.
public class ZmgDbContextFactory : IDesignTimeDbContextFactory<ZmgDbContext>
{
    public ZmgDbContext CreateDbContext(string[] args)
    {
        // Generating migrations and bundles needs the provider, not a live connection; the real
        // connection string is passed to the bundle at run time via --connection.
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Zmg")
                 ?? "Host=localhost;Database=zmg;Username=postgres";

        return new ZmgDbContext(new DbContextOptionsBuilder<ZmgDbContext>().UseNpgsql(cs).Options);
    }
}