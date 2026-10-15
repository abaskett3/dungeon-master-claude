#!/usr/bin/env python3
"""Draw a campaign map as a picture the player watches in a browser tab.

Usage:
  python scripts/map_render.py <map.json> [--png] [--open]
  python scripts/map_render.py <map.json> --measure E4 H9

The map file format (places, scenes, grid, legend, scales, distances) is described in
.claude/skills/draw-map/map-format.md.
"""
import argparse
import datetime
import html
import json
import math
import os
import sys
import webbrowser

HERE = os.path.dirname(os.path.abspath(__file__))

CELL = 40          # pixels per cell
PAD = 16           # outer margin
GUTTER = 26        # room for the coordinates along each edge
PANEL = 290        # width of the legend panel beside the map
FONT = "Segoe UI, Helvetica, Arial, sans-serif"

PAGE_BG = "#1e2227"
INK = "#e6e8eb"
MUTED = "#9aa3ad"
FLOOR = "#ebe6d9"
GRID_LINE = "#cbc4b3"
WALL = "#3b4149"

# symbol -> (terrain, default name)
BUILTIN = {
    "#": ("wall", "wall"),
    ".": ("floor", "floor"),
    "+": ("door", "door"),
    "=": ("window", "window"),
    "~": ("water", "water"),
    "^": ("hazard", "hazard"),
    ">": ("stairs_down", "stairs down"),
    "<": ("stairs_up", "stairs up"),
    "T": ("tree", "tree"),
    "o": ("pillar", "pillar"),
    "_": ("cover", "low cover"),
    ",": ("difficult", "difficult terrain"),
    "?": ("unknown", "not yet seen"),
}
TERRAINS = {"wall", "floor", "door", "window", "water", "hazard", "stairs_down",
            "stairs_up", "tree", "pillar", "cover", "difficult", "unknown",
            "feature"}
# A creature cannot stand in these.
BLOCKED = {"wall", "void"}

# Map scales, in feet per square: 5 ft (the 5e grid), 30 ft (one turn of
# movement at speed 30; a choice for this toolkit, not a DMG scale) and
# 1, 6 and 60 miles (province, kingdom and continent hexes; DMG p. 14).
SCALES = (5, 30, 5280, 31680, 316800)
MILE = 5280


def is_overland(cell_ft):
    return cell_ft >= MILE


def is_coarse(cell_ft):
    """Any square bigger than a creature's 5 ft: tokens share cells and size counts as 1."""
    return cell_ft > 5


KINDS = {
    "pc": ("#2f6fed", "you"),
    "ally": ("#2e9e5b", "ally"),
    "enemy": ("#d0342c", "enemy"),
    "neutral": ("#7d8590", "other"),
}
NORTH_TURN = {"up": 0, "right": 90, "down": 180, "left": 270}


def fail(message):
    sys.exit(f"map_render: {message}")


# ---------- reading and checking ----------

def load_json(path):
    try:
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)
    except FileNotFoundError:
        fail(f"no such file: {path}")
    except json.JSONDecodeError as err:
        fail(f"{path} is not valid JSON: {err}")
    if not isinstance(data, dict):
        fail(f"{path} must hold a JSON object.")
    if "cell_ft" in data:
        check_scale(data["cell_ft"], path)
    return data


def check_scale(cell_ft, where="the map"):
    """Reject a scale that is not standard. `where` names the file that holds it."""
    if cell_ft not in SCALES:
        allowed = ", ".join(f"{s} ({format_distance(1, s)})" if is_overland(s) else str(s) for s in SCALES)
        fail(f'{where}: "cell_ft" must be a standard map scale in feet ({allowed}), '
             f"not {json.dumps(cell_ft)}: every map is drawn to scale.")


def load_map(path):
    """Read a place or scene file; a scene's "base" is merged underneath it."""
    data = load_json(path)
    base = data.get("base")
    if base:
        base_path = os.path.join(os.path.dirname(os.path.abspath(path)), base + ".json")
        if not os.path.exists(base_path):
            fail(f'"base": "{base}" names {base_path}, which does not exist.')
        merged = load_json(base_path)
        if merged.get("base"):
            fail(f"{base_path} is a scene (it has its own \"base\"); base must be a place file.")
        base_ft = merged.get("cell_ft", 5)
        if "cell_ft" in data and data["cell_ft"] != base_ft:
            fail(f'{path} sets "cell_ft": {json.dumps(data["cell_ft"])}, but its base "{base}" '
                 f"is drawn at {json.dumps(base_ft)}: a scene cannot change its place's scale.")
        merged.update({key: value for key, value in data.items() if key != "base"})
        data = merged
    if "grid" not in data:
        fail(f'{path} has no "grid" and no "base" to take one from.')
    return data


