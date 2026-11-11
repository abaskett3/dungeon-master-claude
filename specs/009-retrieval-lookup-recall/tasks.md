---

description: "Task list for dmc lookup and dmc recall (issue #179, Phase A)"
---

# Tasks: Look Up a Section and Recall a Fact, Without Reading Whole Files

**Input**: Design documents from `specs/009-retrieval-lookup-recall/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dmc-lookup.md, contracts/dmc-recall.md,
contracts/recall-test.md, quickstart.md

**Tests**: Included. The constitution (V) requires CI to pass before merge, and User Stories 2 and 3 name fixture tests
as their independent tests. Within each story the tests come first and must fail before the implementation.

**Organization**: Tasks are grouped by user story. US2 (lookup) and US3 (recall) are built first, since they are the
code; US4 (the kit's rules) depends on both; US1 (the play folder) is the maintainer's, after the merge, and its tasks
are only the record of it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US4, as in spec.md
- Every path is relative to the repository root.

## Conventions for every task that writes C#

These come from the repository, not from this feature; each task below assumes them.

- Namespace `Dmc.Retrieval` for `src/Dmc.Cli/Retrieval/`, `Dmc.Commands` for `src/Dmc.Cli/Commands/`,
  `Dmc.Tests.Retrieval` for `tests/Dmc.Tests/Retrieval/`.
- An XML doc comment on every type and member, private ones included (`CLAUDE.md` § Git and GitHub, Documentation).
  Warnings are errors, and the Native AOT and trim analyzers are on: base class library only, no reflection, no new
  package; regexes with `[GeneratedRegex]`.
- Every failure is `throw new DmcException(...)`, which prints `dmc <command>: <message>` as one line with exit code 1,
  or 2 for an argument mistake (`Argparse.Usage`). The messages are the tables in `contracts/dmc-lookup.md` and
  `contracts/dmc-recall.md` § Failures; copy them character for character.
- Library code takes the DMC folder as a parameter; only the two command classes call `DmcFolder.Require()` or check
  `--folder`, as `SizeCommand` does. The library never writes, moves or deletes a file (FR-002, FR-010).
- Every number (weights, the 25-character unit floor, the 700-character trim, the 6,000-byte cap, list lengths) is a
  named constant in the one class that uses it, with a doc comment citing its research entry. The near-cap mark is
  `Budgets.NearCapTokens` and `Budgets.NearCapLines`, never restated.
- Output is printed with `"\n"` line ends, as `io.Out.WriteLine` already does in this program.

## Phase 1: Setup

**Purpose**: A known-good baseline.

- [X] T001 (Baseline: the same `main` passed 1544 with 5 skipped earlier on 2026-11-10; the first run here collided with the first new source file.) Run `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` and `dotnet test` on the branch before any
  change, and note which tests skip locally. A failure here is a pre-existing problem to report, not to fix here.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: What both commands use: the arguments, words and stems, headings, fixtures, the empty commands and the
error-path wiring.

**CRITICAL**: No user story starts until this phase is complete.

- [X] T002 [P] Create `src/Dmc.Cli/Retrieval/Words.cs`: `internal static class Words` with `Stems(string text)`
  returning the stems of a text by research R2 exactly: lower-case; fold `’` to `'`; a word is a run of letters
  (any script), digits and `'`; drop words of one character and words in the common-word list (copy the prototype's
  list verbatim from R2: the English function words plus `say said tell told get got one thing things`); stem by
  dropping a trailing `'s`, then the first of `ation`, `ion`, `ing`, `ed`, `es`, `s` that leaves at least four
  letters. Also `IsCommon(string word)`.
- [X] T003 [P] Create `src/Dmc.Cli/Retrieval/MarkdownHeadings.cs`: `internal static class MarkdownHeadings` that
  scans a file's lines once and returns its headings (level 1–6, text, 1-based line) and, for each line, whether it is
  inside a fenced code block, by research R7: an ATX heading is up to three spaces, 1–6 `#`, then a space or the end of
  the line; fences open and close with ```` ``` ```` or `~~~` (the same character, at least as many). Add
  `SectionEnd(headings, index, lineCount)` (the line before the next heading of the same or a higher level, else the
  last line) and `Key(string headingText)`, the normalised form of R7: lower case; `*`, `_`, `` ` `` and the brackets
  of `[text](url)` links removed (the link text kept); a trailing parenthesis whose text starts with `PDF`, `p.`,
  `pp.` or `page` (any case) removed; other punctuation turned into spaces; spaces collapsed and trimmed.
