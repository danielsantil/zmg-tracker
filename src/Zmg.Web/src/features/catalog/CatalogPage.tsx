import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '@/api';
import { useSongs, useArtists, queryKeys } from '@/api/queries';
import type { SongListItem } from '@/types';
import {
  ArtistSelect,
  Button,
  DataTable,
  dataRowClass,
  EmptyState,
  ErrorBanner,
  FilterBar,
  Loading,
  MenuItem,
  RowMenu,
  SearchInput,
  Toast,
} from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { todayIso } from '@/lib/format';

/**
 * The catalog (M13): every song, searchable by title, ordered by title. Everything derives from the
 * earliest non-archived linked release date (M38): the Status column is Unreleased / Released / Upcoming, and the
 * only row action is Archive — offered when that date is null (orphan or archived-only, i.e. archivable).
 * Delete lives on Archived Songs; archive an orphan here and it lands there. Rows link into the detail.
 */
export default function CatalogPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { toast, toastVariant, showToast } = useToast();
  const [q, setQ] = useState('');
  const [artistId, setArtistId] = useState('');
  const debouncedQ = useDebouncedValue(q);

  const { data: artists = [] } = useArtists();
  const { data: songs = [], isLoading, error } = useSongs({
    q: debouncedQ.trim() || undefined,
    artistId: artistId || undefined,
  });

  const hasFilters = !!(q || artistId);

  const archive = useConfirmDelete<SongListItem>({
    confirm: (s) => ({
      title: t('songs.archiveConfirm.title', { title: s.title }),
      body: <p>{t('songs.archived.subtitle')}</p>,
      confirmLabel: t('common.archive'),
      confirmVariant: 'archive',
    }),
    mutate: (s) => api.songs.archive(s.id),
    invalidate: [queryKeys.songs(), queryKeys.artists],
    errorFallback: t('releases.archiveFailed'),
    showToast,
  });

  const today = todayIso();

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-strong">{t('songs.title')}</h1>
          <p className="text-sm text-muted">{t('songs.subtitle')}</p>
        </div>
        <Button onClick={() => navigate('/catalog/new')}>{t('songs.newSong')}</Button>
      </div>

      <FilterBar
        onClear={hasFilters ? () => { setQ(''); setArtistId(''); } : undefined}
        trailing={
          <Link to="/catalog/archived" className="ml-auto shrink-0 text-sm text-muted hover:text-accent">
            {t('songs.archivedLink')}
          </Link>
        }
      >
        <SearchInput value={q} onChange={setQ} />
        <ArtistSelect artists={artists} value={artistId} onChange={setArtistId} />
      </FilterBar>

      <ErrorBanner error={error ? t('songs.loadFailed') : null} />

      {isLoading ? (
        <Loading />
      ) : songs.length === 0 ? (
        <EmptyState>
          {hasFilters ? t('songs.emptyFiltered') : t('songs.empty')}
        </EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: t('songs.table.name') },
            { label: t('songs.table.mainArtist') },
            { label: t('songs.table.status') },
            { label: '', className: 'text-right' },
          ]}
        >
          {songs.map((s) => {
            // One derivation off the earliest non-archived date (M38): null → archivable orphan/archived-only.
            const archivable = s.releaseDate == null;
            return (
              <tr key={s.id} onClick={() => navigate(`/catalog/${s.id}`)} className={dataRowClass}>
                <td className="px-4 py-3 font-medium text-strong">{s.title}</td>
                <td className="px-4 py-3 text-body">{s.mainArtistName}</td>
                <td className="px-4 py-3">
                  {archivable ? (
                    <span className="text-muted">{t('status.unreleased')}</span>
                  ) : s.releaseDate! <= today ? (
                    <span className="text-okFg">{t('status.released')}</span>
                  ) : (
                    <span className="text-infoFg">{t('status.upcoming')}</span>
                  )}
                </td>
                <td className="px-4 py-3 text-right">
                  {archivable && (
                    <div onClick={(e) => e.stopPropagation()} className="flex justify-end">
                      <RowMenu label={t('songs.rowActions')}>
                        {(close) => (
                          <MenuItem tone="archive" onClick={() => { close(); void archive(s); }}>
                            {t('common.archive')}
                          </MenuItem>
                        )}
                      </RowMenu>
                    </div>
                  )}
                </td>
              </tr>
            );
          })}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
