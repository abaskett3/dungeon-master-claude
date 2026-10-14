"""The map drawer: checking a map file, measuring it and drawing it."""
import json
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET

from support import has_pymupdf, need, run_script

import map_render

PLACE = {
    "title": "Goblin hall",
    "grid": ["##########",
             "#........#",
             "+...o...>#",
             "#........#",
             "##########"],
    "labels": [{"at": "I3", "text": "to the cellar"}],
}
SCENE = {
    "base": "goblin-hall",
    "tokens": [
        {"id": "@", "name": "Thorin", "kind": "pc", "at": "B2"},
        {"id": "1", "name": "Goblin boss", "kind": "enemy", "at": "E2"},
    ],
}


def checked(**changes):
    """A valid map with the given keys replaced, passed through check()."""
    if "grid" in changes:
        changes.setdefault("labels", [])  # the hall's label may not fit another grid
    data = dict(PLACE, **changes)
    return map_render.check(data)


def tokens(*specs):
    return [{"id": str(n), "at": at, **extra} for n, (at, extra) in enumerate(specs)]


def svg_text(svg):
    """Parse the SVG (so it must be well formed) and return all the text in it."""
    return " ".join(ET.fromstring(svg).itertext())


class CellNames(unittest.TestCase):
    def test_column_names(self):
        self.assertEqual([map_render.col_name(i) for i in (0, 1, 25, 26, 27, 51, 52, 701, 702)],
                         ["A", "B", "Z", "AA", "AB", "AZ", "BA", "ZZ", "AAA"])

    def test_cell_name(self):
        self.assertEqual(map_render.cell_name(4, 2), "E3")

    def test_parse_cell(self):
        self.assertEqual(map_render.parse_cell("E3", 10, 5, "x"), (4, 2))
        self.assertEqual(map_render.parse_cell(" e3 ", 10, 5, "x"), (4, 2))
        self.assertEqual(map_render.parse_cell("AA12", 30, 20, "x"), (26, 11))

    def test_parse_cell_is_the_reverse_of_cell_name(self):
        for col in (0, 7, 25, 26, 60):
            for row in (0, 9, 41):
                name = map_render.cell_name(col, row)
                self.assertEqual(map_render.parse_cell(name, 100, 100, "x"), (col, row))

    def test_parse_cell_rejects_nonsense(self):
        for bad in ("", "E", "3", "3E", "E-3", "E3x", "E 3"):
            with self.subTest(cell=bad):
                with self.assertRaises(SystemExit) as caught:
                    map_render.parse_cell(bad, 10, 5, "token 1")
                self.assertIn("is not a cell", str(caught.exception))

    def test_parse_cell_rejects_cells_off_the_map(self):
        for bad in ("K1", "A6", "A0"):
            with self.subTest(cell=bad):
                with self.assertRaises(SystemExit) as caught:
                    map_render.parse_cell(bad, 10, 5, "token 1")
                self.assertIn("off the map (the map is A1 to J5)", str(caught.exception))


