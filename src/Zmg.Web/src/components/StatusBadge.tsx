import { cva } from 'class-variance-authority';
import type { ReleaseStatus } from '@/types';

const badge = cva('rounded-full px-2 py-0.5 text-xs font-medium ring-1', {
  variants: {
    status: {
      Upcoming: 'bg-info/15 text-infoFg ring-info/30',
      Released: 'bg-warn/15 text-warnFg ring-warn/30',
      Complete: 'bg-ok/15 text-okFg ring-ok/30',
      Archived: 'bg-subtle/15 text-body ring-subtle/30',
    },
  },
  defaultVariants: { status: 'Archived' },
});

export function StatusBadge({ status }: { status: ReleaseStatus }) {
  return <span className={badge({ status })}>{status}</span>;
}
