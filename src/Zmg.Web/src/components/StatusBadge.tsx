import { cva } from 'class-variance-authority';
import { useTranslation } from 'react-i18next';
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

// The server's status values are culture-invariant codes (ReleaseStatus.cs) — they stay the `cva`
// variant key, and only the *label* is translated. Static map, so a renamed key won't compile (M43).
const labelKeys = {
  Upcoming: 'status.upcoming',
  Released: 'status.released',
  Complete: 'status.complete',
  Archived: 'status.archived',
} as const satisfies Record<ReleaseStatus, string>;

export function StatusBadge({ status }: { status: ReleaseStatus }) {
  const { t } = useTranslation();
  return <span className={badge({ status })}>{t(labelKeys[status])}</span>;
}
