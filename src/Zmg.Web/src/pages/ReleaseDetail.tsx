import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { PendingAction, ReleaseDetail as ReleaseDetailModel, ReleaseTaskDto, TrackDto } from '@/types';
import { PendingKind, Phase, ReleaseType } from '@/types';
import {
  Button,
  IdentifierWarning,
  MenuItem,
  ProgressBar,
  RowMenu,
  StatusBadge,
  TypeBadge,
  inputClass,
} from '@/components';
import { daysToRelease, formatTimeframe } from '@/lib/format';
import { phaseLabels } from '@/lib/phase';

const PHASE_ORDER: Phase[] = [Phase.Pre, Phase.Release, Phase.Post];

type TaskPatch = Partial<Pick<ReleaseTaskDto, 'title' | 'phase' | 'notes' | 'minDaysBefore' | 'maxDaysBefore'>>;

export default function ReleaseDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  const [release, setRelease] = useState<ReleaseDetailModel | null>(null);
  const [tasks, setTasks] = useState<ReleaseTaskDto[]>([]);
  const [tracks, setTracks] = useState<TrackDto[]>([]);
  const [pendingActions, setPendingActions] = useState<PendingAction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const showToast = useCallback((msg: string) => {
    setToast(msg);
    if (toastTimer.current) clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => setToast(null), 3000);
  }, []);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const detail = await api.getRelease(id);
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
    api.listPendingByRelease(id).then(setPendingActions).catch((e) => {
      console.error('Failed to load pending actions:', e);
    });
  }, [id]);

  const handleBack = () => {
    if (location.key === 'default') {
      navigate('/');
    } else {
      navigate(-1);
    }
  }

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    loadPending();
  }, [loadPending]);

  useEffect(() => () => {
    if (toastTimer.current) clearTimeout(toastTimer.current);
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
      const saved = await api.toggleTask(task.id);
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
      loadPending(); // refresh pending actions after a task toggle
    } catch (e) {
      setTasks((ts) => ts.map((t) => (t.id === task.id ? task : t)));
      showToast(e instanceof ApiError ? e.message : 'Could not save — reverted.');
    }
  }

  async function addTask(phase: Phase, title: string) {
    try {
      const created = await api.addTask(id!, { title, phase });
      setTasks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add task.');
    }
  }

  async function updateTask(task: ReleaseTaskDto, patch: TaskPatch) {
    try {
      // Update is a full replace of editable fields, so always send the task's current
      // timeframe/notes unless the patch overrides them (else a rename would wipe them).
      const saved = await api.updateTask(task.id, {
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
      await api.deleteTask(task.id);
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
      await api.reorderTasks(id!, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
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
      const created = await api.addTrack(id!, { title });
      setTracks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add track.');
    }
  }

  async function renameTrack(track: TrackDto, title: string) {
    try {
      const saved = await api.updateTrack(track.id, { title, isFocusTrack: track.isFocusTrack });
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
      const saved = await api.toggleTrackFocus(track.id);
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
      await api.deleteTrack(track.id);
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
      await api.reorderTracks(id!, { orderedTrackIds: list.map((t) => t.id) });
    } catch (e) {
      setTracks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not reorder.');
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;
  if (error) return <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>;
  if (!release) return null;

  const days = daysToRelease(release.releaseDate);
  const countdown =
    release.status === 'Upcoming' && days >= 0
      ? days === 0
        ? 'Releases today'
        : `${days} days to release`
      : null;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <button onClick={handleBack} className="text-sm text-slate-400 hover:text-slate-200">
          ‹ Releases
        </button>
        <Button variant="ghost" onClick={() => navigate(`/releases/${release.id}/edit`)}>
          Edit
        </Button>
      </div>

      <div className="mb-6 flex gap-4 rounded-xl border border-edge bg-panel p-4">
        <div className="hidden h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-edge sm:block">
          {release.coverUrl ? (
            <img src={release.coverUrl} alt="" className="h-full w-full object-cover" />
          ) : (
            <div className="grid h-full place-items-center text-3xl font-semibold text-slate-600">
              {release.title.slice(0, 1).toUpperCase()}
            </div>
          )}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <h1 className="text-xl font-semibold text-white">
              {release.title} <span className="text-slate-400">— {release.mainArtistName}</span>
            </h1>
            <div className="flex items-center gap-1.5">
              {release.needsIdentifierWarning && <IdentifierWarning upc={release.upc} isrc={release.isrc} />}
              <StatusBadge status={release.status} />
            </div>
          </div>
          {release.featuredArtists.length > 0 && (
            <p className="mt-0.5 text-sm text-slate-400">
              feat. {release.featuredArtists.map((f) => f.name).join(', ')}
            </p>
          )}
          <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-slate-400">
            <TypeBadge type={release.type} />
            <span className="whitespace-nowrap">{release.releaseDate}</span>
            <span className="whitespace-nowrap">· {done}/{total} done</span>
            {countdown && <span className="whitespace-nowrap text-accent">· {countdown}</span>}
          </div>
          <div className="mt-3">
            <ProgressBar done={done} total={total} />
          </div>
        </div>
      </div>

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
            onToggle={toggle}
            onAdd={(title) => addTask(phase, title)}
            onUpdate={updateTask}
            onDelete={removeTask}
            onMove={move}
          />
        ))}
      </div>

      {toast && (
        <div className="fixed bottom-4 left-1/2 z-20 -translate-x-1/2 rounded-lg bg-red-500/90 px-4 py-2 text-sm text-white shadow-lg">
          {toast}
        </div>
      )}
    </div>
  );
}

