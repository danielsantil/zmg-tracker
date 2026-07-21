using Microsoft.EntityFrameworkCore;
using Zmg.Api.Endpoints;
using Zmg.Api.Services;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Infra.Data;

var builder = WebApplication.CreateBuilder(args);

// Production supplies this via ConnectionStrings__Zmg (see the Dockerfile); Development has it in
// appsettings.Development.json. Fail fast with a clear message rather than letting UseSqlite receive null.
var connectionString = builder.Configuration.GetConnectionString("Zmg")
    ?? throw new InvalidOperationException(
        "Connection string 'Zmg' is not configured. Set ConnectionStrings__Zmg.");
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<IReleaseService, ReleaseService>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IPendingService, PendingService>();
builder.Services.AddScoped<ITrackService, TrackService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IReleaseTaskService, ReleaseTaskService>();

// Cover images (M31). The S3 client is built lazily inside the service, so a box without the R2:*
// settings still boots — only an upload attempt fails, with a clear message.
builder.Services.Configure<R2Options>(builder.Configuration.GetSection(R2Options.SectionName));
builder.Services.AddSingleton<IStorageService, R2StorageService>();
builder.Services
    .AddHttpClient<ICoverUploadService, CoverUploadService>(http =>
    {
        // A remote host must not be able to hold a request open; the SSRF guards cover *where* we
        // dial, this covers *how long*.
        http.Timeout = TimeSpan.FromSeconds(10);
        http.MaxResponseContentBufferSize = CoverImage.MaxBytes;
    })
    // Redirects are followed by hand in CoverUploadService so every hop is re-checked against the
    // blocklist — auto-redirect would dial the second host before anything could inspect it.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

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
