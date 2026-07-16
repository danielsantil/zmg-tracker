# ZMG Release Tracker — Build Plan v2.2 (UX improvements)

Delta on top of [build-plan-2.1.md](build-plan-2.1.md). Continues milestone numbering from M18 → **M19–M23**.

## Context

Four post-2.1 UX improvements, all mobile-first:

1. **Artists page is inconsistent and its delete flow is clumsy.** It's a hand-rolled `divide-y` list with inline Edit/Delete buttons (every other list is a bordered table); editing toggles an inline form; delete is a blind confirm whose server-side "can't delete — has releases/songs" guard only surfaces *after* the attempt as an error toast. → a real table (Name · Releases · Songs · Actions), a delete modal that checks counts *up front*, and dedicated create/edit **pages** (mirroring the `/catalog/new` → detail song flow) so future artist fields have room.
2. **Item actions are inline buttons in most places.** The Templates rows already use the shared `RowMenu`/`MenuItem` kebab (colour-coded tones). → the same kebab on the Releases table, Catalog table, Artists table, and the Home cards.
3. **Releases has no time-oriented view.** → a **Table / Calendar** toggle; the calendar is a month grid of releases with a compact preview modal that reuses a new compact release card (which also replaces the taller Home cards).
4. **Checklist reorder is buried in the kebab.** Release-detail and template task rows reorder via "Move up/Move down" *menu items*, while the tracklist already uses inline ↑/↓ arrows. → standardize on inline arrows for checklist rows too.

**Design decisions locked with the user (mockups reviewed):**
- Calendar = **one responsive month grid at all sizes**; on mobile, day cells shrink and show **colored dots** instead of title chips.
- Empty months: calendar **opens on today's month**; a **"Next release · <date>" chip** jumps to the nearest upcoming release and is **hidden when nothing is upcoming**. No "no releases this month" text — the empty grid is self-evident.
- Calendar built **from scratch** — dependency-free, native `Date` helpers. (Surveyed react-big-calendar / FullCalendar / ilamy / DayFlow / react-day-picker: all are heavy schedulers shipping their own CSS/DnD, or date-pickers awkward for multi-event days; fighting them into the `ink/panel/edge/accent` theme costs more than a ~150-line grid, and the repo has a history of npm optional-binding breakage — oxlint, Vite pinned to 7.x.)
- Compact card **replaces the Home cards** (Home keeps its cover image) and is **reused** in the calendar preview (no cover there).

Tech context: React 19 + TS + Tailwind SPA under `src/Zmg.Web/`. Reuse `RowMenu`/`MenuItem` (tones `default|danger|archive`), `Modal`/`useConfirm`/`ConfirmDialog`, `StatusBadge`/`TypeBadge`/`ProgressBar`/`SoftWarning`, `Field`/`inputClass`, `useBackNavigation`. `scope=all` already returns **all non-archived releases regardless of date** (`ReleaseService.ListAsync`), so the calendar needs **no backend change**. Enums serialize as ints (mirror both sides).

---

## M19 — Artists redesign (table · smart delete · dedicated pages)

### Backend — song count + get-by-id (full slice)
- `Contracts/Dtos.cs`: `ArtistDto(Guid Id, string Name, string? Notes, int ReleaseCount, int SongCount)`.
- `Services/ArtistService.cs`: `ListAsync` projects `a.Songs.Count` into `SongCount`; fix the `new ArtistDto(...)` sites (`CreateAsync` → `0, 0`; `UpdateAsync` → count songs too). Add `GetAsync(Guid id)` → `ArtistDto` (+ `IArtistService`).
- `Endpoints/ArtistEndpoints.cs`: `MapGet("/{id:guid}")` → `GetAsync`, `.ToOk()`/NotFound.
- `api/artists.ts`: `get: (id) => request<Artist>(\`/api/artists/${id}\`)`. `types/artist.ts`: add `songCount: number`.

