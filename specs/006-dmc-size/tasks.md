---

description: "Task list for measuring what a resume reads, in tokens (dmc size)"
---

# Tasks: Measure What a Resume Reads, in Tokens

**Input**: Design documents from `specs/006-dmc-size/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dmc-size.md, quickstart.md

**Tests**: Included. FR-017 requires fixture tests on every CI platform, and the constitution (V) requires CI to pass
before merge. Within each story the tests come first and must fail before the implementation.

**Organization**: Tasks are grouped by user story. US1 (see what a resume reads) and US2 (the canon limit in tokens)
are P1; US3 (flags) and US4 (where it runs) are P2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US4, as in spec.md
- Every path is relative to the repository root.

## Conventions for every task that writes C#

These come from the repository, not from this feature; each task below assumes them.

- Namespace `Dmc.Size` for `src/Dmc.Cli/Size/`, `Dmc.Commands` for `src/Dmc.Cli/Commands/`, `Dmc.Tests.Size` for
  `tests/Dmc.Tests/Size/`.
- An XML doc comment on every type and member, private ones included (`CLAUDE.md` § Git and GitHub, Documentation).
  Warnings are errors, and the Native AOT and trim analyzers are on: base class library only, no reflection, no new
  package; regexes with `[GeneratedRegex]`.
- Every failure is `throw new DmcException(...)` (`src/Dmc.Cli/DmcException.cs`), which prints `dmc size: <message>`
  as one line with exit code 1, or 2 for an argument mistake (`Argparse.Usage`). The messages are the table in
  `contracts/dmc-size.md` § Failures; copy them character for character.
- Library code takes the DMC folder as a parameter (as `MapRedraw.Pending(root)` does); only `SizeCommand` calls
  `DmcFolder.Require()`. The library never writes, moves or deletes a file (FR-012), and reads files as bytes.
- Every number comes from `Budgets` (T002). No other file states a budget, a limit or a threshold.

## Phase 1: Setup

**Purpose**: A known-good baseline.

- [X] T001 Run `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` and `dotnet test` on the branch before any
  change, and note which tests skip locally (`TestEnv.Need`). A failure here is a pre-existing problem to report, not
  to fix in this feature.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The numbers, the measurement of a part of a file, the arguments, the campaign lookup, the empty command,
the flag record and the `--check` formatter, the fixtures and the error-path wiring that every story uses.

**CRITICAL**: No user story starts until this phase is complete.

- [X] T002 [P] Create `src/Dmc.Cli/Size/Budgets.cs`, an `internal static` class of `const int` values in estimated
  tokens unless said otherwise, each with a doc comment giving the reason from research R3–R6: `GameState = 4000`,
  `DmNotes = 10000`, `DmNotesAboutThePlayer = 2000` (section), `DmNotesArcPlan = 1500` (section), `Summary = 3000`,
  `PcSheet = 6000` (part read), `OtherSheet = 4000` (part read), `CanonLimit = 19000` (read whole below it),
  `CanonArchiveMark = 16000`, `CanonPricesSection = 3000`, `NearCapTokens = 20000`, `NearCapLines = 1600` (lines),
  `ReadCapTokens = 25000` (the Read tool's cap, for the message only), `LongLineCharacters = 400` (characters),
  `CheckLines = 12` (the most lines `--check` prints). This is the one home of every number (plan, Constitution
  Check III).
- [X] T003 [P] Create `src/Dmc.Cli/Size/Measurement.cs`: a record `Part(string Path, PartKind Kind, long Bytes, int
  Lines, IReadOnlyList<Section> Sections)` with `Tokens => Bytes / 4` (research R1: "rounded down"), an enum
  `PartKind { Whole, ToHistory, Tail, AlwaysReadSections, Section }`, a record `Section(string Heading, long Bytes)`
  with `Tokens`, and static helpers that work on the file's raw bytes split after each `\n` (a line keeps its line
  end, CR included, so "line ends included as on disk"): `ReadLines(path)` (bytes per line plus the line decoded as
  UTF-8 for matching), `Measure(path, kind, lines)` and `Sections(lines)`, which cuts at every line starting `## `
  and names text before the first heading `(top)` (data-model § File measurement). Also `FormatTokens(long tokens)`:
  thousands with one decimal (`6.6k`, `20.4k`, `19k` when the decimal is 0), whole numbers below 1,000 (`812`), as
  contracts/dmc-size.md § Full report says.
