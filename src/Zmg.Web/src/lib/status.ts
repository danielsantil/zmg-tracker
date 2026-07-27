import type { ReleaseStatus } from '@/types';

/**
 * Translation keys for the four derived statuses (M44). The server's values are culture-invariant
 * codes (`ReleaseStatus.cs`) and stay that way on the wire and as `cva` variant keys — only the
 * label is translated. A static map, so a renamed key is a compile error rather than a key path
 * rendered raw on screen. Lives here rather than in `StatusBadge` so the status filter can reuse it
 * without the badge file exporting a non-component.
 */
export const statusLabelKeys = {
  Upcoming: 'status.upcoming',
  Released: 'status.released',
  Complete: 'status.complete',
  Archived: 'status.archived',
} as const satisfies Record<ReleaseStatus, string>;
