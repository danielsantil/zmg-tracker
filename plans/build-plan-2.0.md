# ZMG Release Tracker — Build Plan v2.0 (Songs & Catalog)

Delta on top of [build-plan-1.2.md](build-plan-1.2.md) (and 1.0/1.1 below it) — read those for anything not restated. Continues milestone numbering from M11, covering **M12–M15**.

**Scope:** a breaking data-model revamp — a first-class **Song** entity (title, main artist, ISRC, feats/collabs) linked to releases through a rewritten `Track` join; a **Catalog** section (list + detail + archive); release creation with an inline Tracks section (new track or existing song); pending actions split by identifier owner (UPC→release, ISRC→song) plus an empty-album nudge; and a song archive lifecycle cascaded from release archiving. **Hard schema reset: no data migration** — existing migrations and the dev `zmg.db` are deleted and a fresh `InitialCreate` is generated (template seeds kept via `HasData`).

**Milestone map:**

- **M12 — Song data model, hard schema reset & backend rebuild.** `Song` + `SongArtist` entities; `Track` becomes a Release↔Song join; `ReleaseArtist` and `Release.Isrc` deleted; fresh `InitialCreate`; `CreateAsync` accepts tracks (single = exactly one) with a simplified past-date-only auto-distribute; `TrackService` rewritten; create-form Tracks section (inline new tracks).
- **M13 — Catalog.** Songs API (list/detail/update/search), `/catalog` + `/catalog/{id}` pages, nav item, existing-song picker (create form + album track list), track rows link into the catalog, singles surface their one song.
- **M14 — Pending actions rework.** `MissingIdentifier` splits into `MissingUpc` (release) and `MissingIsrc` (song); new `EmptyAlbum` kind; routing per kind; PendingSection becomes collapsible (PhaseSection-style) and scrolls past 4 items.
- **M15 — Archive cascade & Catalog Archive.** Song `ArchivedAt`/`DeletedAt` lifecycle, release-archive cascades to exclusively-linked upcoming songs, song archive/delete endpoints (orphans skip archive), `/catalog/archived` page, read-only archived song detail.

---

## I. Concept — the new shape

```
Artist ──main──▶ Song ◀──SongArtist(Role)── Artist      (feats/collabs live on the SONG)
Release ◀──Track(ReleaseId, SongId, TrackNumber, IsFocusTrack)──▶ Song
```

- A **Song** is the creative work: `Title`, `MainArtistId` (copied from the release at inline creation, then independent and editable), optional `Isrc`, its own archive lifecycle. No cover, no tasks — those belong to releases.
- A **Release** is the commercial package: keeps `Upc` (and cover), **loses `Isrc` and `FeaturedArtists`**. Every release contains songs: a single exactly one, an album zero or more.
- **Track** is a pure join, so one song can sit on a single *and* later on an album. A song's **UPCs are derived** from its linked releases; its **release date is derived** (earliest linked non-archived release date; null for orphans).
- **Type is still explicitly chosen at create** (Single|Album) and determines the checklist template exactly as today. It cannot change afterwards. A single's one track is fixed at create; album tracks stay editable.
- Song fields (title / ISRC / feats-collabs / main artist) are edited **only on the catalog detail page**, always editable. Release-detail track rows reduce to reorder / focus / remove + a link into the catalog.

## II. Schema (target state, one fresh `InitialCreate`)

**`Song`** (new, `src/Zmg.Domain/Entities/Song.cs`):

```csharp
class Song {
  Guid Id; string Title;
  Guid MainArtistId; Artist? MainArtist;   // Restrict delete
  string? Isrc;                            // free text, same policy as Upc
  DateTime? ArchivedAt;                    // used by M15, column exists from M12
  DateTime? DeletedAt;                     // soft delete, global query filter
  List<SongArtist> Artists;                // feats/collabs
  List<Track> ReleaseLinks;
  bool IsArchived => ArchivedAt is not null;
}
```