- [X] T004 [P] Create `tests/Dmc.Tests/Size/SizeFixtures.cs`, a static helper like
  `tests/Dmc.Tests/Campaign/CampaignFixtures.cs`, using only `System.IO`: (1) a temporary DMC folder that counts as
  one (reuse `tests/Dmc.Tests/Support/DmcFolders.cs` for `.dmc/manifest.json`), deleted afterwards; (2)
  `NewCampaign(root, name)` copying `kit/templates/campaign/` (resolve `kit/` the way `ToolkitTests` does) into
  `adventures/<name>/`, writing `adventures/player-options.md` from `kit/templates/player-options.md`, and setting
  `adventures/.active-campaign`; (3) builders for: a sheet with a title line `# Name`, sections and a last section
  `## History` (or `## Notes`, `## History with the PC`, or history in the middle); a `game-state.md` with Party
  status blocks (`### Name`, plus `### With the party, not companions`), a Present line, a Combat tracker table and a
  `Last full save` line (`2026-11-09 18:40, session 8, through scene 3`); a session log with N `### Scene K:`
  headings (and the template's commented `### Scene N:` example); a canon with `## Rulings`, `## Prices, debts and
  promises`, a section whose first comment says "Always read", and other sections; a module campaign
  (`module-progress.md` naming `modules/<m>/`, `Digest to read before continuing` naming `chapters/02-x.md` and a
  section, a Chapter status) with `modules/<m>/index.md` and the chapter file; (4) `Grow(path, bytes, lineLength)`,
  which appends lines of a given length until the file reaches a size; (5) `Hash(folder)`, a hash of every file's path
  and bytes, to prove nothing changed.
- [X] T005 [P] Create `tests/Dmc.Tests/Size/SizeLibraryTests.cs` with the Measurement and Budgets tests, written to
  fail first: bytes ÷ 4 rounded down (7 bytes → 1 token); a CRLF file counts its CRs; a UTF-8 accented line counts its
  bytes, not its characters; sections largest first with `(top)`; `FormatTokens` for 0, 812, 1000, 6640, 19000,
  20448; and that every `Budgets` value used by a flag is below `ReadCapTokens` (FR-007: "Every budget MUST sit well
  under the Read tool's cap").
- [X] T006 [P] Create `src/Dmc.Cli/Size/SizeArguments.cs`, parsing `dmc size [NAME] [--check] [--folder PATH]
  [--help]` with `src/Dmc.Cli/Argparse.cs` as `src/Dmc.Cli/Campaign/CampaignArguments.cs` does (a unique prefix
  works, `--folder=PATH` works, a mistake is one line, exit 2, never the usage text). Returns a record
  `SizeRequest(string? Name, bool Check, string? Folder)` or null for `--help`. A second positional argument and an
  unknown option use the existing `Argparse` messages.
- [X] T007 Create `src/Dmc.Cli/Size/SizeReport.cs` with the campaign lookup only, for now: `Target(string
  dmcFolder, string? name)` returns the campaign folder or throws, with the messages of contracts/dmc-size.md
  § Failures verbatim: `no active campaign: name one (dmc size NAME)` (no name, and `adventures/.active-campaign`
  missing or its first line empty); `<NAME> is not a campaign (a folder starting with a dot is a staging folder)`;
  `no campaign <NAME> in adventures/`. Add a `--folder` check in the same file: `<path> is not a DMC folder` when
  `DmcFolder.IsDmcFolder` is false (research R8).
- [X] T008 Create `src/Dmc.Cli/Commands/SizeCommand.cs` implementing `ICommand` as `CampaignCommand` does: `Name`
  `size`; `Summary` `what a resume reads, in estimated tokens, with what to trim: dmc size [NAME] [--check] [--folder
  PATH]`; a `Help` text (usage; the full report and `--check`; the estimate is bytes / 4; the command writes nothing;
  `.Replace("\r\n", "\n")`); `Run` parses with `SizeArguments`, resolves the folder (`--folder` else
  `DmcFolder.Require()`) and the campaign with `SizeReport.Target`, and for now prints nothing and returns 0. Register
  it in `src/Dmc.Cli/CommandTable.cs` after `new CampaignCommand(),`.
- [X] T009 Add the flag record and the `--check` formatter to `src/Dmc.Cli/Size/Flags.cs`: an enum `FlagKind`
  (`CanonOverLimit, NearCap, OverBudget, SectionOverBudget, LastPlayedLog, FinishedOpposition, DoneLines,
  PricesSection, LongLines`, in that order, the order lines are printed: "A canon over its limit always comes first"),
  a record `Flag(FlagKind Kind, string Path, string Line, bool HoldsTheSave)`, and `CheckLines(IReadOnlyList<Flag>)`
  returning at most `Budgets.CheckLines` lines, the last one `... and N more: run dmc size <campaign>` when there are
  more, and none for an empty list (FR-013a, research R7). The flag tests themselves come in US2 and US3.
- [X] T010 Wire the guards every command goes through: (1) create `tests/Dmc.Tests/Size/SizeCases.cs` with
  `ErrorCases()` returning `CommandCase`s for each failure of contracts/dmc-size.md § Failures and the argument
  mistakes (unknown option, two names, `--folder` with no value), each seeding its own DMC folder with
  `SizeFixtures` and passing `--folder <work>`, so no case can find the developer's real DMC folder through
  `dmc.json`; (2) in `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs` add `SizeErrors` and
  `EveryErrorOfSizePrintsOneLine`, following `EveryErrorOfCampaignPrintsOneLine` (one line, no stack trace, non-zero
  exit, and the folder hash from `SizeFixtures.Hash` unchanged); (3) in `tests/Dmc.Tests/TokenCost/OutputBytesTests.cs`
  add a run of `dmc size --folder <fixture>` and of `--check` on a fixture with a flag: no byte-order mark, no `\r`.
- [X] T011 [P] Add `"Bash(dmc size *)"` and `"PowerShell(dmc size *)"` to the allow list of `.claude/settings.json`,
  next to the `dmc digest` rules, so `review-transcript` (T040) runs it without a prompt. `kit/settings.json` already
  allows `Bash(dmc *)` and `PowerShell(dmc *)` and does not change.

**Checkpoint**: `dotnet build` is clean; `dmc size --help` works; the failure cases print one line.

---

## Phase 3: User Story 1 - See what a resume reads, and where the bulk is (Priority: P1) MVP

**Goal**: `dmc size [NAME]` prints the full report: every file resume step 2 reads, the part read, its tokens, the
total and the largest sections (FR-001 to FR-005, FR-012, FR-013).

**Independent Test**: on a fixture campaign of known sizes, each file the resume reads is listed with bytes ÷ 4 of
exactly the part read, none it does not read is listed, the total is their sum, and the sections are largest first
(SC-001).

### Tests for User Story 1

- [X] T012 [P] [US1] In `tests/Dmc.Tests/Size/SizeLibraryTests.cs`, read-set tests on `SizeFixtures` campaigns, one
  per rule of data-model § Resume read set and § Part read: both player-options files first; `game-state.md`,
  `summary.md`, `dm-notes.md` whole; every `pc-*.md`; a companion from a Party status block matched by its sheet title
  or its first word ("Kibil" → `# Kibil Rornson`), case-insensitive, whole word; the "With the party, not companions"
  block not read as a companion; an NPC named in the Present line or the Combat tracker read; a sheet both companion
  and present listed once; `minor-npcs.md`, `canon-archive.md`, `dm-notes-archive.md`, older logs and `maps/` never
  listed; a sheet read to its `## History` (and `## Notes`, `## History with the PC`), and whole when the history is in
  the middle; a Party status block with no sheet listed `not found`; a missing `dm-notes.md` listed `missing` and
  counted 0; the log tail from the third-last `### Scene [0-9]+:` (the commented `### Scene N:` not counted); fewer
  than three scenes whole; the whole log when `Last full save` lacks "through scene N", names an earlier session, or N
  is below the log's last scene; the latest log chosen by its number (`session-010.md` after `session-009.md`).
- [X] T013 [P] [US1] In the same file, module tests: `module-progress.md`, `modules/<m>/index.md` and the digest
  section (heading holding the name, to the next heading of the same or a higher level) are counted; a section not
  found counts the chapter whole and says so; a chapter file missing is `not ingested`; with every chapter complete in
  Chapter status only `module-progress.md` counts (research R10).
- [X] T014 [P] [US1] Create `tests/Dmc.Tests/Size/SizeCommandContract.cs`, run in-process (`CommandLine.Run`) and
  black-box (`DmcRunner.BlackBox`) like `tests/Dmc.Tests/Campaign/CampaignCommandContract.cs`: the full report's first
  line `<campaign>: what a resume reads, in estimated tokens (bytes / 4)`; one line per file in resume order with its
  tokens, budget and part; the `total` line; `largest sections:` with at most four per file and `(+N more)`;
  `nothing to flag` on a new campaign; the active campaign used when no `NAME` is given; exit 0; the fixture's
  `SizeFixtures.Hash` unchanged after the run (FR-012); and the long fixture's report at most 40 lines (SC-008).

### Implementation for User Story 1

- [X] T015 [US1] Create `src/Dmc.Cli/Size/ReadSet.cs` with resume step 2 items 1 to 6 (research R9, R10): the two
  player-options files, `game-state.md`, `story/summary.md`, `story/dm-notes.md`, the sheets (all `pc-*.md`; Party
  status `### ` blocks except `With the party, not companions`; names in the Scene block's `**Present**` line and the
  `## Combat tracker` section; matched to `character-sheets/*.md` other than `minor-npcs.md` by the title's whole text
  or first word, whole word, case-insensitive; once each), and the latest session log with the tail rule. Each entry
  is a path, a `PartKind`, a budget kind (`GameState`, `Summary`, `DmNotes`, `PcSheet`, `OtherSheet`, `Canon`, or none)
  and a state (`measured`, `missing`, `not found`, `not ingested`). Name each marker the code relies on in one
  `internal const string` (`"## History"`, `"## Notes"`, `"## History with the PC"`, `"With the party, not
  companions"`, `"through scene"`, the scene pattern `^### Scene [0-9]+:`, `"Rulings"`, `"Prices, debts and
  promises"`, `"Always read"`), so T018 can check them against the skill.
- [X] T016 [US1] Add item 9 to `src/Dmc.Cli/Size/ReadSet.cs`: when `module-progress.md` exists, its `**Module
  folder**`, `**Digest to read before continuing**` and `## Chapter status` lines give `modules/<m>/index.md` and the
  digest section, measured as T013 describes. Canon (item 7) is listed as whole for now; US2 adds its rule.
- [X] T017 [US1] In `src/Dmc.Cli/Size/SizeReport.cs` add `Measure(string dmcFolder, string campaign)` returning the
  read set's `Part`s with their budgets and states (data-model § Relationships; the in-process surface issue #172
  will call, FR-016), and `FullReport(...)` formatting it as contracts/dmc-size.md § Full report shows; then make
  `SizeCommand.Run` print the full report when `--check` is not given.
