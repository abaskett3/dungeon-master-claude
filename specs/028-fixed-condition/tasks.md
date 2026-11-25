---
description: "Task list for spec 028, a condition for fixed"
---

# Tasks: A condition for "fixed"

**Input**: `specs/028-fixed-condition/spec.md`, `plan.md`

- [X] T001 Test first: `ToolkitTests.FixedMeansTheSlipStopped`.
- [X] T002 [US1] `kit/templates/feedback.md`: the statuses, and the old one read as released (FR-001, FR-005).
- [X] T003 [US1] `.claude/skills/review-feedback/SKILL.md` § 1, § 2 and § 7: filed, released, fixed (FR-002, FR-003).
- [X] T004 [US2] `kit/skills/dmc-record-feedback/SKILL.md` § 2 and `.claude/skills/review-transcript/SKILL.md` § 6: the
  search and the reopening (FR-004).
- [X] T005 Documents: `.docs/campaign-files.md`, `.docs/user-guide.md`, `.docs/decisions.md`, `.docs/roadmap.md`
  (FR-006).
- [X] T006 `dotnet test` passes.
- [X] T007 Fixes from the code review of PR #282: reopen only on that release or later; the § 7 line gives the `dmc`
  version and date; an older `fixed in PR #N` maps to a release in `review-transcript` too; PRs merged before
  2026-11-26 need no slip-test statement; the failed change is named; campaign-files.md's search.
