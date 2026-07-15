# Progress / Handoff

A running journal: what shipped per version, decisions that *aren't* captured in a build plan,
and what's next. Read the **build plans** for scope, rationale, wireframes, and per-milestone test
lists; read **this** for current state and the cross-cutting knowledge the plans can't carry.

**Plan versions**
- [build-plan-1.0.md](build-plan-1.0.md) — frozen v1 brief (M0–M5). Shipped.
- [build-plan-1.1.md](build-plan-1.1.md) — singles improvements (M6–M10). Shipped.
- [build-plan-1.2.md](build-plan-1.2.md) — archived releases (M11). Shipped.
- [build-plan-2.0.md](build-plan-2.0.md) — songs & catalog (M12–M15). **M12–M15 shipped — v2.0 complete.**

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** v2.0 **M15** done — **build-plan-2.0 fully shipped**. Song archive lifecycle with a
release-archive cascade and the Catalog archive pages, on top of M14 (pending rework), M13 (catalog) and
M12 (song data model). Tests green (`dotnet test` — domain 56 / API 87). Next work is the
[Backlog / next steps](#backlog--next-steps) (Phase 2 — DSP stats).

> ⚠️ **M12 is a hard schema reset with no migration.** Any existing local `src/Zmg.Api/zmg.db` from
> v1.x must be deleted before running — the fresh `InitialCreate` won't apply on top of the old schema.
> Delete it (`rm src/Zmg.Api/zmg.db*`) and startup will recreate a seeded db.

---

## Journal

**v1 (M0–M5) — foundation.** Domain (entities, template-copy, progress, derived status, validation,
seed), the minimal API + EF/SQLite with seeded templates, and the React SPA (dashboard, artists,
release form + detail checklist, templates editor, album tracklist). M5 was polish: 375px mobile
pass, filters, empty states, and the multi-stage Dockerfile.

**v1.1 (M6–M10) — singles improvements.** UPC/ISRC + the soft "missing identifier" warning (only
after *Distribute to DSPs* is checked); per-task timeframes (`MinDaysBefore`/`MaxDaysBefore`, max
drives calc, Pre-only) + surfaced notes; the dashboard split into **Home** (`scope=home`,
forward-looking) and **All Releases** (`scope=all` table); and the **pending-actions** engine
(`PendingActions.Compute` → `GET /api/pending` + a detail "Needs attention" block). The single
template grew 30 → **31** (Distribute inserted as 3rd Pre); album unchanged at **40**.

**v1.2 (M11) — archived lifecycle.** `Archived` release status + a soft-delete ("Remove"):
`ArchivedAt`/`DeletedAt` on Release, `POST /api/releases/{id}/archive` (guarded to `releaseDate >=
today`, not-twice), `DELETE /api/releases/{id}` repurposed to a guarded soft-delete (archived only),
`scope=archived`, the `/releases/archived` page (linked from All Releases, not a nav item), Archive
on Home cards + All Releases rows, and a read-only archived detail. Verified end-to-end via the
running API + browser SPA.

**Post-M10 fix:** the soft UPC/ISRC warning was a hover-only `title` tooltip (dead on touch); it's
now a tappable button with a dismissable popover ([IdentifierWarning.tsx](src/Zmg.Web/src/components/IdentifierWarning.tsx)).

**v2.0 (M12) — Song data model, hard schema reset & backend rebuild.** A first-class **Song**
([Song.cs](src/Zmg.Domain/Entities/Song.cs)) — title, main artist (copied from the release at
inline creation, then independent), ISRC, feats/collabs ([SongArtist.cs](src/Zmg.Domain/Entities/SongArtist.cs),
replaces the deleted `ReleaseArtist`), and its own `ArchivedAt`/`DeletedAt` columns (lifecycle wired
in M15). **`Track` became a pure Release↔Song join** with composite PK `(ReleaseId, SongId)` — no
surrogate id, structurally blocks a song twice on one release, endpoints re-keyed to
`/api/releases/{releaseId}/tracks/{songId}` (the old `/api/tracks/{id}` group is gone). **Release lost
`Isrc` and `FeaturedArtists`**; `NeedsWarning` is UPC-only. `CreateAsync` now materialises the inline
Tracks section (new inline songs and/or existing catalog songs); the create form's **Tracks section**
([TracksEditor.tsx](src/Zmg.Web/src/features/releases/components/TracksEditor.tsx)) ships new-track
rows only (existing-song picker is M13). Auto-distribute simplified to **past-date-only** (identifiers
no longer imply distribution). Verified end-to-end via the running API + browser SPA.

**v2.0 (M13) — Catalog.** New `SongService`/`SongEndpoints` (`GET /api/songs?q=&scope=`,
`GET/PUT /api/songs/{id}`): list ordered by title with a **derived** release date (earliest
non-archived link, null for orphans), detail exposing every linked release so the page derives the
UPC list, and always-editable song fields (title / main artist / ISRC / feats-collabs; a rename
clashing with an active same-artist song returns the non-blocking warning). Frontend: **Catalog** nav
+ `/catalog` list + `/catalog/:id` detail
([SongDetailPage](src/Zmg.Web/src/features/catalog/SongDetailPage.tsx)) with the **artist-drift hint**
(song's main artist ≠ a linked release's — informational, never blocks). Shared
[SongArtistsEditor](src/Zmg.Web/src/features/catalog/components/SongArtistsEditor.tsx) (extracted from
the release-form block) and [SongPicker](src/Zmg.Web/src/features/catalog/components/SongPicker.tsx)
(debounced catalog search) — the picker is wired into the create-form Tracks section (per-row
"New | From catalog") and the album add-row on the release detail. Track rows link into the catalog;
a **single** swaps its one-row tracklist for a compact
[SongCard](src/Zmg.Web/src/features/releases/components/SongCard.tsx). Verified end-to-end
(list/detail/edit, double-link UPCs, drift hint, existing-song linking, single card) via API + SPA.

**v2.0 (M14) — Pending actions rework.** `PendingKind.MissingIdentifier` split into **`MissingUpc`**
(release-owned) + **`MissingIsrc`** (song-owned), plus a new **`EmptyAlbum`** kind (every non-archived
album with < 2 tracks — released ones included, label "Album is empty" / "…only 1 track"). `PendingAction`
now carries nullable `ReleaseId`/`SongId` and a generic `Subject` (release title *or* song title); the old
`ReleaseTitle` is gone. New pure [`PendingActions.ComputeForSong`](src/Zmg.Domain/PendingActions.cs) —
a song is "distributed" (→ needs ISRC) when **any** linked non-archived release has its Distribute-to-DSPs
task checked, yielding **one action per song**, not per release. [`PendingService`](src/Zmg.Api/Services/PendingService.cs)
adds a second query over active songs for the ISRC flag; `ListByReleaseIdAsync` rolls up its own songs'
`MissingIsrc` rows. Frontend: [`PendingSection`](src/Zmg.Web/src/features/home/components/PendingSection.tsx)
became **collapsible** (PhaseSection-style) with a ~4-row `overflow-y-auto` scroll; per-kind routing
(`MissingIsrc → /catalog/{songId}`, `MissingUpc → …/edit`, `TaskDue`/`EmptyAlbum → /releases/{id}`) in
both `PendingSection` and `NeedsAttention`. Verified via the full test suite (domain 50 / API 74 — the API
tests run the real app + EF `Migrate()`) + `tsc`/`vite build`; a live `dotnet run` smoke test is blocked
by the documented EF-tooling migration issue (see below), not by this change.

**v2.0 (M15) — Archive cascade & Catalog Archive.** Songs got the release lifecycle verbatim
(`Active → Archived (terminal, read-only) → Removed`). New pure
[`SongArchival.ShouldArchive`](src/Zmg.Domain/SongArchival.cs) (unit-tested without EF) drives the
**release-archive cascade**: [`ReleaseService.ArchiveAsync`](src/Zmg.Api/Services/ReleaseService.cs) now
loads its tracks' songs (+ their links) and archives each song that is dormant — not already archived,
never released (no past-dated link), and with no remaining active link (every other link points to an
already-archived release). Released songs and songs shared with an active release stay put. Manual
[`POST /api/songs/{id}/archive`](src/Zmg.Api/Endpoints/SongEndpoints.cs) (409 on active-link / released /
already-archived — mostly orphans in practice) and soft-delete `DELETE /api/songs/{id}` (allowed iff
archived **or** orphan). `PUT /api/songs/{id}` now 409s when archived; `SongListItemDto` gained
**`CanArchive`/`IsOrphan`** (computed in the list projection) so the Catalog row action renders from
backend truth; `TrackDto` gained `IsSongArchived`. Frontend: Catalog "Archived Songs →" link + per-row
Archive/Delete action, new [`ArchivedSongsPage`](src/Zmg.Web/src/features/catalog/ArchivedSongsPage.tsx)
(`/catalog/archived`, registered before `:id`), archived [`SongDetailPage`](src/Zmg.Web/src/features/catalog/SongDetailPage.tsx)
is read-only (fields disabled, Save replaced by a note, release links stay live), and archived-song track
rows show a badge. Verified via the full suite (domain 56 / API 87 — API tests run the real app + EF
`Migrate()`, incl. the cascade end-to-end) + `tsc`/`vite build`; a live `dotnet run` smoke test is blocked
by the documented EF-tooling migration issue below, not by this change.

---

## Cross-cutting decisions (not in any single plan)

- **Status is derived, never stored** — recomputed from tasks + date on every read. The **one**
  exception is `Archived` (v1.2), a persisted flag that overrides the derived value.
- **Soft-delete, never hard-delete** (v1.2). Removed releases are stamped `DeletedAt` and hidden by a
  global query filter (`HasQueryFilter(r => r.DeletedAt == null)`) so stable ids survive for phase-2
  stats. EF logs a benign "required end of a relationship" advisory for the child navs.
- **Template-copy-on-create is backend logic, wired since v1** — a release is born with a full
  snapshot checklist. Editing a template never touches existing releases (covered by
  `TemplateApiTests.Editing_a_template_does_not_touch_existing_releases`).
- **Reorder is move-up/move-down, not drag-and-drop.** The order endpoint takes the full ordered id
  list for a phase; the UI posts a single-swap result. Dependency-free, mobile-friendly; the endpoint
  already supports arbitrary orderings if DnD is added later.
- **Mutations return the single changed DTO** (or 204); the detail screen holds a flat task array and
  recomputes phase groups + progress client-side, so no re-fetch. Moving a task across phases appends
  to the target (`SortOrder = max+1`).
- **Tracks key off `TrackNumber`** (1-based, contiguous) for both order and display; reorder rewrites
  it, delete renumbers survivors. Tracklist is UI-gated to `type === Album` (endpoints aren't hard-scoped).
- **Create/update release responses are `{ data, warnings }`** (`CreatedWithWarnings<T>`) so
  non-blocking validation warnings reach the form.
- **Enums serialize as integers** (System.Text.Json default); the TS layer mirrors (`ReleaseType.Single
  = 0`, …). Change both sides together. `erasableSyntaxOnly` is disabled in `tsconfig.app.json` so TS
  `enum`s compile.
- **Web is organized by feature folder** (`src/Zmg.Web/src/features/{home,releases,artists,templates}`),
  not flat `pages/` — an earlier refactor (#1). Shared UI in `components/`, API client in `api/`,
  types in `types/`.
- **Song vs Release (v2.0).** A **Song** is the creative work (title, ISRC, feats/collabs, main
  artist); a **Release** is the commercial package (UPC, cover, tasks). They link through `Track`, so
  one song can sit on a single *and* an album. A song's **UPCs and release date are derived** from its
  links, never stored. **Type is fixed at create** (determines the checklist) and PUT rejects a change
  with 409. A single is fixed at one track; an album has zero+. Song fields are edited only on the
  catalog detail page (M13) — release-detail track rows just reorder/focus/remove.
- **Hard schema reset, no migration (v2.0).** M12 deleted all v1.x migrations and regenerated a single
  `InitialCreate`; there is intentionally **no upgrade path** from a v1.x db. Delete any local `zmg.db`.
  Template/task `HasData` seeding carried over unchanged (proven by the green seed tests).
- **EF tooling must match the runtime (EF 8).** v2.0 needs no further migrations, so nothing is pinned
  in-repo. **If** you ever regenerate one, use a `dotnet-ef` on the 8.x line — a 10.x-generated
  migration builds fine but silently fails at runtime (`no such table: __EFMigrationsHistory` on the
  history insert). Install matching tooling first, e.g. `dotnet tool install --global dotnet-ef --version 8.0.11`.
- **Track query filter (v2.0).** A join between two soft-filtered entities needs its own filter
  (`t.Release!.DeletedAt == null && t.Song!.DeletedAt == null`) so a stale join vanishes with either
  parent; it also silences EF's required-nav-to-filtered-entity advisory for `Track`. The same benign
  advisory still logs for `Release→ReleaseTask` and now `Song→SongArtist` (expected).

## Run

```bash
# dev (two terminals)
dotnet run --project src/Zmg.Api                 # API on :5274 (default profile)
cd src/Zmg.Web && npm install && npm run dev     # SPA on :5173, proxies /api → :5274

# prod-style (one process)
cd src/Zmg.Web && npm run build                  # → src/Zmg.Api/wwwroot
dotnet run --project src/Zmg.Api                 # serves SPA + API together

dotnet test                                      # full suite
```

DB is `src/Zmg.Api/zmg.db` (git-ignored, migrated on startup). Delete it to reset.

## Project layout

```
src/Zmg.Domain   entities/enums, template-copy, progress, status, validation, seed,
                 identifier-state, pending-actions  (pure, no I/O)
src/Zmg.Api      minimal API: endpoints, service layer (+ interfaces), DTO contracts, extensions
src/Zmg.Infra    EF Core + SQLite: ZmgDbContext (seeding) + migrations
src/Zmg.Web      React + Vite + Tailwind SPA, organized by feature folder
tests/Zmg.Domain.Tests   xUnit unit tests
tests/Zmg.Api.Tests      integration tests (WebApplicationFactory + in-memory SQLite)
```

---

## Backlog / next steps

- **v2.0 is fully shipped (M12–M15).** See the journal above and [build-plan-2.0.md](build-plan-2.0.md).
- **Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data off
  the stable Artist / Release / **Song** / Track ids and UPC/ISRC columns. The v2.0 Song ids are its
  foundation.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track"
  tasks today. Decide after the first real album.
- **Verify the Docker image** on a machine with the daemon running (`docker build -t zmg-tracker .`) —
  written and reviewed, never built here (daemon was down).
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*).

**Env note:** `npm run lint` fails locally — oxlint's native binding (`oxlint.darwin-universal.node`)
is missing, same class as the Vite 8 rolldown binding (Vite is pinned to 7.x for that reason). Use
`tsc --noEmit` + `npm run build` to typecheck until it resolves.