- [X] T018 [US1] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` add `ResumeStepTwoStillHoldsEveryMarkerDmcSizeReads`:
  read `kit/skills/dmc-resume-campaign/SKILL.md` § 2 and assert it contains each `ReadSet` marker constant from T015
  word for word (research R2), so a change to step 2 that drops one fails until `ReadSet` changes too.

**Checkpoint**: `dmc size` on a fixture lists exactly what a resume reads, with correct counts (SC-001, SC-002).

---

## Phase 4: User Story 2 - The canon limit measures size, not lines (Priority: P1)

**Goal**: canon is read whole under 19k estimated tokens and in its always-read sections above; the full save
archives down to 16k; no kit rule counts canon's lines (FR-009, FR-010).

**Independent Test**: two canons with the same line count and three times the size: one read whole, one in
sections, by the command and by the skills' wording (SC-005); the long campaign's canon read whole now and at 17k
(SC-004).

### Tests for User Story 2

- [X] T019 [P] [US2] In `tests/Dmc.Tests/Size/SizeLibraryTests.cs`: canon at 18,999 estimated tokens read whole and
  no flag; at 19,000 read as `## Rulings`, `## Prices, debts and promises` and the "Always read" section only, with a
  `CanonOverLimit` flag; two canons of 200 lines, about 8k and 24k, one whole and one in sections; canons of 15.5k and
  17k read whole (SC-004); the full report's canon line says `limit 19k   read whole` or `read in sections` and, in
  sections, `(the search for the scene's names is not counted)`.