/**
 * "Needs attention" block (M10) — this release's pending actions, computed server-side from the
 * loaded detail payload. Task-due items show days-to-release; data items (missing ids) don't.
 */
function NeedsAttention({ actions }: { actions: PendingAction[] }) {
  return (
    <div className="mb-6 overflow-hidden rounded-xl border border-amber-500/25 bg-amber-500/[0.06]">
      <div className="border-b border-amber-500/20 px-4 py-2.5 text-sm font-semibold text-amber-200">
        Needs attention
      </div>
      <ul className="px-4 py-2">
        {actions.map((a, i) => (
          <li key={`${a.taskId ?? a.kind}-${i}`} className="flex items-baseline gap-2 py-1 text-sm text-slate-200">
            <span className="text-amber-300">•</span>
            <span>{a.label}</span>
            {a.kind === PendingKind.TaskDue && a.daysToRelease != null && (
              <span className="text-xs text-accent">
                — {a.daysToRelease === 0 ? 'releases today' : `${a.daysToRelease} days to release`}
              </span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}

function PhaseSection({
  phase,
  tasks,
  onToggle,
  onAdd,
  onUpdate,
  onDelete,
  onMove,
}: {
  phase: Phase;
  tasks: ReleaseTaskDto[];
  onToggle: (t: ReleaseTaskDto) => void;
  onAdd: (title: string) => void;
  onUpdate: (t: ReleaseTaskDto, patch: TaskPatch) => void;
  onDelete: (t: ReleaseTaskDto) => void;
  onMove: (t: ReleaseTaskDto, dir: -1 | 1) => void;
}) {
  const done = tasks.filter((t) => t.isDone).length;
  const total = tasks.length;
  const allDone = total > 0 && done === total;
  // Fully-done phases collapse by default; tap the header to expand.
  const [open, setOpen] = useState(!allDone);
  const [adding, setAdding] = useState(false);
  const [newTitle, setNewTitle] = useState('');

  function submitAdd() {
    const title = newTitle.trim();
    if (!title) return;
    onAdd(title);
    setNewTitle('');
    setAdding(false);
  }

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <button
        className="flex w-full items-center justify-between px-4 py-3 text-left"
        onClick={() => setOpen((o) => !o)}
      >
        <span className="flex items-center gap-2 font-semibold text-white">
          <span className="text-slate-500">{open ? '▾' : '▸'}</span>
          {phaseLabels[phase].toUpperCase()}
          <span className="text-sm font-normal text-slate-400">
            ({done}/{total})
          </span>
          {allDone && <span className="text-emerald-400">✓</span>}
        </span>
      </button>

      {open && (
        <div className="border-t border-edge">
          {tasks.length === 0 ? (
            <p className="px-4 py-3 text-sm text-slate-500">No tasks in this phase.</p>
          ) : (
            <ul>
              {tasks.map((t, i) => (
                <TaskRow
                  key={t.id}
                  task={t}
                  isFirst={i === 0}
                  isLast={i === tasks.length - 1}
                  onToggle={onToggle}
                  onUpdate={onUpdate}
                  onDelete={onDelete}
                  onMove={onMove}
                />
              ))}
            </ul>
          )}

          <div className="border-t border-edge px-3 py-2">
            {adding ? (
              <div className="flex gap-2">
                <input
                  autoFocus
                  className={inputClass}
                  placeholder="New task title"
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') submitAdd();
                    if (e.key === 'Escape') {
                      setAdding(false);
                      setNewTitle('');
                    }
                  }}
                />
                <Button onClick={submitAdd}>Add</Button>
                <Button
                  variant="ghost"
                  onClick={() => {
                    setAdding(false);
                    setNewTitle('');
                  }}
                >
                  Cancel
                </Button>
              </div>
            ) : (
              <button
                className="rounded-lg px-2 py-1.5 text-sm text-slate-400 hover:text-accent"
                onClick={() => setAdding(true)}
              >
                + Add task
              </button>
            )}
          </div>
        </div>
      )}
    </section>
  );
}

function TaskRow({
  task,
  isFirst,
  isLast,
  onToggle,
  onUpdate,
  onDelete,
  onMove,
}: {
  task: ReleaseTaskDto;
  isFirst: boolean;
  isLast: boolean;
  onToggle: (t: ReleaseTaskDto) => void;
  onUpdate: (t: ReleaseTaskDto, patch: TaskPatch) => void;
  onDelete: (t: ReleaseTaskDto) => void;
  onMove: (t: ReleaseTaskDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState<'title' | 'notes' | 'timeframe' | null>(null);
  const [draft, setDraft] = useState('');

  const timeframe = formatTimeframe(task.minDaysBefore, task.maxDaysBefore);

  function startEdit(field: 'title' | 'notes') {
    setDraft(field === 'title' ? task.title : task.notes ?? '');
    setEditing(field);
  }

  function saveEdit() {
    if (editing === 'title') {
      const title = draft.trim();
      if (title && title !== task.title) onUpdate(task, { title });
    } else if (editing === 'notes') {
      const notes = draft.trim() || null;
      if (notes !== task.notes) onUpdate(task, { notes });
    }
    setEditing(null);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        <button
          role="checkbox"
          aria-checked={task.isDone}
          aria-label={task.title}
          onClick={() => onToggle(task)}
          className={`grid h-6 w-6 shrink-0 place-items-center rounded-md border transition ${
            task.isDone
              ? 'border-accent bg-accent text-white'
              : 'border-edge bg-panel hover:border-accent'
          }`}
        >
          {task.isDone && '✓'}
        </button>

        {editing === 'title' ? (
          <input
            autoFocus
            className={inputClass}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Enter') saveEdit();
              if (e.key === 'Escape') setEditing(null);
            }}
          />
        ) : (
          <button
            className={`flex-1 text-left text-sm ${task.isDone ? 'text-slate-500 line-through' : 'text-slate-100'}`}
            onClick={() => onToggle(task)}
          >
            {task.title}
            {timeframe && (
              <span className="ml-2 whitespace-nowrap text-xs text-accent/80">· {timeframe}</span>
            )}
            {task.notes && (
              <span className="ml-1.5 text-xs text-slate-500" title="Has notes" aria-label="Has notes">
                ✎
              </span>
            )}
          </button>
        )}

        <RowMenu>
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit('title'); }}>Rename</MenuItem>
              <MenuItem onClick={() => { close(); startEdit('notes'); }}>
                {task.notes ? 'Edit notes' : 'Add notes'}
              </MenuItem>
              {task.phase === Phase.Pre && (
                <MenuItem onClick={() => { close(); setEditing('timeframe'); }}>
                  {timeframe ? 'Edit timeframe' : 'Set timeframe'}
                </MenuItem>
              )}
              <MovePhaseItems task={task} onUpdate={onUpdate} close={close} />
              {!isFirst && (
                <MenuItem onClick={() => { close(); onMove(task, -1); }}>Move up</MenuItem>
              )}
              {!isLast && (
                <MenuItem onClick={() => { close(); onMove(task, 1); }}>Move down</MenuItem>
              )}
              <MenuItem danger onClick={() => { close(); onDelete(task); }}>
                Delete
              </MenuItem>
            </>
          )}
        </RowMenu>
      </div>

      {editing === 'notes' && (
        <div className="px-4 pb-3 pl-12">
          <textarea
            autoFocus
            rows={2}
            className={inputClass}
            placeholder="Notes"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Escape') setEditing(null);
            }}
          />
        </div>
      )}
      {editing === 'timeframe' && (
        <TimeframeEditor
          task={task}
          onSave={(min, max) => { onUpdate(task, { minDaysBefore: min, maxDaysBefore: max }); setEditing(null); }}
          onCancel={() => setEditing(null)}
        />
      )}
      {editing !== 'notes' && task.notes && (
        <p className="px-4 pb-2.5 pl-12 text-xs text-slate-400">{task.notes}</p>
      )}
    </li>
  );
}