**`SongArtist`** (new) — clone of today's `ReleaseArtist`: composite PK `(SongId, ArtistId)`, `ArtistRole Role` (reuse enum). SongId→Cascade, ArtistId→Restrict. **`ReleaseArtist.cs` is deleted.**

**`Track`** (rewritten as join, class name kept): `ReleaseId`, `SongId`, `TrackNumber` (1-based contiguous per release), `IsFocusTrack`. **Composite PK `(ReleaseId, SongId)`** — no surrogate Id; structurally prevents the same song twice on one release; track endpoints become addressable by `{releaseId}/tracks/{songId}`. `Title` gone (lives on Song). ReleaseId→Cascade, SongId→Restrict.

**`Release`**: delete `Isrc`, `FeaturedArtists`, `MissingLabel()`; `NeedsWarning` becomes UPC-only. Keep `Upc`, `Type`, `CoverUrl`, `ArchivedAt`, `DeletedAt`, `IsDistributed`, `Tasks`, `Tracks`.

**`Artist`**: drop `ReleaseCredits`; add `Songs` (main) + `SongCredits` navs.

**`ZmgDbContext`**: add `Songs`/`SongArtists` DbSets, drop `ReleaseArtists`; `HasQueryFilter(s => s.DeletedAt == null)` on Song; and on `Track` add `HasQueryFilter(t => t.Release!.DeletedAt == null && t.Song!.DeletedAt == null)` (a join between two filtered entities must vanish with either parent, and it silences the EF required-nav-to-filtered-entity warning). Template/task `HasData` seeding untouched.

**Hard reset procedure (M12, one commit):** delete `src/Zmg.Infra/Migrations/*` and the dev `zmg.db`; `dotnet ef migrations add InitialCreate -p src/Zmg.Infra -s src/Zmg.Api`. `Program.cs` `Migrate()` unchanged. Note in PROGRESS.md: any existing local db must be deleted.

---

## III. M12 — Song data model, hard schema reset & backend rebuild

The big-bang milestone: everything referencing `ReleaseArtist`, `Release.Isrc`, or `Track.Title` is touched, and the app must end the milestone working. The create form's Tracks section ships here with inline **new** tracks only; the existing-song picker needs the songs API and lands in M13.

**Domain:**

- Entities per §II.
- `Validation.cs`: `ValidateSong(title, …)` (title required); `ValidateReleaseTracks(type, tracks)` over `TrackSpec(Guid? ExistingSongId, string? NewTitle)` — errors: Single with count ≠ 1 ("A single must have exactly one track."); a spec with both or neither of song-id/title; duplicate song ids; blank titles. Pure — existence/archived checks stay in the service. `ValidateTrackTitle` folded in.
- **Duplicate-title warning** (non-blocking, same mechanism as the existing duplicate-release warning): no uniqueness is enforced (covers/re-records are legitimate), but a new-title track spec matching an existing song's title for the same main artist adds a warning ("A song with this title already exists for this artist — consider picking it from the catalog."). Applies on release create and (M13) song rename.
- `Release.NeedsWarning(distributed, upc)` — UPC only.

**API:**

- **DTOs (`Contracts/Dtos.cs`):** `SongArtistInput/Dto` replace `ReleaseArtistInput`/`FeaturedArtistDto`; `TrackInput(Guid? SongId, string? Title, string? Isrc, List<SongArtistInput>? Artists)` used by both the create form and the add-track endpoint (exactly one of SongId/Title); `ReleaseInput` −Isrc −FeaturedArtists +`Tracks` (create-only); `TrackDto(SongId, TrackNumber, Title, Isrc, IsFocusTrack, Artists)` projected from Song; `ReorderTracksInput(List<Guid> OrderedSongIds)`; `ReleaseListItemDto`/`ReleaseDetailDto` drop Isrc/featured.
- **`ReleaseService.CreateAsync`:** validate (incl. tracks) → resolve existing SongIds (400 unknown/deleted, 409 archived) → copy template by `input.Type` as today → create Songs for new-title specs (`MainArtistId` inherited from the release, ISRC cleaned, artists deduped minus main) → create `Track` rows in payload order → **simplified auto-distribute backfill**: check "Distribute to DSPs" only when `ReleaseDate < today` (the identifier branch is dropped — identifiers no longer imply distribution).
- **`ReleaseService.UpdateAsync`:** ignore `Tracks` on PUT (tracks mutate only via track endpoints); guard `input.Type != release.Type` → 409 ("Release type can't change after creation.").
- **`TrackService` rewrite** (song-id addressed, all under the release group):