class Checking(unittest.TestCase):
    def assertRejected(self, expected, **changes):
        with self.assertRaises(SystemExit) as caught:
            checked(**changes)
        self.assertIn(expected, str(caught.exception))

    def test_a_valid_map(self):
        m = checked(tokens=SCENE["tokens"])
        self.assertEqual((m["cols"], m["rows"]), (10, 5))
        self.assertEqual(m["title"], "Goblin hall")
        self.assertEqual(m["cell_ft"], 5)
        self.assertEqual(m["north"], "up")
        self.assertEqual([(t["id"], t["col"], t["row"]) for t in m["tokens"]], [("@", 1, 1), ("1", 4, 1)])
        self.assertEqual(m["labels"], [{"col": 8, "row": 2, "text": "to the cellar"}])

    def test_token_defaults(self):
        token = checked(tokens=[{"id": 7, "at": "b2"}])["tokens"][0]
        self.assertEqual(token, {"id": "7", "name": "7", "kind": "neutral", "size": 1, "col": 1,
                                 "row": 1, "cells": [(1, 1)], "note": "", "down": False})

    def test_a_large_token_covers_a_square_of_cells(self):
        token = checked(tokens=[{"id": "O", "at": "B2", "size": 2}])["tokens"][0]
        self.assertEqual(sorted(token["cells"]), [(1, 1), (1, 2), (2, 1), (2, 2)])

    def test_grid_must_be_rows_of_equal_length(self):
        self.assertRejected("must be a list of strings", grid="####")
        self.assertRejected("must be a list of strings", grid=[])
        self.assertRejected("first grid row is empty", grid=["", ""])
        self.assertRejected("grid row 2 is 3 cells wide but row 1 is 4", grid=["####", "#.#", "####"])

    def test_unknown_symbol(self):
        self.assertRejected('the grid uses "X"', grid=["#X#"])

    def test_legend(self):
        m = checked(grid=["#.Xo~"], legend={"X": "altar", "o": "statue",
                                              "~": {"name": "lava", "as": "hazard", "color": "#ff0000"}})
        self.assertEqual(m["legend"]["X"], {"terrain": "feature", "name": "altar", "color": None})
        self.assertEqual(m["legend"]["o"], {"terrain": "pillar", "name": "statue", "color": None})
        self.assertEqual(m["legend"]["~"], {"terrain": "hazard", "name": "lava", "color": "#ff0000"})
        self.assertEqual(m["legend"]["#"], {"terrain": "wall", "name": "wall", "color": None})

    def test_legend_mistakes(self):
        self.assertRejected("must be an object", legend=["altar"])
        self.assertRejected("must be a single character", legend={"XY": "altar"})
        self.assertRejected("must be a single character", legend={" ": "gap"})
        self.assertRejected('unknown terrain "lava"', legend={".": {"name": "lava", "as": "lava"}})
        self.assertRejected("must be a name or an object", legend={".": 5})
        self.assertRejected("needs a name", grid=["X"], legend={"X": {"as": "wall"}})

    def test_a_legend_can_turn_a_symbol_into_a_wall(self):
        self.assertRejected("which is a wall", grid=["X."], legend={"X": {"name": "cliff", "as": "wall"}},
                            tokens=[{"id": "1", "at": "A1"}])

    def test_scale_defaults_to_five_feet(self):
        self.assertEqual(checked()["cell_ft"], 5)

    def test_every_standard_scale_is_accepted(self):
        for cell_ft in map_render.SCALES:
            with self.subTest(cell_ft=cell_ft):
                self.assertEqual(checked(cell_ft=cell_ft)["cell_ft"], cell_ft)
        self.assertEqual(checked(cell_ft=5.0)["cell_ft"], 5)

    def test_a_map_not_to_scale_is_rejected(self):
        self.assertRejected("every map is drawn to scale", cell_ft=None)

    def test_a_scale_off_the_standard_list_is_rejected(self):
        for bad in (2.5, 0, 1, -5, 10, 50, 500, "5", True, False, [5]):
            with self.subTest(cell_ft=bad):
                self.assertRejected('"cell_ft" must be a standard map scale', cell_ft=bad)

    def test_any_token_may_sit_on_any_map(self):
        tokens = [{"id": "@", "at": "B2", "kind": "pc"}, {"id": "K", "at": "C2", "kind": "ally"},
                  {"id": "N", "at": "D2", "kind": "neutral"}, {"id": "1", "at": "E2", "kind": "enemy"}]
        for cell_ft in map_render.SCALES:
            with self.subTest(cell_ft=cell_ft):
                self.assertEqual(len(checked(cell_ft=cell_ft, tokens=tokens)["tokens"]), 4)

    def test_north(self):
        self.assertIsNone(checked(north=None)["north"])
        self.assertRejected('"north" must be', north="north")

    def test_token_mistakes(self):
        self.assertRejected('needs at least "id" and "at"', tokens=[{"id": "1"}])
        self.assertRejected('needs at least "id" and "at"', tokens=["B2"])
        self.assertRejected("must be one or two characters", tokens=[{"id": "abc", "at": "B2"}])
        self.assertRejected("must be one or two characters", tokens=[{"id": "", "at": "B2"}])
        self.assertRejected('two tokens share the id "1"',
                            tokens=[{"id": "1", "at": "B2"}, {"id": 1, "at": "C2"}])
        self.assertRejected('kind "monster" must be one of', tokens=[{"id": "1", "at": "B2", "kind": "monster"}])
        self.assertRejected("size must be 1, 2, 3 or 4", tokens=[{"id": "1", "at": "B2", "size": 5}])

    def test_tokens_cannot_stand_where_nothing_can(self):
        self.assertRejected("is on A1, which is a wall", tokens=[{"id": "1", "at": "A1"}])
        self.assertRejected("is on B1, which is empty space", grid=["# .", "#.."], tokens=[{"id": "1", "at": "B1"}])
        self.assertRejected("off the map", tokens=[{"id": "1", "at": "Z9"}])
        self.assertRejected("with size 2 runs off the map", grid=["..", ".."], tokens=[{"id": "1", "at": "B2", "size": 2}])
        # A large creature is checked on every cell it covers, not only its corner.
        self.assertRejected("is on C4, which is a wall", grid=["....", "....", "....", "..#."],
                            tokens=[{"id": "O", "at": "B2", "size": 3}])

    def test_tokens_cannot_share_a_cell(self):
        self.assertRejected("tokens 0 and 1 are both on B2", tokens=tokens(("B2", {}), ("B2", {})))
        self.assertRejected("tokens 0 and 1 are both on C3", tokens=tokens(("B2", {"size": 2}), ("C3", {})))

    def test_tokens_may_stand_on_doors_stairs_and_water(self):
        m = checked(grid=["+>~,_"], tokens=tokens(*[(cell, {}) for cell in ("A1", "B1", "C1", "D1", "E1")]))
        self.assertEqual(len(m["tokens"]), 5)

    def test_label_and_edge_mistakes(self):
        self.assertRejected('every label needs "at" and "text"', labels=[{"at": "B2"}])
        self.assertRejected("off the map", labels=[{"at": "Z9", "text": "x"}])
        self.assertRejected('"edges" may only have the keys', edges={"north": "road"})


