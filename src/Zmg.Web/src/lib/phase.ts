import { Phase } from '@/types';

/** Canonical phase ordering used everywhere phases are listed. */
export const PHASE_ORDER: Phase[] = [Phase.Pre, Phase.Release, Phase.Post];

export const phaseLabels: Record<Phase, string> = {
  [Phase.Pre]: 'Pre',
  [Phase.Release]: 'Release',
  [Phase.Post]: 'Post',
};
