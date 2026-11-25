# Implementation Plan: The slip test, a replay runner that passes its pilot

**Branch**: `claude/project-thread-bs1df4` (spec 024) | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/024-slip-test-runner/spec.md` (issue #249, Phase 1 of #238)

## Summary

A console program, `slip-test`, replays real play sessions from chosen player messages and measures how often the
replies slip, in two arms at once. It writes the base log a replay continues (truncated, the rules text and the folder
path rewritten), restores the copy folder's campaign files from Claude Code's file history, runs
`claude -p --resume --fork-session`, checks in a pilot that the replays saw what play saw, exports the replies blind,
and turns the scores into rates with intervals over fork points. Three probes on Claude Code 2.1.331 (research R1 to
R3) showed that an edited log can be continued and that its edit reaches the model, and that the edit drops earlier
thinking blocks, which the pilot must compare between arms.

## Technical Context

**Language/Version**: C# on .NET 10 (`src/Dmc.SlipTest`, assembly `slip-test`)

**Primary Dependencies**: none beyond the base library (`System.Text.Json` nodes, `System.Diagnostics.Process`);
Claude Code on the maintainer's computer at run time

**Storage**: the work folder (contract); nothing in the repository

**Testing**: MSTest, `tests/Dmc.Tests/SlipTest/`, on invented logs, file history and scores; the live pilot by hand
(quickstart)

**Target Platform**: the maintainer's computer (Windows; Linux works the same)

**Project Type**: developer console program, beside `dmc`, never released

**Performance Goals**: none; a run is bound by the model (#237: about $0.02 for a fork run)

**Constraints**: never writes into the DMC folder or its Claude Code project folder (FR-010); no text of play in
`pilot.md` (FR-007); one-line errors

**Scale/Scope**: about 50 fork points, 1 to 3 runs each, two arms: up to 300 runs per trial

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | Names which files hold new state | Pass. No campaign state. The trial's state is in the work folder (contract) |
| Core 2 Fun | Says what the player gains | Pass. A fix ships only when a measure shows it decreases slips (#238) |
| Core 3 Low cost | Cost counted | Pass. Nothing in play. Runs cost model usage on the maintainer's account, recorded per run (FR-006) |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill | Pass. 0 lines to `kit/dm-rules.md` and to every skill; the resume's reads do not change |
| Development 2 No change breaks a campaign | Names the migration, or why none | Pass. No campaign file, template or kit file changes; the runner only reads the DMC folder |
| Development 3 One step | Install or update gains a step | Pass. Nothing reaches players |
| Development 4 Grounded | Every claim cites a file or says "unverified" | Pass. research.md; R4's file-history format and the maintainer's log format are marked not verified, and the pilot checks them |
| Development 6 One home | Names the home of each rule | Pass. The contract is the runner's home; `.docs/development.md` points to it; the rules for a measurement stay in #240 |
| Development 7 Tests skip locally | A test that needs a tool calls `TestEnv.Need` | Pass. The tests read and write temporary folders only; none runs `claude` |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 4 | Concise; the PR names its documents | Planned. See Documents |
| Build: AOT, one-line errors, small output | — | Pass. The project builds with the shared settings and warnings as errors; it is not published with AOT, so the AOT analyzers are off for it |

## Project Structure

### Documentation (this feature)

```text
specs/024-slip-test-runner/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/slip-test.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Dmc.SlipTest/
├── Dmc.SlipTest.csproj     assembly slip-test, warnings as errors, InternalsVisibleTo Dmc.Tests
├── Program.cs              the commands and their arguments; one-line errors
├── SlipTestException.cs    a failure the user can fix
├── Trial.cs                trial.json, points.tsv, key.tsv, allow.tsv, the lock
├── LogFile.cs              a Claude Code log: records, turns, the fork message, model, effort, kind
├── LogRewriter.cs          the base log: truncate, new id, folder path, rules text
├── FileHistory.cs          the restore of campaign files to a time
├── Folders.cs              the slug, the copy folder, the guards, copying adventures/
├── ClaudeRunner.cs         the claude command line and its JSON output
├── Runner.cs               run and run --pilot, runs.tsv
├── PilotCheck.cs           the four checks
├── BlindExport.cs          export
└── Scoring.cs              score: rates, bootstrap, the pass line, cost

tests/Dmc.Tests/SlipTest/
├── SlipTestFixtures.cs     invented logs and file history
├── LogRewriterTests.cs
├── FileHistoryTests.cs
├── TrialTests.cs
├── PilotCheckTests.cs
├── BlindExportTests.cs
└── ScoringTests.cs
```

**Structure Decision**: a second console project beside `src/Dmc.Cli`, in `dmc.sln`, so `dotnet build` and
`dotnet test` build and test it; `release.yml` publishes `src/Dmc.Cli` only, so it is never released (research R6).
The runner reads the log itself rather than through `src/Dmc.Cli/Digest/`, whose `Session` gathers statistics and
keeps no records.

## Documents

- `.docs/development.md`: a section "The slip test" after "The recall test": what it is for, how to run it, the
  limits of #249, a pointer to the contract; the repository layout table gains `src/Dmc.SlipTest/`.
- `.docs/architecture.md`: the program, in one line where the source is described.
- `.docs/roadmap.md`: spec 024's status.
- `.docs/decisions.md`: a C# program for the slip test, and fork points as the unit of the interval.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| A second program in the solution | The runner must be tested in CI and must not ship to players | A `dmc` subcommand would ship to every player; a script has no tests in CI (research R6) |