- [X] T020 [P] [US2] In `tests/Dmc.Tests/Size/SizeCommandContract.cs`: `--check` on a canon over its limit prints, as
  its first line, `story/canon.md <n>k is over its 19k limit: read it in sections; a full save archives it down to
  16k`; on a canon under it, nothing.
- [X] T021 [P] [US2] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` add `NoKitRuleCountsCanonInLines`: no file under
  `kit/` contains `230` or `200 lines` beside `canon`, and neither `dmc-resume-campaign` nor `dmc-campaign-full-save`
  tells DMC to count canon's lines with Grep (`pattern \`^\`, count mode` near `canon`).

### Implementation for User Story 2

- [X] T022 [US2] In `src/Dmc.Cli/Size/ReadSet.cs` give canon (item 7) its rule: measure the whole file; under
  `Budgets.CanonLimit` the part is `Whole`; at or over it, `AlwaysReadSections`: the `## Rulings` and `## Prices,
  debts and promises` sections and any `## ` section whose first comment (`<!-- ... -->` directly under the heading)
  contains "Always read".
- [X] T023 [US2] In `src/Dmc.Cli/Size/Flags.cs` add the `CanonOverLimit` flag (whole-file tokens ≥
  `Budgets.CanonLimit`, `HoldsTheSave` true) with the line of T020, and make `SizeReport` print the canon line and the
  search note of T019 in the full report and the flag lines through `CheckLines` when `--check` is given.
