using Zmg.Api.Contracts;
using Zmg.Domain.Entities;

namespace Zmg.Api.Services;

/// <summary>
/// Shared song/track helpers used by both <see cref="ReleaseService"/> (create-form Tracks) and
/// <see cref="TrackService"/> (add-track endpoint): building a new inline song and projecting a
/// track join (+ its song) to the wire DTO.
/// </summary>
internal static class SongMapping
{
    /// <summary>
    /// Build a new inline song: main artist inherited from the release, ISRC cleaned, feat/collab
    /// artists deduped and excluding the main artist. The caller assigns it to a track.
    /// </summary>
    public static Song NewSong(Guid mainArtistId, string title, string? isrc, List<SongArtistInput>? artists)
    {
        var song = new Song
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            MainArtistId = mainArtistId,
            Isrc = string.IsNullOrWhiteSpace(isrc) ? null : isrc.Trim(),
        };

        if (artists is not null)
        {
            foreach (var a in artists.Where(a => a.ArtistId != mainArtistId).DistinctBy(a => a.ArtistId))
            {
                song.Artists.Add(new SongArtist { SongId = song.Id, ArtistId = a.ArtistId, Role = a.Role });
            }
        }

        return song;
    }

    /// <summary>Project a loaded track join (with its Song and the song's artists) to a <see cref="TrackDto"/>.</summary>
    public static TrackDto ToDto(Track track)
    {
        var song = track.Song!;
        var artists = song.Artists
            .Select(a => new SongArtistDto(a.ArtistId, a.Artist?.Name ?? string.Empty, a.Role))
            .ToList();
        return new TrackDto(song.Id, track.TrackNumber, song.Title, song.Isrc, track.IsFocusTrack, artists);
    }
}
