import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, errorMessage } from '@/api';
import { useTemplates, queryKeys } from '@/api/queries';
import type { TemplateTaskDto } from '@/types';
import { Phase, ReleaseType } from '@/types';
import { ErrorBanner, Loading, Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useTaskText } from '@/hooks/useTaskText';
import { useToast } from '@/hooks/useToast';
import { PHASE_ORDER, byPhase } from '@/lib/phase';
import { PhaseSection } from '../releases/components/PhaseSection';
import { TaskEditorModal } from '../releases/components/TaskEditorModal';
import { emptyDraft, type TaskDraft } from '../releases/components/taskDraft';

const TYPE_TABS = [
  { type: ReleaseType.Single, labelKey: 'releaseType.single' },
  { type: ReleaseType.Album, labelKey: 'releaseType.album' },
] as const;

export default function TemplatesPage() {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const taskText = useTaskText();
  const queryClient = useQueryClient();
  const { toast, toastVariant, showToast } = useToast();
  const { data: templates = [], isLoading, error } = useTemplates();

  const [tab, setTab] = useState<ReleaseType>(ReleaseType.Single);
  // Keep a flat task array for the active template so mutations render without a re-fetch.
  const [tasks, setTasks] = useState<TemplateTaskDto[]>([]);
  // The task being edited, or a phase when adding. Null means the editor is closed.
  const [editing, setEditing] = useState<{ task: TemplateTaskDto | null; draft: TaskDraft } | null>(null);

  const active = templates.find((t) => t.type === tab);
  useEffect(() => {
    if (active) setTasks(active.phases.flatMap((p) => p.tasks));
  }, [active]);

  const grouped = byPhase(tasks);

  // Keep the create-release hint's live template count fresh after any edit.
  const invalidateTemplates = () => void queryClient.invalidateQueries({ queryKey: queryKeys.templates });

  const openAdd = (phase: Phase) => setEditing({ task: null, draft: emptyDraft(phase) });

  const openEdit = (task: TemplateTaskDto) =>
    setEditing({
      task,
      draft: {
        titleEn: task.titleEn,
        titleEs: task.titleEs ?? '',
        phase: task.phase,
        minDaysBefore: task.minDaysBefore,
        maxDaysBefore: task.maxDaysBefore,
        notes: '',
      },
    });

  async function saveTask(draft: TaskDraft) {
    if (!active) return;
    const existing = editing?.task ?? null;
    setEditing(null);
    // Blank Spanish is sent as null: "show the English" is one state, not two.
    const payload = {
      titleEn: draft.titleEn,
      titleEs: draft.titleEs || null,
      phase: draft.phase,
      minDaysBefore: draft.minDaysBefore,
      maxDaysBefore: draft.maxDaysBefore,
    };
    try {
      if (existing) {
        const saved = await api.templates.updateTask(existing.id, payload);
        setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
      } else {
        const created = await api.templates.addTask(active.id, payload);
        setTasks((ts) => [...ts, created]);
      }
      invalidateTemplates();
    } catch (e) {
      showToast(errorMessage(e, t(existing ? 'tasks.errors.save' : 'tasks.errors.add')));
    }
  }

  async function removeTask(task: TemplateTaskDto) {
    if (
      !(await confirm({
        title: t('templates.deleteTaskConfirm', { title: taskText(task) }),
        confirmLabel: t('common.delete'),
        confirmVariant: 'danger',
      }))
    )
      return;
    const prev = tasks;
    setTasks((ts) => ts.filter((t) => t.id !== task.id));
    try {
      await api.templates.deleteTask(task.id);
      invalidateTemplates();
    } catch (e) {
      setTasks(prev);
      showToast(errorMessage(e, t('tasks.errors.delete')));
    }
  }

  // Move a task up/down within its phase; persist the phase's new order.
  async function move(task: TemplateTaskDto, dir: -1 | 1) {
    if (!active) return;
    const list = [...(grouped.get(task.phase) ?? [])];
    const i = list.findIndex((t) => t.id === task.id);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const reordered = list.map((t, idx) => ({ ...t, sortOrder: idx }));
    const prev = tasks;
    setTasks((ts) => ts.map((t) => reordered.find((r) => r.id === t.id) ?? t));
    try {
      await api.templates.reorderTasks(active.id, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
      invalidateTemplates();
    } catch (e) {
      setTasks(prev);
      showToast(errorMessage(e, t('tasks.errors.reorder')));
    }
  }

  return (
    <div>
      <div className="mb-4">
        <h1 className="text-2xl font-semibold text-strong">{t('templates.title')}</h1>
        <p className="text-sm text-muted">{t('templates.subtitle')}</p>
      </div>

      <p className="mb-6 rounded-lg border border-warn/30 bg-warn/10 px-4 py-2.5 text-sm text-warnFg">
        {t('templates.futureOnly')}
      </p>

      <div className="mb-6 flex gap-1 rounded-lg border border-edge bg-panel p-1">
        {TYPE_TABS.map((tab_) => (
          <button
            key={tab_.type}
            onClick={() => setTab(tab_.type)}
            className={`flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition ${
              tab === tab_.type ? 'bg-accent text-white' : 'text-muted hover:text-body'
            }`}
          >
            {t(tab_.labelKey)}
          </button>
        ))}
      </div>

      {isLoading ? (
        <Loading />
      ) : error ? (
        <ErrorBanner error={t('templates.loadFailed')} />
      ) : !active ? (
        <p className="text-muted">{t('templates.notFound')}</p>
      ) : (
        <>
          <p className="mb-3 text-xs text-subtle">{t('templates.taskCount', { count: tasks.length })}</p>
          <div className="space-y-3">
            {PHASE_ORDER.map((phase) => (
              <PhaseSection
                key={phase}
                phase={phase}
                tasks={grouped.get(phase) ?? []}
                onAdd={openAdd}
                onEdit={openEdit}
                onDelete={removeTask}
                onMove={move}
              />
            ))}
          </div>
        </>
      )}

      <TaskEditorModal
        open={!!editing}
        mode={editing?.task ? 'edit' : 'add'}
        initial={editing?.draft ?? emptyDraft(Phase.Pre)}
        onSave={saveTask}
        onClose={() => setEditing(null)}
      />

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
