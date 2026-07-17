import { Phase } from '@/types';

/** Canonical phase ordering used everywhere phases are listed. */
export const PHASE_ORDER: Phase[] = [Phase.Pre, Phase.Release, Phase.Post];

export const phaseLabels: Record<Phase, string> = {
  [Phase.Pre]: 'Pre',
  [Phase.Release]: 'Release',
  [Phase.Post]: 'Post',
};

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
