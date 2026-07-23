using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// Boots the real app against a private SQLite in-memory database. The connection is kept open for the
/// factory's lifetime so the schema survives between requests; the app's own startup creates it via
/// <c>db.Database.Migrate()</c> (Program.cs), which this factory does not call itself. Each factory
/// instance is an isolated database.
/// </summary>
public class ZmgApiFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Satisfy Program.cs's fail-fast startup validation so every test boots the same validated path as
        // prod. The connection string is overridden below with the shared open in-memory connection; the
        // dummy R2 values are never dereferenced (UploadApiFactory swaps in FakeStorageService).
        builder.UseSetting("ConnectionStrings:Zmg", "DataSource=:memory:");
        builder.UseSetting("R2:AccountId", "test-account");
        builder.UseSetting("R2:AccessKeyId", "test-access-key");
        builder.UseSetting("R2:SecretAccessKey", "test-secret-access-key");
        builder.UseSetting("R2:Bucket", "test-bucket");
        builder.UseSetting("R2:PublicBaseUrl", "https://covers.test");

        builder.ConfigureServices(services =>
        {
            // Drop the app's SQLite-file DbContext registration.
            services.RemoveAll<DbContextOptions<ZmgDbContext>>();
            services.RemoveAll<ZmgDbContext>();

            // Dispose any connection from a prior host build before replacing it (no leak on rebuild).
            _connection?.Dispose();
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ZmgDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
