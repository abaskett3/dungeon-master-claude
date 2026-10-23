# Implementation Plan: Campaign Export and Import

**Branch**: `001-campaign-import-export` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-campaign-import-export/spec.md`

## Summary

Add `dmc campaign export|import|show`. Export writes one campaign folder, and the general `adventures/player-options.md` if there is one, into a ZIP with a `manifest.json` listing every file's size and SHA-256, named `<campaign>-<YYYYMMDDHHmm>.zip` from the local date and time (for example `the-silver-egg-202610220605.zip`). Import, which takes the campaign name from the manifest and never from the file name, checks the whole archive, extracts it into a staging folder inside `adventures/`, and commits with same-volume renames, so a campaign appears whole or not at all and nothing that exists is ever overwritten. Show prints an archive's summary and writes nothing. Everything uses the base class library, so `dmc` stays one Native AOT binary with no new package. Design choices and their alternatives are in [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`), Native AOT for releases and CI

**Primary Dependencies**: base class library only: `System.IO.Compression` (ZIP), `System.Security.Cryptography` (SHA-256), `System.Text.Json` with a source-generated context. No new package in `Directory.Packages.props`.

**Storage**: files: campaign folders under `adventures/`, export archives under `exports/` at the toolkit root (new, git-ignored)

**Testing**: MSTest 4 in `tests/Dmc.Tests` (library tests in-process, plus the published-binary command cases and one-line error guard)

**Target Platform**: the platforms `dmc` ships for (`.github/workflows/release.yml`): win-x64, win-arm64, linux-x64, linux-arm64, osx-arm64; CI tests on Linux, Windows and macOS

**Project Type**: CLI (one more subcommand of `dmc`)

**Performance Goals**: export or import of the largest existing campaign (about 0.5 MB of Markdown plus maps) in under 5 seconds (SC-006); in practice well under 1 second

**Constraints**: warnings are errors, AOT and trim analyzers on; failures one line, success at most four lines (SC-005); no line-ending or encoding change to any campaign file; all or nothing import; never overwrite or delete an existing campaign file

**Scale/Scope**: one campaign per run; tens to a few hundred files; archives of a few MB at most

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Story consistent, player has fun | A round trip is byte for byte (FR-002, SC-002) and import is all or nothing (FR-009), so a restored or moved campaign resumes exactly where it stopped. It changes no prompt and no rule of play. | Pass |
| II. Play pays nothing for toolkit work | Lives in `dmc`, which the player already installs. `CLAUDE.md` is not changed; DMC finds the command through `dmc --help` when the player asks. No new install. | Pass |
| III. Each rule has one home | The command's behaviour is documented in `.docs/dmc-reference.md`; the user guide and campaign files document point there. The module name is read from where `module-progress.md` already records it (research R8). | Pass |
| IV. Documentation goes with every change | Planned in the same PR: `.docs/dmc-reference.md` (new § campaign, the no-command list), `.docs/user-guide.md` (§ Your files: backups and moving computers), `.docs/campaign-files.md` (`exports/` in the layout), `.docs/decisions.md` (ZIP with manifest, staging import), `.docs/roadmap.md` (status row), `.docs/architecture.md` (the command list on line 15 and the diagram on line 34), `README.md` (the `bin/` line in the layout, and `exports/` beside it), XML doc comments on every new type and member. `.gitignore` gains `exports/*`. | Pass |
| V. Tests skip locally and fail on CI | Nothing needs an optional tool; the command cases use the published `dmc` through the existing `DmcRunner`, which already skips locally and fails on CI when it is missing. | Pass |
| Build: Native AOT, warnings as errors | BCL only; JSON through a source generator (research R4). | Pass |
| Output: one-line errors, small output | Every failure through `DmcException`; the full message table is in the contract. | Pass |
| Workflow: git ignores personal files | Exports hold campaign data and spoilers, so `exports/` is ignored like `adventures/`; nothing under `adventures/`, `modules/` or `rules/` is added to git. | Pass |

No violations, so Complexity Tracking is empty.

**Re-check after Phase 1:** the data model and contract add no dependency, no prompt text and no second home for a rule. The one new folder (`exports/`) is ignored by git and outside `adventures/`, as `.docs/user-guide.md` § Your files asks for backups. Still Pass.

**Re-check after the 2026-10-22 refinement** (file name `<campaign>-<YYYYMMDDHHmm>.zip`, research R7): only the default name changes. It has one home, the contract's Usage block, which `.docs/dmc-reference.md` will copy (IV); the name has no `:` and so is valid on every system (Output, cross-system). Still Pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-campaign-import-export/
├── spec.md
├── plan.md                       # this file
├── research.md                   # Phase 0
├── data-model.md                 # Phase 1
├── quickstart.md                 # Phase 1
├── contracts/
│   └── campaign-command.md       # Phase 1: usage, output lines, failures
├── checklists/
│   └── requirements.md
└── tasks.md                      # Phase 2, written by /speckit-tasks
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── CommandTable.cs               # + new CampaignCommand()
├── Commands/
│   └── CampaignCommand.cs        # parses export|import|show, prints --help, hands off to the library
└── Campaign/
    ├── CampaignArguments.cs      # Argparse-style parsing of the three verbs
    ├── Manifest.cs               # the manifest records and their JsonSerializerContext
    ├── ArchivePaths.cs           # the portable-path rules (research R6), used by export and import
    ├── CampaignExporter.cs       # folder → ZIP through a .partial file; module detection
    ├── CampaignImporter.cs       # verify, stage in adventures/.dmc-import-*, commit by renames, roll back
    └── ExportSummary.cs          # what show prints, shared with import's checks

tests/Dmc.Tests/Campaign/
├── CampaignCases.cs              # CommandCase list for the one-line error guard and the contract test
├── CampaignCommandContract.cs    # published-binary checks of the contract's lines and exit codes
├── CampaignExportTests.cs        # export in-process: the archive, the manifest, the file name, every refusal
├── CampaignShowTests.cs          # show: the lines, the size, nothing written
├── CampaignFixtures.cs           # the sample campaign and the hand-written (and crafted) archives
├── CampaignRoundTripTests.cs     # export then import byte for byte, empty folders, non-ASCII names
├── CampaignImportRefusalTests.cs # every refusal leaves adventures/ untouched
└── ArchivePathsTests.cs          # the path rules on every system

.gitignore                         # + exports/*
.docs/                             # see Constitution Check IV
```

**Structure Decision**: one command class in `Commands/` delegating to a library folder, `Campaign/`, as `DigestCommand` delegates to `Digest/` and `MapRedraw` takes the toolkit root as a parameter. The library takes the root as an argument so tests aim it at a temporary folder, with no test hook in the binary.

## Complexity Tracking

None.
