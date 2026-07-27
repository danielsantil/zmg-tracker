import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { useTranslation } from 'react-i18next';
import { api, ApiError } from '@/api';
import { useArtists, queryKeys } from '@/api/queries';
import type { SongArtistInput } from '@/types';
import { Button, EmptyState, ErrorBanner, Field, Loading, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { SongArtistsEditor } from './components/SongArtistsEditor';

/**
 * Create a catalog song directly (2.0 improvement). Follows the same rule as releases — at least one
 * artist must exist first. The song is born an orphan (no release links) until linked from a release.
 */
export default function SongFormPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const goBack = useBackNavigation();
  const queryClient = useQueryClient();
  const { data: artists = [], isLoading } = useArtists();

  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ title?: string }>({});

  const [title, setTitle] = useState('');
  const [mainArtistId, setMainArtistId] = useState('');
  const [isrc, setIsrc] = useState('');
  const [songArtists, setSongArtists] = useState<SongArtistInput[]>([]);

  // Default the main artist to the first once the roster loads (and only while none is chosen).
  const effectiveMainArtistId = mainArtistId || artists[0]?.id || '';

  // Switching the main artist drops it from any feat/collab selection (the editor already hides it).
  function changeMainArtist(id: string) {
    setMainArtistId(id);
    setSongArtists((prev) => prev.filter((a) => a.artistId !== id));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErrors([]);
    setFieldErrors({});

    if (!title.trim()) {
      setFieldErrors({ title: t('songs.form.titleRequired') });
      return;
    }

    setSaving(true);
    try {
      await api.songs.create({
        title: title.trim(),
        mainArtistId: effectiveMainArtistId,
        isrc: isrc.trim() || null,
        artists: songArtists,
      });
      void queryClient.invalidateQueries({ queryKey: queryKeys.songs() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
      goBack();
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : [t('songs.form.saveFailed')]);
    } finally {
      setSaving(false);
    }
  }

  if (isLoading) return <Loading />;

  if (artists.length === 0) {
    return (
      <EmptyState>
        <p className="text-body">{t('songs.form.needArtist')}</p>
        <Button className="mt-4" onClick={() => navigate('/artists')}>
          {t('releases.form.goToArtists')}
        </Button>
      </EmptyState>
    );
  }

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-strong">{t('songs.form.createTitle')}</h1>

      <form onSubmit={submit} className="space-y-4">
        <Field label={t('releases.form.fields.title')} error={fieldErrors.title}>
          <input
            className={clsx(inputClass, fieldErrors.title && inputErrorClass)}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t('releases.form.placeholders.title')}
            autoFocus
          />
        </Field>

        <Field label={t('releases.form.fields.mainArtist')}>
          <select className={inputClass} value={effectiveMainArtistId} onChange={(e) => changeMainArtist(e.target.value)}>
            {artists.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('songs.form.isrc')} hint={t('songs.form.isrcHintCreate')}>
          <input
            className={`${inputClass} max-w-[16rem]`}
            value={isrc}
            onChange={(e) => setIsrc(e.target.value)}
            placeholder={t('songs.form.isrcPlaceholder')}
          />
        </Field>

        <Field label={t('songs.form.feats')} hint={t('common.optional')}>
          <SongArtistsEditor
            artists={artists}
            value={songArtists}
            onChange={setSongArtists}
            mainArtistId={effectiveMainArtistId}
          />
        </Field>

        <ErrorBanner error={errors} />

        <div className="flex gap-2">
          <Button type="submit" disabled={saving}>
            {saving ? t('common.saving') : t('songs.form.createSong')}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  );
}
