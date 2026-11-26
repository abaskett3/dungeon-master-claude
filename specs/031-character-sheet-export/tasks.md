---

description: "Task list for spec 031, printable character sheets (issue #191 phase 1)"
---

# Tasks: Printable character sheets, `dmc sheet` (phase 1)

**Input**: Design documents from `specs/031-character-sheet-export/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md, contracts/dmc-sheet.md

**Tests**: requested by the spec (FR-017).

## Phase 1: User Stories 1 and 3 (the export)

- [X] T001 [US1] `Sheets/SheetText.cs`: comments removed, sections, labelled fields, tables (FR-009).
- [X] T002 [US1] `Sheets/SheetLayouts.cs`: the three layouts, the template label lists, the not-exported list (FR-008,
  FR-014).
- [X] T003 [US1] `Sheets/SheetMarkdown.cs` and `Sheets/SheetHtml.cs`: the page, escaped first (FR-005, FR-016).
- [X] T004 [US1] [US3] `Sheets/SheetExport.cs`, `Sheets/SheetArguments.cs`, `Commands/SheetCommand.cs`,
  `CommandTable.cs`: choice by exact name, the write, the output (FR-001 to FR-006).
- [X] T005 [US1] `kit/templates/pc-sheet.md`: `Other skills` (FR-007).

## Phase 2: User Story 2 (the check)

- [X] T006 [US2] `Sheets/SheetCheck.cs` and `Sheets/SheetNumbers.cs`: the findings, the arithmetic and Party status
  warnings (FR-010, FR-012, FR-013).

## Phase 3: Tests

- [X] T007 `tests/Dmc.Tests/Fixtures/sheets/`: an invented campaign with a spoiler word in every field that must not
  print, and the golden party page.
- [X] T008 `Sheets/SheetCommandContract.cs` (in-process and binary), `Sheets/SheetCases.cs` and the rows in
  `TokenCost/ErrorPathTests.cs`, `Sheets/SheetLibraryTests.cs` (the label map, FR-011),
  `Playwright/SheetPageTests.cs` (FR-017).

## Phase 4: Polish

- [X] T009 `kit/skills/dmc-character-sheet-update/SKILL.md`: § Showing a sheet to the player, and its description
  (FR-015).
- [X] T010 Documents (FR-018).
- [X] T011 Run quickstart steps 1 and 3; step 2 on the invented campaign.
- [X] T012 If the change alters how DMC plays (CLAUDE.md § Git and GitHub, "Slip test", says what counts), run the
  slip test against the pass line in the issue and put the result in the PR: it runs on the maintainer's computer
  (research R5); the PR states the trial.
- [X] T013 Converge: spec, plan and tasks against the diff.
- [X] T014 Code review on the PR: a bonus in brackets or after a colon is read; a value wrapped onto an unindented
  next line continues its field; a party block that names two sheets no longer stops a PC's export by name; a history
  file's `## ` headings render as headings; the unused `FreeSections` list is gone and the layouts' doc names
  `SheetLibraryTests`; one character's page never takes the party's or the several-name page's file name. Tests added.
- [X] T015 Converge again after T014; no task added.

## Phase 5: The slip test's point A

- [X] T016 `src/Dmc.Cli/ContextCheck/SheetRequestReminder.cs` and its place last in `Commands/ContextCheckCommand.cs`
  (FR-019); `ContextCheck/SheetRequestReminderContract.cs` (in-process and binary).
- [X] T017 `dmc-character-sheet-update`'s description; `.docs/dmc-reference.md`, `architecture.md`, `roadmap.md`,
  `decisions.md`.
- [ ] T018 Run point A of the slip test again on the maintainer's computer (spec § The slip test, Result and rerun) and
  put the result on the PR.
