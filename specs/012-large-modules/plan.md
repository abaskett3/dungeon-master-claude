# Implementation Plan: Large Modules Cost One Area, Not One Chapter

**Branch**: `012-large-modules` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-large-modules/spec.md`

## Summary

A module campaign's resume reads the index and the area the party is in, not the chapter section around it. The
pointer in `module-progress.md` gains a form that names one or more headings (`chapters/05-hub.md § 12. The Well`),
which the resume reads with `dmc lookup` and `dmc size` measures with lookup's own matcher, so the report is what DMC
reads. Old pointers keep their meaning.

A new read-only command, `dmc module check NAME`, reports a module's files at the Read cap mark, sections over a new
3,000-token section limit, headings that hold several stat blocks and headings repeated under one parent. The ingest
skill writes one heading per area, NPC, stat block, item and handout and runs the check before it reports chapters
ready. `dmc size --check`, which resume and the full save already run, prints one `module:` line while the module has
problems, so modules ingested before this change are found without anyone asking. The check only reports; the repair
is an edit. One PR, no migration. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown for the kit's skills, template and rules

**Primary Dependencies**: None added

**Storage**: Files only. The pointer line of `module-progress.md`; modules are read, never written

**Testing**: MSTest in `tests/Dmc.Tests` (a new `Modules/` folder, plus `Size/`, `Retrieval/`, `Toolkit/`), temporary
DMC folders from `Support/` with synthetic modules; a live resume for the model's part ([quickstart.md](quickstart.md))

**Target Platform**: Windows, Linux (x64, arm64), as every `dmc` command

**Project Type**: CLI plus the prompts it installs

**Performance Goals**: The check over about 1.5 MB of module markdown within one second on the release build
(SC-006); `dmc size --check` gains that read only for an unfinished module campaign

**Constraints**: Every failure one line `dmc <command>: <message>`; at most 12 lines of problems; warnings are errors;
an XML comment on every new or changed member, private ones included; `kit/dm-rules.md` grows by no line (two allowed)

**Scale/Scope**: One new command (`module check`), two changed (`size`, and `lookup`'s matcher factored out with no
behaviour change), one new budget, edits to two skills, one template comment and one rule sentence, about six
documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | The resume reads the area the party is in, with any event heading the pointer names; a pointer that misses still reads the chapter and says so. Lookup is unchanged, so every module fact is reached as before. A module the check flags keeps working. | Pass |
| II. Play pays nothing for toolkit work | A resume of a large-module campaign reads about 10k less at the hub (SC-001). `--check` adds nothing for a sound module and one line otherwise; no command call is added to play. `dm-rules.md` gains no line. The ingest skill loads only when ingesting. Nothing new to install. | Pass, cost measured in the PR |
| III. Each rule has one home | The numbers in `Budgets.cs`; the section matcher in `Lookup.FindSection`, used by lookup and size; the check in `ModuleCheck`, used by the command and by size; the module shape in the ingest skill. | Pass |
| IV. Documentation goes with every change | `dmc-reference.md` (`module`, `size`), `campaign-files.md` (the pointer), `user-guide.md` § Published modules, `architecture.md` (resume item 9), `roadmap.md`, `decisions.md` (the check, the limit, the Phase A amendment). | Pass |
| V. Tests skip locally and fail on CI | Every test needs only the built program and temporary folders; the timing test uses the release build as `RecallTimingTests` does. | Pass |
| Build and Output Constraints | No reflection, no dependency added; one-line errors; output capped at 12 lines. | Pass |

Re-check after Phase 1 design: unchanged. No migration: the template keeps its line and label, every old pointer stays
valid, and modules are not campaigns (research R11). The play-without-`dmc` principle is not extended (issue #208).

## Project Structure

### Documentation (this feature)

```text
specs/012-large-modules/
├── plan.md                     # This file
├── research.md                 # Phase 0: R0 to R12
├── data-model.md               # Phase 1: module, section, problem, pointer
├── quickstart.md               # Phase 1: CI fixtures and the live checks
├── contracts/
│   ├── dmc-module-check.md     # dmc module check; the module line and pointer in dmc size; lookup unchanged
│   └── prompts.md              # every kit change
├── checklists/requirements.md  # spec quality (specify)
└── tasks.md                    # Phase 2 (/speckit-tasks), not created here
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── Commands/ModuleCommand.cs   # new: dmc module check NAME [--folder PATH]
├── Modules/
│   ├── ModuleArguments.cs      # new: argparse-style parsing, as SizeArguments
│   └── ModuleCheck.cs          # new: the four problems (data-model.md)
├── Retrieval/Lookup.cs         # FindSection factored out of Run; no behaviour change
├── Size/Budgets.cs             # ModuleSection = 3000
├── Size/ReadSet.cs             # the pointer's new form; DigestSection through Lookup.FindSection; the miss flag
├── Size/Flags.cs               # FlagKind.PointerMiss
├── Commands/SizeCommand.cs     # the module: line in --check and the full report
└── CommandTable.cs             # ModuleCommand added

tests/Dmc.Tests/
├── Modules/                    # new: ModuleCheckTests, ModuleCommandContract, ModuleFixtures, timing
├── Size/                       # pointer forms, the miss flag, the module: line
├── Retrieval/                  # existing lookup tests unchanged
└── Toolkit/                    # prompt tests (contracts/prompts.md)

kit/
├── skills/dmc-ingest-module/SKILL.md
├── skills/dmc-resume-campaign/SKILL.md
├── templates/module-progress.md
└── dm-rules.md
```

**Structure Decision**: The check gets its own namespace and command (research R1); everything else changes in place.

## Complexity Tracking

No violations.