def col_name(index):
    """0 -> A, 25 -> Z, 26 -> AA."""
    name = ""
    index += 1
    while index:
        index, rem = divmod(index - 1, 26)
        name = chr(65 + rem) + name
    return name


def cell_name(col, row):
    return f"{col_name(col)}{row + 1}"


def parse_cell(text, cols, rows, what):
    """Turn "E3" into (col, row), both 0-based."""
    text = str(text).strip().upper()
    letters = "".join(ch for ch in text if ch.isalpha())
    digits = text[len(letters):]
    if not letters or not digits.isdigit():
        fail(f'{what}: "{text}" is not a cell like "E3".')
    col = 0
    for ch in letters:
        col = col * 26 + (ord(ch) - 64)
    col -= 1
    row = int(digits) - 1
    if not (0 <= col < cols and 0 <= row < rows):
        fail(f"{what}: {text} is off the map (the map is A1 to {cell_name(cols - 1, rows - 1)}).")
    return col, row


def build_legend(data, grid):
    """Resolve every symbol used in the grid to {terrain, name, color}."""
    custom = data.get("legend", {})
    if not isinstance(custom, dict):
        fail('"legend" must be an object of symbol: name.')
    for symbol in custom:
        if len(symbol) != 1 or symbol == " ":
            fail(f'legend key "{symbol}" must be a single character, not a space.')
    legend = {}
    for symbol in sorted({ch for row in grid for ch in row if ch != " "}):
        terrain, name = BUILTIN.get(symbol, (None, None))
        color = None
        entry = custom.get(symbol)
        if isinstance(entry, str):
            name = entry
        elif isinstance(entry, dict):
            name = entry.get("name", name)
            terrain = entry.get("as", terrain)
            color = entry.get("color")
        elif entry is not None:
            fail(f'legend entry for "{symbol}" must be a name or an object.')
        if terrain is None:
            if entry is None:
                fail(f'the grid uses "{symbol}", which is not a built-in symbol '
                     f'and is not in "legend".')
            terrain = "feature"
        if terrain not in TERRAINS:
            fail(f'legend entry for "{symbol}": unknown terrain "{terrain}". '
                 f"Use one of: {', '.join(sorted(TERRAINS))}.")
        if not name:
            fail(f'legend entry for "{symbol}" needs a name.')
        legend[symbol] = {"terrain": terrain, "name": str(name), "color": color}
    return legend


