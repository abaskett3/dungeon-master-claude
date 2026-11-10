# Implementation Plan: Image Prompts of the Story, in Saved Styles

**Branch**: `007-image-prompts` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md) | **Issue**: #178

**Input**: Feature specification from `specs/007-image-prompts/spec.md`

## Summary

A new player skill, `dmc-generate-image-prompt`, writes prompts for an image model. Their content comes only from the
campaign files: everyone present in a wide shot, each figure's species and size as a comparison, nothing the character
has not perceived, and no detail the files do not hold. Their style is chosen from the request, a saved style, the last
saved style used, or the built-in Storybook.

Saved styles live in a new, optional `## Image styles` section of the general `adventures/player-options.md`. The
feature changes only the kit. There is no migration and no `dmc` code (research R9): Rorn's Landing's two styles move
once, when the player asks.

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`), Native AOT for releases. The prompts are Markdown.

**Primary Dependencies**: None new.

**Storage**: Files in a DMC folder: `adventures/player-options.md`, and the campaign's sheets.

**Testing**: The toolkit tests in `tests/Dmc.Tests/Toolkit`. The prompts are checked by live runs
([quickstart.md](quickstart.md)).

**Target Platform**: The four release platforms (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`).

**Project Type**: A CLI (`dmc`) and the kit it embeds: rules, skills, templates and migrations.

**Performance Goals**: None.

**Constraints**:
- Every session pays at most the skill's description, about 400 bytes (FR-004, SC-005).
- No skill clause repairs older files (`MigrationPromptTests`).

**Scale/Scope**: One skill, one template section, one toolkit test, and the documents.

## Constitution Check

*Before Phase 0, and again after Phase 1.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | The content comes only from the files and invents nothing (FR-006 to FR-009); spoilers are kept out (FR-008); a prompt changes no game state (FR-003). | Pass |
| II. Play pays nothing for toolkit work | No line is added to the DM rules (R2); the skill's description is the only cost per session. Saved styles are the player's data, and moving Rorn's Landing's from one always-read file to another is neutral (R5). | Pass |
| III. Each rule has one home | The spoiler rule stays in `dm-rules.md`, and the skill points at it. The style rules are in the skill only. The section's shape is in the template, and the contract points at it. | Pass |
| IV. Documentation goes with every change | The docs to change are listed under Documents below (FR-019). | Pass, on the condition the tasks do it |
| V. Tests skip locally and fail on CI | New tests are pure .NET, with no outside tool. | Pass |
| Build and output | No `dmc` code changes. | Pass |
| Workflow | One issue (#178); the PR says `Closes #178`, and why no migration is needed; one review on the finished PR. | Pass |

No violations, so Complexity Tracking is empty.

**After Phase 1:** the design is one skill and one optional template section. Still Pass.

## Project Structure

### Documentation (this feature)

```text
specs/007-image-prompts/
├── spec.md
├── plan.md                         # this file
├── research.md                     # R1 to R10
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── dmc-generate-image-prompt.md
│   └── image-styles-section.md
├── checklists/requirements.md
└── tasks.md                        # /speckit-tasks
```

### Source Code (repository root)

```text
kit/
├── skills/dmc-generate-image-prompt/SKILL.md      # new (R1, R4, R6 to R8)
└── templates/player-options.md                    # + ## Image styles (R3)

tests/Dmc.Tests/Toolkit/ToolkitTests.cs            # + the section's shape in the template
```

**Structure Decision**: The existing layout. The skill sits beside the other player skills.

### Documents

| Document | Change |
|---|---|
| `README.md` | `/dmc-generate-image-prompt` in the skill table |
| `.docs/user-guide.md` | Image prompts: asking, styles, saving, what stays out |
| `.docs/campaign-files.md` | The general `player-options.md` row gains Image styles (optional) |
| `.docs/architecture.md` | The Skills row: twelve procedures, naming `dmc-generate-image-prompt` |
| `.docs/roadmap.md` | A row for issue #178 |
| `.docs/decisions.md` | Styles in the general options; facts only, never assumed; no migration |

## Complexity Tracking

None.
