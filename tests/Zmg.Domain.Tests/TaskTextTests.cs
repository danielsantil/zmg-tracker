namespace Zmg.Domain.Tests;

/// <summary>
/// M47 — per-locale checklist text, resolved by lookup. The whole point is that lookup <b>never</b>
/// loses text: every miss falls back to the stored English title, because that column is where
/// English lives and the alternative is rendering a raw slug at the user.
/// </summary>
public class TaskTextTests
{
    private static readonly Dictionary<string, string> Spanish = new()
    {
        [TaskCodes.MixMaster] = "Mezcla/master",
        [TaskCodes.DistributeToDsps] = "Distribuir a DSPs",
    };

    [Fact]
    public void A_coded_task_resolves_its_translation()
    {
        Assert.Equal("Mezcla/master", TaskText.Resolve(TaskCodes.MixMaster, "Mix/master", Spanish));
    }

    [Theory]
    [InlineData(null)]                          // user-added task — never translated
    [InlineData("")]                            // defensive: a blank code is not a lookup key
    [InlineData("code-with-no-row-yet")]        // seeded task whose Spanish hasn't landed
    public void Anything_without_a_translation_falls_back_to_the_stored_title(string? code)
    {
        Assert.Equal("Mix/master", TaskText.Resolve(code, "Mix/master", Spanish));
    }

    [Fact]
    public void A_null_or_empty_map_falls_back_too()
    {
        // The `en` path: no rows exist by design, so resolution is the column itself.
        Assert.Equal("Mix/master", TaskText.Resolve(TaskCodes.MixMaster, "Mix/master", null));
        Assert.Equal("Mix/master", TaskText.Resolve(TaskCodes.MixMaster, "Mix/master", new Dictionary<string, string>()));
    }

    [Fact]
    public void A_blank_translation_falls_back_rather_than_rendering_empty()
    {
        var blank = new Dictionary<string, string> { [TaskCodes.MixMaster] = "   " };
        Assert.Equal("Mix/master", TaskText.Resolve(TaskCodes.MixMaster, "Mix/master", blank));
    }

    [Theory]
    [InlineData("es", "es")]
    [InlineData("ES", "es")]
    [InlineData("es-MX", "es")]
    [InlineData("es-419,es;q=0.9,en;q=0.8", "es")] // a real Accept-Language header
    [InlineData("en", "en")]
    [InlineData("fr", "en")]                       // unsupported → English, never a 400
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void Locale_normalization_strips_region_and_falls_back_to_english(string? raw, string expected)
    {
        Assert.Equal(expected, TaskText.NormalizeLocale(raw));
    }
}
