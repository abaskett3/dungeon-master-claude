# Implementation Plan: The slip test restores each file from the backup that holds it at the fork

**Branch**: `claude/project-thread-bs1df4` (spec 026) | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

## Summary

`FileHistory` reads the edits from the logs and the unrecorded `@v1` backups from disk. `StateAt` takes the backup
that holds each file at the fork, or refuses the point. `Runner.RestoreFork` passes the edits through.

## Technical Context

**Language/Version**: C# on .NET 10 (`src/Dmc.SlipTest/FileHistory.cs`, `Runner.cs`)

**Testing**: MSTest, `tests/Dmc.Tests/SlipTest/FileHistoryEditTests.cs` (new); the present `FileHistoryTests`

## The rule (research R4, corrected)

A backup holds the file as it was at its `backupTime`. For each file under `adventures/` with a backup after the fork
time T:

1. The earliest backup after T, if no edit of the file falls between T and it.
2. Else the latest backup at or before T, if no edit falls between it and T.
3. Else, if the file has no backup and no edit at or before T, it did not exist at T.
4. Else refuse the point.

A file with no backup after T was not changed through Claude Code's tools since T, so the copy already holds it.
`StateAt` without edits (`edits: null`) keeps spec 024's rule, so the present tests still check it.

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. A fork sees the files as play had them, which is what the trial measures |
| Core 2 Fun | Pass. No change to play |
| Core 3, Development 1 Cost | Pass. 0 bytes to `kit/dm-rules.md` and to every skill; the resume's reads do not change |
| Development 2 Migration | Pass. None needed: no campaign file changes |
| Development 3 One step | Pass. A developer tool, not released |
| Development 4 Grounded | Pass. The format is from the maintainer's logs (#249 pilot comment); spec 024 R4 marked it not verified |
| Development 6 One home | Pass. The rule's home is `FileHistory.StateAt` and its doc comment |
| Development 7 | Pass. The tests use invented logs and history in a temporary folder |
| Documentation 4 | Planned: development.md, decisions.md, roadmap.md, a pointer in spec 024 R4 |
