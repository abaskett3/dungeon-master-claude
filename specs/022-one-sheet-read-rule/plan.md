# Implementation Plan: One kit rule for how far DMC reads a sheet

**Branch**: `claude/project-thread-86m1m7` (spec 022) | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/022-one-sheet-read-rule/spec.md` (issue #247, Phase 0 of #238)

## Summary

`kit/dm-rules.md:78` and resume step 2.5 disagree on how far to read a companion's sheet. The Present-line rule now
points at resume step 2.5 instead of giving its own extent, a vestigial " as before" goes from the same bullet, and a
toolkit test guards the pointer. `kit/dm-rules.md` shrinks by 6 bytes.

## Technical Context

**Language/Version**: Markdown kit text; C# tests on .NET 10 (`tests/Dmc.Tests`)

**Primary Dependencies**: none new

**Storage**: none

**Testing**: MSTest, `tests/Dmc.Tests/Toolkit/`

**Target Platform**: a DMC folder (the rules become its `CLAUDE.md`)

**Project Type**: kit change with toolkit tests

**Constraints**: `kit/dm-rules.md` must not grow (plan #238; `DmRulesBytesAllowed`)

**Scale/Scope**: one line of `kit/dm-rules.md`, two test files, two documents

## Constitution Check

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | Names which files hold new state | Pass. No new state |
| Core 2 Fun | Says what the player gains | Pass. One read rule; fewer tokens where it was the larger read |
| Core 3 Low cost | Cost counted | Pass. 6 bytes less per turn (research R4) |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill | Pass. 0 lines, -6 bytes; no skill changes; the resume's reads do not change, so no `dmc size` result |
| Development 2 No change breaks a campaign | Migration or why none | Pass. No file shape changes |
| Development 3 One step | Install or update gains a step | Pass. `dmc update` rewrites `CLAUDE.md` |
| Development 4 Grounded | Claims cite a file | Pass. research.md; the log figures are cited from #247 |
| Development 6 One home | Names the home of each rule | Pass. The home of how far a sheet is read is resume step 2.5; `kit/dm-rules.md` points at it (R1) |
| Development 7 Tests skip locally | `TestEnv.Need` where a tool is needed | Pass. The test reads files only |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 4 | Concise; the PR names its documents | Planned. See Design 4 |

No violations.

## Design

1. **`kit/dm-rules.md:78`**: "read their sheet down to its History, once per session part" becomes "read their sheet
   as resume step 2.5 does, once per session part"; "search `canon.md` and those archives as before, and in a sequel"
   becomes "search `canon.md` and those archives, and in a sequel".
2. **`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`**: a new test `ThePresentLineReadsASheetAsTheResumeDoes` (FR-001 to
   FR-003), before `NarrationOffersTheJumpByTheBreakTest`; `DmRulesBytesAllowed` to 32,504 with its doc comment
   extended, and the cap test's message naming specs 014 to 022.
3. **`tests/Dmc.Tests/Toolkit/FeedbackRoundTests.cs`**: the assertion of the old words goes; the doc comment of
   `TheRulesCarryTheClausesOfAAndB` points at the new test.
4. **Documents**: `.docs/decisions.md`, a new entry "The Present-line read of a sheet points at the resume (issue
   #247, spec 022)"; `.docs/roadmap.md`, a row. No other document states the old extent (`grep "down to its History"
   .docs` finds nothing).

## Project Structure

```text
specs/022-one-sheet-read-rule/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md
```

No data model or contract: the change is one sentence of prompt text.

## Complexity Tracking

None.