```
POST   /api/releases/{releaseId}/tracks                201/400/404/409
PATCH  /api/releases/{releaseId}/tracks/{songId}/focus 200/404
PUT    /api/releases/{releaseId}/tracks/order          204/400/404
DELETE /api/releases/{releaseId}/tracks/{songId}       204/404/409
```

  Add: 409 on singles ("Singles carry exactly one track."), 409 if the song is already on the release or archived, new-song path inherits main artist. Delete: 409 on singles; removes the join only (the Song survives, possibly as an orphan) and renumbers. Track rename endpoint deleted (renames moved to catalog). The old `/api/tracks/{id}` group disappears.
- **`ArtistService`:** delete guard extends to songs (`MainArtistId` Restrict FK) — an artist who is a song's main artist can't be deleted.
- **Pending:** compile-fix only — `MissingIdentifier` temporarily reads UPC alone (full rework in M14).

**Frontend:**

- Types/api mirror the DTO changes (`types/release.ts`, `types/track.ts`, new `types/song.ts` stub; `api/tracks.ts` rewritten release-scoped).
- **`ReleaseFormPage.tsx`:** remove ISRC field and the Featured/Collab block; add a **Tracks section** (`components/TracksEditor.tsx`), create-only, between Notes and the buttons — rows of inline new tracks (Name, ISRC, feats/collabs with role, reusing the removed featured-artists UI), add/remove/reorder rows. **Single: exactly one fixed row; Album: zero+.** Client-side guards mirror the 400s.
- **`TrackList.tsx`/`TrackRow.tsx`:** drop rename; keep focus/move/delete; add/delete hidden for singles; quick-add stays title-only until M13.
- **`ReleaseDetailPage.tsx`:** handlers re-keyed on `songId`; TrackList renders for **both** types (singles: one row, no controls).
- `ReleaseHeader`/`IdentifierWarning`/`AllReleasesPage`: drop featured/ISRC displays; warning text "Missing UPC".

**Tests:**

- Domain: single needs exactly 1 track / album 0 ok / both-neither spec / dup ids; `NeedsWarning` UPC-only; seed tests still green (proves the reset kept the templates).
- API: create single with one inline track → 201 + Song row with inherited main artist/ISRC/feats; single with 0 or 2 tracks → 400; album with 0 tracks → 201; add/delete track on single → 409; same song twice on an album → 409; delete album track → 204 and the song survives as an orphan; reorder by songIds; type change on PUT → 409; auto-distribute (past date ✓ / future date ✗ even with UPC+ISRCs set); duplicate same-artist song title in a new-track spec → 201 with warning.

---

## IV. M13 — Catalog (songs API, pages, pickers)

**API — new `SongService`/`SongEndpoints`:**

```
GET /api/songs?q=&scope=all|archived   200
GET /api/songs/{id}                    200/404
PUT /api/songs/{id}                    200/400/404 (409 archived — M15)
```

