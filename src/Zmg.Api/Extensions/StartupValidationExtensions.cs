using Microsoft.Extensions.Configuration;
using Zmg.Api.Services;

namespace Zmg.Api.Extensions;

/// <summary>
/// Boot-time configuration guard. Every required setting is checked once, up front, so a misconfigured
/// deploy fails immediately with a message naming <em>all</em> offenders — rather than booting and then
/// surfacing as a null-reference deep inside the first request that happens to need one. Runs in every
/// environment (tests included) so the test suite exercises the same validated startup path as prod.
/// </summary>
public static class StartupValidationExtensions
{
    /// <summary>
    /// Collects every missing/blank required key and throws a single <see cref="InvalidOperationException"/>
    /// listing them all. Does not stop at the first offender.
    /// </summary>
    public static void Validate(this IConfiguration configuration)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Zmg")))
        {
            missing.Add("ConnectionStrings__Zmg");
        }

        var r2 = new R2Options();
        configuration.GetSection(R2Options.SectionName).Bind(r2);
        missing.AddRange(r2.MissingKeys());

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required configuration: " + string.Join(", ", missing) +
                ". Set them via environment variables (dev: dotnet user-secrets; prod: ACA secrets).");
        }
    }
}