class Distances(unittest.TestCase):
    def test_squares_between(self):
        self.assertEqual(map_render.squares_between([(0, 0)], [(0, 0)]), 0)
        self.assertEqual(map_render.squares_between([(0, 0)], [(3, 0)]), 3)
        self.assertEqual(map_render.squares_between([(0, 0)], [(0, 4)]), 4)
        # A diagonal step costs the same as a straight one.
        self.assertEqual(map_render.squares_between([(0, 0)], [(3, 3)]), 3)
        self.assertEqual(map_render.squares_between([(0, 0)], [(2, 5)]), 5)

    def test_big_creatures_measure_from_their_nearest_cell(self):
        ogre = [(1, 1), (2, 1), (1, 2), (2, 2)]
        self.assertEqual(map_render.squares_between(ogre, [(4, 1)]), 2)
        self.assertEqual(map_render.squares_between([(4, 1)], ogre), 2)
        self.assertEqual(map_render.squares_between(ogre, [(3, 3)]), 1)

    def test_feet(self):
        self.assertEqual(map_render.feet(3, 5), "15 ft")
        self.assertEqual(map_render.feet(0, 5), "0 ft")

    def test_overland_distances_are_given_in_miles(self):
        self.assertEqual(map_render.feet(1, 5280), "1 mile")
        self.assertEqual(map_render.feet(4, 31680), "24 miles")
        self.assertEqual(map_render.feet(0, 5280), "0 miles")
        self.assertEqual(map_render.feet(2, 316800), "120 miles")

    def test_a_5_ft_map_always_says_feet(self):
        self.assertEqual(map_render.feet(2000, 5), "10000 ft")

    def test_mile_scale_distances_are_straight_lines(self):
        # B2 to E5 is 3 squares across and 3 down: 4.24 squares, 25.5 miles at 6 miles a square.
        b2, e5 = [(1, 1)], [(4, 4)]
        self.assertEqual(map_render.distance(b2, e5, 31680), "25 miles")
        self.assertEqual(map_render.distance(b2, [(4, 1)], 31680), "18 miles")
        self.assertEqual(map_render.distance(b2, [(2, 2)], 5280), "1 mile")  # 1.41 miles
        # At 5 ft a diagonal costs the same as a straight step.
        self.assertEqual(map_render.distance(b2, e5, 5), "15 ft")

    def test_mile_scale_distances_measure_between_the_nearest_cells(self):
        ogre = [(1, 1), (2, 1), (1, 2), (2, 2)]
        self.assertEqual(map_render.distance(ogre, [(5, 1)], 5280), "3 miles")

    def test_distance_lines(self):
        m = checked(tokens=SCENE["tokens"] + [{"id": "O", "name": "Ogre", "at": "G2", "size": 2}])
        lines = map_render.distance_lines(m)
        self.assertEqual(len(lines), 4)  # a heading and one line for each pair
        self.assertIn("1 cell = 5 ft", lines[0])
        self.assertEqual(lines[1], "  @ Thorin (B2)  <->  1 Goblin boss (E2):  15 ft")
        self.assertEqual(lines[2], "  @ Thorin (B2)  <->  O Ogre (G2):  25 ft")
        self.assertEqual(lines[3], "  1 Goblin boss (E2)  <->  O Ogre (G2):  10 ft")

    def test_distance_lines_use_the_cell_size(self):
        m = checked(cell_ft=31680, tokens=SCENE["tokens"])
        self.assertTrue(map_render.distance_lines(m)[1].endswith(":  18 miles"))

    def test_no_distances_without_two_creatures(self):
        self.assertEqual(map_render.distance_lines(checked()), [])
        self.assertEqual(map_render.distance_lines(checked(tokens=SCENE["tokens"][:1])), [])

    def test_distance_lines_are_printed_at_every_scale_for_two_or_more_creatures(self):
        pair = [{"id": "@", "at": "B2", "kind": "pc"}, {"id": "K", "at": "E2", "kind": "ally"}]
        for cell_ft in map_render.SCALES:
            with self.subTest(cell_ft=cell_ft):
                lines = map_render.distance_lines(checked(cell_ft=cell_ft, tokens=pair))
                self.assertEqual(len(lines), 2)
                self.assertTrue(lines[1].endswith(f":  {map_render.feet(3, cell_ft)}"), lines)

    def test_the_distance_heading_gives_the_scale_once_and_drops_combat_clauses_in_miles(self):
        pair = [{"id": "@", "at": "B2", "kind": "pc"}, {"id": "K", "at": "E4", "kind": "ally"}]
        miles = map_render.distance_lines(checked(cell_ft=31680, tokens=pair))
        self.assertEqual(miles[0], "Distances (1 cell = 6 miles, straight line):")
        self.assertTrue(miles[1].endswith(":  22 miles"), miles)  # 3 across, 2 down: 3.6 squares
        feet = map_render.distance_lines(checked(tokens=pair))
        self.assertEqual(feet[0], "Distances (1 cell = 5 ft, straight line, walls ignored; 5 ft is adjacent):")


