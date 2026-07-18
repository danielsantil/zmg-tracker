using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

/// <summary>
/// One ObjectMother for the pure-domain entity graphs (M25 task 9). Before this, six near-identical
/// private builders were scattered across the test files (`Task`, `Rel`, `SongOn`); they now live here.
/// Every field has a sensible default so a test names only what it cares about.
/// </summary>
internal static class Builders
{
    public static ReleaseTask Task(string title = "Task", Phase phase = Phase.Pre, bool done = false,
        int? min = null, int? max = null, int sort = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Phase = phase,
            SortOrder = sort,
            IsDone = done,
            MinDaysBefore = min,
            MaxDaysBefore = max,
        };

    public static Release Release(DateOnly date, string title = "Song", string? upc = null,
        ReleaseType type = ReleaseType.Single, bool archived = false, params ReleaseTask[] tasks) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Type = type,
            ReleaseDate = date,
            MainArtist = new Artist { Name = "Artist" },
            Upc = upc,
            ArchivedAt = archived ? DateTime.UtcNow : null,
            Tasks = tasks.ToList(),
            // A single carries exactly one track; an album's tracklist is set per test.
            Tracks = type == ReleaseType.Single ? new List<Track> { new() } : new List<Track>(),
        };

    public static Song Song(string title = "Song") =>
        new() { Id = Guid.NewGuid(), Title = title, MainArtist = new Artist { Name = "Artist" } };

    // A song linked to the given releases (each join row points back at its loaded release + the song).
    public static Song SongOn(params Release[] releases)
    {
        var song = Song();
        song.ReleaseLinks = releases
            .Select(r => new Track { ReleaseId = r.Id, Release = r, SongId = song.Id, Song = song })
            .ToList();
        return song;
    }
}
