import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api, errorMessage } from '@/api';
import { queryKeys } from '@/api/queries';
import type { Phase, ReleaseTaskDto } from '@/types';
import { byPhase } from '@/lib/phase';
import { useConfirm } from '@/hooks/useConfirm';
import type { TaskPatch } from '../components/TaskRow';

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
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const [tasks, setTasks] = useState<ReleaseTaskDto[]>(initial);

  // Reseed when the release query yields new data (initial load, refetch on focus/return).
  useEffect(() => setTasks(initial), [initial]);

  const refreshPending = () => void queryClient.invalidateQueries({ queryKey: queryKeys.pending });

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
      showToast(errorMessage(e, 'Could not save — reverted.'));
    }
  }

  async function addTask(phase: Phase, title: string) {
    try {
      const created = await api.tasks.add(id, { title, phase });
      setTasks((ts) => [...ts, created]);
    } catch (e) {
      showToast(errorMessage(e, 'Could not add task.'));
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
      showToast(errorMessage(e, 'Could not save task.'));
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
      showToast(errorMessage(e, 'Could not delete task.'));
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
      showToast(errorMessage(e, 'Could not reorder.'));
    }
  }

  return { tasks, grouped, toggle, addTask, updateTask, removeTask, move };
}
