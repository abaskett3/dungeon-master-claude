# Implementation Plan: Look Up a Section and Recall a Fact, Without Reading Whole Files

**Branch**: `009-retrieval-lookup-recall` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/009-retrieval-lookup-recall/spec.md` (issue #179, Phase A)

## Summary

Two read-only `dmc` commands over the markdown, building what they need on every call and storing nothing:

- **`dmc lookup SOURCE HEADING...`** prints one section exactly (a heading down to the next of the same or a higher
  level) from a campaign, a module's digest and reference files, or a markdown rules reference. Matching is exact on a
  normalised heading, then on all of the request's words; several matches print a list, a section over the Read cap
  prints its line range. A PDF rules reference gives its index rows, or the page numbers its extracted text holds the
  words on, never an entry's text.
- **`dmc recall [NAME] WORDS...`** ranks the lines of the campaign, its Predecessor chain and their modules by BM25
  weighted by authority (canon, archives, sheets, other story files, session logs; predecessors a little lower; modules
  lowest), trims long lines around the match, and prints the best within 6,000 bytes, each labelled with its campaign,
  file, line, session and a DM-only mark.

The kit's play rules (`kit/dm-rules.md` and `dmc-setup-rules`) say when DMC recalls and looks up, with the fallback to
searching as before; the resume and full save do not change (FR-020a). A PowerShell harness reruns the issue's recall
test on the maintainer's play folder. Step 1 (update, migration 004, size measurement) is the maintainer's, after the
merge. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT for release (`src/Dmc.Cli`), warnings as errors

**Primary Dependencies**: none new. The BCL only (`System.IO`, `System.Text`, source-generated `Regex`); argument
parsing through `Argparse`; the DMC folder through `DmcFolder`; campaign resolution and the near-cap mark shared with
`Dmc.Size` (`SizeReport.ActiveName`, `Budgets.NearCapTokens`, `Budgets.NearCapLines`)

**Storage**: none. Reads `adventures/<campaign>/`, `modules/<module>/` and `rules/` in a DMC folder; writes nothing

**Testing**: MSTest in `tests/Dmc.Tests`: library tests in-process; contract tests in-process and against the published
binary (`DmcRunner`); rows in `TokenCost/ErrorPathTests` and `TokenCost/OutputBytesTests`; a timing test on the AOT
build; toolkit tests over `kit/`. Fixture campaigns, modules and rules built in temporary folders (real campaigns are
git-ignored). The recall-test harness runs only on the maintainer's machine

**Target Platform**: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64` (`SupportedPlatforms`)

**Project Type**: two CLI subcommands of `dmc`, a developer script in `tools/`, and prompt changes in `kit/`

**Performance Goals**: a recall over a few megabytes of markdown (the measured sequel, its predecessor and the module)
in under one second with the release build (SC-004); a lookup in well under that

**Constraints**: recall output at most 6,000 bytes; lookup prints only the section or one line; one-line failures; no
tokenizer, network or model; no writes; `kit/dm-rules.md` at most two lines longer (212 now)

**Scale/Scope**: one campaign chain per recall (two campaigns and a module today, about 3 MB); two new commands, about
eight source files in a new `Dmc.Retrieval` namespace, one test folder, one script, three kit files, six documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this feature meets it | Pre | Post |
|---|---|---|---|
| I. The story stays consistent and the player has fun | Recall finds the exact name, price or ruling the files hold, ranked so canon outweighs narrative and a module never outranks the campaign (R1, R6); lookup prints a stat block byte for byte, matched exactly or not at all (R7); a PDF's extracted text, which drops digits, is never printed as a rule (R9). Neither command writes. "Not found" never means "not true" (FR-021). | Pass | Pass |
| II. Play pays nothing for toolkit work | `kit/dm-rules.md` lines are rewritten in place, at most two added, pinned by a test (R14). A recall costs at most about 1.5k tokens where today's Greps return whole long lines. Players install nothing new; `Bash(dmc *)` already allows the commands. | Pass | Pass |
| III. Each rule has one home | When to recall and look up lives in `dm-rules.md` § Accuracy and consistency and § Running a published module; the rules-reference detail in `dmc-setup-rules`; the ranking and limits only in `Dmc.Retrieval`, described in `.docs/dmc-reference.md`. The resume and full save keep theirs (FR-020a). | Pass | Pass |
| IV. Documentation goes with every change | XML doc comments on every type and member; a header comment on the script; `.docs/` updates listed below; the PR lists them and says `Part of #179`. | Pass | Pass |
| V. Tests skip locally and fail on CI | No new external tool for the tests. The timing test uses the AOT build through `TestEnv`, as existing timing tests do. The harness is a developer script, not a test, so it neither skips nor runs in CI. | Pass | Pass |
| Build: Native AOT, warnings as errors | No reflection, no new package; regexes source-generated; plain dictionaries and spans (R13). | Pass | Pass |
| Output: one-line errors, small output | Failures through `DmcException`; new rows in `ErrorPathTests`; output size checked in `OutputBytesTests` and by the recall cap. | Pass | Pass |

