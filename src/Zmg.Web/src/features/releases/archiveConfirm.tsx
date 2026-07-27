import { api } from '@/api';
import type { ConfirmOptions } from '@/components';
// Not a component, so there's no `useTranslation` — module helpers translate off the i18n instance
// directly. Safe because `main.tsx` initializes it before anything can call this.
import i18n from '@/i18n';

/**
 * Build the archive confirmation for a release, warning about any songs that will
 * cascade-archive alongside it (2.0). Only songs exclusive to this release and never released
 * elsewhere are pulled in — the preview endpoint applies the same rule as the archive itself.
 * The preview is best-effort: on failure we fall back to the bare confirmation.
 */
export async function archiveReleaseConfirm(id: string, title: string): Promise<ConfirmOptions> {
  const base: ConfirmOptions = {
    title: i18n.t('releases.archiveConfirm.title', { title }),
    body: <p>{i18n.t('releases.archived.subtitle')}</p>,
    confirmLabel: i18n.t('common.archive'),
    confirmVariant: 'archive',
  };
  let songs: string[] = [];
  try {
    songs = (await api.releases.archivePreview(id)).songsToArchive;
  } catch {
    return base;
  }
  if (songs.length === 0) return base;
  return {
    ...base,
    body: (
      <>
        <p>{i18n.t('releases.archived.subtitle')}</p>
        <p className="mt-3">{i18n.t('releases.archiveConfirm.cascade')}</p>
        <ul className="mt-1 list-disc pl-5 text-muted">
          {songs.map((s) => (
            <li key={s}>{s}</li>
          ))}
        </ul>
      </>
    ),
  };
}