def check(data):
    """Validate the map and return it in the form the drawing code uses."""
    grid = data["grid"]
    if not isinstance(grid, list) or not grid or not all(isinstance(r, str) for r in grid):
        fail('"grid" must be a list of strings, one per row.')
    cols = len(grid[0])
    if cols == 0:
        fail("the first grid row is empty.")
    for number, row in enumerate(grid, 1):
        if len(row) != cols:
            fail(f"grid row {number} is {len(row)} cells wide but row 1 is {cols}. "
                 f"Every row must be the same length (pad with spaces).")
    rows = len(grid)
    legend = build_legend(data, grid)

    def terrain_at(col, row):
        symbol = grid[row][col]
        return "void" if symbol == " " else legend[symbol]["terrain"]

    cell_ft = data.get("cell_ft", 5)
    check_scale(cell_ft)  # load_json has already checked it by file name; this one guards direct callers

    north = data.get("north", "up")
    if north is not None and north not in NORTH_TURN:
        fail('"north" must be "up", "down", "left", "right" or null.')

    tokens = []
    seen_ids = set()
    taken = {}
    for token in data.get("tokens", []):
        if not isinstance(token, dict) or "id" not in token or "at" not in token:
            fail('every token needs at least "id" and "at".')
        tid = str(token["id"])
        if not 1 <= len(tid) <= 2:
            fail(f'token id "{tid}" must be one or two characters.')
        if tid in seen_ids:
            fail(f'two tokens share the id "{tid}".')
        seen_ids.add(tid)
        name = str(token.get("name", tid))
        kind = token.get("kind", "neutral")
        if kind not in KINDS:
            fail(f'token {tid}: kind "{kind}" must be one of {", ".join(KINDS)}.')
        size = token.get("size", 1)
        if size not in (1, 2, 3, 4):
            fail(f"token {tid}: size must be 1, 2, 3 or 4 (cells across).")
        span = 1 if is_coarse(cell_ft) else size  # cells across on this map
        col, row = parse_cell(token["at"], cols, rows, f"token {tid} ({name})")
        cells = [(col + dx, row + dy) for dy in range(span) for dx in range(span)]
        for c, r in cells:
            if c >= cols or r >= rows:
                fail(f"token {tid} ({name}) at {cell_name(col, row)} with size {size} "
                     f"runs off the map.")
            if terrain_at(c, r) in BLOCKED:
                fail(f"token {tid} ({name}) is on {cell_name(c, r)}, which is "
                     f"{'empty space' if terrain_at(c, r) == 'void' else 'a wall'}.")
            if (c, r) in taken and not is_coarse(cell_ft):
                fail(f"tokens {taken[(c, r)]} and {tid} are both on {cell_name(c, r)}.")
            taken[(c, r)] = tid
        tokens.append({"id": tid, "name": name, "kind": kind, "size": size, "span": span,
                       "col": col, "row": row, "cells": cells,
                       "note": str(token.get("note", "")),
                       "down": bool(token.get("down", False))})

    labels = []
    for label in data.get("labels", []):
        if not isinstance(label, dict) or "at" not in label or "text" not in label:
            fail('every label needs "at" and "text".')
        col, row = parse_cell(label["at"], cols, rows, f'label "{label["text"]}"')
        labels.append({"col": col, "row": row, "text": str(label["text"])})

    edges = data.get("edges", {})
    if not isinstance(edges, dict) or set(edges) - {"top", "bottom", "left", "right"}:
        fail('"edges" may only have the keys top, bottom, left and right.')

    return {"title": str(data.get("title", "Map")), "notes": str(data.get("notes", "")),
            "grid": grid, "cols": cols, "rows": rows, "legend": legend,
            "cell_ft": cell_ft, "scale_note": str(data.get("scale_note", "")),
            "north": north, "tokens": tokens, "labels": labels, "edges": edges}


# ---------- distances ----------

def squares_between(cells_a, cells_b):
    return min(max(abs(ac - bc), abs(ar - br)) for ac, ar in cells_a for bc, br in cells_b)


def straight_squares(cells_a, cells_b):
    """True straight-line distance in squares between the nearest cell centres."""
    return min(math.hypot(ac - bc, ar - br) for ac, ar in cells_a for bc, br in cells_b)


def format_distance(squares, cell_ft):
    """A distance in squares as text: feet on a foot-scale map, whole miles on an overland one."""
    if is_overland(cell_ft):
        miles = round(squares * cell_ft / MILE)
        return f"{miles} mile" + ("" if miles == 1 else "s")
    return f"{squares * cell_ft:g} ft"


def squares_apart(cells_a, cells_b, cell_ft):
    """Chebyshev squares on a foot-scale map (the 5e rule); a straight line, in
    squares, on an overland map."""
    measure = straight_squares if is_overland(cell_ft) else squares_between
    return measure(cells_a, cells_b)


def distance_text(squares, cell_ft):
    """A distance as text. Above 5 ft a square is bigger than a creature, so it is
    only approximate ("about"), and one cell is "same square"."""
    if not is_coarse(cell_ft):
        return format_distance(squares, cell_ft)
    if squares == 0:
        return f"same square (within {format_distance(1, cell_ft)})"
    return "about " + format_distance(squares, cell_ft)


def distance(cells_a, cells_b, cell_ft):
    return distance_text(squares_apart(cells_a, cells_b, cell_ft), cell_ft)


def measure_text(a, b, cell_ft):
    """The line `--measure` prints for two cells."""
    squares = squares_apart([a], [b], cell_ft)
    span = f"{cell_name(*a)} to {cell_name(*b)}: "
    far = distance_text(squares, cell_ft)
    if is_coarse(cell_ft) and squares == 0:
        return span + far
    if not is_overland(cell_ft):  # a count of squares only means something on a grid
        far = f"{squares} square{'' if squares == 1 else 's'}, {far}"
    return f"{span}{far} (straight line, walls ignored)"


