---

description: "Tasks for spec 032: who calls whom what, and limits, held by dmc"
---

# Tasks: Who calls whom what, and limits, held by `dmc`

**Input**: `specs/032-who-calls-whom/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: included. `.docs/development.md` § Adding to `dmc` requires a contract test (in-process and on the
binary) and rows in the error-path guard for each command, and § Changing a campaign-file standard requires a golden
fixture. Every fixture uses invented names only, never play text (the write-path test's cast: Pim, Hessa and Benno
Kettering, Ilvane Vell, Grumbald, Nettlebone, Quenby Tolliver).

**Documentation**: every type and member added or changed gets an XML doc comment, private ones included
(`CLAUDE.md` § Git and GitHub, Documentation).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story of spec.md (US1 to US7)

---

## Phase 1: Setup

- [X] T001 Create `src/Dmc.Cli/PersonRecords/` and the invented fixture campaign `tests/Dmc.Tests/Fixtures/records/` (a DMC folder with `adventures/.active-campaign`, one campaign of 6 persons with records, 6 session logs of 6 `### Scene` headings each, a play card on two sheets with a Voice sample line, canon with a People section, and a second, closed campaign it continues, linked by its `player-options.md` Continuity section), built from the write-path test's fixture
- [X] T002 [P] Add a fixtures helper `tests/Dmc.Tests/PersonRecords/RecordFixtures.cs` that copies the fixture to a temporary folder and returns its paths

---

## Phase 2: Foundational (blocks every story)

