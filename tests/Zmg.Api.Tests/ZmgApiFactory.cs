using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Zmg.Api.Tests;

/// <summary>
/// Boots the real app against a private Postgres database. One container is shared across the whole test
/// run (Testcontainers' Ryuk reaps it at process exit); each factory instance gets its own freshly
/// created database, so isolation matches the old per-factory model. The app's own startup
/// <c>Migrate()</c> (Program.cs) builds the schema + seed in that database.
/// </summary>
public class ZmgApiFactory : WebApplicationFactory<Program>
{
    private static readonly PostgreSqlContainer Postgres = StartShared();
    private readonly string _dbName = $"zmg_{Guid.NewGuid():N}";

    private static PostgreSqlContainer StartShared()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        container.StartAsync().GetAwaiter().GetResult();
        return container;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Carve out an isolated database for this factory instance in the shared container.
        var csb = new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString());
        using (var conn = new NpgsqlConnection(csb.ConnectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            cmd.ExecuteNonQuery();
        }
        csb.Database = _dbName;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Zmg", csb.ConnectionString);
    }
}
