# Implementation Plan: Measure What a Resume Reads, in Tokens

**Branch**: `006-dmc-size` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/006-dmc-size/spec.md` (issue #174)

## Summary

A new read-only `dmc size [NAME] [--check] [--folder PATH]` command lists the files `dmc-resume-campaign` step 2
reads for a campaign, chosen by the same rules, and gives each one's estimated tokens (bytes ÷ 4) for the part the
resume reads, the total, each whole-read file's sections largest first, and flags: near the Read tool's cap, over a
per-file budget, canon over its limit, and the bloat patterns the templates name. `--check` is the play form: silent
when all is well, one line per thing to act on otherwise.

Every size rule in the kit moves from lines and words to these token budgets, which live in one place in `dmc`: the
canon limit becomes 19k to read whole and 16k to archive down to, high enough that the long campaign's canon stays read
whole (consistency over token cost). The resume runs `dmc size --check` once for its canon decision and a near-cap
warning; the full save's consistency check runs it and is not complete until it prints nothing or the report says
why each line stands; `review-transcript` records the full report with the slips. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT for release (`src/Dmc.Cli`), warnings as errors

**Primary Dependencies**: none new. The BCL only (`System.IO`, `System.Text`, source-generated `Regex`); argument
parsing through the existing `Argparse`; the DMC folder through `DmcFolder`

**Storage**: none. Reads campaign files in a DMC folder (`adventures/<campaign>/`, `adventures/player-options.md`,
`modules/<module>/`); writes nothing

**Testing**: MSTest in `tests/Dmc.Tests`: library tests in-process, a contract test run in-process and against the
published binary, rows in the error-path guard and the output-bytes test, and toolkit tests over `kit/`. Fixture
campaigns built in temporary folders from the templates (real campaigns are git-ignored)

**Target Platform**: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64` (`SupportedPlatforms`)

**Project Type**: CLI subcommand of `dmc`, plus prompt changes in `kit/` and one repository skill

**Performance Goals**: measures the long campaign (about 30 files, 300k bytes) in well under a second, so it costs
nothing noticeable at every resume and full save

**Constraints**: one-line failures; full report about 40 lines or fewer, `--check` 0 lines when all is well and at
most 12 otherwise (everything printed lands in DMC's context); no tokenizer, no network; no writes

**Scale/Scope**: one campaign per run; campaigns of up to a few hundred files; one new command, about six source
files, one test folder, two kit skills, three templates, one repository skill, one settings file, five documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this feature meets it | Pre | Post |
|---|---|---|---|
| I. The story stays consistent and the player has fun | The measurement exists so files read at resume are complete and current: it flags a file about to be cut short, and the canon limit is set so live facts stay read whole rather than searched (R5). The command reports and never edits a story file. | Pass | Pass |
| II. Play pays nothing for toolkit work | `--check` prints nothing when all is well (FR-013a). One command call replaces a Grep count at resume and two in the save, and the numbers leave the skills and templates (R11), so the play prompts get shorter. `kit/dm-rules.md` is unchanged. Players install nothing new. | Pass | Pass |
| III. Each rule has one home | Budgets and limits live only in `Budgets` in `dmc`; the skills say "what `dmc size` checks" without numbers, and `.docs/campaign-files.md` describes them. The read rules stay in resume step 2; the code mirrors them and a toolkit test keeps the markers in step (R2). | Pass | Pass |
| IV. Documentation goes with every change | XML doc comments on every type and member; `.docs/` updates listed under Documentation below; the PR description lists them. | Pass | Pass |
| V. Tests skip locally and fail on CI | No new external tool. The contract test uses the published binary, which already follows `TestEnv.Need`. | Pass | Pass |
| Build: Native AOT, warnings as errors | No reflection, no new package; regexes are source-generated as elsewhere. | Pass | Pass |
| Output: one-line errors, small output | Failures through `DmcException`; new rows in `ErrorPathTests`; output bytes checked. | Pass | Pass |

No violation, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/006-dmc-size/
├── spec.md              # The specification (with clarifications)
├── plan.md              # This file
├── research.md          # Phase 0: decisions and measurements
├── data-model.md        # Phase 1: read set, measurements, budgets, flags, report
├── quickstart.md        # Phase 1: how to validate it
├── contracts/
│   └── dmc-size.md      # Phase 1: the command's interface
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks, not made here)
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── Commands/
│   └── SizeCommand.cs           # new: parses arguments, finds the folder and campaign, prints a form
├── Size/                        # new
│   ├── SizeArguments.cs         # NAME, --check, --folder, --help, via Argparse
│   ├── ReadSet.cs               # resume step 2's rules: which files, which part (R2, R9, R10)
│   ├── Measurement.cs           # bytes, lines, tokens, sections of a part (R1)
│   ├── Budgets.cs               # every budget and limit, the one home (R4, R5)
│   ├── Flags.cs                 # the flag kinds and their tests (R3, R6)
│   └── SizeReport.cs            # Measure(dmcFolder, campaign); full and check formatting (R7)
└── CommandTable.cs              # add SizeCommand

