using System.Text.Json;

namespace Zmg.Api.Extensions;

/// <summary>
/// Environment-conditional host wiring, pulled out of Program.cs so the pipeline there reads as one
/// unbranched sequence. Each method owns its own environment check, so Program.cs calls them
/// unconditionally rather than repeating <c>IsDevelopment()</c> at three sites.
/// </summary>
public static class EnvironmentExtensions
{
    private const string DevCorsPolicy = "dev";

    /// <summary>
    /// Structured JSON logs to stdout outside Development (v2.10/M57): one object per line, which ACA
    /// collects into <c>ContainerAppConsoleLogs_CL</c> for KQL's <c>parse_json</c> — no sink, no package
    /// and no network call, so nothing here can fail closed and take the app down. Scopes are included
    /// because the request id rides in one. Dev keeps the readable single-line console; JSON at a
    /// terminal is a downgrade.
    /// </summary>
    public static void AddZmgConsoleLogging(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment()) return;

        builder.Logging.AddJsonConsole(o =>
        {
            o.IncludeScopes = true;
            o.UseUtcTimestamp = true;
            o.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            o.JsonWriterOptions = new JsonWriterOptions { Indented = false };
        });
    }

    /// <summary>Swagger and a permissive localhost CORS policy — registered only in Development.</summary>
    public static void AddDevTooling(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
            options.AddPolicy(DevCorsPolicy, p => p
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()));
    }

    /// <summary>The middleware half of <see cref="AddDevTooling"/>: the dev CORS policy and Swagger UI.</summary>
    public static void UseDevTooling(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.UseCors(DevCorsPolicy);
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}