- [X] T024 [US2] Rewrite `kit/skills/dmc-resume-campaign/SKILL.md` § 2 item 7: read canon whole unless `dmc size
  <campaign> --check` (run once before the reads, T033) printed that canon is over its limit; then read it in sections
  and search as the item already says. Remove the Grep line count and "Under about 230 lines". Keep the Rulings,
  Prices, "Always read", search and "Do not archive anything now" text, and every marker T018 checks.
- [X] T025 [US2] Rewrite `kit/skills/dmc-campaign-full-save/SKILL.md` step 4's archive paragraph and step 7's canon
  bullet: archive when `dmc size <campaign> --check` says canon is over its limit, until it no longer does and canon is
  under the mark the line names; keep "move lines in bulk", "by whether the subject is still live", the always-read
  sections never moving, and "a long canon is legitimate". Remove the Grep count and the 230 and 200 numbers; in step
  4's split paragraph change "so that the line count means something" to say one fact per line keeps canon findable.

**Checkpoint**: canon is decided by tokens everywhere; T021 passes.

---

## Phase 5: User Story 3 - Trouble is flagged before it shows up in play (Priority: P2)

**Goal**: the flags of FR-006 to FR-008 in the full report and `--check`, one line per kind and file (research R3,
R6); `--check` silent when all is well (FR-013a).

**Independent Test**: each flag appears on the fixture built to trigger it and on no fixture built not to (SC-009);
`--check` on a new campaign prints nothing (SC-007).

### Tests for User Story 3

- [X] T026 [P] [US3] In `tests/Dmc.Tests/Size/SizeLibraryTests.cs`, a positive and a negative fixture per flag, with
  the boundary: NearCap at 19,999 and 20,000 tokens and at 1,599 and 1,600 lines, on a budgeted and on an unbudgeted
  file (a session log); OverBudget for `game-state.md` at 4,000 (no flag) and 4,001, a PC sheet at 6,001, a companion
  sheet at 4,001; no budget flag on either player-options file, the log tail or a module file; SectionOverBudget for
  About this player over 2,000 and Arc plan over 1,500; LastPlayedLog for two `Last played` lines and for a
  `## Mix tracker` section, not for one `Last played` line; FinishedOpposition for rows `| Goblins | 0 / 4 |` and
  `0/4`, not for `1 / 4`, and counted (`3 rows`); DoneLines for `DONE` and `RESOLVED` outside the Arc plan, not inside
  it and not for lowercase `done`; PricesSection over 3,000; LongLines counted per file at 401 characters, not at 400.
