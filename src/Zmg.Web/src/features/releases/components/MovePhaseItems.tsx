import { useTranslation } from 'react-i18next';
import type { Phase } from '@/types';
import { MenuItem } from '@/components';
import { PHASE_ORDER, phaseLabelKeys } from '@/lib/phase';

/**
 * The "Move to <phase>" menu items for a task — one per phase other than its current one. Generic
 * over anything carrying a `phase`, so release tasks and template tasks share it (M24.5).
 */
export function MovePhaseItems<T extends { phase: Phase }>({
  task,
  onUpdate,
  close,
}: {
  task: T;
  onUpdate: (t: T, patch: { phase: Phase }) => void;
  close: () => void;
}) {
  const { t } = useTranslation();
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
          {t('tasks.moveToPhase', { phase: t(phaseLabelKeys[p]) })}
        </MenuItem>
      ))}
    </>
  );
}
