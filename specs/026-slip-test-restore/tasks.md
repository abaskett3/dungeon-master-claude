---
description: "Task list for spec 026, the slip test restores each file from the backup that holds it at the fork"
---

# Tasks: The slip test restores each file from the backup that holds it at the fork

**Input**: `specs/026-slip-test-restore/spec.md`, `plan.md`

- [X] T001 [US1] Tests first, `tests/Dmc.Tests/SlipTest/FileHistoryEditTests.cs`:
  `AnEditBeforeTheNextBackupTakesTheBackupBefore`, `TheUnrecordedFirstBackupIsFoundOnDisk`,
  `AFileAWriteCreatedLaterDidNotExist`, `AFileNoBackupHoldsIsRefused`.
- [X] T002 [US1] `src/Dmc.SlipTest/FileHistory.cs`: `FileEdit`, `CollectEdits`, the `@v1` backups in `Collect`, and
  `StateAt` with the edits (FR-001 to FR-004).
- [X] T003 [US2] `StateAt` refuses a point no backup can restore (FR-005).
- [X] T004 `src/Dmc.SlipTest/Runner.cs`: `States` computes every point's state before the first run (FR-005), and
  `RestoreFork` applies it.
- [X] T005 Documents: `.docs/development.md` § The slip test, `.docs/decisions.md`, `.docs/roadmap.md`, and a pointer
  in `specs/024-slip-test-runner/research.md` R4 (FR-006).
- [X] T006 `dotnet test` passes.
- [X] T007 Fixes from the code review of PR #279, tests first: `AnEditNoBackupRecordsYetTakesTheBackupBefore`,
  `AFileAWriteMadeWithNoBackupDidNotExist`, `AFileFirstEditedAfterTheForkWithNoBackupBeforeIsRefused`,
  `ARunRefusesEveryUnrestorablePointBeforeAnyCall`; `StateAt` also reads files that only edits name, takes a missing
  file only after a `Write`, and the edits are looked up by file; a relative path with no `cwd` is skipped (FR-007).
