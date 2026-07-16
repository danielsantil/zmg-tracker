# ZMG Release Tracker — Build Plan v2.1 (UX refinements)

Delta on top of [build-plan-2.0.md](build-plan-2.0.md). Continues milestone numbering from M15 → **M16–M18**.

## Context

Four rough edges surfaced after the v2.0 catalog work. None are bugs — they're UX inconsistencies that make the app feel less finished and less mobile-friendly:

1. **Song picker can't help when you forget a title.** `SongPicker` only searches by typed title text and searches the *whole* catalog. If you don't remember the name you're stuck, and it surfaces songs by artists other than the release's main artist.
2. **Two visually different track pickers.** The create form (`TracksEditor`) and the release-detail tracklist (`TrackList`) render tracks, reordering, and the add-existing affordance differently (inline ↑/↓/✕ vs a kebab menu; per-row picker vs a bottom picker), even though both already call the same `SongPicker`.
3. **The "Saved." confirmation looks like an error.** It's actually the `Toast` component, whose only style is hardcoded `bg-red-500/90`. A red pop-up after a successful save reads as failure.
4. **Confirmations use the native `window.confirm`/`alert`.** ~13 call sites. They look unbranded and inconsistent on mobile.

**Design decisions locked with the user:** reorder standardizes on **↑/↓ arrow buttons** everywhere; the song picker becomes a **full sheet/modal** (bottom sheet on mobile, centered card on desktop), always scoped to the release's main artist.

**Through-line:** items 1, 2 and 4 all want the same missing primitive — a real overlay. So the first deliverable is a shared `Modal`, and the picker sheet + confirm dialog both build on it.

Tech context: React 19 + TS + Tailwind SPA under `src/Zmg.Web/`, hand-rolled primitives in `src/Zmg.Web/src/components/` (barrel `index.ts`). Theme tokens: `ink`/`panel`/`edge`/`accent` + slate text. The songs API **already supports** `artistId` filtering end-to-end (`api.songs.list({ artistId })` → `SongEndpoints.cs` → `SongService.cs` `s.MainArtistId == aid`), so **no backend change is needed**.

---

## M16 — Shared `Modal` primitive + custom confirm dialog

### New: `components/Modal.tsx`
- Renders via `createPortal(…, document.body)`; `role="dialog"` + `aria-modal`.
- Backdrop `fixed inset-0 z-40 bg-black/50`; Escape and backdrop-click call `onClose`.
- Responsive shell: full-width bottom sheet on mobile, centered card on desktop —
  `fixed inset-x-0 bottom-0 sm:inset-auto sm:top-1/2 sm:left-1/2 sm:-translate-x-1/2 sm:-translate-y-1/2 rounded-t-2xl sm:rounded-2xl border border-edge bg-panel …`.
- Focus the panel on open; body scroll-lock while open. Props: `{ open, onClose, title?, children }`. Export from `components/index.ts`.

### New: `components/ConfirmDialog.tsx` + `hooks/useConfirm.tsx`
- `ConfirmProvider` mounted once at the app root (`App.tsx`), holding a single `<ConfirmDialog>` built on `Modal`.
- `useConfirm()` returns `confirm(opts) => Promise<boolean>` where
  `opts = { title, body?: ReactNode, confirmLabel?='Confirm', cancelLabel?='Cancel', confirmVariant?: 'primary' | 'danger' | 'archive' }`.
- Layout: title, body, then `Cancel` (`Button variant="ghost"`) + confirm (`Button variant={confirmVariant}`). Enter = confirm, Escape/backdrop = cancel.

### New Button variant: `archive` (amber) — standardized app-wide
- `components/Button.tsx` gains an `archive` variant using amber (`bg-amber-500/15 text-amber-300 ring-1 ring-amber-500/30 hover:bg-amber-500/25`), parallel to `danger` but clearly not red. Archiving is terminal but distinct from a hard delete; red stays reserved for delete.
- **Replace every existing Archive button/action** with this variant: Home card archive, `AllReleasesPage`, `ReleaseDetailPage` archive, `CatalogPage` archive, and the archive confirm dialog (`confirmVariant: 'archive'`). Hard-delete confirms keep `danger` (red).

