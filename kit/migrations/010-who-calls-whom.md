---
standard: 10
title: Who calls whom what, and what each person cannot do, move into a record per person
shipped: the PR for issue #290
touches: [character-sheets/, story/canon.md, records/]
drops: []
checks: [records-in-place]
---

# Who calls whom what

## What changes

The names people use for each other, the relationships between them, and what each person cannot do move from the
sheets' "Names they use" lines and canon's `(also: …)` lists into one record per person, `records/<name>.json`, that
`dmc` checks. Each name says who uses it, so the reminder before your messages can tell whose word is meant. You rule
on every conflict the files hold; nothing is assumed. A sheet keeps one line for the names of places and things.
Nothing is removed: every line that moves is accounted for.

## Steps

Read each file before you change it, and the `records-format.md` of the `dmc-character-sheet-update` skill for the
record's shape. Every step first checks whether it is needed, so a campaign already in shape is left as it is.

1. **The persons.** The person of each sheet in `character-sheets/` (its `# ` title; not `minor-npcs.md` or the
   `-history.md` files), of each canon People entry (its first bold name), and each person a Names line or an
   `(also: …)` item names, with a sheet or not. A person of a campaign this one continues, whom this campaign's files
   name, gets a record here, with `"of"` naming that campaign; never change a closed campaign.
2. **The draft.** Run `dmc records draft <campaign>`. It writes the draft beside the backup the migration just made,
   with each People `(also: …)` item as a row with no speaker, and prints its path. If it says the draft exists, use
   it as it is.
3. **The Names lines into the draft.** With Edit on the draft, for each sheet's "Names they use" lines (the play
   card's and the one under "Playing them" or "Story"), add a row for each part of the line about a person: `to` the
   person meant (full name), `from` the sheet's person, the `word`, `kind` `usage` (or `relationship` for what the
   person is to them, such as "Dessa is his mother"), `when` if the line says when, and `line` the sheet and line it
   came from (`character-sheets/npc-x.md:14`). A word the line says they never use for someone is a `never` entry on
   the row of that word on that person's record; add a `name` row from `anyone` for it when there is none. The
   `source` is the scene the line cites (`s12.1`), or the player's words if the line quotes them; when it gives
   neither, leave it `null`. For each `(also: …)` row, fill in `from` only where canon or a sheet says who uses it.
4. **The conflicts, to the player.** Run `dmc records check <campaign> --draft`. Put every finding to the player with
   the lines it cites, and ask for their ruling: who says a word, which of two lines is right, the source of a row.
   A row with no source is kept only on the player's word: they name the scene, keep it on their own words (its
   `player: "…"` source), or drop it; never offer to write it with no source, or as `inference`.
   Ask them also to confirm any ruling of theirs that the files do not yet hold, and quote their answer as the
   `player: "…"` source. Change the draft as they rule, and run the check again until it prints `no findings`, or
   only findings the player chose to keep.
5. **The records.** Write each person's record with Write, from the draft and the player's rulings, as
   `records-format.md` says. `dmc` checks each one when it is written; fix any it refuses.
6. **Sample lines.** Each quoted sample line on a sheet's Voice line that speaks to someone gets `(to <name>)` after
   it. Run `dmc records check <campaign>` and put each sample finding to the player: a sample is theirs to keep,
   change or replace.
7. **The sheets.** Replace each sheet's "Names they use" lines with one line, `- **Names for places and things**:`,
   holding only what the old lines said about places and things (on the play card if the sheet has one, else under
   "Playing them" or "Story"); write `none` when nothing is left. Remove the other Names line.
8. **Canon.** Remove `(also: …)` from each People entry, keeping its bold name and the rest of the line.
9. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says. Account each line
   that moved or was shortened as `reworded`, with an anchor in what now holds it: the record's `"word": "…"` text for
   a name, or the new "Names for places and things" line.

## Judgments

- **Only what the files say.** Never guess a speaker, a source or a relationship. A relationship the files do not
  state is not written, and a usage is never written because of a relationship: someone may call a person "Mama" who
  is not their mother.
- **A word for different persons to different speakers** is not a conflict when every speaker is known: it is what
  the records are for.
- **A title or kin word** is a word like any other: write its row with its speaker; never leave one out because it is
  a title.
- **A sheet too unlike the templates** to find its Names lines: give the person a record with what canon says, name
  the sheet to the player, and let the next full save finish it.

## Declared drops

None: every line moves into a record or is shortened, and is accounted as `reworded`.

## Done when

The check passes: `records-in-place` (every sheet's person has a record, every record passes `dmc`'s rules, no sheet
has a "Names they use" line, and no canon People entry has `(also:`).
