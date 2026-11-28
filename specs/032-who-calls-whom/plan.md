# Implementation Plan: Who calls whom what, and limits, held by `dmc`

**Branch**: `claude/032-who-calls-whom` | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/032-who-calls-whom/spec.md` (issue #290, part A of #263)

## Summary

The words people use for each other, the relationships between persons, and each person's limits move from free-text
Names lines and canon's `(also: …)` lists into one JSON record per person under `records/`. DMC writes them with
Edit, and a `PostToolUse` hook (`dmc records edited`) checks every record right after each edit and refuses a bad one
in one line, as the write-path test on #263 chose. `dmc` builds an index over the records of the campaign and the
campaigns it continues, and `dmc context-check` prints the rows that fit the scene and the message, with every
candidate for an ambiguous word. `dmc records` shows a record as text and checks the records and the migration's
draft at the full save. Migration 010 moves the existing lines, with every conflict put to the player.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli/Dmc.Cli.csproj`)

**Primary Dependencies**: none new. `System.Text.Json` with a source-generated context (research R10)

**Storage**: campaign files: `adventures/<campaign>/records/<slug>.json` (new); the migration's draft beside its backup

**Testing**: MSTest in `tests/Dmc.Tests` (contract tests in-process and on the binary; the migration golden test);
invented fixtures only

**Target Platform**: Windows and Linux (x64), as `dmc` ships

**Project Type**: CLI (`dmc`) and the prompt kit (`kit/`)

**Performance Goals**: `dmc context-check` and `dmc records edited` run on every message and edit; each stays well
under the hook's time (unverified; measured in the build against `dmc recall`'s 0.64 s over two campaigns, #238)

**Constraints**: `kit/dm-rules.md` no net growth (32,507 of 32,510 bytes, `ToolkitTests.cs:1378`); the hook's new part
at most 2,400 bytes; SC-004's 1,000 tokens per average play turn; one-line errors

**Scale/Scope**: the long campaign has 20 sheets (not counting `-history.md` files) and continues one campaign of 19;
records a few kilobytes each (unverified, measured in the migration)

## Constitution Check

*GATE: checked before Phase 0 and again after Phase 1.*

| Principle | How this plan meets it |
|---|---|
| Core 1, Consistency | New state is in `records/*.json` (named in data-model.md); every other file keeps its role. A resume reads the records' text view for the party (FR-010). |
| Core 2, Fun | The player gains fewer name slips (#270: correct facts next to the reply cut fact slips by 53%), and reads records as text, never JSON. |
| Core 3 and Development 1, Cost | `kit/dm-rules.md`: net 0 bytes or fewer (lines 78 and 81 reworded). Skills: resume about +200 bytes, full save about +250, sheet-update about +150 net, migrate none; `records-format.md` about 3 KB, loaded only by the skills that write rows. The hook part is capped at 2,400 bytes and measured per play turn (SC-004). The resume's `dmc size` result is measured before and after on the migrated copy. |
| Development 2, No change breaks a campaign | Migration 010 (standard 10), with its shape check and golden fixture. A campaign without `records/` behaves as before in every part (research R6). Migration 003's check is widened so a campaign below 3 can reach 10 (research R9). |
| Development 3, One step | No new install step or dependency; the hook is added to `kit/settings.json`, which `dmc install` and `dmc update` already write. |
| Development 4, Grounded | Each decision in research.md cites a file and line, or says unverified. |
| Development 6, One home | The row rules: `src/Dmc.Cli/PersonRecords/` (code) and `records-format.md` (the kit's one reference). The when-to-write rule stays in the sheet-update and full-save skills, which point to the reference. |
| Development 7, Tests | No new tool; tests use invented fixtures. |
| Dungeon Master 1 to 4 | Not affected: no change to arcs, fights or modules. A refusal is out of play, before the reply. |
| Documentation 1 to 4 | The documents of FR-011 change in this PR; the decision record names the end of "plain Markdown files". |

No violations.

## Project Structure

### Documentation (this feature)

```text
specs/032-who-calls-whom/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── records-file.md
│   ├── dmc-records.md
│   └── context-check-rows.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/Dmc.Cli/
├── PersonRecords/            # new (not "Records": a namespace of that name hides members called Records)
│   ├── RecordModel.cs        # Record, Row, Limit, Source; the JSON context (R10)
│   ├── RecordFiles.cs        # reading records/ and the draft, with line numbers for errors
│   ├── RecordRules.cs        # the refusals and the note (data-model.md § Rules)
│   ├── RecordIndex.cs        # by word, by speaker, names; over the linked campaigns (R4)
│   ├── RecordText.cs         # the text view (FR-009)
│   ├── RecordCheck.cs        # the findings (R7)
│   └── RecordDraft.cs        # the draft's regular shapes (R7)
├── Commands/RecordsCommand.cs        # show, check, draft, edited
├── Retrieval/RowsReminder.cs         # the hook's rows part (R5)
├── Retrieval/CanonReminder.cs        # finds persons through the index when records exist (R6)
├── Commands/ContextCheckCommand.cs   # adds the rows part
├── Campaign/MigrationChecks.cs       # records-in-place; 003's check widened (R9)
├── Sheets/SheetExport.cs, SheetLayouts.cs   # Called and limits from the record (FR-009)
└── CommandTable.cs

kit/
├── settings.json                                       # the PostToolUse hook
├── dm-rules.md                                         # lines 78 and 81, no net growth
├── migrations/010-who-calls-whom.md
├── templates/npc-sheet.md, pc-sheet.md, campaign/story/canon.md
└── skills/
    ├── dmc-character-sheet-update/SKILL.md, records-format.md
    ├── dmc-campaign-full-save/SKILL.md
    ├── dmc-resume-campaign/SKILL.md
    └── (dmc-migrate-campaign/SKILL.md unchanged: migration 010's own steps name the draft and its commands)

tests/Dmc.Tests/
├── PersonRecords/            # rules, index, text view, check, draft; RecordsCommandContract.cs
├── ContextCheck/RowsReminderContract.cs
├── Campaign/MigrationChecksTests.cs
└── Fixtures/records/, Fixtures/migrations/010/
```

**Structure Decision**: one new folder, `src/Dmc.Cli/PersonRecords/`, for the model and its rules, used by one new command
and by the hook. Every other change is in the file that already owns the behaviour.

## Phase 0 and Phase 1

- Research: [research.md](research.md), R1 to R11; nothing left open.
- Data model: [data-model.md](data-model.md).
- Contracts: [records-file.md](contracts/records-file.md), [dmc-records.md](contracts/dmc-records.md),
  [context-check-rows.md](contracts/context-check-rows.md).
- Quickstart: [quickstart.md](quickstart.md).

**Constitution Check after design**: unchanged; no violations.

## Risks

- **A false refusal stops a good edit.** The check refuses only rules 1 to 7 of the data model; the write-path test
  had 0 refusals on 96 valid changes (16 in each of 6 runs). The contract tests replay its 20 changes.
- **The hook prints too much.** The cap and the once-per-scene rule; SC-004 measures it before the PR.
- **The migration invents lines.** Every draft row points to its sheet or canon line; the player rules every
  conflict; `dmc campaign verify` accounts for every line.
- **The quotation check refuses a true quotation from an earlier session.** It checks only sources new in the edit,
  against this session's messages; an older quotation needs the player's words again, or another source.
