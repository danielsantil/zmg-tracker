using Microsoft.EntityFrameworkCore;
using Zmg.Api.Endpoints;
using Zmg.Api.Extensions;
using Zmg.Infra.Data;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on any missing required setting (connection string + all R2:* keys), naming every offender
// at once, rather than letting a null surface deep inside the first request that needs it. Prod supplies
// these as ACA secrets; dev via user-secrets; tests via dummy UseSetting values (never dereferenced).
builder.Configuration.Validate();

var connectionString = builder.Configuration.GetConnectionString("Zmg");
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.RegisterServices(builder.Configuration);

builder.Services.AddCors(options =>
    options.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations at startup so `dotnet run` gives a ready database with seeded templates.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
    db.Database.Migrate();
}

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

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
