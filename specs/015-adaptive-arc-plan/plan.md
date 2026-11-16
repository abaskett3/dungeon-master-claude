# Implementation Plan: An arc plan built on a story structure, that DMC holds to and revises

**Branch**: `015-adaptive-arc-plan` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/015-adaptive-arc-plan/spec.md` (issue #217; issue #181, causes 2 to 6)

## Summary

Give homebrew arcs a story structure and make DMC keep the plan true. `dmc-new-campaign` gains step 4a, researched
guidance on the Five Commandments (the default) and the Hero's Journey, which is also the one home of how to revise
an arc. The template's Arc plan comment swaps Freytag's example for the Five Commandments' beats, with a rough session
budget per stage, the current stage's start session, and milestones as achievements "or a victory of the same
weight"; the Antagonists comment asks for each next move with its session. The full save's arc check gains Revise,
Pace and Clocks, and its Progression awards an equivalent. In play, one sentence in § Narration offers the jump once
a scene's point has landed. Migration 008 refreshes the two comments in existing campaigns; the next full save fills
in the rest. See [research.md](research.md) and the exact wording in [contracts/](contracts/).

## Technical Context

**Language/Version**: Markdown prompts (`kit/dm-rules.md`, two skills, the template, the migration, all embedded in
`dmc`); C# 14 / .NET 10 for the shape check and the tests

**Primary Dependencies**: none new

**Storage**: campaign files only: `story/dm-notes.md`'s Arc plan and Antagonists gain fields
([data-model.md](data-model.md)); the existing milestone Pending line in `game-state.md`. Standard 8.

**Testing**: MSTest: toolkit tests (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`), the migration tests
(`MigrationGoldenTests`, `MigrationFilesTests`) on fixture 008, the check's tests
(`tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`); headless creation, migration and five or more play sessions in
`D:\Claude\test-campaign` with `claude -p` ([quickstart.md](quickstart.md))

**Target Platform**: a DMC folder on Windows or Linux, played in Claude Code

**Project Type**: prompt change in a CLI toolkit, with one campaign-file standard change