class Drawing(unittest.TestCase):
    def test_wrap(self):
        self.assertEqual(map_render.wrap("one two three four", 9), ["one two", "three", "four"])
        self.assertEqual(map_render.wrap("short", 20), ["short"])
        self.assertEqual(map_render.wrap("", 20), [""])
        self.assertEqual(map_render.wrap("unbreakablyLongWord x", 5), ["unbreakablyLongWord", "x"])

    def test_the_picture_is_well_formed_and_names_everything(self):
        m = checked(tokens=SCENE["tokens"], notes="Smoke hangs under the rafters.",
                    edges={"top": "to the yard", "bottom": "cliff", "left": "road", "right": "river"},
                    scale_note="Ceiling 10 ft")
        words = svg_text(map_render.render_svg(m))
        for expected in ("Goblin hall", "Smoke hangs", "Thorin", "Goblin boss", "to the cellar", "to the yard",
                         "cliff", "road", "river", "1 square = 5 ft. Ceiling 10 ft", "ON THE MAP", "KEY",
                         "pillar", "stairs down", "door"):
            self.assertIn(expected, words)

    def test_every_terrain_draws(self):
        grid = ["#.+=~^><To_,?X"]
        m = checked(grid=grid, legend={"X": "altar"})
        self.assertEqual({e["terrain"] for e in m["legend"].values()}, map_render.TERRAINS)
        words = svg_text(map_render.render_svg(m))
        self.assertIn("altar", words)
        self.assertNotIn("ON THE MAP", words)

    def test_text_from_the_map_file_is_escaped(self):
        m = checked(title='Tom & Jerry\'s <"den">', legend={"o": "<b>pillar</b>"},
                    tokens=[{"id": "<", "name": "A & B", "at": "B2", "note": '"prone" <1>'}],
                    labels=[{"at": "C3", "text": "<script>"}])
        svg = map_render.render_svg(m)
        self.assertNotIn("<script>", svg)
        words = svg_text(svg)
        for expected in ('Tom & Jerry\'s <"den">', "<b>pillar</b>", "A & B", '"prone" <1>', "<script>"):
            self.assertIn(expected, words)

    def test_a_map_with_no_north_has_no_compass(self):
        words = svg_text(map_render.render_svg(checked(north=None)))
        self.assertNotIn(" N ", f" {words} ")

    def test_every_picture_has_a_scale_bar_and_says_its_scale(self):
        said = {5: "1 square = 5 ft", 5280: "1 square = 1 mile",
                31680: "1 square = 6 miles", 316800: "1 square = 60 miles"}
        for cell_ft in map_render.SCALES:
            with self.subTest(cell_ft=cell_ft):
                svg = map_render.render_svg(checked(cell_ft=cell_ft))
                self.assertIn(f'v8 h{map_render.CELL} v-8"', svg)
                self.assertIn(said[cell_ft], svg_text(svg))
                self.assertNotIn("Not to scale", svg)

    def test_large_and_fallen_creatures_are_described(self):
        m = checked(tokens=[{"id": "O", "name": "Ogre", "at": "B2", "size": 2, "note": "prone", "down": True},
                            {"id": "G2", "name": "Goblin", "at": "F2"}])
        svg = map_render.render_svg(m)
        self.assertIn("(2×2)", svg_text(svg))
        self.assertIn('opacity="0.45"', svg)

    def test_the_picture_grows_with_the_map(self):
        def size(grid):
            root = ET.fromstring(map_render.render_svg(checked(grid=grid, labels=[])))
            return float(root.get("width")), float(root.get("height"))
        small, wide, tall = size(["." * 10] * 10), size(["." * 30] * 10), size(["." * 10] * 30)
        self.assertEqual(wide[0] - small[0], 20 * map_render.CELL)
        self.assertEqual(tall[1] - small[1], 20 * map_render.CELL)