- [X] T003 [P] Write `src/Dmc.Cli/PersonRecords/RecordModel.cs`: `Record` (`person` string, required, unique in the campaign ignoring case; `of` string, optional; `rows`; `limits`; no other field), `Row` (`kind`: `usage`, `name` or `relationship`, required; `from` required: a person with a record, `anyone`, or `group: <who>`; `word` required, not blank; `when` optional; `never` optional list of persons or `group: …`; `source` required), `Limit` (`text` required, `source` required), `Source` (`sN`, `sN.M`, `<campaign> sN[.M]`, `player: "…"`, `setup`, `inference`), the slug rule (lower case, each run of other characters a hyphen), and a source-generated `JsonSerializerContext` (research R10)
- [X] T004 Write `src/Dmc.Cli/PersonRecords/RecordFiles.cs`: read every `records/*.json` of a campaign with `JsonDocument`, keeping the line of each row and limit for errors; a file that is not a JSON object is an error line naming the file and line; also read the migration's draft (data-model.md § Draft)
- [X] T005 Write `src/Dmc.Cli/PersonRecords/RecordRules.cs`: rules 1 to 7 of data-model.md § Rules, each failure one line `records/<file> row <n> ("<word>"): <what>`, first failure first, and the note "to <speaker>, \"<word>\" now means <A> and <B>"; a scene source checked against `story/sessions/session-NNN.md` and its `### Scene` headings, and `<campaign> sN[.M]` against that linked campaign's logs (research R2)
- [X] T006 Write `src/Dmc.Cli/PersonRecords/RecordIndex.cs`: the index over the active campaign and every member of `CampaignLinks.Build` with `Before` that has `records/` (research R4); views by word (lower case, whole word, longest first), by speaker, and names; and `Named(text)`, the words of a text with their rows
- [X] T007 [P] Tests `tests/Dmc.Tests/PersonRecords/RecordRulesTests.cs`: each rule refused with its line, each note given, the 4 bad changes of the write-path test refused and its 16 good changes passed
- [X] T008 [P] Tests `tests/Dmc.Tests/PersonRecords/RecordIndexTests.cs`: by word, by speaker, longest-first matching ("Auntie Em" over "Auntie"), a word with no row unmatched whatever it is, rows of the linked earlier campaign included, a campaign with no `records/` giving an empty index, and a `relationship` row never giving a usage (FR-002: "mother" on Hessa's record is not a word Pim says)
- [X] T009 Write `src/Dmc.Cli/Commands/RecordsCommand.cs` with the four subcommands' argument parsing and one-line errors (contracts/dmc-records.md), add it to `src/Dmc.Cli/CommandTable.cs`, and add its rows to `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs`

**Checkpoint**: the model, its rules and the index exist and are tested.

---

## Phase 3: User Story 1, the hook gives each speaker's word (P1) 🎯 MVP

**Goal**: `dmc context-check` prints the rows between the persons present and those of each person named.

**Independent Test**: in the fixture, a message naming one speaker and a word gives that speaker's person only.

- [X] T010 [US1] Write `src/Dmc.Cli/Retrieval/RowsReminder.cs`: persons present (`ReadSet.PresentText`, matched to records by their name rows) and named (a word whose rows all mean one person); for each, the rows between the persons present and their words for them, under the header of contracts/context-check-rows.md with the scene key (8-hex hash of the Present text); nothing in a campaign without `records/`
- [X] T011 [US1] Print the rows part after the canon reminder in `src/Dmc.Cli/Commands/ContextCheckCommand.cs`, through `Quietly` so it never fails, and update the class's XML comment
- [X] T012 [US1] In `src/Dmc.Cli/Retrieval/CanonReminder.cs`, find the persons a message names through `RecordIndex` when the campaign has `records/`, else `Register` as now (research R6)
- [X] T013 [P] [US1] Contract test `tests/Dmc.Tests/ContextCheck/RowsReminderContract.cs` (in-process and on the binary): the three probe shapes on invented data (a speaker's word, a title for family only, a name from the linked earlier campaign), and the unchanged output for a campaign without `records/`

---

## Phase 4: User Story 2, an ambiguous word prints every candidate (P1)

**Goal**: a word in the message prints every row with it, each time.

**Independent Test**: a fixture word that means three persons to three speakers prints all three on a message with no speaker.

- [X] T014 [US2] Add the word lines to `src/Dmc.Cli/Retrieval/RowsReminder.cs` (every row of each word in the message, grouped by word, `"<word>": <person> to <speaker>[ (<when>)][, never <…>]; …`), printed on every message
- [X] T015 [US2] Cap the part at 2,400 bytes in `src/Dmc.Cli/Retrieval/RowsReminder.cs`, cutting person blocks before word lines
- [X] T016 [P] [US2] Extend `tests/Dmc.Tests/ContextCheck/RowsReminderContract.cs`: all candidates printed; a word only the data makes ambiguous printed with both rows; the cap

---

## Phase 5: User Story 3, a person's limits are in front of DMC (P1)

**Goal**: each person present or named has their limits printed once per scene.

**Independent Test**: limits printed on the scene's first message, not on the next, and again after the Present line changes.

- [X] T017 [US3] Add limits to the person blocks in `src/Dmc.Cli/Retrieval/RowsReminder.cs`, and the once-per-scene rule: a person is due when no header since the log's last compaction carries this scene key and their name (as `SheetReminder.cs:199` finds what it printed)
- [X] T018 [P] [US3] Extend `tests/Dmc.Tests/ContextCheck/RowsReminderContract.cs` with logs: printed once, not repeated in the scene, repeated after a Present line change and after a compaction

---

## Phase 6: User Story 4, a bad row is refused at once (P1)

**Goal**: the check after each edit refuses a bad record in one line, exit 2.

**Independent Test**: the write-path test's 20 changes, replayed as hook inputs, refuse its 4 bad ones and pass the rest.

- [X] T019 [US4] Implement `dmc records edited` in `src/Dmc.Cli/Commands/RecordsCommand.cs` (`NeverFails`, as `RedrawCommand.cs:49`): a file under the active campaign's `records/` checked with `RecordRules`, refusal on stderr and exit 2; the note as `additionalContext` JSON and exit 0; anything else nothing and exit 0 (contracts/dmc-records.md)
- [X] T020 [US4] Add to `dmc records edited` the two edit-time checks of FR-007 class 4: a new `player: "…"` source whose quotation is not in the typed player messages of `transcript_path` (`UserText.IsTyped`), and an Edit of a campaign Markdown file whose `new_string` both changes a line of `old_string` and adds a credit to the player
- [X] T021 [US4] Add the `PostToolUse` hook (matcher `Edit|Write|MultiEdit`, command `dmc records edited`) to `kit/settings.json`, and update the toolkit test that checks the kit's hooks in `tests/Dmc.Tests/Toolkit/`
- [X] T022 [P] [US4] Contract test `tests/Dmc.Tests/PersonRecords/RecordsEditedContract.cs` (in-process and on the binary): every row of the contract's table, and the write-path test's 20 changes replayed

---

## Phase 7: User Story 5, the migration keeps every line and asks about each conflict (P1)

**Goal**: migration 010 moves the Names lines and People `(also: …)` items into records, with the player ruling every conflict.

**Independent Test**: the golden fixture passes `MigrationGoldenTests`; the live run on a copy (T047).

- [X] T023 [US5] Write `src/Dmc.Cli/PersonRecords/RecordDraft.cs` and `dmc records draft <campaign>`: each People `(also: …)` item as a draft row with no speaker, sourced to its canon line, beside the latest migration backup (`BackupFolder.Companion`); refuse an existing draft
- [X] T024 [US5] Add `--draft` to `dmc records check` in `src/Dmc.Cli/PersonRecords/RecordCheck.cs` (after T035, whose checker it extends): the draft's rows checked by the rules, FR-007's classes and the draft's three of its own (`ambiguous-no-speaker`, `names-lines-differ`, `no-source`: data-model.md § Finding), every finding with its source line
- [X] T025 [US5] Add the shape check `records-in-place` to `src/Dmc.Cli/Campaign/MigrationChecks.cs` (every sheet's person has a record, no sheet has a "Names they use" line, no People entry has `(also:`, every record passes `RecordRules`), and widen `npc-sheets-have-names-line` to pass "Names for places and things" when `records/` exists (research R9); tests in `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`
- [X] T026 [US5] Write `kit/migrations/010-who-calls-whom.md` (standard 10, touches `character-sheets/`, `story/canon.md`, `records/`; drops none; checks `records-in-place`) with the five headings of `specs/005-campaign-migrations/contracts/migration-file.md`: draft, findings to the player, records written with Write and Edit, Names lines reduced to "Names for places and things", `(also: …)` removed, sample lines glossed `(to X)`, every line accounted `reworded` with its record anchor (research R8); judgments for a person of a closed earlier campaign (`of`, sources `<campaign> sN`), a relationship the files do not state (not written), and the player's #237 rulings as first data, each put to the player to confirm in the migration's session and quoted as its `player:` source (FR-008)
- [X] T027 [US5] Change the templates: `kit/templates/npc-sheet.md` and `kit/templates/pc-sheet.md` (both "Names they use" lines become one "Names for places and things" line, on the play card where there is one, with a comment pointing to the record), and `kit/templates/campaign/story/canon.md:15` (People entries: the bold name only; who calls them what is in `records/`)
- [X] T028 [US5] Update `src/Dmc.Cli/Sheets/SheetLayouts.cs` for the templates' new label, and the toolkit tests that read the templates
- [X] T029 [P] [US5] Build the golden fixture `tests/Dmc.Tests/Fixtures/migrations/010/` (`before/` at standard 9 with two Names lines that disagree, a People `(also: …)` item, a sample line; `after/`; `accounting.tsv`), as `.docs/development.md` § Changing a campaign-file standard step 4 says
- [X] T030 [P] [US5] Contract tests for `dmc records draft` and `check --draft` in `tests/Dmc.Tests/PersonRecords/RecordsCommandContract.cs`

---

## Phase 8: User Story 6, the player reads a record as text (P2)

**Goal**: `dmc records show` and `dmc sheet` show the Called list, relationships and limits.

**Independent Test**: the text view of a fixture record matches contracts/dmc-records.md.

- [X] T031 [US6] Write `src/Dmc.Cli/PersonRecords/RecordText.cs` and `dmc records show <campaign> [<person>...]` (Called, relationships from both sides, the words the person uses for others, limits, each with its source)
- [X] T032 [US6] Show the limits from the record in `dmc sheet` (the player ruled: limits only, not the Called list; FR-009 amended) (`src/Dmc.Cli/Sheets/SheetExport.cs`, `SheetHtml.cs`, `SheetText.cs` as needed), spoiler-free as the sheet is
- [X] T033 [US6] In `kit/skills/dmc-resume-campaign/SKILL.md`, read the text view of the records of the persons in the party (`dmc records show <campaign> <names>`) in place of the play card's Names line
- [X] T034 [P] [US6] Extend `tests/Dmc.Tests/PersonRecords/RecordsCommandContract.cs` for `show`, and the sheet export tests in `tests/Dmc.Tests/Sheets/`

---

## Phase 9: User Story 7, the full save finds the data errors (P2)

**Goal**: `dmc records check <campaign>` lists findings in FR-007's classes.

**Independent Test**: planted errors in a fixture copy are found, and a word that means different persons to different speakers is not.

- [X] T035 [US7] Write `src/Dmc.Cli/PersonRecords/RecordCheck.cs` for records: `sample-breaks-rows` (quoted samples on the speaker's sheet, the `(to X)` gloss), `rows-disagree`, `sample-stale` (rows and the key words of limits), and `invalid` for a rule broken; output `<class>: <where>: <what>`, then `N findings` or `no findings`, exit 0
- [X] T036 [US7] In `kit/skills/dmc-campaign-full-save/SKILL.md`, run `dmc records check <campaign>` and fix or ask about each finding, pointing to the write reference
- [X] T037 [P] [US7] Tests `tests/Dmc.Tests/PersonRecords/RecordCheckTests.cs`: each class found on planted errors, the draft's three classes on a planted draft, the not-a-finding case, the output shape

---

## Phase 10: Polish and cross-cutting

- [X] T038 [P] Write the write reference `kit/skills/dmc-character-sheet-update/records-format.md`: the record's shape, a worked example of each kind (usage, name, relationship), a limit, each source form, `never`, `when`, and what a refusal asks DMC to do; point to it from `kit/skills/dmc-character-sheet-update/SKILL.md` (lines 25 and 38 reworded for rows), the full-save skill and migration 010
- [X] T039 Reword `kit/dm-rules.md:78` and `:81` ("Names they use") for the record, with no net growth of the file (32,507 bytes now, cap 32,510); line 63 unchanged; count the bytes
- [X] T040 [P] Update `.docs/product-overview.md:9` and `README.md:3` ("plain Markdown files"), and add the decision record to `.docs/decisions.md` (records as JSON, written with Edit and checked after each edit, and why: the write-path test on #263)
- [X] T041 [P] Update `.docs/campaign-files.md` (records/, the Names line's new shape, migration 010), `.docs/architecture.md` (`Records/`, the hook's rows part, the `PostToolUse` check), `.docs/dmc-reference.md` (`dmc records`), `.docs/roadmap.md` (#290 status)
- [X] T042 Check every new and changed type and member has an XML doc comment, private ones included, and every changed script a header comment
- [X] T043 Run migration 010 live once on the golden fixture's `before/` in a scratch DMC folder made with `artifacts/bin/dmc install` (`.docs/development.md` § Changing a campaign-file standard step 5) and fix the migration's prose for what DMC got wrong
- [X] T044 Measure SC-004 on this computer: the hook's output on every player message of the long campaign's play logs, with and without the rows part, on the migrated copy (T047), as real tokens; the mean extra per play turn against 1,000; and the resume's `dmc size` before and after
- [X] T045 Measure the index's and the hook's run time on the migrated copy (plan, Performance Goals)
- [X] T046 Link `specs/032-who-calls-whom/` from issue #290's description
- [X] T047 **This computer**: migrate a copy of the long campaign, never the live one and never while it is in play (quickstart.md step 1): put every finding of `dmc records check --draft` to the player, apply #237's rulings first (Hala's sample line becomes "Wuv you, Nytha." to Tam; the note crediting the player removed), run `dmc campaign verify`, and give the player printed records for Hala, Tam and Kole (step 7 of #263's build)
- [X] T048 **This computer**: SC-001's probes and "Nytha?" on the migrated copy; SC-002 (the four known cases, a hand check of every finding at 20% or fewer false, planted errors in a second copy)
- [X] T049 If the change alters how DMC plays (CLAUDE.md § Git and GitHub, "Slip test", says what counts), run the slip test against the pass line in the issue and put the result in the PR: SC-003 with the pass line, fork points and runs fixed on #263 (comment of 2026-11-26), against a control at the same time, after checking for other slip-test runs on this computer. Run 2026-11-27, 360 forks: fail and underpowered, with a defective treatment arm (spec.md SC-003; T055); not rerun, by the maintainer's ruling
- [ ] T050 `/speckit-converge` until a pass adds no tasks, after the tests and again after the code review's fixes
- [X] T051 SC-003's pilot found no hook output in any fork: run each slip-test run on its own base log in place, with no `--fork-session`, so the hook's transcript exists (`src/Dmc.SlipTest/ClaudeRunner.cs`, `Runner.cs`; the stand-in in `tests/Dmc.Tests/SlipTest/SlipTestFixtures.cs`; `RunnerTests.EachRunContinuesItsOwnBaseLogSoTheHooksTranscriptExists`), and note on #289 that it is its defect 1
- [X] T052 Cap `dmc context-check`'s whole output at 9,500 bytes (FR-005): the one-line parts and the rows first, then canon, then the sheet lines (`src/Dmc.Cli/Commands/ContextCheckCommand.cs`, `Retrieval/CanonReminder.cs`, `Retrieval/SheetReminder.cs`; `SheetReminderContract.TheWholeOutputStaysWithinTheHooksCapAndTheShortLinesAlwaysPrint`); SC-003's control is 0.4.3 with the same cap and nothing else
- [X] T055 SC-003's run found the rows part never printed some persons' blocks: every present or named person's "says" line prints before the rest of any block, within the room the hook's whole output has left; a person is present when the Present text names them as a message would, outside bracketed notes (`src/Dmc.Cli/Retrieval/RowsReminder.cs`, `src/Dmc.Cli/Commands/ContextCheckCommand.cs`; `RowsReminderCapTests.EverySpeakersSaysLineIsPrintedBeforeTheRestOfAnyBlock`, `APersonIsPresentByAnyWordThatMeansOnlyThemAndABracketedNoteNamesNoOne`; on this computer `RowsAtSlipPointsCheck` over the trial's 60 points and four probes)
- [X] T054 `dmc records show` finds a person by a file name or words of their name, gives every record a name fits, and names a miss last instead of failing the call (SC-003's treatment base resume needed six calls; `src/Dmc.Cli/Commands/RecordsCommand.cs`; `RecordsCommandContract.ShowFindsAPersonByAFileNameOrWordsOfTheirNameAndNamesTheMissesLast`)
- [X] T053 The pilot's thinking check takes a difference `allow.tsv` documents arm by arm (`src/Dmc.SlipTest/PilotCheck.cs`; `RunnerTests.ThinkingThatDiffersBetweenArmsFailsThePilot`)

---

## Dependencies and order

- Phase 1, then Phase 2, block everything.
- US1 (T010 to T013) before US2 and US3, which extend `RowsReminder.cs`.
- US4 (T019 to T022) depends only on Phase 2.
- US5's code (T023 to T025, T030) depends on Phase 2 and on T035's checker for the classes; its kit files (T026, T027) and fixture (T029) on the decided shape only.
- US6 and US7 depend on Phase 2.
- T038 before T043; T043 before T047; T047 before T044, T045 and T048; T048 before T049; T050 last.

## Parallel examples

- After Phase 2: US4 (T019 to T022), US6 (T031 to T034) and US7 (T035 to T037) touch different files and can run alongside US1.
- In Phase 10: T038, T040 and T041 touch different files.

## Implementation strategy

1. Phases 1 and 2, then US1 (the MVP: the probes' shape works on fixtures).
2. US2, US3 and US4, then US5's code and kit files, US6, US7.
3. Polish: the kit's words, the documents, the live migration run on the fixture.
4. This computer: the migrated copy, the probes and check, the tokens, then the slip test.
5. One PR, `Closes #290` and `Part of #263`, with the result of each measurement.

## Phase 11: Convergence

- [X] T056 Key a scene on the sorted persons present (`RowsReminder.Present`), not on the whole Present text and Combat tracker, so an HP or initiative change is not a new scene, in `src/Dmc.Cli/Retrieval/RowsReminder.cs` per FR-005 (contradicts); `RowsReminderCapTests.TheSceneKeyChangesWithWhoIsPresentAndNothingElse`
- [X] T057 Print each due person's limits line in the first pass with their "says" line, so a long Called list never crowds out a limit, in `src/Dmc.Cli/Retrieval/RowsReminder.cs` per US3/AC1 and FR-005 (partial); `RowsReminderCapTests.EverySpeakersSaysLineIsPrintedBeforeTheRestOfAnyBlock`
- [X] T058 Find a record edit's new quotations of the player by their words, from the Edit's `new_string` or each MultiEdit edit, not by row position, so a moved row or a deleting Edit adds none, in `src/Dmc.Cli/PersonRecords/RecordEdit.cs` per FR-007 (partial); `RecordsEditedContract.AnOldQuotationIsNotNewWhenItsRowMovesOrAnotherIsDeleted`
- [X] T059 Let two records whose rows name each other be written one at a time (migration 010 step 5, and two new people in play), in `src/Dmc.Cli/PersonRecords/RecordEdit.cs` or `kit/migrations/010-who-calls-whom.md` per FR-008 and FR-006 (partial). The maintainer's ruling: in the edit hook a speaker or never-person with a sheet and no record yet is a note; `dmc records check` still refuses it; `RecordsEditedContract.ASpeakerWithASheetAndNoRecordYetStandsWithANote`, `RecordRulesTests.ASpeakerWithASheetAndNoRecordIsANoteAfterAnEditAndABreakAtTheFullSave`
- [X] T060 Count the persons of the campaigns this one continues as speakers and never-persons in the check, in `src/Dmc.Cli/PersonRecords/RecordRules.cs` per FR-004 and FR-003 (partial); `RecordRulesTests.APersonOfTheCampaignThisOneContinuesMayBeASpeaker`
- [X] T061 Check a scene source against the session log's real scene headings (`ReadSet.ScenePattern`, by number), not a count of every `### Scene` line, in `src/Dmc.Cli/PersonRecords/RecordRules.cs` per FR-003 (partial); `RecordRulesTests.AScenesHeadingMustBeRealAndTheSceneInPlayCounts`
- [X] T062 Measure SC-004 again after T055 and T057 on the migrated copy, and record it in `specs/032-who-calls-whom/spec.md` per SC-004 (partial); +2 tokens per turn on average, max 683, as before
- [X] T063 Keep every letter and digit in a record's file name, lower-cased, so two persons never share one, in `src/Dmc.Cli/PersonRecords/RecordFiles.cs` per FR-001 (partial); `RecordRulesTests.AFileNameKeepsEveryLetter`
- [X] T064 Accept a scene source for the scene in play, the one after the last heading of this campaign's latest session log, whose block is written when the scene ends, in `src/Dmc.Cli/PersonRecords/RecordRules.cs` (`SceneBook`), by the maintainer's ruling (2026-11-28) per FR-003 and FR-006 (partial); `RecordRulesTests.AScenesHeadingMustBeRealAndTheSceneInPlayCounts`
