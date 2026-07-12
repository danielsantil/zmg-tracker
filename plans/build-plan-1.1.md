# ZMG Release Tracker — Build Plan v1.1 (Singles improvements)

Additions on top of [build-plan-1.0.md](build-plan-1.0.md). That file is the frozen v1 brief (M0–M5, shipped); this file is the delta only — read 1.0 for anything not restated here. Section references like "1.0 §5.4" point into the v1 doc.

**Scope:** singles only. The album template and album-specific behavior are deliberately untouched this round (see §V. Out of scope). Everything here continues the milestone numbering from 1.0 (which ended at M5), so this file covers **M6–M10**.

**Milestone map:**

- **M6 — Schema & seed foundation.** New columns + migration, timeframe semantics, template-copy carries them, single-template seed changes. Everything else builds on this.
- **M7 — Release identifiers (UPC/ISRC) & soft warnings.** Form fields, the soft empty-id warning, past-date backfill auto-check.
- **M8 — Task timeframes & notes.** Surface the timeframe range and per-release notes in the detail + template editor.
- **M9 — Navigation: Home vs All Releases.** Split the v1 dashboard into a forward-looking Home and a full All Releases table.
- **M10 — Pending-actions engine.** The `PendingActions` domain function, `GET /api/pending`, Home's pending section, and the detail "Needs attention" block.

Dependency order: M6 first (foundation). M7 and M8 can follow in either order. M9 creates Home, which M10's pending section plugs into, so do M9 before M10. M10 depends on M6 (timeframes) and M7 (identifier state).

---

## I. Schema & data-model changes

Applies across the milestones; collected here so the migration is defined in one place. One new EF migration adds every column below (existing rows get nulls).

**Release** (1.0 §7):

- `Upc string?` — optional free text, no format validation.
- `Isrc string?` — optional free text, no format validation.

**TemplateTask** and **ReleaseTask** (1.0 §5.1):

- `MinDaysBefore int?`, `MaxDaysBefore int?` — task timeframe (see below). Copied from template to release task by template-copy (1.0 §5.2).
- `ReleaseTask.Notes` already exists in v1 — no schema change, only newly surfaced in the UI (M8).

```csharp
class TemplateTask {
  Guid Id; string Title; Phase Phase; int SortOrder;
  int? MinDaysBefore; int? MaxDaysBefore;   // v1.1
}

class ReleaseTask {
  Guid Id; Guid ReleaseId; string Title; Phase Phase; int SortOrder;
  bool IsDone; DateTime? CompletedAt; string? Notes; Guid? SourceTemplateTaskId;
  int? MinDaysBefore; int? MaxDaysBefore;   // v1.1, copied from the template task
}

class Release {                              // fields added to the v1 entity
  /* ...v1 fields... */ string? Upc; string? Isrc;
}
```

**Task timeframe semantics** (`MinDaysBefore`/`MaxDaysBefore`, both nullable, most tasks leave them null):

- **Pre tasks:** "complete this N–M days *before* release." Shown to the user as a range ("7–14 days before"); **the max drives every calculation** (pending window, sort). Min is display-only in v1.1.
- **Release/Post tasks:** the same two columns read as "days *to complete*" after release. Stored but not acted on by any logic in v1.1 — reserved for later.

---

## M6 — Schema & seed foundation

The data layer for the rest of v1.1. No user-visible feature ships alone here, but it's a clean, testable unit.

**Do:**

- Add the columns in §I and generate one EF migration.
- Extend template-copy (1.0 §5.2) to carry `MinDaysBefore`/`MaxDaysBefore` onto each release task.
- **Single-template seed changes** (1.0 §5.4), single template only:
  - Insert **"Distribute to DSPs"** as the **third** Pre task, `MinDaysBefore=7, MaxDaysBefore=14`.
  - Set **"Pitch to Spotify"** to `MinDaysBefore=7, MaxDaysBefore=14`.
  - Result: single template 30 → **31 tasks (6 Pre / 18 Release / 7 Post)**. Album template unchanged at **40**.
  - Keep deterministic seed ids. Existing releases are snapshots and are **not** retro-modified — a release created before this change simply won't have the new task, which is correct per the snapshot rule (1.0 §2).

**Tests:**

- Template-copy carries both timeframe fields onto the release task.
- Seed counts: single = 31 (6/18/7); "Distribute to DSPs" is the 3rd Pre task with 7/14; "Pitch to Spotify" has 7/14; album still 40.

---

## M7 — Release identifiers (UPC/ISRC) & soft warnings