No violation, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/009-retrieval-lookup-recall/
├── spec.md              # The specification (with clarifications)
├── plan.md              # This file
├── research.md          # Phase 0: decisions
├── data-model.md        # Phase 1: sources, sections, scope, units, hits
├── quickstart.md        # Phase 1: how to validate it, Step 1 and the recall test
├── contracts/
│   ├── dmc-lookup.md    # Phase 1: lookup's interface
│   ├── dmc-recall.md    # Phase 1: recall's interface
│   └── recall-test.md   # Phase 1: the harness and its questions file
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks, not made here)
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── CommandTable.cs                  # + LookupCommand, RecallCommand
├── Commands/
│   ├── LookupCommand.cs             # dmc lookup: arguments, folder, print
│   └── RecallCommand.cs             # dmc recall: arguments, folder, print
└── Retrieval/
    ├── RetrievalArguments.cs        # both command lines, through Argparse (R10)
    ├── MarkdownHeadings.cs          # ATX headings outside fences, sections, the normalised key (R7)
    ├── LookupSource.cs              # campaign / module / rules, the /path form, the clash rule (R8)
    ├── Lookup.cs                    # match, list, too-large, the PDF rules path (R7, R9)
    ├── Words.cs                     # tokens, common words, stemming (R2)
    ├── RecallScope.cs               # the chain, modules, files, tiers, weights, notices (R1, R6, R11)
    ├── LineUnits.cs                 # units, scene and citation, DM-only (R3)
    └── Recall.cs                    # BM25 × weight, order, trim, cap, the last line (R1, R4, R5)

tests/Dmc.Tests/
├── Retrieval/
│   ├── RetrievalFixtures.cs         # fixture campaigns (a chain), a module, markdown and PDF rules
│   ├── LookupLibraryTests.cs
│   ├── LookupCommandContract.cs     # in-process and binary
│   ├── RecallLibraryTests.cs
│   ├── RecallCommandContract.cs     # in-process and binary, the cap, labels, exclusions
│   └── RecallTimingTests.cs         # SC-004 on the AOT build
├── TokenCost/ErrorPathTests.cs      # + one row per one-line failure
├── TokenCost/OutputBytesTests.cs    # + lookup and recall
└── Toolkit/ToolkitTests.cs          # + dm-rules.md line ceiling; the new commands named in kit/

tools/recall-test.ps1                # the recall-test harness (R12)

kit/
├── dm-rules.md                      # lines 71, 73, 78, 113 rewritten in place (R14)
├── skills/dmc-setup-rules/SKILL.md  # § Using it in play: lookup first
├── skills/dmc-new-campaign/SKILL.md # the Predecessor line: recalled
└── templates/campaign/player-options.md  # the Predecessor comment: recalled
```

**Structure Decision**: One CLI project, as before. The two commands share a new `Dmc.Retrieval` namespace, alongside
`Dmc.Size`, whose campaign resolution and near-cap mark they reuse rather than restate (Constitution III). Commands stay
thin, as `SizeCommand` is: they find the folder and print; the library takes the folder as a parameter, so the library
tests need no process.

**Documentation**: `.docs/dmc-reference.md` (two command sections, the command list in § Rules that apply to every
command), `.docs/decisions.md` (markdown retrieval with no stored index; ranking by weighted BM25; lookup's exact
matching; the PDF rules path; play only, behind Phase D's gate), `.docs/roadmap.md` (the #179 row, Phase A),
`.docs/user-guide.md` (one line: DMC can look things up faster; nothing for the player to do), `.docs/development.md`
(the recall-test harness under Tests), `.docs/architecture.md` (the commands row), and `README.md`, which describes
`dmc size` for players and gains a short section beside it on `dmc lookup` and `dmc recall`. No migration: no campaign
file changes shape.

## Complexity Tracking

No violations.
