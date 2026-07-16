import type { TrackInput } from '@/types';

// Seed one empty new-track row (used by the single's fixed row and the release form's initial state).
// Lives apart from TracksEditor so that component file only exports components (Fast Refresh hygiene).
export function emptyTrack(): TrackInput {
  return { songId: null, title: '', isrc: null, artists: [] };
}