- [X] T004 [P] Create `src/Dmc.Cli/Retrieval/RetrievalArguments.cs` with `internal sealed record LookupRequest(string
  Source, string Heading, string? Folder)` and `internal sealed record RecallRequest(IReadOnlyList<string> Words,
  string? Folder)` (the campaign is decided later, R10) and `ParseLookup(string[] args)` / `ParseRecall(string[]
  args)`, reading options through `Argparse.ClassifyAll` with `--help` and `--folder` as `SizeArguments` does.
  Positionals: for lookup the first is the source and the rest, joined by single spaces, the heading (a usage mistake,
  exit 2, if either is missing); for recall all are words (a usage mistake if there are none). Null means `--help`.
- [X] T005 [P] Create `tests/Dmc.Tests/Retrieval/RetrievalFixtures.cs`, a builder of DMC folders in a temporary folder
  (with `.dmc/manifest.json`, as the size fixtures make one): campaign `new-road` whose `player-options.md` has
  `- **Predecessor**: old-road`; campaign `old-road` with a `module-progress.md` naming `` `modules/fen-crossing/` ``;
  module `fen-crossing` with `index.md`, `chapters/01-the-ford.md`, and a `monsters.md` of `## NAME (PDF n)` stat
  blocks; a markdown rules folder and a PDF-style rules folder (`index.md` with section rows and letter lines, and
  `extracted/text/page-NNN.txt`). Methods to plant a line in a given file and to add noise under `import/`, `exports/`,
  `backups/` and a `.staging` folder. Invented names only (no real campaign text).
- [X] T006 Create `src/Dmc.Cli/Commands/LookupCommand.cs` and `src/Dmc.Cli/Commands/RecallCommand.cs` implementing
  `ICommand` (`Name` `lookup` / `recall`; `Summary` and `--help` text from the contracts' usage lines), each parsing
  its arguments (T004), finding the folder as `SizeCommand` does, and calling a library method that does not exist yet
  (stubbed to throw `DmcException("not implemented")` until US2 / US3). Register both in `src/Dmc.Cli/CommandTable.cs`
  and add them to the command lists in the "no command" and "unknown command" messages wherever those lists live.
- [X] T007 [P] Unit tests for T002 and T003 in `tests/Dmc.Tests/Retrieval/WordsAndHeadingsTests.cs`: stems
  (`rulings`→`ruling`, `teaching`→`teach`, `Oswin's`→`oswin`, `ruled`→`ruled` and `nation`→`nation` because the
  stem would be under four letters, `horses`→`hors` but `horse`→`horse` as the prototype's known gap, common words
  gone, one-letter words gone, accented letters kept); headings (fenced `#` lines are not headings, four spaces is not a
  heading, `#hashtag` is not, levels 1–6); section ends; keys (`## GOBLIN BOSS (PDF 212)` and `goblin boss` give the
  same key; `**Prices**, debts & promises` → `prices debts promises`).
- [X] T007a [P] Create `src/Dmc.Cli/Retrieval/Trim.cs`: the window of research R4 (lines of up to 700 characters
  whole; otherwise the 700-character window holding the most distinct matched stems, starting at a matched word,
  widened to word boundaries, `…` on each cut side), with its unit tests in
  `tests/Dmc.Tests/Retrieval/WordsAndHeadingsTests.cs`.

**Checkpoint**: `dotnet build` passes; `dmc lookup --help` and `dmc recall --help` print their usage.

---

## Phase 3: User Story 2 - DMC reads one section, not a whole file (Priority: P1)

**Goal**: `dmc lookup SOURCE HEADING...` per `contracts/dmc-lookup.md`.

**Independent Test**: on the fixtures, each lookup prints exactly the section's bytes (LF line ends), with its file and
line range; each failure is one line.

### Tests for User Story 2

- [X] T008 [P] [US2] Create `tests/Dmc.Tests/Retrieval/LookupLibraryTests.cs` covering research R7–R9 on the fixtures:
  exact key match (`goblin boss` finds `## GOBLIN BOSS (PDF 212)` and returns down to, not including, the next `##`);
  a sub-section is included; all-words fallback (`prices` finds `## Prices, debts and promises`); several matches list
  `<file>:<line>: <heading>` (at most 8, then `(+N more)`) and print no section; no match gives the five nearest
  headings; the `/path` form narrows to one file; a section at the near-cap mark prints the one too-large line; a
  campaign source skips `import/`, `exports/`, `backups/` and dot folders; a module source skips `source/`; markdown
  rules are searched like any source; PDF rules give the matching `rules/index.md` lines (every word held, at most 8),
  and when none match, the page-numbers line from `extracted/text/` (phrase match, at most 10 pages) and never page
  text; CRLF files come out LF and otherwise unchanged.
- [X] T009 [P] [US2] Create `tests/Dmc.Tests/Retrieval/LookupCommandContract.cs` (abstract cases run in-process and
  against the published binary, as `SizeCommandContract` does): exit codes and outputs of `contracts/dmc-lookup.md`
  § Output and every row of § Failures, character for character; `--folder` and the default folder; usage mistakes
  exit 2.

### Implementation for User Story 2

- [X] T010 [US2] Create `src/Dmc.Cli/Retrieval/LookupSource.cs` (research R8): resolve `SOURCE` (split off `/<path>`)
  to a campaign in `adventures/`, a module in `modules/` or `rules`; refuse a dot-prefixed name, a name that is both a
  campaign and a module, and an unknown name, with the contract's messages (the unknown-name message lists at most 5
  campaigns and 5 modules); list the files it searches per `data-model.md` § Source; decide markdown or PDF rules
  (`*.md` other than `index.md`, outside `extracted/`).