/**
 * Inline "days before release" editor for a Pre task (v1.1 M8). Min is display-only in the
 * hint; the max drives pending calculations. Leaving both blank clears the timeframe.
 */
function TimeframeEditor({
  task,
  onSave,
  onCancel,
}: {
  task: ReleaseTaskDto;
  onSave: (min: number | null, max: number | null) => void;
  onCancel: () => void;
}) {
  const [min, setMin] = useState(task.minDaysBefore?.toString() ?? '');
  const [max, setMax] = useState(task.maxDaysBefore?.toString() ?? '');

  function parse(v: string): number | null {
    const n = parseInt(v, 10);
    return Number.isFinite(n) && n >= 0 ? n : null;
  }

  return (
    <div className="flex flex-wrap items-center gap-2 px-4 pb-3 pl-12 text-sm text-slate-300">
      <span className="text-xs text-slate-500">Days before release:</span>
      <input
        autoFocus
        type="number"
        min={0}
        className={`${inputClass} w-20`}
        placeholder="min"
        value={min}
        onChange={(e) => setMin(e.target.value)}
      />
      <span className="text-slate-500">–</span>
      <input
        type="number"
        min={0}
        className={`${inputClass} w-20`}
        placeholder="max"
        value={max}
        onChange={(e) => setMax(e.target.value)}
      />
      <Button onClick={() => onSave(parse(min), parse(max))}>Save</Button>
      <Button variant="ghost" onClick={() => onSave(null, null)}>Clear</Button>
      <Button variant="ghost" onClick={onCancel}>Cancel</Button>
    </div>
  );
}