**Performance Goals** (Development 1, measured on the contracts' wording):

| Where | Lines | Bytes | Estimated tokens (bytes / 4) | Paid |
|---|---|---|---|---|
| `kit/dm-rules.md` § Narration | 0 | +267 | about 67 | every turn |
| `dmc-campaign-full-save` step 5 | +3 | +1,082 | about 270 | each full save |
| `dmc-new-campaign` step 4 and new 4a | +21 | +3,134 | about 784 (767 for 4a alone) | campaign creation; a full save that plans or revises (reads 4a only) |
| Template: Arc plan and Antagonists comments | 0 | +433 | about 108 | every resume of a campaign holding them |
| Migration 008 | new file | | | once, when a campaign is migrated |

**Constraints**: `kit/dm-rules.md` stays at 212 lines and grows by at most 300 bytes (FR-008); the Arc plan section
within its 1.5k `dmc size` budget filled for seven stages (measured 1.3k, research R9); `dm-notes.md` within 10k.

**Scale/Scope**: 1 sentence in one bullet of `kit/dm-rules.md`; 2 template comments; 1 skill step added and 1
sentence changed; 3 full-save bullets added and 2 changed; 1 migration, 1 shape check with tests, 1 golden fixture;
2 toolkit tests changed or added; 4 documents and any that list the migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| Core 1. Consistency | New state named: structure, stages' budgets and starts, milestones and antagonist clocks and signs, all in `story/dm-notes.md` (read whole at resume); awards in `game-state.md`'s Pending line, as now | Pass |
| Core 2. Fun | The player gains a story with a crisis they choose through, a plan that follows what they did instead of a dead villain, threats that act, levels that come for what they achieve, and an offer to move on that they may decline | Pass |
| Core 3. Low context and token cost | Counted in the table above; the per-turn cost is 67 tokens; the guidance is paid only at creation and when a full save plans or revises an arc | Pass |
| Development 1. Count the cost | Lines and bytes for `kit/dm-rules.md`, each skill and the template, and a `dmc size` result on a filled example (research R8 to R11) | Pass |
| Development 2. No change breaks a campaign | Migration 008 replaces the template comments only, with a check, a fixture and a live run ([contracts/migration-008.md](contracts/migration-008.md)); it removes nothing the player or DMC wrote | Pass |
| Development 3. One step to install, one to upgrade | No install or update change; the migration is offered at resume as before | Pass |
| Development 4. Grounded in the code | Every claim cites a file, a command or a source; unverified items marked (research R1's "every unit", R2's grouping, R6's DMG) | Pass |
| Development 5. A model and effort for each skill | Two skills change (`dmc-new-campaign`, `dmc-campaign-full-save`) and neither names a model or effort; no DMC skill does yet | Break, justified in § Complexity Tracking (issue #219) |
| Development 6. Each rule has one home | What the plan holds: the template's comments. When it is checked, revised and paced: the full save. How to design and revise an arc: `dmc-new-campaign` step 4a. The jump offer: `kit/dm-rules.md` § Narration, pointing at § Compaction's break test. The full save points at 4a instead of copying it. | Pass |
| Development 7. Tests skip locally and fail on CI | The new tests need no external tool | Pass |
| Dungeon Master 1. An arc | `dmc-new-campaign` and the template name the structure and its beats (the principle's check) | Pass: this feature |
| Dungeon Master 2. Fair fights | No change to sizing or ending fights; a sign of an antagonist's move is not a forced fight | Pass |
| Dungeon Master 3. The module is the source | While a module runs, the Arc plan holds Character arcs only and Revise, Pace and Clocks skip; the module's arc and antagonists apply | Pass |
| Dungeon Master 4. The player shapes the story | A revision changes what the world does next, never what the player chose; the crisis is planned as a dilemma, never its answer; the jump is an offer the player may decline; a budget never hurries an open exchange | Pass |
| Documentation 1 to 4 | Short wording; the user guide in plain words; decisions, roadmap and campaign files in the same PR | Pass |

Re-checked after Phase 1: no change.

## Project Structure

### Documentation (this feature)

```text
specs/015-adaptive-arc-plan/
├── plan.md              # This file
├── research.md          # Phase 0: R1 to R14
├── data-model.md        # Phase 1: Arc plan and Antagonists fields, a stage's and a milestone's life
├── quickstart.md        # Phase 1: tests, the live migration, creation, the overtaken plan, five sessions
├── contracts/
│   ├── dm-rules-narration.md         # the jump offer sentence
│   ├── dm-notes-template.md          # the two template comments
│   ├── new-campaign-arc-guidance.md  # step 4 and the new step 4a
│   ├── full-save-arc-check.md        # step 5's arc check
│   └── migration-008.md              # the migration, its check and its fixture
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
kit/
├── dm-rules.md                                  # § Narration line 195: the jump offer
├── skills/dmc-new-campaign/SKILL.md             # step 4's sentence; new step 4a
├── skills/dmc-campaign-full-save/SKILL.md       # step 5: Arc plan, Revise, Pace, Clocks, Progression
├── templates/campaign/story/dm-notes.md         # the Arc plan and Antagonists comments
└── migrations/008-arc-plan-structure.md         # new: standard 8

src/Dmc.Cli/Campaign/
└── MigrationChecks.cs                           # new check arc-plan-guidance-current

tests/Dmc.Tests/
├── Toolkit/ToolkitTests.cs                      # byte cap 30,631 → the new size; a § Narration test
├── Campaign/MigrationChecksTests.cs             # the new check's tests
└── Fixtures/migrations/008/                     # new: before/, after/, accounting.tsv

.docs/
├── user-guide.md                                # § Talking to DMC: the offer to skip ahead
├── campaign-files.md                            # the dm-notes.md row; migration 008
├── decisions.md                                 # a new entry; the 3.0.0 entry's Limits drop Dungeon Master 1
└── roadmap.md                                   # a row for #217 and #181 causes 2 to 6
```

**Structure Decision**: a prompt change with one campaign-file standard change: the template, its migration, a check
in `dmc`, and their tests. `dmc` gains only the check, so the kit and the binary ship together as before.

## Complexity Tracking

| Principle broken | Why | Simpler alternative rejected because |
|---|---|---|
| Development 5: `dmc-new-campaign` and `dmc-campaign-full-save` change without naming a recommended model and effort | No DMC skill names one yet; the gap is project-wide and tracked in issue #219 (open). Which skill frontmatter fields Claude Code honours is unverified (`.docs/decisions.md` line 730), and a model choice must rest on test, play and historical data, which this feature does not gather. | Adding a model and effort to these two skills alone would set values with no data behind them, in fields that may not be honoured, ahead of #219's one decision for every skill. |
