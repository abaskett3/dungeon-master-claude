---
standard: 9
title: Sheets get a Wardrobe and keepsakes section; Looks is what they wear now
shipped: PR #287
touches: [character-sheets/]
drops: []
checks: [wardrobe-sections-in-place]
---

# Wardrobe and keepsakes

## What changes

A character's sheet now keeps what they own but are not wearing or carrying now (other clothes, an outfit for an
occasion, winter gear, keepsakes someone holds for them, things outgrown) in a section of its own, `## Wardrobe and
keepsakes`, with where each thing is kept and when it is worn. The sheet's Looks line is what they wear now. This adds
the section to the sheets of your player characters and companions and moves into it what those sheets already say
about clothes and keepsakes that are not worn now, so a change of clothes no longer loses the old ones. Nothing is
removed: every line that moves is accounted for.

## Steps

Read each file before you change it, and `templates/npc-sheet.md` for the section's heading and comment. Every step
first checks whether it is needed, so a sheet already in shape is left as it is.

1. **Which sheets.** Every `character-sheets/pc-*.md`, and the sheet of each companion: in `game-state.md`, under
   `## Party status`, each `### ` block other than "With the party, not companions" and the PC's, found by its `# `
   title as the resume does. Also any other sheet in `character-sheets/` that has a `## ` or `### ` heading starting
   with "Wardrobe". Leave out `minor-npcs.md` and the `-history.md` files.
2. **The section.** A sheet with a `## ` heading that starts with "Wardrobe" but is not exactly `## Wardrobe and
   keepsakes`: rename the heading, keep its lines, and move the section to where a new one goes (below). A `### `
   heading that starts with "Wardrobe" (under another section): move its lines into the `## ` section and remove the
   heading. Two such sections become one. A PC or companion sheet with no such section: add `## Wardrobe and
   keepsakes`, a blank line and the template's comment, right before `## History` (or the older `## Notes` or
   `## History with the PC`), or at the end when the sheet has none.
3. **What moves in.** On each of these sheets, find what the character owns and is not wearing or carrying now: in
   `Looks`, `Appearance`, `Items used in play`, `Gear`, `Equipped`, `Carried` and `Stored elsewhere`, in canon, and
   in another sheet's `Stored elsewhere` that holds the character's things. Put each in the section, one item a line:
   what it is; where it is kept and who holds it; when it is worn. Then shorten the line it came from to what is worn
   or carried now. A planned outfit for a coming occasion goes in the section with the occasion, and Looks keeps a
   short pointer to it ("for the dedication: see Wardrobe"). An item on another sheet's `Stored elsewhere` moves to
   its owner's section, and the keeper's line keeps a short pointer. Leave canon as it is: it records what was settled
   and when, and the sheet says what is true now.
4. **What moves out.** A line in the section that names something worn or carried now (it is in Looks or Items used
   in play) is taken out of the section; the line in Looks stays.
5. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says. Account each line
   that moved or was shortened as `reworded`, with the line that now holds it as the anchor.

## Judgments

- **Only what the files say.** Where the files do not say where a thing is kept or when it is worn, write "not
  recorded"; never guess one, and never add an item the files do not name.
- **Owned, not worn now** means the character could put it on or take it out later. A thing lost, destroyed, given
  away or sold is not in the section; it stays in the History.
- **What they wear now** when Looks lists several outfits ("on watch and cold days…"): keep in Looks the everyday one
  and any the latest session shows them in, and move the others, each with when it is worn.
- **A sheet too unlike the templates** to find these lines: add the section empty, name the sheet to the player, and
  let the next full save fill it.

## Declared drops

None: lines move into the section or are shortened, and every one is accounted as `reworded`.

## Done when

The check passes: `wardrobe-sections-in-place` (each PC's and companion's sheet has one `## Wardrobe and keepsakes`
section, and no sheet has another `## ` or `### ` heading that starts with "Wardrobe").