function TrackList({
  tracks,
  onAdd,
  onRename,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  tracks: TrackDto[];
  onAdd: (title: string) => void;
  onRename: (t: TrackDto, title: string) => void;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  const [adding, setAdding] = useState(false);
  const [newTitle, setNewTitle] = useState('');

  function submitAdd() {
    const title = newTitle.trim();
    if (!title) return;
    onAdd(title);
    setNewTitle('');
    setAdding(false);
  }

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="px-4 py-3 font-semibold text-white">
        Tracklist <span className="text-sm font-normal text-slate-400">({tracks.length})</span>
      </div>

      <div className="border-t border-edge">
        {tracks.length === 0 ? (
          <p className="px-4 py-3 text-sm text-slate-500">No tracks yet.</p>
        ) : (
          <ul>
            {tracks.map((t, i) => (
              <TrackRow
                key={t.id}
                track={t}
                isFirst={i === 0}
                isLast={i === tracks.length - 1}
                onRename={onRename}
                onToggleFocus={onToggleFocus}
                onDelete={onDelete}
                onMove={onMove}
              />
            ))}
          </ul>
        )}

        <div className="border-t border-edge px-3 py-2">
          {adding ? (
            <div className="flex gap-2">
              <input
                autoFocus
                className={inputClass}
                placeholder="New track title"
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') submitAdd();
                  if (e.key === 'Escape') {
                    setAdding(false);
                    setNewTitle('');
                  }
                }}
              />
              <Button onClick={submitAdd}>Add</Button>
              <Button
                variant="ghost"
                onClick={() => {
                  setAdding(false);
                  setNewTitle('');
                }}
              >
                Cancel
              </Button>
            </div>
          ) : (
            <button
              className="rounded-lg px-2 py-1.5 text-sm text-slate-400 hover:text-accent"
              onClick={() => setAdding(true)}
            >
              + Add track
            </button>
          )}
        </div>
      </div>
    </section>
  );
}

