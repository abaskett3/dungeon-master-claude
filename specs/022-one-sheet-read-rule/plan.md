# Implementation Plan: One kit rule for how far DMC reads a sheet

**Branch**: `claude/project-thread-86m1m7` (spec 022) | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/022-one-sheet-read-rule/spec.md` (issue #247, Phase 0 of #238)

## Summary

`kit/dm-rules.md:78` and resume step 2.5 disagree on how far to read a companion's sheet. The Present-line rule now
says "through its play card (if none, down to its History)", as the step does; two cuts in the same bullet that change
no instruction pay for it; a toolkit test holds the rule and the step together. `kit/dm-rules.md` stays at its cap.

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
| Core 3 Low cost | Cost counted | Pass. 0 bytes per turn (research R4) |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill | Pass. 0 lines, 0 bytes; no skill changes; the resume's reads do not change, so no `dmc size` result |
| Development 2 No change breaks a campaign | Migration or why none | Pass. No file shape changes |
| Development 3 One step | Install or update gains a step | Pass. `dmc update` rewrites `CLAUDE.md` |
| Development 4 Grounded | Claims cite a file | Pass. research.md; the log figures are cited from #247 |
| Development 6 One home | Names the home of each rule | Justified. The extent is in both texts, because a pointer from the always-loaded rules to the resume skill fails (R1); the toolkit test holds the two together. See Complexity Tracking |
| Development 7 Tests skip locally | `TestEnv.Need` where a tool is needed | Pass. The test reads files only |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 4 | Concise; the PR names its documents | Planned. See Design 4 |

No violations.

## Design

1. **`kit/dm-rules.md:78`**: "read their sheet down to its History, once per session part" becomes "read their sheet
   through its play card (if none, down to its History), once per session part"; "search `canon.md` and those archives
   as before, and in a sequel" becomes "search `canon.md` and those archives, and in a sequel"; "holds `.closed`: it is
   closed for good." becomes "holds `.closed`.".
2. **`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`**: a new test `ThePresentLineReadsASheetAsTheResumeDoes` (FR-001 to
   FR-003), before `NarrationOffersTheJumpByTheBreakTest`; `DmRulesBytesAllowed` stays 32,510 and its doc comment
   records spec 022's three changes; the cap test's message names specs 014 to 022.
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

| Violation | Why needed | Simpler alternative rejected because |
|---|---|---|
| Development 6: the extent of the Present-line read is in `kit/dm-rules.md` and in resume step 2.5 | The always-loaded rule must work when the resume skill is not in context, and the step decides which sheets to read as well as how far (R1) | A pointer to the step: the code review of PR #273 showed it unresolvable by label, wrong for someone not yet present, and absent in a new-campaign session |
