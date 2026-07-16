import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, ApiError } from '@/api';
import type { Template, TemplateTaskDto } from '@/types';
import { Phase, ReleaseType } from '@/types';
import { Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';
import { PHASE_ORDER } from '@/lib/phase';
import { TemplatePhaseSection } from './components/TemplatePhaseSection';
import type { TemplatePatch } from './components/TemplateTaskRow';

const TYPE_TABS: { type: ReleaseType; label: string }[] = [
  { type: ReleaseType.Single, label: 'Single' },
  { type: ReleaseType.Album, label: 'Album' },
];

export default function TemplatesPage() {
  const confirm = useConfirm();
  const [templates, setTemplates] = useState<Template[]>([]);
  const [tab, setTab] = useState<ReleaseType>(ReleaseType.Single);
  const [tasks, setTasks] = useState<TemplateTaskDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toast, showToast } = useToast();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const all = await api.templates.list();
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
      const created = await api.templates.addTask(active.id, { title, phase });
      setTasks((ts) => [...ts, created]);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not add task.');
    }
  }

  async function updateTask(task: TemplateTaskDto, patch: TemplatePatch) {
    try {
      // Full replace of editable fields — carry the current timeframe unless the patch overrides it.
      const saved = await api.templates.updateTask(task.id, {
        title: patch.title ?? task.title,
        phase: patch.phase ?? task.phase,
        minDaysBefore: patch.minDaysBefore !== undefined ? patch.minDaysBefore : task.minDaysBefore,
        maxDaysBefore: patch.maxDaysBefore !== undefined ? patch.maxDaysBefore : task.maxDaysBefore,
      });
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Could not save task.');
    }
  }

  async function removeTask(task: TemplateTaskDto) {
    if (
      !(await confirm({
        title: `Delete template task "${task.title}"?`,
        confirmLabel: 'Delete',
        confirmVariant: 'danger',
      }))
    )
      return;
    const prev = tasks;
    setTasks((ts) => ts.filter((t) => t.id !== task.id));
    try {
      await api.templates.deleteTask(task.id);
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
      await api.templates.reorderTasks(active.id, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
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
              <TemplatePhaseSection
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

      <Toast message={toast} />
    </div>
  );
}