- [X] T011 [US2] Create `src/Dmc.Cli/Retrieval/Lookup.cs`: for markdown sources, gather headings across the files
  (T003), match by key then by all words (T002), and return one of: the section (header `<file>:<first>-<last>` with
  the file relative to the DMC folder, then the lines), the list of matches, the too-large line (sizes by bytes ÷ 4
  rounded up and lines, against `Budgets.NearCapTokens` and `Budgets.NearCapLines`), or the no-heading failure with the
  five headings sharing the most stems with the request (ties by file and line order). For PDF rules, the index-lines
  and page-numbers paths of research R9, trimming an index line over 700 characters using `Trim` (T007a).
- [X] T012 [US2] Wire `LookupCommand.Run` (T006) to `Lookup`, printing each output line with `io.Out.WriteLine`;
  every path in output uses `/`.
- [X] T013 [US2] Add the lookup failures to `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs` (one row per contract
  failure) and lookup's outputs to `tests/Dmc.Tests/TokenCost/OutputBytesTests.cs`, in the shape of the existing rows.

**Checkpoint**: T008, T009 and T013 pass; US2 works on its own.

---

## Phase 4: User Story 3 - DMC recalls a fact from everything the campaign knows (Priority: P1)

**Goal**: `dmc recall [NAME] WORDS...` per `contracts/dmc-recall.md`, and the harness of `contracts/recall-test.md`.

**Independent Test**: on the fixture chain, each planted fact is in the output when recalled by its words, with the
right labels; nothing from the excluded folders appears; the output is within 6,000 bytes.

### Tests for User Story 3

- [X] T014 [P] [US3] Create `tests/Dmc.Tests/Retrieval/RecallLibraryTests.cs` covering research R1–R6 and R11 on the
  fixtures: scope (the campaign, `old-road` through the Predecessor line with backticks, `adventures/` and a trailing
  `/` stripped, `fen-crossing` through `old-road`'s module line; a loop followed once; `none` and a missing folder,
  the latter as a notice; a module that both the campaign and its predecessor name is searched once, at the campaign's
  rank); exclusions (`import/`, `exports/`, `backups/`, dot folders, `source/`, map JSON); units
  (under 25 characters, headings, `<!--` lines, `|---` lines and fenced lines are not units); tiers and weights (a
  canon line and a session-log line holding the same words: canon first; a session-log line holding all the rare
  words above a canon line holding one; a predecessor line below the same line in the campaign; a module line below
  every campaign line of equal relevance); labels (`sN.M` from the scene heading for a session log, `N` from
  `session-NNN.md`; the first citation for other files; `DM` for `dm-notes.md`, `dm-notes-archive.md`,
  `module-progress.md`, module lines and an NPC sheet's lines under a heading containing "Secret"); the trim (a
  3,000-character line shows a 700-character window holding the matched words, `…` on the cut sides, cut at word
  boundaries); the cap (output with notices and the last line at most 6,000 bytes; `(N more lines matched)` counts the
  rest); `nothing matched: <words>`; deterministic order on ties.