function TrackRow({
  track,
  isFirst,
  isLast,
  onRename,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  track: TrackDto;
  isFirst: boolean;
  isLast: boolean;
  onRename: (t: TrackDto, title: string) => void;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState('');

  function startEdit() {
    setDraft(track.title);
    setEditing(true);
  }

  function saveEdit() {
    const title = draft.trim();
    if (title && title !== track.title) onRename(track, title);
    setEditing(false);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        <span className="w-6 shrink-0 text-right text-sm tabular-nums text-slate-500">
          {track.trackNumber}
        </span>

        <button
          aria-label={track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
          aria-pressed={track.isFocusTrack}
          onClick={() => onToggleFocus(track)}
          className={`shrink-0 text-lg leading-none transition ${
            track.isFocusTrack ? 'text-amber-400' : 'text-slate-600 hover:text-slate-400'
          }`}
        >
          {track.isFocusTrack ? '★' : '☆'}
        </button>

        {editing ? (
          <input
            autoFocus
            className={inputClass}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Enter') saveEdit();
              if (e.key === 'Escape') setEditing(false);
            }}
          />
        ) : (
          <button className="flex-1 text-left text-sm text-slate-100" onClick={startEdit}>
            {track.title}
            {track.isFocusTrack && (
              <span className="ml-2 text-xs text-amber-400/80">focus</span>
            )}
          </button>
        )}

        <RowMenu label="Track actions">
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit(); }}>Rename</MenuItem>
              <MenuItem onClick={() => { close(); onToggleFocus(track); }}>
                {track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
              </MenuItem>
              {!isFirst && (
                <MenuItem onClick={() => { close(); onMove(track, -1); }}>Move up</MenuItem>
              )}
              {!isLast && (
                <MenuItem onClick={() => { close(); onMove(track, 1); }}>Move down</MenuItem>
              )}
              <MenuItem danger onClick={() => { close(); onDelete(track); }}>
                Delete
              </MenuItem>
            </>
          )}
        </RowMenu>
      </div>
    </li>
  );
}

function MovePhaseItems({
  task,
  onUpdate,
  close,
}: {
  task: ReleaseTaskDto;
  onUpdate: (t: ReleaseTaskDto, patch: Partial<Pick<ReleaseTaskDto, 'phase'>>) => void;
  close: () => void;
}) {
  const targets = PHASE_ORDER.filter((p) => p !== task.phase);
  return (
    <>
      {targets.map((p) => (
        <MenuItem
          key={p}
          onClick={() => {
            onUpdate(task, { phase: p });
            close();
          }}
        >
          Move to {phaseLabels[p]}
        </MenuItem>
      ))}
    </>
  );
}
