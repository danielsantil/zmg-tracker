using Microsoft.EntityFrameworkCore;
using Zmg.Api.Endpoints;
using Zmg.Api.Services;
using Zmg.Api.Services.Interfaces;
using Zmg.Infra.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Zmg");
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<IReleaseService, ReleaseService>();
builder.Services.AddScoped<IPendingService, PendingService>();
builder.Services.AddScoped<ITrackService, TrackService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IReleaseTaskService, ReleaseTaskService>();

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
app.MapTaskEndpoints();
app.MapTemplateEndpoints();
app.MapTrackEndpoints();
app.MapPendingEndpoints();

// Serve the built SPA (wwwroot) in production; SPA fallback for client-side routing.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