- [X] T015 [P] [US3] Create `tests/Dmc.Tests/Retrieval/RecallCommandContract.cs` (in-process and binary): the campaign
  argument rule of research R10 (`dmc recall new-road ferry cart` uses `new-road`; `dmc recall ferry cart` uses the
  active campaign; `dmc recall new-road` alone searches the word "new-road" in the active campaign; a misspelled
  campaign name, `dmc recall new-raod ferry`, is searched as a word in the active campaign), every row of
  `contracts/dmc-recall.md` § Failures character for character, exit 0 for hits and for `nothing matched`, usage
  mistakes exit 2.

### Implementation for User Story 3

- [X] T016 [US3] Create `src/Dmc.Cli/Retrieval/RecallScope.cs` (research R1, R6, R11): build the ordered places of
  `data-model.md` § Recall scope from the DMC folder and the campaign; parse the Predecessor and Module folder lines
  with `[GeneratedRegex]`; list each place's files with their tier; the weights as named constants (canon 1.30,
  archives and `-history.md` 1.20, sheets and `minor-npcs.md` 1.15, other campaign markdown 1.05, session logs 1.00,
  module 0.80, × 0.95 per step of distance); collect notices (at most three, then `(+N more skipped)`) for a missing
  predecessor, a missing module folder and an unreadable file.
- [X] T017 [US3] Create `src/Dmc.Cli/Retrieval/LineUnits.cs` (research R3): read each file once, mark fenced lines and
  headings with `MarkdownHeadings` (T003), and produce `data-model.md` § Line unit records: text trimmed, stems (T002),
  tier and weight, scene for session logs (the nearest `### Scene M:` above, N from the file name, as `sN.M` with no
  leading zeros), the first citation matching `\bs\d+(\.\d+)?\b` for other files, and the DM-only flag.
- [X] T018 [US3] Create `src/Dmc.Cli/Retrieval/Recall.cs` (research R1, R4, R5): an inverted index over the units'
  stems; BM25 with k1 = 1.4 and b = 0.6 (idf = ln(1 + (N − df + 0.5) / (df + 0.5)), length normalised by the average
  unit length) times the unit's weight; order by score rounded to two decimals, then tier, distance, file, line; the
  trim of R4 using `Trim` (T007a); the hit line
  `<place> <file>:<line> [<sN.M>] [DM] | <text>` (place is the campaign folder name or `modules/<name>`); fill to
  6,000 UTF-8 bytes counting notices and the last line; the `(N more lines matched)` line; `nothing matched:
  <words>`; the "every word is common" failure.
