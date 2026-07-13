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
/// Boots the real app against a private SQLite in-memory database. The connection is
/// kept open for the factory's lifetime so the schema (created via Migrate) survives
/// between requests. Each factory instance is an isolated database.
/// </summary>
public class ZmgApiFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Drop the app's SQLite-file DbContext registration.
            services.RemoveAll<DbContextOptions<ZmgDbContext>>();
            services.RemoveAll<ZmgDbContext>();

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