**Do:**

- **Form (1.0 §7.1):** add two optional fields.

  | Field | Req | Maps to | Notes |
  |---|---|---|---|
  | UPC | Optional | Release.Upc | free string, no format check. Blank is fine until DSP distribution |
  | ISRC | Optional | Release.Isrc | free string, no format check. Blank is fine until DSP distribution |

- **API:** `POST`/`PUT /api/releases` accept `upc`/`isrc`; list items and the detail DTO return them.
- **Soft warning:** a release with an empty UPC **or** ISRC shows a soft warning icon — **only once its "Distribute to DSPs" task is checked** (before distribution, blank ids are expected, so stay silent). Soft = an advisory amber/warning glyph with a tooltip ("UPC missing" / "ISRC missing" / both), never a red error, never blocks a save. The list DTO exposes a `needsIdentifierWarning` bool so cards and table rows render it without an extra fetch; the detail computes it from its own payload. Appears on Home cards (M9), All Releases rows (M9), and the release-detail header.
- **Past-date backfill (1.0 §9):** when a release is created with a `releaseDate` already in the past, the create flow auto-checks its "Distribute to DSPs" task (a song already out was, by definition, distributed). Only that task is auto-checked; everything else stays open. Because it's now checked, a blank UPC/ISRC on that release does surface as a data pending action (M10) — usually the exact reason you're backfilling.

**Validation (1.0 §6):** UPC/ISRC are optional strings, no format/length/checksum check; empty allowed. The soft warning is advisory only, never a Layer-1 error.

**Tests:**

- UPC/ISRC round-trip on create and update.
- `needsIdentifierWarning` is false while "Distribute to DSPs" is unchecked, flips true once it's checked with a blank id, stays false when both ids are filled.
- Create with a past date auto-checks "Distribute to DSPs".

---

## M8 — Task timeframes & notes

Surfaces the M6 timeframe columns and the existing `ReleaseTask.Notes` in the UI. Depends on M6.

**Do:**

- **Release detail (1.0 §8.2):**
  - Tasks with a timeframe show a small hint next to the title ("7–14 days before").
  - `[⋮]` task menu gains "set timeframe" (days-before, Pre tasks) alongside the existing rename / notes / move-phase / delete.
  - Notes are editable inline / via the menu; a task with notes shows a note indicator. (Use cases: pitch copy, smart-link URLs.)
- **Template editor (1.0 §8 screen 5):** allow setting a Pre task's days-before timeframe so future releases inherit it.
- **API:** `POST /api/releases/{id}/tasks` and `PUT /api/tasks/{id}` accept `minDaysBefore`/`maxDaysBefore`; template-task endpoints accept them too.

**Detail wireframe delta** (over 1.0 §8.2):

```
▾ PRE  (5/6)
   [x] Mix/master
   [x] Design cover for DSPs
   [ ] Distribute to DSPs   ·  7–14 days before   [⋮]   ← timeframe hint
   ...
```

**Tests:** covered by M6 copy tests + API accept/round-trip of timeframe on task create/update.

---

## M9 — Navigation: Home vs All Releases

Splits the single v1 "Releases dashboard" (1.0 §8 screen 1) into two screens. The nav gets both entries.

**Home** (`/`) — the daily driver:

- Cards for releases with `releaseDate >= today` only (status Upcoming or Complete-with-future-date). Server: `GET /api/releases?scope=home`.
- Keeps the artist/type/status filters over the card set.
- Hosts the **Pending Tasks** section (built in M10; leave a slot).
- A **New Release** button.
- Past releases drop off Home and live in All Releases.

**All Releases** (`/releases`) — the full history:

- A **table**, not cards: columns **Name · Type · Released Date**, ordered `releaseDate desc`.
- Filters: artist name, type, and a free-text search (matches title). Server: `GET /api/releases?scope=all&artistId=&type=&q=`.
- A **New Release** button here too.
- Rows link to the release detail; soft warning icon (M7) on rows missing UPC/ISRC after distribution.

**API deltas** (1.0 §4.1):

- `GET /api/releases?...&scope=&q=` — `scope=home` returns only `releaseDate >= today`; `scope=all` (default) returns everything; `q` is a case-insensitive title substring search.

**Tests:**

- `scope=home` returns only `releaseDate >= today`.
- All Releases sort (`releaseDate desc`) + artist/type/`q` search filters.

---

## M10 — Pending-actions engine

The payoff feature. A pending action is something the user should act on soon. Depends on M6 (timeframes), M7 (identifier state), M9 (Home hosts the section).

