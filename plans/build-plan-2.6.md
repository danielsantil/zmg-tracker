# ZMG Release Tracker — Build Plan v2.6 (hardening · hard-delete · navbar · catalog fixes)

Delta on [build-plan-2.5.md](build-plan-2.5.md). Continues milestone numbering from M34 → **M35–M38**.

## Context

The app is feature-complete through v2.4 and fully deployed since v2.5 (ACA · Neon · R2 · Terraform ·
CI/CD). v2.6 is a hardening-and-cleanup pass before the multilingual work: fail-fast on
misconfiguration, drop soft-delete, fix the mobile navbar, and correct the catalog's release-counting.

Decisions locked with the user:
- **Env validation runs in every environment, including tests** — no env-based skip. Tests keep parity
  with prod by supplying dummy `R2__*` values (never used — storage tests use fakes).
- **R2 becomes required at startup**, so the lazy S3 client is removed and built eagerly.
- **Hard-delete replaces soft-delete app-wide.** Archive (`ArchivedAt`) is a separate, user-facing
  lifecycle and stays untouched.
- **Navbar mobile pattern = hamburger dropdown** (mockups approved). The **language selector is
  deferred to v2.7** with i18n, so v2.6 never ships a dead control.
- **Catalog fields collapse to one source of truth** — drop `isOrphan`/`canArchive`, derive everything
  from the earliest non-archived release date. Delete moves off the catalog to Archived Songs.

The multilingual + Terraform-state work is deferred to a separate **`build-plan-2.7.md`** (outline at
the end). Blast radius (per CLAUDE.md): **M35** API → full `dotnet test`. **M36** API/DTO/migration →
full `dotnet test`. **M37** SPA-only → `pnpm lint` + `pnpm build`. **M38** API/DTO + SPA → full
`dotnet test` **and** `pnpm lint`/`build`.

---

## M35 — Startup env-var validation (fail-fast) + eager R2 client

**Scope:** the API validates every required env var at startup and refuses to boot if any is missing or
blank, listing all offenders in one message. R2 becomes required; the lazy S3 client is removed.

**Required vars (complete set):** `ConnectionStrings__Zmg`, `R2__AccountId`, `R2__AccessKeyId`,
`R2__SecretAccessKey`, `R2__Bucket`, `R2__PublicBaseUrl`. (`ASPNETCORE_*` come from the Dockerfile with
defaults — framework-level, out of scope for an app validator.)

**Backend changes:**
- New `Extensions/StartupValidationExtensions.cs` — `Validate(this IConfiguration)` gathers **all**
  missing/blank keys and throws one `InvalidOperationException` naming every one (don't stop at the
  first). Reuse `R2Options` — add a `MissingKeys()` helper rather than re-listing R2 keys by hand.
- `Program.cs`: call it right after `CreateBuilder`, folding in the existing connection-string throw
  (`Program.cs:10-13`).
- `Services/R2StorageService.cs`: replace `Lazy<IAmazonS3>` with a client built in the constructor
  (inline `CreateClient()`); keep `IDisposable` disposing it. Update the
  `ServiceRegistrationExtensions.cs:24-30` comment (no longer "boots without R2"). Leave the upstream
  `CoverUploadService` `NotConfigured()` guard as harmless defense-in-depth.

**Tests (parity, not skips):** `tests/Zmg.Api.Tests/ZmgApiFactory.cs` supplies dummy `R2__*` values via
`UseSetting` (alongside the dummy `ConnectionStrings:Zmg` at `:27`) so every API test boots through the
same validated startup path prod uses; `UploadApiFactory` still swaps in `FakeStorageService`, so the
dummy values are never dereferenced.

**Verification:** full `dotnet test`. Manually `dotnet run` with `R2__Bucket` unset → clear boot-time
failure naming the missing key.

**Files:** `Program.cs`, new `Extensions/StartupValidationExtensions.cs`, `Services/R2StorageService.cs`,
`Services/R2Options.cs`, `Extensions/ServiceRegistrationExtensions.cs`, `tests/Zmg.Api.Tests/ZmgApiFactory.cs`,
root `README.md` (env-var table).

---

## M36 — Hard-delete everywhere (remove soft-delete)

**Scope:** DELETE actually removes rows. Drop `DeletedAt`, the three query filters, and the soft-delete
model. **Keep archive** (`ArchivedAt`) entirely.

**Reach:** only `Release` and `Song` carry `DeletedAt`; only `DELETE /api/releases/{id}` and
`DELETE /api/songs/{id}` soft-delete today. Every other DELETE (artists, tracks, tasks, template tasks)
is already a hard `Remove()`. The SPA never references `DeletedAt` — no frontend logic change.

**Backend changes:**
- `ReleaseService.DeleteAsync` (`:329-340`): `release.DeletedAt = UtcNow` → `db.Releases.Remove(release)`.
  Keep the `IsArchived` guard. Release FK **cascades** to `ReleaseTask` + `Track`; songs may orphan (fine).
