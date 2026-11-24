# Implementation Plan: Count every slip in every story session

**Branch**: `claude/project-thread-bs1df4-slip-count` (spec 025) | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

## Summary

A `conduct` category for the `Slip:` line, one rewritten sentence in `kit/dm-rules.md` § the correction paragraph
(byte-neutral), a **Play turns** line in `dmc digest show`'s `stats.md`, and `review-transcript` § 7 rewritten as a
count for every story session, with the #179 gate moved to § 8.

## Technical Context

**Language/Version**: Markdown prompts; C# on .NET 10 (`src/Dmc.Cli/Digest/Session.cs`, `Report.cs`)

**Testing**: MSTest, `tests/Dmc.Tests/Digest/DigestLibraryTests.cs`; the toolkit tests (the byte cap)

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. The new state is a `Slip:` line in the session log, which already holds them |
| Core 2 Fun | Pass. A conduct slip becomes countable, so a fix for it can be measured |
| Core 3, Development 1 Cost | Pass. `kit/dm-rules.md` −3 bytes (the paragraph at line 85 rewritten: "add a `Slip:` line to the session log (its template gives the shape)" became one closing sentence for every slip; "every beat from that point that depended on it" became "every later beat resting on it"; "wherever" became "where"; "if it is unclear" became "if unclear"). Template +125 bytes. Resume reads unchanged |
| Development 2 Migration | Pass. None needed: an optional line's new category |
| Development 3 One step | Pass. Reaches a DMC folder with `dmc update` |
| Development 4 Grounded | Pass. The rule is `kit/dm-rules.md:85`, the template `kit/templates/session-log.md:22`, the digest `src/Dmc.Cli/Digest/Report.cs` `Stats`; `sessionKind` is marked not verified |
| Development 6 One home | Pass. The categories' home is the template; the rule points to it by "`Slip:` line"; the count's home is `review-transcript` § 7 |
| Development 7 | Pass. The new test reads a temporary log only |
| Documentation 4 | Planned: campaign-files, dmc-reference, roadmap, decisions |
