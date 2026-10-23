# Implementation Plan: Remove macOS Support

**Branch**: `002-remove-macos-support` | **Date**: 2026-10-22 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-remove-macos-support/spec.md`

## Summary

Stop building, testing, documenting and promising macOS (`osx-arm64`). The work is subtraction in five places: the
release and CI workflows drop the macOS runner and the `osx-arm64` archive; the pin file and its writer drop the
macOS hash; both `get-dmc` scripts keep one small macOS check that refuses with a clear message instead of
installing; `dmc`'s source loses its macOS branches (the `open` browser opener, the `macos` platform declaration,
comments); the tests lose their macOS cases and gain one that proves the refusal; and every document that lists
platforms is brought into line, with the decision log marking the macOS decision superseded and recording this one.
No new behavior is added for Windows or Linux. Earlier releases are not touched. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10 (`dmc`, Native AOT per platform); bash and PowerShell (`get-dmc`, launchers, release helper); GitHub Actions YAML

**Primary Dependencies**: None added. PDFtoImage, SkiaSharp and HarfBuzzSharp stay; only the platform they are declared for narrows.

**Storage**: Files only. `tools/dmc-release.json` is the one data file that changes.

**Testing**: MSTest project `tests/Dmc.Tests` (Distribution, Map, Toolkit); `.github/workflows/tests.yml` runs it on each supported OS

**Target Platform**: Windows x64 and Arm, Linux x64 and Arm (glibc 2.34 or later). macOS is removed.

**Project Type**: CLI plus launcher scripts and prompts (a toolkit)

**Performance Goals**: N/A. SC-004 asks that the Mac refusal arrive within 5 seconds; it is a local check before any network use.

**Constraints**: Warnings are errors in `src/Dmc.Cli`, so removing `[assembly: SupportedOSPlatform("macos")]` must not leave a CA1416 warning. A failure of `get-dmc` stays one line `get-dmc: <message>` on stderr, exit 1. Release 0.1.3's published `osx-arm64` archive is not altered.

**Scale/Scope**: About 28 files with a match for macOS, of which roughly 20 change; no new files outside `specs/` and `.docs/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Story and fun | No change to play, campaigns, prompts or `CLAUDE.md`. Campaign files are plain files and behave the same on Windows and Linux (spec Assumptions). | Pass |
| II. Play pays nothing | No `CLAUDE.md` growth; nothing new to install. The only player-visible change is a refusal message on a system the toolkit no longer builds for. | Pass |
| III. One home | The platform list lives in `.docs/releases.md` § Platforms and `.docs/user-guide.md`; other documents point to them or say the same thing in a clause. No rule is copied into the constitution. | Pass |
| IV. Documentation | The documents to change are listed in [research.md](research.md) R7 and in the task list; the decision log and the roadmap are included. A change to scripts and workflows updates their header comments (CLAUDE.md § Git and GitHub). | Pass, to be verified in the PR |
| V. Tests skip locally, fail on CI | The macOS refusal test needs no tool that can be missing (a fake `uname`, an environment variable for PowerShell), so nothing skips. | Pass |
| Build constraints | Native AOT per platform and warnings-as-errors stay; the one-line `get-dmc: ...` error form is kept. | Pass |

No violations, so Complexity Tracking is empty. Re-checked after Phase 1: the design adds one test-only
environment variable (`DMC_GET_TEST_SYSTEM`) to `get-dmc.ps1`, in the same style as its existing
`DMC_GET_TEST_MACHINE`; this adds no new rule and breaks no principle.

## Project Structure

### Documentation (this feature)

```text
specs/002-remove-macos-support/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── get-dmc-refusal.md   # Phase 1 output: the setup scripts' macOS refusal
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
.github/
├── workflows/
│   ├── release.yml          # drop the osx-arm64 matrix row, the osx-* pack branch, osx in the rid loops, "== 5" -> 4, release notes wording
│   └── tests.yml            # drop the macos-latest row and the header wording
└── release/write-pin.sh     # drop osx-arm64 and osx-x64 from the platforms it knows; header comment

tools/
├── dmc-release.json         # drop the "osx-arm64" hash (the existing pin, tag dmc-v0.1.3, keeps its other four)
├── get-dmc.sh               # Darwin -> macOS refusal; drop Rosetta, osx-x64 and the osx shasum branch; header and message wording
└── get-dmc.ps1              # $IsMacOS -> macOS refusal (plus the DMC_GET_TEST_SYSTEM test hook); drop osx-x64; header wording

src/Dmc.Cli/
├── SupportedPlatforms.cs    # drop [assembly: SupportedOSPlatform("macos")]; comment "these three" -> "these two"
├── Maps/Browser.cs          # drop the macOS "open" opener: sh runs xdg-open; doc comments
├── Dice/RandomBytes.cs      # comment only (Security framework on macOS)
├── Pdf/PdfFile.cs           # comment only (Windows and macOS do not use OpenSSL)
├── Commands/PlayCommand.cs  # comment only ("Linux and macOS" -> "Linux")
├── Commands/ContextCheckCommand.cs  # comment only
├── Campaign/CampaignImporter.cs     # comment only
└── Dmc.Cli.csproj           # comment only (Xcode command line tools)

tests/Dmc.Tests/
├── Distribution/GetDmcTests.cs   # Darwin rows -> a refusal test for both scripts; rid lists drop osx-arm64
├── Distribution/Bootstrap.cs     # Rid no longer yields "osx"
├── Map/BrowserTests.cs           # drop IsMacOS() choices
├── Campaign/CampaignImportRefusalTests.cs, Pdf/OutputStagingTests.cs, Pdf/PdfLibraryTests.cs   # comments only

README.md, .docs/{architecture,development,product-overview,user-guide,releases,decisions,roadmap}.md   # see research.md R7
```

**Structure Decision**: Single existing repository; this is a removal inside the existing layout. No new source
file is added. The only new files are the spec-kit documents under `specs/002-remove-macos-support/`.

## Complexity Tracking

No constitution violations, so nothing to justify.
