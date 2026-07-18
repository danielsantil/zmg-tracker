# ZMG Release Tracker — Build Plan v2.4 (UI polish · dark/light groundwork)

Delta on top of [build-plan-2.3.md](build-plan-2.3.md). Continues milestone numbering from M25 →
**M26–M27**.

## Context

v2.3 paid down debt. v2.4 is a **SPA-only** pass over `src/Zmg.Web` — **no** API, DTO, domain, or
migration changes, so per CLAUDE.md the blast radius is `npm run lint` + `npm run build` + a browser
drive; **no `dotnet test`**. Two milestones:

- **M26 — UI polish:** four independent, user-visible fixes to the tables, the row-actions kebab, the
  mobile Releases table, and the home release cards.
- **M27 — Semantic color tokens:** route the hardcoded dark-tuned neutrals through
  theme-swappable tokens, so a **later** dark/light-mode plan becomes a values-only override instead
  of a 43-file rewrite. This milestone changes **no visible appearance**.

Decisions locked with the user: icon library = **lucide-react** (over Font Awesome / hand-rolled
SVG); mobile tables = **fold Type/Status into the Name cell** (over keep-horizontal-scroll).

---

## M26 — UI polish

### 1. lucide-react + the kebab glyph
- Add `lucide-react` to `src/Zmg.Web/package.json`.
- `components/RowMenu.tsx:66` — replace the literal `⋮` with
  `<EllipsisVertical className="h-4 w-4" aria-hidden />`. Keep the button's `aria-label` (the icon is
  decorative). `RowMenu` is the **one** kebab primitive, so this updates the glyph **app-wide** across
  all five call sites: `AllReleasesPage.tsx:141`, `CatalogPage.tsx`, `ArtistsPage.tsx`,
  `ReleaseCard.tsx:49`, `TaskRow.tsx`.
- Scope stays on the kebab. The other literal glyphs (`ReorderArrows` ↑/↓, Tracklist `✕`,
  `Open release →`) are **left as-is**; lucide is now available if we want them later (see Not in scope).

### 2. Row-actions column: drop the header, align the kebab to the row end
The action column has a visible header — `"Action"` in four tables, `"Actions"` in Artists
(inconsistent) — and the kebab is left-packed in its cell via a `w-fit` wrapper.

- **`components/DataTable.tsx`** — evolve `headers: string[]` →
  `headers: { label: string; className?: string }[]`; render
  `<th className={clsx('px-4 py-3 font-medium', h.className)}>{h.label}</th>` (uses the `clsx`
  adopted in M24). This one API change unlocks headerless, right-aligned, **and** responsive-hidden
  columns (the latter used by task 3).
