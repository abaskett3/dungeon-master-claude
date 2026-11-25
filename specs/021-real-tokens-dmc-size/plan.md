# Implementation Plan: Real tokens in `dmc size`

**Branch**: `021-real-tokens-dmc-size` | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/021-real-tokens-dmc-size/spec.md` (issue #246, Slips 0.3, part of #239,
plan #238)

## Summary

Every token figure `dmc` prints is bytes ÷ 4, about 1.6 times too low. `dmc size` says the long campaign's resume
reads 57k, and the resumes in the logs grew by 109k and 112k. The near-cap flag fires past the Read tool's cap.

Measured for this plan (research R1, R2, R4):

- In 611 batches of Read calls in 194 logs, a Read call costs 0.4 tokens a character of text and about 160 a call.
  Line numbers cost close to nothing. Out of sample, the median error on large batches is 2.1%.
- Real counts of four campaign files, taken from Claude Code's usage figures, give 0.38 to 0.43 tokens a character.
  The counts are the same on Sonnet and Opus.
- In the two named resumes, tool results were about 96% of the growth, and the measure matches it within 2%.

The change:

- One measure, 2 tokens for 5 characters, in `Measurement`, replaces every bytes ÷ 4 (eleven, research R6).
- A Read call costs that plus 160 tokens.
- Every budget becomes exactly 1.6 times its number, so it allows the same text, and no part is newly over. Research
  R3 proves this.
- The near-cap mark becomes 18,000 of Read cost, and canon within its limit is not flagged.
- The total becomes the Read cost: about 95k for the long campaign.
- No migration is needed: no campaign holds anything different.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); MSTest (`tests/Dmc.Tests`)

**Primary Dependencies**: none new; no tokenizer and no network (FR-007)

**Storage**: campaign Markdown files, read only

**Testing**: unit and contract tests under `tests/Dmc.Tests/Size`, `Modules`, `Retrieval`, `Campaign`, `ContextCheck`,
`Toolkit`. The calibration checks are by hand on the player's logs (quickstart Q4).

**Target Platform**: Windows and Linux, the published `dmc`

**Project Type**: CLI

**Performance Goals**: no measurable change. Counting characters replaces counting bytes, over the lines already read.

**Constraints**:

- No part may be newly over a budget (spec Clarifications; research R3).
- The `--check` lines keep their words, because DMC and the skills act on them.

**Scale/Scope**: `Measurement.cs`, `Budgets.cs`, `Flags.cs`, `SizeReport.cs`, `ReadSet.cs`, `SizeCommand.cs`,
`ModuleCheck.cs`, `SequelDigest.cs`, `Lookup.cs`, `CanonReminder.cs`, `SceneRead.cs` and `Recall.cs` (comment only);
their tests; one kit skill phrase; four documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | Names which files hold new state | Pass. No new state, and no file holds less text, so the full save rewrites no more lines (the risk of #262) |
| Core 2 Fun | Says what the player gains | Pass. A file about to be cut at resume is flagged before the cut, so DMC plays from the whole file |
| Core 3 Low cost | Cost counted as Development 1 says | Pass. Play reads the same files. The `--check` output keeps its lines |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill; a `dmc size` result where the resume's reads change | Pass. 0 lines to `kit/dm-rules.md`. `dmc-new-campaign`: one phrase replaced (step 1d's "a kind near 20k tokens"), about the same bytes, read only at a sequel import. The resume's reads do not change; quickstart Q3 gives the new `dmc size` figure |
| Development 2 No change breaks a campaign | Names the migration, or why none | Pass. None needed: no template, shape or allowed text changes (research R3, R8); SC-004 checks it on every campaign at hand. Modules and digests are checked only at ingest and sequel import (research R5) |
| Development 3 One step | Install or update gains a step | Pass. No dependency, key or download. `dmc update` replaces the binary |
| Development 4 Grounded | Every claim cites a file or says "unverified" | Pass. research.md cites every count. Not verified: the Read cap re-measured (R5), and the Read tool's handling of CR (R3, which does not depend on it) |
| Development 6 One home | Names the home of each rule | Pass. The rate's home is `Measurement`; the numbers' home stays `Budgets.cs`; the reason goes in the amended decision |
| Development 7 Tests skip locally | A test that needs a tool calls `TestEnv.Need` | Pass. No new test needs a tool; the calibration scripts are not tests |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 4 | Concise; the PR names its documents | Planned. See Design step 6 |
| Build: AOT, one-line errors, small output | — | Pass. No reflection, no new output line |

No violations. Complexity Tracking stays empty.

**Re-check after Phase 1**: unchanged. The design adds no file outside `specs/`. The canon exemption (research R5)
keeps Core 1 and the spec's rule against extra trimming.

## Design

1. **The measure** (`Measurement.cs`; data-model.md):
   - `Line` gains `Characters`.
   - Add `Tokens(long characters)`, `ReadCallTokens = 160` and `ReadCost`.
   - `Section` and `Part` sum characters and convert once. `ExtraBytes` becomes `ExtraCharacters`.
   - `Part.ReadCost` counts one call, plus one for the recall output.
   - XML comments on every changed member, private ones included.
2. **The numbers** (`Budgets.cs`):
   - Every token member becomes 1.6 times its old value (research R3 table), and `NearCapTokens` becomes 18,000.
   - The summaries say what each is compared with. The comments of `SceneReadBytes` and Recall's cap give their
     approximate tokens in the new unit.
3. **The callers** (research R6):
   - `Flags.cs` compares text tokens. The near-cap flag compares `ReadCost` and skips canon.
   - In `SizeReport.cs`, `Total` becomes the sum of `ReadCost`. The total line follows the contract, and the
     table's near-cap note follows the flag's rule.
   - `ReadSet.cs:485`, `CanonReminder.cs:69` and `SceneRead.cs:47` compare text tokens with `CanonWholeBelow`.
   - `ModuleCheck.cs:74` and `:91`, `Lookup.cs:143` and `SequelDigest.cs:258` use `ReadCost` against the mark, and
     text tokens against `ModuleSection`. The sequel check reads the file's text for its characters.
4. **The help and output text** (`SizeCommand.cs`; contracts/dmc-size-output.md): the header, the help's unit
   sentence and the total line.
5. **Tests** (research R7):
   - Fixtures size their files through `Measurement.Tokens` or its inverse, so a later change of rate moves them too.
   - New `Measurement` unit tests: the R3 edges, the conversion, and `ReadCost`.
   - Update the two header assertions in `SizeCommandContract.cs`.
   - New tests for the near-cap flag: it skips canon, and it fires at 18k of Read cost.
   - A toolkit test, `NoCodeDividesBytesIntoTokens`, finds no `/ 4` after a byte sum or `Length` in `src/`. It has an
     XML doc comment.
6. **Documents**:
   - **`.docs/decisions.md`**, "Size rules are measured in tokens by `dmc size`": an **Amended 2026-11-24** paragraph
     (issue #246, spec 021) gives:
     - the measure and its calibration (research R1, R2, R4);
     - why the budgets keep their text and not their numbers (#262), and the 1.6 factor;
     - the near-cap mark and the canon exemption;
     - why no migration is needed;
     - the limits: it is an estimate, small batches vary by about 8%, and it must be measured again if a model's
       tokenizer changes;
     - the Crooked Moon module files and the Silver Egg digest files now over the mark, which their checks will report
       at the next ingest or sequel import.
     - The Decision line's "bytes ÷ 4" and its numbers are left as they were, with the amendment saying what replaced
       them, as earlier amendments do.
   - **`.docs/campaign-files.md`**:
     - `:85` and `:103`: the unit and the mark.
     - The budget table, in the new numbers.
     - `:239` and `:255`: "near 20k tokens" and "over 20k tokens" become the Read cap mark.
   - **`.docs/dmc-reference.md`**: § size (`:692`, `:703`, the example), § module (`:737`, `:746`) and § sequel
     (`:666`), with the new numbers and header.
   - **`kit/skills/dmc-new-campaign/SKILL.md:47`**: "a kind near 20k tokens goes on in `people-2.md`" becomes "a kind
     that `dmc campaign sequel --check` reports too large goes on in `people-2.md`". This puts the number in its one
     home (Development 6).
   - **`.docs/roadmap.md`**: issue #246's row and the status date.
   - **The PR**:
     - It says `Closes #246` and `Part of #239`.
     - Its Development 2 statement says no migration is needed, with quickstart Q2's outputs.
     - It quotes Q3 and Q4.

