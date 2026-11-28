# Implementation Plan: The rest of a sheet as facts and free-text lists, with sources

**Branch**: `claude/036-sheet-facts` | **Date**: 2026-11-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/036-sheet-facts/spec.md` (issue #291, part B of #263)

## Summary

A person's record (spec 032) gains `facts` (a key, a value, a source) and `lists` (a list's name, one fact's free
text, a source), and a `sheet` field naming the Markdown sheet it replaces. Part A's check after each edit
(`dmc records edited`) gains the new rules: one value per key, the checked keys' values, every entry's source, full
in-world dates; and a note that shows a record's keys when a new one is added. `dmc` prints a record as a sheet in its
template's shape, and the readers of a Markdown sheet (the resume, `dmc size`, the hook's sheet lines, `dmc sheet`,
`dmc records check`) read that print where the Markdown sheet is gone. `dmc records draft --sheets` drafts the
regular lines for migration 011, which moves each sheet into its record with every line accounted for.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli/Dmc.Cli.csproj`)

**Primary Dependencies**: none new; `System.Text.Json`'s `JsonDocument`, as `RecordFiles.Parse` reads records today
(`src/Dmc.Cli/PersonRecords/RecordFiles.cs:104`)

**Storage**: the same `adventures/<campaign>/records/<slug>.json` files, with three new fields; the sheet draft beside
the migration's backup

**Testing**: MSTest in `tests/Dmc.Tests` (rule tests, the print against the Markdown it came from, the edit hook's
contract in-process and on the binary, the migration's golden fixture); invented fixtures only

**Target Platform**: Windows and Linux (x64)

**Project Type**: CLI (`dmc`) and the prompt kit (`kit/`)

**Performance Goals**: `dmc records edited` runs after every edit of a campaign file; the new rules are a pass over
each record's entries with two regular expressions, no new file reads except `game-state.md`'s date line

**Constraints**: `kit/dm-rules.md` no net growth (`ToolkitTests` byte cap); one-line errors; the note at most one line

**Scale/Scope**: the long campaign's 26 sheet files are 385,210 bytes (19 person sheets, 6 history files,
`minor-npcs.md`), 2026-11-28

## Constitution Check

*GATE: checked before Phase 0 and again after Phase 1.*

| Principle | How this plan meets it |
|---|---|
| Core 1, Consistency | The new state is in the records (data-model.md). One home for each fact: the card is a view (`card`), not a copy, and a second value for a key is refused. |
| Core 2, Fun | The player gains fewer contradictions (a second value refused, a date with no month refused) and sees a source for every line of a sheet. |
| Core 3 and Development 1, Cost | `kit/dm-rules.md`: 0 bytes. Skills: resume about +250 bytes (the `dmc records show` reads), sheet-update about +300, full save about +100, new-campaign about +150; `records-format.md` about +2,000 bytes, loaded only by the skills that write records. A resume reads the print with sources: about 6 tokens a source mark more than a sheet with none; the long campaign's sheets carry 1,079 marks already, so the change is measured, not assumed (SC-004). |
| Development 2, No change breaks a campaign | Migration 011 (standard 11) with its shape check `sheets-in-records` and golden fixture. A record with no facts or lists, and a campaign with no records, read as before. |
| Development 3, One step | No new install step or dependency. |
| Development 4, Grounded | research.md cites a file and line for each decision or says unverified. |
| Development 6, One home | The rules: `src/Dmc.Cli/PersonRecords/RecordRules.cs` (code) and `records-format.md` (kit). The print: `RecordSheet.cs`. The skills point to `records-format.md`. |
| Development 7, Tests | No new tool. |
| Dungeon Master 1 to 4 | Not affected. A refusal is out of play, before the reply. |
| Documentation 1 to 4 | The documents of FR-015 change in this PR. |

No violations.

## Project Structure

### Documentation (this feature)

```text
specs/036-sheet-facts/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── records-file.md      # the new fields
│   └── dmc-records.md       # show's flags, draft --sheets, the new refusals and the note
└── tasks.md
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── PersonRecords/
│   ├── RecordModel.cs        # Fact, ListEntry; PersonRecord gains Sheet, Facts, Lists
│   ├── RecordFiles.cs        # reads the new fields
│   ├── RecordRules.cs        # one value a key, checked keys, sources, sheet; dates through RecordDates
│   ├── RecordDates.cs        # new: the full-date rule (FR-005)
│   ├── RecordSheet.cs        # new: a record printed as a sheet (FR-008), and the sheets of a campaign (FR-010)
│   ├── RecordEdit.cs         # the keys note (FR-006); quotations in facts and lists
│   ├── RecordText.cs         # facts and lists in show (FR-009)
│   ├── RecordCheck.cs        # sheet-and-record; samples from the print
│   └── RecordDraft.cs        # draft --sheets (FR-012)
├── Commands/RecordsCommand.cs   # show --card/--sheet/--all, draft --sheets
├── Size/ReadSet.cs              # the read set from the print
├── Retrieval/SheetReminder.cs   # the sheet lines from the print
├── Sheets/SheetExport.cs        # dmc sheet from the print
└── Campaign/MigrationChecks.cs  # sheets-in-records

kit/
├── migrations/011-sheet-facts.md
└── skills/
    ├── dmc-character-sheet-update/SKILL.md, records-format.md
    ├── dmc-resume-campaign/SKILL.md
    ├── dmc-campaign-full-save/SKILL.md
    └── dmc-new-campaign/SKILL.md

tests/Dmc.Tests/
├── PersonRecords/            # RecordFactsTests, RecordDatesTests, RecordSheetTests; the edited contract's new rows
├── Campaign/MigrationChecksTests.cs
└── Fixtures/migrations/011/
```

**Structure Decision**: the new model lives in the existing `PersonRecords/` folder; each reader changes only where
it lists a campaign's sheets, through one function (`RecordSheet.Sheets`).

## Phase 0 and Phase 1

- Research: [research.md](research.md), R1 to R8.
- Data model: [data-model.md](data-model.md).
- Contracts: [records-file.md](contracts/records-file.md), [dmc-records.md](contracts/dmc-records.md).
- Quickstart: [quickstart.md](quickstart.md).

**Constitution Check after design**: unchanged; no violations.

## Risks

- **The date rule refuses good text.** SC-002 counts its false refusals on the long campaign before the migration;
  the refusal names the day, so DMC's fix is one word.
- **A record grows large.** Hala's sheet is about 30,000 bytes; as JSON with sources it may be 40,000 (unverified,
  measured on the migrated copy). Edit changes one line, and the resume reads the print, not the JSON.
- **The print differs from the sheet it replaces.** A test prints a fixture record and reads it with each reader,
  against the Markdown sheet it came from.
- **The migration invents lines.** Every draft entry points to its sheet line; `dmc campaign verify` accounts for
  each line; an entry with no source goes to the player. #291: do one group, verify, then the next.
