---
description: Compact plans/PROGRESS.md after a build version completes — collapse its milestones into one dense journal entry.
argument-hint: [version, e.g. v2.1] (optional — inferred from Current state if omitted)
---

Compact `plans/PROGRESS.md` now that a build version is complete. Target version: **$ARGUMENTS**
(if blank, infer the just-completed version from the "Current state" line and confirm it with me
before editing).

Follow the established compaction pattern (see commit `6caeb42` for the reference example):

1. **Collapse the target version's Journal entries into ONE dense paragraph.** Fold every
   per-milestone paragraph (e.g. M16, M17…) and any "post-vX" / fix paragraphs for that version into
   a single entry, matching the one-paragraph-per-version density of the older `v1` / `v1.1` / `v1.2`
   entries. Keep the key file links; drop blow-by-blow detail.
2. **Preserve durable knowledge — don't just delete it.** Any cross-cutting rule, contract, or
   decision buried in the removed detail (schema invariants, API contracts, naming conventions) moves
   up into the **## Cross-cutting decisions** section rather than being lost.
3. **Refresh the "Current state" line** at the top: what's shipped, and what's next. Run
   `dotnet test` and update the real test counts (domain N / API N) if they're cited.
4. **Fix stale references** exposed by the compaction — renamed files, dropped types, old endpoint
   paths, outdated "next up" pointers.
5. **Keep "Backlog / next steps" accurate** — remove anything the completed version delivered; make
   sure the next milestone is clearly the new head.
6. **Do not touch the `plans/build-plan-*.md` files** — they stay frozen. Only PROGRESS.md changes.

When done, show me a short summary of what collapsed and what got promoted to Cross-cutting decisions,
then stage and commit with a message in the existing style:
`docs: compact PROGRESS — collapse <version> milestones into one entry` (plus a body noting refreshed
state / test counts / promoted decisions). Do not push — leave that to me.
