# ZMG Release Tracker — Build Plan v2.9 (checklist text, simplified)

Delta on [build-plan-2.8.md](build-plan-2.8.md), which shipped M43–M48 on `feat/i18n-multilingual`.
Continues milestone numbering from M48 → **M49–M52**.

v2.8 is **unmerged and undeployed**, so this plan *supersedes* its M47/M48 mechanism rather than
correcting production. It lands on the same branch.

## Context

v2.8 got the three text layers right in two places out of three. UI chrome (M43–M45) and API message
codes (M46) are settled and are **not touched here**. Checklist task text (M47–M48) is not: it has
produced a steady stream of small bugs, and they all trace to one decision.

**English is special.** Task text lives in two kinds of storage simultaneously — the `Title` column
(English) and per-locale child rows (everything else). That single asymmetry forces four separate
special cases that all have to agree with each other:

1. **Two resolve paths.** `TaskText.Resolve(code, title, map)` for template tasks (code-keyed, via a
   per-request lookup service) and `TaskText.Resolve(rows, locale, title)` for release tasks
   (row-keyed, off the task's own collection).
2. **Writes have to guess intent.** "Did the user really edit the title, or is this the same title
   round-tripped by a phase move?" — answered by comparing against *what they were shown*, per locale,
   because comparing against the stored column would overwrite English with Spanish.
3. **The same action behaves differently per language.** An English edit writes one row; a Spanish edit
   fans out to every template task sharing the code (M48 had to add that, because the base checklist is
   seeded into both templates and a single-row write appeared to do nothing).
4. **`Code` means two things at once** — stable identity that *rules* key off, and the join key that
   *translations* resolve through. Because it's the join key, a title edit has to null it.

That fourth one is a **live bug**, not just complexity: reword "Distribute to DSPs" on a release and
`ReleaseTaskService.UpdateAsync` nulls `SourceCode`, which silently kills `Release.IsDistributed` — and
with it the missing-UPC warning, the pending-actions engine, and the past-date backfill, for that
release. Nothing fails; the app just quietly stops noticing.

None of this is what translation should cost. Translation is a **convenience for the reader**. It gets
a second column and a second form field. It does not get a service, a lookup map, a write heuristic, or
a say in how a rule identifies a task.

## Locked decisions — don't re-litigate

Five decisions, four of them answered directly by the user before this plan was written.

- **Both texts live on the task row.** `TemplateTask` and `ReleaseTask` each get `TitleEn` (required)
  and `TitleEs` (nullable). The `Title` column and **both** translation tables are dropped. No join, no
  lookup service, no fallback matrix — resolution is one expression over two columns.
- **Dual titles apply to `TemplateTask` and `ReleaseTask` and nowhere else.** Song titles, artist names,
  release titles, task notes, and every other user-entered string stay single-value. This is a checklist
  feature, not an app-wide pattern, and it must not spread.
- **A third language is a column, not a table.** Adding `TitleFr` would be one column, one line in the
  resolver, one field in the modal. That is a deliberate trade against the "proper" normalized shape —
  chosen because two languages is the real requirement and the normalized shape is what caused v2.8's
  bugs.
- **Template edits are per-template.** Editing a base task on the Single tab changes the Single template
  only. The two-tab editor already implies this, and it makes English and Spanish edits behave
  identically — today Spanish fans out across templates and English doesn't, which is bug source #3.
  Cost, accepted: a typo shared by both templates is fixed twice in the app.
- **`Code` is identity only.** `TemplateTask.Code` / `ReleaseTask.SourceCode` keep their `TaskCodes`
  slugs and stay `null` for user-added tasks — but **nothing resolves text through them anymore**, so a
  title edit no longer clears the code. This is what fixes the `IsDistributed` bug above, and it is the
  rule that must survive any future change: *never key a rule off a title, and never let display text
  touch identity.*
- **The API ships both languages; the SPA picks the column.** (Decision 5 — the one not pre-answered,
  adopted on the recommendation in this plan.) Consequences, all intended:
  - `ILocaleAccessor`, `LocaleAccessor`, the `X-Lang` header and `TaskText.NormalizeLocale` are
    **deleted**. The server carries no locale plumbing at all, which strengthens rather than merely
    preserves the `InvariantGlobalization=true` guarantee (M41).
  - **Switching language stops refetching.** The M48 race — invalidate the query cache before
    `i18n.changeLanguage` and the refetch re-requests the old locale, so the chrome flips and the
    checklist doesn't — cannot exist, because there is nothing to invalidate. Same cached data,
    different column.
  - `PendingAction.Label` stops being two things switched on `Kind`. It becomes always a warning code,
    plus a separate nullable `TaskTitle` pair for `PendingKind.TaskDue`. This is a DTO shape change and
    the only real cost of the decision.
- **Blank Spanish falls back to English, and that is a valid state.** Three seeded titles (Spotify
  Canvas / Artist Pick / Discovery Mode) are proper nouns end to end and get no Spanish at all. Storing
  a "translation" identical to its fallback would be dishonest and one more thing to keep in step.
  `SeedDataTests` pins that exact set, so a *forgotten* translation fails a test rather than passing as
  English inside a Spanish checklist.

## How this plan is run

Same branch (`feat/i18n-multilingual`), same solo cadence as v2.8: **commit after each milestone**, so
each is independently reviewable and a resume marker.

**Two things need the user, and only two:**

1. **Seed copy review.** [`seed-checklist-text.md`](seed-checklist-text.md) holds all 41 tasks with both
   languages side by side. The user edits it in place; M49 transcribes it verbatim. **This gates M49's
   migration** — the schema work can start, but the seed data isn't written until the file comes back.
2. **The prod database reset.** Destructive and outward-facing, so the user runs it, not me. See
   *Schema reset* below.

**Blast radius** (per CLAUDE.md): M49 and M50 change entities, DTOs and migrations → **full
`dotnet test`**. M51 is SPA-only → `pnpm lint` + `pnpm test` + `pnpm build`, no `dotnet test`. M52 is
verification and docs.

## Schema reset

All five existing migrations and the model snapshot are deleted and replaced by one clean
`InitialCreate`. There is precedent — v2.0 did exactly this — and it clears the **seed-data 3-way drift
hazard** that PROGRESS has carried since the M24 audit (`SeedData.cs` → `InitialCreate` → snapshot,
where `DeterministicTaskId` renumbers every later GUID on a mid-list insert).

- **Dev:** `dotnet ef database drop` + `dotnet ef database update`, run by me.
- **Prod:** the user drops the public schema **including `__EFMigrationsHistory`** (or resets the Neon
  branch) after merge and *before* the first deploy, so the squashed `InitialCreate` applies cleanly.
  Every prod release, song and artist goes with it. Their R2 cover objects are orphaned — harmless, but
  they will linger in the bucket.
- Rollback across this is impossible by definition. That's accepted: the branch is unshipped and the
  data is not real yet.

---

## M49 — Domain + fresh schema

**Entities**

- `TemplateTask`: `Title` → `TitleEn` (required) + `TitleEs` (nullable). `Code` unchanged, now
  documented as identity-only. `Translations` collection removed.
- `ReleaseTask`: same two columns. `SourceCode` unchanged, now identity-only. `Translations` removed.
- **Delete** `TemplateTaskTranslation.cs`, `ReleaseTaskTranslation.cs`, their `DbSet`s, their
  `OnModelCreating` blocks, and both `HasMany`/`OnDelete(Cascade)` relationships.

**`TaskText`** collapses from ~80 lines to a resolver plus a pair type:

```csharp
public readonly record struct LocalizedText(string En, string? Es);

public static string Resolve(string locale, string en, string? es) =>
    locale == "es" && !string.IsNullOrWhiteSpace(es) ? es : en;
```

`NormalizeLocale` and `SupportedLocales` move to the SPA's concern and are deleted server-side. The
one-liner keeps the "never return a raw code or an empty string" guarantee by construction.

**`SeedData`** — `TaskSeed` grows a Spanish field so both languages sit on one line, and the separate
`SpanishTitles` dictionary plus `AllTemplateTaskTranslations()` are deleted:

```csharp
new(Phase.Pre, TaskCodes.DesignCover, "Design cover for DSPs", "Diseñar la portada para los DSPs"),
new(Phase.Release, TaskCodes.SpotifyCanvas, "Spotify Canvas"),   // no es: proper noun, deliberate
```

A missing translation is now visible in the diff instead of 100 lines away in a dictionary. Content
comes from [`seed-checklist-text.md`](seed-checklist-text.md), transcribed verbatim after review.

**`TemplateCopy`** copies both columns and the code, unconditionally — no collection to map, no
conditional. The snapshot rule holds by construction: a release's text is its own columns, so a template
edit cannot reach it in any language.

**Migrations** squashed to one `InitialCreate`.

**Tests:** `TaskTextTests` shrinks to the resolver's four cases (en, es present, es blank, es null).
`TemplateCopyTests` asserts both columns and the code survive the copy. `SeedDataTests` keeps the
untranslated-set pin and the 31/41 counts. `Builders.cs` updated.

## M50 — API surface

- **DTOs** (`ReleaseTaskDto`, `TemplateTaskDto`) carry `titleEn` + `titleEs`; `AddTaskInput`,
  `UpdateTaskInput`, `AddTemplateTaskInput`, `UpdateTemplateTaskInput` take both. No `title` on the
  wire. **Only these DTOs** — no other contract gains a second text field.
- `TemplateService.UpdateTaskAsync` becomes a plain field write. **Deleted:** the shown-vs-stored
  comparison, `UpsertTranslationAsync`, the code-scoped sibling fan-out, and the "text equals the
  English title → delete the row" rule.
- `ReleaseTaskService.UpdateAsync` likewise, and **stops nulling `SourceCode`** — the fix for the
  `IsDistributed` bug.
- **Delete** `TaskTranslationService` + `ITaskTranslationService` + their DI registration;
  `LocaleAccessor` + `ILocaleAccessor` + `IHttpContextAccessor` if nothing else needs it.
- Remove every `.ThenInclude(t => t.Translations)` — `ReleaseQueryExtensions`, `ReleaseService`,
  `PendingService` — and the `locale` parameter threaded through `PendingActions.Compute`.
- `PendingAction`: `Label` becomes always a code; new nullable `TaskTitle` (`LocalizedText?`) carries
  the pair for `TaskDue`.
- `Validation.ValidateTaskTitle` validates English (required — existing code, unchanged); Spanish stays
  optional and unvalidated.

**Tests:** `ChecklistTranslationApiTests` and `TemplateTranslationEditApiTests` are largely deleted —
most of their cases pin behaviours that no longer exist. What survives, rewritten: a task's Spanish
edit doesn't touch its English; a template edit doesn't reach an existing release;
`ReleaseSnapshotApiTests` keeps proving the snapshot in both languages. **New:** renaming a release's
DSP-distribution task keeps `IsDistributed` firing — the regression test for the live bug. Expect a net
*drop* in API test count.

## M51 — SPA: one modal, both languages

`features/releases/components/TaskEditorModal.tsx` — one component on the existing `Modal` primitive,
used by **both** the templates editor and the release-detail checklist, for **add and edit**:

```
┌─ Add task ──────────────────────┐
│ Phase      [ Pre ▾ ]            │
│ English *  [___________________]│
│ Español    [___________________]│
│            blank = shows English│
│ Timeframe  [ 7 ]–[ 14 ] days    │   (Pre only)
│ Notes      [___________________]│   (release only)
│              [Cancel] [Save]    │
└─────────────────────────────────┘
```

- Replaces `TaskRow`'s inline rename and `PhaseSection`'s `InlineAddForm` **for tasks**. `InlineAddForm`
  itself stays — other screens use it.
- Full-width sheet below `sm`, standard centred modal above. Portals to `<body>` per the popover rule
  (`RowMenu` opens it, and a `position: fixed` popover inside a transformed modal panel lands off-panel).
- Rows render `resolve(language, titleEn, titleEs)` client-side.
- `useLanguage` drops its query-cache invalidation; `client.ts` drops the `X-Lang` header on **both**
  the JSON and the FormData branch.
- The `templates.perLocaleEdit` banner is removed — the modal makes it self-evident. Its i18n keys go;
  the modal's new keys are added to **both** `en.json` and `es.json` (the parity test enforces it).

**Tests:** Vitest on the modal — both fields round-trip, blank Spanish is allowed, blank English blocks
save, phase move doesn't disturb either text.

## M52 — Verification + docs

Full `dotnet test`, then `pnpm lint && pnpm test && pnpm build`, then live browser verification:

1. Create a release; confirm the checklist reads Spanish end to end.
2. Edit a task through the modal in both languages; confirm the other template and other releases are
   untouched.
3. Switch language — confirm **no network request** and instant re-render.
4. **Reword** the DSP-distribution task on a release, check it, and confirm the missing-UPC warning and
   the pending action still fire. This is the bug the whole plan exists to make impossible.

Docs: `plans/PROGRESS.md` gets a v2.9 journal entry, and the M47/M48 cross-cutting bullets (currently
~25 lines of rules describing the mechanism being deleted) are replaced by the much shorter new rule.
`CLAUDE.md`'s checklist-text convention block is rewritten to match.

---

## Out of scope

- UI chrome i18n (M43–M45) and API message codes (M46) — settled, untouched.
- Any third language. The design makes it cheap; nothing here adds one.
- Per-track task fan-out on albums, and everything else in PROGRESS's backlog.
