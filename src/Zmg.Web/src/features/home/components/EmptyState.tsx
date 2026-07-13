import { Link } from 'react-router-dom';

export function EmptyState({ hasArtists }: { hasArtists: boolean }) {
  return (
    <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center">
      <p className="text-slate-300">No upcoming releases.</p>
      <p className="mt-1 text-sm text-slate-500">
        {hasArtists ? (
          <>
            Create one from the{' '}
            <Link to="/releases/new" className="text-accent underline">
              New release
            </Link>{' '}
            form, or browse{' '}
            <Link to="/releases" className="text-accent underline">
              All Releases
            </Link>
            .
          </>
        ) : (
          <>
            Start by adding an artist on the{' '}
            <Link to="/artists" className="text-accent underline">
              Artists
            </Link>{' '}
            page.
          </>
        )}
      </p>
    </div>
  );
}