def distance_lines(m):
    tokens = m["tokens"]
    if len(tokens) < 2:
        return []
    scale = format_distance(1, m["cell_ft"])
    if is_overland(m["cell_ft"]):
        lines = [f"Distances (1 cell = {scale}, straight line, walls ignored):"]
    elif is_coarse(m["cell_ft"]):  # a foot scale above 5 ft: 30 ft
        lines = [f"Distances (1 cell = {scale}, so about ±{scale}; straight line, walls ignored):"]
    else:
        lines = [f"Distances (1 cell = {scale}, straight line, walls ignored; {scale} is adjacent):"]
    for i, a in enumerate(tokens):
        for b in tokens[i + 1:]:
            lines.append(f"  {a['id']} {a['name']} ({cell_name(a['col'], a['row'])})"
                         f"  <->  {b['id']} {b['name']} ({cell_name(b['col'], b['row'])})"
                         f":  {distance(a['cells'], b['cells'], m['cell_ft'])}")
    return lines


# ---------- drawing ----------

def esc(text):
    return html.escape(str(text), quote=True)


def wrap(text, width):
    """Break text into lines of at most `width` characters, on spaces."""
    lines, line = [], ""
    for word in text.split():
        if line and len(line) + 1 + len(word) > width:
            lines.append(line)
            line = word
        else:
            line = f"{line} {word}".strip()
    if line:
        lines.append(line)
    return lines or [""]


def text(x, y, content, size=13, fill=INK, anchor="start", weight="normal"):
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-family="{FONT}" font-size="{size}" '
            f'fill="{fill}" text-anchor="{anchor}" font-weight="{weight}">{esc(content)}</text>')


def tile(terrain, symbol, x, y, s, color=None):
    """SVG for one cell of the given terrain with its top-left corner at x, y."""
    out = []

    def rect(fill, inset=0.0, rx=0, stroke=None):
        extra = f' stroke="{stroke}" stroke-width="1"' if stroke else ""
        out.append(f'<rect x="{x + inset:.1f}" y="{y + inset:.1f}" width="{s - 2 * inset:.1f}" '
                   f'height="{s - 2 * inset:.1f}" rx="{rx}" fill="{fill}"{extra}/>')

    def floor(fill=FLOOR):
        rect(fill, stroke=GRID_LINE)

    def glyph(char, fill, size=0.5):
        out.append(text(x + s / 2, y + s / 2 + s * size * 0.36, char, s * size, fill,
                        "middle", "bold"))

    cx, cy = x + s / 2, y + s / 2
    if terrain == "wall":
        rect(color or WALL)
    elif terrain == "floor":
        floor(color or FLOOR)
    elif terrain == "door":
        floor()
        rect(color or "#8a5a2b", inset=s * 0.14, rx=3, stroke="#5b3a1a")
        out.append(f'<circle cx="{x + s * 0.68:.1f}" cy="{cy:.1f}" r="{s * 0.06:.1f}" fill="#e8c56a"/>')
    elif terrain == "window":
        rect(WALL)
        out.append(f'<rect x="{x + s * 0.1:.1f}" y="{y + s * 0.36:.1f}" width="{s * 0.8:.1f}" '
                   f'height="{s * 0.28:.1f}" fill="{color or "#9fd3f0"}"/>')
    elif terrain == "water":
        rect(color or "#6fb1e0", stroke="#4f93c4")
        for dy in (0.38, 0.66):
            yy = y + s * dy
            out.append(f'<path d="M{x + s * 0.15:.1f} {yy:.1f} q{s * 0.09:.1f} {-s * 0.1:.1f} '
                       f'{s * 0.175:.1f} 0 t{s * 0.175:.1f} 0 t{s * 0.175:.1f} 0 t{s * 0.175:.1f} 0" '
                       f'fill="none" stroke="#e8f4fc" stroke-width="1.6"/>')
    elif terrain == "hazard":
        floor(color or "#f3c98b")
        out.append(f'<path d="M{cx:.1f} {y + s * 0.2:.1f} L{x + s * 0.82:.1f} {y + s * 0.8:.1f} '
                   f'L{x + s * 0.18:.1f} {y + s * 0.8:.1f} Z" fill="#d9541e" stroke="#7a2c0c" '
                   f'stroke-width="1"/>')
        out.append(text(cx, y + s * 0.74, "!", s * 0.4, "#ffffff", "middle", "bold"))
    elif terrain in ("stairs_down", "stairs_up"):
        floor(color or "#d6d0c2")
        for i in range(4):
            xx = x + s * (0.2 + i * 0.2)
            out.append(f'<line x1="{xx:.1f}" y1="{y + s * 0.15:.1f}" x2="{xx:.1f}" '
                       f'y2="{y + s * 0.85:.1f}" stroke="#6a6558" stroke-width="1.5"/>')
        glyph("▼" if terrain == "stairs_down" else "▲", "#2b2f36", 0.42)
    elif terrain == "tree":
        floor()
        out.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{s * 0.4:.1f}" fill="{color or "#4c9a52"}" '
                   f'stroke="#2c6331" stroke-width="1.5"/>')
        out.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{s * 0.1:.1f}" fill="#6b4a2a"/>')
    elif terrain == "pillar":
        floor()
        out.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{s * 0.32:.1f}" fill="{color or "#7b828c"}" '
                   f'stroke="#454b53" stroke-width="2"/>')
    elif terrain == "cover":
        floor()
        rect(color or "#b98952", inset=s * 0.12, rx=4, stroke="#6b4a2a")
    elif terrain == "difficult":
        floor(color or "#dccfb0")
        for dx, dy in ((0.25, 0.3), (0.6, 0.22), (0.75, 0.6), (0.4, 0.68), (0.2, 0.8), (0.55, 0.45)):
            out.append(f'<circle cx="{x + s * dx:.1f}" cy="{y + s * dy:.1f}" r="{s * 0.055:.1f}" '
                       f'fill="#8f7f5c"/>')
    elif terrain == "unknown":
        rect(color or "#2a2f36", stroke="#3a4048")
        glyph("?", "#6d7682", 0.45)
    else:  # feature: floor with the map's own symbol on it
        floor(color or "#e0d6bd")
        glyph(symbol, "#4a4233", 0.5)
    return "".join(out)


