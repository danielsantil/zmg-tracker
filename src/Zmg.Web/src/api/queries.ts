import { useQuery } from '@tanstack/react-query';
import { api } from './index';

/**
 * TanStack Query hooks over the existing `api/` modules (M24.3). The `api/` layer stays exactly as
 * it was — thin typed fns over `client.ts`; these hooks add caching, dedup, and a single loading /
 * error surface, retiring the per-page `useState(loading/error)` + hand-rolled fetch idioms.
 *
 * `useArtists()` is the headline win: the roster was refetched on every navigation (8 independent
 * `api.artists.list()` call sites) — now one cached query serves them all.
 */

export type ReleaseFilters = NonNullable<Parameters<typeof api.releases.list>[0]>;
export type SongFilters = NonNullable<Parameters<typeof api.songs.list>[0]>;

/**
 * Query-key factory — the single source of truth for keys, so hooks and `invalidateQueries` can't
 * drift. `pending` is a prefix of `pendingByRelease`, so invalidating `['pending']` refreshes both
 * the Home list and any per-release block in one call.
 */
export const queryKeys = {
  artists: ['artists'] as const,
  releases: (filters: ReleaseFilters = {}) => ['releases', filters] as const,
  release: (id: string) => ['release', id] as const,
  songs: (filters: SongFilters = {}) => ['songs', filters] as const,
  song: (id: string) => ['song', id] as const,
  templates: ['templates'] as const,
  pending: ['pending'] as const,
  pendingByRelease: (id: string) => ['pending', id] as const,
};

export function useArtists() {
  return useQuery({ queryKey: queryKeys.artists, queryFn: () => api.artists.list() });
}

export function useReleases(filters: ReleaseFilters = {}) {
  return useQuery({ queryKey: queryKeys.releases(filters), queryFn: () => api.releases.list(filters) });
}

export function useRelease(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.release(id ?? ''),
    queryFn: () => api.releases.get(id!), // enabled-gated, so id is present when this runs
    enabled: !!id,
  });
}

export function useSongs(filters: SongFilters = {}) {
  return useQuery({ queryKey: queryKeys.songs(filters), queryFn: () => api.songs.list(filters) });
}

export function useSong(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.song(id ?? ''),
    queryFn: () => api.songs.get(id!), // enabled-gated, so id is present when this runs
    enabled: !!id,
  });
}

export function useTemplates() {
  return useQuery({ queryKey: queryKeys.templates, queryFn: () => api.templates.list() });
}

/** Global pending actions (Home). */
export function usePending() {
  return useQuery({ queryKey: queryKeys.pending, queryFn: () => api.pending.list() });
}

/** Pending actions for one release (detail "Needs attention"). */
export function usePendingByRelease(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.pendingByRelease(id ?? ''),
    queryFn: () => api.pending.listByRelease(id!), // enabled-gated
    enabled: !!id,
  });
}
