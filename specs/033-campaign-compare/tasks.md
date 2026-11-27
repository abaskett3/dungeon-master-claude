---
description: "Task list for spec 033, dmc campaign compare (issue #189)"
---

# Tasks: Compare a campaign with any export of it, `dmc campaign compare`

**Input**: Design documents from `specs/033-campaign-compare/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md, contracts/campaign-compare.md

**Tests**: requested by the spec (SC-001, SC-002).

## Phase 1: User Stories 1 and 2 (the verb)

- [X] T001 [US1] [US2] `Campaign/CampaignComparer.cs`: the comparison, the default file, the note, the cap (FR-001,
  FR-002, FR-005, FR-006, FR-008).
- [X] T002 [US2] `Campaign/CampaignArguments.cs`: the verb, `--with` and `--all`, `CompareRequest` (FR-004).
- [X] T003 [US1] `Commands/CampaignCommand.cs`: the help text, the summary, `RunCompare` after `Recover`.

## Phase 2: User Story 3 (a closed campaign's closing backup)

- [X] T004 [US3] `Campaign/BackupFolder.cs` `ClosingPath` and `CampaignVerifier.ClosingBackup`: `backups/`, then
  `exports/`; `VerifyClosed` uses it (FR-003, FR-007).

## Phase 3: Tests

- [X] T005 `tests/Dmc.Tests/Campaign/CampaignCompareTests.cs`: every acceptance scenario and edge case, with a snapshot
  of `adventures/` and `exports/` before and after.
- [X] T006 `CampaignVerifyClosedTests`: the closing backup moved out of `backups/` verifies from `exports/`, then the new
  message; `CampaignCases`: the argument mistakes of `compare` for the one-line guard.

## Phase 4: Documentation

- [X] T007 `.docs/dmc-reference.md` (usage, § Comparing a campaign with an export, the closed verify, the verb list),
  `.docs/user-guide.md`, `.docs/campaign-files.md`, `.docs/architecture.md`, `.docs/roadmap.md`, `.docs/decisions.md`
  (FR-009).
