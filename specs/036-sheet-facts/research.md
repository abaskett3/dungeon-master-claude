# Research: The rest of a sheet as facts and free-text lists (spec 036)

Each decision cites the code it rests on, or says unverified.

## R1. The facts and lists go in the person's existing record

- **Decision**: three new fields on the record of spec 032: `sheet`, `facts`, `lists`. **ASSUMED** (spec
  Clarifications).
- **Rationale**: one file a person, so one home for each fact; part A's reader, rules and edit hook already handle
  one record a file (`src/Dmc.Cli/PersonRecords/RecordFiles.cs:80`, `RecordEdit.cs:75`). A second file a person
  would need a second slug rule and a second check of the pair.
- **Alternatives**: a `sheets/<slug>.json` beside `records/` (two files a person, rejected); Markdown lines with a
  source on each (rejected: #263 item 11 fixes JSON).

## R2. Readers read a print, not the JSON

- **Decision**: `RecordSheet.Print(record, sources)` builds the Markdown a template-shaped sheet would hold, and
  `RecordSheet.Sheets(campaignFolder)` lists a campaign's sheets as (file name, lines): each Markdown person sheet, then
  each record with a `sheet` whose file is not there.
- **Rationale**: four readers list `character-sheets/*.md` and parse the Markdown: the read set
  (`src/Dmc.Cli/Size/ReadSet.cs:189`), the hook's sheet lines (`src/Dmc.Cli/Retrieval/SheetReminder.cs:125`),
  `dmc sheet` (`src/Dmc.Cli/Sheets/SheetExport.cs:138`) and the sample check
  (`src/Dmc.Cli/PersonRecords/RecordCheck.cs:180`). Swapping their file listing for one function keeps their parsing,
  budgets and layouts as they are.
- **Alternatives**: teach each reader the JSON (four parsers of one shape, rejected by Development 6).

## R3. The card is a view

- **Decision**: an entry with `"card": true` prints under `## Play card`, in record order, and not in its section.
- **Rationale**: the resume reads a companion's sheet only to the end of the card
  (`src/Dmc.Cli/Size/ReadSet.cs:376`, `kit/skills/dmc-resume-campaign/SKILL.md` § 2 item 5), so what the card needs
  must be on it; a copy elsewhere is two homes.

## R4. The full-date rule reads text

- **Decision**: a pattern over each value, entry, limit and `when`: an ordinal day must be followed by a month word,
  or by `to`/`and`/`-` and another ordinal followed by one, and a year after the month. **Ruled by the player,
  2026-11-28** (was ASSUMED, a year only when `game-state.md`'s in-world date held one): every date has a year, so
  memories can tell one Midwinter from the next.
- **Rationale**: the slip was in free text ("ready on the 14th", #263 feedback comment). A separate date field would
  not catch it. On 2026-11-28 the long campaign's game-state date was "Wednesday the 18th of Eleasis, about 6 AM"
  (no year), and its sheets had 436 ordinal days with no month after them (a pattern count, unverified by hand;
  SC-002 checks a sample).
- **Month word**: a month of the Harptos calendar after the day, with an optional `of` (the player adopted Harptos
  for the campaigns, 2026-11-28). With any capitalised word as a month, the year rule would refuse 13 lines of the
  live sheets that are names ("the 58th Dwarven", "the 9th Kelderak", "1st Class"; a count, 2026-11-28), so an ordinal
  followed straight by a capitalised word that is not a month is a name. "the 3rd floor" is still refused. **ASSUMED**.

## R5. Checked keys

- **Decision**: `STR` to `CHA` (1 to 30), `<ABILITY> save` (signed), `Proficiency bonus` (+2 to +9) and `Passive
  Perception` (whole number), each value starting with its number. **ASSUMED**.
- **Rationale**: these are the values `dmc sheet`'s arithmetic reads (`src/Dmc.Cli/Sheets/SheetCheck.cs:244` to
  `:310`) through `SheetNumbers.ParseSigned` (`src/Dmc.Cli/Sheets/SheetNumbers.cs:29`). The print builds the ability
  table from them, so they are the keys `dmc` computes with.

## R6. The keys note

- **Decision**: in `dmc records edited`, after a record edit that passes, compare the record's keys and list names
  with the record as it was (`tool_response.originalFile`, or the edit undone, as the rows' notes do at
  `RecordEdit.cs:111`); for each new one, one note naming the others and any that shares a word of three letters or
  more.
- **Rationale**: #291: "When DMC adds a new key, `dmc` shows the keys the record already has".

## R7. The sheet draft

- **Decision**: `dmc records draft <campaign> --sheets` writes `backups/<backup>-sheets-draft.json` beside the backup,
  as part A's draft does (`src/Dmc.Cli/PersonRecords/RecordDraft.cs`): one entry for each labelled line and each
  bullet of a person sheet, with the section, the label, the text, the line, and a source only where the line ends
  with one scene mark or a quotation of the player.
- **Rationale**: #263 § The migration: `dmc` converts what has a regular shape; the model converts free text only
  where no program can.

## R8. What stays Markdown

- **Decision**: `-history.md` archives and `minor-npcs.md`. **ASSUMED**.
- **Rationale**: the resume never reads either (`ReadSet.cs:192`); `minor-npcs.md` holds many people and belongs with
  canon's register, part C (#264).
