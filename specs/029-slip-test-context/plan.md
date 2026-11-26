# Implementation Plan: The slip test puts per-point text next to the player's message

**Branch**: `claude/project-thread-bs1df4` (spec 029) | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

## Summary

`Trial` reads `context` and locks its files. A new `ContextHook` adds and removes the hook in the copy's
`.claude/settings.local.json` and builds its command. `Runner` calls it around each fork run. `Program` gains the
`context <file>` command the hook runs. `PilotCheck` leaves the text out of its hook check and adds the `context`
check.

## Technical Context

**Language/Version**: C# on .NET 10 (`src/Dmc.SlipTest/`)

**Testing**: MSTest, `tests/Dmc.Tests/SlipTest/ContextTests.cs` (new). The stand-in for Claude Code reads the copy's
`settings.local.json` and records the text a context hook names, as Claude Code would.

## Design

- **Where the text comes from.** `<work>/<folder>/<point>.md`, printed by the hook from the work folder, so no file
  in the copy folder holds it.
- **Why a hook, not the message.** #270 asks for the text "in the place where the hook's text arrives". A hook
  output is recorded as an attachment, as the kit's is; text added to the message would be the player's.
- **Why `settings.local.json`.** It is the project's local settings, which the copy has from the DMC folder and which
  the kit does not write. The runner restores it from the DMC folder before every run, so a run that was killed leaves
  nothing behind for the next.
- **The command.** `Environment.ProcessPath` is the `slip-test` apphost under `dotnet run`. When it is `dotnet`
  itself, the command is `dotnet <Dmc.SlipTest.dll>`. Paths use `/` and are quoted, as the kit's own hook is run by
  a POSIX shell (`2>/dev/null` in `kit/settings.json`).

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. The oracle test measures whether correct facts next to the reply decrease fact slips |
| Core 2 Fun | Pass. No change to play |
| Core 3, Development 1 Cost | Pass. 0 bytes to `kit/dm-rules.md` and to every skill |
| Development 2 Migration | Pass. None needed: no campaign file changes |
| Development 3 One step | Pass. A developer tool, not released |
| Development 4 Grounded | Pass. #270 names the place; the hook format is the kit's (`kit/settings.json`). The live behaviour is not verified here, and SC-001 checks it |
| Development 6 One home | Pass. The hook's home is `ContextHook` |
| Development 7 | Pass. The tests use invented logs in a temporary folder |
| Documentation 4 | Planned: spec 024's contract, development.md, decisions.md, roadmap.md |
