# ZMG Release Tracker — Build Plan for Claude Code

Release campaign tracker for Zion Music Group. Every song release comes with a repeatable checklist (pre-release, release, post-release); today it lives in a text list. This app turns it into a per-release checklist with progress tracking, supporting multiple artists and both single and album release types. Single user, no auth in v1. This document is the brief to hand to Claude Code. It covers scope, architecture, data model, checklist engine, screens, milestones, and known risks.

## 1. Locked decisions

- **Stack:** ASP.NET Core (.NET 8) + EF Core backend, React + Vite + Tailwind frontend.
- **Users:** Single user, no auth in v1. Runs privately (localhost or private host). Schema stays auth-ready (no user FK assumptions baked into queries).
- **Scope / coverage:** v1 is the checklist tracker: artists CRUD, releases CRUD, per-release checklists copied from editable templates. Singles are the priority; albums are a separate template and section. No DSP data pulling in v1 (that's a later phase).
- **Platform:** Responsive web, desktop and mobile. Native mobile comes in a separate build later, so the API is the contract: keep all logic server-side, frontend stays a thin SPA client.
- **Templates:** Editable in-app, seeded from the fixed checklists in section 5.4. New releases copy the template snapshot; edits to a template never touch existing releases.

## 2. Key rationale (don't skip this)

Why build instead of using Notion/Trello with a checklist template: the label plans to pull streaming/revenue data from DSPs per release and per artist in later phases. That needs its own data model (Release, Track, Artist with stable ids and room for UPC/ISRC). Starting with the checklist on top of that model means phase 2 extends tables instead of migrating out of a note-taking tool.

Why snapshot-copy templates instead of referencing them: a release checklist is historical record ("what did we do for this song"). If template edits mutated old releases, past campaigns would lie. Copy on create, then the release owns its tasks.

## 3. Domain mental model

```
Artist 1—n Release (main artist)
Artist n—n Release (featured/collab, via ReleaseArtist)
Release 1—n ReleaseTask        (the live checklist, owned by the release)
Release 1—n Track              (albums only in practice; empty for singles in v1)
ChecklistTemplate 1—n TemplateTask   (one default template per release type)
        └── copied into ReleaseTask on release creation
```

Rules:

- Every task belongs to a phase: `Pre`, `Release`, `Post`. Phases order the checklist UI.
- Creating a release copies the default template for its type into ReleaseTask rows. After that, tasks on the release can be added, removed, renamed, reordered, checked.
- One default template per release type (`Single`, `Album`). Templates are editable; `sourceTemplateTaskId` on ReleaseTask keeps lineage but nothing depends on it in v1.

## 4. Architecture

```
/src
  /Zmg.Domain        Entities, enums, template-copy logic. No I/O.
  /Zmg.Api           ASP.NET Core minimal API, EF Core (SQLite), serves SPA build in prod
  /Zmg.Web           React + Vite + Tailwind SPA
/tests
  /Zmg.Domain.Tests  xUnit unit tests (template copy, ordering, progress calc)
  /Zmg.Api.Tests     Integration tests (WebApplicationFactory + SQLite in-memory)
```

- **Persistence:** SQLite via EF Core. Single user, zero infra, file-backed. EF migrations from day one so a later move to SQL Server/Postgres is a provider swap.
- **Deployment:** One ASP.NET Core app serving the built SPA from wwwroot. `dotnet run` locally; Dockerfile included for hosting later.
- **Purity boundary:** Zmg.Domain has no I/O. Template copying, phase ordering, and progress calculation live there and are unit-tested without a database.

### 4.1 API surface (v1)

Artists

- `GET /api/artists` — list.
- `POST /api/artists` / `PUT /api/artists/{id}` / `DELETE /api/artists/{id}` — CRUD. Delete blocked if artist has releases (409).

Releases

- `GET /api/releases?artistId=&type=&status=` — list with progress counts (done/total).
- `GET /api/releases/{id}` — detail with tasks grouped by phase.
- `POST /api/releases` — create; copies default template for the type.
- `PUT /api/releases/{id}` / `DELETE /api/releases/{id}`.

Release tasks

- `POST /api/releases/{id}/tasks` — add ad-hoc task (title, phase).
- `PUT /api/tasks/{id}` — rename, move phase, notes.
- `PATCH /api/tasks/{id}/toggle` — check/uncheck, stamps `completedAt`.
- `PUT /api/releases/{id}/tasks/order` — reorder within a phase.
- `DELETE /api/tasks/{id}`.

Templates

- `GET /api/templates` — both templates with tasks.
- `POST /api/templates/{id}/tasks` / `PUT /api/template-tasks/{id}` / `DELETE /api/template-tasks/{id}` / reorder — template task CRUD.

## 5. Checklist engine (the core deliverable)

### 5.1 Data model

```csharp
enum ReleaseType { Single, Album }
enum Phase { Pre, Release, Post }

class ChecklistTemplate { Guid Id; ReleaseType Type; List<TemplateTask> Tasks; }
class TemplateTask { Guid Id; string Title; Phase Phase; int SortOrder; }

class ReleaseTask {
  Guid Id; Guid ReleaseId; string Title; Phase Phase; int SortOrder;
  bool IsDone; DateTime? CompletedAt; string? Notes; Guid? SourceTemplateTaskId;
}
```

### 5.2 Template copy

Input: new Release + its type. Responsibility: load the default template for the type, map each TemplateTask to a ReleaseTask (same title/phase/order, `IsDone=false`, lineage id set). Output: the release with a full checklist in one transaction. Pure mapping function in Zmg.Domain; the API layer wraps it with persistence.

### 5.3 Progress

`done/total` per release, plus per phase. Computed in Domain from the task list; the list endpoint returns it so the dashboard doesn't fetch every task.

### 5.4 Seed data (exact initial templates)

**Single template** — seed verbatim from the current checklist:

- *Pre:* Mix/master · Design cover for DSPs · Make video for YouTube, thumbnail and additional YouTube resources · Pitch to Amazon · Pitch to Spotify
- *Release:* Setup smart link to all stores · Setup smart link redirect from zionmusicgroup.com/&lt;song-name&gt; · Register composition to BMI · Register composition to MLC · Register to SoundExchange · Musixmatch lyrics, add/sync · Check release in Deezer (wrong artist) · Check release in Amazon (wrong artist) · Check release in Apple (wrong artist) · Spotify Canvas · Spotify Artist Pick · Update YouTube banner · Update YouTube home video · Update cards in existing videos · Update pinned comment in existing videos with link to new video · Update YouTube link on Instagram bios · Update song on Instagram bios · Send master splits to collaborators
- *Post:* Meta ads, initial release campaign · Meta ads, ongoing campaign · Spotify Discovery Mode · YouTube video ads · TikTok ads · Create YouTube lyrics video · Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos

**Album template** — the single list plus album-specific work (from release-strategy research; adjust in-app as you learn):

- *Pre:* everything in single Pre, plus: Finalize tracklist and sequencing (locked once submitted to distributor) · Confirm ISRC/UPC and per-track metadata/credits · Pick focus tracks and plan 2–4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams) · Album pre-save campaign · Update artist bio / press release / EPK · Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts) · Physical media if applicable (vinyl/CD lead times are months)
- *Release:* everything in single Release, noting registrations (BMI, MLC, Musixmatch, splits) repeat per track
- *Post:* everything in single Post, plus: Rotate focus tracks every few weeks with per-track playlist pitching · Lyric videos for remaining tracks

