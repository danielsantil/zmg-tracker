import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '@/api';
import { useSongs, queryKeys } from '@/api/queries';
import type { SongListItem } from '@/types';
import { Button, DataTable, dataRowClass, EmptyState, ErrorBanner, Loading, Toast } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';

/**
 * Archived Songs (M15) — the terminal, read-only bucket, mirroring Archived Releases. Table is
 * Name · Main Artist · Action, where the action is Delete: a hard-delete on the server (M36).
 * Reached via the "Archived Songs →" link on the catalog, not a nav item.
 */
export default function ArchivedSongsPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { toast, toastVariant, showToast } = useToast();
  const { data: songs = [], isLoading, error } = useSongs({ scope: 'archived' });

  const remove = useConfirmDelete<SongListItem>({
    confirm: (s) => ({
      title: t('releases.deleteConfirm.title', { title: s.title }),
      body: <p>{t('common.cannotBeUndone')}</p>,
      confirmLabel: t('common.delete'),
      confirmVariant: 'danger',
    }),
    mutate: (s) => api.songs.delete(s.id),
    invalidate: [queryKeys.songs(), queryKeys.artists],
    errorFallback: t('releases.deleteFailed'),
    showToast,
  });

  return (
    <div>
      <div className="mb-6">
        <Link to="/catalog" className="text-sm text-muted hover:text-body">
          {t('songs.archived.back')}
        </Link>
        <h1 className="mt-2 text-2xl font-semibold text-strong">{t('songs.archived.title')}</h1>
        <p className="text-sm text-muted">{t('songs.archived.subtitle')}</p>
      </div>

      <ErrorBanner error={error ? t('songs.archived.loadFailed') : null} />

      {isLoading ? (
        <Loading />
      ) : songs.length === 0 ? (
        <EmptyState>{t('songs.archived.empty')}</EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: t('songs.table.name') },
            { label: t('songs.table.mainArtist') },
            { label: '', className: 'text-right' },
          ]}
        >
          {songs.map((s) => (
            <tr key={s.id} onClick={() => navigate(`/catalog/${s.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <Link
                  to={`/catalog/${s.id}`}
                  onClick={(e) => e.stopPropagation()}
                  className="font-medium text-strong hover:text-accent"
                >
                  {s.title}
                </Link>
              </td>
              <td className="px-4 py-3 text-body">{s.mainArtistName}</td>
              <td className="px-4 py-3 text-right">
                <Button
                  variant="danger"
                  onClick={(e) => {
                    e.stopPropagation();
                    void remove(s);
                  }}
                >
                  {t('common.delete')}
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
