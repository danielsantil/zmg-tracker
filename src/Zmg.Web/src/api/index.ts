import { artistsApi } from './artists';
import { pendingApi } from './pending';
import { releasesApi } from './releases';
import { tasksApi } from './tasks';
import { templatesApi } from './templates';
import { tracksApi } from './tracks';

/**
 * Single grouped entry point, composed from the per-entity modules. The flat shape
 * (e.g. `api.listReleases(...)`) is preserved so call sites don't care how it's split.
 */
export const api = {
  ...artistsApi,
  ...releasesApi,
  ...tasksApi,
  ...templatesApi,
  ...tracksApi,
  ...pendingApi,
};

export { ApiError } from './client';
