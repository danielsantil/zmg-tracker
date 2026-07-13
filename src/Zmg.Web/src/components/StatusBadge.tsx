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