- All five list tables move to the object form (they're touched by tasks 2–3 anyway):
  `AllReleasesPage.tsx:116`, `ArchivedReleasesPage.tsx`, `CatalogPage.tsx`, `ArchivedSongsPage.tsx`,
  `ArtistsPage.tsx`. Action column header → `{ label: '', className: 'text-right' }`.
- Each action `<td>` gets `text-right`; swap the kebab wrapper `w-fit` → `flex justify-end` so the
  button sits at the row's end (e.g. `AllReleasesPage.tsx:140`). This also retires the
  "Action"/"Actions" inconsistency (label is now empty).
- `ReleaseCard.tsx` already end-aligns its kebab inside `flex items-start justify-between` — no layout
  change, it just inherits the new icon from task 1.

### 3. Mobile Releases table: fold Type/Status into the Name cell
Below `sm`, hide the **Type** and **Status** columns and surface those badges under the release name,
so the end-aligned kebab (task 2) is reachable without sideways scroll. `overflow-x-auto` (the M22
mobile-clipping fix) stays as the safety net.

- `AllReleasesPage.tsx` — Type/Status header entries get `className: 'hidden sm:table-cell'`; their
  `<td>`s (`:132–134`, `:136–138`) get the same. Inside the Name `<td>`, add a mobile-only row under
  the artist sub-line: `<div className="mt-1 flex items-center gap-1.5 sm:hidden">` containing
  `<TypeBadge type={r.type} />` + `<StatusBadge status={r.status} />`.
- `ArchivedReleasesPage.tsx` — same fold for its **Type** column (it has no Status; Status already
  shows inline next to the name there).
- Catalog / Artists have no Type/Status and few columns — **left on horizontal scroll**; reassess only
  if the browser check shows them cramped.

### 4. Even home release cards with covers
**Cause:** the home grid (`HomePage.tsx:82`, `grid gap-4 sm:grid-cols-2 lg:grid-cols-3`) defaults to
`align-items: stretch`, so every card in a row grows to the tallest. `ReleaseCard`'s body is `flex-1`
with the progress bar pinned via `mt-auto` (`ReleaseCard.tsx:84`), so the extra height shows as an
empty gap. The cover is already a stable `aspect-[16/9]`; it just makes the taller card more obvious.

**Fix (no redesign):** add `items-start` to the home grid container so each card sizes to its own
content. One-line change in `HomePage.tsx`. `ReleaseCard` markup is untouched, so the calendar preview
(its other consumer via `ReleaseCalendar`) is unaffected.

---

## M27 — Semantic color tokens (dark/light groundwork)

Today: 4 flat-hex custom colors (`ink`/`panel`/`edge`/`accent`), **128 `text-slate-*` + 36
`text-white`** hardcoded across **43 files**, no CSS variables, no `darkMode`. Goal: route colors
through **semantic, theme-swappable tokens with zero change to today's appearance**.

**Approach — CSS variables as space-separated RGB channels + the `<alpha-value>` pattern in Tailwind**,
which preserves the opacity modifiers already in use (`bg-ink/80`, `border-edge/50`,
`hover:bg-edge/40`).

**Token names (final):** keep the role-based `ink`/`panel`/`edge`/`accent` (only their definition
moves to vars — their 99 usages don't churn), and add four text tokens named `strong` / `body` /
`muted` / `subtle` (so the utilities are `text-strong` / `text-body` / `text-muted` / `text-subtle` —
short, no `text-base` font-size clash).

### 1. Define the tokens
- **`src/index.css`** — under `:root`, current dark values as channels:
  ```css
  :root {
    color-scheme: dark;             /* unchanged; the future dark/light plan flips this */
    --ink: 15 17 21;   --panel: 23 26 33;   --edge: 37 42 52;   --accent: 83 136 199;
    --strong: 255 255 255;          /* white / slate-100 */
    --body: 226 232 240;            /* slate-200/300 — default body text */
    --muted: 148 163 184;           /* slate-400 */
    --subtle: 100 116 139;          /* slate-500/600 */
  }
  ```
  Change the `body` rule's `text-slate-200` → `text-body`.
- **`tailwind.config.js`** — back every color with its var via
  `<name>: 'rgb(var(--<name>) / <alpha-value>)'` for `ink`/`panel`/`edge`/`accent` **and** the four
  text tokens. Fold the working-tree `accent` tweak (`#7c5cff` → `#5388c7`) into `--accent`.

### 2. Remap the neutral text utilities (mechanical, ~43 files)

| Current | → utility |
|---|---|
| `text-white`, `text-slate-100` | `text-strong` |
| `text-slate-200`, `text-slate-300` | `text-body` |
| `text-slate-400`, `text-gray-400` | `text-muted` |
| `text-slate-500`, `text-slate-600` | `text-subtle` |
| `hover:text-slate-200` | `hover:text-body` |

Enumerate the full file set during implementation via
`grep -rE 'text-(white|slate-[0-9]+|gray-400)' src/Zmg.Web/src`.

**Stragglers (~4, case-by-case, not table-driven):** `bg-slate-700`, `bg-slate-500`, `ring-slate-500`,
`bg-black` — inspect each, map to nearest token (`panel`/`edge`) or leave if intentional.

**Out of scope this milestone:** status colors (`amber`/`red`/`emerald`/`green` — badges/warnings)
stay raw; they're better tokenized alongside the dark/light plan when both themes' badge treatments are
designed together.

**Payoff (record in PROGRESS):** after v2.4, light mode = add `darkMode: 'class'` (or `[data-theme]`)
to the config, add a `:root[data-theme='light'] { --ink: …; … }` override with light channels, and a
toggle reusing `usePersistedState` (like the calendar/table toggle). No JSX changes.

---

## Not in scope (deliberate)

- **The actual dark/light toggle** — M27 is groundwork only; flipping `color-scheme`, adding the light
  override block, and the toggle UI are the follow-up plan.
- **Tokenizing status colors** — amber/red/emerald/green stay raw until the theme work needs them.
- **Upgrading the non-kebab glyphs** (`ReorderArrows` ↑/↓, Tracklist `✕`, `Open release →`) — lucide
  is available; not done here.

---

## Files at a glance

**Modified (M26):** `src/Zmg.Web/package.json` (+lucide-react), `components/RowMenu.tsx`,
`components/DataTable.tsx`, `features/releases/AllReleasesPage.tsx`,
`features/releases/ArchivedReleasesPage.tsx`, `features/catalog/CatalogPage.tsx`,
`features/catalog/ArchivedSongsPage.tsx`, `features/artists/ArtistsPage.tsx`,
`features/home/HomePage.tsx`.
**Modified (M27):** `src/index.css`, `tailwind.config.js`, plus the ~43 files carrying neutral text
utilities (mechanical remap — full set enumerated during implementation via grep).

---

## Verification

SPA-only (no API/DTO/domain change) → no `dotnet test`.

1. `cd src/Zmg.Web && npm install` (picks up lucide-react), then `npm run lint` + `npm run build` —
   must stay green; the M27 remap is the main lint/type risk.
2. Drive the app (preview tools, dev server) at desktop **and** 375px:
   - kebab shows the lucide icon everywhere; menus still open / flip-up / close on scroll;
   - action columns have no header text and the kebab sits at each row's end;
   - **mobile Releases:** Name + Released Date + kebab only, Type/Status folded under the name, kebab
     reachable without horizontal scroll;
   - **home:** cards in a row are equal height with no empty gap when covers render;
   - **M27 is a visual no-op:** colors look identical to before the remap (spot-check a few pages).
3. `read_console_messages` — no new errors.
4. Update `plans/PROGRESS.md`: journal entry for v2.4 (M26–M27).
