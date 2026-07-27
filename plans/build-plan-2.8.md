# ZMG Release Tracker — Build Plan v2.8 (multilingual EN/ES)

Delta on [build-plan-2.7.md](build-plan-2.7.md), whose closing section outlined this as three layers.
Continues milestone numbering from M42 → **M43–M48**.

## Context

The app is English-only: ~250 hardcoded strings in the SPA, ~35 English error/warning messages minted
in `Zmg.Domain` and the services, and 41 distinct checklist task titles seeded into the database. ZMG
works in Spanish; the tracker doesn't. v2.7 deliberately deferred the M37 language selector here.

Three separate bodies of text, each needing a different mechanism:

| Text | Lives in | Mechanism | Milestones |
|---|---|---|---|
| UI chrome (~250 strings) | `src/Zmg.Web` TSX | react-i18next JSON bundles | M43–M45 |
| API errors + warnings (~35) | `Zmg.Domain` / `Services` | stable codes, SPA owns the prose | M46 |
| Checklist task text (41 titles) | Postgres, seeded | per-locale child table keyed by a stable `Code` | M47–M48 |

## How this plan is run

**I build this one solo** — no interactive walkthrough, unlike v2.7. Branch `feat/i18n-multilingual`
off `dev`; **commit and push after each milestone**, so every milestone is an independently reviewable
commit and a resume marker. Checklists inside each milestone are the finer-grained resume markers.

Milestones are independently shippable and ordered so stopping after any one leaves the app coherent
(M44 is the one exception — see its note). **Two things need the user, and only two:**

1. **Spanish checklist copy (M48).** I draft all 41 titles; the user reviews and corrects. This is the
   only content I can't validate myself, and it's *not* a blocker — M47 ships the whole mechanism with
   English-only rows, so review can happen after the machinery is proven.
2. **Nothing else.** No new infra, no new secrets, no Terraform, no Cloudflare change, no cost. The
   Worker serves whatever `dist/` contains; translations are static JSON in the bundle.

**Blast radius** (per CLAUDE.md): M43/M44/M45 are **SPA-only** → `pnpm lint` + `pnpm test` + `pnpm build`,
no `dotnet test`. M46 and M47 change DTOs and a migration → **full `dotnet test`** plus the SPA three.
M48 is seed data + one SPA screen → full `dotnet test`.

## Locked decisions — don't re-litigate

- **Two languages: `en` and `es`.** `es` unqualified, not `es-ES`/`es-419` — `Intl` resolves it and ZMG
  has no regional-format requirement. Adding a third language must not require touching component code.
