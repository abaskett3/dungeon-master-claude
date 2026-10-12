---
name: draw-map
description: Draw an ASCII map of the current room, battlefield, building, town or region so the player can see where things are, and save it if the place may be revisited. Use at the start of combat, when positions matter, when the player asks for a map or seems confused about the layout.
---

# Draw map

A map exists to remove confusion. Keep it small, accurate and readable in a terminal.

## Before drawing

Check `maps/` in the active campaign folder for an existing map of this place and start from it. The layout must agree with what you have already described and with `story/canon.md`: same exits, same distances, same features. If your earlier description was vague, the map now fixes the details; log anything new in canon.

## Drawing

- Put the map in a fenced code block so it renders in a fixed-width font.
- Use plain ASCII characters only, so columns line up in every terminal.
- Battle maps: one character cell is 5 feet. Leave a space between cells so the grid looks roughly square. Keep it under about 30 cells wide.
- Larger areas: state the scale under the map (for example "1 cell = 50 ft" or "not to scale").
- North is up unless you say otherwise.
- Always include a legend listing every symbol used.
- Show only what the character can see or knows. Leave unexplored areas blank or marked `?`. Do not reveal hidden doors, traps or lurking enemies.

Usual symbols:

```
#  wall            .  floor           +  door           =  window
~  water           ^  hazard/trap     >  stairs down    <  stairs up
T  tree            o  pillar/boulder  _  table/low cover
@  the player character     A-Z  named allies and NPCs (say who)
1-9  enemies (number them and say what each is)
```

Example:

```
# # # # # # # # # #
# . . . . . . 2 . #
# . o . . . . . . #
+ . . . @ . . 1 . #
# . o . . . _ _ . #
# . . . . . . . > #
# # # # # # # # # #

@ you   1 goblin boss   2 goblin archer
o pillar   _ overturned table (half cover)   + door (west)   > stairs down
1 cell = 5 ft
```

## After drawing

- In combat, record each combatant's position in the tracker in `game-state.md` so the map can be redrawn correctly later. Redraw when positions change enough to matter, not every turn.
- If the place may be revisited, save the map without creature positions to `maps/<place-name>.md`, with the legend, the scale and a line of description.
- For relationships that are not spatial (a faction web, a family tree, a timeline), a simple indented list or box-and-arrow diagram in a code block works the same way.