SLOT_RADIUS = 8.5    # tokens sharing a cell: two across, so four fit in a 40 px cell
SLOT_FONT = 10       # and their ids are never drawn smaller than this
SHOWN_IN_A_CELL = 4  # when more share a cell, the last place is a "+N" badge


def shared_slots(tokens):
    """Lay out tokens that share a cell. Returns (slots, badges). slots maps a token id
    to (dx, dy) in pixels from the cell's top-left corner, or to None for a token left
    out because the cell is crowded. badges is one (col, row, dx, dy, hidden tokens) for
    each crowded cell, drawn as "+N" in the last place."""
    groups = {}
    for token in tokens:
        groups.setdefault((token["col"], token["row"]), []).append(token)
    slots, badges = {}, []
    half = CELL / 2
    for (col, row), group in groups.items():
        if len(group) < 2:
            continue
        places = min(len(group), SHOWN_IN_A_CELL)
        rows = (places + 1) // 2
        for i in range(places):
            r, c = divmod(i, 2)
            across = 1 if r == rows - 1 and places % 2 else 2
            place = ((CELL - across * half) / 2 + (c + 0.5) * half, (CELL - rows * half) / 2 + (r + 0.5) * half)
            if len(group) > SHOWN_IN_A_CELL and i == places - 1:
                badges.append((col, row, *place, group[places - 1:]))
            else:
                slots[group[i]["id"]] = place
        for token in group[places - 1:] if len(group) > SHOWN_IN_A_CELL else []:
            slots[token["id"]] = None
    return slots, badges


def draw_badge(col, row, dx, dy, hidden, x0, y0):
    """The "+N" that stands for the tokens that did not fit in a crowded cell."""
    cx, cy = x0 + col * CELL + dx, y0 + row * CELL + dy
    tip = "Also here: " + ", ".join(t["name"] for t in hidden)
    return (f'<g><title>{esc(tip)}</title>'
            f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{SLOT_RADIUS}" fill="{MUTED}" stroke="#ffffff" stroke-width="2.5"/>'
            + text(cx, cy + SLOT_FONT * 0.36, f"+{len(hidden)}", SLOT_FONT, "#15181c", "middle", "bold") + "</g>")


