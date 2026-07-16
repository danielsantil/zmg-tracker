import { api } from '@/api';

/**
 * Build the archive confirmation message for a release, warning about any songs that will
 * cascade-archive alongside it (2.0). Only songs exclusive to this release and never released
 * elsewhere are pulled in — the preview endpoint applies the same rule as the archive itself.
 * The preview is best-effort: on failure we fall back to the base message.
 */
export async function archiveReleaseConfirmMessage(id: string, title: string): Promise<string> {
  const base = `Archive "${title}"? Archived releases are read-only and can't be restored.`;
  let songs: string[] = [];
  try {
    songs = (await api.releases.archivePreview(id)).songsToArchive;
  } catch {
    return base;
  }
  if (songs.length === 0) return base;
  const list = songs.map((s) => `  • ${s}`).join('\n');
  return `${base}\n\nThese new songs will be archived too (they haven't been released elsewhere):\n${list}`;
}
