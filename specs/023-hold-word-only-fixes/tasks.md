---

description: "Task list for spec 023, hold fixes from feedback that only add words"
---

# Tasks: Hold fixes from feedback that only add words

**Input**: Design documents from `specs/023-hold-word-only-fixes/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: requested by the spec (FR-005).

## Phase 1: User Stories 1 to 3 (P1, P1, P2)

All three stories are one paragraph and one sentence of the same skill, so they run in order.

- [X] T001 [US1] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, add `ReviewFeedbackHoldsWordOnlyFixesUntilTheSlipTest`
  with an XML doc comment (FR-005); it fails before T002.
- [X] T002 [US1] [US2] [US3] In `.claude/skills/review-feedback/SKILL.md` § 6, add the hold paragraph (FR-001 to
  FR-003).
- [X] T003 [US1] In § 7, add that a held entry is not acted on unless the user lifts the hold (FR-004).

## Phase 2: Polish

- [X] T004 `.docs/decisions.md`, `.docs/roadmap.md`, `.docs/development.md` (FR-006).
- [X] T005 Run quickstart Q1; record the result.
- [X] T006 Converge: spec, plan and tasks against the diff; no task added.
