import { api } from '@/api';
import type { ConfirmOptions } from '@/components';

/**
 * Build the archive confirmation for a release, warning about any songs that will
 * cascade-archive alongside it (2.0). Only songs exclusive to this release and never released
 * elsewhere are pulled in — the preview endpoint applies the same rule as the archive itself.
 * The preview is best-effort: on failure we fall back to the bare confirmation.
 */
export async function archiveReleaseConfirm(id: string, title: string): Promise<ConfirmOptions> {
  const base: ConfirmOptions = {
    title: `Archive "${title}"?`,
    body: <p>Archived releases are read-only and can't be restored.</p>,
    confirmLabel: 'Archive',
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
        <p>Archived releases are read-only and can't be restored.</p>
        <p className="mt-3">These new songs will be archived too (they haven't been released elsewhere):</p>
        <ul className="mt-1 list-disc pl-5 text-muted">
          {songs.map((s) => (
            <li key={s}>{s}</li>
          ))}
        </ul>
      </>
    ),
  };
}
