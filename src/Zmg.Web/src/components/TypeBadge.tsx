import { ReleaseType } from '@/types';

export function TypeBadge({ type }: { type: ReleaseType }) {
  const label = type === ReleaseType.Album ? 'Album' : 'Single';
  return (
    <span className="rounded-full bg-edge px-2 py-0.5 text-xs font-medium text-slate-300">
      {label}
    </span>
  );
}
