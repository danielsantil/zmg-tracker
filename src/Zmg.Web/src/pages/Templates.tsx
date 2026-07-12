import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api, ApiError } from '../api';
import type { Template, TemplateTaskDto } from '../types';
import { Phase, ReleaseType } from '../types';
import { Button, MenuItem, RowMenu, inputClass, phaseLabels } from '../ui';

const PHASE_ORDER: Phase[] = [Phase.Pre, Phase.Release, Phase.Post];
const TYPE_TABS: { type: ReleaseType; label: string }[] = [
  { type: ReleaseType.Single, label: 'Single' },
  { type: ReleaseType.Album, label: 'Album' },
];

export default function Templates() {
  const [templates, setTemplates] = useState<Template[]>([]);
  const [tab, setTab] = useState<ReleaseType>(ReleaseType.Single);
  const [tasks, setTasks] = useState<TemplateTaskDto[]>([]);
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
    setLoading(true);
    setError(null);
    try {
      const all = await api.listTemplates();
      setTemplates(all);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to load templates.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => () => {
    if (toastTimer.current) clearTimeout(toastTimer.current);
  }, []);

  // Keep a flat task array for the active template so mutations don't need a re-fetch.
  const active = templates.find((t) => t.type === tab);
  useEffect(() => {
    if (active) setTasks(active.phases.flatMap((p) => p.tasks));
  }, [active]);

  const total = tasks.length;

  const byPhase = useMemo(() => {
    const map = new Map<Phase, TemplateTaskDto[]>();
    for (const phase of PHASE_ORDER) {
      map.set(
        phase,
        tasks.filter((t) => t.phase === phase).sort((a, b) => a.sortOrder - b.sortOrder),
      );
    }
    return map;
  }, [tasks]);

  async function addTask(phase: Phase, title: string) {
    if (!active) return;
    try {
      const created = await api.addTemplateTask(active.id, { title, phase });
      setTasks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add task.');
    }
  }

  async function updateTask(task: TemplateTaskDto, patch: Partial<Pick<TemplateTaskDto, 'title' | 'phase'>>) {
    try {
      const saved = await api.updateTemplateTask(task.id, {
        title: patch.title ?? task.title,
        phase: patch.phase ?? task.phase,
      });
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not save task.');
    }
  }

  async function removeTask(task: TemplateTaskDto) {
    if (!confirm(`Delete template task "${task.title}"?`)) return;
    const prev = tasks;
    setTasks((ts) => ts.filter((t) => t.id !== task.id));
    try {
      await api.deleteTemplateTask(task.id);
    } catch (e) {
      setTasks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not delete task.');
    }
  }

  // Move a task up/down within its phase; persist the phase's new order.
  async function move(task: TemplateTaskDto, dir: -1 | 1) {
    if (!active) return;
    const list = [...(byPhase.get(task.phase) ?? [])];
    const i = list.findIndex((t) => t.id === task.id);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const reordered = list.map((t, idx) => ({ ...t, sortOrder: idx }));
    const prev = tasks;
    setTasks((ts) => ts.map((t) => reordered.find((r) => r.id === t.id) ?? t));
    try {
      await api.reorderTemplateTasks(active.id, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
    } catch (e) {
      setTasks(prev);
      showToast(e instanceof ApiError ? e.message : 'Could not reorder.');
    }
  }

  return (
    <div>
      <div className="mb-4">
        <h1 className="text-2xl font-semibold text-white">Templates</h1>
        <p className="text-sm text-slate-400">
          The default checklist copied onto each new release.
        </p>
      </div>

      <p className="mb-4 rounded-lg border border-amber-500/30 bg-amber-500/10 px-4 py-2.5 text-sm text-amber-200">
        Changes apply to future releases only — existing releases keep their own copy.
      </p>

      <div className="mb-6 flex gap-1 rounded-lg border border-edge bg-panel p-1">
        {TYPE_TABS.map((t) => (
          <button
            key={t.type}
            onClick={() => setTab(t.type)}
            className={`flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition ${
              tab === t.type ? 'bg-accent text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : error ? (
        <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>
      ) : !active ? (
        <p className="text-slate-400">No template found.</p>
      ) : (
        <>
          <p className="mb-3 text-xs text-slate-500">{total} tasks in this template</p>
          <div className="space-y-3">
            {PHASE_ORDER.map((phase) => (
              <PhaseSection
                key={phase}
                phase={phase}
                tasks={byPhase.get(phase) ?? []}
                onAdd={(title) => addTask(phase, title)}
                onUpdate={updateTask}
                onDelete={removeTask}
                onMove={move}
              />
            ))}
          </div>
        </>
      )}

      {toast && (
        <div className="fixed bottom-4 left-1/2 z-20 -translate-x-1/2 rounded-lg bg-red-500/90 px-4 py-2 text-sm text-white shadow-lg">
          {toast}
        </div>
      )}
    </div>
  );
}

function PhaseSection({
  phase,
  tasks,
  onAdd,
  onUpdate,
  onDelete,
  onMove,
}: {
  phase: Phase;
  tasks: TemplateTaskDto[];
  onAdd: (title: string) => void;
  onUpdate: (t: TemplateTaskDto, patch: Partial<Pick<TemplateTaskDto, 'title' | 'phase'>>) => void;
  onDelete: (t: TemplateTaskDto) => void;
  onMove: (t: TemplateTaskDto, dir: -1 | 1) => void;
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
      <div className="flex items-center justify-between px-4 py-3">
        <span className="flex items-center gap-2 font-semibold text-white">
          {phaseLabels[phase].toUpperCase()}
          <span className="text-sm font-normal text-slate-400">({tasks.length})</span>
        </span>
      </div>

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
    </section>
  );
}

function TaskRow({
  task,
  isFirst,
  isLast,
  onUpdate,
  onDelete,
  onMove,
}: {
  task: TemplateTaskDto;
  isFirst: boolean;
  isLast: boolean;
  onUpdate: (t: TemplateTaskDto, patch: Partial<Pick<TemplateTaskDto, 'title' | 'phase'>>) => void;
  onDelete: (t: TemplateTaskDto) => void;
  onMove: (t: TemplateTaskDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState('');

  function startEdit() {
    setDraft(task.title);
    setEditing(true);
  }

  function saveEdit() {
    const title = draft.trim();
    if (title && title !== task.title) onUpdate(task, { title });
    setEditing(false);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
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
            {task.title}
          </button>
        )}

        <RowMenu>
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit(); }}>Rename</MenuItem>
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
    </li>
  );
}

function MovePhaseItems({
  task,
  onUpdate,
  close,
}: {
  task: TemplateTaskDto;
  onUpdate: (t: TemplateTaskDto, patch: Partial<Pick<TemplateTaskDto, 'phase'>>) => void;
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
