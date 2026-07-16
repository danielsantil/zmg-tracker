import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, PendingAction, ReleaseDetail as ReleaseDetailModel, ReleaseTaskDto, SongListItem, TrackDto } from '@/types';
import { Phase, ReleaseType } from '@/types';
import { Button, Modal, Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { PHASE_ORDER } from '@/lib/phase';
import { todayIso } from '@/lib/format';
import { ReleaseHeader } from './components/ReleaseHeader';
import { NeedsAttention } from './components/NeedsAttention';
import { PhaseSection } from './components/PhaseSection';
import { Tracklist, type NewTrackDraft, type TracklistRow } from './components/Tracklist';
import { SongCard } from './components/SongCard';
import { archiveReleaseConfirm } from './archiveConfirm';
import type { TaskPatch } from './components/TaskRow';

export default function ReleaseDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const goBack = useBackNavigation();
  const confirm = useConfirm();

  const [release, setRelease] = useState<ReleaseDetailModel | null>(null);
  const [tasks, setTasks] = useState<ReleaseTaskDto[]>([]);
  const [tracks, setTracks] = useState<TrackDto[]>([]);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [pendingActions, setPendingActions] = useState<PendingAction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  // Set when adding a new track whose title collides with an existing song for this artist (M19).
  const [dupPrompt, setDupPrompt] = useState<{ title: string; existing: SongListItem | null; onRelease: boolean } | null>(null);
  const { toast, toastVariant, showToast } = useToast();

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const detail = await api.releases.get(id);
      setRelease(detail);
      setTasks(detail.phases.flatMap((p) => p.tasks));
      setTracks(detail.tracks);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to load release.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  const loadPending = useCallback(async () => {
    if (!id) return;
    api.pending.listByRelease(id).then(setPendingActions).catch((e) => {
      console.error('Failed to load pending actions:', e);
    });
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    loadPending();
  }, [loadPending]);

  // The full artist list feeds the feats editor when adding a new track (an album's main artist
  // can't feature on their own song). Best-effort — the add form still works without it.
  useEffect(() => {
    api.artists.list().then(setArtists).catch(() => setArtists([]));
  }, []);

  const done = tasks.filter((t) => t.isDone).length;
  const total = tasks.length;

  const byPhase = useMemo(() => {
    const map = new Map<Phase, ReleaseTaskDto[]>();
    for (const phase of PHASE_ORDER) {
      map.set(
        phase,
        tasks.filter((t) => t.phase === phase).sort((a, b) => a.sortOrder - b.sortOrder),
      );
    }
    return map;
  }, [tasks]);

  // Optimistic toggle: flip locally, revert + toast on failure.
  async function toggle(task: ReleaseTaskDto) {
    const optimistic = { ...task, isDone: !task.isDone };
    setTasks((ts) => ts.map((t) => (t.id === task.id ? optimistic : t)));
    try {
      const saved = await api.tasks.toggle(task.id);
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
      loadPending(); // refresh pending actions after a task toggle
    } catch (e) {
      setTasks((ts) => ts.map((t) => (t.id === task.id ? task : t)));
      showToast(e instanceof ApiError ? e.message : 'Could not save — reverted.');
    }
  }

  async function addTask(phase: Phase, title: string) {
    try {
      const created = await api.tasks.add(id!, { title, phase });
      setTasks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add task.');
    }
  }

  async function updateTask(task: ReleaseTaskDto, patch: TaskPatch) {
    try {
      // Update is a full replace of editable fields, so always send the task's current
      // timeframe/notes unless the patch overrides them (else a rename would wipe them).
      const saved = await api.tasks.update(task.id, {
        title: patch.title ?? task.title,
        phase: patch.phase ?? task.phase,
        notes: patch.notes !== undefined ? patch.notes : task.notes,
        minDaysBefore: patch.minDaysBefore !== undefined ? patch.minDaysBefore : task.minDaysBefore,
        maxDaysBefore: patch.maxDaysBefore !== undefined ? patch.maxDaysBefore : task.maxDaysBefore,
      });
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not save task.');
    }
  }

  async function removeTask(task: ReleaseTaskDto) {
    if (
      !(await confirm({
        title: `Delete task "${task.title}"?`,
        confirmLabel: 'Delete',
        confirmVariant: 'danger',
      }))
    )
      return;
    const prev = tasks;
    setTasks((ts) => ts.filter((t) => t.id !== task.id));
    try {
      await api.tasks.delete(task.id);
    } catch (e) {
      setTasks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not delete task.');
    }
  }

  // Move a task up/down within its phase; persist the phase's new order.
  async function move(task: ReleaseTaskDto, dir: -1 | 1) {
    const list = [...(byPhase.get(task.phase) ?? [])];
    const i = list.findIndex((t) => t.id === task.id);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const reordered = list.map((t, idx) => ({ ...t, sortOrder: idx }));
    const prev = tasks;
    setTasks((ts) => ts.map((t) => reordered.find((r) => r.id === t.id) ?? t));
    try {
      await api.tasks.reorder(id!, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
    } catch (e) {
      setTasks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not reorder.');
    }
  }

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
  const track = (row: TracklistRow) => orderedTracks.find((t) => t.songId === row.key)!;

  // Returns false when the add was rejected (keeps NewTrackForm open so the title can be changed).
  async function addTrack(draft: NewTrackDraft): Promise<boolean> {
    try {
      const created = await api.tracks.add(id!, {
        songId: null,
        title: draft.title,
        isrc: draft.isrc,
        artists: draft.artists.length ? draft.artists : null,
      });
      setTracks((ts) => [...ts, created]);
      loadPending(); // refresh pending actions after tracklist changes
      return true;
    } catch (e) {
      // A duplicate title (unique per artist) opens a prompt: pick the existing song or rename.
      if (e instanceof ApiError && e.errors.some((m) => m.includes('already exists for this artist'))) {
        await promptDuplicate(draft.title);
        return false;
      }
      showToast(e instanceof ApiError ? e.message : 'Could not add track.');
      return false;
    }
  }

  // Look up the clashing catalog song (scoped to this artist) so the prompt can offer to link it.
  async function promptDuplicate(rawTitle: string) {
    const title = rawTitle.trim();
    const wanted = title.toLowerCase();
    let existing: SongListItem | null = null;
    try {
      const matches = await api.songs.list({ artistId: release!.mainArtistId, q: title });
      existing = matches.find((s) => !s.isArchived && s.title.trim().toLowerCase() === wanted) ?? null;
    } catch {
      existing = null;
    }
    const onRelease = !!existing && orderedTracks.some((t) => t.songId === existing!.id);
    setDupPrompt({ title, existing, onRelease });
  }

  async function addExistingTrack(songId: string) {
    try {
      const created = await api.tracks.add(id!, { songId, title: null, isrc: null, artists: null });
      setTracks((ts) => [...ts, created]);
      loadPending(); // refresh pending actions after tracklist changes
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add song.');
    }
  }

  // Optimistic focus toggle: flip locally, revert + toast on failure.
  async function toggleFocus(track: TrackDto) {
    const optimistic = { ...track, isFocusTrack: !track.isFocusTrack };
    setTracks((ts) => ts.map((t) => (t.songId === track.songId ? optimistic : t)));
    try {
      const saved = await api.tracks.toggleFocus(id!, track.songId);
      setTracks((ts) => ts.map((t) => (t.songId === saved.songId ? saved : t)));
    } catch (e) {
      setTracks((ts) => ts.map((t) => (t.songId === track.songId ? track : t)));
      showToast(e instanceof ApiError ? e.message : 'Could not save — reverted.');
    }
  }

  async function removeTrack(track: TrackDto) {
    if (
      !(await confirm({
        title: `Remove "${track.title}" from this release?`,
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
        .filter((t) => t.songId !== track.songId)
        .sort((a, b) => a.trackNumber - b.trackNumber)
        .map((t, i) => ({ ...t, trackNumber: i + 1 })),
    );
    try {
      await api.tracks.delete(id!, track.songId);
      loadPending(); // refresh pending actions after tracklist changes
    } catch (e) {
      setTracks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not remove track.');
    }
  }

  // Move a track up/down; persist the release's new track order.
  async function moveTrack(track: TrackDto, dir: -1 | 1) {
    const list = [...orderedTracks];
    const i = list.findIndex((t) => t.songId === track.songId);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const renumbered = list.map((t, idx) => ({ ...t, trackNumber: idx + 1 }));
    const prev = tracks;
    setTracks(renumbered);
    try {
      await api.tracks.reorder(id!, { orderedSongIds: list.map((t) => t.songId) });
    } catch (e) {
      setTracks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not reorder.');
    }
  }

  async function archive(r: ReleaseDetailModel) {
    if (!(await confirm(await archiveReleaseConfirm(r.id, r.title)))) return;
    try {
      await api.releases.archive(r.id);
      load();
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not archive.');
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;
  if (error) return <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>;
  if (!release) return null;

  // Archived releases are terminal and read-only: no edit, no toggles, no add/menu controls.
  const readOnly = release.isArchived;
  // Archive is only allowed for releases still to come (releaseDate >= today).
  const canArchive = !readOnly && release.releaseDate >= todayIso();

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <button onClick={goBack} className="text-sm text-slate-400 hover:text-slate-200">
          ‹ Releases
        </button>
        {readOnly ? (
          <span className="text-sm text-slate-500">Archived — read only</span>
        ) : (
          <div className="flex items-center gap-2">
            <Button variant="ghost" onClick={() => navigate(`/releases/${release.id}/edit`)}>
              Edit
            </Button>
            {canArchive && (
              <Button variant="archive" onClick={() => archive(release)}>
                Archive
              </Button>
            )}
          </div>
        )}
      </div>

      <ReleaseHeader release={release} done={done} total={total} />

      {pendingActions.length > 0 && <NeedsAttention actions={pendingActions} />}

      {release.notes && (
        <p className="mb-6 whitespace-pre-wrap rounded-lg border border-edge bg-panel/50 px-4 py-3 text-sm text-slate-300">
          {release.notes}
        </p>
      )}

      <div className="mb-6">
        {release.type === ReleaseType.Single ? (
          orderedTracks.length > 0 && (
            <SongCard track={orderedTracks[0]} mainArtistName={release.mainArtistName} />
          )
        ) : (
          <Tracklist
            rows={trackRows}
            readOnly={readOnly}
            mainArtistId={release.mainArtistId}
            excludeIds={orderedTracks.map((t) => t.songId)}
            artists={artists}
            onAddNew={addTrack}
            onAddExisting={(song) => addExistingTrack(song.id)}
            onToggleFocus={(row) => toggleFocus(track(row))}
            onRemove={(row) => removeTrack(track(row))}
            onMove={(row, dir) => moveTrack(track(row), dir)}
          />
        )}
      </div>

      <div className="space-y-3">
        {PHASE_ORDER.map((phase) => (
          <PhaseSection
            key={phase}
            phase={phase}
            tasks={byPhase.get(phase) ?? []}
            readOnly={readOnly}
            onToggle={toggle}
            onAdd={(title) => addTask(phase, title)}
            onUpdate={updateTask}
            onDelete={removeTask}
            onMove={move}
          />
        ))}
      </div>

      <Modal open={!!dupPrompt} onClose={() => setDupPrompt(null)} title="Song already exists">
        {dupPrompt && (
          <div className="space-y-4">
            <p className="text-sm text-slate-300">
              A song titled <span className="font-medium text-slate-100">“{dupPrompt.title}”</span> already
              exists for {release.mainArtistName}.{' '}
              {dupPrompt.onRelease
                ? "It's already on this release — change the name to add a different song."
                : 'Add the existing song to this release, or change the name.'}
            </p>
            <div className="flex gap-2">
              {dupPrompt.existing && !dupPrompt.onRelease && (
                <Button
                  onClick={async () => {
                    const song = dupPrompt.existing!;
                    setDupPrompt(null);
                    await addExistingTrack(song.id);
                  }}
                >
                  Add existing song
                </Button>
              )}
              <Button variant="ghost" onClick={() => setDupPrompt(null)}>
                Change the name
              </Button>
            </div>
          </div>
        )}
      </Modal>

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
