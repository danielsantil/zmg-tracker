import { Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, errorMessage } from '@/api';
import { useArtists, queryKeys } from '@/api/queries';
import type { Artist } from '@/types';
import { Button, DataTable, dataRowClass, EmptyState, Loading, MenuItem, RowMenu, Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';

/**
 * Artists roster (M19): a bordered table (Name · Releases · Songs · Actions) matching Catalog/Releases,
 * with a kebab per row. Delete checks the release/song counts the row already carries and branches
 * *before* asking — an info modal when the artist is still referenced, a red confirm when it's clean —
 * so the server-side guard never surfaces as a post-hoc error toast. Create/edit live on dedicated pages.
 */
export default function ArtistsPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const { toast, toastVariant, showToast } = useToast();
  const { data: artists = [], isLoading } = useArtists();

  async function remove(a: Artist) {
    const activeDependents = a.releaseCount + a.songCount + a.creditCount;
    if (activeDependents > 0) {
      const parts = [
        a.releaseCount > 0 ? t('artists.counts.releases', { count: a.releaseCount }) : null,
        a.songCount > 0 ? t('artists.counts.songs', { count: a.songCount }) : null,
        a.creditCount > 0 ? t('artists.counts.credits', { count: a.creditCount }) : null,
      ].filter(Boolean);
      await confirm({
        title: t('artists.deleteBlocked.title', { name: a.name }),
        body: <p>{t('artists.deleteBlocked.body', { parts: parts.join(', ') })}</p>,
        confirmLabel: t('common.ok'),
        hideCancel: true,
      });
      return;
    }

    // No active ties, but archived data may reference the artist — warn that deleting the artist
    // permanently removes that archived data too (the server cascades it in the same delete).
    const archivedParts = [
      a.archivedReleaseCount > 0
        ? t('artists.counts.archivedReleases', { count: a.archivedReleaseCount })
        : null,
      a.archivedSongCount > 0
        ? t('artists.counts.archivedSongs', { count: a.archivedSongCount })
        : null,
    ].filter(Boolean);

    if (
      !(await confirm({
        title: t('artists.deleteConfirm.title', { name: a.name }),
        body:
          archivedParts.length > 0 ? (
            <p>
              {t('artists.deleteConfirm.archivedBody', {
                parts: archivedParts.join(` ${t('common.and')} `),
              })}
            </p>
          ) : (
            <p>{t('common.cannotBeUndone')}</p>
          ),
        confirmLabel: t('common.delete'),
        confirmVariant: 'danger',
      }))
    )
      return;
    try {
      await api.artists.delete(a.id);
      void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
      void queryClient.invalidateQueries({ queryKey: queryKeys.songs() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
    } catch (e) {
      // Concurrency safety net: a release/song could have been added since the list loaded.
      showToast(errorMessage(e, t('artists.deleteFailed')));
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-strong">{t('artists.title')}</h1>
          <p className="text-sm text-muted">{t('artists.subtitle')}</p>
        </div>
        <Button onClick={() => navigate('/artists/new')}>{t('artists.newArtist')}</Button>
      </div>

      {isLoading ? (
        <Loading />
      ) : artists.length === 0 ? (
        <EmptyState>{t('artists.empty')}</EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: t('artists.table.name') },
            { label: t('artists.table.releases') },
            { label: t('artists.table.songs') },
            { label: t('artists.table.collabs') },
            { label: '', className: 'text-right' },
          ]}
        >
          {artists.map((a) => (
            <tr key={a.id} onClick={() => navigate(`/artists/${a.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <Link
                  to={`/artists/${a.id}`}
                  onClick={(e) => e.stopPropagation()}
                  className="font-medium text-strong hover:text-accent"
                >
                  {a.name}
                </Link>
                {a.notes && <p className="text-xs text-subtle">{a.notes}</p>}
              </td>
              <td className="px-4 py-3 text-body">{a.releaseCount}</td>
              <td className="px-4 py-3 text-body">{a.songCount}</td>
              <td className="px-4 py-3 text-body">{a.creditCount}</td>
              <td className="px-4 py-3 text-right">
                <div onClick={(e) => e.stopPropagation()} className="flex justify-end">
                  <RowMenu label={t('artists.rowActions')}>
                    {(close) => (
                      <MenuItem
                        tone="danger"
                        onClick={() => {
                          close();
                          void remove(a);
                        }}
                      >
                        {t('common.delete')}
                      </MenuItem>
                    )}
                  </RowMenu>
                </div>
              </td>
            </tr>
          ))}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
