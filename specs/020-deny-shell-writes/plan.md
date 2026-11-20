# Implementation Plan: Deny the usual shell writes and `SendFeedback` in play

**Branch**: `020-deny-shell-writes` | **Date**: 2026-11-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/020-deny-shell-writes/spec.md`

## Summary

Add fourteen deny rules to `kit/settings.json`: the thirteen shell writes and script runners of issue #245 and the
`SendFeedback` tool ([research.md](research.md) R1, R2). Name the Write tool for `adventures/.active-campaign` in
`dmc-new-campaign` and `dmc-resume-campaign` (R3). Keep the `auto` mode (R4). A toolkit test partial,
`ShellWriteDenyTests.cs`, holds the kit to it. No `dmc` code changes; the kit reaches players in the next release.

## Technical Context

**Language/Version**: JSON settings and Markdown prompts (`kit/`); C# on .NET 10 for the toolkit tests

**Primary Dependencies**: none new

**Storage**: N/A: no campaign file changes (FR-006)

**Testing**: MSTest toolkit tests (`dotnet test`); live checks in [quickstart.md](quickstart.md)

**Target Platform**: the DMC folder on Windows and Linux, as today

**Project Type**: prompt kit embedded in a CLI (`src/Dmc.Cli/Kit/GeneratedFiles.cs`)

**Performance Goals**: about 5,500 characters less in each play session (R2)

**Constraints**: no line added to `kit/dm-rules.md`; 20 bytes in each of two skills

**Scale/Scope**: 1 settings file, 2 skill lines, 1 new test file, 5 documents

## Constitution Check

| Principle | Check | Result |
|---|---|---|
| Core 1. Consistency | Files that hold new state | None. Pass. |
| Core 2. Fun | What the player gains | Changes they can see as a diff; no hidden feedback drafts. Pass. |
| Core 3 / Development 1. Cost | Context added | `kit/dm-rules.md` 0 bytes; skills +20 bytes each; settings are not context; about −5,500 characters per session. Pass. |
| Development 2. No change breaks a campaign | Migration | None needed (R5). Pass. |
| Development 3. One step to install and upgrade | New step | None. Pass. |
| Development 4. Grounded in the code | Claims cited | research.md; one unverified point (R2) checked live in quickstart Q2. Pass. |
| Development 6. One home per rule | Home | "No shell writes on campaign files" stays at `kit/dm-rules.md:63`; the deny rules enforce it. Pass. |
| Development 7. Tests skip locally, fail on CI | Tools needed | The tests read files only. Pass. |
| Dungeon Master 1 to 4 | | Not touched. Pass. |
| Documentation 1 to 4 | Docs in this PR | user guide, architecture, development, decisions, roadmap. Pass. |

## Project Structure

```text
specs/020-deny-shell-writes/
├── spec.md
├── plan.md
├── research.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md

kit/settings.json                              # 14 deny rules
kit/skills/dmc-new-campaign/SKILL.md           # § 1: with the Write tool
kit/skills/dmc-resume-campaign/SKILL.md        # § 1: with the Write tool
tests/Dmc.Tests/Toolkit/ShellWriteDenyTests.cs # new
.docs/user-guide.md, architecture.md, development.md, decisions.md, roadmap.md
```

No `data-model.md` and no `contracts/`: no data or interface changes.

## Complexity Tracking

None.