- [X] T027 [P] [US3] In `tests/Dmc.Tests/Size/SizeCommandContract.cs`: `--check` prints nothing and exits 0 on a new
  campaign; one line per flag in `FlagKind` order with contracts/dmc-size.md's wording; 14 flags print 11 lines and
  `... and 3 more: run dmc size <campaign>`; the full report's `flags:` block holds the same lines; exit 0 with flags.

### Implementation for User Story 3

- [X] T028 [US3] In `src/Dmc.Cli/Size/Flags.cs` add `NearCap` (part read ≥ `NearCapTokens` or ≥ `NearCapLines`
  lines: `<path> <n>k is near the Read cap (25k): it may be cut short at resume`), `OverBudget` (`<path> <n>k is over
  its <b>k budget`) and `SectionOverBudget` (`<path> <section>: <n>k, over its <b>k budget`), with `HoldsTheSave` true
  for budgeted files and false for NearCap on an unbudgeted file (data-model § Flag).
- [X] T029 [US3] In `src/Dmc.Cli/Size/Flags.cs` add the bloat flags of research R6 on `story/dm-notes.md` and
  `story/canon.md`: `LastPlayedLog` (`<path> Mix tracker: a log of sessions, where one Last played line belongs`, or
  `<path>: <n> Last played lines, where one belongs`), `FinishedOpposition` (`<path> Opposition: <n> rows with none
  left alive`, a row whose second cell starts `0 /` or `0/`), `DoneLines` (`<path>: <n> lines marked DONE or
  RESOLVED`, whole word, upper case, outside `## Arc plan`), `PricesSection` (`<path> Prices, debts and promises:
  <n>k, which suggests paid debts or kept promises not deleted`) and `LongLines` (`<path>: <n> lines over 400
  characters`, on every file read whole).
- [X] T030 [US3] In `src/Dmc.Cli/Size/SizeReport.cs` collect every flag for the read set in `FlagKind` order, print
  them under `flags:` (or `nothing to flag`) in the full report, and through `CheckLines` for `--check`.

**Checkpoint**: `dmc size --check` is silent on a healthy campaign and names each problem on a grown one.

---

## Phase 6: User Story 4 - The measurement runs where it helps (Priority: P2)

**Goal**: the resume runs the check once and warns only near the cap; the full save is not complete until the check
prints nothing or each line is explained; `review-transcript` records the report; every size rule in the kit is the
check's (FR-011, FR-015, FR-015a).

**Independent Test**: on a fixture DMC folder refreshed from the build, a resume of a healthy campaign says nothing
about size, a resume of a campaign near the cap says one line, and a full save names what it could not trim (quickstart
§ 4); the toolkit tests find no size rule in lines or words (SC-006).

### Tests for User Story 4

- [X] T031 [P] [US4] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` add `NoSizeRuleInTheKitIsInLinesOrWords`: no file
  under `kit/` matches `about \d+ (lines|words)` or `under about \d+` (the forms found in `dmc-campaign-full-save`, the
  three templates and `dmc-resume-campaign`; plan § Source Code).
- [X] T032 [P] [US4] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` add `TheResumeAndTheFullSaveRunDmcSizeCheck`:
  `dmc-resume-campaign` § 2 and `dmc-campaign-full-save` § 7 each contain `dmc size <campaign> --check`, and
  `.claude/skills/review-transcript/SKILL.md` contains `dmc size --folder`. The existing
  `EveryDmcCommandInThePromptsIsARealSubcommandAndTheOnesDmcRunsNeedNoPermissionPrompt` then checks both forms
  against the allow lists (T011).

### Implementation for User Story 4

- [X] T033 [US4] In `kit/skills/dmc-resume-campaign/SKILL.md` § 2, before item 1: run `dmc size <campaign> --check`
  once. Its output says what to do: canon over its limit decides item 7 (T024); a file near the Read cap is read in
  parts if it comes back cut, and the player is told in one line out of character that the campaign's files are large
  and the next full save will trim them; other lines are for the full save and are not mentioned. Nothing printed
  means canon is read whole and nothing is said about size (FR-015, SC-007).
