import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Trans, useTranslation } from 'react-i18next';
import { api } from '@/api';
import { useReleases, useArtists, usePending, queryKeys } from '@/api/queries';
import type { ReleaseListItem } from '@/types';
import { ReleaseType } from '@/types';
import { ArtistSelect, Button, EmptyState, ErrorBanner, FilterBar, Loading, StatusSelect, Toast, TypeSelect } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';
import { PendingSection } from './components/PendingSection';
import { ReleaseCard } from '../releases/components/ReleaseCard';
import { archiveReleaseConfirm } from '../releases/archiveConfirm';

export default function HomePage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { toast, toastVariant, showToast } = useToast();

  const [artistId, setArtistId] = useState('');
  const [type, setType] = useState('');
  const [status, setStatus] = useState('');

  const { data: artists = [] } = useArtists();
  const { data: pending = [] } = usePending();
  const { data: releases = [], isLoading, error } = useReleases({
    scope: 'home',
    artistId: artistId || undefined,
    type: type === '' ? undefined : (Number(type) as ReleaseType),
    status: status || undefined,
  });

  const hasFilters = !!(artistId || type || status);

  const archive = useConfirmDelete<ReleaseListItem>({
    confirm: (r) => archiveReleaseConfirm(r.id, r.title),
    mutate: (r) => api.releases.archive(r.id),
    invalidate: [queryKeys.releases(), queryKeys.pending, queryKeys.artists],
    errorFallback: t('releases.archiveFailed'),
    showToast,
  });

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-strong">{t('home.title')}</h1>
          <p className="text-sm text-muted">{t('home.subtitle')}</p>
        </div>
        <Button onClick={() => navigate('/releases/new')}>{t('releases.newRelease')}</Button>
      </div>

      <PendingSection pending={pending} />

      <FilterBar onClear={hasFilters ? () => { setArtistId(''); setType(''); setStatus(''); } : undefined}>
        <ArtistSelect artists={artists} value={artistId} onChange={setArtistId} />
        <TypeSelect value={type} onChange={setType} />
        <StatusSelect value={status} onChange={setStatus} options={['Upcoming', 'Complete']} />
      </FilterBar>

      <ErrorBanner error={error ? t('home.loadFailed') : null} />

      {isLoading ? (
        <Loading />
      ) : releases.length === 0 ? (
        <EmptyState>
          <p className="text-body">{t('home.empty.title')}</p>
          <p className="mt-1 text-sm text-subtle">
            {artists.length > 0 ? (
              <Trans
                i18nKey="home.empty.createHint"
                components={{
                  newRelease: <Link to="/releases/new" className="text-accent underline" />,
                  all: <Link to="/releases" className="text-accent underline" />,
                }}
              />
            ) : (
              <Trans
                i18nKey="home.empty.artistHint"
                components={{ artists: <Link to="/artists" className="text-accent underline" /> }}
              />
            )}
          </p>
        </EmptyState>
      ) : (
        <div className="grid items-start gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {releases.map((r) => (
            <ReleaseCard
              key={r.id}
              r={r}
              showCover
              onOpen={() => navigate(`/releases/${r.id}`)}
              onEdit={() => navigate(`/releases/${r.id}/edit`)}
              onArchive={() => archive(r)}
            />
          ))}
        </div>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
