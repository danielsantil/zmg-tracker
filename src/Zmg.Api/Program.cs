using Microsoft.EntityFrameworkCore;
using Zmg.Api.Data;
using Zmg.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Zmg")
    ?? "Data Source=zmg.db";
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddCors(options =>
    options.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

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
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapArtistEndpoints();
app.MapReleaseEndpoints();

// Serve the built SPA (wwwroot) in production; SPA fallback for client-side routing.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
