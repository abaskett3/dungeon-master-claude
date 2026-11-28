---

description: "Tasks for spec 036: the rest of a sheet as facts and free-text lists, with sources"
---

# Tasks: The rest of a sheet as facts and free-text lists, with sources

**Input**: `specs/036-sheet-facts/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: included. Contract tests run in-process and on the binary; the migration has a golden fixture. Every
fixture uses invented names only (spec 032's cast: Pim, Hessa and Benno Kettering, Ilvane Vell, Grumbald,
Nettlebone, Quenby Tolliver).

**Documentation**: every type and member added or changed gets an XML doc comment, private ones included.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story of spec.md (US1 to US7)

---

## Phase 1: Foundational (blocks every story)

- [X] T001 Extend `src/Dmc.Cli/PersonRecords/RecordModel.cs`: `Fact` (key, value, section, card, source, problems), `ListEntry` (list, text, section, card, source, problems), and `PersonRecord` gains `Sheet`, `Facts`, `Lists`
- [X] T002 Extend `src/Dmc.Cli/PersonRecords/RecordFiles.cs` to read `sheet`, `facts` and `lists` (a number value as its text; unknown keys and wrong types as problems)
- [X] T003 Write `src/Dmc.Cli/PersonRecords/RecordDates.cs`: rule F of data-model.md (an ordinal day needs a month, a range ends with one, a year when the calendar has one) and the calendar's year read from `game-state.md`
- [X] T004 Extend `src/Dmc.Cli/PersonRecords/RecordRules.cs` with rules 8 to 12 (data-model.md), dates on limits and `when` too, and `SceneBook` carrying whether the calendar has a year
- [X] T005 [P] Tests `tests/Dmc.Tests/PersonRecords/RecordFactsTests.cs`: each new rule refused with its line, the good record passing, a number value taken as text
- [X] T006 [P] Tests `tests/Dmc.Tests/PersonRecords/RecordDatesTests.cs`: day with month, day alone, ranges, a year required and given, "the 3rd floor" refused

## Phase 2: User Stories 1 to 4 (the check after each edit) (P1)

- [X] T007 [US3] In `src/Dmc.Cli/PersonRecords/RecordEdit.cs`, the keys note (research R6), and the quotation check over facts and lists
- [X] T008 [US1] [US2] [US3] [US4] Rows in `tests/Dmc.Tests/PersonRecords/RecordsEditedContract.cs`: a second value refused (exit 2), a day alone refused, a new key noted, a fact with no source refused, a new quotation on a fact checked

## Phase 3: User Story 7 and the print (P1)

- [X] T009 Write `src/Dmc.Cli/PersonRecords/RecordSheet.cs`: `Print(record, sources)` (data-model.md § Print, the ability and attacks tables included) and `Sheets(campaignFolder)`, the person sheets of a campaign as (file, lines): each Markdown sheet, then each record's print whose `sheet` file is not there
- [X] T010 [US7] `dmc records show` facts and lists, and `--card`, `--sheet`, `--all` (contracts/dmc-records.md) in `src/Dmc.Cli/PersonRecords/RecordText.cs` and `src/Dmc.Cli/Commands/RecordsCommand.cs`
- [X] T011 [P] Tests `tests/Dmc.Tests/PersonRecords/RecordSheetTests.cs`: the print of the contract's record, the card as a view, the tables, sections in the template's order with History last, the print read back by `SheetText` giving the same fields
- [X] T012 Rows in `tests/Dmc.Tests/PersonRecords/RecordsCommandContract.cs` for the new flags

## Phase 4: User Story 5 (the readers) (P1)

- [X] T013 [US5] `src/Dmc.Cli/Size/ReadSet.cs` lists sheets through `RecordSheet.Sheets`; the play-card budget on the card without sources
- [X] T014 [US5] `src/Dmc.Cli/Retrieval/SheetReminder.cs` lists sheets through `RecordSheet.Sheets`
- [X] T015 [US5] `src/Dmc.Cli/Sheets/SheetExport.cs` lists sheets through `RecordSheet.Sheets`
- [X] T016 [US5] `src/Dmc.Cli/PersonRecords/RecordCheck.cs`: samples and sheet titles through `RecordSheet.Sheets`; the `sheet-and-record` finding
- [X] T017 [P] [US5] Tests: a fixture person as a Markdown sheet and as a record give the same `dmc size` parts, hook sheet lines and export check

## Phase 5: User Story 6 (the migration) (P1)

- [X] T018 [US6] `dmc records draft <campaign> --sheets` in `src/Dmc.Cli/PersonRecords/RecordDraft.cs` (research R7), with a contract row
- [X] T019 [US6] `sheets-in-records` in `src/Dmc.Cli/Campaign/MigrationChecks.cs`, with tests in `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`
- [X] T020 [US6] `kit/migrations/011-sheet-facts.md` (spec FR-013)
- [X] T021 [US6] Golden fixture `tests/Dmc.Tests/Fixtures/migrations/011/` (`before/`, `after/`, `accounting.tsv`)

## Phase 6: The kit and the documents

- [X] T022 `kit/skills/dmc-character-sheet-update/records-format.md`: facts and lists, with a worked example of each and the date rule
- [X] T023 The skills: character-sheet update, resume (§ 2 item 5: `dmc records show --card` or `--sheet` for a person whose record prints their sheet), full save, new campaign; `kit/dm-rules.md` unchanged
- [X] T024 Documents: `.docs/campaign-files.md`, `.docs/architecture.md`, `.docs/dmc-reference.md`, `.docs/roadmap.md`, `.docs/decisions.md`

## Phase 7: Polish

- [X] T025 The token cost (constitution Core 3): the resume's `dmc size` and an average play turn's hook output for the fixture before and after, and the bytes each skill and `records-format.md` changed
- [X] T026 SC-002: the date rule's hits on a read-only copy of the long campaign's sheets, 50 checked by hand (390 hits; 4 of 50 false, 8%: a spell slot's "1st", two ranges with a word before "to", "the 5th of the second month")
- [ ] T027 Publish, `dotnet test` (passed locally on the branch), CI (no PR yet)
- [ ] T028 Run migration 011 live once in a scratch DMC folder (`.docs/development.md` § Changing a campaign-file standard, step 5)
- [ ] T029 If the change alters how DMC plays (CLAUDE.md § Git and GitHub, "Slip test", says what counts), run the slip test against the pass line in the issue and put the result in the PR
- [ ] T030 `/speckit-converge` until a pass adds no tasks

## Phase 8: Found in the build, open

- [X] T031 `dmc recall` searches a record's print with sources as a sheet (`src/Dmc.Cli/Retrieval/RecallScope.cs`, `Recall.cs`), so a fact moved into a record is still found; a hit names the sheet's path and the print's line
- [X] T036 `dmc lookup <campaign> <heading>` searches each record's print with sources as a sheet (`src/Dmc.Cli/Retrieval/Lookup.cs`). (The canon reminder reads only `canon-archive.md`, not sheets: nothing to change there.)
- [X] T032 `dmc campaign sequel` and the closing sample read a closed campaign's records that hold sheets (`src/Dmc.Cli/Campaign/SequelDigest.cs`, `SampleDraw.cs`): each fact and entry a unit, worded as the print words it, so a copied digest line matches; a record of rows and limits alone is not a source, as before
- [X] T033 `dmc size` marks a companion's printed sheet with no card entry as having no play card, and reads one with a card to its end (`ReadSet.SheetPart`), with a test
- [X] T034 The date rule takes up to two words before a range's joint ("the 14th night to the 18th of Eleasis") and "of the <ordinal> month", the two kinds of false hit SC-002's sample found
- [X] T037 `dmc-generate-image-prompt` reads a sheet held in a record with `dmc records show --all`
- [ ] T038 A sheet's title bracket (`# Rorn (Hysvearorn)`) is lost in the print, which titles it with the record's person (`Rorn`): keep the sheet's full title (a `title` field, or the migration keeps it as a fact) so Party status blocks and `dmc sheet` still match it
- [X] T035 `dmc size` measures a printed sheet with its source marks, which the resume reads, and compares the budget without them (`Part.MarkCharacters`, `Part.BudgetTokens`; #262)

## Dependencies

Phase 1 blocks all. Phase 3's T009 blocks Phase 4 and T021. T020 needs T018 and T019. T025 needs Phase 4.
