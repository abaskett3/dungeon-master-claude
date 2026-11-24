# Implementation Plan: Hold fixes from feedback that only add words

**Branch**: `claude/project-thread-86m1m7-248` (spec 023) | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/023-hold-word-only-fixes/spec.md` (issue #248, Phase 0 of #238)

## Summary

`review-feedback` gains a hold: a fix that is only words is listed as held and not made, until the slip test of #249
is done or 2026-12-31. Three kinds of change are not held. A toolkit test pins the text.

## Technical Context

**Language/Version**: Markdown developer skill; C# tests on .NET 10

**Primary Dependencies**: none new

**Storage**: none

**Testing**: MSTest, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`

**Target Platform**: toolkit work in this repository

**Project Type**: developer-skill change with a toolkit test

**Constraints**: no kit file changes

**Scale/Scope**: one skill, one test, three documents

## Constitution Check

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | New state | Pass. None; the hold is a Worker note in an existing entry |
| Core 2 Fun | What the player gains | Pass. Rules that grow only when a test shows they help; the cost is fewer small fixes for some weeks |
| Core 3 Low cost | Cost counted | Pass. 0 in play (R4) |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill | Pass. 0 to `kit/dm-rules.md`; `review-feedback` +8 lines and one longer line, a developer skill |
| Development 2 No change breaks a campaign | Migration | Pass. No campaign file changes |
| Development 3 One step | Install or update | Pass. Nothing reaches a DMC folder |
| Development 4 Grounded | Claims cite a file | Pass. spec Background, research |
| Development 6 One home | Home of the rule | Pass. The skill; `.docs/development.md` and `.docs/decisions.md` point at it |
| Development 7 Tests skip locally | `TestEnv.Need` | Pass. The test reads files only |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 4 | The PR names its documents | Planned, Design 3 |

No violations.

## Design

1. **`.claude/skills/review-feedback/SKILL.md`**: in § 6, before "List separately", a paragraph "The hold on
   word-only fixes (issue #248)" with FR-001 to FR-003; in § 7, after its first sentence, FR-004 (the Worker note is
   written there, on the user's word, so a report alone changes no entry file).
2. **`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`**: `ReviewFeedbackHoldsWordOnlyFixesUntilTheSlipTest`, before
   `FeedbackHasOneRoute`, with an XML doc comment.
3. **Documents**: `.docs/decisions.md` (new entry), `.docs/roadmap.md` (row, date), `.docs/development.md` § Writing
   prompts (one sentence in the feedback bullet), and one sentence each in `.docs/user-guide.md` § the feedback
   section, `README.md` and `.docs/product-overview.md` where they describe `review-feedback`.

## Project Structure

```text
specs/023-hold-word-only-fixes/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md
```

No data model or contract.

## Complexity Tracking

None.
