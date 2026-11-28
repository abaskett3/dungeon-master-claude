# Data model: Who calls whom what, and limits (spec 032)

The file format in full is [contracts/records-file.md](contracts/records-file.md). This page gives the entities and
the rules `dmc` holds them to.

## Record

One person. File `adventures/<campaign>/records/<slug>.json`, where the slug is the person's name in lower case with
each run of characters that are not letters, their marks or digits a hyphen, letters outside ASCII kept (T063; the name it had before, ASCII letters and digits only, still passes, T069)
("Captain Emmajeen Kole" → `captain-emmajeen-kole.json`, "Éla" → `éla.json`).

| Field | Type | Rule |
|---|---|---|
| `person` | string | The person's full name, as their sheet's `# ` title when they have one. Unique in the campaign, ignoring case. |
| `of` | string, optional | The linked earlier campaign the person belongs to (`the-silver-egg`), when the record is kept for them in a later campaign. |
| `rows` | list of Row | The rows that name this person: their Called list and the relationships. |
| `limits` | list of Limit | What they cannot do, or where they differ from the usual. |

No other field. A record with no rows and no limits is allowed (a new person).

## Row

| Field | Type | Rule |
|---|---|---|
| `kind` | `usage`, `name` or `relationship` | Required. |
| `from` | string | Required. A person with a record (full name), `anyone`, or `group: <who>`. |
| `word` | string | Required, not blank. |
| `when` | string, optional | A short note on when this form is used. |
| `never` | list of string, optional | Persons (full names) or `group: …` who never say this word for this person. |
| `source` | Source | Required. |

**Kind and speaker**: `name` ⇔ `from` is `anyone`. `usage` is from a person or a group. `relationship` is from a
person: the record's person is that to the speaker (on Dessa's record, from Tam, word `mother`).

**One row per form**: Kibil's eight words for Kole are eight rows on Kole's record, each with its own `when`.

## Limit

| Field | Type | Rule |
|---|---|---|
| `text` | string | Required. One fact. |
| `source` | Source | Required. |

## Source

One of: `sN` or `sN.M` (a scene of this campaign); `<campaign> sN[.M]` (a scene of a linked earlier campaign);
`player: "<the player's words>"`; `setup`; `inference`. A scene must exist in that campaign's session logs (research
R2): a real `### Scene M:` heading of `session-NNN.md`, not the template's commented one (T061), or, in the check after
each edit only, the scene in play, the one after the last heading of this campaign's latest session log, whose block
is written when it ends (T064, T067).

## Rules the check holds (refusals)

Checked over every record of the campaign, after each edit and at `dmc records check`:

1. The file is a JSON object with only `person`, `of`, `rows` and `limits`; a row or limit has only its fields.
2. The file name is the person's slug; no two records name the same person.
3. Each row has a valid kind, a word, a speaker that fits the kind, and a source (Source above).
4. A speaker or `never` entry is a person with a record here or in a campaign this one continues (T060), `anyone`
   (speaker only) or `group: …`. After an edit, a person with a sheet (not `minor-npcs.md` or a history file) and no record yet is a
   note on the record edited, not a refusal, so two records that name each other can be written one at a time (T059,
   T068, T071); `dmc records check` refuses it. Migration 010's `records-in-place`, which checks shape alone, takes
   any speaker a row names (T070).
5. No row repeats another on the same record (same kind, speaker and word, ignoring case).
6. No row's speaker is on the `never` list of a row with the same word on the same record.
7. Each limit has text and a source.

**A note, not a refusal**: a usage that makes one word mean two persons to the same speaker.

## Index

Built on each run, never stored (research R4). Rows of the active campaign and of each campaign it continues that has
`records/`.

- **By word**: lower-case word → the rows with it (speaker, person, kind, when, never, source).
- **By speaker**: speaker → (person, word) pairs.
- **Names**: the words of `name` rows → their person, for matching the Present line and the message.

## Finding (`dmc records check`)

One line: the class, the file and row or sheet line, and what is wrong. Classes (FR-007):

| Class | Input | Finding |
|---|---|---|
| `no-speaker` | draft | A row with no speaker (a People `(also: …)` item). |
| `sample-breaks-rows` | records, draft | A quoted sample on the speaker's sheet uses, for the person it addresses `(to X)`, a word the speaker never says or a word that means another person to them; or a word on the speaker's `never` list. |
| `rows-disagree` | records, draft | A speaker on a word's `never` list who also has a usage of it for that person; in the draft, two rows of one speaker with one word for two persons from two sheet lines. |
| `credit-in-edit` | the edit (hook) | An edit that changes a line and adds a credit to the player in the same edit; a `player:` quotation not in the player's messages. |
| `sample-stale` | records | A sample with a word the speaker's rows no longer hold, or with a key word of one of the speaker's limits ("fly" against "Cannot fly yet"). |
| `ambiguous-no-speaker` | draft | A word that names two persons where at least one of its rows has no speaker. |
| `names-lines-differ` | draft | Two sheet lines of one speaker that give different rows for one person (rows from one line missing from the other). |
| `no-source` | draft | A row whose line gives no scene, no quotation of the player and is not the setup. |

A word that means different persons to different speakers, all of them known, is never a finding.

## Draft (the migration's)

A companion file of the migration's backup, `backups/<backup>-records-draft.json`: a list of rows, each with its
record's person and an extra `line` (the file and line it came from). Rows may break rules 3 to 6; `dmc records
check --draft` lists them as findings. Never read in play.

## State and lifecycle

- A campaign at standard 10 has `records/`; one before has none, and every part of `dmc` behaves as before for it.
- A record is created by Write and changed by Edit (FR-006); `dmc` never writes a record itself.
- The sheets keep their other lines; "Names they use" becomes "Names for places and things" (one line). Canon's People
  entries keep their bold name and lose `(also: …)`.