- DTOs: `SongListItemDto(Id, Title, MainArtistId, MainArtistName, ReleaseDate?, Isrc, ReleaseCount, IsArchived)` — `ReleaseDate` = min over non-deleted, non-archived links, null for orphans/unreleased; `SongDetailDto(…, Artists, Releases: List<SongReleaseLinkDto(ReleaseId, Title, Type, ReleaseDate, Upc, IsArchived)>)` — the detail page's UPC list derives from these links; `SongUpdateInput(Title, MainArtistId, Isrc, Artists)`.
- `ListAsync`: `q` LIKE on title (same pattern as releases), order by title; orphans included by design. `GetAsync` includes links to archived releases (badged) — history stays readable. `UpdateAsync`: title/main-artist validated, artists replaced (dedup, exclude main), ISRC cleaned — **always editable** per the decided rule; a rename matching another song's title for the same main artist returns the non-blocking duplicate warning.

**Frontend:**

- **Nav + routes (`App.tsx`):** nav item **Catalog** after All Releases; `/catalog`, `/catalog/:id` (`/catalog/archived` registered before `:id` in M15).
- **`features/catalog/CatalogPage.tsx`:** table **Name | Main Artist | Release Date** (blank when null), title search box, rows → `/catalog/{id}`.
- **`features/catalog/SongDetailPage.tsx`:** editable Name / Main artist / ISRC / feats-collabs (shared `SongArtistsEditor.tsx` extracted from the old release-form block); read-only **Releases** section listing every linked release (type badge, date, UPC) each linking to `/releases/{id}` — covers the single-and-album double-link scenario; save via PUT. **Artist-drift hint:** when the song's main artist differs from any linked release's main artist, show a subtle informational note ("Main artist differs from release *{release title}* (Artist X)") — divergence is intentional (compilations, collab albums), so it never blocks and there is no reconciliation.
- **`SongPicker.tsx`** (shared): debounced `GET /api/songs?q=` search+select, excluding songs already on the release. Wired into **TracksEditor** (per-row "New track | From catalog" toggle) and **TrackList's** album add-row.
- **Track rows link** to `/catalog/{songId}`. **Singles on the release detail** swap the one-row TrackList for a compact Song card (title → catalog link, main artist, ISRC indicator).

**Tests:** list derives earliest date across two releases; orphan lists with null date; `q` filters; detail returns both release links with UPCs for a song released twice; update renames/sets ISRC/replaces artists; blank title → 400; unknown → 404; adding an existing catalog song to an album reflects the catalog title.

---

## V. M14 — Pending actions rework

**Domain (`PendingKind`, `PendingActions.cs`):**

- Kinds: `TaskDue`, **`MissingUpc`**, **`MissingIsrc`**, **`EmptyAlbum`** (replaces `MissingIdentifier`). `PendingAction` gains nullable `ReleaseId`/`SongId` and a generic `Subject`.
- `Compute(release, today)`: TaskDue unchanged; `MissingUpc` when `IsDistributed && Upc` blank; `EmptyAlbum` when `Type == Album && !IsArchived && Tracks.Count < 2` — label "Album is empty" (0) / "Album has only 1 track" (1). Applies to **every non-archived album, released ones included** — the nag persists until the tracks exist.
- `ComputeForSong(song, hasDistributedRelease)`: `MissingIsrc` when distributed, ISRC blank, not archived. **A song counts as distributed when any linked, non-deleted, non-archived release has its "Distribute to DSPs" task checked** — one action per song, not per release.
- `Order`: TaskDue first (nearest release date), then the data kinds by subject.

**API (`PendingService`):** release query includes `Tracks` (for EmptyAlbum); a second query over active songs computes the distributed flag per song via its links; merge + order. `ListByReleaseIdAsync` (detail-page `NeedsAttention`) adds `MissingIsrc` rows for that release's own songs — this is the decided **rolled-up "tracks missing ISRC" view** on the release detail, each row linking to `/catalog/{songId}`.

**Frontend:** `PendingSection`/`NeedsAttention` routing — `TaskDue` → `/releases/{id}`; `MissingUpc` → `/releases/{id}/edit` (current behavior kept); `MissingIsrc` → `/catalog/{songId}`; `EmptyAlbum` → `/releases/{id}`. Types updated.

**Frontend — PendingSection UX (`features/home/components/PendingSection.tsx`):**