### Frontend — table + kebab (`features/artists/ArtistsPage.tsx`)
- Bordered `<table>` matching Catalog/Releases. Columns **Name · Releases · Songs · Actions**. Name = `<Link to={/artists/:id}>`; row click-navigates to the edit page; kebab wrapper stops propagation. Actions = `<RowMenu>` with a **Delete** `MenuItem tone="danger"`.
- Header **+ New artist** → `navigate('/artists/new')`.

### Frontend — smart delete modal (no post-hoc toast)
Row carries `releaseCount`/`songCount`, so branch *before* opening:
- `releaseCount + songCount > 0` → **info** modal: `confirm({ title, body, confirmLabel: 'OK', hideCancel: true })`, result ignored.
- else → **confirm** modal: `confirm({ title, body, confirmLabel: 'Delete', confirmVariant: 'danger' })` → `api.artists.delete`. Catch→error-toast kept only as a concurrency safety net.
- **Primitive tweak:** add optional `hideCancel?: boolean` to `ConfirmOptions` (`components/ConfirmDialog.tsx`) — renders only the confirm button.

### Frontend — dedicated create/edit pages
- New `features/artists/ArtistFormPage.tsx` (mirrors `SongFormPage`): `mx-auto max-w-xl`, handles create (no `:id`) and edit (`useParams().id` → `api.artists.get`, prefill). Name (autofocus) + Notes. Save: create → `api.artists.create` then `/artists`; edit → `api.artists.update` then back. Cancel → `useBackNavigation()`.
- Routes (`App.tsx`): `/artists/new`, `/artists/:id` → `ArtistFormPage`. Remove the inline-form branch; **delete `components/ArtistForm.tsx`**.

### Tests
Artist API: list returns `songCount`; GET-by-id 200/404. (Domain delete-guard already covered.)

---

## M20 — Kebab menus on the Releases & Catalog tables

Replace inline action `Button`s with `<RowMenu>` (rows are click-navigable → menu wrapper stops propagation):
- **`AllReleasesPage.tsx`**: `<RowMenu>` with **Edit** (`/releases/:id/edit`) + **Archive** (`tone="archive"`, only when `releaseDate >= today`).
- **`CatalogPage.tsx`**: `<RowMenu>` with **Delete** (`tone="danger"`, `isOrphan`) or **Archive** (`tone="archive"`, `canArchive`); when neither applies, render the `—` placeholder (no kebab).
- Existing `useConfirm` flows unchanged — only the trigger moves into the menu.

---

## M21 — Compact release card (replaces Home cards, reused by the calendar)

New **`features/releases/components/ReleaseCard.tsx`** (delete the old `features/home/components/ReleaseCard.tsx`).
- Props `{ r: ReleaseListItem; onOpen; onEdit; onArchive; showCover?: boolean }`.
- Layout: title (+`SoftWarning`) with a right cluster of `StatusBadge` + **`RowMenu`** (Edit + Archive-when-upcoming); artist sub-line; meta row (`TypeBadge` · date · accent countdown via `formatCountdown`); slim `ProgressBar` + "X / Y tasks". Cover (`aspect-[16/9]`) only when `showCover`.
- **Home:** pass `showCover`; grid unchanged, kebab replaces the button pair.
- Exported for the calendar preview (M22), rendered with `showCover={false}` + an "Open release →" affordance.

---

## M22 — Releases calendar view

### Toggle (`AllReleasesPage.tsx`)
Below the filters, left-aligned to the table: a Table/Calendar segmented control. Local `view` state (default `'table'`). `calendar` → `<ReleaseCalendar releases={releases} onOpen … onArchived={loadReleases} />`, consuming the **already-filtered** `scope=all` list. No new fetch.

