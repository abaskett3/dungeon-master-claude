# Data model: facts and lists on a person's record (spec 036)

The file format in full is [contracts/records-file.md](contracts/records-file.md). Part A's fields and rules
(`specs/032-who-calls-whom/data-model.md`) are unchanged; this page gives what is added.

## Record (added fields)

| Field | Type | Rule |
|---|---|---|
| `sheet` | string, optional | The sheet file the record prints as: `pc-<name>.md` or `npc-<name>.md`. Required when `facts` or `lists` has an entry. Unique in the campaign. |
| `facts` | list of Fact | One value a key. |
| `lists` | list of List entry | Free text, one fact an entry. |

## Fact

| Field | Type | Rule |
|---|---|---|
| `key` | string | Required, not blank. Unique on the record, ignoring case and spaces at the ends. |
| `value` | string or number | Required, not blank. A checked key's value starts with its number (below). Full dates. |
| `section` | string, optional | The sheet heading it prints under, without `## `; none for the opening fields. |
| `card` | true, optional | Prints on the play card instead of its section. |
| `source` | Source | Required (part A's sources). |

**Checked keys** (ignoring case): `STR`, `DEX`, `CON`, `INT`, `WIS`, `CHA`: a whole number 1 to 30.
`STR save` to `CHA save`: a signed number. `Proficiency bonus`: +2 to +9. `Passive Perception`: a whole number.

## List entry

| Field | Type | Rule |
|---|---|---|
| `list` | string | Required, not blank: the list's name (`Looks`, `What they know`, `History`). |
| `text` | string | Required, not blank. One fact. Full dates. |
| `section`, `card`, `source` | | As for a fact. |

## Full dates (rule F)

In a fact's value, an entry's text, a limit's text and a row's `when`: each ordinal day (`1st`, `14th`) is followed by
a month (a capitalised word, with an optional `of`), or by `to`, `and`, `or`, `-` or `–` and another ordinal day that
is. When `game-state.md`'s "In-world date and time" line holds a number of three or four digits, each date also has
such a number within the next three words after its month.

## Rules the check adds (refusals)

8. A fact has a key, a value and a source; a list entry has a list, a text and a source; `section` is text, `card` is
   true or false; no other field.
9. No two facts of a record have one key.
10. A checked key's value is in its range.
11. Rule F.
12. A record with a fact or an entry has a `sheet` of the form `pc-….md` or `npc-….md`; no two records have one
    `sheet`.

**A note, not a refusal**: an edit that adds a key or a list name the record did not have.

## Finding (added class)

| Class | Input | Finding |
|---|---|---|
| `sheet-and-record` | records | A Markdown sheet in `character-sheets/` that a record also prints (`sheet`). |

## Print

Built on each run, never stored. Lines, in order:

1. `# <person>`.
2. The opening fields: facts and lists with no `section` and no `card`.
3. `## Play card`, when an entry has `card`: those entries, facts and lists in record order.
4. Each other section: the template's sections in its order (`src/Dmc.Cli/Sheets/SheetLayouts.cs`), then any other
   in record order, `History` last. In each: the facts, then each list. A list named as its section prints one bullet
   an entry; another list prints `- **List**: ` and its entries joined by spaces.
5. In `Abilities`, the six score facts and their saves print as the ability table (Score, Modifier worked out, Save);
   in `Combat`, the `Attacks` list prints as `### Attacks` and a table, one row an entry.

Each entry ends with ` (<source>)` in the print with sources; table rows never do.

## Sheet draft (the migration's)

`backups/<backup>-sheets-draft.json`: a list of `{ to, section, label, text, source, line }`, where `label` is the
line's bold label or null for a bullet with none, `source` is filled only for a line that ends with one scene mark or
quotes the player, and `line` is `character-sheets/<file>:<n>`. Never read in play.

## State and lifecycle

- Standard 10: records with rows and limits; sheets are Markdown.
- Standard 11: each person sheet's lines are facts and lists on its record, which names it as `sheet`; the Markdown
  sheet is gone; `-history.md` and `minor-npcs.md` stay.
