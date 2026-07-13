import type { TemplateTaskDto } from '@/types';
import { MenuItem } from '@/components';
import { PHASE_ORDER, phaseLabels } from '@/lib/phase';

export function TemplateMovePhaseItems({
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
