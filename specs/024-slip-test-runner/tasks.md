---
description: "Task list for spec 024, the slip test runner"
---

# Tasks: The slip test, a replay runner that passes its pilot

**Input**: Design documents from `specs/024-slip-test-runner/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/slip-test.md, quickstart.md

**Tests**: Requested by the spec (SC-004). The pure parts are tested on invented logs; nothing in CI runs `claude`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 to US4)

## Phase 1: Setup

- [X] T001 Create `src/Dmc.SlipTest/Dmc.SlipTest.csproj` (console, assembly `slip-test`, warnings as errors,
  `InternalsVisibleTo Dmc.Tests`), add it to `dmc.sln`, and reference it from `tests/Dmc.Tests/Dmc.Tests.csproj`.
- [X] T002 `src/Dmc.SlipTest/SlipTestException.cs` and `Program.cs`: the five commands, `--work`, one-line errors,
  exit codes as the contract says.

## Phase 2: Foundational

- [X] T003 `src/Dmc.SlipTest/LogFile.cs`: read a log's records; find a user record by uuid; say whether it is a
  message the player typed; its text; the model, effort and permission mode in force at it; the session kind; the
  records of the turn after it.
- [X] T004 [P] `src/Dmc.SlipTest/Folders.cs`: the project-folder slug, the guards (distinct, not nested), copying a
  folder and replacing `adventures/`.
- [X] T005 [P] `src/Dmc.SlipTest/Trial.cs`: parse and check `trial.json`, `points.tsv`, `key.tsv`, `allow.tsv`; write
  and check `lock.json`.
- [X] T006 [P] Tests: `tests/Dmc.Tests/SlipTest/SlipTestFixtures.cs`, `TrialTests.cs` (each malformed input refused
  with one line, a changed locked file refused).

## Phase 3: User Story 2, fork mode (P1)

- [X] T007 [US2] `src/Dmc.SlipTest/LogRewriter.cs`: the base log up to the fork message, a new session id, the folder
  path replaced in every string, the recorded `CLAUDE.md` text replaced (the `instructions` attachment, else every
  whole string equal to the recorded text). Tests in `LogRewriterTests.cs`.
- [X] T008 [US2] `src/Dmc.SlipTest/FileHistory.cs`: collect every `file-history-snapshot` of the folder's logs; the
  content of each tracked file at a time (research R4); apply it to the copy's `adventures/`. Tests in
  `FileHistoryTests.cs`: a later edit undone, a file created after the time deleted, a file never edited kept, a
  backup from a later session used, a relative path.
- [X] T009 [US2] `src/Dmc.SlipTest/ClaudeRunner.cs`: the command line (`-p --resume <id> --fork-session --model
  --effort --permission-mode --output-format json`, the message on standard input) and the parse of its output
  (session id, usage, cost). Tests on a saved output.
- [X] T010 [US2] `src/Dmc.SlipTest/Runner.cs`: `prepare` (resolve, copy, lock) and `run` (interleaved arms, skip done
  runs, restore, write base log, run, copy and delete logs, append `runs.tsv`). Test with a fake `claude` that copies
  its resumed log as the fork log.

## Phase 4: User Story 3, fresh-resume mode (P2)

- [X] T011 [US3] In `Runner.cs`: per arm, a fresh copy with the overlay, one base session from the resume message,
  its `adventures/` saved; each fork restores it and resumes the base log. Test with the fake `claude`.

## Phase 5: User Story 1, the pilot (P1)

- [X] T012 [US1] `src/Dmc.SlipTest/PilotCheck.cs`: the four checks of FR-007 and the allow file; `pilot.md` with
  numbers and type names only. Tests in `PilotCheckTests.cs`, one per failure and one documented difference.

## Phase 6: User Story 4, export and score (P1)

- [X] T013 [US4] `src/Dmc.SlipTest/BlindExport.cs`: the replies, shuffled, under random ids; the map; the scores
  file. Tests in `BlindExportTests.cs`: no arm name, no tool text, no hook text.
- [X] T014 [US4] `src/Dmc.SlipTest/Scoring.cs`: rates, the paired bootstrap over fork points (seed 249), the pass
  line, the cost. Tests in `ScoringTests.cs` on a worked example.

## Phase 7: Polish

- [X] T015 [P] Documents: `.docs/development.md` (section and layout row), `.docs/architecture.md`,
  `.docs/roadmap.md`, `.docs/decisions.md`.
- [X] T016 `dotnet build` with no warnings, `dotnet test` passes; every type and member has an XML comment.
- [ ] T017 The pilot and the base rate on the maintainer's computer (quickstart Q1, Q2); the numbers as comments on
  #249. Needs the maintainer's logs.