tests/Dmc.Tests/
├── Size/                        # new
│   ├── SizeFixtures.cs          # fixture campaigns from kit/templates, aged and grown as each test needs
│   ├── SizeLibraryTests.cs      # read set, measurement, flags, canon decision
│   └── SizeCommandContract.cs   # arguments, output, exit codes, no writes; in-process and black-box
├── TokenCost/ErrorPathTests.cs  # add the size failures
├── TokenCost/OutputBytesTests.cs# add dmc size runs
└── Toolkit/ToolkitTests.cs      # resume step 2 markers; no size rule in lines or words in kit/

kit/
├── skills/dmc-resume-campaign/SKILL.md     # step 2: run dmc size --check once; item 7 by its output
├── skills/dmc-campaign-full-save/SKILL.md  # steps 1, 2, 4, 5, 6 lose numbers; step 7 runs the check (FR-015a)
└── templates/campaign/
    ├── game-state.md            # drop "keep it under about 100 lines"
    ├── story/dm-notes.md        # drop "about 25 lines", "about 40 lines at most"
    └── story/summary.md         # drop "under about 600 words"

.claude/
├── skills/review-transcript/SKILL.md       # § Context and tokens: run dmc size --folder and record it
└── settings.json                           # allow Bash(dmc size *), PowerShell(dmc size *)
```

**Structure Decision**: one more subcommand in the existing single project, following `.docs/development.md`
§ Adding to `dmc`: a command class in `Commands/`, the work in a `Size/` folder the tests call in-process, a contract
test and error-path rows. No new project.

## Documentation

Changed in the same PR (FR-018, Principle IV):

- `.docs/campaign-files.md`: the size column of "What each file holds" (canon by its token limit; each budget); a
  short "How big the files may be" note pointing at `dmc size`.
- `.docs/architecture.md`: "Starting a session" and "A full save" (canon by tokens, the check in step 7); "Inside
  `dmc`" lists `size`.
- `.docs/user-guide.md`: under "Your files", how to see what a resume reads (`dmc size`), and what the flags mean.
- `.docs/decisions.md`: a new entry, "Size rules are measured in tokens by `dmc size`", with R1, R4, R5 and R7's
  reasons; the entries "Resume and full save are bounded" and the 230-line canon one get a line pointing to it.
- `.docs/roadmap.md`: a row for issue #174.
- `.docs/development.md`: the Layers table names the new tests.
- `README.md`: a short paragraph on `dmc size` beside the one on `dmc campaign`.

## Order of work

1. `Budgets`, `Measurement` and their tests (the estimate and the sections).
2. `ReadSet` and its tests, on fixtures for each rule of resume step 2.
3. `Flags` and the canon decision, with a fixture per flag and its negative.
4. `SizeReport`, `SizeArguments`, `SizeCommand`; the contract test; error-path and output-bytes rows.
5. The toolkit tests (markers, no line or word targets), then the kit prompt and template changes they require.
6. `review-transcript` and `.claude/settings.json`.
7. The documents; then the long campaign by hand (quickstart § 3), recording its numbers in the PR.

## Complexity Tracking

None: the Constitution Check has no violation.
