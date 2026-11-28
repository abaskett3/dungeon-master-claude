---
standard: 11
title: The rest of each sheet moves into its person's record, as facts and lists with sources
shipped: spec 036
touches: [character-sheets/, records/]
drops: []
checks: [sheets-in-records]
---

# The rest of each sheet

## What changes

Each person's sheet moves into their record, `records/<name>.json`: each fact (a hair colour, an armour class, a
status) becomes one key and value, and everything else (looks, voice, what they know, history) one entry a fact in a
list, each with where it comes from. `dmc` then refuses a second value for one fact, a fact with no source, and a date
that gives a day with no month. The sheets read the same as before: `dmc records show` prints them. You rule on every
line that says no source; nothing is assumed. The history files and the minor NPCs file stay as they are.

## Before the backup

Tell the player this migration reads every sheet and rewrites it, and that it takes a long time on a big campaign: on
one with many long sheets it is several sittings, one or two people at a time, out of play. Ask whether to go on now.
On a no, stop: nothing has changed.

## Steps

Read `records-format.md` of the `dmc-character-sheet-update` skill for the record's shape before you start. Every step
first checks whether it is needed, so a campaign already in shape is left as it is.

1. **The draft.** Run `dmc records draft <campaign> --sheets`. It writes one entry for each line of each sheet in
   `character-sheets/` (not `minor-npcs.md` or a `-history.md` file) beside the backup the migration just made, with
   the line it came from, and a source only where the line cites one scene. If it says the draft exists, use it.
2. **One person at a time.** Take one sheet and its entries; finish it (steps 3 to 6) before the next. Start with the
   companions, then the person sheets the player cares most about.
3. **Split.** For each entry, write what the line says as facts and list entries in the person's record, one fact
   each: a line with one value (a status, an AC, a score) is a fact, its label the key; a line of description, or
   several facts on one line, becomes one list entry for each fact, the label as the list's name. Keep the section it
   was in (`section`), and mark what was on the play card `"card": true`. Copy the words; never add, merge away or
   improve a fact.
4. **Sources.** Each fact and entry gets the scene its words cite (`s12.1`), the player's words if the line quotes them,
   or `setup` for what the campaign began with. A line that cites several scenes gives each fact the scene that says
   it. When a line gives no source, put it to the player with the line: they name the scene, keep it on their own words
   (`player: "…"`, quoting their answer), or drop it. Never write `inference` for a line that came from a sheet.
5. **Dates.** A date with a day and no month ("ready on the 14th") gets its month, and every date gets its year
   (`the 14th of Eleasis, 1493 DR`), from the line, its scene's session log, the campaign's in-world date or the
   player. Ask the player when nothing says it.
6. **The record.** Add `"sheet": "<the sheet's file name>"` and the facts and lists to the person's record with Edit
   (Write for a person with no record yet), as `records-format.md` says; `dmc` checks it after each edit: fix what it
   refuses. Then run `dmc records show <campaign> <name> --all`, compare it with the sheet line by line, and put any
   difference to the player. Delete the Markdown sheet only when the two say the same.
7. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says. Account each sheet
   line as `reworded`, with an anchor in the record that now holds it (the entry's text, or `"key": "…"`); a line the
   player dropped as `dropped` is not allowed here: write it into the record with the player's ruling as its source,
   or ask them to keep it.

## Judgments

- **Only what the sheet says.** One sheet line may become several entries; no entry says more than its line did.
- **One home.** A fact on both the play card and another section of a sheet becomes one entry with `"card": true`;
  if the two lines disagree, the player rules which is right.
- **A key the record has.** When `dmc` lists a record's keys after you add one, use the existing key for the same
  fact instead of a new one ("Hair", not "Hair colour").
- **The opening fields** (role, species, status) are facts with no `section`.

## Declared drops

None: every sheet line moves into a record and is accounted as `reworded`.

## Done when

The check passes: `sheets-in-records` (no person sheet left in `character-sheets/`; each sheet the campaign had is
printed by a record that names it and holds its facts and lists; every record passes `dmc`'s rules).
