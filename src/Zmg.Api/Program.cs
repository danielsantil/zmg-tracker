using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zmg.Api.Endpoints;
using Zmg.Api.Extensions;
using Zmg.Api.Logging;
using Zmg.Infra.Data;

// Start logging startup boot time
var bootStart = Environment.TickCount64;

var builder = WebApplication.CreateBuilder(args);

// One JSON object per line to stdout, which ACA collects into ContainerAppConsoleLogs_CL and KQL can
// parse_json — no sink, no package, no network call, so there is nothing here that can fail closed and
// take the app down with it (v2.10/M57). Scopes are included because the request id rides in one.
// Dev keeps the readable single-line console: JSON at a terminal is a downgrade.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole(o =>
    {
        o.IncludeScopes = true;
        o.UseUtcTimestamp = true;
        o.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        o.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    });
}

// Fail fast on any missing required setting (connection string + all R2:* keys), naming every offender
// at once, rather than letting a null surface deep inside the first request that needs it. Prod supplies
// these as ACA secrets; dev via user-secrets; tests via dummy UseSetting values (never dereferenced).
builder.Configuration.Validate();

var connectionString = builder.Configuration.GetConnectionString("Zmg");
builder.Services.AddDbContext<ZmgDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.RegisterServices(builder.Configuration);
builder.Services.AddZmgAuthentication(builder.Configuration, builder.Environment);

// One shape for every unhandled exception: logged once with its stack, answered with the same coded
// envelope as every other failure. AddProblemDetails is required for the parameterless
// UseExceptionHandler() below — it supplies the fallback the middleware demands, though our handler
// always claims the exception first.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

// Outermost, so every line below — including the exception handler's — carries the request id, and so
// the id reaches the response even when the pipeline blows up.
app.UseMiddleware<CorrelationMiddleware>();

// Outside the exception handler on purpose: by the time this middleware's finally runs, an unhandled
// exception has already become a 500 and is reported as a failed request rather than escaping unmeasured.
app.UseMiddleware<RequestSummaryMiddleware>();
app.UseExceptionHandler();

// Before authentication, and before anything reads Request.Host: the Cloudflare Worker rewrites Host
// to the ACA FQDN (the ingress routes on it), so the public hostname arrives as X-Forwarded-Host and
// the OIDC redirect_uri would otherwise be built from the wrong one.
app.UseZmgForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
    app.UseSwagger();
    app.UseSwaggerUI();
}

// BEFORE authentication/authorization, and this ordering is load-bearing (v2.10/M59).
//
// The built SPA's assets are file paths that match no endpoint — MapFallbackToFile's catch-all carries
// the `nonfile` constraint, so anything with a dot in it is excluded by design. And "no endpoint" is
// precisely when AuthorizationMiddleware applies the fallback policy, so with these registered after
// it every /assets/*.js and *.css answered an anonymous browser with a 302 to /Account/Login. The
// shell loaded, its scripts did not, and the login screen it exists to render never appeared. Prod hid
// it: Cloudflare serves the assets from the edge, so only the container — the documented rollback
// target — was a blank page.
//
// Serving them first is also the framework's documented order. Nothing is exposed by it: wwwroot holds
// only the built SPA, which the edge already serves to anonymous users, and the shell is deliberately
// anonymous because it is what renders the login gate.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Anonymous by necessity: the readiness probe, and the auth endpoints themselves. Everything else is
// protected by the fallback policy without having to say so.
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapAuthEndpoints();
app.MapArtistEndpoints();
app.MapReleaseEndpoints();
app.MapSongEndpoints();
app.MapTaskEndpoints();
app.MapTemplateEndpoints();
app.MapTrackEndpoints();
app.MapPendingEndpoints();
app.MapUploadEndpoints();

// The SPA fallback for client-side routing (the files themselves are served above, before the auth
// middleware). MapFallbackToFile *is* an endpoint, so it opts out of the fallback policy explicitly —
// the shell has to be anonymous, since it is what renders the login screen.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Lifetime.ApplicationStarted.Register(() => 
    app.Logger.LogInformation("[boot] Application started - listening {Ms} ms", Environment.TickCount64 - bootStart));

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