Sources for the album additions: [Orphiq music release checklist](https://orphiq.com/resources/music-release-checklist), [Orphiq waterfall strategy](https://orphiq.com/resources/waterfall-release-strategy), [Sonikit album launch checklist](https://www.sonikit.com/blog/articles/the-ultimate-checklist-for-an-album-launch), [Groover release checklist](https://blog.groover.co/en/tips/planning-checklist-releasing-single/).

## 6. Validation / correctness rules

**Layer 1 — hard errors (400/409).** Release requires title, main artist, type, release date. Task title non-empty. Artist name non-empty and unique (case-insensitive). Artist with releases can't be deleted. Template must keep at least one task.

**Layer 2 — warnings (advise, don't block).** Release date in the past on create ("backfilling an old release?"). Duplicate release title for the same artist. Surfaced inline in the form, dismissible.

## 7. Data model (domain)

- **Artist:** id, name, notes. Later phases add DSP identifiers (Spotify artist id, etc.).
- **Release:** id, title, type, releaseDate, mainArtistId, coverUrl (optional), notes.
- **ReleaseArtist:** releaseId, artistId, role (`Featured` | `Collab`). Main artist stays a direct FK.
- **Track:** id, releaseId, trackNumber, title, isFocusTrack. Albums only in practice; singles skip it in v1.
- **ReleaseTask / ChecklistTemplate / TemplateTask:** section 5.1.

Keep Release and Track ids and the UPC/ISRC-ready metadata columns stable; phase 2 (DSP stats) hangs off them.

### 7.1 Input checklist (per release)

**Release form**

| Field | Req | Maps to | Notes / example |
|---|---|---|---|
| Title | Required | Release.Title | Song or album name. Ex: `Luz` |
| Main artist | Required | Release.MainArtistId | Picker from artists. Ex: `Karen Santana` |
| Featured/collab artists | Optional | ReleaseArtist | Multi-select with role per entry |
| Release type | Required | Release.Type | `Single` (default) / `Album` |
| Release date | Required | Release.ReleaseDate | Date picker |
| Cover URL | Optional | Release.CoverUrl | Shown on cards; file upload is a later nicety |
| Notes | Optional | Release.Notes | Free text |

**Artist form:** name (required, unique), notes (optional).

Section 8.1 of the template (field-level help catalog) is dropped: every field above is self-explanatory to the only user.

## 8. Frontend (screens)

1. **Releases dashboard** — home. Cards per release: cover, title, artist, date, type badge, progress bar (done/total), days-to-release for upcoming ones. Filter by artist/type/status.
2. **Release detail** — the screen that matters (wireframe below). Checklist grouped by Pre/Release/Post with per-phase progress, one-tap check, add task, task notes.
3. **Release form** — create/edit per section 7.1. On create, show "checklist will start from the {type} template (N tasks)".
4. **Artists** — list + create/edit/delete.
5. **Templates** — tabs Single/Album; add/rename/delete/reorder tasks, move between phases. Banner: "changes apply to future releases only".

Highest-value UX: release detail must be fast on a phone. Checking off tasks is the daily action, likely while inside Spotify for Artists or YouTube Studio on mobile. Big tap targets, optimistic updates, collapsed done-phases.

### 8.2 Release detail layout

```
< Releases                                    [Edit]
┌────────────────────────────────────────────────┐
│ [cover]  Luz — Karen Santana                   │
│          Single · 2026-08-14 · 12/30 done      │
│          ████████░░░░░░░░░░░░  40%             │
└────────────────────────────────────────────────┘
▾ PRE  (5/5) ✓                       [collapsed]
▾ RELEASE  (7/18)
   [x] Setup smart link to all stores
   [x] Register composition to BMI
   [ ] Register composition to MLC          [⋮]
   [ ] Musixmatch lyrics - add/sync         [⋮]
   ...
   [+ Add task]
▸ POST  (0/7)
```

Behavior notes for Code:

- Checkbox toggles are optimistic; revert on API failure with a toast.
- `[⋮]` menu per task: rename, notes, move phase, delete.
- Fully-done phases render collapsed with a check; tap to expand.
- Progress header recomputes from the loaded task list, no extra fetch.

## 9. Release lifecycle

Create release (template copied) → work Pre tasks → release day, work Release tasks → work Post tasks → all done, release card shows 100%. Status is derived, not stored: `Upcoming` (date in future), `Released` (date passed), `Complete` (all tasks done). No state machine needed in v1.

## 10. Milestones

**M0 — Skeleton.** Solution layout per section 4, EF Core + SQLite with initial migration, seeded templates (5.4), React shell with routing and Tailwind, health endpoint, SPA served by the API in prod profile.
**M1 — Artists + Releases CRUD.** Sections 4.1 (artists, releases), 7.1 forms, dashboard cards without progress.
**M2 — Checklist engine.** Template copy on create, release detail screen with phases, toggle/add/edit/delete/reorder tasks, progress on dashboard cards. **M0–M2 is the first usable version.**
**M3 — Template management.** Templates screen, template task CRUD.
**M4 — Album support.** Track list on album releases (add/reorder, focus-track flag), album template surfaced end to end.
**M5 — Polish.** Mobile pass on all screens, filters, empty states, Dockerfile.

## 11. Testing strategy

- **Strongest correctness signal:** unit tests on template-copy (counts, phases, order, lineage ids, IsDone=false) and progress calculation in Zmg.Domain, no DB.
- **Unit tests:** one per validation rule in section 6, pass and fail cases.
- **Integration tests:** WebApplicationFactory + SQLite in-memory. Golden path: create artist → create release → checklist matches seeded template → toggle tasks → progress correct. Plus: artist-delete conflict, template edit not affecting existing release.
- Don't commit the runtime SQLite file; seed data lives in code/migrations.

## 12. Risks and open questions

- **Per-track task fan-out on albums.** Registrations repeat per track. v1 keeps them as single tasks with a "per track" note; auto-generating one task per track is a possible M4+ addition. Decide after the first album.
- **No auth on a hosted deployment.** Fine on localhost. If deployed to a public URL before auth lands, put it behind basic auth at the proxy or keep it on a private network.
- **Task due dates.** Tasks currently have no dates; phases carry the timing. If offset-from-release-date scheduling (e.g. "pitch Spotify at least 7 days before") turns out to be wanted, add `dueOffsetDays` to TemplateTask later. Deliberately out of v1.
- **Phase 2 (DSP stats) schema pull.** Mitigated by keeping Artist/Release/Track stable and metadata-ready (section 7).

## 13. Reference material

- Current single checklist: provided in the brief, seeded verbatim (section 5.4).
- Album checklist research: [Orphiq music release checklist](https://orphiq.com/resources/music-release-checklist) · [Orphiq waterfall strategy](https://orphiq.com/resources/waterfall-release-strategy) · [Sonikit album launch checklist](https://www.sonikit.com/blog/articles/the-ultimate-checklist-for-an-album-launch) · [Groover release checklist](https://blog.groover.co/en/tips/planning-checklist-releasing-single/).

## 14. First prompt to give Claude Code

> Set up the solution per section 4 of this plan (Zmg.Domain, Zmg.Api, Zmg.Web, tests). Implement M0 and M1: EF Core + SQLite with migrations, seed both checklist templates exactly as in section 5.4, artists and releases CRUD per sections 4.1/6/7.1, dashboard and forms per section 8. Add the Domain unit tests for validation rules and the integration test for artist/release CRUD, run everything, and stop. Do NOT build the checklist screens, template management, or album tracks yet (M2+).

No prerequisites; the plan is self-contained.