- [X] T019 [US3] Wire `RecallCommand.Run` (T006): decide the campaign by research R10 (the first word is the campaign
  when it names a folder in `adventures/` that does not start with a dot and at least one more word follows; otherwise
  the active campaign via `SizeReport.ActiveName`, failing with the contract's message when there is none), then call
  `Recall` and print.
- [X] T020 [US3] Add the recall failures to `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs` and recall's outputs to
  `tests/Dmc.Tests/TokenCost/OutputBytesTests.cs`.
- [X] T021 [P] [US3] Create `tests/Dmc.Tests/Retrieval/RecallTimingTests.cs` (SC-004): build a synthetic chain of about
  3 MB of markdown (generated lines of varied words, two campaigns and a module) and assert that one recall against the
  published AOT build completes in under one second, following the existing timing tests' use of `TestEnv` and the AOT
  binary.
- [X] T022 [P] [US3] Create `tools/recall-test.ps1` per `contracts/recall-test.md`: a header comment (what it is, that it
  never runs in CI, where the questions file lives and why it is not committed); parameters `-Questions` (required),
  `-Campaign`, `-Dmc` (default `artifacts/bin/dmc` if present, else `dmc`), `-Folder`; read the tab-separated file
  (skip blanks and `#` lines; a malformed line fails with one line); run `dmc recall` per question; judge the answer
  regex case-insensitively against the whole output and against the first hit line; print one line per question and the
  two summary lines; exit 1 on a `dmc` failure other than `nothing matched`. Write nothing.

**Checkpoint**: T014, T015, T020 and T021 pass; US3 works on its own; the harness runs against the fixture folder with a
small questions file written in a temporary folder.

---

## Phase 5: User Story 4 - DMC uses the two commands, and still plays without them (Priority: P2)

**Goal**: the play rules of FR-020 to FR-022 and research R14; the resume and full save unchanged (FR-020a).

**Independent Test**: toolkit tests pass; a live run in a scratch folder recalls a returning subject and falls back with
`dmc` off the PATH.

### Tests for User Story 4

- [X] T023 [P] [US4] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: assert `kit/dm-rules.md` has at most 214 lines
  (212 before this change, FR-022), with a message naming FR-022; assert that the play rules name `dmc recall` and
  `dmc lookup` and keep a fallback sentence (search the files as before when `dmc` fails or a recall does not hold
  it); assert that `kit/skills/dmc-resume-campaign/SKILL.md` and `kit/skills/dmc-campaign-full-save/SKILL.md` name
  neither command (FR-020a). Check that the existing test of `dmc` commands named in `kit/` accepts the two new ones.

### Implementation for User Story 4

- [X] T024 [US4] Edit `kit/dm-rules.md` in place, adding no line if possible and at most two (research R14):
  § Accuracy and consistency item 1 (recall in place of search; the Predecessor chain is covered); item 3 (look a rules
  entry up with `dmc lookup rules <entry>` before reading the index); "Look before you bring something back" (recall
  the subject with `dmc recall <campaign> <words>` before narrating, read the sheet or look up its Playing them
  section; when `dmc` fails or the recall does not hold it, search the files as before; a fact not found is not a fact
  denied); § Running a published module, "Run from the files" (look up the area, NPC or stat block with `dmc lookup
  <module> <heading>`; the cited `source/` pages stay the authority for read-aloud text). Keep the sentences that name
  the archives, so the fallback still knows where to search.
- [X] T025 [P] [US4] Edit `kit/skills/dmc-setup-rules/SKILL.md` § Using it in play: `dmc lookup rules <entry>` first
  (for a PDF it gives the index rows or the pages to read), then the index and pages as today.
- [X] T026 [P] [US4] (The template's comment is kept: migration 004 writes the same line into older campaigns; only the skill changed.) Edit `kit/skills/dmc-new-campaign/SKILL.md` (the Predecessor bullet) and
  `kit/templates/campaign/player-options.md` (the Predecessor comment): the earlier campaign is "recalled with `dmc
  recall`", not "searched". Check `kit/migrations/` for a test that pins the template's comment text before changing
  it; if one does, leave the template and change only the skill.
- [X] T027 [US4] (Skipped by the user's decision on 2026-11-10; the PR says so.) Publish, write a scratch DMC folder from the build, and do the live run of `quickstart.md` § 3: a
  returning NPC is recalled before narrating; with `dmc` off the PATH the game goes on by searching. Note the result
  for the PR description.

**Checkpoint**: T023 passes and the live run is noted.

---

## Phase 6: User Story 1 - The play folder runs the size budgets that already exist (Priority: P1)

**Goal**: FR-001, SC-001, SC-002, after the merge, by the maintainer (spec § Clarifications).

**Independent Test**: the size total and the recall-test figures are posted on issue #179.

- [X] T028 [US1] Confirm `quickstart.md` § Step 1 and § The recall test match the shipped commands and the harness
  (names, paths, parameters), and link them from the PR description.
- [X] T029 [US1] (Done 2026-11-11: 66.5k against 76k; the rest is three files over budget for the next full save. Posted on issue #179.) After the merge, the maintainer: `dmc update`, resume Rorn's Landing, accept migration 004, run
  `dmc size rorns-landing`; post the total on issue #179 beside the 76k baseline (SC-001).
- [X] T030 [US1] (Done 2026-11-11: named 28/30 in output and 22/30 first, indirect 23/30 and 12/30, all at or above the prototype; largest output 5,984 bytes; no tuning needed. Posted on issue #179.) After T029, convert the prototype's `queries.py` into the maintainer's
  `recall-test/questions.tsv` in the DMC folder (never committed), run `tools/recall-test.ps1` against
  `rorns-landing`, and post the two summary lines on issue #179 (SC-002, SC-003). If a figure is below the prototype's
  (28/30 named, 22/30 indirect), open a follow-up to tune the weights (research R1) or the trim (R4). Also run
  `dmc lookup the-crooked-moon <a monster in its monsters.md>` and post the section's size beside the file's (SC-005).

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T031 [P] `.docs/dmc-reference.md`: a `## lookup` and a `## recall` section from the contracts (usage, arguments,
  what is searched, output, failures, exit codes), and both commands added to the command lists in § Rules that apply
  to every command.
- [X] T032 [P] `.docs/decisions.md`: entries for retrieval over markdown with no stored index (issue #179's
  decision), ranking by BM25 weighted by authority (R1), exact heading matching (R7), the PDF rules path (R9, the first
  clarification and its page-number step), recall reaching predecessors' modules (the second clarification), and play
  only until Phase D (FR-020a); each with date, decision, reason, limits and source.
- [X] T033 [P] `.docs/roadmap.md`: the #179 row (Phase A in review on this branch; B to D to come; Phase B after
  #180).
- [X] T034 [P] `.docs/user-guide.md` and `README.md`: a short section beside "How big your files are" on `dmc lookup`
  and `dmc recall` (what each does, one example each with an invented name, that DMC runs them itself and the player
  need do nothing).
- [X] T035 [P] `.docs/development.md` (under Tests: the recall-test harness, where its questions file lives, that it
  never runs in CI) and `.docs/architecture.md` (the commands row and the `Retrieval/` folder).
- [X] T036 Run `dotnet publish` and the full `dotnet test`; every test passes with the same skips as T001.
- [X] T037 Tick the finished tasks here; commit on `009-retrieval-lookup-recall`; push; open the PR with a full
  description (what, why, sources, tests, documents changed, no migration and why, the live run, T029 and T030 left
  for after the merge) and `Part of #179` on its own line; check it with `gh pr view <n> --json body`.
- [X] T038 (Converged with no tasks added; the code review's fixes are in PR #188.) Run `/speckit-converge` until it adds no tasks, then the code review once on the finished PR; bring its
  fixes to the user for approval before committing them; wait for CI to pass before asking the user to merge.

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2**: the baseline first.
- **Phase 2 blocks every story.** T002, T003, T004 and T005 can run in parallel; T006 needs T004; T007 needs T002 and
  T003.
- **US2 (Phase 3) and US3 (Phase 4) are independent** of each other once Phase 2 is done; both use `Trim` (T007a).
- **US4 (Phase 5) needs US2 and US3**: its rules name both commands, and the live run uses them.
- **US1 (Phase 6)**: T028 before the PR; T029 and T030 after the merge.
- **Polish (Phase 7)** after the stories; T036 to T038 last, in order.

### Within each story

Tests first, failing; then the library; then the command wiring; then the guard rows.

## Parallel Examples

```text
Phase 2:  T002 Words.cs | T003 MarkdownHeadings.cs | T004 RetrievalArguments.cs | T005 RetrievalFixtures.cs | T007a Trim.cs
US2:      T008 LookupLibraryTests.cs | T009 LookupCommandContract.cs
US3:      T014 RecallLibraryTests.cs | T015 RecallCommandContract.cs | T021 RecallTimingTests.cs | T022 recall-test.ps1
US4:      T025 dmc-setup-rules | T026 dmc-new-campaign and the template
Polish:   T031 to T035, one document each
```

## Implementation Strategy

- **MVP**: Phases 1 to 3, `dmc lookup`, which helps on its own (stat blocks and sections without whole-file reads).
- **Then** Phase 4, `dmc recall`, the larger gain for long campaigns and sequels.
- **Then** Phase 5, the rules that make DMC use them, with the live run; then the documents and the PR.
- **After the merge**, the maintainer's Step 1 and the recall test (T029, T030), whose figures decide whether a tuning
  follow-up is needed.
