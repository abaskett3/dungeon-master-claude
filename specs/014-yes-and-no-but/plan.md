# Implementation Plan: "Yes, and…" and "No, but…"

**Branch**: `014-yes-and-no-but` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/014-yes-and-no-but/spec.md` (issue #216)

## Summary

Teach DMC to answer a bold choice with "yes, and", a choice the world rules out with "no, but" in the fiction, and a
first attack on a major NPC with a "no, but" that leaves everyone alive and is recorded in canon; and to weave what
the character wants into the main plot. The change is three existing bullets of `kit/dm-rules.md` § Fun first,
rewritten in place: no new line, 685 bytes (about 171 tokens per turn). The toolkit test's byte cap rises by 498
bytes. No template changes, so no migration. See [research.md](research.md) and the exact wording in
[contracts/dm-rules-fun-first.md](contracts/dm-rules-fun-first.md).

## Technical Context

**Language/Version**: Markdown prompt (`kit/dm-rules.md`, embedded in `dmc`); C# 14 / .NET 10 for the toolkit test
only

**Primary Dependencies**: none new

**Storage**: campaign files, unchanged in shape: a canon line in `story/canon.md`, a goal in the Arc plan's
Character arcs in `story/dm-notes.md`

**Testing**: MSTest toolkit tests (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`); a headless play test in
`D:\Claude\test-campaign` with `claude -p`

**Target Platform**: a DMC folder on Windows or Linux, played in Claude Code

**Project Type**: prompt change in a CLI toolkit

**Performance Goals**: about 171 more tokens per turn (685 bytes / 4, the estimate `dmc size` uses,
`src/Dmc.Cli/Size/Measurement.cs:16`)

**Constraints**: `kit/dm-rules.md` stays at 212 lines; bytes at most 30,543 (spec FR-008); measured draft 30,541

**Scale/Scope**: 3 bullets of one section; 1 toolkit test changed and 1 added; 4 documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| Core 1. Consistency | New state named: the "no, but" canon line in `story/canon.md`; goals in the Arc plan's Character arcs in `story/dm-notes.md` | Pass |
| Core 2. Fun | The player gains choices that matter and no flat refusals (spec User Stories 1 to 3) | Pass |
| Core 3. Low context and token cost | About 171 tokens per turn, counted as Development 1 says | Pass, a cost the clarification accepted |
| Development 1. Count the cost | `kit/dm-rules.md`: 0 lines, +685 bytes; no skill changes; no resume read changes, so no `dmc size` result applies | Pass |
| Development 2. No change breaks a campaign | No template or file shape changes; no migration needed | Pass |
| Development 3. One step to install, one to upgrade | No install or update change | Pass |
| Development 4. Grounded in the code | Every claim cites a file and line or a command; unverified items marked (research R9) | Pass |
| Development 5. A model and effort for each skill | No skill or agent added or changed (the standing gap is issue #219) | N/A |
| Development 6. Each rule has one home | The rule lives in `kit/dm-rules.md` § Fun first; the user guide describes it and points there | Pass |
| Development 7. Tests skip locally and fail on CI | The toolkit tests need no external tool | Pass |
| Dungeon Master 1. An arc | Goals feed the Arc plan's Character arcs; nothing else changes | Pass |
| Dungeon Master 2. Fair fights | The one first-attack exception spares everyone once, at every lethality (a clarification); after it the dice fall where they fall, so the player's lethality governs every other fight | Pass, an exception the maintainer chose |
| Dungeon Master 3. The module is the source | The story adapts within the module; staying in its world is issue #215 | Pass |
| Dungeon Master 4. The player shapes the story | This feature | Pass |
| Documentation 1 to 4 | Short wording; the user guide in plain words; decisions, roadmap and product overview in the same PR | Pass |

Re-checked after Phase 1: no change.

## Project Structure

### Documentation (this feature)

```text
specs/014-yes-and-no-but/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── dm-rules-fun-first.md   # The exact wording of the three bullets
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
kit/
└── dm-rules.md                      # § Fun first: three bullets rewritten in place

tests/Dmc.Tests/Toolkit/
└── ToolkitTests.cs                  # byte cap 30,043 → 30,541 and its comment; one new test

.docs/
├── user-guide.md                    # § Talking to DMC: one paragraph
├── product-overview.md              # § What it deliberately does not do: one bullet
├── decisions.md                     # one entry
└── roadmap.md                       # one row
```

**Structure Decision**: a prompt change. No source code beyond the toolkit test, no template, no migration.

## Complexity Tracking

No violations.
