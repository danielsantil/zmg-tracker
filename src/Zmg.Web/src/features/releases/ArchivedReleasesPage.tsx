import { Link, useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useReleases, queryKeys } from '@/api/queries';
import type { ReleaseListItem } from '@/types';
import { Button, DataTable, dataRowClass, EmptyState, ErrorBanner, Loading, StatusBadge, Toast, TypeBadge } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';

/**
 * Archived Releases (v1.2) — the terminal, read-only bucket. Same table design as All Releases
 * (Name · Type · Released Date · Action), but the action is Remove: a hard-delete on the server
 * (M36). Reached via the "Archived Releases →" link on All Releases,
 * not a nav item.
 */
export default function ArchivedReleasesPage() {
  const navigate = useNavigate();
  const { toast, toastVariant, showToast } = useToast();
  const { data: releases = [], isLoading, error } = useReleases({ scope: 'archived' });

  const remove = useConfirmDelete<ReleaseListItem>({
    confirm: (r) => ({
      title: `Delete "${r.title}"?`,
      body: <p>This can't be undone.</p>,
      confirmLabel: 'Delete',
      confirmVariant: 'danger',
    }),
    mutate: (r) => api.releases.delete(r.id),
    invalidate: [queryKeys.releases()],
    errorFallback: 'Failed to delete.',
    showToast,
  });

  return (
    <div>
      <div className="mb-6">
        <Link to="/releases" className="text-sm text-muted hover:text-body">
          ‹ All Releases
        </Link>
        <h1 className="mt-2 text-2xl font-semibold text-strong">Archived Releases</h1>
        <p className="text-sm text-muted">Archived releases are read-only and can't be restored.</p>
      </div>

      <ErrorBanner error={error ? 'Failed to load archived releases.' : null} />

      {isLoading ? (
        <Loading />
      ) : releases.length === 0 ? (
        <EmptyState>No archived releases.</EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: 'Name' },
            { label: 'Type', className: 'hidden sm:table-cell' },
            { label: 'Released Date' },
            { label: '', className: 'text-right' },
          ]}
        >
          {releases.map((r) => (
            <tr key={r.id} onClick={() => navigate(`/releases/${r.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <div className="flex items-center gap-1.5">
                  <Link
                    to={`/releases/${r.id}`}
                    onClick={(e) => e.stopPropagation()}
                    className="font-medium text-strong hover:text-accent"
                  >
                    {r.title}
                  </Link>
                  <StatusBadge status={r.status} />
                </div>
                <div className="text-xs text-muted">{r.mainArtistName}</div>
                {/* Below sm the Type column is hidden — fold its badge under the name. */}
                <div className="mt-1 flex items-center gap-1.5 sm:hidden">
                  <TypeBadge type={r.type} />
                </div>
              </td>
              <td className="hidden px-4 py-3 sm:table-cell">
                <TypeBadge type={r.type} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-body">{r.releaseDate}</td>
              <td className="px-4 py-3 text-right">
                <Button
                  variant="danger"
                  onClick={(e) => {
                    e.stopPropagation();
                    void remove(r);
                  }}
                >
                  Delete
                </Button>
              </td>
            </tr>
          ))}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
