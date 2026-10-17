---
name: draw-map
description: Draw a map of the current room, battlefield, building, town or region as a picture in the player's browser, so they can see where things are, and save it if the place may be revisited. Use at the start of combat, when positions matter, when anyone moves in a fight, when the player asks for a map or seems confused about the layout.
---

# Draw map

A map exists to remove confusion. You describe the place as a small data file; `dmc map` checks it, draws it, and shows it in a browser tab the player keeps open beside the terminal. The file is the truth about where everything is: positions and distances are read from it, not remembered.

## Before drawing

Check `maps/` in the active campaign folder for an existing map of this place and start from it. The layout must agree with what you have already described and with `story/canon.md`: same exits, same distances, same features. If your earlier description was vague, the map now fixes the details; log anything new in canon.

An older map may exist only as `maps/<place>.md` with an ASCII drawing, or as a `.json` whose `"cell_ft"` is null or not a standard scale, which `dmc map` rejects. Read its notes and any spoiler warning, then redraw it as a `.json` at 5 or 30 ft (a local place) or a mile scale (an overland map), whichever fits in about 30 cells, agreeing with the distances in `canon.md`, and keep the `.md` for its notes.

## The files

**A place**, `maps/<place-name>.json`: the layout, with no creatures. Reusable.

```json
{
  "title": "Goblin hall",
  "notes": "A low cellar under the mill, lit by one guttering torch.",
  "cell_ft": 5,
  "grid": [
    "##########",
    "#........#",
    "#.o......#",
    "+........#",
    "#.o...__.#",
    "#.......>#",
    "###??#####"
  ],
  "legend": { "_": "overturned table (half cover)" },
  "labels": [ { "at": "I6", "text": "cellar" } ],
  "edges": { "left": "WEST: the mill yard" }
}
```

**The scene**, `maps/live.json`: the place the party is in now, plus who is standing where. Rewritten as the fight moves.

```json
{
  "base": "goblin-hall",
  "tokens": [
    { "id": "@", "name": "Thorin",        "kind": "pc",    "at": "E4" },
    { "id": "K", "name": "Kibil",         "kind": "ally",  "at": "D5" },
    { "id": "1", "name": "Goblin boss",   "kind": "enemy", "at": "H2" },
    { "id": "2", "name": "Goblin archer", "kind": "enemy", "at": "I5", "note": "prone", "down": true },
    { "id": "O", "name": "Ogre",          "kind": "enemy", "at": "F2", "size": 2 }
  ]
}
```

Rules for the grid:

- One character per cell, no spaces between cells, every row the same length. A space means nothing is there (pad short rows with spaces).
- Every map is to scale, never squeezed, under about 30 cells wide. A fight uses 5 ft squares (the default), or `"cell_ft": 30` when the place is too large for 5 ft (a battlefield, a wide cavern, a town); draw only the part in play and describe the rest in `"edges"`. Never use a mile scale for a fight: 5280, 31680 and 316800 (1, 6, 60 miles a square) are for overland travel maps. A scene takes its place's scale; it cannot change it.
- North is up unless `"north"` says otherwise (`"down"`, `"left"`, `"right"`, or `null` when no direction has been established).
- Cells are named by column letter and row number, as printed along the edges of the picture: `E4` is the fifth cell of the fourth row. Use the same names in the combat tracker's Position column and when talking to the player, who can answer the same way ("I move to F4").

Built-in symbols:

```
#  wall            .  floor           +  door           =  window
~  water           ^  hazard/trap     >  stairs down    <  stairs up
T  tree            o  pillar/boulder  _  table/low cover
,  difficult terrain                  ?  not yet seen
```

`"legend"` renames a built-in symbol for this place (`"~": "the melt ring: wet slush"`) or defines a new one. A new symbol given only a name is drawn as floor with the symbol on it. To choose how it is drawn, give `{"name": ..., "as": <terrain>, "color": "#rrggbb"}`, where the terrain is one of `wall`, `floor`, `door`, `window`, `water`, `hazard`, `stairs_down`, `stairs_up`, `tree`, `pillar`, `cover`, `difficult`, `unknown`, `feature`. Keep legend names short; longer detail belongs in the place's notes.

Tokens: `id` is one or two characters (`@` for the player character, a letter for each named ally or NPC, a number for each enemy). `kind` is `pc`, `ally`, `enemy` or `neutral`. `at` is the top-left cell; `size` is the width in cells (2 Large, 3 Huge, 4 Gargantuan); above 5 ft it counts as 1, and several tokens may share a cell. `note` is a short status the player can see (prone, on the ledge, 10 ft up). `down` greys out a creature that is dead or unconscious.

The full format is in `.claude/skills/draw-map/map-format.md`.

## What must not be in these files

The player can see everything in a map file. Put in only what the character can see or knows. Mark unexplored areas `?` or leave them empty. Hidden doors, unsprung traps, lurking enemies and anything else from `dm-notes.md` stay out until they are discovered; then add them.

Draw only what exists now. Something the party is thinking of placing (a tripline, a lantern, a barricade) goes on the map when it has been placed, not while it is being discussed.

## Drawing

Run `dmc map` on the scene (or on a place file, to show a place with nobody on it):

```
./bin/dmc map adventures/<campaign>/maps/live.json --png
```

- It refuses a map with a mistake in it (rows of different lengths, an unknown symbol, a creature in a wall or off the edge, two creatures on one cell) and says what is wrong. Fix the file and run it again.
- It prints the distance between every pair of creatures. **Use those numbers** whenever you state a distance or a position, in the state of play and in rulings on range and movement. For a distance between two cells, run it with `--measure E4 H9`. Distances are straight lines that ignore walls: squares at 5 ft, "about N ft" at 30 ft, "about N miles" on a mile-scale map, and "same square" for one cell above 5 ft; a route around a wall is longer, so count it along the path.
- `--png` also writes `maps/live.png`. When the layout is new or has changed, look at it with the Read tool and check it against what you described before telling the player it is there. For a redraw where only tokens moved, the distance list is enough and `--png` can be left off.
- The first time in a session, add `--open`: it opens `maps/live.html` in the player's browser. Tell them once, in a line, that the map is in their browser and updates by itself. After that, do not open it again; each run updates the open tab within a couple of seconds. Just say the map is updated.

Do not also paste a text drawing of the map into the terminal.

## After drawing

- In combat, record each combatant's cell in the tracker in `game-state.md`. When anyone's position changes, edit `live.json` with the tracker and run `dmc map` after the narration, in the order CLAUDE.md § Maps gives, so the picture, the tracker and your description always agree.
- If the place may be revisited, keep its `maps/<place-name>.json`, and put longer notes (ceiling heights, what lies beyond the edges, rulings about the terrain) in `maps/<place-name>.md` beside it.
- When the fight ends or the party moves on, remove the tokens from `live.json` or point it at the new place.

## When there is no picture

If `dmc` is unavailable, or the player options ask for text maps, draw the same grid in a fenced code block instead: plain ASCII, a space between cells, the legend underneath, 30 cells wide at most. Keep the same scale and state it under the legend ("1 square = 5 ft").

For relationships that are not spatial (a faction web, a family tree, a timeline), a simple indented list or box-and-arrow diagram in a code block is still the right tool.
