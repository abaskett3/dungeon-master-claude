# Implementation Plan: The break baseline after a typed resume

**Branch**: `019-typed-resume-baseline` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/019-typed-resume-baseline/spec.md` (issue #244, Phase 0 of #238)

## Summary

When the player types "Resume campaign." instead of running `/dmc-resume-campaign`, the context hook measures the
break bands from before the resume, so the first break offer comes too early. In log `082db04e` it came at 154,000
tokens against a right band of about 186,000. The fix has two parts:

- The forward pass of `TranscriptReader.BaselineOf` also watches the first three turns of each log part for a main
  `Skill` call of the resume. When a turn holds one, the last call of that turn becomes the baseline.
- `SheetReminder.For` prints nothing when the named log has no reply since its latest boundary.

Every other case keeps the present method. No kit, skill or campaign file changes.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT on release (`src/Dmc.Cli/Dmc.Cli.csproj`)

**Primary Dependencies**: `System.Text.Json` (`JsonDocument`), already used by `TranscriptReader`; no new package

**Storage**: None. The hook reads the Claude Code log and stores nothing

**Testing**: MSTest in `tests/Dmc.Tests` (`ContextCheck/ContextCheckTests.cs`, `ContextCheck/SheetReminderContract.cs`)

**Target Platform**: Windows and Linux (macOS support was removed, spec 002); the hook runs before every player message

**Project Type**: CLI (`dmc`), run as a Claude Code `UserPromptSubmit` hook

**Performance Goals**: The hook's time over process start stays at about 20 ms on an 18 MB log
(`.docs/dmc-reference.md:827`: 72 ms against 51 ms with no log)

**Constraints**: The hook never fails or blocks a prompt (`ContextCheckCommand.NeverFails`). Its output stays
small. The build must be AOT-safe, with warnings as errors

**Scale/Scope**: Two source files (`TranscriptReader.cs`, `SheetReminder.cs`), two test files and five documents.
The longest main play log measured is about 7 MB

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | Names which files hold new state | Pass. No new state. The hook reads the log only |
| Core 2 Fun | Says what the player gains | Pass. No early break offer after a typed resume, which was mid-correction in `082db04e` |
| Core 3 Low cost | Cost counted as Development 1 says | Pass. 0 lines on an ordinary turn. A typed resume saves up to 10,000 bytes (about 2,500 estimated tokens) of sheet lines |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill; `dmc size` if the resume's reads change | Pass. 0 lines to `kit/dm-rules.md` and 0 to each skill. The resume's reads do not change, so no `dmc size` result is needed |
| Development 2 No change breaks a campaign | Names the migration, or why none | Pass. No campaign file, template or skill changes, so no migration |
| Development 3 One step | Install or update gains a step | Pass. Neither does |
| Development 4 Grounded | Every claim cites a file or says "unverified" | Pass. See research.md. R6 (after a compaction) is marked unverified |
| Development 6 One home | Names the home of each rule | Pass. The rule's home is the code. `.docs/dmc-reference.md` § `context-check` describes it, `.docs/decisions.md` gives the reason, and `.docs/architecture.md:71` points to the reference |
| Development 7 Tests skip locally | A test that needs a tool calls `TestEnv.Need` | Pass. The new tests need no tool |
| Dungeon Master 1 to 4 | — | Not touched. No change to arcs, fights, modules or player choice |
| Documentation 1 to 3 | Concise; player steps; simple developer English | Pass. The user guide (`.docs/user-guide.md:184`) already states the rule in player terms and needs no change |
| Documentation 4 | The PR names the documents it changed | Planned. See Documents below |
| Build: AOT, one-line errors, small output | — | Pass. Same JSON API as today, no new output line |

No violations. Complexity Tracking stays empty.

**Re-check after Phase 1**: unchanged. The design adds one helper and four values to an existing pass. It adds one
early return to `SheetReminder.For`, and no file, setting or output line.

## Design

Research.md R1 to R5 give the detail and the reasons.

1. **`TranscriptReader`** (`src/Dmc.Cli/ContextCheck/TranscriptReader.cs`)
   - A new private helper says whether a log line is a main assistant record with a `Skill` tool-use block for
     `dmc-resume-campaign` or `resume-campaign`. Its byte prefilter parses only lines that hold `resume-campaign`
     or `\u`.
   - `BaselineOf` keeps `before` and `baseline` as they are. It adds four values: the player-message count up to 4,
     whether this turn has a resume call, the last call in this turn, and the resume baseline (data-model.md). It
     closes a turn on the next player message or at the end of the log. A boundary resets everything. The cheap
     skip starts only after the fourth player message and once the baseline is known.
   - The return value is the resume baseline when it is greater than 0. Otherwise it is the present answer.
   - Doc comments to change: the class summary (the baseline list, and "needs no marker" becomes "needs no marker
     outside the first three turns"), `Read`, `BaselineOf` and `ContextReading.Baseline`.
2. **`SheetReminder.For`** (`src/Dmc.Cli/Retrieval/SheetReminder.cs:79`)
   - It returns `Found.None` when `transcript_path` is set and `contextNow` is 0. The doc comments of `For`'s
     `contextNow` parameter, `Recent` and `Seen` say when 0 can still reach them.
3. **`ContextCheckCommand`**: no code change. Its class summary gets one clause about the first message
   (`src/Dmc.Cli/Commands/ContextCheckCommand.cs:6-20`).

## Tests

The tests are in `tests/Dmc.Tests/ContextCheck/`. One test or a data row covers each row of
[contracts/context-check-baseline.md](contracts/context-check-baseline.md):

- **`ContextCheckTests.cs`**: a new helper `SkillCall(context, skill, sidechain)`. Tests for C1's rows: turn 1,
  turn 2, two calls in one turn, a turn still running, turn 4 not counting, a sidechain call, a part after a
  boundary, the old name, and a resume record whose usage is zero. Then one break-line test: at 154,000 with the
  first row's log, `BreakReminder.For` prints nothing.
- **`SheetReminderContract.cs`**: C2's rows. A log of only a `/clear` echo, and a log whose boundary is last, both
  give no `Sheets of` header. The existing tests stay unchanged and cover the "as today" rows.
- **Unchanged**: every existing test in both files, with no change to what it expects (SC-003).

## Documents

| Document | Change |
|---|---|
| `.docs/decisions.md` | New entry "A typed resume sets the break baseline" (issue #244). It has the decision, the reason (`082db04e`), the rejected options from research.md, and the open point R7. The Date line of "The break alert is relative to where the resume left the context" gains an "amended" note with the PR's date that points to the new entry |
| `.docs/dmc-reference.md` § `context-check` | The baseline paragraph (`:821`) and the "needs no marker" paragraph (`:825`), with the compaction limit stated as unverified (R6). The sheet-lines paragraph says there are none before DMC's first reply. The timing figures change if step 4 of the quickstart changes them |
| `.docs/architecture.md:71` | The parenthesis on the baseline gains "or the end of a typed resume in the first three messages" |
| `.docs/roadmap.md` | A row for spec 019 and issue #244 |
| `.docs/user-guide.md`, `README.md` | None. `user-guide.md:184` already says "where the conversation settled after DMC loaded your campaign", which is now true for a typed resume as well |

## Project Structure

### Documentation (this feature)

```text
specs/019-typed-resume-baseline/
├── spec.md
├── plan.md                         # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── context-check-baseline.md
├── checklists/
│   └── requirements.md
└── tasks.md                        # /speckit-tasks, not made yet
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── ContextCheck/TranscriptReader.cs     # baseline pass, resume-call helper, doc comments
├── Retrieval/SheetReminder.cs           # no sheet lines before the first reply, doc comments
└── Commands/ContextCheckCommand.cs      # class summary only

tests/Dmc.Tests/ContextCheck/
├── ContextCheckTests.cs                 # C1 tests
└── SheetReminderContract.cs             # C2 tests
```

**Structure Decision**: the existing single CLI project and its test project. No new file in either.

## Open point for the maintainer

Research R7: a `/dmc-resume-campaign` slash command sent as the second or third message, after a typed one, is not
a `Skill` call, so this plan leaves it at today's baseline. Counting its echo would need one more clause in FR-001
and one test. It is not adopted unless the maintainer agrees.

## Complexity Tracking

None.
