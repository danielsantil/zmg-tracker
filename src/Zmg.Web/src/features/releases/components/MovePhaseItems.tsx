import type { ReleaseTaskDto } from '@/types';
import { MenuItem } from '@/components';
import { PHASE_ORDER, phaseLabels } from '@/lib/phase';

export function MovePhaseItems({
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
