import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, errorMessage } from '@/api';
import { queryKeys } from '@/api/queries';
import type { ReleaseTaskDto } from '@/types';
import { byPhase } from '@/lib/phase';
import { useConfirm } from '@/hooks/useConfirm';
import { taskText } from '@/lib/taskText';
import { useLanguage } from '@/i18n/useLanguage';
import type { TaskDraft } from '../components/taskDraft';

/**
 * The task half of the release detail (M24.7): a flat, optimistically-mutated task array seeded from
 * the release query, plus toggle/add/update/remove/move. The flat array + client-side regrouping is
 * the documented "no re-fetch" pattern; each mutation returns the single changed DTO and patches
 * locally, reverting + toasting on failure. Task changes can flip pending actions, so those queries
 * are invalidated on success.
 */
export function useReleaseTasks(
  id: string,
  initial: ReleaseTaskDto[],
  showToast: (msg: string) => void,
) {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const [tasks, setTasks] = useState<ReleaseTaskDto[]>(initial);

  // Reseed when the release query yields new data (initial load, refetch on focus/return).
  useEffect(() => setTasks(initial), [initial]);

  // A toggle changes both pending actions and the list's done-count, so mark both stale.
  const refreshPending = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.pending });
    void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
    void queryClient.invalidateQueries({ queryKey: queryKeys.release(id) });
  };

  const grouped = byPhase(tasks);

  // Optimistic toggle: flip locally, revert + toast on failure.
  async function toggle(task: ReleaseTaskDto) {
    setTasks((ts) => ts.map((t) => (t.id === task.id ? { ...t, isDone: !t.isDone } : t)));
    try {
      const saved = await api.tasks.toggle(task.id);
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
      refreshPending();
    } catch (e) {
      setTasks((ts) => ts.map((t) => (t.id === task.id ? task : t)));
      showToast(errorMessage(e, t('tasks.errors.saveReverted')));
    }
  }

  // Add and edit share one payload shape, because they share one editor (v2.9): the modal always
  // hands back every editable field, so there is no patch to merge and no field that can be dropped
  // by forgetting to carry it. Blank Spanish is sent as null — "show the English" is one state.
  function payload(draft: TaskDraft) {
    return {
      titleEn: draft.titleEn,
      titleEs: draft.titleEs || null,
      phase: draft.phase,
      notes: draft.notes || null,
      minDaysBefore: draft.minDaysBefore,
      maxDaysBefore: draft.maxDaysBefore,
    };
  }

  async function addTask(draft: TaskDraft) {
    try {
      const created = await api.tasks.add(id, payload(draft));
      setTasks((ts) => [...ts, created]);
      refreshPending();
    } catch (e) {
      showToast(errorMessage(e, t('tasks.errors.add')));
    }
  }

  async function updateTask(task: ReleaseTaskDto, draft: TaskDraft) {
    try {
      const saved = await api.tasks.update(task.id, payload(draft));
      setTasks((ts) => ts.map((t) => (t.id === saved.id ? saved : t)));
      // A timeframe change can open or close a pending window, so this is not toggle-only.
      refreshPending();
    } catch (e) {
      showToast(errorMessage(e, t('tasks.errors.save')));
    }
  }

  async function removeTask(task: ReleaseTaskDto) {
    if (
      !(await confirm({
        title: t('tasks.deleteConfirm', { title: taskText(language, task) }),
        confirmLabel: t('common.delete'),
        confirmVariant: 'danger',
      }))
    )
      return;
    const prev = tasks;
    setTasks((ts) => ts.filter((t) => t.id !== task.id));
    try {
      await api.tasks.delete(task.id);
      refreshPending();
    } catch (e) {
      setTasks(prev);
      showToast(errorMessage(e, t('tasks.errors.delete')));
    }
  }

  // Move a task up/down within its phase; persist the phase's new order.
  async function move(task: ReleaseTaskDto, dir: -1 | 1) {
    const list = [...(grouped.get(task.phase) ?? [])];
    const i = list.findIndex((t) => t.id === task.id);
    const j = i + dir;
    if (j < 0 || j >= list.length) return;
    [list[i], list[j]] = [list[j], list[i]];

    const reordered = list.map((t, idx) => ({ ...t, sortOrder: idx }));
    const prev = tasks;
    setTasks((ts) => ts.map((t) => reordered.find((r) => r.id === t.id) ?? t));
    try {
      await api.tasks.reorder(id, { phase: task.phase, orderedTaskIds: list.map((t) => t.id) });
    } catch (e) {
      setTasks(prev);
      showToast(errorMessage(e, t('tasks.errors.reorder')));
    }
  }

  return { tasks, grouped, toggle, addTask, updateTask, removeTask, move };
}
