# Contract: the map's `aura` and `view` (S2, FR-025, research R9)

Two optional fields in the map format, whose home is `kit/skills/dmc-draw-map/map-format.md`. A map without them draws
exactly as before, so no migration (#226).

## A token's aura

```json
{"id": "3", "name": "Grey stone", "kind": "enemy", "at": "H7", "aura": {"ft": 10, "name": "rime"}}
```

- `ft`: the reach from the token's edge, a multiple of the map's `cell_ft`. `name`: shown in the legend.
- Drawn as a shaded area around the token, moving with it when `at` changes. Terrain is not changed.
- The distance list (`src/Dmc.Cli/Maps/Distances.cs`) says which tokens are inside each aura.
- Refused, with a one-line error: `ft` not a positive multiple of `cell_ft`; a missing `name`.

## A map's view

```json
{"view": {"cols": "C-R", "rows": "3-17"}}
```

- Draws only those columns and rows, at the size that range fits, with the place's own cell labels, so the tracker's
  cells still hold.
- Tokens outside the view are listed under the map, not drawn.
- Refused: a range outside the grid, or reversed.

## Tests

`tests/Dmc.Tests/Map/`: an aura before and after a move, in the picture and the distance list; a view's labels match the
full map's; the refusals; a map with neither field is unchanged byte for byte.