def draw_token(token, x0, y0, legend_scale=None, slot=None):
    """SVG for a creature. With legend_scale, draw a small sample at x0, y0. With
    slot (dx, dy), draw it small inside its cell, which it shares with others."""
    color, _ = KINDS[token["kind"]]
    if slot is not None:
        cx = x0 + token["col"] * CELL + slot[0]
        cy = y0 + token["row"] * CELL + slot[1]
        radius = SLOT_RADIUS
    elif legend_scale is None:
        width = token["span"] * CELL
        cx = x0 + token["col"] * CELL + width / 2
        cy = y0 + token["row"] * CELL + width / 2
        radius = width / 2 - 4
    else:
        cx, cy, radius = x0, y0, legend_scale
    opacity = "0.45" if token["down"] else "1"
    font = min(radius * 1.05, 26) if len(token["id"]) == 1 else min(radius * 0.85, 22)
    if slot is not None:
        font = SLOT_FONT
    tip = f"{token['name']} ({cell_name(token['col'], token['row'])})"
    if token["note"]:
        tip += f": {token['note']}"
    parts = [f'<g opacity="{opacity}"><title>{esc(tip)}</title>',
             f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{radius:.1f}" fill="{color}" '
             f'stroke="#ffffff" stroke-width="2.5"/>',
             f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{radius + 1.5:.1f}" fill="none" '
             f'stroke="#15181c" stroke-width="1"/>',
             text(cx, cy + font * 0.36, token["id"], font, "#ffffff", "middle", "bold")]
    if token["down"]:
        d = radius * 0.7
        parts.insert(-1, f'<path d="M{cx - d:.1f} {cy + d:.1f} L{cx + d:.1f} {cy - d:.1f}" '
                         f'stroke="#15181c" stroke-width="2.5"/>')
    parts.append("</g>")
    return "".join(parts)


