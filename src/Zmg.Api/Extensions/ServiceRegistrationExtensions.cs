using Zmg.Api.Services;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Register DI services
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IReleaseService, ReleaseService>();
        services.AddScoped<ISongService, SongService>();
        services.AddScoped<IPendingService, PendingService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IReleaseTaskService, ReleaseTaskService>();
        
        // Cover images (M31). R2 is required at startup (M35) — the env-var validator fails the boot if any
        // R2:* setting is missing, so the eagerly-built S3 client always has real credentials to work with.
        services.Configure<R2Options>(configuration.GetSection(R2Options.SectionName));
        // Singleton, not scoped like the DbContext-backed services: the S3 client owns a connection pool, and
        // R2StorageService disposes it — scoped would build and tear down a pool on every upload.
        services.AddSingleton<IStorageService, R2StorageService>();
        services
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
    }
}