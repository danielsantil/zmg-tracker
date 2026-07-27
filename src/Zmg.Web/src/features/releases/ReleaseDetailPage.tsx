import { useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { Trans, useTranslation } from 'react-i18next';
import { api, errorMessage } from '@/api';
import { useRelease, usePendingByRelease, useArtists, queryKeys } from '@/api/queries';
import type { ReleaseDetail } from '@/types';
import { ReleaseType } from '@/types';
import { Button, ErrorBanner, Loading, Modal, Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { PHASE_ORDER } from '@/lib/phase';
import { ReleaseHeader } from './components/ReleaseHeader';
import { NeedsAttention } from './components/NeedsAttention';
import { PhaseSection } from './components/PhaseSection';
import { Tracklist } from './components/Tracklist';
import { SongCard } from './components/SongCard';
import { archiveReleaseConfirm } from './archiveConfirm';
import { useReleaseTasks } from './hooks/useReleaseTasks';
import { useReleaseTracks } from './hooks/useReleaseTracks';

export default function ReleaseDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const { data: release, isLoading, error } = useRelease(id);

  if (isLoading) return <Loading />;
  if (error) return <ErrorBanner error={t('releases.detail.loadFailed')} />;
  if (!release) return null;

  // Inner view receives a guaranteed release, so id and the loaded data are never null below.
  return <ReleaseDetailView release={release} />;
}

function ReleaseDetailView({ release }: { release: ReleaseDetail }) {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const goBack = useBackNavigation();
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const { toast, toastVariant, showToast } = useToast();

  const { data: pendingActions = [] } = usePendingByRelease(release.id);
  // The full artist list feeds the feats editor when adding a new track (an album's main artist
  // can't feature on their own song). Cached across the app, so this is usually a cache hit.
  const { data: artists = [] } = useArtists();

  const initialTasks = useMemo(() => release.phases.flatMap((p) => p.tasks), [release]);
  const { tasks, grouped, toggle, addTask, updateTask, removeTask, move } = useReleaseTasks(
    release.id,
    initialTasks,
    showToast,
  );
  const {
    orderedTracks,
    trackRows,
    track,
    addTrack,
    addExistingTrack,
    toggleFocus,
    removeTrack,
    moveTrack,
    dupPrompt,
    setDupPrompt,
  } = useReleaseTracks(release, release.tracks, showToast);

  const done = tasks.filter((t) => t.isDone).length;
  const total = tasks.length;

  async function archive() {
    if (!(await confirm(await archiveReleaseConfirm(release.id, release.title)))) return;
    try {
      await api.releases.archive(release.id);
      void queryClient.invalidateQueries({ queryKey: queryKeys.release(release.id) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.pending });
      void queryClient.invalidateQueries({ queryKey: queryKeys.songs() });
    } catch (e) {
      showToast(errorMessage(e, t('releases.detail.archiveFailed')));
    }
  }

  // Archived releases are terminal and read-only: no edit, no toggles, no add/menu controls.
  const readOnly = release.isArchived;
  // Archive affordance follows the server's canArchive (upcoming & not archived).
  const canArchive = release.canArchive;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <button onClick={goBack} className="text-sm text-muted hover:text-body">
          {t('releases.detail.back')}
        </button>
        {readOnly ? (
          <span className="text-sm text-subtle">{t('releases.detail.readOnly')}</span>
        ) : (
          <div className="flex items-center gap-2">
            <Button variant="ghost" onClick={() => navigate(`/releases/${release.id}/edit`)}>
              {t('common.edit')}
            </Button>
            {canArchive && (
              <Button variant="archive" onClick={archive}>
                {t('common.archive')}
              </Button>
            )}
          </div>
        )}
      </div>

      <ReleaseHeader release={release} done={done} total={total} />

      {pendingActions.length > 0 && <NeedsAttention actions={pendingActions} />}

      {release.notes && (
        <p className="mb-6 whitespace-pre-wrap rounded-lg border border-edge bg-panel/50 px-4 py-3 text-sm text-body">
          {release.notes}
        </p>
      )}

      <div className="mb-6">
        {release.type === ReleaseType.Single ? (
          orderedTracks.length > 0 && (
            <SongCard track={orderedTracks[0]} mainArtistName={release.mainArtistName} />
          )
        ) : (
          <Tracklist
            rows={trackRows}
            readOnly={readOnly}
            mainArtistId={release.mainArtistId}
            excludeIds={orderedTracks.map((t) => t.songId)}
            artists={artists}
            onAddNew={addTrack}
            onAddExisting={(song) => addExistingTrack(song.id)}
            onToggleFocus={(row) => toggleFocus(track(row))}
            onRemove={(row) => removeTrack(track(row))}
            onMove={(row, dir) => moveTrack(track(row), dir)}
          />
        )}
      </div>

      <div className="space-y-3">
        {PHASE_ORDER.map((phase) => (
          <PhaseSection
            key={phase}
            phase={phase}
            tasks={grouped.get(phase) ?? []}
            readOnly={readOnly}
            onToggle={toggle}
            getIsDone={(t) => t.isDone}
            getNotes={(t) => t.notes}
            onAdd={(title) => addTask(phase, title)}
            onUpdate={updateTask}
            onDelete={removeTask}
            onMove={move}
          />
        ))}
      </div>

      <Modal open={!!dupPrompt} onClose={() => setDupPrompt(null)} title={t('releases.detail.dupSong.title')}>
        {dupPrompt && (
          <div className="space-y-4">
            <p className="text-sm text-body">
              <Trans
                i18nKey="releases.detail.dupSong.body"
                values={{ title: dupPrompt.title, artist: release.mainArtistName }}
                components={{ songTitle: <span className="font-medium text-strong" /> }}
              />{' '}
              {dupPrompt.onRelease
                ? t('releases.detail.dupSong.onRelease')
                : t('releases.detail.dupSong.elsewhere')}
            </p>
            <div className="flex gap-2">
              {dupPrompt.existing && !dupPrompt.onRelease && (
                <Button
                  onClick={async () => {
                    const song = dupPrompt.existing;
                    if (!song) return;
                    setDupPrompt(null);
                    await addExistingTrack(song.id);
                  }}
                >
                  {t('releases.detail.dupSong.addExisting')}
                </Button>
              )}
              <Button variant="ghost" onClick={() => setDupPrompt(null)}>
                {t('releases.detail.dupSong.rename')}
              </Button>
            </div>
          </div>
        )}
      </Modal>

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