- [X] T034 [US4] In `kit/skills/dmc-campaign-full-save/SKILL.md` remove the size targets in lines and words: step 1
  ("aim for under about 100 lines"), step 2 ("aim for a sheet under about 150 lines"), step 5 ("keep the section under
  about 40 lines", "aim for the whole file under about 150 lines") and step 6 ("under about 600 words"). Replace each
  with "within the budget `dmc size` checks in step 7", keeping every content rule (one fact per line, history cut,
  split long lines, recent events in detail, older arcs compressed, nothing open dropped).
- [X] T035 [US4] In `kit/skills/dmc-campaign-full-save/SKILL.md` step 7 add the size check as its last bullet: run
  `dmc size <campaign> --check`; for each line, trim dead text from the file it names (steps 1 to 6 say how) and run
  it again; the save is complete when it prints nothing, or when the step 8 report names each line still printed and
  why it stays (every subject left in the file is live, or the flagged text is still needed, such as an enemy that fled
  and may return) (FR-015a, SC-010). In step 8 add that sentence's report item.
- [X] T036 [P] [US4] In `kit/templates/campaign/game-state.md` (line 3), `kit/templates/campaign/story/dm-notes.md`
  (the Arc plan's "Keep the section to about 25 lines" and About this player's "about 40 lines at most") and
  `kit/templates/campaign/story/summary.md` (line 3, "kept under about 600 words"), remove the line and word targets
  and say instead that the full save checks the size with `dmc size`; keep the content rules.
- [X] T037 [US4] Check `kit/skills/dmc-resume-campaign/SKILL.md` § 2 as a whole after T024 and T033: the check is run
  once, item 7 refers to it, no Grep line count remains, and the section is no longer than before (Principle II).
- [X] T038 [P] [US4] In `kit/skills/dmc-update-game-state/SKILL.md` and `kit/skills/dmc-character-sheet-update/SKILL.md`
  search for any size target in lines or words and replace it as T034 does (none were found in planning; confirm and
  leave the files unchanged if so).
- [X] T039 [P] [US4] Search `kit/dm-rules.md` for `230`, `lines` and `words` used as a size rule; if any, point it at
  the full save's check instead. Planning found none, so the file should not change (plan, Constitution Check II).
- [X] T040 [US4] In `.claude/skills/review-transcript/SKILL.md` § 4 "Context and tokens", add: run `dmc size --folder
  <dmc folder> <campaign>` and put its total and flags, with the date measured, in the context entry, since the
  command measures the files as they are now, not as they were when each session was played (spec Edge Cases).

**Checkpoint**: the toolkit tests pass; a refreshed scratch DMC folder behaves as quickstart § 4 says.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: The documents, the measurement on the long campaign, and the full test run.

- [X] T041 [P] Update `.docs/campaign-files.md`: in "What each file holds", the canon row's read rule (whole under its
  token limit; past it, Rulings, Prices, debts and promises and a search) and the `canon-archive.md` row (archived
  when `dmc size` says canon is over its limit); add a short "How big the files may be" note: each budget and the
  canon limit from `Budgets`, checked by `dmc size` at every full save, the files without a budget, and that
  `src/Dmc.Cli/Size/Budgets.cs` is where they are set.
- [X] T042 [P] Update `.docs/architecture.md`: "Starting a session" (the resume runs `dmc size --check` once; canon
  whole under its token limit), the full save table's Canon row and a line for step 7's check, and "Inside `dmc`"
  listing `size`.
- [X] T043 [P] Update `.docs/user-guide.md` § Your files: how to see what a resume reads (`dmc size`, run in a
  terminal or by asking DMC "are your files too big?"), what the flags mean and that the full save fixes them, and
  that the command changes nothing.
- [X] T044 [P] Add to `.docs/decisions.md` the entry "Size rules are measured in tokens by `dmc size`" (Decision,
  Reason, Alternatives): bytes ÷ 4 (R1), the budgets and why the PC sheet is 6k and the summary 3k (R4), the canon limit
  of 19k and mark of 16k for consistency (R5), the two forms and exit 0 (R7), the numbers' one home in `Budgets`; and a
  line in "Resume and full save are bounded" and in the 230-line canon entry pointing to it.
- [X] T045 [P] Update `.docs/roadmap.md` with a row for issue #174 (status, what shipped) and `.docs/development.md`
  § Tests, Layers, naming `Size/` library and contract tests and the new toolkit tests.
