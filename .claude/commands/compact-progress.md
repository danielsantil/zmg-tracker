---
description: Compact plans/PROGRESS.md after a build version completes — collapse its milestones into one dense journal entry.
argument-hint: [version, e.g. v2.1] (optional — inferred from Current state if omitted)
---

Compact `plans/PROGRESS.md` now that a build version is complete. Target version: **$ARGUMENTS**
(if blank, infer the just-completed version from the "Current state" line and confirm it with me
before editing).

PROGRESS.md carries exactly two things the rest of the repo can't: **current state** and
**cross-cutting knowledge that lives in no single build plan**. Everything else has a better home —
scope/rationale/wireframes/test lists are in `build-plan-*.md`, commands are in `README.md` /
`CLAUDE.md`, and anything a developer would learn by opening the source doesn't belong here at all.
Compaction means enforcing that split, not just shortening prose.

**The file's target shape** (sections in this order, nothing else):
header + plan-version list → **Current state** → schema-reset warning → **Journal** →
**Cross-cutting decisions** → **Project layout** → **Backlog / next steps**.

1. **Journal — one SHORT entry per version, not per milestone.** Collapse every per-milestone and
   post-version/fix paragraph for the target version into a **single 2–4 sentence** entry:
   `**vN.N (M#–M#) — <theme>.** <what shipped, in one or two sentences.>` plus, at most, one sentence
   for a fix or reversal that a future reader would otherwise trip over. It names *what* shipped;
   the build plan holds the *how*, and the git history holds the blow-by-blow. Link the build plan.
   Match the density of the oldest `v1` / `v1.1` entries — if a new entry is longer than those, it's
   not done. Drop file links, class names, prop names, CSS details, and verification narration; if a
   detail feels too valuable to lose, it's either a cross-cutting rule (rule 2) or it isn't valuable.
2. **Promote durable knowledge before deleting — this is the one irreversible step.** Any rule,
   contract, invariant, or hard-won trap in the removed detail (schema invariants, API contracts,
   naming conventions, "never do X" rules, environment gotchas that cost real debugging) moves up into
   **## Cross-cutting decisions**. Compaction may lose *narrative*; it must never lose a *rule*.
3. **Cross-cutting decisions — tight bullets, no essays.** Each bullet is **1–3 lines**: the rule
   first, then only the "why" a reader couldn't reconstruct. Merge bullets covering one topic. Cut
   anything that (a) is already in `CLAUDE.md` / `README.md`, (b) is plainly visible in the source, or
   (c) restates a build plan. Keep the traps — a rule that stops a future bug earns its lines. When
   promoting from the journal, fold it into an existing bullet where one fits rather than appending.
4. **Backlog / next steps — names, not descriptions.** Exactly:
   - a one-line **Shipped** entry for the just-completed version: version + milestone range + bare
     milestone names, **no descriptions** (the journal and the build plan already explain them);
   - the **next milestones to build**, as the clear new head;
   - standing longer-term items (e.g. Phase 2 — DSP stats) and genuinely open deferrals.
   Delete everything the completed version delivered, plus per-milestone status bullets, "branch X
   exists" notes, and any item now done.
5. **Refresh "Current state"** — what's shipped, what's next, in a few lines. Run `dotnet test` and
   update the real counts (domain N / API N) if cited; if the version was SPA-only, say so rather than
   re-running the suite for a number that didn't move.
6. **Delete what other files own.** No `## Run` section (commands live in `README.md` / `CLAUDE.md`),
   and no tooling/env notes that are inferable from the repo (linter choice and config, pinned
   versions, package layout). Keep an env note **only** if it encodes a trap that would otherwise be
   re-discovered the hard way — and then it belongs in Cross-cutting decisions, not a footer.
7. **Fix stale references** exposed by the compaction — renamed files, dropped types, old endpoint
   paths, outdated "next up" pointers, plan-version lines still saying "next".
8. **Do not touch `plans/build-plan-*.md`** — they stay frozen. Only PROGRESS.md changes.

When done, show me a short summary: what collapsed, what got promoted to Cross-cutting decisions, and
anything you deleted that wasn't purely redundant. Then stage and commit in the existing style:
`docs: compact PROGRESS — collapse <version> milestones into one entry` (body noting refreshed state /
test counts / promoted decisions). Do not push — leave that to me.
