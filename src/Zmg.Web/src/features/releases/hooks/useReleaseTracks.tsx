import { useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api, ApiError, errorMessage, DUPLICATE_SONG_TITLE_MESSAGE } from '@/api';
import { queryKeys } from '@/api/queries';
import type { ReleaseDetail, SongListItem, TrackDto } from '@/types';
import { useConfirm } from '@/hooks/useConfirm';
import type { NewTrackDraft, TracklistRow } from '../components/Tracklist';

/**
 * The tracklist half of the release detail (M24.7): an optimistically-mutated track array seeded
 * from the release query, plus add/add-existing/toggle-focus/remove/move and the duplicate-title
 * prompt. Renumbering mirrors the server's contiguous track numbers; tracklist changes can flip
 * pending actions, so those queries are invalidated on success.
 */
export function useReleaseTracks(
  release: ReleaseDetail,
  initial: TrackDto[],
  showToast: (msg: string) => void,
) {
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const id = release.id;
  const [tracks, setTracks] = useState<TrackDto[]>(initial);
  // Set when a new track's title collides with an existing song for this artist (M19).
  const [dupPrompt, setDupPrompt] = useState<{ title: string; existing: SongListItem | null; onRelease: boolean } | null>(null);

  useEffect(() => setTracks(initial), [initial]);

  // Tracklist edits can change pending actions and a release's warnings (e.g. "Album is empty").
  const refreshPending = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.pending });
    void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
  };

  const orderedTracks = useMemo(
    () => [...tracks].sort((a, b) => a.trackNumber - b.trackNumber),
    [tracks],
  );

  // Adapter onto the shared Tracklist (M18): rows are keyed by songId, so `track()` maps a row back
  // to exactly one track for the optimistic api.tracks.* handlers below.
  const trackRows: TracklistRow[] = useMemo(
    () =>
      orderedTracks.map((t) => ({
        key: t.songId,
        trackNumber: t.trackNumber,
        title: t.title,
        isrc: t.isrc,
        songId: t.songId,
        isFocusTrack: t.isFocusTrack,
        isSongArchived: t.isSongArchived,
      })),
    [orderedTracks],
  );
  // Invariant: trackRows is derived from orderedTracks (keyed by songId), so every row maps back.
  const track = (row: TracklistRow) => orderedTracks.find((t) => t.songId === row.key)!;

  // Returns false when the add was rejected (keeps NewTrackForm open so the title can be changed).
  async function addTrack(draft: NewTrackDraft): Promise<boolean> {
    try {
      const created = await api.tracks.add(id, {
        songId: null,
        title: draft.title,
        isrc: draft.isrc,
        artists: draft.artists.length ? draft.artists : null,
      });
      setTracks((ts) => [...ts, created]);
      refreshPending();
      return true;
    } catch (e) {
      // A duplicate title (unique per artist) opens a prompt: pick the existing song or rename.
      // Matches the server's DuplicateSongTitleMessage (mirrored in api/serverMessages.ts), M25.
      if (e instanceof ApiError && e.errors.some((m) => m.includes(DUPLICATE_SONG_TITLE_MESSAGE))) {
        await promptDuplicate(draft.title);
        return false;
      }
      showToast(errorMessage(e, 'Could not add track.'));
      return false;
    }
  }

  // Look up the clashing catalog song (scoped to this artist) so the prompt can offer to link it.
  async function promptDuplicate(rawTitle: string) {
    const title = rawTitle.trim();
    const wanted = title.toLowerCase();
    let existing: SongListItem | null = null;
    try {
      const matches = await api.songs.list({ artistId: release.mainArtistId, q: title });
      existing = matches.find((s) => !s.isArchived && s.title.trim().toLowerCase() === wanted) ?? null;
    } catch {
      existing = null;
    }
    const found = existing;
    const onRelease = found != null && orderedTracks.some((t) => t.songId === found.id);
    setDupPrompt({ title, existing, onRelease });
  }

  async function addExistingTrack(songId: string) {
    try {
      const created = await api.tracks.add(id, { songId, title: null, isrc: null, artists: null });
      setTracks((ts) => [...ts, created]);
      refreshPending();
    } catch (e) {
      showToast(errorMessage(e, 'Could not add song.'));
    }
  }

  // Optimistic focus toggle: flip locally, revert + toast on failure.
  async function toggleFocus(target: TrackDto) {
    setTracks((ts) => ts.map((t) => (t.songId === target.songId ? { ...t, isFocusTrack: !t.isFocusTrack } : t)));
    try {
      const saved = await api.tracks.toggleFocus(id, target.songId);
      setTracks((ts) => ts.map((t) => (t.songId === saved.songId ? saved : t)));
    } catch (e) {
      setTracks((ts) => ts.map((t) => (t.songId === target.songId ? target : t)));
      showToast(errorMessage(e, 'Could not save — reverted.'));
    }
  }

  async function removeTrack(target: TrackDto) {
    if (
      !(await confirm({
        title: `Remove "${target.title}" from this release?`,
        body: <p>The song stays in the catalog.</p>,
        confirmLabel: 'Remove',
        confirmVariant: 'danger',
      }))
    )
      return;
    const prev = tracks;
    // Drop it and renumber locally to mirror the server's contiguous numbering.
    setTracks((ts) =>
      ts
        .filter((t) => t.songId !== target.songId)
        .sort((a, b) => a.trackNumber - b.trackNumber)
        .map((t, i) => ({ ...t, trackNumber: i + 1 })),
    );
    try {
      await api.tracks.delete(id, target.songId);
      refreshPending();
    } catch (e) {
      setTracks(prev);
      showToast(errorMessage(e, 'Could not remove track.'));
    }
  }

  // Move a track up/down; persist the release's new track order.
  async function moveTrack(target: TrackDto, dir: -1 | 1) {
    const list = [...orderedTracks];
    const i = list.findIndex((t) => t.songId === target.songId);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const renumbered = list.map((t, idx) => ({ ...t, trackNumber: idx + 1 }));
    const prev = tracks;
    setTracks(renumbered);
    try {
      await api.tracks.reorder(id, { orderedSongIds: list.map((t) => t.songId) });
    } catch (e) {
      setTracks(prev);
      showToast(errorMessage(e, 'Could not reorder.'));
    }
  }

  return {
    tracks,
    orderedTracks,
    trackRows,
    track,
    addTrack,
    addExistingTrack,
    toggleFocus,
    removeTrack,
    moveTrack,
    dupPrompt,
    setDupPrompt,
  };
}