- [X] T046 [P] Add a short paragraph on `dmc size` to `README.md` beside the `dmc campaign` one (what it shows, the
  `--check` form, that it changes nothing).
- [X] T047 Publish, run `dotnet test`, and fix any failure; confirm nothing new is skipped locally beyond T001's list.
- [X] T048 Run quickstart § 2 by hand on a scratch DMC folder and § 3 on the long campaign (`dmc size --folder
  D:\Claude\dmc rorns-landing`, read-only), and record the report's total, the dm-notes and canon lines and the
  flags in the PR description (SC-003, SC-004, SC-008).
- [X] T050 Before opening the PR, check whether issue #172's migration has merged (`git log origin/main --
  kit/skills/dmc-migrate-campaign/SKILL.md`). If it has, add the `dmc size <campaign> --check` step to that skill as
  its decision C1 requires, move its size check from line counts against the old targets to `dmc size`, rerun T031
  (which would otherwise fail on any line target it states), and say so in the PR. If it has not, say in the PR that
  the migration's PR must add the step (FR-016a).
- [X] T049 Write the PR description per `CLAUDE.md` § Git and GitHub: what changed, why (the PR #173 review), the
  sources (spec, research, issue #174), how it was tested (T047, T048), the documents changed, `Part of #174` or
  `Closes #174`, and a migration line: none needed, because the change alters when canon is archived and what the save
  checks, not what a campaign file holds (spec Assumptions); then `gh pr view <n> --json body` to check it is there.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none.
- **Foundational (Phase 2)**: after Setup; blocks every story.
- **US1 (Phase 3)**: after Foundational.
- **US2 (Phase 4)**: after US1's `ReadSet` (T015) and `SizeReport.Measure` (T017); its prompt tasks (T024, T025) need
  only T018.
- **US3 (Phase 5)**: after US1 (flags need the measured parts); independent of US2 except `FlagKind` order (T009).
- **US4 (Phase 6)**: the prompt tasks need `--check` to exist (T023 or T030); T033 builds on T024.
- **Polish (Phase 7)**: after the stories it documents.

### Within Each Story

- Tests first, failing; then the code; then the prompts.
- `ReadSet` before `SizeReport`; `Flags` before the `--check` output.
- Kit prompt edits after the toolkit tests that check them (T018, T021, T031, T032).

### Parallel Opportunities

- Phase 2: T002, T003, T004, T005, T006 and T011 touch different files.
- US1: T012, T013 and T014 (tests) together.
- US2: T019, T020 and T021 together.
- US3: T026 and T027 together.
- US4: T031 and T032 together; T036, T038 and T039 together.
- Polish: T041 to T046 together.

---

## Parallel Example: User Story 1

```text
Task: "Read-set tests in tests/Dmc.Tests/Size/SizeLibraryTests.cs" (T012)
Task: "Module read-set tests in tests/Dmc.Tests/Size/SizeLibraryTests.cs" (T013, same file: run after T012 if one person)
Task: "Full report contract in tests/Dmc.Tests/Size/SizeCommandContract.cs" (T014)
```

## Parallel Example: Polish

```text
Task: ".docs/campaign-files.md" (T041)
Task: ".docs/architecture.md" (T042)
Task: ".docs/user-guide.md" (T043)
Task: ".docs/decisions.md" (T044)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 and Phase 2.
2. Phase 3: `dmc size` prints the full report. This alone answers the player's "are your files too big?".
3. Stop and validate on a fixture and on the long campaign (quickstart § 3, read-only).

### Incremental Delivery

1. US1: the measurement.
2. US2: canon by tokens; the kit's canon rules change.
3. US3: the flags and the silent `--check`.
4. US4: the resume and the full save run it; the other size rules go; `review-transcript` records it.
5. Polish: documents, the long-campaign numbers, the PR.

One PR for the whole feature (one issue, #174), since US2 and US4 change the kit's rules together and a half-moved set
of size rules would leave two homes for them.

---

## Notes

- [P] tasks touch different files and depend on no unfinished task.
- The command never writes; every contract test checks the fixture hash.
- A kit change reaches players only with a new `dmc` and a refresh of their DMC folder: say so in the PR
  (`.docs/development.md` § Adding to `dmc`).
- Commit after each task or logical group; the user merges.