def render_svg(m):
    cols, rows = m["cols"], m["rows"]
    map_w, map_h = cols * CELL, rows * CELL
    edges = m["edges"]
    side_l = 150 if edges.get("left") else 0
    side_r = 150 if edges.get("right") else 0

    # The map sits on the left; the legend is a panel to its right.
    x0 = PAD + side_l + GUTTER                             # left edge of the grid
    left_w = max(x0 + map_w + GUTTER + side_r, 300)        # right edge of the map block
    panel_x = left_w + 18
    width = panel_x + PANEL + PAD
    out = []
    y = PAD + 22

    out.append(text(PAD, y, m["title"], 20, INK, weight="bold"))
    for line in wrap(m["notes"], int((width - 2 * PAD) / 7)) if m["notes"] else []:
        y += 19
        out.append(text(PAD, y, line, 13, MUTED))
    y += 14
    top = y
    if edges.get("top"):
        for line in wrap(edges["top"], max(int(map_w / 6.5), 20)):
            y += 16
            out.append(text(x0 + map_w / 2, y, line, 12, MUTED, "middle"))
        y += 6

    y0 = y + GUTTER                                        # top edge of the grid

    # tiles
    for r, row in enumerate(m["grid"]):
        for c, symbol in enumerate(row):
            if symbol == " ":
                continue
            entry = m["legend"][symbol]
            out.append(tile(entry["terrain"], symbol, x0 + c * CELL, y0 + r * CELL, CELL,
                            entry["color"]))

    # coordinates on all four sides
    for c in range(cols):
        cx = x0 + c * CELL + CELL / 2
        out.append(text(cx, y0 - 8, col_name(c), 12, MUTED, "middle"))
        out.append(text(cx, y0 + map_h + 17, col_name(c), 12, MUTED, "middle"))
    for r in range(rows):
        cy = y0 + r * CELL + CELL / 2 + 4
        out.append(text(x0 - 8, cy, r + 1, 12, MUTED, "end"))
        out.append(text(x0 + map_w + 8, cy, r + 1, 12, MUTED, "start"))

    # side captions
    for key, anchor, tx in (("left", "end", x0 - GUTTER - 8),
                            ("right", "start", x0 + map_w + GUTTER + 8)):
        if edges.get(key):
            lines = wrap(edges[key], 20)
            ty = y0 + map_h / 2 - (len(lines) - 1) * 8
            for line in lines:
                out.append(text(tx, ty, line, 12, MUTED, anchor))
                ty += 16

    # labels: text on a pale plate, centred on the cell
    for label in m["labels"]:
        lx = x0 + label["col"] * CELL + CELL / 2
        ly = y0 + label["row"] * CELL + CELL / 2
        w = len(label["text"]) * 6.4 + 10
        out.append(f'<rect x="{lx - w / 2:.1f}" y="{ly - 9:.1f}" width="{w:.1f}" height="18" rx="4" '
                   f'fill="#fffdf5" fill-opacity="0.88" stroke="#8a8371" stroke-width="0.8"/>')
        out.append(text(lx, ly + 4, label["text"], 11, "#2b2f36", "middle"))

    # creatures
    slots, badges = shared_slots(m["tokens"])
    for token in m["tokens"]:
        if slots.get(token["id"], ()) is not None:  # None: too many in one cell, shown as +N
            out.append(draw_token(token, x0, y0, slot=slots.get(token["id"])))
    for badge in badges:
        out.append(draw_badge(*badge, x0, y0))

    y = y0 + map_h + GUTTER
    if edges.get("bottom"):
        for line in wrap(edges["bottom"], max(int(map_w / 6.5), 20)):
            y += 14
            out.append(text(x0 + map_w / 2, y, line, 12, MUTED, "middle"))
        y += 6

    # scale bar and compass, under the map
    y += 22
    out.append(f'<path d="M{PAD} {y - 9} v8 h{CELL} v-8" fill="none" stroke="{INK}" stroke-width="1.5"/>')
    scale = f"1 square = {format_distance(1, m['cell_ft'])}"
    if m["scale_note"]:
        scale += f". {m['scale_note']}"
    sx = PAD + CELL + 10
    scale_lines = wrap(scale, max(int((left_w - sx - 60) / 7), 16))
    for i, line in enumerate(scale_lines):
        out.append(text(sx, y + i * 17, line, 13, INK))
    if m["north"] is not None:
        nx, ny = left_w - 18, y - 4
        out.append(f'<g transform="rotate({NORTH_TURN[m["north"]]} {nx} {ny})">'
                   f'<path d="M{nx} {ny - 15} L{nx + 7} {ny + 9} L{nx} {ny + 4} L{nx - 7} {ny + 9} Z" '
                   f'fill="{INK}"/></g>')
        out.append(text(nx - 16, y, "N", 13, INK, "end", "bold"))
    map_bottom = y + (len(scale_lines) - 1) * 17 + PAD

    # legend panel: creatures first, then terrain
    chars = int((PANEL - 34) / 6.8)
    row_h = 17
    py = top + 14

    def heading(label):
        nonlocal py
        out.append(text(panel_x, py, label.upper(), 11, MUTED, weight="bold"))
        py += 10

    def entry_text(lines):
        nonlocal py
        for i, line in enumerate(lines):
            out.append(text(panel_x + 34, py + 16 + i * row_h, line, 13, INK))
        py += max(len(lines) * row_h, 24) + 7

    if m["tokens"]:
        heading("On the map")
        for token in m["tokens"]:
            label = f"{token['name']}  \u00b7  {cell_name(token['col'], token['row'])}"
            if token["size"] > 1:
                label += f" ({token['size']}\u00d7{token['size']})"
            if token["note"]:
                label += f"  \u00b7  {token['note']}"
            out.append(draw_token(token, panel_x + 12, py + 12, legend_scale=11))
            entry_text(wrap(label, chars))
        py += 16

    shown = [(s, e) for s, e in m["legend"].items()
             if not (e["terrain"] == "floor" and e["name"] == "floor")]  # plain floor needs no key
    if shown:
        heading("Key")
        for symbol, entry in shown:
            out.append(tile(entry["terrain"], symbol, panel_x, py, 24, entry["color"]))
            entry_text(wrap(entry["name"], chars))

    out.append(f'<line x1="{left_w + 4}" y1="{top}" x2="{left_w + 4}" y2="{max(py, map_bottom) - PAD}" '
               f'stroke="#3a4048" stroke-width="1"/>')
    height = max(py + PAD - 7, map_bottom)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width:.0f} {height:.0f}" '
            f'width="{width:.0f}" height="{height:.0f}">'
            f'<rect width="100%" height="100%" fill="{PAGE_BG}"/>' + "".join(out) + "</svg>")


