---

description: "Task list for spec 030, a wardrobe and keepsakes section on sheets"
---

# Tasks: A wardrobe and keepsakes section on sheets

**Input**: Design documents from `specs/030-wardrobe-and-keepsakes/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: requested by the spec (FR-007).

## Phase 1: User Stories 1 and 2 (P1)

- [X] T001 [US1] [US2] Add `## Wardrobe and keepsakes` with its comment before `## History`, and label Looks "worn
  now", in `kit/templates/npc-sheet.md` and `kit/templates/pc-sheet.md`; narrow the PC's `Stored elsewhere` (FR-001,
  FR-002).
- [X] T002 [US1] [US2] `kit/skills/dmc-character-sheet-update/SKILL.md`: step 8 and the linked-campaign phrase (FR-003).
- [X] T003 [US2] `dmc-generate-image-prompt` and `dmc-campaign-full-save` (FR-004).
- [X] T004 [US1] Toolkit test `SheetsKeepAWardrobeAndAChangeOfClothesIsASwap` (FR-007).

## Phase 2: User Story 3 (P2)

- [X] T005 [US3] `kit/migrations/009-wardrobe-and-keepsakes.md` (FR-006).
- [X] T006 [US3] `wardrobe-sections-in-place` in `src/Dmc.Cli/Campaign/MigrationChecks.cs`, sharing the sheet list with
  `companions-have-play-card`; its tests in `MigrationChecksTests.cs` (FR-006, FR-007).

## Phase 3: Polish

- [X] T007 Documents: `.docs/campaign-files.md`, `.docs/dmc-reference.md`, `.docs/roadmap.md`, `.docs/decisions.md`
  (FR-008).
- [X] T008 Run quickstart steps 1 and 2.
- [ ] T009 If the change alters how DMC plays (CLAUDE.md § Git and GitHub, "Slip test", says what counts), run the
  slip test against the pass line in the issue and put the result in the PR: it runs on the maintainer's computer
  (research R4); the PR states the trial.
- [ ] T010 Converge: spec, plan and tasks against the diff.
- [ ] T011 Code review on the PR; fixes; converge again.
