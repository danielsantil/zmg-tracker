import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { useTranslation } from 'react-i18next';
// The load effect translates off the i18n instance rather than the hook's `t`: adding `t` to its
// deps would re-fetch the artist on every language switch, and this message is produced once, at
// throw time. Same precedent as `releases/archiveConfirm.tsx`.
import i18n from '@/i18n';
import { api, ApiError } from '@/api';
import { queryKeys } from '@/api/queries';
import { Button, ErrorBanner, Field, Loading, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';

/**
 * Create/edit an artist on a dedicated page (M19), mirroring SongFormPage and replacing the old
 * inline ArtistForm. No `:id` → create; with `:id` → load and prefill for edit. Leaves room for
 * future artist fields (DSP ids, etc.).
 */
export default function ArtistFormPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const goBack = useBackNavigation();
  const queryClient = useQueryClient();
  const { id } = useParams();
  const editing = Boolean(id);

  const [loading, setLoading] = useState(editing);
  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ name?: string }>({});

  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');

  useEffect(() => {
    if (!id) return;
    void (async () => {
      try {
        const artist = await api.artists.get(id);
        setName(artist.name);
        setNotes(artist.notes ?? '');
      } catch (err) {
        setErrors(err instanceof ApiError ? err.messages : [i18n.t('artists.form.loadFailed')]);
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErrors([]);
    setFieldErrors({});

    if (!name.trim()) {
      setFieldErrors({ name: t('artists.form.nameRequired') });
      return;
    }

    setSaving(true);
    try {
      const input = { name: name.trim(), notes: notes.trim() || null };
      if (id) {
        await api.artists.update(id, input);
        void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
        goBack();
      } else {
        await api.artists.create(input);
        void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
        void navigate('/artists');
      }
    } catch (err) {
      setErrors(err instanceof ApiError ? err.messages : [t('artists.form.saveFailed')]);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <Loading />;

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-strong">{editing ? t('artists.form.editTitle') : t('artists.form.createTitle')}</h1>

      <form onSubmit={submit} className="space-y-4">
        <Field label={t('artists.form.name')} error={fieldErrors.name}>
          <input
            className={clsx(inputClass, fieldErrors.name && inputErrorClass)}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t('artists.form.namePlaceholder')}
            autoFocus
          />
        </Field>

        <Field label={t('releases.form.fields.notes')} hint={t('common.optional')}>
          <textarea className={inputClass} rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>

        <ErrorBanner error={errors} />

        <div className="flex gap-2">
          <Button type="submit" disabled={saving}>
            {saving ? t('common.saving') : editing ? t('common.save') : t('artists.form.createArtist')}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  );
}
