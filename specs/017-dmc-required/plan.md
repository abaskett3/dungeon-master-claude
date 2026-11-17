# Implementation Plan: `dmc` is a required part of DMC

**Branch**: `017-dmc-required` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/017-dmc-required/spec.md`

## Summary

Retire the principle that play never depends on `dmc` (issue #208). The kit loses its six instructions for working
without `dmc` ([research.md](research.md) R2); `kit/dm-rules.md:139` now ends "then play nothing until `dmc --version`
runs", so a missing `dmc` (the shell says "command not found", or the program cannot start) is an error that stops
play until the player reinstalls. Errors from a working `dmc` keep today's handling (R4). The start-of-session notice,
the reinstall lines, lookup's and recall's next steps after finding nothing, and text maps as a player option stay
(R3). A toolkit test holds the kit to it, the byte cap of `kit/dm-rules.md` drops by 16, and the README, the
architecture, the user guide, the decisions and the roadmap say `dmc` is required. No `dmc` code changes; the kit
reaches players in the next release (R1).

## Technical Context

**Language/Version**: Markdown prompts (`kit/`); C# (`LangVersion` latest) on .NET 10 for the toolkit tests (`tests/Dmc.Tests`)

**Primary Dependencies**: none new

**Storage**: N/A: no campaign file changes (FR-008)

**Testing**: MSTest toolkit tests (`dotnet test`, after `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`);
two headless `claude -p` runs ([quickstart.md](quickstart.md))

**Target Platform**: the DMC folder on Windows and Linux, as today

**Project Type**: prompt kit embedded in a CLI (`src/Dmc.Cli/Kit/GeneratedFiles.cs`)

**Performance Goals**: `kit/dm-rules.md` does not grow (FR-009): 32,522 → 32,506 bytes, 212 lines (R5, measured)

**Constraints**: no new line in `kit/dm-rules.md`; no new skill line; the start-of-session notice text unchanged
(asserted at `tests/Dmc.Tests/Toolkit/ToolkitTests.cs:348`)

**Scale/Scope**: 6 kit edits in 4 files, 1 new test file, 1 test constant and 1 assertion in `ToolkitTests.cs`, 5 documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1. Consistency | Files that hold new state | None: no state is added. Pass. |
| Core 2. Fun | What the player gains | No turn where DMC makes up a roll or draws a map by hand because `dmc` is gone; a clear way back (the install line). Pass. |
| Core 3 / Development 1. Cost | Lines added to `kit/dm-rules.md` and each skill | `kit/dm-rules.md`: 0 lines, −16 bytes. Skills: 0 lines added; `dmc-ingest-module` −1 bullet, `dmc-setup-rules` −2 clauses, `dmc-draw-map` −1 clause. No change to what a resume reads, so no `dmc size` result is needed. Pass. |
| Development 2. No change breaks a campaign | Migration, or why none | None needed: no template or file-shape rule changes (R8). Pass. |
| Development 3. One step to install and upgrade | New step or dependency | None (R8). Pass. |
| Development 4. Grounded in the code | Every claim cites a file or is marked unverified | research.md cites each; one unverified point (the `SessionStart` hook under `claude -p`, R9) is tested, not assumed. Pass. |
| Development 5 | Removed in constitution 4.0.0 (PR #232; issue #219 closed as not planned) | N/A |
| Development 6. One home per rule | Home of each rule changed | "Play nothing until `dmc` runs": `kit/dm-rules.md:139`, nowhere else. Pass. |
| Development 7. Tests skip locally, fail on CI | Tests that need a tool | The new test reads files only. Pass. |
| Dungeon Master 1 to 4 | Arcs, fights, modules, player choice | Not touched. Pass. |
| Documentation 1 to 4 | Concise; player docs non-technical; docs in the same PR | README and user guide in player words; all five documents in this PR (R7). Pass. |

Re-check after Phase 1: unchanged; no design artifact adds state, cost or a rule home.

## Project Structure

### Documentation (this feature)

```text
specs/017-dmc-required/
├── spec.md
├── plan.md              # this file
├── research.md          # Phase 0
├── quickstart.md        # Phase 1
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

No `data-model.md`: the change holds no data. No `contracts/`: no interface changes (no `dmc` command or output, and
the start-of-session notice keeps its text).

### Source Code (repository root)

```text
kit/
├── dm-rules.md                          # lines 78 and 139 (R2)
└── skills/
    ├── dmc-draw-map/SKILL.md            # line 114
    ├── dmc-ingest-module/SKILL.md       # line 26 removed
    └── dmc-setup-rules/SKILL.md         # lines 17 and 42
tests/Dmc.Tests/Toolkit/
├── DmcRequiredTests.cs                  # new: TheKitCarriesNoRuleForPlayingWithoutDmc (R6)
└── ToolkitTests.cs                      # DmRulesBytesAllowed lowered to the measured size
README.md                                # line 11
.docs/
├── architecture.md                      # line 120
├── user-guide.md                        # lines 65 and 450
├── decisions.md                         # new entry; two "Partly superseded" lines
└── roadmap.md                           # a row for spec 017
```

**Structure Decision**: Prompt edits in place, one toolkit test partial per spec as `FeedbackRoundTests.cs` does.

## Complexity Tracking

None: no principle is broken.
