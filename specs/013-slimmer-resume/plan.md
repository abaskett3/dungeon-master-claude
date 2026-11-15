# Implementation Plan: A Slimmer Resume (issue #179, Phase D)

**Branch**: `013-slimmer-resume` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/013-slimmer-resume/spec.md`

## Summary

A resume reads each companion's play card, not the sheet down to its history, and reads canon as Rulings, Prices and
the other always-read sections plus the scene's best matches, ranked as recall ranks them and cut at 3k. A new form of
recall, `dmc recall NAME --scene`, builds the search words from `game-state.md` itself, so DMC makes one call and
`dmc size` measures exactly what it prints. The `UserPromptSubmit` hook's command, `dmc context-check`, also reminds
DMC of canon lines for names in the player's message, once per name per session part, using a register written inside
canon's People entries. Migration 006 writes the cards and the People names into existing campaigns. The gate is
checked after release, against a baseline counted in this PR from every session log of The Silver Egg and Rorn's
Landing, with a new `Slip:` line in session logs from now on. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown for the kit's rules, skills, templates and
migration

**Primary Dependencies**: None added

**Storage**: Files only. Campaign files gain a `## Play card` section on companion sheets and `(also: …)` brackets in
canon's People entries; the hook stores nothing (it reads its earlier output from the session log)

**Testing**: MSTest in `tests/Dmc.Tests` (`Size/`, `Retrieval/`, `ContextCheck/`, `Campaign/`, `Toolkit/`), temporary
DMC folders from `Support/`, migration 006's golden fixture; a headless play test in `D:\Claude\test-campaign` for the
model's part and the hook's real input ([quickstart.md](quickstart.md))

**Target Platform**: Windows, Linux (x64, arm64), as every `dmc` command

**Project Type**: CLI plus the prompts it installs

**Performance Goals**: `dmc recall --scene` and the hook's canon part each under half a second on Rorn's Landing on the
release build (SC-007); the hook today takes about 145 ms

**Constraints**: Every failure one line `dmc <command>: <message>`; `context-check` never fails; warnings are errors; an
XML comment on every new or changed member, private ones included; `kit/dm-rules.md` grows by no line and at most 400
bytes; no fallback for a missing `dmc` (issue #208 is separate)

**Scale/Scope**: One changed command form (`recall --scene`), two changed commands (`size`, `context-check`), two new
budgets, two new shape checks, one migration, edits to the resume, full save, sheet update and review-transcript skills,
two templates, two sentences of `dm-rules.md`, about seven documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | Rulings and Prices are still read whole; the scene's best canon lines are read; the full sheet is read whenever the past, long speech or an off-card number comes up; the hook reminds DMC of a named person's canon. The risk (a fact below the cut) is what the gate measures, with a rollback per category the user chose. | Pass, gated after release |
| II. Play pays nothing for toolkit work | A resume reads about 21k less (SC-003). `dm-rules.md` gains no line and at most 400 bytes; the hook adds at most about 1k a message, and only on a name not yet reminded. No new process per message, nothing new to install. | Pass, cost measured in the PR |
| III. Each rule has one home | Numbers in `Budgets.cs`; the scene's words and ranking in one method used by recall and size; the register's parse in one place used by the hook and the migration check; the card's shape in the NPC template; the gate's procedure in `review-transcript`. | Pass |
| IV. Documentation goes with every change | `campaign-files.md`, `dmc-reference.md`, `architecture.md` (resume items 5 and 7, the hook), `user-guide.md`, `roadmap.md`, `decisions.md` (four decisions, two amendments), the templates. | Pass |
| V. Tests skip locally and fail on CI | Every test needs only the built program and temporary folders; timing on the release build as `RecallTimingTests` does. | Pass |
| Build and Output Constraints | No reflection, no dependency; one-line errors; outputs capped (12,000 bytes for the scene read, 4,000 for the hook). | Pass |

Re-check after Phase 1 design: unchanged. A migration is needed (standard 6) because companion sheets and People entries
change shape; the canon read and the hook need none (research R7).

## Project Structure

### Documentation (this feature)

```text
specs/013-slimmer-resume/
├── spec.md
├── plan.md              # This file
├── research.md          # R0 to R9
├── data-model.md        # Play card, scene read, register, reminder, slip line, gate result
├── quickstart.md        # Build, test, the size check, the headless play test, the baseline count
├── baseline.md          # The gate's baseline, per session (written in implementation)
├── contracts/
│   ├── dmc-recall-scene.md
│   ├── dmc-size.md
│   └── context-check-canon.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── Retrieval/
│   ├── SceneRead.cs           # new: words from game-state.md, canon outside the always-read sections, ranked, capped
│   ├── Register.cs            # new: names from canon's People entries
│   ├── CanonReminder.cs       # new: the hook's lines for new names in a prompt
│   ├── RetrievalArguments.cs  # --scene
│   ├── Recall.cs              # Rank and Hit reused, unchanged in behaviour
│   └── CanonReminder.cs       # the hook's canon lines; Reminded reads names already reminded after the latest compaction boundary
├── Commands/
│   ├── RecallCommand.cs       # --scene form and help
│   └── ContextCheckCommand.cs # prints the reminder after the break line
├── Size/
│   ├── Budgets.cs             # PlayCard 1000, CanonWholeBelow 6000, SceneReadBytes 12000
│   ├── ReadSet.cs             # companion to its play card; canon by scene; markers
│   └── Flags.cs               # "a companion with no play card"
└── Campaign/MigrationChecks.cs  # companions-have-play-card, people-entries-named

kit/
├── dm-rules.md                       # two sentences changed (R2, R8)
├── migrations/006-play-cards-and-names.md
├── skills/dmc-resume-campaign/SKILL.md      # items 5 and 7
├── skills/dmc-campaign-full-save/SKILL.md   # cards, People names, standing facts (FR-010)
├── skills/dmc-character-sheet-update/SKILL.md # a card for a new companion, People names
├── templates/npc-sheet.md, pc-sheet.md      # the card section
├── templates/campaign/story/canon.md        # the People shape
└── templates/session-log.md                 # the Slip: line

.claude/skills/review-transcript/SKILL.md     # the slip tally and the gate

tests/Dmc.Tests/
├── Retrieval/ (SceneRead, Register, recall --scene contract, timing)
├── ContextCheck/ (the reminder: new names, once per part, cap, silence)
├── Size/ (card part, canon by scene, the flag)
├── Campaign/MigrationChecksTests.cs, Fixtures/migrations/006/
└── Toolkit/ToolkitTests.cs (dm-rules 212 lines and the byte cap; markers)
```

**Structure Decision**: The existing single project. The new code sits in `Retrieval/` beside recall, whose ranking it
reuses; `dmc size` calls it so the measured read is the printed read.

## Complexity Tracking

None: no constitution check fails.
