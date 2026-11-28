# Feature Specification: Who calls whom what, and limits, held by `dmc`

**Feature Branch**: `claude/032-who-calls-whom` (spec 032)

**Created**: 2026-11-26

**Status**: Draft

**Input**: Issue #290, part A of #263 (Slips 3.3, part of #242, plan #238), from the maintainer's spec draft for #263
and the rulings of 2026-11-26 on #263: the four open points (D8), the result of the write-path test, and SC-003's
pass line. #270, the gate, passed: correct facts next to the reply cut fact slips about people by 53% over 117
replays for each arm.

## Background

The words people use for each other, and what a person cannot do, are lines of free text with no speaker and no
source, in more than one place (#263):

- A sheet holds "Names they use" twice, in the play card and under "Playing them" (`kit/templates/npc-sheet.md:16`,
  `:28`). Five sheets of the long campaign hold two that disagree.
- Canon's People register lists "other names and role words" after a person, with no place for who says them
  (`kit/templates/campaign/story/canon.md:15`). The long campaign lists *Nytha* under Shale alone, but *Nytha*
  means Shale to Kibil, Tam to Hala, and Rorn to Vaelorn and Miira.
- The context hook matches the player's message against that register (`src/Dmc.Cli/Retrieval/Register.cs:149`). It
  resolves a word to one person or to nobody, and it drops a title or kin word that stands alone
  (`Register.cs:88`). On 2026-11-18 it gave the wrong person or nobody for all three of #263's probes.
- A program cannot check prose: #237's first names check gave 57% false findings. The map, the one campaign file that
  is JSON, is the one a program protects: `dmc` refused 14 of 484 hand changes with a one-line error.

Part A moves these words, the relationships between persons, and each person's limits into one record per person
that `dmc` checks, indexes and prints. The rest of a sheet stays Markdown until part B (#291).

| Part | What it builds | Issue |
|---|---|---|
| **A (this spec)** | Rows between persons (relationships, usages, names), the index over them, the hook lines from them, the limits list, the check at the full save for rows, and the migration of the Names lines and canon's People register | #290 |
| B | The rest of a sheet as facts and free-text lists, with full in-world dates | #291 |
| C | The other adventure files (`game-state.md`, canon, `dm-notes.md`, the session logs) | #264 |

## Clarifications

### Session 2026-11-26 (the player's rulings on #263, before this spec)

- Q: The forms of a name? → A: A set of forms, each with a short note on when it is used. DMC chooses which form to
  use; the data holds no rules.
- Q: The order the files move? → A: Part A, then part B (#291), then `game-state.md` (#264), then the session logs
  last.
- Q: The label of the list on a sheet? → A: "Called".
- Q: How does DMC write rows? → A: With Edit on the record's JSON, checked by `dmc` right after each edit, as the
  write-path test on #263 decided: arm B (Edit with `dmc` checking) kept 0 wrong rows, arm A (`dmc` commands only)
  kept 4, and B used 30% more tokens. Arm A's one error (a usage written as a relationship) may have come from its
  instructions having no worked example, so the write reference shows one of each kind. `kit/dm-rules.md:63` ("change
  them with Edit") stays as it is.
- Q: Full in-world dates, and the PC's place after a time jump (#263's feedback comment of 2026-11-26)? → A: Out of
  part A. Dates go to #291 and #264, the PC's place to #264. Until then both stay on #242's list of known wrong lines.

### Session 2026-11-26 (`/speckit-specify`)

- Q: Where does the record of a person of a closed earlier campaign live ("Mother")? → A: In the later campaign that
  names them, sourced to the earlier campaign's scenes. The closed campaign is never changed.
- Q: How is "a note crediting the player, added in the same change" checked, with no history in campaign folders? →
  A: At the edit, by the check that sees the old and new text; and a source quoting the player must be in the
  player's own messages in the session's log.
- Q: What does "a sample that keeps a fact the record has since changed" check in part A? → A: The sample against the
  rows and the key words of the limits. Other facts are part B's.

### Session 2026-11-26 (`/speckit-clarify`)

- Q: How often does the hook print a person's rows and limits? → A: Once per scene for each person present or named;
  a change in who is present starts a new scene. A row whose word is in the message prints each time the word
  appears.
- Q: Where do the parts of a Names line about places and things go, when rows hold only persons? → A: They stay on
  the sheet in one line, "Names for places and things"; only the parts about persons move to rows.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The hook gives each speaker's word for a person (Priority: P1)

The player writes "Hala runs to Nytha." The hook prints that, to Hala, *Nytha* is Tam. It does not print Shale or
Rorn as the one meant.

**Why this priority**: it is the path of the slip of 2026-11-17, and of two more like it since (#238).

**Independent Test**: #263's three probes, run through `dmc context-check` on a migrated copy of the long campaign.

**Acceptance Scenarios**:

1. **Given** the migrated copy, **When** the message is "Hala runs to Nytha.", **Then** the hook names Tam as Hala's
   *Nytha*.
2. **Given** the migrated copy, **When** the message is "Kibil asks Auntie about it.", **Then** the hook names Kole,
   with her note that "Auntie" is for family only.
3. **Given** the migrated copy, **When** the message is "What do we know about Mother?", **Then** the hook names the
   hag of the earlier campaign.

---

### User Story 2 - An ambiguous word prints every candidate (Priority: P1)

When the message does not say who speaks, the hook prints each person the word can mean and who uses it for them.
It never picks one.

**Independent Test**: "Nytha?" with no speaker named prints all three rows (Shale to Kibil, Tam to Hala, Rorn to
Vaelorn and Miira).

**Acceptance Scenarios**:

1. **Given** a word that means different persons to different speakers, **When** the message holds the word and no
   speaker, **Then** every row with that word is printed.
2. **Given** a word only the data makes ambiguous ("Auntie": Kole to family, Hush's own word for herself), **When**
   the message holds it alone, **Then** both rows are printed. A word no row holds is not printed, whatever word it
   is.

---

### User Story 3 - A person's limits are in front of DMC (Priority: P1)

For each person present or named, the hook prints their limits, such as Hala's "Cannot fly yet. Was shown the glide;
cannot land one."

**Independent Test**: a scene where Hala is present prints her limits once in that scene, and again once the Present
line changes and she is present or named.

**Acceptance Scenarios**:

1. **Given** Hala on the Present line, **When** the first message of the scene arrives, **Then** her limits are
   printed.
2. **Given** her limits printed earlier in the same scene, **When** the next message does not name her, **Then**
   they are not printed again.

---

### User Story 4 - A bad row is refused at once (Priority: P1)

DMC adds a usage with no speaker, a row with no source, a scene that is not in the logs, or a kind that does not
exist. The edit is refused in one line right after it, and DMC fixes the record or puts it back before it replies,
as the map's check works.

**Independent Test**: each of the bad changes of the write-path test (#263) is refused with one line that names the
record, the row and what is wrong.

**Acceptance Scenarios**:

1. **Given** a record, **When** DMC adds a usage with no `from`, **Then** `dmc` refuses it with one line and DMC's
   next step is to fix or undo it.
2. **Given** a usage that makes one word mean two persons to the same speaker, **When** DMC adds it, **Then** the
   edit stands and `dmc` says so in one line, and the hook prints both from then on.

---

### User Story 5 - The migration keeps every line and asks about each conflict (Priority: P1)

The migration moves what each Names line (both homes on a sheet) and each People `(also: …)` item says about persons
into rows on the record of the person who is named; what it says about places and things stays on the sheet. `dmc campaign verify` accounts for each line that goes. Each conflict goes to the
player; nothing is assumed.

**Independent Test**: on a copy of the long campaign, `dmc campaign verify` passes, every conflict was put to the
player, and #237's rulings are the first data.

**Acceptance Scenarios**:

1. **Given** two Names lines on one sheet that disagree, **When** the migration reaches them, **Then** the player
   rules which rows are written, and both lines are accounted for.
2. **Given** Hala's sample line, **When** the migration runs, **Then** it becomes *"Wuv you, Nytha."* (to Tam) in
   both places on her sheet, and the note that credits the player is removed (#237).

---

### User Story 6 - The player reads a record as text (Priority: P2)

The player, or DMC at a resume, asks for a person's record and gets it as text: what they are called and by whom,
their relationships, the words they use for others, their limits, each with its source. Nobody reads raw JSON to
check a sheet.

**Independent Test**: printed records for Hala, Tam and Kole on the migrated copy, checked by the player (step 7 of
the build).

---

### User Story 7 - The full save finds the data errors (Priority: P2)

At a full save, `dmc` lists the findings of fixed classes in the records and the sheets' sample lines, so DMC
corrects them, or asks the player, before they reach play.

**Independent Test**: SC-002.

### Edge Cases

- **A person with no sheet** (Vaelorn and Miira) gets a record, so rows can name them.
- **A person of a linked earlier campaign** ("Mother", the hag of the campaign this one continues). That campaign is
  closed, and a closed campaign is never migrated (`kit/skills/dmc-migrate-campaign/SKILL.md:25`). Their record lives
  in the later campaign that names them, sourced to the earlier campaign's scenes (`the-silver-egg s8`). The closed
  campaign is never changed.
- **A campaign before standard 10** has no records: the hook prints what it prints today, and nothing fails.
- **A record that is not valid JSON**: the check after the edit refuses it, naming the file and line. The context
  hook never fails because of it and leaves that record out.
- **A speaker who is a group** ("her crew"), or anyone: a row says so. "anyone" is a name, not a usage.
- **A word the speaker never says** (Kibil never calls Hush "Auntie"): stored as `never` on the row, and printed with
  it. Only `never` is hard; the forms are not a closed list.
- **A Names line that names places or things too** ("never calls the keep 'the castle'"): the parts about persons
  move to rows; the rest stays on the sheet as "Names for places and things".
- **A person renamed or a word respelled** (the player rules "Emmajean" or "Emmajeen"): an Edit of the rows that hold
  it; the check refuses a row that names a person with no record.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 (records and rows)**: Each person named in a row, in a limit, or with a sheet has one record. A row has one
  shape for three kinds:
  - who says the word (`from`: a person, a group, or anyone);
  - the word;
  - the person it means (the record it is on);
  - the kind: `usage` (a person or group says it), `name` (anyone says it) or `relationship` (what the person is to
    the speaker: Dessa is Tam's *mother*);
  - an optional note on when it is used, an optional `never` list of who never says it, and a source.

  A form of a name is its own row with its own note (Kibil's eight words for Kole are eight rows). DMC chooses among
  them; the data holds no rule for choosing. A relationship is stored once, on the record of the person it names,
  and shown from both sides. The list of usages and names is labelled "Called" wherever it is shown.
- **FR-002 (no derivation)**: A usage is never derived from a relationship. Hala calls Tam *Nytha*, and Tam is not
  her mother.
- **FR-003 (sources)**: Each row and each limit has a source:
  - a scene that is in the session logs (`s40.2`), or in a linked earlier campaign's (`the-silver-egg s8`);
  - the player's words as a quotation;
  - the campaign setup;
  - or DMC's inference (#261).

  `dmc` refuses an entry with no source, or with a scene the logs do not have.
- **FR-004 (index)**: `dmc` builds one index of the rows each time it runs, over this campaign and the campaigns it
  continues. It has two views: by word (*Nytha*: Shale to Kibil, Tam to Hala, Rorn to Vaelorn and Miira) and by
  speaker (Hala's words for each person). It has no fixed list of titles or kin words: a word is ambiguous only when
  the data says so. It replaces `Register.cs`'s rule that drops a bare title from an `(also: …)` list, and the
  canon reminder finds the people a message names through it.
- **FR-005 (hook lines)**: `dmc context-check` prints:
  - every row whose word is in the player's message, and every candidate when the message does not say who speaks;
  - the rows between the persons present, and those of each person the message names;
  - each such person's limits.

  A person's rows and limits are printed once per scene, for each person present or named: a change in who is present
  (the Present line) starts a new scene. A row whose word is in the message is printed each time the word appears,
  so an ambiguous word is never left unresolved. The hook never fails or blocks a message. Its extra tokens per average play turn are measured against
  the present output. Its whole output stays within 9,500 bytes, below the size over which Claude Code passes a hook's
  output only as a 2 KB preview: the one-line parts and the rows first, then canon, then the sheet lines; a part that
  runs short leaves a whole person out (found by SC-003's pilot, 2026-11-27).
- **FR-006 (writing)**: DMC changes a record with Edit on its JSON file, and creates one with Write. Right after each
  edit of a record, `dmc` checks every record of the campaign. On a bad one it refuses in one line (the file, the
  row, what is wrong) and DMC fixes or undoes the change before it replies. A usage that makes one word mean two
  persons to the same speaker stands, with one line that says so. The write reference that DMC follows shows a
  worked example of each kind. `kit/dm-rules.md:63` is unchanged.
- **FR-007 (check at the full save)**: At a full save, `dmc` lists findings in these classes, fixed before the
  first run:
  - a usage with no speaker;
  - a sample line that breaks its speaker's own rows (a word for the person it addresses that the speaker never
    uses, or a word that means another person to them);
  - two rows that disagree (a speaker on a word's `never` list who also has a usage of it for that person);
  - a note crediting the player, added in the same change that changed the text. Campaign folders keep no history,
    so this is checked at the edit: the check after each edit sees the old and new text, and flags an edit that both
    changes a line and adds a credit to the player. Also, a source that quotes the player must be found in the
    player's own messages in the session's log;
  - a sample that keeps a fact the record has since changed: in part A, a sample line checked against the rows (a
    word the speaker no longer uses) and the key words of the limits ("fly" against "Cannot fly yet"). Other facts
    are part B's (#291). Hala's flying sample is fixed in the migration by the player's ruling (#237).

  A word that means different persons to different speakers is not a finding. A sample line names whom it addresses,
  as *"Wuv you, Nytha."* (to Tam) does, so the check can read it.

  The migration's draft (FR-008) has three more classes, because its rows come before the player rules:
  - a word that names two persons where at least one of its rows has no speaker (it cannot be resolved without one:
    "Auntie" for Kole and for Hush);
  - two Names lines of one speaker that give different rows for one person (the five sheets of #237);
  - a row whose line gives no source (no scene, no quotation of the player, not the setup), so the player rules it
    and DMC never fills in `inference` for it.
- **FR-008 (migration, standard 10)**: A new migration, `kit/migrations/010-…`, with steps in the shape of migration
  006:
  - it moves what each Names line (both homes on a sheet) and each People `(also: …)` item says about persons into
    rows on the named person's record, and removes it from the sheet and canon, so the rows are its one home;
  - what a Names line says about places and things stays on the sheet, in one line, "Names for places and things"
    (on the play card when the sheet has one, else under "Playing them"), until part B (#291);
  - it first writes a draft of every proposed row, beside the migration's backup and never in the campaign: `dmc`
    converts what has a regular shape, and the model converts free text only where no program can, each draft row
    pointing to the line it came from;
  - `dmc` lists the draft's findings (FR-007), and every one goes to the player; a relationship the files do not
    state is not written;
  - the records are then written from the draft and the player's rulings, and `dmc campaign verify` accounts for
    each line that goes;
  - it never runs while a campaign is in play;
  - #237's rulings are the first data, including Hala's sample line becoming *"Wuv you, Nytha."* (to Tam) in both
    places, with the note crediting the player removed. DMC puts each of them to the player to confirm in the
    migration's session, and a row sourced to the player quotes those words, so the check of each quotation (FR-007)
    holds without an exception.

  It names at least one shape check that fails on a campaign left in the old shape.
- **FR-009 (the text view)**: `dmc` prints a record as text: Called (word, who says it, when, never), relationships
  from both sides, the words the person uses for others, and limits, each with its source. `dmc sheet` (#191) shows
  the limits from the record, not its Called list: a Called list may hold a name the PC has not heard, as the old
  "Names they use" could (the player's ruling, 2026-11-27).
- **FR-010 (resume and full save)**: The resume reads the text view of the records of the persons in the party, in
  place of the play card's Names line. The full save runs the check of FR-007 and acts on its findings.
- **FR-011 (documents)**: These change in the same PR:
  - `.docs/product-overview.md:9` and `README.md:3` ("plain Markdown files"), with a decision record in
    `.docs/decisions.md`;
  - `kit/dm-rules.md:63`, only if a word must change, with no net growth (the file is at its byte cap);
  - `.docs/campaign-files.md`, `.docs/architecture.md`, `.docs/roadmap.md`;
  - the sheet and canon templates;
  - the resume and full-save skills.

### Key Entities

- **Record**: one person. Their Called list, the relationships that name them, and their limits.
- **Row**: from (a person, a group, or anyone), a word, to (the record's person), a kind (`usage`, `name`,
  `relationship`), an optional note on when, an optional `never` list, and a source.
- **Limit**: what the person cannot do, or where they differ from the usual, with a source.
- **Source**: a scene in the logs, the player's words as a quotation, the setup, or DMC's inference.
- **Index**: every row of this campaign and the campaigns it continues, by word and by speaker, built each time
  `dmc` runs.
- **Finding**: one result of the full-save check, in one of FR-007's classes, with the record or sheet line it is on.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a migrated copy of the long campaign, #263's three probes give the correct persons (Tam; Kole,
  family only; the hag of the earlier campaign), and "Nytha?" with no speaker prints all three of its rows.
- **SC-002**: The full-save check finds the known cases: the People line with no speaker, "Tam" in Hala's sample
  line, the five sheets with two Names lines that disagree, and "Auntie" for two persons. On a hand check, 20% or
  fewer of its findings are false. Errors planted in a copy measure how many it misses.
- **SC-003 (the outcome)**: With the slip test in fresh mode, on migrated data against the present files, the share
  of the risk speakers' quoted lines that use a word the speaker never says for the person meant decreases by 1/3 or
  more, with a one-sided permutation p below 0.05. The pass line, the fork points, the runs and the key are fixed on
  #263 (comment of 2026-11-26).
- **SC-004**: The hook's extra tokens per average play turn, against the present output, stay within #241's limit
  of 1,000.
- **SC-005**: The migration of the copy passes `dmc campaign verify`, with every line that went accounted for and
  every conflict put to the player.

## Assumptions

- The records are JSON. `dmc` already reads and writes JSON in its native build (the map, the manifest) and has no
  YAML library.
- The check after each edit follows the map's pattern, but runs right after the edit rather than when DMC stops, so
  DMC sees a refusal before it replies (the write-path test, #263).
- The hook knows who is present from the scene's Present line and the Combat tracker, as the sheet lines do now
  (`src/Dmc.Cli/Retrieval/SheetReminder.cs:98`).
- The long campaign is at standard 8, so its copy is brought to standard 9 (wardrobe) before 10.
- The migration needs a campaign that is not in play. It runs first on a copy of the long campaign, never the live
  one.
- `dmc` cannot know if a row is true. It knows that it has a source and a speaker.

## Out of Scope

- Part B (#291): the rest of a sheet as facts and lists, and full in-world dates.
- `game-state.md` and the PC's place after a time jump (#264).
- A check of which form DMC used in a reply. It needs to know who speaks each quoted line, and #237's check was
  right on 4 of 41 flags.
- Any check that free text is true.
- A growth of `kit/dm-rules.md`.
