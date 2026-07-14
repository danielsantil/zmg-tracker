# ZMG Release Tracker — Build Plan v1.2 (Archived releases)

Additions on top of [build-plan-1.1.md](build-plan-1.1.md) (which added M6–M10 on top of the frozen [build-plan-1.0.md](build-plan-1.0.md)). This file is the delta only — read 1.0/1.1 for anything not restated here. Section references like "1.1 §I" point into those docs.

**Scope:** a release **lifecycle** addition — an `Archived` status, an Archived Releases screen, and a soft-delete ("Remove") that only archived releases can reach. Continues the milestone numbering from 1.1 (which ended at M10), so this file covers **M11**.

**Milestone map:**

- **M11 — Archived status & soft-delete lifecycle.** New `ArchivedAt`/`DeletedAt` columns + migration, the derived `Archived` status, `scope=archived` on the list, `POST /api/releases/{id}/archive`, a repurposed soft-delete `DELETE /api/releases/{id}`, the Archived Releases page (linked from All Releases), the Archive action on Home cards + All Releases rows, and the read-only archived detail.

---

## I. Concept & lifecycle

A release moves through three persisted lifecycle states, on top of the existing **derived** status (Upcoming / Released / Complete, from date + task progress — 1.0 §9):

```
  Active ──archive──▶ Archived ──remove(soft-delete)──▶ Removed
 (default)            (terminal, read-only)            (hidden everywhere)
```

- **Active** — `ArchivedAt == null && DeletedAt == null`. Behaves exactly as today: shows on Home (when `releaseDate >= today`) and in All Releases; status is derived.
- **Archived** — `ArchivedAt != null && DeletedAt == null`. Drops off Home and All Releases, appears only on the Archived Releases screen. Status renders as **`Archived`** (overrides the derived value). **Terminal: archives are non-restorable.** Its checklist/tracklist stay visible on the detail screen but every control is disabled.
- **Removed** — `DeletedAt != null`. Soft-deleted; hidden from every list and from pending actions. **Releases are never hard-deleted** — the row stays in the DB (phase-2 stats hang off stable ids, 1.1 backlog).

**Rules (the two guarded transitions):**

- **Archive** — allowed only when `ReleaseDate >= today` (you archive a plan you've decided not to ship; a release already out stays in the history). Not allowed on an already-archived release. → 409 Conflict otherwise.
- **Remove** — allowed only on an **archived** release (`ArchivedAt != null`). → 409 Conflict on an active release.

---

## II. Schema & data-model changes

One new EF migration (`AddReleaseArchival`) adds both columns (existing rows get nulls → all currently active, correct).

**Release** (1.0 §7 / 1.1 §I):

- `ArchivedAt DateTime?` — set on archive, null while active.
- `DeletedAt DateTime?` — set on remove (soft-delete), null otherwise.

```csharp
class Release {                              // fields added to the v1.1 entity
  /* ...v1/v1.1 fields... */
  DateTime? ArchivedAt;   // v1.2 — null = active
  DateTime? DeletedAt;    // v1.2 — soft-delete; null = live
}
```

A **global query filter** `HasQueryFilter(r => r.DeletedAt == null)` on `Release` keeps soft-deleted rows out of every read (lists, detail, pending) from one place; the archived scope filters on `ArchivedAt != null` on top of it.

---

## III. M11 — Archived status & soft-delete lifecycle

**Domain:**

- `ReleaseStatus.Archived` const + `Derive(..., bool isArchived = false)` returns `Archived` first, before the date/progress logic. Pure; unit-tested.

**API:**

- **List** (`GET /api/releases`, 1.1 §M9): a new `scope=archived` returns only archived releases (`ArchivedAt != null`) ordered `releaseDate desc`, same shape as `scope=all`. `home` and `all` gain `ArchivedAt == null` (archives never appear there). The status projection carries `ArchivedAt` so `Derive` can stamp `Archived`.
- **Archive**: `POST /api/releases/{id}/archive` — 404 unknown; 409 if `releaseDate < today` or already archived; else stamps `ArchivedAt = UtcNow`, 204.
- **Remove (soft-delete)**: `DELETE /api/releases/{id}` is **repurposed** from a hard delete to a guarded soft-delete — 404 unknown; 409 if not archived; else stamps `DeletedAt = UtcNow`, 204. (No caller hard-deletes a release anymore; the old Home "Delete" is replaced by Archive.)
- **Pending** (1.1 §M10): archived releases contribute nothing — `PendingService` filters `ArchivedAt == null` on both the aggregate and by-release paths (the global filter already excludes removed).

**Frontend:**

- **Status badge**: an `Archived` style (neutral/slate) added to `StatusBadge`.
- **Home cards** (1.1 §M9): the **Delete** button is replaced by **Archive** (every Home card is `releaseDate >= today`, so Archive always applies there). Confirm, call archive, reload — the card drops off Home.
- **All Releases** (1.1 §M9): a **"Archived Releases →"** link sits at the top of the table (not a nav item), routing to `/releases/archived`. Rows gain an **Action** cell with an **Archive** button, shown only when `releaseDate >= today`.
- **Archived Releases** (`/releases/archived`, new page): same table design as All Releases — **Name · Type · Released Date · Action**, `scope=archived`. Action is a **Delete** button (soft-delete via `DELETE`). Rows link to the (read-only) detail.
- **Release detail** (1.0 §8.2): when the release is archived, the checklist and tracklist render but **every control is disabled** (no toggles, no add forms, no row menus) and the top-bar **Edit** button is hidden — a small "Archived — read only" note replaces it. The `Needs attention` block is naturally empty (pending excludes archived).
- **API client / types**: `scope` gains `'archived'`; `api.releases.archive(id)` added; `api.releases.delete(id)` now means soft-delete.

**Tests:**

- Domain: `Derive(..., isArchived: true)` returns `Archived` regardless of date/progress.
- API: archive marks a future release archived and drops it from `scope=home`/`scope=all` while `scope=archived` returns it; archiving a past release → 409; archiving twice → 409; remove on an archived release → 204 and it disappears from `scope=archived`; remove on an active release → 409; an archived release contributes no pending actions.

---

## IV. Out of scope this round

- **Un-archive / restore.** Archives are terminal by rule; no restore endpoint or UI.
- **Hard delete / purge.** Removed rows persist for phase-2 stats; no admin purge in v1.2.
- **Bulk archive/remove**, archive reasons/notes, and auto-archive on some age — not in v1.2.
