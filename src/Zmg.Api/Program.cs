using Microsoft.EntityFrameworkCore;
using Zmg.Api.Endpoints;
using Zmg.Api.Extensions;
using Zmg.Infra.Data;

// Start logging startup boot time
var bootStart = Environment.TickCount64;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on any missing required setting (connection string + all R2:* keys), naming every offender
// at once, rather than letting a null surface deep inside the first request that needs it. Prod supplies
// these as ACA secrets; dev via user-secrets; tests via dummy UseSetting values (never dereferenced).
builder.Configuration.Validate();

var connectionString = builder.Configuration.GetConnectionString("Zmg");
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.RegisterServices(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    builder.Services.AddCors(options =>
        options.AddPolicy("dev", p => p
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));
}

var app = builder.Build();

app.Logger.LogInformation("[boot] built {Ms} ms", Environment.TickCount64 - bootStart);

// Migrate at startup by default: `dotnet run` gets a ready database with seeded templates, and the
// integration tests rely on this call for their SQLite schema (see ZmgApiFactory). Prod sets
// Database__MigrateOnStartup=false and applies migrations from the deploy.yml pipeline instead,
// improving startup boot time. If key not found, migration is still applied on startup.
if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
    db.Database.Migrate();
    app.Logger.LogInformation("[boot] Database migrated on startup.");
}

app.Logger.LogInformation("[boot] DB ready {Ms} ms", Environment.TickCount64 - bootStart);

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapArtistEndpoints();
app.MapReleaseEndpoints();
app.MapSongEndpoints();
app.MapTaskEndpoints();
app.MapTemplateEndpoints();
app.MapTrackEndpoints();
app.MapPendingEndpoints();
app.MapUploadEndpoints();

// Serve the built SPA (wwwroot) in production; SPA fallback for client-side routing.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Lifetime.ApplicationStarted.Register(() => 
    app.Logger.LogInformation("[boot] Application started - listening {Ms} ms", Environment.TickCount64 - bootStart));

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