class CommandLine(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.folder = self.tmp.name
        self.place = self.write("goblin-hall.json", PLACE)
        self.scene = self.write("live.json", SCENE)

    def write(self, name, data):
        path = os.path.join(self.folder, name)
        with open(path, "w", encoding="utf-8") as handle:
            if isinstance(data, str):
                handle.write(data)
            else:
                json.dump(data, handle)
        return path

    def outputs(self):
        return sorted(set(os.listdir(self.folder)) - {"goblin-hall.json", "live.json", "overland.json", "old.json"})

    def scene_on(self, place, **scene):
        """Write a scene on a place file saved with its own scale, and return its path."""
        self.write(place + ".json", dict(PLACE, cell_ft={"overland": 31680, "old": None}[place]))
        self.write("live.json", dict(scene, base=place))
        return self.scene

    def assertFails(self, expected, *args):
        before = self.outputs()
        result = run_script("map_render.py", *args)
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("map_render: ", result.stderr)
        self.assertIn(expected, result.stderr)
        self.assertEqual(self.outputs(), before, "a rejected map must not be drawn")

    def test_draws_a_scene_on_its_base(self):
        result = run_script("map_render.py", self.scene)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('Drew "Goblin hall": 10 x 5 cells, 2 creatures.', result.stdout)
        self.assertIn("@ Thorin (B2)  <->  1 Goblin boss (E2):  15 ft", result.stdout)
        self.assertEqual(self.outputs(), ["live-data.js", "live.html"])

        with open(os.path.join(self.folder, "live-data.js"), encoding="utf-8") as handle:
            script = handle.read()
        self.assertTrue(script.startswith("dmcMap(") and script.endswith(");\n"))
        payload = json.loads(script[len("dmcMap("):-len(");\n")])
        self.assertEqual(sorted(payload), ["stamp", "svg", "time", "title"])
        self.assertEqual(payload["title"], "Goblin hall")
        self.assertIn("Goblin boss", svg_text(payload["svg"]))

        with open(os.path.join(self.folder, "live.html"), encoding="utf-8") as handle:
            page = handle.read()
        self.assertIn("live-data.js", page)
        self.assertIn("function dmcMap(", page)
        self.assertNotRegex(page, r"@[A-Z]+@", "a placeholder was left in the page")

    def test_draws_a_place_with_no_creatures(self):
        result = run_script("map_render.py", self.place)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("0 creatures", result.stdout)
        self.assertNotIn("Distances", result.stdout)

    def test_each_run_has_a_new_stamp(self):
        stamps = []
        for _ in range(2):
            run_script("map_render.py", self.scene)
            with open(os.path.join(self.folder, "live-data.js"), encoding="utf-8") as handle:
                stamps.append(json.loads(handle.read()[len("dmcMap("):-len(");\n")])["stamp"])
        self.assertNotEqual(stamps[0], stamps[1], "the page only redraws when the stamp changes")

    def test_a_scene_overrides_its_base(self):
        self.write("live.json", dict(SCENE, title="Goblin hall, on fire"))
        result = run_script("map_render.py", self.scene)
        self.assertIn('Drew "Goblin hall, on fire"', result.stdout)
        self.assertIn(":  15 ft", result.stdout)

    def test_a_scene_cannot_change_its_bases_scale(self):
        for cell_ft in (31680, 5280, None):
            with self.subTest(cell_ft=cell_ft):
                self.write("live.json", dict(SCENE, cell_ft=cell_ft))
                self.assertFails('sets "cell_ft": %s, but its base "goblin-hall" is drawn at 5: '
                                 "a scene cannot change its place's scale" % cell_ft, self.scene)
        self.write("overland.json", dict(PLACE, cell_ft=31680))
        self.write("live.json", {"base": "overland", "cell_ft": 5})
        self.assertFails('but its base "overland" is drawn at 31680', self.scene)

    def test_a_scene_may_repeat_its_bases_scale(self):
        self.write("live.json", dict(SCENE, cell_ft=5))
        self.assertEqual(run_script("map_render.py", self.scene).returncode, 0)
        self.write("overland.json", dict(PLACE, cell_ft=31680))
        self.write("live.json", {"base": "overland", "cell_ft": 31680})
        self.assertEqual(run_script("map_render.py", self.scene).returncode, 0)

    def test_measure_draws_nothing(self):
        result = run_script("map_render.py", self.scene, "--measure", "b2", "E4")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "B2 to E4: 3 squares, 15 ft (straight line, walls ignored)")
        self.assertEqual(self.outputs(), [])

    def test_measure_mistakes(self):
        self.assertFails("off the map", self.scene, "--measure", "B2", "Z99")
        self.assertFails("every map is drawn to scale", self.scene_on("old"), "--measure", "B2", "E4")

    def test_measure_on_a_mile_scale_map_is_a_straight_line(self):
        scene = self.scene_on("overland")
        result = run_script("map_render.py", scene, "--measure", "B2", "E5")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "B2 to E5: about 25 miles (straight line)")
        self.assertEqual(self.outputs(), [])

    def test_a_map_not_to_scale_is_not_drawn(self):
        self.assertFails('"cell_ft" must be a standard map scale', self.scene_on("old"))

    def test_a_fight_and_a_party_are_drawn_on_a_mile_scale_map(self):
        scene = self.scene_on("overland", tokens=[
            {"id": "@", "name": "Thorin", "kind": "pc", "at": "B2"},
            {"id": "1", "name": "Wyvern", "kind": "enemy", "at": "G2"}])
        result = run_script("map_render.py", scene)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Distances (1 cell = 6 miles, straight line):", result.stdout)
        self.assertIn("@ Thorin (B2)  <->  1 Wyvern (G2):  30 miles", result.stdout)

    def test_png(self):
        need(has_pymupdf(), "PyMuPDF")
        result = run_script("map_render.py", self.scene, "--png")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Picture to check:", result.stdout)
        with open(os.path.join(self.folder, "live.png"), "rb") as handle:
            self.assertEqual(handle.read(8), b"\x89PNG\r\n\x1a\n")

    def test_non_ascii_names_survive(self):
        self.write("live.json", dict(SCENE, title="Thrór's hall — 北"))
        result = run_script("map_render.py", self.scene, env=dict(os.environ, PYTHONIOENCODING="ascii"))
        self.assertEqual(result.returncode, 0, result.stderr)
        with open(os.path.join(self.folder, "live-data.js"), encoding="utf-8") as handle:
            payload = json.loads(handle.read()[len("dmcMap("):-len(");\n")])
        self.assertEqual(payload["title"], "Thrór's hall — 北")

    def test_file_mistakes(self):
        self.assertFails("no such file", os.path.join(self.folder, "missing.json"))
        self.assertFails("is not valid JSON", self.write("broken.json", '{"grid": ['))
        self.assertFails("must hold a JSON object", self.write("list.json", "[]"))
        self.assertFails('has no "grid" and no "base"', self.write("empty.json", {"title": "x"}))
        self.assertFails("which does not exist", self.write("orphan.json", {"base": "nowhere"}))
        self.assertFails("base must be a place file", self.write("chain.json", {"base": "live"}))

    def test_a_bad_scene_is_rejected(self):
        self.write("live.json", dict(SCENE, tokens=[{"id": "@", "name": "Thorin", "at": "A1"}]))
        self.assertFails("token @ (Thorin) is on A1, which is a wall", self.scene)


if __name__ == "__main__":
    unittest.main()
