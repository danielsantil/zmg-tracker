import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Artist, SongArtistInput } from '@/types';
import { Button, inputClass } from '@/components';
import { SongArtistsEditor } from '@/features/catalog/components/SongArtistsEditor';
import type { NewTrackDraft } from './Tracklist';

/**
 * Add-a-new-track form for the release detail tracklist: a title field with an optional
 * "Details (optional)" disclosure (ISRC + feats), submitted in one shot. The create form keeps
 * its own per-row disclosure (details persist only on release-create); the detail page persists
 * immediately, so a new song's ISRC/feats are collected here at add time rather than left null.
 */
export function NewTrackForm({
  artists,
  mainArtistId,
  onAdd,
}: {
  artists: Artist[];
  mainArtistId: string;
  /** Resolve `false` to keep the form open with its values (e.g. a rejected duplicate title). */
  onAdd: (draft: NewTrackDraft) => void | boolean | Promise<void | boolean>;
}) {
  const { t } = useTranslation();
  const [adding, setAdding] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [title, setTitle] = useState('');
  const [isrc, setIsrc] = useState('');
  const [feats, setFeats] = useState<SongArtistInput[]>([]);

  function reset() {
    setTitle('');
    setIsrc('');
    setFeats([]);
    setAdding(false);
  }

  async function submit() {
    const t = title.trim();
    if (!t || submitting) return;
    setSubmitting(true);
    try {
      const ok = await onAdd({ title: t, isrc: isrc.trim() || null, artists: feats });
      // Keep the typed values in place when the add was rejected, so the user can rename and retry.
      if (ok !== false) reset();
    } finally {
      setSubmitting(false);
    }
  }

  if (!adding) {
    return (
      <div className="border-t border-edge px-3 py-2">
        <button
          type="button"
          className="rounded-lg px-2 py-1.5 text-sm text-muted hover:text-accent"
          onClick={() => setAdding(true)}
        >
          {t('tracks.addTrack')}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-2 border-t border-edge px-3 py-3">
      <input
        autoFocus
        className={inputClass}
        placeholder={t('tracks.newTrackTitle')}
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') void submit();
          if (e.key === 'Escape') reset();
        }}
      />
      <details className="text-sm">
        <summary className="cursor-pointer text-xs text-subtle hover:text-body">
          {t('tracks.detailsOptional')}
        </summary>
        <div className="mt-2 space-y-2">
          <input
            className={`${inputClass} max-w-[16rem]`}
            placeholder={t('tracks.isrcOptional')}
            value={isrc}
            onChange={(e) => setIsrc(e.target.value)}
          />
          <SongArtistsEditor
            artists={artists}
            value={feats}
            onChange={setFeats}
            mainArtistId={mainArtistId}
          />
        </div>
      </details>
      <div className="flex gap-2">
        <Button type="button" onClick={submit} disabled={submitting}>
          {submitting ? t('common.adding') : t('common.add')}
        </Button>
        <Button type="button" variant="ghost" onClick={reset}>
          {t('common.cancel')}
        </Button>
      </div>
    </div>
  );
}