### Replace every native dialog
- All `window.confirm(...)` → `if (!(await confirm({...}))) return;`. Files: `HomePage.tsx`, `AllReleasesPage.tsx`, `ArchivedReleasesPage.tsx`, `ReleaseDetailPage.tsx` (task delete, track remove, archive), `CatalogPage.tsx`, `ArchivedSongsPage.tsx`, `TemplatesPage.tsx`, `ArtistsPage.tsx`. Delete confirms pass `confirmVariant: 'danger'`; archive confirms pass `confirmVariant: 'archive'`.
- `archiveReleaseConfirmMessage` refactored to return a `ReactNode` `body` (heading + a bulleted `<ul>` of cascade-archived songs) instead of a `\n`-joined string.
- All `window.alert(...)` failure paths → the **error toast** from M17 (`showToast(msg, 'error')`).

---

## M17 — Toast variants (fixes the red "Saved.")

- `Toast` gains `variant: 'success' | 'error' | 'info'` (default `error`, preserving today's callers): success = `bg-emerald-600/90` with a ✓ glyph, error = current red, info = slate. Keep fixed bottom-center; add `mb-[env(safe-area-inset-bottom)]` and a subtle slide-in.
- `useToast`: `showToast(msg, variant='error')`; store `{ message, variant }`.
- `SongDetailPage` save success → `showToast('Saved.', 'success')`. Other `showToast(...)` revert/error calls stay red by default; `alert→toast` replacements pass `'error'`.

---

## M18 — `SongPickerModal` + unified `Tracklist` component

### New: `features/catalog/components/SongPickerModal.tsx`
Replaces the inline `SongPicker`, built on `Modal`. Props: `{ open, mainArtistId, excludeIds, onSelect, onClose }`.
- **Browse-on-open:** immediately loads `api.songs.list({ artistId: mainArtistId })` — the main artist's songs, no typing required.
- **Search filters within that scope:** debounced `api.songs.list({ q, artistId: mainArtistId })` (250ms). Always artist-scoped.
- Client-side `excludeIds` filter unchanged. Rows show title + secondary line (release date / ISRC). Empty states: "No songs by this artist yet" / "No matches."
- `SongPicker.tsx` removed once both call sites migrate.

### New: `features/releases/components/Tracklist.tsx` (one component, both contexts)
- Props: `{ tracks, readOnly, onMove(t,dir), onToggleFocus(t), onRemove(t), onAddNew(title), onAddExisting(song), mainArtistId, excludeIds }`.
- Row: track number, title, focus star, and (when editable) **↑/↓ arrows + ✕**. The detail page's kebab menu is retired. Add a small margin (~`ml-3`) between ↓ and ✕.
- Add area: `InlineAddForm` (new song by title) **and** an "Add existing song" button opening `SongPickerModal`.
- Two thin adapters keep existing persistence:
  - **Create form** (`TracksEditor`): callbacks mutate local `EditorRow[]` and emit `TrackInput[]`. Optional per-row new-song details (ISRC / feats) stay create-only, as a collapsible "Details (optional)" disclosure.
  - **Detail page** (`ReleaseDetailPage`/`TrackList`): callbacks call `api.tracks.*` optimistically as today.

---

## Files at a glance

**New:** `components/Modal.tsx`, `components/ConfirmDialog.tsx`, `hooks/useConfirm.tsx`, `features/catalog/components/SongPickerModal.tsx`, `features/releases/components/Tracklist.tsx`.
**Modified:** `components/Toast.tsx`, `components/Button.tsx`, `hooks/useToast.ts`, `components/index.ts`, `App.tsx`, `features/releases/components/TracksEditor.tsx`, `TrackList.tsx` + `TrackRow.tsx`, `ReleaseDetailPage.tsx`, `SongDetailPage.tsx`, `archiveConfirm.ts`, and every `window.confirm`/`alert` call site.
**Removed:** `SongPicker.tsx`; `TrackRow`'s kebab reorder menu.
**Backend:** none.

---

## Verification

Run the app and drive each flow at desktop and mobile widths:

1. **Picker / forgot-title:** create form + detail page → "Add existing song" sheet lists the main artist's songs with no typing; other artists never appear; typing filters; selecting adds the track. Bottom sheet on mobile, centered card on desktop.
2. **Unified tracklist:** create form and detail page show identical rows and ↑/↓ reorder; no kebab; reorder/focus/remove behave as before.
3. **Saved toast:** edit a song, Save → green ✓ "Saved." toast, auto-dismiss ~3s; error path → still red.
4. **Confirm dialog:** delete and archive (with cascade songs) → custom dialog, red Delete / amber Archive, cascade list rendered; Cancel/Escape/backdrop abort; forced failure → red error toast.
5. `npm run build` / oxlint clean; no `window.confirm`/`window.alert` remain.
