import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { PendingAction, ReleaseDetail as ReleaseDetailModel, ReleaseTaskDto, TrackDto } from '@/types';
import { Phase, ReleaseType } from '@/types';
import { Button, Toast } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { PHASE_ORDER } from '@/lib/phase';
import { ReleaseHeader } from './components/ReleaseHeader';
import { NeedsAttention } from './components/NeedsAttention';
import { PhaseSection } from './components/PhaseSection';
import { TrackList } from './components/TrackList';
import type { TaskPatch } from './components/TaskRow';

export default function ReleaseDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const goBack = useBackNavigation();

  const [release, setRelease] = useState<ReleaseDetailModel | null>(null);
  const [tasks, setTasks] = useState<ReleaseTaskDto[]>([]);
  const [tracks, setTracks] = useState<TrackDto[]>([]);
  const [pendingActions, setPendingActions] = useState<PendingAction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toast, showToast } = useToast();

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
    if (!confirm(`Delete task "${task.title}"?`)) return;
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

  async function addTrack(title: string) {
    try {
      const created = await api.tracks.add(id!, { title });
      setTracks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add track.');
    }
  }

  async function renameTrack(track: TrackDto, title: string) {
    try {
      const saved = await api.tracks.update(track.id, { title, isFocusTrack: track.isFocusTrack });
      setTracks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not save track.');
    }
  }

  // Optimistic focus toggle: flip locally, revert + toast on failure.
  async function toggleFocus(track: TrackDto) {
    const optimistic = { ...track, isFocusTrack: !track.isFocusTrack };
    setTracks((ts) => ts.map((t) => (t.id === track.id ? optimistic : t)));
    try {
      const saved = await api.tracks.toggleFocus(track.id);
      setTracks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
    } catch (e) {
      setTracks((ts) => ts.map((t) => (t.id === track.id ? track : t)));
      showToast(e instanceof ApiError ? e.message : 'Could not save — reverted.');
    }
  }

  async function removeTrack(track: TrackDto) {
    if (!confirm(`Delete track "${track.title}"?`)) return;
    const prev = tracks;
    // Drop it and renumber locally to mirror the server's contiguous numbering.
    setTracks((ts) =>
      ts
        .filter((t) => t.id !== track.id)
        .sort((a, b) => a.trackNumber - b.trackNumber)
        .map((t, i) => ({ ...t, trackNumber: i + 1 })),
    );
    try {
      await api.tracks.delete(track.id);
    } catch (e) {
      setTracks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not delete track.');
    }
  }

  // Move a track up/down; persist the release's new track order.
  async function moveTrack(track: TrackDto, dir: -1 | 1) {
    const list = [...orderedTracks];
    const i = list.findIndex((t) => t.id === track.id);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const renumbered = list.map((t, idx) => ({ ...t, trackNumber: idx + 1 }));
    const prev = tracks;
    setTracks(renumbered);
    try {
      await api.tracks.reorder(id!, { orderedTrackIds: list.map((t) => t.id) });
    } catch (e) {
      setTracks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not reorder.');
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;
  if (error) return <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>;
  if (!release) return null;

  // Archived releases are terminal and read-only: no edit, no toggles, no add/menu controls.
  const readOnly = release.isArchived;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <button onClick={goBack} className="text-sm text-slate-400 hover:text-slate-200">
          ‹ Releases
        </button>
        {readOnly ? (
          <span className="text-sm text-slate-500">Archived — read only</span>
        ) : (
          <Button variant="ghost" onClick={() => navigate(`/releases/${release.id}/edit`)}>
            Edit
          </Button>
        )}
      </div>

      <ReleaseHeader release={release} done={done} total={total} />

      {pendingActions.length > 0 && <NeedsAttention actions={pendingActions} />}

      {release.notes && (
        <p className="mb-6 whitespace-pre-wrap rounded-lg border border-edge bg-panel/50 px-4 py-3 text-sm text-slate-300">
          {release.notes}
        </p>
      )}

      {release.type === ReleaseType.Album && (
        <div className="mb-6">
          <TrackList
            tracks={orderedTracks}
            readOnly={readOnly}
            onAdd={addTrack}
            onRename={renameTrack}
            onToggleFocus={toggleFocus}
            onDelete={removeTrack}
            onMove={moveTrack}
          />
        </div>
      )}

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

      <Toast message={toast} />
    </div>
  );
}
