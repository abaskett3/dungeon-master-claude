---

description: "Task list for spec 022, one kit rule for how far DMC reads a sheet"
---

# Tasks: One kit rule for how far DMC reads a sheet

**Input**: Design documents from `specs/022-one-sheet-read-rule/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: requested by the spec (FR-003, FR-005).

## Phase 1: Setup

- [X] T001 Note the byte size of `kit/dm-rules.md` before the change (32,510, at its cap) and its line count (212).

## Phase 2: User Story 1 - One answer to "how far do I read this sheet?" (P1)

- [X] T002 [US1] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, add `ThePresentLineReadsASheetAsTheResumeDoes` with an
  XML doc comment (FR-001 to FR-003); it fails before T003.
- [X] T003 [US1] In `kit/dm-rules.md:78`, replace "down to its History" in the Present-line sentence with "through its
  play card (if none, down to its History)" (FR-001, FR-002).
- [X] T004 [US1] In the same bullet, drop " as before" from the recall fallback and ": it is closed for good" from the
  closed-campaign rule (FR-004, research R2); check the file is 32,510 bytes and 212 lines.
- [X] T005 [US1] Keep `DmRulesBytesAllowed` at 32,510 and record spec 022 in its doc comment; name specs 014 to 022 in
  the cap test's message (FR-004).
- [X] T006 [US1] In `tests/Dmc.Tests/Toolkit/FeedbackRoundTests.cs`, drop the assertion of the old words and point the
  doc comment at the new test (FR-005).

## Phase 3: Polish

- [X] T007 Add the entry to `.docs/decisions.md` and the row to `.docs/roadmap.md` (FR-006).
- [X] T008 Run quickstart Q1; record the result in `quickstart.md`.
- [X] T009 Converge: check spec, plan and tasks against the diff; no task added.
- [X] T010 Code review of PR #273: replace the pointer ("as resume step 2.5 does") with the extent in the rule's own
  words (research R1), update the test, the spec folder, the decision and the roadmap.
- [X] T011 Converge again after T010; no task added.
