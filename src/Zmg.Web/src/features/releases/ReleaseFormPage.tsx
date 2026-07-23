import { useEffect, useReducer, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { api, ApiError } from '@/api';
import { useArtists, useRelease, useTemplates, queryKeys } from '@/api/queries';
import type { TrackInput } from '@/types';
import { ReleaseType } from '@/types';
import { Button, EmptyState, ErrorBanner, Field, Loading, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { CoverField } from './components/CoverField';
import { TracksEditor } from './components/TracksEditor';
import { emptyTrack } from './components/trackInput';

interface FormState {
  title: string;
  type: ReleaseType;
  releaseDate: string;
  mainArtistId: string;
  coverUrl: string;
  notes: string;
  upc: string;
  tracks: TrackInput[]; // create-only. A single starts with one fixed row; an album starts empty.
}

type TextField = 'title' | 'releaseDate' | 'mainArtistId' | 'coverUrl' | 'notes' | 'upc';

type FormAction =
  | { kind: 'set'; field: TextField; value: string }
  | { kind: 'setType'; value: ReleaseType }
  | { kind: 'setTracks'; value: TrackInput[] }
  | { kind: 'hydrate'; value: Partial<FormState> };

const initialForm: FormState = {
  title: '',
  type: ReleaseType.Single,
  releaseDate: '',
  mainArtistId: '',
  coverUrl: '',
  notes: '',
  upc: '',
  tracks: [emptyTrack()],
};

function formReducer(state: FormState, action: FormAction): FormState {
  switch (action.kind) {
    case 'set':
      return { ...state, [action.field]: action.value };
    // Switching type resets the Tracks section to its type's baseline (single = 1 fixed row, album = empty).
    case 'setType':
      return { ...state, type: action.value, tracks: action.value === ReleaseType.Single ? [emptyTrack()] : [] };
    case 'setTracks':
      return { ...state, tracks: action.value };
    case 'hydrate':
      return { ...state, ...action.value };
  }
}

/** Client-side validation, lifted out of submit. Mirrors the API 400s (track guards are create-only). */
function validateForm(state: FormState, isEdit: boolean): { fieldErrors: { title?: string; releaseDate?: string }; formErrors: string[] } {
  const fieldErrors: { title?: string; releaseDate?: string } = {};
  if (!state.title.trim()) fieldErrors.title = 'Release title is required.';
  if (!state.releaseDate) fieldErrors.releaseDate = 'Release date is required.';
  if (fieldErrors.title || fieldErrors.releaseDate) return { fieldErrors, formErrors: [] };

  const formErrors: string[] = [];
  if (!isEdit) {
    // A row is valid if it's an existing catalog song (songId) or a new title.
    const filled = state.tracks.filter((t) => t.songId || (t.title ?? '').trim());
    if (state.type === ReleaseType.Single && filled.length !== 1) {
      formErrors.push('A single must have exactly one track.');
    } else if (state.tracks.some((t) => !t.songId && !(t.title ?? '').trim())) {
      formErrors.push('Every new track needs a title (or pick an existing song).');
    }
  }
  return { fieldErrors, formErrors };
}

export default function ReleaseFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const goBack = useBackNavigation();
  const queryClient = useQueryClient();

  const { data: artists = [], isLoading: artistsLoading } = useArtists();
  const { data: templates = [] } = useTemplates();
  const { data: editing, isLoading: releaseLoading } = useRelease(isEdit ? id : undefined);

  const [form, dispatch] = useReducer(formReducer, initialForm);
  const [errors, setErrors] = useState<string[]>([]);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [coverUploading, setCoverUploading] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ title?: string; releaseDate?: string }>({});

  // Hydrate the form from the release being edited, once it arrives.
  useEffect(() => {
    if (!editing) return;
    dispatch({
      kind: 'hydrate',
      value: {
        title: editing.title,
        type: editing.type,
        releaseDate: editing.releaseDate,
        mainArtistId: editing.mainArtistId,
        coverUrl: editing.coverUrl ?? '',
        notes: editing.notes ?? '',
        upc: editing.upc ?? '',
      },
    });
  }, [editing]);

  // Default the main artist to the first once the roster loads (create only, and only while unset).
  useEffect(() => {
    if (!isEdit && !form.mainArtistId && artists.length > 0) {
      dispatch({ kind: 'set', field: 'mainArtistId', value: artists[0].id });
    }
  }, [isEdit, form.mainArtistId, artists]);

  const set = (field: TextField) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
    dispatch({ kind: 'set', field, value: e.target.value });

  // Live template size for the hint — drives off the real /api/templates count (no stale constant).
  const templateTaskCount = templates.find((t) => t.type === form.type)?.phases.reduce((n, p) => n + p.tasks.length, 0);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    // The submit button is disabled while a cover uploads, but Enter in any text input still fires
    // the form — saving here would persist the pre-upload coverUrl and orphan the stored image.
    if (coverUploading) return;
    setErrors([]);
    setWarnings([]);

    const { fieldErrors: fe, formErrors } = validateForm(form, isEdit);
    setFieldErrors(fe);
    if (fe.title || fe.releaseDate) return;
    if (formErrors.length > 0) {
      setErrors(formErrors);
      return;
    }

    setSaving(true);
    try {
      const cleanedTracks: TrackInput[] = form.tracks.map((t) =>
        t.songId
          ? { songId: t.songId, title: null, isrc: null, artists: null }
          : {
              songId: null,
              title: (t.title ?? '').trim(),
              isrc: (t.isrc ?? '').trim() || null,
              artists: t.artists && t.artists.length > 0 ? t.artists : null,
            },
      );

      const input = {
        title: form.title,
        type: form.type,
        releaseDate: form.releaseDate || null,
        mainArtistId: form.mainArtistId,
        coverUrl: form.coverUrl || null,
        notes: form.notes || null,
        upc: form.upc || null,
        tracks: isEdit ? null : cleanedTracks,
      };
      const result = isEdit && id ? await api.releases.update(id, input) : await api.releases.create(input);
      void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
      if (isEdit && id) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.release(id) });
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.songs() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.pending });
      if (result.warnings.length > 0) {
        setWarnings(result.warnings);
      } else {
        goBack();
      }
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : ['Failed to save release.']);
    } finally {
      setSaving(false);
    }
  }

  if (artistsLoading || (isEdit && releaseLoading)) return <Loading />;

  if (artists.length === 0) {
    return (
      <EmptyState>
        <p className="text-body">You need at least one artist before creating a release.</p>
        <Button className="mt-4" onClick={() => navigate('/artists')}>
          Go to Artists
        </Button>
      </EmptyState>
    );
  }

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-strong">{isEdit ? 'Edit release' : 'New release'}</h1>

      <form onSubmit={submit} className="space-y-4">
        <Field label="Title" error={fieldErrors.title}>
          <input
            className={clsx(inputClass, fieldErrors.title && inputErrorClass)}
            value={form.title}
            onChange={set('title')}
            placeholder="e.g. Luz"
            autoFocus
          />
        </Field>

        <Field label="Main artist">
          <select className={inputClass} value={form.mainArtistId} onChange={set('mainArtistId')}>
            {artists.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Release type">
            <select
              className={inputClass}
              value={form.type}
              onChange={(e) => dispatch({ kind: 'setType', value: Number(e.target.value) as ReleaseType })}
              disabled={isEdit}
            >
              <option value={ReleaseType.Single}>Single</option>
              <option value={ReleaseType.Album}>Album</option>
            </select>
          </Field>
          <Field label="Release date" error={fieldErrors.releaseDate}>
            <input
              type="date"
              className={clsx(inputClass, fieldErrors.releaseDate && inputErrorClass)}
              value={form.releaseDate}
              onChange={set('releaseDate')}
            />
          </Field>
        </div>

        {!isEdit && templateTaskCount !== undefined && (
          <p className="rounded-lg bg-accent/10 px-3 py-2 text-sm text-accent">
            Checklist will start from the {form.type === ReleaseType.Album ? 'Album' : 'Single'} template (
            {templateTaskCount} tasks).
          </p>
        )}

        {/* Upload or paste a URL — either way the image is stored in R2 and coverUrl holds its
            public URL (M31). */}
        <CoverField
          value={form.coverUrl}
          onChange={(value) => dispatch({ kind: 'set', field: 'coverUrl', value })}
          onUploadingChange={setCoverUploading}
        />

        <Field label="UPC" hint="Optional — blank until DSP distribution">
          <input className={`${inputClass} max-w-[16rem]`} value={form.upc} onChange={set('upc')} placeholder="e.g. 0123456789012" />
        </Field>

        <Field label="Notes" hint="Optional">
          <textarea className={inputClass} rows={3} value={form.notes} onChange={set('notes')} />
        </Field>

        {/* Tracks are set at create only; editing them happens via the release detail (and the catalog). */}
        {!isEdit && (
          <TracksEditor
            key={form.type}
            type={form.type}
            onChange={(value) => dispatch({ kind: 'setTracks', value })}
            artists={artists}
            mainArtistId={form.mainArtistId}
          />
        )}

        <ErrorBanner error={errors} />

        {warnings.length > 0 && (
          <div className="mb-4 rounded-lg bg-warn/10 px-4 py-3 text-sm text-warnFg">
            <p className="font-medium">Saved with warnings:</p>
            <ul className="ml-4 list-disc">
              {warnings.map((msg) => (
                <li key={msg}>{msg}</li>
              ))}
            </ul>
            {/* Both live inside the <form>, so they need an explicit type — the HTML default
                is `submit`, which re-fired the create POST and duplicated the release. */}
            <div className="mt-3 flex gap-2">
              <Button type="button" variant="ghost" onClick={goBack}>
                Go back
              </Button>
              <Button type="button" variant="ghost" onClick={() => setWarnings([])}>
                Keep editing
              </Button>
            </div>
          </div>
        )}

        <div className="flex gap-2">
          <Button type="submit" disabled={saving || coverUploading}>
            {saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create release'}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
