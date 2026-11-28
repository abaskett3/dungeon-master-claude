# Feature Specification: The rest of a sheet as facts and free-text lists, with sources

**Feature Branch**: `claude/036-sheet-facts` (spec 036)

**Created**: 2026-11-28

**Status**: Draft

**Input**: Issue #291, part B of #263 (Slips 3.3, part of #242, plan #238): items 2 to 4 of the model agreed with the
player on 2026-11-18, the requirement for full in-world dates from the feedback of 2026-11-25, and the audit moved
here from #264. Part A (#290, spec 032) has merged: its records, sources, write path and index are what this builds
on. Written unattended overnight; every choice the player had not ruled is marked **ASSUMED** and listed under
Clarifications.

## Background

After part A, a person's record (`records/<name>.json`) holds who calls them what, the relationships that name them
and their limits. The rest of a sheet is still free Markdown with no source on a line (#291, #263 § The problem):

- A line often holds several facts with one scene mark at its end, or none. On 2026-11-28 the long campaign's 26
  sheet files were 385,210 bytes with 1,079 scene marks and 60 credits to the player (part A's credit pattern,
  `src/Dmc.Cli/PersonRecords/RecordEdit.cs:25`).
- Nothing stops a second value for one fact: a sheet can give two hair colours in two sections, as five sheets gave
  two Names lines (#237).
- A date is often a day alone. 436 ordinal days in those sheets have no month after them; one of them, an item
  "ready on the 14th", was read as the next day when the 14th was a month past (feedback of 2026-11-25, #263).
- `dmc` already checks one kind of person data, the records, right after each edit
  (`src/Dmc.Cli/PersonRecords/RecordEdit.cs:39`), and the map is protected the same way. A program cannot check prose.

| Part | What it builds | Issue |
|---|---|---|
| A | Rows between persons, the index, the hook lines, limits, the full save's check for rows, migration 010 | #290 (merged) |
| **B (this spec)** | The rest of a sheet as facts and free-text lists, each with a source; full in-world dates; migration 011 | #291 |
| C | `game-state.md`, canon, `dm-notes.md`, the session logs | #264 |

## Clarifications

### Session 2026-11-18 and 2026-11-26 (the player's rulings on #263, before this spec)

- Q: What does a sheet become? → A: Atomic facts (a key and a value with a source; `dmc` refuses a second value for a
  key), keys not a fixed list (`dmc` checks the values of the few keys it computes with, and shows the keys a record
  already has when a new one is added), and free text in lists, one entry one fact with a source (#263 items 2 to 4).
- Q: The format? → A: JSON (#263 item 11).
- Q: The order? → A: Part A, then part B, then `game-state.md` (#264), the session logs last.
- Q: Dates? → A: A fact with a date holds a full in-world date: day, month and year where the calendar has one, never
  a day alone. `dmc` refuses one that lacks one of them (#291, feedback of 2026-11-26).

### Session 2026-11-28 (`/speckit-specify` and `/speckit-clarify`, unattended: ASSUMED answers)

Each answer below was chosen as the one closest to #263's model, because the player was not there to rule. The
player confirms or changes each before the PR.

- Q: Where does the rest of a sheet go? → **Ruled by the player, 2026-11-28** (was ASSUMED): into the person's record from part A, so a person has one
  file and one home for each fact. Two new lists, `facts` and `lists`, and a `sheet` field naming the sheet file the
  record now prints as (`npc-hala-rornsdottir.md`). The migration removes the Markdown sheet. (Closest to #263 item 1:
  all the adventure files move to the model.)
- Q: How do readers that expect a Markdown sheet (the resume, `dmc size`, the hook's sheet lines, `dmc sheet`, the
  check of sample lines) get one? → **Ruled by the player, 2026-11-28** (was ASSUMED): `dmc` prints the record as a sheet in the template's shape (its
  sections and labels), and each reader reads that print where the Markdown file is gone. The templates stay as the
  shape of the print and the names DMC gives keys and lists.
- Q: The play card, which the resume reads alone for a companion? → **ASSUMED**: a view, not a copy. An entry marked
  `"card": true` prints on the card, and not again in its own section. Nothing is held twice.
- Q: Which keys' values does `dmc` check? → **ASSUMED**: the keys `dmc sheet`'s arithmetic computes with
  (`src/Dmc.Cli/Sheets/SheetCheck.cs:244`): the six ability scores (`STR` to `CHA`, a whole number 1 to 30), their
  saves (`STR save` to `CHA save`, a signed number), `Proficiency bonus` (signed, +2 to +9) and `Passive Perception`
  (a whole number). Any other key is stored, sourced and printed as it is.
- Q: How does `dmc` know which facts hold a date, and whether the calendar has a year? → **ASSUMED**: a date is an
  ordinal day (`14th`) in any value or entry text, and in part A's limits and `when` notes. It must have a month
  after it (`the 14th of Eleasis`), or end a range that does (`the 8th to the 10th of Flamerule`). The calendar has a
  year when `game-state.md`'s "In-world date and time" line holds one (a number of three or four digits); then the
  date needs one too. The long campaign's dates have no year ("the 18th of Eleasis"), so there it needs none.
- Q: What counts as "a new key" for the note? → **ASSUMED**: a fact key, or a list name, that the record did not have
  before the edit. The note lists the record's keys and lists, and names any key that shares a word with the new
  one ("Hair colour" and "Hair").
- Q: The `-history.md` files and `minor-npcs.md`? → **ASSUMED**: they stay Markdown in this part. The history files
  are an archive the resume does not read; `minor-npcs.md` holds many people in one file and goes with canon (#264).
  A record's `History` list is the sheet's History section; the full save still moves old entries to the archive.
- Q: A free-text entry checked against a sample line, as part A checks limits? → **ASSUMED**: no new finding class.
  #291 says `dmc` checks the source of a free-text entry and does not interpret the text.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A second value for a fact is refused (Priority: P1)

DMC adds `Hair=black` to a record that already has `Hair=brown`. The edit is refused in one line right after it,
naming the record, the key and the first value, and DMC changes the first fact or puts the record back before it
replies.

**Why this priority**: two values for one fact is how a sheet contradicts itself (#237's five sheets).

**Independent Test**: an edit hook input that adds a fact whose key the record has; `dmc records edited` exits 2
with one line.

**Acceptance Scenarios**:

1. **Given** a record with `Hair=brown`, **When** DMC adds `Hair=black`, **Then** `dmc` refuses it, naming fact 1.
2. **Given** a record with `Hair=brown`, **When** DMC adds `hair=black`, **Then** it is refused too: keys ignore case.

---

### User Story 2 - A date with no month is refused (Priority: P1)

DMC writes "ready on the 14th" in an item's entry. The edit is refused: the date needs its month (and its year
where the calendar has one).

**Why this priority**: it is the slip of 2026-11-25.

**Independent Test**: an edit that adds "ready on the 14th" is refused; "ready on the 14th of Eleasis" stands.

**Acceptance Scenarios**:

1. **Given** a campaign whose in-world date has no year, **When** an entry says "the 14th of Eleasis", **Then** it
   stands.
2. **Given** a campaign whose in-world date is "the 14th of Eleasis, 1492 DR", **When** an entry says "the 14th of
   Eleasis", **Then** it is refused for want of a year.
3. **Given** "the 8th to the 10th of Flamerule", **Then** it stands: the range ends with a month.

---

### User Story 3 - A new key shows the keys already there (Priority: P1)

DMC adds `Hair colour` to a record that has `Hair`. The edit stands, and `dmc` says in one line which keys the
record already has, naming `Hair` as close to it, so DMC can merge them.

**Independent Test**: the note's text on an edit that adds a key.

---

### User Story 4 - Every entry has a source (Priority: P1)

A fact or list entry with no source, or with a scene not in the logs, is refused at once, as part A's rows are.

**Independent Test**: part A's source cases, on a fact and on a list entry.

---

### User Story 5 - The resume and the hook read the same facts as before (Priority: P1)

After the migration, the resume reads each companion's play card and each other sheet it reads today, printed from
the record by `dmc records show`, and the hook's sheet lines and `dmc sheet` read the same print.

**Independent Test**: a fixture person printed from a record gives the same play card fields, sheet lines and export
as the Markdown sheet it came from.

---

### User Story 6 - The migration converts each sheet and accounts for every line (Priority: P1)

Migration 011 moves each person sheet into the person's record: `dmc` drafts what has a regular shape (a labelled
line with one scene mark), the model splits the rest one fact an entry, each with a source; a line with no source
goes to the player; `dmc campaign verify` accounts for each line; the Markdown sheet goes.

**Independent Test**: the golden fixture of migration 011 passes `MigrationGoldenTests`.

---

### User Story 7 - The player reads a person's facts as text (Priority: P2)

`dmc records show <campaign> <person>` prints the facts and lists with their sources after part A's lines. Nobody
reads JSON to check a sheet.

### Edge Cases

- **A campaign before standard 11** has records with no facts or lists, or no records at all: everything reads the
  Markdown sheets as today.
- **Mid-migration**, a person may have both a Markdown sheet and a record with facts. The print is used only when the
  Markdown file is gone; the full save's check lists the pair as a finding (`sheet-and-record`).
- **A fact's value that is a number** in the JSON (`"value": 13`) is taken as its text.
- **"The 3rd floor"** is read as a date with no month and refused; DMC writes "the third floor". The pattern is
  checked against the long campaign before the build (436 hits, almost all real dates or ranges).
- **A table** (the PC's ability table, the attacks table) prints from facts and a list: the ability table from the
  six score facts and their save facts, its modifiers worked out; the attacks table from the `Attacks` list of the
  `Combat` section, one row an entry, its cells separated by `|`.
- **A person with no sheet** (part A's Vaelorn) keeps a record with no `sheet`, as now.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 (facts)**: A record may hold `facts`, each `{ key, value, source }`, with an optional `section` (the sheet
  heading it prints under; none for the opening fields) and `card` (true to print on the play card). A key is any
  text. `dmc` refuses a second fact with the same key on a record, ignoring case and spaces at the ends.
- **FR-002 (checked keys)**: `dmc` checks the values of the keys it computes with: `STR`, `DEX`, `CON`, `INT`, `WIS`,
  `CHA` (a whole number 1 to 30), `STR save` to `CHA save` (a signed number), `Proficiency bonus` (+2 to +9) and
  `Passive Perception` (a whole number), each value starting with the number. Any other key is stored, sourced and
  printed as it is.
- **FR-003 (lists)**: A record may hold `lists`, each entry `{ list, text, source }` with the same optional `section`
  and `card`. One entry is one fact. `dmc` checks an entry's source and its dates, never what its text says.
- **FR-004 (sources)**: A fact and a list entry take part A's sources (spec 032 FR-003): a scene in the logs, the
  player's words quoted, `setup` or `inference`. The check after each edit refuses one with none, or with a scene the
  logs do not have, and checks a new quotation of the player against the session's messages, as for rows.
- **FR-005 (full dates)**: An ordinal day (`14th`, `1st`) in a fact's value, a list entry, and, on a record that holds
  a sheet, a limit's text or a row's `when` (so a standard 10 record is checked as before) must have a month after it (`of Eleasis` or `Eleasis`), or end a range that does (`the 8th to the 10th of
  Flamerule`, `the 5th and 6th of Kythorn`). When `game-state.md`'s "In-world date and time" line holds a year, each
  such date needs a year after its month too. `dmc` refuses the record in one line that names the entry and the day.
- **FR-006 (the keys note)**: When an edit adds a fact key or a list name the record did not have, the edit stands
  and `dmc` adds one line to DMC's context: the record's other keys and lists, and any that shares a word with the
  new one.
- **FR-007 (sheet)**: A record with facts or lists names `sheet`: the file name it prints as, `pc-<name>.md` or
  `npc-<name>.md`, which says which template's shape it prints in and names its `-history.md` archive. `dmc` refuses
  a record with facts or lists and no `sheet`, a `sheet` that is not `pc-…md` or `npc-…md`, and two records with one
  `sheet`.
- **FR-008 (the print)**: `dmc` prints a record as a sheet in its template's shape: the `# ` title, the opening
  fields, `## Play card` with the card entries (when there are any), then each section in the template's order and
  then the others, with History last. A fact prints as `- **Key**: value`; the entries of a list named as its
  section print one bullet each; the entries of another list print on one line, `- **List**: …`. Each entry ends
  with its source in brackets, except in the print for programs (no sources) and in table rows. The ability table and
  the attacks table print as Edge Cases says.
- **FR-009 (`dmc records show`)**: `show` prints, after part A's lines, the facts and lists with their sources.
  `--card` prints the record's sheet to the end of its play card; `--sheet` prints it to the History heading (what the
  resume reads of a sheet today); `--all` prints all of it. Each with sources.
- **FR-010 (readers)**: Where a person's Markdown sheet is gone and their record names it as `sheet`, these read the
  print without sources in its place: the resume (through `dmc records show --card` or `--sheet`), `dmc size` (its
  read set, with the play-card budget on the card without sources, #262), the hook's sheet lines, `dmc sheet`, and
  `dmc records check`'s sample lines.
- **FR-011 (full-save check)**: `dmc records check` adds one class, `sheet-and-record`: a Markdown sheet that still
  exists beside a record that prints it.
- **FR-012 (draft)**: `dmc records draft <campaign> --sheets` writes a second draft beside the latest migration
  backup: one entry for each labelled line or bullet of each person sheet, `{ to, list or key, text, section, source,
  line }`, the source filled in only when the line ends with exactly one scene mark or quotes the player, else null.
  It never writes in the campaign. It refuses when the draft exists.
- **FR-013 (migration 011)**: A new migration, `kit/migrations/011-sheet-facts.md`, standard 11, in the shape of
  migration 010: the draft, the model splitting each line into one fact an entry, the player ruling on each entry
  with no source (kept on their words, given a scene, or dropped, never `inference`), each record written with Write,
  each sheet removed, each line accounted for by `dmc campaign verify`. It never runs while the campaign is in play.
  Its shape check, `sheets-in-records`, fails while a person sheet remains in `character-sheets/` (not
  `minor-npcs.md` or a `-history.md` file) or a record breaks a rule.
- **FR-014 (kit)**: `records-format.md` shows facts and lists with a worked example of each; the character-sheet
  update, resume, full save and new-campaign skills read and write records for a campaign at standard 11;
  `kit/dm-rules.md` does not grow.
- **FR-015 (documents)**: `.docs/campaign-files.md`, `.docs/architecture.md`, `.docs/dmc-reference.md`,
  `.docs/roadmap.md` and `.docs/decisions.md` change in the same PR.

### Key Entities

- **Fact**: a key, a value and a source, with an optional section and card mark.
- **List entry**: a list's name, one fact's free text and a source, with an optional section and card mark.
- **Print**: a record as a Markdown sheet in its template's shape, with or without sources.
- **Sheet draft**: the migration's proposed entries, each pointing to the sheet line it came from.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every rule of FR-001 to FR-007 refuses its case in one line through `dmc records edited`, and lets the
  good case through, in the contract tests.
- **SC-002**: The date rule's false refusals on the long campaign's sheets, counted by hand on a sample of 50 of its
  hits before the migration, are 10% or fewer.
- **SC-003 (the outcome)**: With the slip test, on migrated and audited data against the present files, fact slips
  about people per reply decrease by 1/3 or more with a one-sided permutation p below 0.05. The pass line, fork
  points and runs are fixed on #291 before any run (draft in the overnight notes). Not run in this spec's first
  session.
- **SC-004**: The resume's tokens for the long campaign, and an average play turn's hook output, after the migration
  against before (constitution Core 3), measured with `dmc size` and the hook's output over the play logs.
- **SC-005**: Migration 011 on a copy of the long campaign passes `dmc campaign verify`, every line accounted for and
  every entry with no source put to the player.

## Assumptions

- Part A's migration (010) runs before this one; the long campaign has no `records/` yet (2026-11-28).
- The record stays one file a person; the facts make it larger (Hala's sheet is about 30,000 bytes), and Edit
  changes one line of it, so a write reads the record once as now.
- The print is built on each run and never stored, as part A's index is.
- `dmc` cannot know if a fact or an entry is true. It knows that it has a source and a full date.

## Out of Scope

- The audit of the sheets (#291 § The work for the sheets), the migration of the long campaign and the slip test:
  they need the player and run out of play.
- `game-state.md`, canon, `minor-npcs.md`, `dm-notes.md` and the session logs (#264).
- A check of what a free-text entry says.
- A growth of `kit/dm-rules.md`.
