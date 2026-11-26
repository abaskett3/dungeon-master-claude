---
description: "Task list for spec 029, the slip test puts per-point text next to the player's message"
---

# Tasks: The slip test puts per-point text next to the player's message

**Input**: `specs/029-slip-test-context/spec.md`, `plan.md`

- [X] T001 [US1] [US3] Tests first, `tests/Dmc.Tests/SlipTest/ContextTests.cs`: `AnArmWithContextGetsItsTextAtEachPoint`,
  `TheLocalSettingsAreAsTheDmcFolderHasThemAfterARun`, `TheContextCommandPrintsTheFile`, `TheHookCommandUsesForwardSlashes`,
  `PrepareRefusesAContextFolderWithoutAPointsFile`, `AContextFileChangedAfterPrepareStopsTheRun`, `ContextNeedsForkMode`.
- [X] T002 [US1] `src/Dmc.SlipTest/Trial.cs`: `context` read, checked and locked (FR-001, FR-004).
- [X] T003 [US1] `src/Dmc.SlipTest/ContextHook.cs` and `Runner.cs`: the hook around each fork run (FR-002, FR-003);
  `Program.cs`: the `context` command.
- [X] T004 [US2] Tests first: `ThePilotPassesWhenTheContextArrived`, `ThePilotFailsWhenTheContextIsMissing`; then
  `PilotCheck.cs` (FR-005).
- [X] T005 Documents: `specs/024-slip-test-runner/contracts/slip-test.md`, `.docs/development.md` § The slip test,
  `.docs/decisions.md`, `.docs/roadmap.md` (FR-006).
- [X] T006 `dotnet test` passes.
- [X] T007 Fixes from the code review, tests first: the hook check leaves out every record of the context hook
  (`TheHookCheckLeavesOutEveryRecordOfTheContextHook`); `prepare` refuses a context file over 10,000 characters and
  local settings that do not parse; paths are single-quoted (`TheQuotedPathReachesTheProgramUnchangedThroughAShell`);
  `context` prints Claude Code's JSON form, so a file that is JSON is still text.