- `SongService.DeleteAsync` (`:160-174`): stamp → `db.Songs.Remove(song)`. Keep the archived-or-orphan
  guard. **`Track` FK = Restrict:** orphans have no tracks so delete is clean; if an archived-but-linked
  song can reach delete, `RemoveRange` its `Track` rows first in the same `SaveChanges`.
- `Infra/Data/ZmgDbContext.cs`: delete all three `HasQueryFilter` (Release `:38`, Song `:52`, Track
  `:82` — the Track filter existed only because its parents were filtered).
- `Domain/Entities/Release.cs`, `Song.cs`: remove `DeletedAt` + fix doc comments.
- **Audit reads that leaned on the filters** (behavior unchanged since nothing stamps `DeletedAt` now —
  confirm, don't assume): `SongService` earliest-date `.Min()`, `ResolveExistingSongsAsync`,
  `SongQueryExtensions`, `ReleaseQueryExtensions`, `PendingService`.
- **Migration:** `dotnet ef migrations add DropSoftDelete` (EF 8 tooling) → `DropColumn DeletedAt` on
  `Releases`/`Songs` + regenerated snapshot; applied at startup by `db.Database.Migrate()`.
  Non-destructive (nullable timestamp); instant `ALTER TABLE` on Neon. Reset local via the Neon branch
  or `dotnet ef database update`.

**Frontend:** update stale "soft-delete" comments only (`api/releases.ts:38`, `api/songs.ts:33`,
`ArchivedReleasesPage.tsx`, `ArchivedSongsPage.tsx:11`).

**Docs:** rewrite the "Soft-delete, never hard-delete" cross-cutting rule in `PROGRESS.md` + `CLAUDE.md`.
Note the trade-off: soft-delete existed for Phase-2 stable ids, but delete is only reachable for
already-archived or orphan entities (released items must be archived, which is kept) — so it's moot.

**Verification:** full `dotnet test`. Update tests asserting `DeletedAt` was stamped / that a deleted
row stayed queryable.

**Files:** `Services/ReleaseService.cs`, `Services/SongService.cs`, `Infra/Data/ZmgDbContext.cs`,
`Domain/Entities/Release.cs`, `Domain/Entities/Song.cs`, `Infra/Migrations/*` (new migration + snapshot),
SPA comment-only edits, `plans/PROGRESS.md`, `CLAUDE.md`, plus affected tests.

---

## M37 — Responsive navbar (hamburger)

**Scope:** the approved hamburger design. Desktop keeps the current horizontal row; below `sm` (640px)
the five links collapse into a `☰` menu, while brand + theme toggle stay always-visible. **No language
selector** (ships in v2.7 with i18n) — but leave layout room for it.

**Wireframe (approved):**
```
Desktop ≥640:  [Z ZMG Tracker]  Home Releases Catalog Artists Templates    [☀]
Mobile  <640:  [Z] ....................................................  [☀] [☰]
                 tap ☰ ↓  (dropdown sheet)
                 ┌───────────────┐
                 │ Home          │  (active row filled)
                 │ Releases      │
                 │ Catalog       │
                 │ Artists       │
                 │ Templates     │
                 └───────────────┘
```

**Frontend changes:**
- Extract inline `Nav()`/`ThemeToggle()` from `App.tsx:20-69` into `components/NavBar.tsx`; App renders
  `<NavBar />`.
- Desktop (`sm:`+): the existing row, unchanged.
- Mobile: `☰`-triggered dropdown sheet anchored under the bar. `useState` open flag; close on route
  change (`useLocation`) and on outside click. Portal-to-`<body>` + `z-50` if clipping risks (per the
  RowMenu/SoftWarning rule; the sticky header is `z-10`). No page-level horizontal scroll. Reuse the
  `bg-ink/80 backdrop-blur`, `border-edge`, `text-muted`/`text-strong` tokens; `Menu`/`X` from lucide.

**Verification:** SPA-only → `pnpm lint` + `pnpm build`. Browser-verify at 375px (open/close, active
highlight, no sideways scroll) and desktop (unchanged), light + dark.

**Files:** `src/App.tsx`, new `src/components/NavBar.tsx`.

---

## M38 — Catalog / song release-counting fixes (four bugs)

**Scope:** the catalog mis-reports whether a song is released, because `SongService.ListAsync` mixes
archived-inclusive and archived-exclusive counting. Fix the inconsistency, collapse the redundant
fields to one source of truth, and correct the catalog's action + column.

**Root cause (Bugs A + B):** `releaseCount` counted *all* links incl. archived (Bug A: an
archived-release link showed `releaseCount:1` + `isOrphan:false` → "released"), while `releaseDate`
excluded archived — and `isOrphan`/`canArchive` still count archived links. *(Working tree already
changed `releaseCount` to `Count(t => t.Release!.ArchivedAt == null)` and made `WithDetailIncludes`
include all links so the song **detail** can show archived-release links — M38 builds on both.)*

**A — consistent archived exclusion:** in the `ListAsync` projection (`SongService.cs:38-58`), every
link-derived value filters `t.Release!.ArchivedAt == null`.

**B — drop `isOrphan` + `canArchive`, derive from `releaseDate`:** remove both from `SongListItemDto`
(`Contracts`) and `types/song.ts`; keep `releaseCount` and `releaseDate`. Single mapping:
- `releaseDate == null` → **No** (orphan or archived-only) → **archivable**
- `releaseDate <= today` → **Yes**
- `releaseDate > today` → **Upcoming**

This holds because "archivable" ⟺ "no active/past active release" ⟺ `releaseDate == null`. **Fix the
matching backend guard so the equivalence is true:** `SongService.ArchiveAsync`'s "released" guard
(`:149-150`) blocks on *any* past release incl. archived — make it require `ArchivedAt == null` (or drop
it, since the active-release guard at `:147-148` already covers active past releases). Otherwise an
archived-past-release song shows Archive but 409s.

**C — catalog offers Archive only; Delete lives on Archived Songs:** `CatalogPage.tsx` — remove the
`remove`/`useConfirmDelete` delete path and the `isOrphan ? Delete : Archive` branch; show **Archive**
only when `releaseDate == null`, no action otherwise. `ArchivedSongsPage.tsx` already has Delete —
unchanged. Flow: archive an orphan → it enters Archived Songs → deletable there.

**D — three-state "Released" column:** `CatalogPage.tsx:113-115` — replace `releaseCount > 0 ? Yes : No`
with the three-state derived from `releaseDate`: **No** (`text-muted`) / **Yes** (`text-okFg`) /
**Upcoming** (`text-infoFg`).

**Note on Bug A repro:** the archived-counting fix fully explains the reported `releaseCount:1` +
`isOrphan:false` (a link to an archived release). If, after the fix, a **non-archived** link ever
lingers post-removal, that's a separate track-deletion issue — verify during browser testing, but
current evidence points only at archived-counting.

**Verification:** full `dotnet test` — update `SongApiTests` assertions on
`isOrphan`/`canArchive`/`releaseCount`. Then SPA `pnpm lint` + `pnpm build`; browser-verify the Released
column and Archive-only menu across orphan / upcoming / released / archived-linked songs, plus the
detail page badging archived links.

**Files:** `Services/SongService.cs`, `Contracts/*` (`SongListItemDto`), `Extensions/SongQueryExtensions.cs`
(keep working-tree change), SPA `types/song.ts`, `features/catalog/CatalogPage.tsx`, plus affected
`tests/Zmg.Api.Tests/SongApiTests`. `ArchivedSongsPage.tsx` unchanged.

---

## v2.7 — deferred to its own plan (`build-plan-2.7.md`)

Authored as a separate plan when v2.6 lands. Multilingual EN/ES, layered so each layer ships
independently, plus the Terraform state-backend runbook:

- **L1 — UI chrome via react-i18next.** `i18next`+`react-i18next`; `src/i18n/` with `en.json`/`es.json`,
  `fallbackLng:'en'`. Language persisted via `usePersistedState('lang', …)` and stamped on `<html lang>`
  pre-paint (mirror the theme inline script). **The language selector deferred from M37 lands here**, in
  the navbar before the theme toggle, wired to `i18n.changeLanguage`. Migrate ~150–250 strings
  feature-by-feature; client-side error *fallbacks* translate here.
- **L2 — DB-authored checklist content.** Template/task text translated in the DB (editable without a
  deploy). Recommended schema: a `TemplateTaskTranslation(TemplateTaskId, Locale, Text)` child table
  (jsonb the lighter alt). Localize standard concrete tasks via a stable `Code` on the template task +
  `SourceCode` on the copy, resolved by locale with English fallback (or ship templates-editor-only
  first). SPA sends the active language (`Accept-Language`/`X-Lang`); seed EN+ES for the 31 single + 40
  album tasks — **Spanish content is the gating input**.
- **L3 — API messages as stable codes.** `Validation`/`ReleaseWarnings`/service `OperationResult`
  strings → culture-invariant codes; UI maps code → i18next key (generalize `serverMessages.ts`),
  English fallback during migration. Touches Domain + services + contracts + tests.
- **Terraform remote state (self-service runbook + cost).** Move `infra/terraform.tfstate` (cleartext
  secrets on one laptop) to an Azure Storage blob — encrypted at rest by default, shared, locked via
  native **blob lease** (no DynamoDB-equivalent). Hand-create the account + `tfstate` container via `az`
  CLI (chicken-and-egg), add a `backend "azurerm"` block to `versions.tf`, `terraform init
  -migrate-state`, delete local state, update `infra/README.md`. **Cost ≈ $0** (<$0.01/mo).
  Open decisions to settle then: L2 schema (table vs jsonb), L2 scope, and who authors the Spanish copy.