- **Collapsible header, PhaseSection-style** (`features/releases/components/PhaseSection.tsx` is the pattern): the header row becomes a full-width toggle button with the `▾/▸` chevron; the count stays visible while collapsed ("Pending Tasks (N)"). Default open; plain `useState` like PhaseSection (no persistence).
- **4 items max, then scroll:** the `<ul>` gets a max-height equal to ~4 rows with `overflow-y-auto`, so the 5th+ actions scroll inside the section instead of growing the page.

**Tests:** UPC gating on distributed; song with blank ISRC + one distributed release → MissingIsrc; only undistributed/archived releases → nothing; orphan blank-ISRC song → nothing; song on two distributed releases → exactly one action; album 0/1/2 tracks → empty/only-1/nothing; archived album → nothing; single → never EmptyAlbum; ordering.

---

## VI. M15 — Archive cascade & Catalog Archive

Songs copy the release lifecycle verbatim (`Active → Archived (terminal, read-only) → Removed`), with two twists: archives can arrive **via cascade**, and **orphans skip the archive step** for deletion.

- **Cascade** (in `ReleaseService.ArchiveAsync`, after existing guards): archive each song on the release iff not already archived, **upcoming** (no linked release with a past date), and **no remaining active link** (every other link points to an already-archived release). Released songs untouched; songs shared with an active release untouched. Pure helper `SongArchival.ShouldArchive(…)` in `src/Zmg.Domain/SongArchival.cs`, unit-tested without EF.
- **Manual archive** `POST /api/songs/{id}/archive` — 404; 409 already archived; 409 any active release link ("Song is on an active release — archive flows through the release."); 409 released; else stamp, 204. (In practice this applies mostly to orphans; cascade covers the rest.)
- **Soft delete** `DELETE /api/songs/{id}` — 404; allowed iff **archived OR orphan** (no non-deleted release links); else 409; stamp `DeletedAt`, 204. The Track query filter (§II) hides stale join rows.
- Song `scope=archived` activates (`ArchivedAt desc`); default scope excludes archived. `PUT /api/songs/{id}` → 409 when archived. Pending already excludes archived songs (M14).
- `SongListItemDto` gains **`CanArchive`** and **`IsOrphan`** flags (computed in the list projection from the archive rules above) so the Catalog row actions render from backend truth instead of re-deriving it client-side.

**Frontend:**

- **`CatalogPage.tsx`:** always-visible **"Archived Songs →"** link above the table (exact `AllReleasesPage` pattern, reachable when empty); per-row Action cell driven by the DTO flags — **Archive** when `canArchive`, **Delete** when `isOrphan` (confirm: "This song was never released — delete it from the catalog? This can't be undone.").
- **`features/catalog/ArchivedSongsPage.tsx`** (`/catalog/archived`): table **Name | Main Artist | Action(Delete)** — clone of `ArchivedReleasesPage.tsx`.
- **`SongDetailPage.tsx`** archived → all fields disabled, "Archived — read only" note replaces Save; release links stay clickable.
- `api/songs.ts`: `archive(id)`, `delete(id)`, `scope`.
- **`TrackList.tsx`:** rows whose song is archived render an "Archived" badge (only reachable on an archived release, which is read-only anyway).

**Tests:** Domain (`SongArchivalTests`) — exclusive upcoming song cascades; shared-with-active survives; shared-only-with-archived cascades; released untouched. API — release archive cascades correctly across catalog scopes; manual archive on active-linked / released song → 409; delete archived → 204; delete active-linked → 409; delete orphan directly → 204; PUT archived → 409; archived song → no pending action; adding an archived song to a release → 409.

---

## VII. Out of scope this round

- Un-archive / restore (songs or releases) and hard delete / purge — same policy as 1.2.
- Per-track task fan-out on albums, DSP stats (PROGRESS backlog) — the Song ids created here are their foundation.
- ISRC/UPC format validation, song merge/dedup tooling, track durations.
