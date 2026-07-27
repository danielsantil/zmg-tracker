using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Zmg.Api.Contracts;
using Zmg.Api.Services;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M46 — the API ships culture-invariant <see cref="Message"/> codes and the SPA owns every sentence.
/// Two things need locking: the wire shape (code + args, no prose), and the fact that every code the
/// server can emit has a key on the other side. The second is the real failure mode — a code with no
/// i18next key renders as its own key path, in both languages, with everything else perfectly green.
/// </summary>
public class MessageCodeApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    // ---- Wire shape ----

    [Fact]
    public async Task A_validation_failure_ships_a_code_and_no_prose()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput("   ", null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(Validation.ArtistNameRequiredCode, body!.Errors.Single().Code);

        // And no prose field rode along beside it — one channel, nothing to drift out of sync.
        var raw = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var error = raw.RootElement.GetProperty("errors")[0];
        Assert.Equal(["code", "args"], error.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task An_interpolating_message_ships_its_args()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/artists", new ArtistInput("Args Artist", null));

        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput("args artist", null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var error = (await res.Content.ReadFromJsonAsync<ValidationErrorResponse>())!.Errors.Single();
        Assert.Equal(Validation.DuplicateArtistNameCode, error.Code);
        Assert.Equal("args artist", error.Args!["name"]);
    }

    [Fact]
    public async Task A_conflict_ships_a_code_too()
    {
        var client = factory.CreateClient();
        var artist = (await (await client.PostAsJsonAsync("/api/artists", new ArtistInput("Conflict Code Artist", null)))
            .Content.ReadFromJsonAsync<ArtistDto>())!;
        var created = (await (await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
                "Conflict Code Release", ReleaseType.Single, TestDates.Today.AddDays(10), artist.Id,
                null, null, new List<TrackInput> { new(null, "Conflict Code Song", null, null) })))
            .Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        (await client.PostAsync($"/api/releases/{created.Id}/archive", null)).EnsureSuccessStatusCode();

        var res = await client.PutAsJsonAsync($"/api/releases/{created.Id}", new ReleaseInput(
            "Renamed", ReleaseType.Single, TestDates.Today.AddDays(10), artist.Id, null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(ReleaseMutability.ArchivedReadOnlyCode, body!.Errors.Single().Code);
    }

    [Fact]
    public async Task Create_warnings_ride_the_envelope_as_codes()
    {
        var client = factory.CreateClient();
        var artist = (await (await client.PostAsJsonAsync("/api/artists", new ArtistInput("Warning Code Artist", null)))
            .Content.ReadFromJsonAsync<ArtistDto>())!;

        // A past release date is the advisory that doesn't block.
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Backfilled", ReleaseType.Single, TestDates.Today.AddDays(-30), artist.Id, null, null,
            new List<TrackInput> { new(null, "Backfilled Song", null, null) }));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        Assert.Contains(created.Warnings, w => w.Code == Validation.PastReleaseDateCode);
    }

    // ---- Code ↔ translation parity ----

    /// <summary>
    /// Every <c>error.*</c> / <c>warning.*</c> constant across both projects, found by reflection so a
    /// code added tomorrow is covered without touching this test. Only the classes that actually mint
    /// codes are scanned — a broad assembly sweep would drag in unrelated string constants.
    /// </summary>
    public static TheoryData<string> AllCodes()
    {
        Type[] sources =
        [
            typeof(Validation), typeof(ReleaseWarnings), typeof(PendingActions),
            typeof(ReleaseMutability), typeof(CoverImage), typeof(ServiceErrors),
        ];

        var data = new TheoryData<string>();
        foreach (var code in sources
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(v => v.StartsWith("error.", StringComparison.Ordinal) || v.StartsWith("warning.", StringComparison.Ordinal))
            .Distinct()
            .OrderBy(v => v, StringComparer.Ordinal))
        {
            data.Add(code);
        }

        Assert.NotEmpty(data); // reflection returning nothing would make this whole file a no-op
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCodes))]
    public void Every_code_has_a_key_in_both_locales(string code)
    {
        foreach (var locale in new[] { "en", "es" })
        {
            var text = Lookup(LocaleFile(locale), code);
            Assert.False(string.IsNullOrWhiteSpace(text), $"{locale}.json is missing a value for '{code}'.");
        }
    }

    private static readonly Dictionary<string, JsonElement> LocaleCache = new();

    private static JsonElement LocaleFile(string locale)
    {
        if (LocaleCache.TryGetValue(locale, out var cached)) return cached;

        // Walk up from the test binary to the repo root — the SPA's locale files are the counterpart
        // being checked, and they live outside any project this assembly references.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Zmg.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "Zmg.Web", "src", "i18n", "locales", $"{locale}.json");
        var root = JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
        LocaleCache[locale] = root;
        return root;
    }

    /// <summary>Resolves a dotted i18next key path against the nested JSON, or null if any hop is missing.</summary>
    private static string? Lookup(JsonElement root, string code)
    {
        var node = root;
        foreach (var segment in code.Split('.'))
        {
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(segment, out node)) return null;
        }
        return node.ValueKind == JsonValueKind.String ? node.GetString() : null;
    }
}