# ---------- output ----------

SHELL = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>DMC map</title>
<style>
  html, body { margin: 0; height: 100%; background: @BG@; color: @INK@;
               font-family: @FONT@; }
  #map { height: 100%; display: flex; align-items: center; justify-content: center;
         padding: 8px; box-sizing: border-box; }
  #map svg { max-width: 100%; max-height: 100%; width: auto; height: auto; }
  #stamp { position: fixed; right: 10px; bottom: 8px; font-size: 12px; color: @MUTED@;
           padding: 2px 8px; border-radius: 10px; transition: background 1.5s; }
  #stamp.fresh { background: #2e9e5b; color: #fff; transition: none; }
</style>
</head>
<body>
<div id="map">Waiting for the first map…</div>
<div id="stamp"></div>
<script>
  // live-data.js is rewritten each time DMC redraws. Pages opened from disk
  // cannot fetch files, so it is loaded as a script that calls dmcMap().
  var shown = null;
  function dmcMap(data) {
    if (data.stamp === shown) return;
    var first = shown === null;
    shown = data.stamp;
    document.getElementById('map').innerHTML = data.svg;
    document.title = data.title + ' · DMC map';
    var stamp = document.getElementById('stamp');
    stamp.textContent = 'updated ' + data.time;
    if (!first) {
      stamp.className = 'fresh';
      setTimeout(function () { stamp.className = ''; }, 400);
    }
  }
  function poll() {
    var tag = document.createElement('script');
    tag.src = 'live-data.js?' + Date.now();
    tag.onload = tag.onerror = function () { tag.remove(); };
    document.head.appendChild(tag);
  }
  poll();
  setInterval(poll, 2000);
</script>
</body>
</html>
"""


def write_png(svg, path):
    sys.path.insert(0, os.path.join(HERE, ".pylib"))
    try:
        import pymupdf  # PyMuPDF
    except ImportError:
        print("No PNG written: PyMuPDF is not installed. Run:\n"
              "  python -m pip install --target scripts/.pylib -r scripts/requirements.txt",
              file=sys.stderr)
        return False
    doc = pymupdf.open(stream=svg.encode("utf-8"), filetype="svg")
    doc[0].get_pixmap(dpi=96).save(path)
    return True


def main():
    parser = argparse.ArgumentParser(description="Draw a campaign map for the player's browser tab.")
    parser.add_argument("map", help="a place or scene JSON file")
    parser.add_argument("--png", action="store_true", help="also write live.png for DMC to look at")
    parser.add_argument("--open", action="store_true", help="open live.html in the default browser")
    parser.add_argument("--measure", nargs=2, metavar=("FROM", "TO"),
                        help="print the distance between two cells and stop")
    args = parser.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    m = check(load_map(args.map))

    if args.measure:
        a = parse_cell(args.measure[0], m["cols"], m["rows"], "--measure")
        b = parse_cell(args.measure[1], m["cols"], m["rows"], "--measure")
        print(measure_text(a, b, m["cell_ft"]))
        return

    svg = render_svg(m)
    folder = os.path.dirname(os.path.abspath(args.map))
    now = datetime.datetime.now()
    payload = {"stamp": now.isoformat(), "time": now.strftime("%H:%M:%S"),
               "title": m["title"], "svg": svg}
    with open(os.path.join(folder, "live-data.js"), "w", encoding="utf-8") as handle:
        handle.write("dmcMap(" + json.dumps(payload) + ");\n")
    page = os.path.join(folder, "live.html")
    with open(page, "w", encoding="utf-8") as handle:
        handle.write(SHELL.replace("@BG@", PAGE_BG).replace("@INK@", INK)
                     .replace("@MUTED@", MUTED).replace("@FONT@", FONT))

    print(f"Drew \"{m['title']}\": {m['cols']} x {m['rows']} cells, {len(m['tokens'])} creatures.")
    print(f"Player's page: {page}")
    if args.png:
        png = os.path.join(folder, "live.png")
        if write_png(svg, png):
            print(f"Picture to check: {png}")
    for line in distance_lines(m):
        print(line)
    if args.open:
        webbrowser.open("file:///" + page.replace(os.sep, "/").lstrip("/"))


if __name__ == "__main__":
    main()
