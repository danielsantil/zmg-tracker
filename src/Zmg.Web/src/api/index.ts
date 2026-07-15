import { artistsApi } from './artists';
import { pendingApi } from './pending';
import { releasesApi } from './releases';
import { songsApi } from './songs';
import { tasksApi } from './tasks';
import { templatesApi } from './templates';
import { tracksApi } from './tracks';

/**
 * Namespaced entry point: one property per entity, e.g. `api.artists.list()`,
 * `api.releases.get(id)`, `api.tasks.toggle(id)`. Each namespace is defined in its
 * own module under api/.
 */
export const api = {
  artists: artistsApi,
  releases: releasesApi,
  songs: songsApi,
  tasks: tasksApi,
  templates: templatesApi,
  tracks: tracksApi,
  pending: pendingApi,
};

export { ApiError } from './client';
