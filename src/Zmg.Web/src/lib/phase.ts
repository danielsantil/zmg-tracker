import { Phase } from '@/types';

/** Canonical phase ordering used everywhere phases are listed. */
export const PHASE_ORDER: Phase[] = [Phase.Pre, Phase.Release, Phase.Post];

/**
 * Translation keys for the three phases (M43) — a `t()` call away from a label. Kept as a static
 * map rather than a template literal so a renamed key is a compile error, not a key path rendered
 * raw on screen.
 */
export const phaseLabelKeys = {
  [Phase.Pre]: 'phase.pre',
  [Phase.Release]: 'phase.release',
  [Phase.Post]: 'phase.post',
} as const satisfies Record<Phase, string>;

/**
 * Group a flat task list into a phase→tasks map in canonical order, each phase sorted by sortOrder.
 * Shared by the release detail and the templates page, which both hold a flat array and render by
 * phase (was duplicated in both).
 */
export function byPhase<T extends { phase: Phase; sortOrder: number }>(tasks: T[]): Map<Phase, T[]> {
  const map = new Map<Phase, T[]>();
  for (const phase of PHASE_ORDER) {
    map.set(
      phase,
      tasks.filter((t) => t.phase === phase).sort((a, b) => a.sortOrder - b.sortOrder),
    );
  }
  return map;
}
