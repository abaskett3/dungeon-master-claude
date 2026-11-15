---
standard: 6
title: Companions get a play card; canon's People entries start with the names they go by
shipped: PR #213
touches: [character-sheets/, story/canon.md]
drops: []
checks: [companions-have-play-card, people-entries-named]
---

# Play cards and People names

## What changes

A resume now reads each companion's play card, a short first section of their sheet, instead of the sheet down to its
history; the rest of the sheet is read when play needs it. This writes the card on each companion's sheet from what
the sheet already says, and moves the fight numbers onto it, so each number has one copy. Canon's People entries each
start with the person's bold name, followed by `(also: …)` with the other names the campaign already uses for them,
so the reminder DMC gets when you name someone finds them. Nothing is removed: lines that move or are shortened are
accounted for.

## Steps

Read each file before you change it. Every step first checks whether it is needed, so a sheet that already has its
card, or an entry already in shape, is left as it is.

1. **The companions.** In `game-state.md`, under `## Party status`, each `### ` block other than "With the party, not
   companions" and the PC's names a companion. Find each one's sheet in `character-sheets/` by its `# ` title, as the
   resume does. A block with no sheet (a treasure or a mount kept there) gets nothing: tell the player in one line.
2. **The card.** For each companion sheet whose first `## ` section is not `## Play card`, add the `## Play card`
   section from `templates/npc-sheet.md` right after the sheet's title and its opening list of fields, before its first
   `## ` section, and fill each line from that sheet's own lines: who they are in a line, how they look, the voice with
   one sample line, the names they use, their attitude to the PC and what they want now, AC, max HP, speed, saves and
   senses, each attack, recharge and limited abilities, standing traits that change play, and the items they use in
   play. Write `none` for a line nothing fills. Keep the card within the budget `dmc size` checks.
3. **One copy of each number.** Delete from the rest of the sheet each line whose number the card now holds (AC, max
   HP, speed, saves, skills, senses, attacks, traits); keep what the card does not hold (the source creature, ability
   scores, spells in full, the rest of the gear). A line the card shortens stays where it is when it says more.
4. **People.** In `story/canon.md` under `## People`, give each entry that does not start with a bold name its person's
   bold name first, with the rest of the line unchanged after a colon. Then, after an entry's first bold name, add
   `(also: …)` with the other names and role words this campaign's files already use for that person (a nickname, a
   surname used alone, a title such as "the ferryman"), separated by commas; leave it out when there are none.
5. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says. Account each line
   that moved to the card or was shortened as `reworded`, with the card line that now holds it as the anchor, and each
   People line you changed as `reworded`, with its new start as the anchor.

## Judgments

- **The names**: write only names the files already use for the person (canon, the sheets, the session logs). Never a
  name DMC would use as a label, or one the character has not heard; never invent one.
- **A companion built on the PC template** (`- **Companion**`): it gets a card the same way; its numbers move from
  `## Combat` and the attacks table.
- **A sheet whose history is in the middle**: add the card first anyway; leave the history where it is.
- **A sheet too unlike either template** to find its numbers: skip it, name it to the player, and let the next full
  save write the card (`dmc size` names it until then).
- **A People entry about a fact, not a person** (`- **Hala's two loves**: …`): it already starts with a bold name;
  leave it.

## Declared drops

None: the card moves and shortens lines, and every one is accounted as `reworded`.

## Done when

The checks pass: `companions-have-play-card` (each companion's sheet has `## Play card` as its first section) and
`people-entries-named` (each entry under canon's People starts with a bold name).
