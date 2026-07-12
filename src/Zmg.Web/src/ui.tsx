import type { ReactNode } from 'react';
import { Phase, ReleaseType } from './types';

export const phaseLabels: Record<Phase, string> = {
  [Phase.Pre]: 'Pre',
  [Phase.Release]: 'Release',
  [Phase.Post]: 'Post',
};

export function ProgressBar({ done, total }: { done: number; total: number }) {
  const pct = total === 0 ? 0 : Math.round((done / total) * 100);
  return (
    <div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-edge">
        <div
          className="h-full rounded-full bg-accent transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
      <div className="mt-1 text-xs text-slate-400">
        {done}/{total} done · {pct}%
      </div>
    </div>
  );
}

const statusStyles: Record<string, string> = {
  Upcoming: 'bg-sky-500/15 text-sky-300 ring-sky-500/30',
  Released: 'bg-amber-500/15 text-amber-300 ring-amber-500/30',
  Complete: 'bg-emerald-500/15 text-emerald-300 ring-emerald-500/30',
};

export function StatusBadge({ status }: { status: string }) {
  const cls = statusStyles[status] ?? 'bg-slate-500/15 text-slate-300 ring-slate-500/30';
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ring-1 ${cls}`}>
      {status}
    </span>
  );
}

export function TypeBadge({ type }: { type: ReleaseType }) {
  const label = type === ReleaseType.Album ? 'Album' : 'Single';
  return (
    <span className="rounded-full bg-edge px-2 py-0.5 text-xs font-medium text-slate-300">
      {label}
    </span>
  );
}

export function Button({
  children,
  variant = 'primary',
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'ghost' | 'danger' }) {
  const base =
    'inline-flex items-center justify-center rounded-lg px-3 py-2 text-sm font-medium transition disabled:opacity-50 disabled:cursor-not-allowed';
  const variants = {
    primary: 'bg-accent text-white hover:bg-accent/90',
    ghost: 'bg-edge text-slate-200 hover:bg-edge/70',
    danger: 'bg-red-500/15 text-red-300 ring-1 ring-red-500/30 hover:bg-red-500/25',
  };
  return (
    <button className={`${base} ${variants[variant]}`} {...props}>
      {children}
    </button>
  );
}

export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-300">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-slate-500">{hint}</span>}
    </label>
  );
}

export const inputClass =
  'w-full rounded-lg border border-edge bg-panel px-3 py-2 text-sm text-slate-100 outline-none focus:border-accent';

export function daysToRelease(date: string): number {
  const d = new Date(date + 'T00:00:00');
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  return Math.round((d.getTime() - now.getTime()) / 86_400_000);
}