- **Translations are bundled, not fetched.** JSON imported statically into the Vite bundle; no
  `i18next-http-backend`, no extra request at the edge, no loading state for chrome text. Cost is ~20KB
  gzipped for both languages plus ~15KB for i18next itself — paid once, cached, and it keeps the SPA's
  first paint (M42's whole point) untouched.
- **The server stays culture-free.** M41 ships plain `chiseled` with `InvariantGlobalization=true`, and
  **v2.8 must keep that true** (PROGRESS, Cross-cutting decisions). No `.resx`, no `CurrentUICulture`, no
  server-side date/number formatting. M46 ships codes precisely so no prose needs a culture; M47's
  translations are *data* rows, not framework resources. If any of this ever needs `chiseled-extra`
  (+33MB), it means the design went wrong — revisit rather than switch the image.
- **The API ships codes; the SPA owns every user-facing sentence.** One error channel, no parallel
  "message" field to drift (M46). The only prose left on the wire is `Results.Problem` detail on a 500,
  which is a developer-facing string.
- **Checklist text is translated by lookup, never by rewriting rows.** A release's task titles stay the
  snapshot `TemplateCopy` writes; a stable `Code` resolves a per-locale override at read time. Editing a
  task's title makes it custom and drops it out of translation (M47).
- **`Release.IsDistributed` stops matching on the English title.** It currently matches
  `Title == "Distribute to DSPs"` (`Release.cs:38`, plus `ReleaseService.cs:57,187`). One Spanish title
  and the UPC warning, the pending-actions engine, and the past-date backfill all silently stop firing.
  M47's `Code`/`SourceCode` is the fix and is **not optional**.

---

## M43 — i18n foundation + language selector

```
[x] 1. Add i18next + react-i18next             i18next 26.3.6 / react-i18next 17.0.11
[x] 2. src/i18n/ — init, resources, guard      ← Code change  (+ typed t() off en.json)
[x] 3. useLanguage + pre-paint <html lang>     ← Code change  (folded into the theme IIFE)
[x] 4. LanguageToggle in the navbar            ← Code change  (the M37 deferral)
[x] 5. Locale-aware dates + counted phrases    ← Code change  (+ useFormatters, lib/calendar too)
[x] 6. Enum/status/phase label maps → t()      ← Code change
[x] 7. Key-parity test                         ← Code change  (web 32 → 50 tests)
```

Everything the later string sweeps depend on, plus enough real translation that the toggle visibly does
something on day one. No feature-page strings yet.

**Step 1.** `pnpm add i18next react-i18next` from `src/Zmg.Web`. Take whatever the registry resolves and
**check the peer range against React 19** before committing the lockfile — do not pin a version from
memory. No `i18next-browser-languagedetector`: detection is six lines and has to agree with
`usePersistedState` anyway (below).

**Step 2 — `src/i18n/`.**

```
src/i18n/index.ts          init + the isLang guard + SUPPORTED
src/i18n/locales/en.json   one file per language, nested by feature
src/i18n/locales/es.json
```

One namespace, nested keys (`releases.detail.needsAttention`), **not** per-feature namespaces — with
everything bundled there is nothing to lazy-load, so namespaces would only add `useTranslation('x')`
ceremony at every call site. Init with `fallbackLng: 'en'`, `interpolation: { escapeValue: false }`
(React already escapes), `returnNull: false`, `supportedLngs: ['en','es']`. Imported once from
`main.tsx` **before** `<App />` renders, so no component ever sees an uninitialized instance.

**Step 3 — `src/i18n/useLanguage.ts`.** Mirror `hooks/useTheme.ts` deliberately, including its rule that
a value is persisted **only on an explicit choice** — a first-time visitor follows their browser until
they actually pick:

- `resolveInitialLanguage()` = `readPersisted('zmg.lang', browserLanguage(), isLang)`, where
  `browserLanguage()` reads `navigator.language`, takes the part before `-`, and returns `es` only for
  an exact `es` match (everything else → `en`). Wrap in try/catch like `systemTheme()`.
- The hook calls `i18n.changeLanguage(lang)` and sets `document.documentElement.lang = lang` in an
  effect, and `writePersisted` on explicit change only.
- **Pre-paint script in `index.html`**, folded into the existing theme IIFE rather than added as a second
  script: stamp `document.documentElement.lang` from `zmg.lang` (or the browser fallback) so screen
  readers and spellcheck get the right language on the first paint. Same "keep the two in sync" comment
  the theme script already carries.

**Step 4 — `components/LanguageToggle.tsx`**, slotted into `NavBar.tsx` immediately before `ThemeToggle`
— the navbar comment at that spot already reserves the place. With exactly two languages this is a
**toggle, not a dropdown**: a `Languages` lucide icon plus the *other* language's code, matching
`ThemeToggle`'s "shows what you'd switch TO" convention and its exact button classes. `aria-label` is
translated. If a third language ever lands, it becomes a `RowMenu`-style popover — which must portal to
`<body>` per the standing popover rule.

**Step 5 — `lib/format.ts`.** Two different problems, split deliberately:

- **Dates stay in `format.ts`** — `formatReleaseDate(date, locale = 'en')` passes `locale` to
  `toLocaleDateString`. Still pure, still `.test.ts`-testable, still parsing at local midnight (never
  `new Date('yyyy-MM-dd')` — the standing rule). Callers pass `i18n.language`.
- **Phrases leave `format.ts`.** `formatCountdown` ("in 3 days" / "Releasing today") and
  `formatTimeframe` ("7–14 days before") are sentences, not formatting; they become i18next keys with
  `count` (`_one`/`_other` in both languages) called from the components, with `daysToRelease` /
  `todayIso` staying in `format.ts` as the pure numeric core. `format.test.ts` loses the phrase
  assertions and keeps the arithmetic ones.

**Step 6.** The label maps that are pure English display text: `lib/phase.ts` `phaseLabels`,
`components/TypeBadge.tsx`, and `components/StatusBadge.tsx` — note the last renders the server's
`status` string **directly as its own label**, so it must map `Upcoming|Released|Complete|Archived`
through `t()` while keeping the raw value as the `cva` variant key. These four values are already
culture-invariant codes on the wire; M46 does not need to touch them.

**Step 7 — `src/i18n/i18n.test.ts`.** Vitest here is `environment: 'node'` and includes only
`src/**/*.test.ts` — **no `.tsx`, no Testing Library** (vite.config.ts). So the guard is a pure-module
test over the two JSON imports, and it is the thing that keeps M44/M45 honest:

- every leaf key in `en` exists in `es` and vice versa (flatten both, compare sorted key lists);
- no empty or whitespace-only values;
- interpolation placeholders (`{{…}}`) match per key across languages;
- plural key families are complete (`_one` implies `_other` in both).

**Verification:** `pnpm lint` · `pnpm test` · `pnpm build`. Manually: toggle in both themes, at 375px and
desktop, confirm the choice survives a reload and that `<html lang>` flips. No `dotnet test`.

**Files:** `package.json`, new `src/i18n/{index.ts,useLanguage.ts,i18n.test.ts}`, new
`src/i18n/locales/{en,es}.json`, new `src/components/LanguageToggle.tsx`, `src/components/NavBar.tsx`,
`src/components/index.ts`, `src/components/StatusBadge.tsx`, `src/components/TypeBadge.tsx`,
`src/lib/phase.ts`, `src/lib/format.ts`, `src/lib/format.test.ts`, `src/main.tsx`, `index.html`.

---

## M44 — SPA strings: home + releases

```
[x] 1. features/home + PendingSection
[x] 2. features/releases pages (list, archived, form, detail)
[x] 3. features/releases/components (13 files)
[x] 4. Toast/confirm/error copy on those paths
[x] 5. Shared components/** those pages render   ← scope pulled forward from M45
```

**Scope note (landed).** Step 5 was M45's in the original split, but Home and Releases render
`FilterBar` / `Loading` / `ConfirmDialog` / `RowMenu` / `ReorderArrows` / `ProgressBar` directly — leaving
them English would have left exactly the half-translated section this milestone exists to avoid. They're
~30 strings, so they moved here and **M45 is now catalog + artists + templates only**.

The big slice: ~130 of the ~250 strings, and every hard case (pluralization, interpolated titles,
confirm dialogs, `aria-label`s). **This is the one milestone that is not independently shippable
mid-way** — a half-translated Releases section is worse than an untranslated one, so it lands as one
commit even though it is the largest.

Conventions, fixed here and reused by M45:

- **Keys are `feature.screen.element`**; anything used on three or more screens goes to `common.*`
  (Save, Cancel, Delete, Edit, Archive, Back, "Loading…", "No results").
- **Never concatenate.** `t('releases.detail.trackCount', { count })`, not `` `${n} tracks` ``. Spanish
  word order differs from English often enough that concatenation is a correctness bug, not a style one.
- **`aria-label`, `title`, `placeholder`, and confirm-dialog copy are user-facing** and translate.
  `data-testid`-style identifiers and `cva` variant keys are not.
- **`<Trans>` only where a sentence wraps inline markup** (a link or `<strong>` mid-sentence). Everything
  else is `t()` — `<Trans>` is more expensive to read and to keep in sync.
- **Existing `errorMessage(e, 'Could not add task.')` fallbacks translate now**; the server-side half of
  those messages is M46's job. The two are independent by design: this milestone's fallbacks are text the
  SPA already owns.

**Verification:** `pnpm lint` · `pnpm test` (key parity catches every miss) · `pnpm build`. Browser-verify
the release create → detail → archive path end to end in Spanish, at 375px and desktop, light and dark —
Spanish strings run 15–30% longer than English, which is exactly what breaks a `sm:` layout, and the
standing rule is that the page body never scrolls sideways.

**Files:** `src/features/home/**`, `src/features/releases/**`, `src/i18n/locales/{en,es}.json`.

---

## M45 — SPA strings: catalog, artists, templates, shared components

```
[x] 1. features/catalog (4 pages + 2 components)
[x] 2. features/artists (2 pages)
[x] 3. features/templates
[x] 4. components/** leftovers          most landed in M44; only InlineAddForm was left
[x] 5. Sweep for stragglers             clean — the only literals left are KeyboardEvent keys
                                        and the four ReleaseStatus wire codes
```

**Landed:** the artists delete dialogs were the interesting case — three hand-rolled
`count === 1 ? '' : 's'` part lists became `artists.counts.*` plural keys joined with `common.and`,
which is the only way that sentence reads correctly in Spanish. `eslint-plugin-i18next` was **not**
adopted (see below); the sweep plus the key-parity test covered it.

~120 strings, all of them shapes M44 already solved. Step 5 is a manual grep sweep for JSX text nodes and
quoted capitalized strings under `src/features` and `src/components`, plus a click-through of every
screen in Spanish looking for English.

**Considered and left as a judgement call:** `eslint-plugin-i18next`'s `no-literal-string` rule to make
new literals a lint error. It false-positives heavily on Tailwind class strings and `cva` variants, and
would need an attribute allowlist tuned by hand. **If it can't be configured to a clean baseline in one
pass, drop it** — the M43 key-parity test plus a Spanish click-through already catch the realistic
failure (a key added to `en` and forgotten in `es`), which is the one a lint rule wouldn't.

**Verification:** `pnpm lint` · `pnpm test` · `pnpm build`, plus a full Spanish pass over catalog,
artists, and templates at 375px and desktop, light and dark.

**Files:** `src/features/{catalog,artists,templates}/**`, `src/components/**`,
`src/i18n/locales/{en,es}.json`.

---

## M46 — API messages as stable codes

```
[ ] 1. Domain: Message record + ValidationResult carries codes   ← Code change
[ ] 2. Validation.cs → codes + args (13 messages)                ← Code change
[ ] 3. ReleaseWarnings → codes (3)                               ← Code change
[ ] 4. Services → codes (17 messages)                            ← Code change
[ ] 5. OperationResult + ValidationErrorResponse shape           ← Code change
[ ] 6. SPA: client.ts translates, serverMessages.ts retires      ← Code change
[ ] 7. Domain + API tests updated                                ← Code change
```

Every server-minted sentence becomes a culture-invariant code the SPA renders. This is what lets the
server keep `InvariantGlobalization=true` while the user reads Spanish.

**Wire shape.** `ValidationErrorResponse` goes from `string[]` to a list of
`{ code, args? }`; `args` is a `Dictionary<string,string>` for the messages that interpolate (artist
name, release title). **No `message` field** — a parallel prose field is exactly the "second channel that
drifts" the project already rules out for warnings. The cost is that a raw `curl` gets
`{"errors":[{"code":"error.song.duplicateTitle"}]}` instead of a sentence; acceptable with a single
consumer, and codes are strictly better in logs. `Results.Problem` (500) keeps its developer-facing
prose — it is not user-facing text.

**Domain.** A `readonly record struct Message(string Code, IReadOnlyDictionary<string,string>? Args = null)`
in `Zmg.Domain`; `ValidationResult.Errors`/`Warnings` become `List<Message>`, and `OperationResult.Errors`
becomes `IReadOnlyList<Message>` (`Problem` wraps its detail in a code-less message). Constants move next
to the rule that raises them — `Validation.DuplicateSongTitleCode` replaces
`Validation.DuplicateSongTitleMessage`, `ReleaseWarnings.MissingUpc` becomes `"warning.missingUpc"`, and
so on. `ValidationTests.cs:46-48` is the only place asserting literal prose; the rest already assert
through constants and just follow the rename.

**Naming.** `error.<area>.<rule>` and `warning.<name>`, matching the i18next key path 1:1 so the SPA map
is `t(code, args)` with no translation table. Codes are **permanent identifiers** — renaming one is a
breaking change across both sides, same rule as the integer enums.

**Pending actions.** `PendingActionDto.Label` is two different things today: a task **title** for
`TaskDue` (user content, M47's problem) and English **prose** for the other three kinds. No DTO change —
the SPA switches on `kind`, rendering `label` verbatim for `TaskDue` and `t()` on the code otherwise.
`PendingActions.cs` emits codes for the three data kinds, which falls out of step 3 anyway since two of
them already reuse `ReleaseWarnings`' constants.

**SPA.** `client.ts` builds `ApiError` from the new shape and translates `code` + `args` through i18next
at construction; `errorMessage(e, fallback)` is unchanged at every call site. `api/serverMessages.ts`
retires — the whole reason it existed (mirroring a C# string exactly so the SPA could recognise it) is
gone once the wire carries a code, and its one consumer compares against the code instead.

**Verification:** full `dotnet test` (this changes DTOs) · `pnpm lint` · `pnpm test` · `pnpm build`.
Browser-verify at least one 400 (duplicate song title), one 409 (write to an archived release), and a
release-create warning, in both languages.

**Files:** `src/Zmg.Domain/{Validation,ReleaseWarnings,PendingActions}.cs`, new
`src/Zmg.Domain/Message.cs`, `src/Zmg.Api/Services/OperationResult.cs`, `src/Zmg.Api/Contracts/Dtos.cs`,
`src/Zmg.Api/Extensions/OperationResultExtensions.cs`, the five `*Service.cs` that mint messages,
`src/Zmg.Web/src/api/client.ts`, delete `src/Zmg.Web/src/api/serverMessages.ts`, `tests/**`.

---

## M47 — DB-authored checklist translations (schema + resolution)

```
[ ] 1. TemplateTask.Code + ReleaseTask.SourceCode          ← Code change
[ ] 2. TemplateTaskTranslation entity + DbContext config   ← Code change
[ ] 3. Migration + backfill of both columns                ← Code change
[ ] 4. IsDistributed off Code, not Title                   ← Code change  (see locked decisions)
[ ] 5. X-Lang header + locale resolution in the services   ← Code change
[ ] 6. Editing a task title clears SourceCode              ← Code change
[ ] 7. Tests: copy, resolution, fallback, backfill         ← Code change
```

The mechanism, shipped with English-only rows. M48 fills in Spanish. Splitting them keeps a schema
migration and a content review out of the same commit.

**Schema.**

- `TemplateTask.Code` — nullable string, unique per template. Seeded tasks get a stable slug
  (`distribute-to-dsps`, `mix-master`, …); tasks the user adds in the editor get `null` and are simply
  never translated, which is correct — they're user content.
- `TemplateTaskTranslation(TemplateTaskId, Locale, Text)`, composite PK `(TemplateTaskId, Locale)`,
  cascade from `TemplateTask`. Chosen over a `jsonb` column: it is provider-agnostic, and **tests run
  SQLite** (`jsonb` querying is not). English lives in `TemplateTask.Title` as today, so `en` needs no
  rows and the fallback path is the existing column.
- `ReleaseTask.SourceCode` — nullable, copied by `TemplateCopy` alongside the existing
  `SourceTemplateTaskId`. Lineage that survives the template task being deleted or renumbered, which the
  GUID alone doesn't (see the seed-data drift hazard carried in PROGRESS).

**Migration.** Both columns backfill from existing data, no data loss and no manual step:
`TemplateTask.Code` by deterministic id (the seeded ids come from `SeedData.DeterministicTaskId`, so a
static id→code map in the migration is exact); `ReleaseTask.SourceCode` by joining
`SourceTemplateTaskId` → the freshly-coded template task. Migrations are Postgres-specific and applied by
the deploy pipeline (M41), so this ships through CI like any other — **but** it is the first migration
since the pipeline took over, so confirm the bundle step actually runs it before the image swaps.
Additive-only, so it does not narrow the rollback window (PROGRESS, forward-only rule).

**Step 4 is the load-bearing one.** `Release.IsDistributed` becomes
`Tasks.Any(t => t.SourceCode == TaskCodes.DistributeToDsps && t.IsDone)`, and the two `ReleaseService`
title comparisons (`:57` list projection, `:187` past-date backfill) follow. `SeedData.DistributeToDspsTitle`
stops being an identity and becomes just the English text. Domain and API tests reference the code
instead. **Verify explicitly that a release created before this migration still reports distributed** —
that is exactly what the backfill exists for, and the failure mode is silent.

**Resolution.** `client.ts` sends `X-Lang: <i18n.language>` on every request (one place, same spot the
JSON `Content-Type` is set — and note the FormData branch must keep it too). The API reads `X-Lang`,
falls back to `Accept-Language`, then to `en`. A pure `Zmg.Domain` helper resolves
`(code, locale, translations, fallbackTitle) → text` — English fallback always, never a blank or a raw
code. Applied in `TemplateService.ToDto` and wherever release-task DTOs are built, so both the templates
editor and a live checklist translate.

**Step 6.** When a release task's or template task's title is edited, **clear its `SourceCode`/`Code`** —
the user has overridden the standard text, and a translation that silently reverts their edit on a
language switch would be a bug. Applies to `ReleaseTaskService.UpdateAsync` and
`TemplateService.UpdateTaskAsync`.

**Verification:** full `dotnet test` · `pnpm lint` · `pnpm test` · `pnpm build`. New tests: `TemplateCopy`
carries `SourceCode`; resolution returns the translation, falls back to English for a missing locale and
for a `null` code; `IsDistributed` keys off the code; a title edit clears the code. Then a real deploy —
check the migration applied and a pre-existing release still shows as distributed.

**Files:** `src/Zmg.Domain/Entities/{TemplateTask,ReleaseTask}.cs`, new
`src/Zmg.Domain/Entities/TemplateTaskTranslation.cs`, new `src/Zmg.Domain/TaskCodes.cs`,
`src/Zmg.Domain/{TemplateCopy,SeedData,Entities/Release}.cs`, `src/Zmg.Infra/Data/ZmgDbContext.cs`,
new migration, `src/Zmg.Api/{Contracts/Dtos.cs,Services/*}`, `src/Zmg.Web/src/api/client.ts`, `tests/**`.

---

## M48 — Spanish checklist content + per-locale editing

```
[ ] 1. Draft es for 41 titles                    ← Code change (SeedData)
[ ] 2. Seed via HasData + migration              ← Code change
[ ] 3. User review pass                          ← the one input I can't self-serve
[ ] 4. Templates editor: per-locale text field   ← Code change  (droppable — see below)
[ ] 5. PROGRESS + README + CLAUDE.md             ← Code change
```

**Step 1.** 41 distinct titles — 31 base (the single template) + 10 album extras. The album template is
the base list *plus* the extras, so it holds 41 tasks and shared titles are translated once. (Counted
from `SeedData.cs`: PROGRESS and build-plan-2.7 both say the album template is 40, which has been stale
since M6 inserted "Distribute to DSPs" into the shared base list. Fix it in M48's PROGRESS pass.) They
are dense with domain jargon that must **not** be
translated: DSP/BMI/MLC/SoundExchange/Musixmatch/Canvas/Artist Pick are proper nouns, and
"smart link", "pre-save", "waterfall", "multitracks" are industry terms ZMG uses in English. I draft
conservatively — translate the verb and the connective tissue, leave the nouns — and flag every
judgement call inline for review rather than deciding silently.

**Step 2.** Seeded through `SeedData` + `HasData` like the templates themselves, so the rows are
deterministic, versioned in the repo, and reviewable in a diff. Ids are `(TemplateTaskId, Locale)`, which
is already deterministic — no new id scheme, and none of the `DeterministicTaskId` renumbering hazard.

**Step 3.** The user reviews the Spanish and edits it. **The bar here is deliberately low** (user, v2.8
kickoff): a first pass, not perfect copy. **Anything genuinely ambiguous stays in English** rather than
being guessed at — the user revisits the running site and completes it. So: translate what's clearly
translatable, leave the doubtful term as-is, and never invent a Spanish equivalent for an industry term
to avoid an English string. Corrections after this ship as either a follow-up migration or, once step 4
exists, directly in the app.

**Step 4** is the "editable without a deploy" half of the v2.7 outline: the templates editor grows a
per-locale text field so ZMG can fix Spanish copy without a migration, over a
`PUT /api/template-tasks/{id}/translations/{locale}`. **This is the droppable step.** If M48 runs long,
ship 1–3 and carry step 4 into the backlog explicitly — seeded Spanish with no in-app editor is a
complete, coherent state, and the editor is a convenience, not a correctness requirement.

**Step 5 — final docs pass.** PROGRESS.md is **already being updated per milestone** (its v2.8 journal
entry, the in-flight banner, and the i18n cross-cutting block landed with M45), and CLAUDE.md already
carries the SPA i18n convention. So step 5 is what's left: **compact** the v2.8 journal entry now that
the version is done and drop the in-flight banner, fold the M46 code convention and the M47
`Code`/`SourceCode` rule into Cross-cutting decisions, and refresh **README.md** — whose status line
still says v2.5 and predates this plan.

**Verification:** full `dotnet test` · the SPA three. Browser-verify a Spanish checklist end to end:
create a single and an album, confirm every seeded task reads Spanish, confirm a user-added task and an
edited task both stay verbatim, and confirm switching to English restores the English titles live.

**Files:** `src/Zmg.Domain/SeedData.cs`, new migration, `src/Zmg.Web/src/features/templates/**`,
`plans/PROGRESS.md`, `README.md`, `CLAUDE.md`.

---

## Not doing

| Option | Why not |
|---|---|
| Server-side `.resx` + `CurrentUICulture` | Forces `chiseled-extra` (+33MB on an image re-pulled every cold start, M40) and moves text away from the layer that renders it. Codes + SPA prose cost nothing |
| `i18next-http-backend` (lazy locale loading) | ~10KB per language gzipped. A network round-trip to save that would undo M42's whole point |
| `i18next-browser-languagedetector` | Six lines of detection vs. a dependency that has to be reconciled with `usePersistedState` anyway |
| Translating user content (artist/release/song titles) | It's the user's data in the user's language. Only *app* text translates |
| A third language / RTL | Nothing in the design blocks a third language (add a JSON file, flip the toggle to a popover). RTL would need a layout pass and there is no demand |
| `jsonb` translations column | Tests run SQLite; a child table is provider-agnostic and indexable on both |
| Localized URLs (`/es/releases`) | Router churn for no benefit — the language is a preference, not a route |
| Postgres Spanish collation (`ñ` between `n` and `o`) | Real, but a database setting and orthogonal to this plan. Fold into the Phase-2 real-Postgres test work |

## Known trap, carried from v2.7

`SongService.cs:31` searches titles via `EF.Functions.Like(s.Title.ToLower(), …)`. **Postgres' `lower()`
is Unicode-aware; SQLite's is ASCII-only** — so a search for "cancion" matching "Canción" behaves
differently in prod than in the SQLite-backed tests. This plan doesn't cause it and doesn't fix it, but
Spanish content is what makes it reachable. Don't "fix" it by reaching for Npgsql `ILike`, which would
break the provider-agnostic rule; the real fix is the Phase-2 real-Postgres test work.