**Domain (pure, testable):** `PendingActions.Compute(release, tasks, today)` in Zmg.Domain, reused by the aggregate endpoint and the release detail.

**What creates a pending action** (generic, keyed off data — not off task titles):

1. **Task due** — any **incomplete** task that has a timeframe, where `today >= releaseDate - MaxDaysBefore` (the window has opened) **and** `releaseDate >= today` (not yet released). Today this naturally surfaces only "Distribute to DSPs" and "Pitch to Spotify" (the only seeded timeframes); adding a timeframe to any task later makes it participate with no code change.
2. **Missing identifier** — the release's "Distribute to DSPs" task is **done** and `Upc` or `Isrc` is empty. One action per release summarizing which ids are missing (e.g. "Missing UPC, ISRC").

**Shape:** `{ releaseId, releaseTitle, artistName, kind: TaskDue | MissingIdentifier, taskId?, label, daysToRelease? }`.

**Ordering:** all `TaskDue` items first, sorted by **nearest release date first** (ascending days-to-release, so the most time-critical is on top); then all `MissingIdentifier` (data) items at the bottom, grouped by release.

**Surfaces:**

- **Home** — the Pending Tasks section renders the aggregate `GET /api/pending` (whole list, this ordering). Each row links to that release's detail.
- **Release detail** — a "Needs attention" block at the top shows just that release's pending actions (computed from the loaded detail payload, same function), hidden when empty.

**Detail wireframe delta** (over 1.0 §8.2, at the top of the screen):

```
┌─ Needs attention ──────────────────────────────┐
│ • Distribute to DSPs — 14 days to release       │
│ • Missing UPC, ISRC                             │
└────────────────────────────────────────────────┘
```

**API deltas** (1.0 §4.1):

- `GET /api/pending` — aggregate pending actions across all releases, ordered as above.
- `GET /api/releases/{id}` detail DTO adds `pendingActions` for that release.

**Tests:**

- `PendingActions.Compute` (the strongest v1.1 signal): a timeframe task becomes pending only when incomplete **and** within the window **and** not yet released; tasks without a timeframe never appear; missing-id appears only when "Distribute to DSPs" is done; ordering = task-due by nearest release first, data actions always last; empty when nothing pending.
- `GET /api/pending` ordering across multiple releases.

---

## V. Out of scope this round

- **Albums.** No album-template changes (no Distribute-to-DSPs insertion, no timeframes), no album-specific pending or warning behavior. Album parity is its own later task once the album phase is revisited.
- **Release/Post "days to complete"** is stored (M6) but drives no logic yet.
- **UPC/ISRC format validation**, absolute per-task due dates, and min-days-based urgency styling — not in v1.1.

## VI. Kickoff prompts for Claude Code

One prompt per milestone; run them in order (M6 → M10). Each ends by updating PROGRESS.md and stopping.

- **M6:** "Implement M6 from build-plan-1.1.md — add `Upc`/`Isrc` to Release and `MinDaysBefore`/`MaxDaysBefore` to TemplateTask + ReleaseTask with one EF migration, have template-copy carry the timeframe fields, and update the single-template seed (insert 'Distribute to DSPs' as the 3rd Pre task at 7–14, set 'Pitch to Spotify' to 7–14; album template unchanged). Add the M6 domain tests. Run everything, update PROGRESS.md, stop."
- **M7:** "Implement M7 — UPC/ISRC on the release form and API, the soft empty-id warning (only after 'Distribute to DSPs' is checked) with a `needsIdentifierWarning` list flag, and the past-date create auto-check of 'Distribute to DSPs'. Add the M7 tests. Run, update PROGRESS.md, stop."
- **M8:** "Implement M8 — surface per-task timeframe hints and editable notes on the release detail, and let the template editor set a Pre task's days-before timeframe; task + template-task endpoints accept the timeframe fields. Run, update PROGRESS.md, stop."
- **M9:** "Implement M9 — split the dashboard into Home (`/`, forward-looking cards via `scope=home` + New Release, with a slot for the pending section) and All Releases (`/releases`, table Name/Type/Released Date sorted desc, artist/type/search filters + New Release); add `scope` and `q` to the releases list. Add the M9 tests. Run, update PROGRESS.md, stop."
- **M10:** "Implement M10 — the pure `PendingActions.Compute`, `GET /api/pending`, the Home Pending Tasks section, and the release-detail 'Needs attention' block; detail DTO returns `pendingActions`. Add the M10 tests. Run, update PROGRESS.md, stop."
