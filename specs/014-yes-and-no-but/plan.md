# Implementation Plan: "Yes, and…" and "No, but…"

**Branch**: `014-yes-and-no-but` | **Date**: 2026-11-15 (revised after the first play test) | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/014-yes-and-no-but/spec.md` (issue #216)

## Summary

Teach DMC to answer a bold choice with "yes, and", a choice the world rules out with "no, but" in the fiction (with,
in the same reply, a way toward what the player wanted), and a first attack on a major NPC with a "no, but" that
leaves everyone alive and is recorded in canon; and to weave what the character wants into the main plot through the
Arc plan's Character arcs. The rules change is three existing bullets of `kit/dm-rules.md` § Fun first, rewritten in
place: no new line, 719 bytes (about 180 tokens per turn); the toolkit test's byte cap rises by 593. The first play
test found that many campaigns have no Arc plan, so the template's Arc plan comment keeps Character arcs in every
campaign, and migration 007 gives every campaign an Arc plan section, with a shape check and a golden fixture. See
[research.md](research.md) and the exact wording in [contracts/](contracts/).

Already built and committed on the branch (tasks T001 to T017): the first wording of lines 165, 166 and 169, the
toolkit tests, the documents and the first play test. What remains is research R12 to R19.

## Technical Context

**Language/Version**: Markdown prompts (`kit/dm-rules.md`, the template, the migration, all embedded in `dmc`); C# 14
/ .NET 10 for the shape check and the tests

**Primary Dependencies**: none new

**Storage**: campaign files: a canon line in `story/canon.md` (no shape change); `story/dm-notes.md` gains an
`## Arc plan` section with a Character arcs line in every campaign (standard 7)

**Testing**: MSTest: toolkit tests (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`), the migration tests
(`MigrationGoldenTests`, `MigrationFilesTests`) on fixture 007, the check's tests
(`tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`); a headless play test and a live migration run in
`D:\Claude\test-campaign` with `claude -p`

**Target Platform**: a DMC folder on Windows or Linux, played in Claude Code

**Project Type**: prompt change in a CLI toolkit, with one campaign-file standard change

**Performance Goals**: about 180 more tokens per turn (719 bytes / 4, `src/Dmc.Cli/Size/Measurement.cs:16`); about
21 more tokens per resume in a campaign holding the new Arc plan comment (82 bytes, research R14)

**Constraints**: `kit/dm-rules.md` stays at 212 lines; bytes at most 30,636 (spec FR-008, a rise of 593); measured
draft 30,636

**Scale/Scope**: 3 bullets of one section; 2 lines of one template comment; 1 migration; 1 shape check with tests;
1 golden fixture; 1 toolkit test changed and 1 added; 5 documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| Core 1. Consistency | New state named: the "no, but" canon line in `story/canon.md`; goals in the Arc plan's Character arcs in `story/dm-notes.md`, which every campaign now has | Pass |
| Core 2. Fun | The player gains choices that matter, a way toward what they wanted when the world says no, and no flat refusals (spec User Stories 1 to 3) | Pass |
| Core 3. Low context and token cost | About 180 tokens per turn and 21 per resume, counted as Development 1 says | Pass, a cost the clarifications accepted |
| Development 1. Count the cost | `kit/dm-rules.md`: 0 lines, +719 bytes; no skill changes; the template comment +82 bytes, read on resume (research R14); the migration adds a few lines to `dm-notes.md`, and says in its What changes that a moved arc may be flagged by `dmc size` | Pass |
| Development 2. No change breaks a campaign | The template changes what `dm-notes.md` holds, so migration 007 ships with it (contract [migration-007.md](contracts/migration-007.md)); it removes nothing | Pass |
| Development 3. One step to install, one to upgrade | No install or update change; the migration is offered at resume as before | Pass |
| Development 4. Grounded in the code | Every claim cites a file and line or a command; unverified items marked (research R9, R17, R18) | Pass |
| Development 5. A model and effort for each skill | No skill or agent added or changed (the standing gap is issue #219) | N/A |
| Development 6. Each rule has one home | The play rule lives in `kit/dm-rules.md` § Fun first; what the Arc plan holds lives in the template's comment; the migration's steps live in the migration; the user guide describes and points | Pass |
| Development 7. Tests skip locally and fail on CI | The new tests need no external tool | Pass |
| Dungeon Master 1. An arc | Every campaign gets an Arc plan section; a homebrew arc planned under another heading moves into it unchanged | Pass |
| Dungeon Master 2. Fair fights | The one first-attack exception spares everyone once, at every lethality (a clarification); after it the dice fall where they fall | Pass, an exception the maintainer chose |
| Dungeon Master 3. The module is the source | In a module campaign the Arc plan holds Character arcs only; the story adapts within the module; staying in its world is issue #215 | Pass |
| Dungeon Master 4. The player shapes the story | This feature | Pass |
| Documentation 1 to 4 | Short wording; the user guide in plain words; decisions, roadmap, campaign files and product overview in the same PR | Pass |

Re-checked after Phase 1: no change.

## Project Structure

### Documentation (this feature)

```text
specs/014-yes-and-no-but/
├── plan.md              # This file
├── research.md          # Phase 0 (R1 to R11; R12 to R19 after the play test)
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1, with the first play test's Results
├── contracts/
│   ├── dm-rules-fun-first.md   # The exact wording of the three bullets
│   ├── dm-notes-arc-plan.md    # The template's Arc plan comment
│   └── migration-007.md        # The migration, its check and its fixture
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
kit/
├── dm-rules.md                                  # § Fun first: line 166's way forward (165 and 169 done)
├── templates/campaign/story/dm-notes.md         # the Arc plan comment, two lines
└── migrations/007-arc-plan-everywhere.md        # new: standard 7

src/Dmc.Cli/Campaign/
└── MigrationChecks.cs                           # new check dm-notes-have-arc-plan

tests/Dmc.Tests/
├── Toolkit/ToolkitTests.cs                      # byte cap 30,602 → 30,636 and its comment
├── Campaign/MigrationChecksTests.cs             # the new check's tests
└── Fixtures/migrations/007/                     # new: before/, after/, accounting.tsv

.docs/
├── user-guide.md                                # § Talking to DMC: done; the way forward
├── product-overview.md                          # done
├── campaign-files.md                            # the dm-notes.md row: Character arcs in every campaign
├── decisions.md                                 # the entry's Limits rewritten; migration 007
└── roadmap.md                                   # the row names migration 007
```

**Structure Decision**: a prompt change with one campaign-file standard change: the template, its migration, a
check in `dmc`, and their tests. `dmc` gains only the check, so the kit and the binary ship together as before.

## Complexity Tracking

No violations.
