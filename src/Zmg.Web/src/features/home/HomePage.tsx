import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
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
    invalidate: [queryKeys.releases(), queryKeys.pending],
    errorFallback: 'Failed to archive.',
    showToast,
  });

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-strong">Home</h1>
          <p className="text-sm text-muted">Upcoming releases and what needs your attention.</p>
        </div>
        <Button onClick={() => navigate('/releases/new')}>+ New release</Button>
      </div>

      <PendingSection pending={pending} />

      <FilterBar onClear={hasFilters ? () => { setArtistId(''); setType(''); setStatus(''); } : undefined}>
        <ArtistSelect artists={artists} value={artistId} onChange={setArtistId} />
        <TypeSelect value={type} onChange={setType} />
        <StatusSelect value={status} onChange={setStatus} options={['Upcoming', 'Complete']} />
      </FilterBar>

      <ErrorBanner error={error ? 'Failed to load home.' : null} />

      {isLoading ? (
        <Loading />
      ) : releases.length === 0 ? (
        <EmptyState>
          <p className="text-body">No upcoming releases.</p>
          <p className="mt-1 text-sm text-subtle">
            {artists.length > 0 ? (
              <>
                Create one from the{' '}
                <Link to="/releases/new" className="text-accent underline">New release</Link> form, or browse{' '}
                <Link to="/releases" className="text-accent underline">All Releases</Link>.
              </>
            ) : (
              <>
                Start by adding an artist on the{' '}
                <Link to="/artists" className="text-accent underline">Artists</Link> page.
              </>
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