## Project Structure

### Documentation (this feature)

```text
specs/021-real-tokens-dmc-size/
├── spec.md
├── plan.md              # This file
├── research.md          # R1 to R8
├── data-model.md        # The measure and the numbers
├── quickstart.md        # Q1 to Q5
├── contracts/
│   └── dmc-size-output.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── Size/Measurement.cs          # the measure, its one home
├── Size/Budgets.cs              # the numbers × 1.6; NearCapTokens 18,000
├── Size/Flags.cs, SizeReport.cs, ReadSet.cs
├── Commands/SizeCommand.cs      # help text
├── Modules/ModuleCheck.cs
├── Campaign/SequelDigest.cs
└── Retrieval/Lookup.cs, CanonReminder.cs, SceneRead.cs, Recall.cs
tests/Dmc.Tests/
├── Size/                        # fixtures, contract, library tests, new Measurement tests
├── Modules/, Retrieval/, Campaign/SequelDigestTests.cs, ContextCheck/CanonReminderContract.cs
└── Toolkit/ToolkitTests.cs      # NoCodeDividesBytesIntoTokens
kit/skills/dmc-new-campaign/SKILL.md
.docs/decisions.md, campaign-files.md, dmc-reference.md, roadmap.md
```

**Structure Decision**: no new source file. The measure goes in `Measurement`, which already holds the counting, and
the numbers stay in `Budgets`.

## Complexity Tracking

None.
