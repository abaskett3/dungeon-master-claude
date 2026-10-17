# Map file format

DMC describes a place as a small JSON file (a grid of symbols, a legend and the creatures standing on it). The
renderer checks the file, draws it, and prints the distances between the creatures so they are read, not guessed.
`SKILL.md` says when and how to draw; this file says what the JSON may hold.

```
dmc map <map.json> [--png] [--open]
dmc map <map.json> --measure E4 H9
```

Written next to the map file: `live.html` (the page the player keeps open; it updates itself), `live-data.js` (the
current picture, rewritten on every run) and, with `--png`, `live.png` (the same picture at the same size in pixels,
for DMC to look at with Read). `--open` also opens `live.html` in the default browser. `--measure` prints the distance
between two cells and draws nothing. A mistake in the file is one line that says what is wrong; nothing is drawn.

## Place and scene files

A place file holds the layout and no creatures:

```json
{"title": "Goblin hall", "cell_ft": 5,
 "grid": ["##########", "#........#", "+...o...>#", "##########"],
 "legend": {"o": "pillar"},
 "labels": [{"at": "I3", "text": "to the cellar"}]}
```

A scene file puts creatures on a place. `"base"` is the place file's name, without `.json`, in the same folder. A
scene may hold its own `"grid"` instead, and any key it sets replaces the place's.

```json
{"base": "goblin-hall",
 "tokens": [{"id": "@", "name": "Thorin", "kind": "pc", "at": "E3"},
            {"id": "O", "name": "Ogre", "kind": "enemy", "at": "B2", "size": 2, "note": "prone", "down": false}]}
```

## Grid and tokens

One character per cell, every row the same length, no spaces between cells. A space is empty (nothing there). Columns
are lettered A, B, C from the left and rows numbered 1, 2, 3 from the top, so `E3` is the fifth cell of the third row.
A creature cannot stand in a wall or in empty space.

A token needs `id` (one or two characters, unique) and `at` (its top-left cell). Optional: `name`, `kind` (`pc`,
`ally`, `enemy` or `neutral`, the default), `size`, `note` (a short status the player can see) and `down` (greys the
creature out). `size` is the width in cells (1 Medium or smaller, 2 Large, 3 Huge, 4 Gargantuan). Above 5 ft every
creature fits in one square, so `size` counts as 1 for footprint, blocking and distance, and the legend panel only
shows it (as 2×2).

## Symbols and the legend

```
#  wall        .  floor       +  door        =  window      ~  water
^  hazard      >  stairs down <  stairs up   T  tree        o  pillar
_  low cover   ,  difficult terrain          ?  not yet seen
```

`"legend"` renames a built-in symbol or defines a new one. A value is a name, or
`{"name": ..., "as": <terrain>, "color": "#rrggbb"}`. Terrain names: `wall`, `floor`, `door`, `window`, `water`,
`hazard`, `stairs_down`, `stairs_up`, `tree`, `pillar`, `cover`, `difficult`, `unknown`, `feature`. A new symbol with
only a name is drawn as floor with the symbol written on it.

## Scale

Every map is drawn to scale: one square is the same distance everywhere. `"cell_ft"` is the feet in one square. It
must be 5 (the default; a fight or close quarters), 30 (a larger place: one turn of movement at speed 30), or for
overland maps 5280, 31680 or 316800 (1, 6 or 60 miles; shown as miles). Anything else, including `null`, is rejected.
If a place is too big for 5 ft, draw only the part in play and describe the rest in `"edges"`, or use a larger scale;
never squeeze or skip stretches.

Any kind of token may sit on any map. At 5 ft two tokens cannot share a cell. Above 5 ft several can (a melee), drawn
small inside the cell: up to four are all drawn; with more than four, the first three are drawn and the fourth place
is a "+N" badge, where N counts the fourth creature and every one after it and its tooltip names them. A scene with a
`"base"` cannot change its place's scale.

## Other keys, all optional

`"scale_note"` (a line after the scale), `"north"` (`"up"` by default; `"down"`, `"left"`, `"right"`, or `null` when
no direction is established), `"edges"` (`{"top"|"bottom"|"left"|"right": text}`, a caption on that side), `"notes"`
(a line under the title) and `"labels"` (`[{"at": "I3", "text": "..."}]`, text on a pale plate over a cell).

## Turn order in a fight

Two optional keys, for a scene (a place file has no fight):

```json
"initiative": ["1", "@", "K"],
"current": "@"
```

`"initiative"` is the creatures in the order they act, each written as the `id` of a token on the map (preferred:
ids are unique), or as the `name` of a token when exactly one token has it. `"current"` is whose turn it is, written
the same way and in the list. The picture then has a **Turn order** list at the top of the panel beside the grid,
numbered, each creature with its token's colour and mark, and the current turn on a highlighted plate tagged `TURN`,
in `live.html` and in `live.png`. Without `"initiative"` there is no list, and without `"current"` the list has no
mark (between rounds); a map with neither is drawn exactly as before. When the fight ends, delete both keys.

The keys belong to the scene that holds them: a scene does not inherit them from its `"base"` place file, so a
place file's own `"initiative"` and `"current"` are ignored when it is used as a base.

A mistake is one line, and nothing is drawn: `"initiative"` must be a list of ids or names, none twice, each matching
a token on the map (a creature that is not on the map is not in the order), and `"current"` must be in it. An entry
that is the name of several tokens (two goblins both called "Goblin") is refused, and the error lists their ids to
use instead. `--measure` does not check the turn order, so a stale one never blocks a distance.

## Distances

Distances are between the nearest cells of two creatures, in a straight line; walls are not taken into account. At 5
and 30 ft they count squares the 5e way (a diagonal step costs the same as a straight one). Above 5 ft they are
approximate: "about 90 ft", and "same square (within 30 ft)" for one cell. On a mile-scale map they are the true
straight-line distance between cell centres, to the nearest mile ("about 25 miles"). A run prints one line for each
pair of creatures under a heading that gives the scale; `--measure` prints one line, for example
`B2 to E4: 3 squares, 15 ft (straight line, walls ignored)`.
