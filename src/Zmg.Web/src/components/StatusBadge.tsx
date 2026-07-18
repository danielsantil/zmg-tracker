import { cva } from 'class-variance-authority';
import type { ReleaseStatus } from '@/types';

const badge = cva('rounded-full px-2 py-0.5 text-xs font-medium ring-1', {
  variants: {
    status: {
      Upcoming: 'bg-sky-500/15 text-sky-300 ring-sky-500/30',
      Released: 'bg-amber-500/15 text-amber-300 ring-amber-500/30',
      Complete: 'bg-emerald-500/15 text-emerald-300 ring-emerald-500/30',
      Archived: 'bg-slate-500/15 text-body ring-slate-500/30',
    },
  },
  defaultVariants: { status: 'Archived' },
});

export function StatusBadge({ status }: { status: ReleaseStatus }) {
  return <span className={badge({ status })}>{status}</span>;
}