### New `features/releases/components/ReleaseCalendar.tsx` (hand-rolled)
- **Date helpers** `lib/calendar.ts`: `addMonths`, `monthLabel`, `monthGrid(year, month) → string[][]` of `yyyy-MM-dd` cells (6×7, adjacent-month fill). **Timezone caution:** format cells by manual `yyyy-MM-dd` string building and group releases by their raw `releaseDate` string — never `new Date('yyyy-MM-dd')` comparisons (UTC drift); mirror `todayIso()`.
- **Header:** ‹ prev / label / next ›, **Today** button, and the **Next release chip** (nearest `releaseDate >= todayIso()`, jumps to its month, hidden when none upcoming).
- **Grid:** weekday header + 6 rows. Day cell shows day number (today highlighted, out-of-month dimmed) and its releases — `≥sm`: up to ~2 type-tinted title chips + "+N more"; mobile: up to 3 colored dots (`hidden sm:flex` chips / `sm:hidden` dots).
- **Preview modal (`Modal`):** clicking a chip / dot-day / "+N more" lists that day's releases as compact `ReleaseCard`s (`showCover={false}`) → "Open release →" routes to detail; Archive reuses `archiveReleaseConfirm` + refreshes.

---

## M23 — Inline reorder arrows on checklist rows (replace kebab Move up/down)

`TaskRow.tsx` and `TemplateTaskRow.tsx` already receive `isFirst`/`isLast`/`onMove` — only the trigger changes:
- **Remove** the `Move up`/`Move down` `MenuItem`s from each `RowMenu` (kebab keeps Rename / timeframe / move-phase / Delete).
- **Add inline ↑/↓** before the kebab, mirroring the Tracklist (`Tracklist.tsx:117–136`): `↑` disabled `isFirst`, `↓` disabled `isLast`, `px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30`.
- **Spacing:** arrows in a `flex shrink-0 items-center gap-1` group; kebab wrapped in `<div className="ml-3">` — same gap the tracklist puts between `↓` and `✕`. In `TaskRow`, arrows stay gated behind `!readOnly`.
- Extract shared **`components/ReorderArrows.tsx`** (`{ isFirst, isLast, onMove }`); refactor `Tracklist` to use it too.

---

## Files at a glance

**New:** `features/artists/ArtistFormPage.tsx`, `features/releases/components/ReleaseCard.tsx`, `features/releases/components/ReleaseCalendar.tsx`, `lib/calendar.ts`, `components/ReorderArrows.tsx`.
**Modified (backend):** `Contracts/Dtos.cs`, `Services/ArtistService.cs`, `Services/Interfaces/IArtistService.cs`, `Endpoints/ArtistEndpoints.cs`, `tests/Zmg.Api.Tests`.
**Modified (web):** `api/artists.ts`, `types/artist.ts`, `App.tsx`, `features/artists/ArtistsPage.tsx`, `features/releases/AllReleasesPage.tsx`, `features/catalog/CatalogPage.tsx`, `features/home/HomePage.tsx`, `components/ConfirmDialog.tsx`, `components/index.ts`, `features/releases/components/TaskRow.tsx`, `features/templates/components/TemplateTaskRow.tsx`, `features/releases/components/Tracklist.tsx`.
**Removed:** `features/artists/components/ArtistForm.tsx`, `features/home/components/ReleaseCard.tsx`.

---

## Verification

Run the app and drive each flow at desktop and 375px mobile:

1. **Artists table:** Name · Releases · Songs · Actions with correct counts; row/name → edit page (prefilled), save → list updated; **+ New artist** → `/artists/new` → create. Delete kebab: artist with dependents → info modal (single OK, no delete); clean artist → red Delete confirm → removed. No error toast on the blocked path.
2. **Kebabs:** Releases (Edit + upcoming-only Archive) and Catalog (Delete/Archive) rows use `⋮`; row-click nav still works; tones correct.
3. **Compact Home cards:** cover still shown, tighter, Edit/Archive in the kebab; grid intact on mobile.
4. **Calendar:** toggle flips table↔grid; opens on today's month; prev/next/Today; "Next release" chip appears/jumps and is absent when nothing upcoming; a 2-release day shows both (chips desktop / dots mobile); click → preview modal → Open release; Archive from preview works; filters constrain the calendar.
5. **Checklist reorder:** release detail *and* template rows show inline ↑/↓ (disabled at ends); kebab no longer lists Move up/down; ↓-to-kebab gap matches the tracklist; read-only archived detail shows no arrows.
6. `dotnet test` green; `npm run lint` + `npm run build` clean.
