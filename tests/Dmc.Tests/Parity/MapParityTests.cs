using System.Text;
using System.Text.RegularExpressions;
using Dmc.Maps;
using Dmc.Tests.Map;
using Dmc.Tests.Support;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Parity;

/// <summary>
/// <c>dmc map</c> against <c>scripts/map_render.py</c> on the same files: the fixtures of the Python tests, a map
/// at every scale, tokens sharing cells, scene and base merging, <c>--measure</c> at every scale, every error, and
/// broken JSON. stdout, stderr, the exit code and the files written must be identical, with these exceptions,
/// each one deliberate and narrow (see <see cref="MapScript"/>):
/// <list type="bullet">
/// <item>the error prefix, "map_render: " in the script and "dmc map: " in dmc;</item>
/// <item>the usage text argparse prints before an argument error, which dmc leaves out (one line only);</item>
/// <item>the stamp and the clock time in <c>live-data.js</c>;</item>
/// <item>the contents of <c>live.png</c>: dmc draws it at the size of the SVG, the script a third bigger.
///   <see cref="ThePngIsTheSizeOfTheSvg"/> checks dmc's size.</item>
/// </list>
/// </summary>
[TestClass]
public sealed partial class MapParityTests
{
    [GeneratedRegex(@"^usage: map_render\.py .*\n( {5,}.*\n)*", RegexOptions.Multiline)]
    private static partial Regex ArgparseUsage();

    [GeneratedRegex(@"^dmcMap\(\{""stamp"": ""[^""]*"", ""time"": ""[^""]*""")]
    private static partial Regex Stamp();

    static readonly ParityTool MapScript = new("map_render.py", "map",
        (work, args, stdin) =>
        {
            string? python = Legacy.Python();
            TestEnv.Need(python is not null, "Python 3");
            return TestEnv.Run(python!, [Legacy.Script("map_render.py"), .. args], stdin, work,
                environment: new Dictionary<string, string> { ["PYTHONIOENCODING"] = "utf-8" });
        },
        Normalize: text =>
        {
            text = ArgparseUsage().Replace(text, "");
            return text.Replace("map_render.py: error: ", "dmc map: ").Replace("map_render: ", "dmc map: ");
        },
        NormalizeFile: (_, text) => Stamp().Replace(text, "dmcMap({\"stamp\": \"<stamp>\", \"time\": \"<time>\""),
        SkipContents: name => name.EndsWith(".png", StringComparison.Ordinal));

    // ---------- building cases ----------

    static string Text(object? value) => value as string ?? Json(value);

    /// <summary>A case with these files in the working folder and these arguments (file names are relative to it).</summary>
    static ParityCase Files(string name, (string Name, object? Content)[] files, params string[] args) =>
        new(name, _ => args, null, work =>
        {
            foreach (var (file, content) in files)
            {
                string path = Path.Combine(work, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, Text(content), new UTF8Encoding(false));
            }
        });

    /// <summary>The goblin hall as the place file, <paramref name="scene"/> as live.json, and <c>dmc map live.json</c>.</summary>
    static ParityCase Scene(string name, PyDict scene, params string[] args) =>
        Files(name, [("goblin-hall.json", Place()), ("live.json", scene)], ["live.json", .. args]);

    /// <summary>One file, live.json, a map of its own.</summary>
    static ParityCase Alone(string name, PyDict map, params string[] args) => Files(name, [("live.json", map)], ["live.json", .. args]);

    static PyDict Hall(params (string, object?)[] changes)
    {
        var map = Place();
        foreach (var (key, value) in changes)
            map[key] = value is int i ? (long)i : value;
        return map;
    }

    static PyDict OnHall(params (string, object?)[] changes)
    {
        var scene = Scene_();
        foreach (var (key, value) in changes)
            scene[key] = value is int i ? (long)i : value;
        return scene;
    }

    static PyDict Scene_() => MapFixtures.Scene();

    static object?[] Pair => [Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
                              Obj(("id", "1"), ("name", "Goblin boss"), ("kind", "enemy"), ("at", "E2"))];

    static List<object?> Crowd(int count, string at = "B2") =>
        Enumerable.Range(0, count).Select(n => (object?)Obj(("id", n.ToString()), ("name", $"Orc{n}"),
            ("kind", n % 2 == 0 ? "enemy" : "ally"), ("at", at))).ToList();

    // ---------- the cases ----------

    static IEnumerable<ParityCase> Drawn()
    {
        yield return Scene("a scene on its base", Scene_());
        yield return Files("a place with no creatures", [("goblin-hall.json", Place())], "goblin-hall.json");
        yield return Scene("a scene overrides its base", OnHall(("title", "Goblin hall, on fire")));
        yield return Scene("a scene may repeat its base's scale", OnHall(("cell_ft", 5)));
        yield return Scene("a scene drawn with a png", Scene_(), "--png");
        yield return new ParityCase("an absolute path", work => [Path.Combine(work, "live.json")], null, work =>
        {
            File.WriteAllText(Path.Combine(work, "goblin-hall.json"), PyJson.Dumps(Place()));
            File.WriteAllText(Path.Combine(work, "live.json"), PyJson.Dumps(Scene_()));
        });

        yield return Alone("everything on one map", Hall(
            ("notes", "Smoke hangs under the rafters, and a note long enough that it has to wrap onto a second line of the picture."),
            ("edges", Obj(("top", "to the yard and a lot more words to make this wrap around"), ("bottom", "cliff of about 400 ft of open snow, which is long"),
                          ("left", "the road to the old mill by the river"), ("right", "river"))),
            ("scale_note", "Ceiling 10 ft, and a long remark that wraps around the scale bar"),
            ("legend", Obj(("o", "statue"), ("~", Obj(("name", "lava"), ("as", "hazard"), ("color", "#ff0000"))), ("#", Obj(("name", "cliff"), ("color", "#334455"))))),
            ("tokens", Arr(
                Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
                Obj(("id", "1"), ("name", "Goblin boss"), ("kind", "enemy"), ("at", "E2"), ("note", "bloodied")),
                Obj(("id", "O"), ("name", "Ogre"), ("kind", "enemy"), ("at", "G2"), ("size", 2), ("note", "prone"), ("down", true)),
                Obj(("id", "K2"), ("name", "Kira"), ("kind", "ally"), ("at", "C4")),
                Obj(("id", 7), ("at", "D4"))))));

        yield return Alone("every terrain", Hall(("grid", Arr("#.+=~^><To_,?X", "..............")), ("labels", Arr()),
            ("legend", Obj(("X", "altar")))));
        yield return Alone("every terrain with colours", Hall(("grid", Arr("#.+=~^><To_,?XY")), ("labels", Arr()),
            ("legend", Obj(("#", Obj(("name", "w"), ("color", "#111111"))), (".", Obj(("name", "f"), ("color", "#222222"))),
                           ("+", Obj(("name", "d"), ("color", "#333333"))), ("=", Obj(("name", "win"), ("color", "#444444"))),
                           ("~", Obj(("name", "wat"), ("color", "#555555"))), ("^", Obj(("name", "haz"), ("color", "#666666"))),
                           (">", Obj(("name", "sd"), ("color", "#777777"))), ("T", Obj(("name", "tr"), ("color", "#888888"))),
                           ("o", Obj(("name", "pi"), ("color", "#999999"))), ("_", Obj(("name", "co"), ("color", "#aaaaaa"))),
                           (",", Obj(("name", "di"), ("color", "#bbbbbb"))), ("?", Obj(("name", "un"), ("color", "#cccccc"))),
                           ("X", Obj(("name", "ft"), ("color", "#dddddd"))), ("Y", Obj(("name", "ft"), ("as", "wall"), ("color", ""))))))
            );
        yield return Alone("a legend that renames floor", Hall(("legend", Obj((".", "the dance floor")))));
        yield return Alone("a legend entry with falsy colour and numbers", Hall(("legend", Obj(("o", Obj(("name", 5), ("color", 0))), ("~", Obj(("name", "x"), ("color", 7)))))));

        foreach (int cellFt in MapRules.Scales)
        {
            yield return Scene($"two creatures at {cellFt} ft", OnHall(("tokens", Arr(Pair))), "--png").WithPlace(Hall(("cell_ft", cellFt)));
            yield return Scene($"three creatures, one big, at {cellFt} ft", OnHall(("tokens", Arr(Pair[0], Pair[1],
                Obj(("id", "O"), ("name", "Ogre"), ("at", "G2"), ("size", 3), ("kind", "enemy"))))),
                []).WithPlace(Hall(("cell_ft", cellFt)));
            yield return Scene($"a bare place at {cellFt} ft", OnHall(), []).WithPlace(Hall(("cell_ft", cellFt), ("scale_note", "a note")));
        }

        foreach (int count in new[] { 2, 3, 4, 5, 6, 10, 20 })
        {
            foreach (int cellFt in new[] { 30, 31680 })
                yield return Scene($"{count} creatures share a cell at {cellFt} ft", OnHall(("tokens", Crowd(count))))
                    .WithPlace(Hall(("cell_ft", cellFt)));
        }
        yield return Scene("two crowded cells and a loner at 30 ft", OnHall(("tokens", Crowd(6, "B2").Concat(Crowd(5, "E3").Select((t, n) =>
            (object?)Obj(("id", $"x{n}"), ("name", $"Imp{n}"), ("at", "E3")))).Append(Obj(("id", "L"), ("name", "Loner"), ("at", "H4"))).ToList())))
            .WithPlace(Hall(("cell_ft", 30)));
        yield return Scene("a down token in a crowd and a big creature at 30 ft", OnHall(("tokens", Arr(
            Obj(("id", "a"), ("at", "B2"), ("down", true), ("note", "dead")), Obj(("id", "b"), ("at", "B2")),
            Obj(("id", "c"), ("at", "B2"), ("size", 4), ("name", "Dragon"))))))
            .WithPlace(Hall(("cell_ft", 30)));

        yield return Scene("a size written as a float on a coarse map", OnHall(("tokens", Arr(Obj(("id", "O"), ("at", "B2"), ("size", 2.0)))))).WithPlace(Hall(("cell_ft", 30)));
        yield return Scene("a scale written as a float", OnHall(("tokens", Arr(Pair)))).WithPlace(Hall(("cell_ft", 30.0)));
        yield return Scene("a boolean size of one", OnHall(("tokens", Arr(Obj(("id", "O"), ("at", "B2"), ("size", true)))))).WithPlace(Hall());

        foreach (var north in new object?[] { null, "up", "down", "left", "right" })
            yield return Alone($"north is {north ?? "null"}", Hall(("north", north)));

        yield return Alone("a wide map with two-letter columns", Hall(("grid", Arr(Enumerable.Repeat(new string('.', 30), 4).ToArray())),
            ("labels", Arr(Obj(("at", "AD4"), ("text", "far end")))),
            ("tokens", Arr(Obj(("id", "@"), ("at", "AA2")), Obj(("id", "e"), ("at", "AD3"), ("kind", "enemy"))))));
        yield return Alone("a tall map", Hall(("grid", Arr(Enumerable.Repeat(".....", 40).ToArray())), ("labels", Arr()),
            ("tokens", Arr(Obj(("id", "@"), ("at", "A1")), Obj(("id", "e"), ("at", "E40"), ("kind", "enemy"))))));
        yield return Alone("a one-cell map", Hall(("grid", Arr(".")), ("labels", Arr())));
        yield return Alone("a map with empty space and a gap", Hall(("grid", Arr("..  ..", " .... ", "..  ..")), ("labels", Arr()),
            ("tokens", Arr(Obj(("id", "@"), ("at", "B2"))))));
        yield return Alone("a large creature on 5 ft squares", Hall(("tokens", Arr(
            Obj(("id", "T"), ("name", "Troll"), ("kind", "enemy"), ("at", "B2"), ("size", 3)),
            Obj(("id", "@"), ("at", "F2"), ("kind", "pc"))))));
        yield return Alone("a gargantuan creature", Hall(("grid", Arr(Enumerable.Repeat("..........", 6).ToArray())), ("labels", Arr()),
            ("tokens", Arr(Obj(("id", "G"), ("name", "Dragon"), ("kind", "enemy"), ("at", "C2"), ("size", 4)), Obj(("id", "@"), ("at", "A1"))))));

        yield return Alone("text that needs escaping", Hall(("title", "Tom & Jerry's <\"den\">"), ("legend", Obj(("o", "<b>pillar</b>"))),
            ("tokens", Arr(Obj(("id", "<"), ("name", "A & B"), ("at", "B2"), ("note", "\"prone\" <1>")))),
            ("labels", Arr(Obj(("at", "C3"), ("text", "<script>"))))));
        yield return Alone("non-ASCII text", Hall(("title", "Thr\u00f3r's hall \u2014 \u5317 \ud83d\ude00"), ("notes", "caf\u00e9 \u00fcber"),
            ("tokens", Arr(Obj(("id", "\u00e9"), ("name", "Ren\u00e9e"), ("at", "B2"), ("note", "\u2603"))))));
        yield return Alone("an emoji symbol in the grid", Hall(("grid", Arr("#\ud83d\ude00.", "#...")), ("labels", Arr()),
            ("legend", Obj(("\ud83d\ude00", "a smiley")))));
        yield return Alone("a missing title", Hall(("title", null)));
        yield return Alone("a title that is a number", Hall(("title", 5)));
        yield return Alone("notes and a scale note that are not text", Hall(("notes", 12), ("scale_note", 1.5)));
        yield return Alone("ids and names that are numbers", Hall(("tokens", Arr(Obj(("id", 1), ("name", 2.5), ("at", "B2")),
            Obj(("id", true), ("at", "C2")), Obj(("id", "ab"), ("name", Arr(1, "x")), ("at", "D2"), ("note", null))))));
        yield return Alone("cells written loosely", Hall(("tokens", Arr(Obj(("id", "1"), ("at", " b2 ")), Obj(("id", "2"), ("at", "c3"))))));
        yield return Alone("duplicate keys keep the last value", Hall()).WithFile("live.json",
            "{\"title\": \"first\", \"grid\": [\"..\"], \"title\": \"last\"}");
        yield return Alone("a scene with its own grid and a base", OnHall(("grid", Arr("...", "...")), ("tokens", Arr(Obj(("id", "@"), ("at", "B2"))))))
            .WithPlace(Hall());
        yield return Files("a scene may hold its own grid", [("live.json", Obj(("grid", Arr("...")), ("title", "own")))], "live.json");
        yield return Files("a file with CRLF line endings", [("live.json", "{\r\n  \"grid\": [\r\n    \"..\"\r\n  ]\r\n}\r\n")], "live.json");
        yield return Files("a file with a lone CR", [("live.json", "{\r  \"grid\": [\"..\"]\r}")], "live.json");
        yield return Scene("a label wider than its cell", OnHall(("labels", Arr(Obj(("at", "A1"), ("text", "a long label that runs off the edge of the cell"))))));
        yield return Scene("many labels", OnHall(("labels", Arr(Obj(("at", "B2"), ("text", "x")), Obj(("at", "C2"), ("text", "twelve chars")), Obj(("at", "D2"), ("text", ""))))));
        yield return Scene("a falsy base", Obj(("base", ""), ("grid", Arr("..."))));
        yield return Alone("a plain png of a map with notes", Hall(("notes", "n")), "--png");
        yield return Scene("--png with creatures at 30 ft", OnHall(("tokens", Crowd(6))), "--png").WithPlace(Hall(("cell_ft", 30)));
        yield return Scene("png abbreviated", Scene_(), "--pn");
    }

    static IEnumerable<ParityCase> Measured()
    {
        foreach (int cellFt in MapRules.Scales)
        {
            foreach (var (from, to) in new[] { ("B2", "E4"), ("B2", "b2"), ("A1", "J5"), ("b2", "C3"), ("E4", "B2"), (" b2", "e4 "), ("A3", "J3"), ("B2", "B5") })
                yield return Scene($"measure {from} to {to} at {cellFt} ft", Scene_(), "--measure", from, to).WithPlace(Hall(("cell_ft", cellFt)));
        }
        yield return Scene("measure with the options in front", Scene_(), "--measure", "B2", "E4", "--png");
        yield return Scene("measure to the same cell on a 5 ft map", Scene_(), "--measure", "B2", "B2");
        yield return Scene("measure one square", Scene_(), "--measure", "B2", "C3");
        yield return Scene("measure draws nothing even with --png", Scene_(), "--png", "--measure", "B2", "C3");
        yield return Alone("measure on a big map", Hall(("grid", Arr(Enumerable.Repeat(new string('.', 40), 60).ToArray())), ("labels", Arr()), ("cell_ft", 5280)),
            "--measure", "A1", "AN60");
        yield return Alone("measure on a very big map", Hall(("grid", Arr(Enumerable.Repeat(new string('.', 40), 60).ToArray())), ("labels", Arr()), ("cell_ft", 316800)),
            "--measure", "A1", "AN60");
        yield return Alone("measure far on a 30 ft map", Hall(("grid", Arr(Enumerable.Repeat(new string('.', 40), 60).ToArray())), ("labels", Arr()), ("cell_ft", 30)),
            "--measure", "A1", "AN60");
        yield return Scene("measure off the map", Scene_(), "--measure", "B2", "Z99");
        yield return Scene("measure from off the map", Scene_(), "--measure", "K1", "B2");
        yield return Scene("measure row zero", Scene_(), "--measure", "A0", "B2");
        yield return Scene("measure a word", Scene_(), "--measure", "here", "B2");
        yield return Scene("measure an empty cell", Scene_(), "--measure", "", "B2");
        yield return Scene("measure digits only", Scene_(), "--measure", "12", "B2");
        yield return Scene("measure letters only", Scene_(), "--measure", "B2", "E");
        yield return Scene("measure a negative row", Scene_(), "--measure", "B-2", "E3");
        yield return Scene("measure a huge column", Scene_(), "--measure", "ZZZZZZZZZZZZZZZZZZZZ1", "B2");
        yield return Scene("measure a huge row", Scene_(), "--measure", "B99999999999999999999", "B2");
        yield return Scene("measure after a scale error", Obj(("base", "goblin-hall"), ("cell_ft", 7)), "--measure", "B2", "C3");
    }

    static IEnumerable<ParityCase> Rejected()
    {
        // The messages the Python tests look for, one by one.
        (string Name, PyDict Map)[] maps =
        [
            ("grid is a string", Hall(("grid", "####"))),
            ("grid is empty", Hall(("grid", Arr()))),
            ("grid is null", Hall(("grid", null))),
            ("grid has a number", Hall(("grid", Arr("..", 5)))),
            ("first row is empty", Hall(("grid", Arr("", "")))),
            ("rows of different length", Hall(("grid", Arr("####", "#.#", "####")))),
            ("an unknown symbol", Hall(("grid", Arr("#X#")), ("labels", Arr()))),
            ("legend is a list", Hall(("legend", Arr("altar")))),
            ("legend is null", Hall(("legend", null))),
            ("legend key too long", Hall(("legend", Obj(("XY", "altar"))))),
            ("legend key is a space", Hall(("legend", Obj((" ", "gap"))))),
            ("unknown terrain", Hall(("legend", Obj((".", Obj(("name", "lava"), ("as", "lava"))))))),
            ("terrain is a number", Hall(("legend", Obj((".", Obj(("as", 5))))))),
            ("legend entry is a number", Hall(("legend", Obj((".", 5))))),
            ("legend entry needs a name", Hall(("grid", Arr("X")), ("labels", Arr()), ("legend", Obj(("X", Obj(("as", "wall"))))))),
            ("legend entry with an empty name", Hall(("legend", Obj((".", ""))))),
            ("a symbol turned into a wall", Hall(("grid", Arr("X.")), ("labels", Arr()), ("legend", Obj(("X", Obj(("name", "cliff"), ("as", "wall"))))),
                ("tokens", Arr(Obj(("id", "1"), ("at", "A1")))))),
            ("north is a word", Hall(("north", "north"))),
            ("a token without at", Hall(("tokens", Arr(Obj(("id", "1")))))),
            ("a token that is a string", Hall(("tokens", Arr("B2")))),
            ("a long id", Hall(("tokens", Arr(Obj(("id", "abc"), ("at", "B2")))))),
            ("an empty id", Hall(("tokens", Arr(Obj(("id", ""), ("at", "B2")))))),
            ("a shared id", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2")), Obj(("id", 1), ("at", "C2")))))),
            ("a bad kind", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("kind", "monster")))))),
            ("a kind that is a number", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("kind", 3)))))),
            ("a bad size", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("size", 5)))))),
            ("a size of zero", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("size", 0)))))),
            ("a size that is text", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("size", "2")))))),
            ("a token in a wall", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "A1")))))),
            ("a token in empty space", Hall(("grid", Arr("# .", "#..")), ("labels", Arr()), ("tokens", Arr(Obj(("id", "1"), ("at", "B1")))))),
            ("a token off the map", Hall(("tokens", Arr(Obj(("id", "1"), ("at", "Z9")))))),
            ("a token whose at is a number", Hall(("tokens", Arr(Obj(("id", "1"), ("at", 5)))))),
            ("a large token off the edge", Hall(("grid", Arr("..", "..")), ("labels", Arr()), ("tokens", Arr(Obj(("id", "1"), ("at", "B2"), ("size", 2)))))),
            ("a large token on a wall", Hall(("grid", Arr("....", "....", "....", "..#.")), ("labels", Arr()), ("tokens", Arr(Obj(("id", "O"), ("at", "B2"), ("size", 3)))))),
            ("two tokens on a cell", Hall(("tokens", Arr(Obj(("id", "0"), ("at", "B2")), Obj(("id", "1"), ("at", "B2")))))),
            ("a large token over another", Hall(("tokens", Arr(Obj(("id", "0"), ("at", "B2"), ("size", 2)), Obj(("id", "1"), ("at", "C3")))))),
            ("tokens is an object", Hall(("tokens", Obj(("a", 1))))),
            ("tokens is a string", Hall(("tokens", "ab"))),
            ("tokens is an empty object", Hall(("tokens", Obj()))),
            ("a label without text", Hall(("labels", Arr(Obj(("at", "B2")))))),
            ("a label off the map", Hall(("labels", Arr(Obj(("at", "Z9"), ("text", "x")))))),
            ("a label at nonsense", Hall(("labels", Arr(Obj(("at", "nowhere"), ("text", 12)))))),
            ("edges with a bad key", Hall(("edges", Obj(("north", "road"))))),
            ("edges is a list", Hall(("edges", Arr()))),
            ("scale is null", Hall(("cell_ft", null))),
            ("scale is 2.5", Hall(("cell_ft", 2.5))),
            ("scale is 10", Hall(("cell_ft", 10))),
            ("scale is text", Hall(("cell_ft", "5"))),
            ("scale is true", Hall(("cell_ft", true))),
            ("scale is a list", Hall(("cell_ft", Arr(5)))),
            ("scale is an object", Hall(("cell_ft", Obj(("a", Arr(1, null, "é")))))),
            ("no grid", Obj(("title", "x"))),
            ("an orphan base", Obj(("base", "nowhere"))),
            ("a chained base", Obj(("base", "live"))),
            ("a self base", Obj(("base", "chain"))),
        ];
        foreach (var (name, map) in maps)
            yield return Alone("rejects: " + name, map);

        // Scales and bases, where the file named in the message matters.
        foreach (int cellFt in new[] { 30, 31680, 5280 })
            yield return Scene($"a scene cannot change its base's scale ({cellFt})", Obj(("base", "goblin-hall"), ("cell_ft", cellFt),
                ("tokens", Arr(Pair))));
        yield return Files("a scene cannot change an overland base", [("overland.json", Hall(("cell_ft", 31680))), ("live.json", Obj(("base", "overland"), ("cell_ft", 5)))], "live.json");
        yield return Files("a scene repeats an overland base", [("overland.json", Hall(("cell_ft", 31680))), ("live.json", Obj(("base", "overland"), ("cell_ft", 31680)))], "live.json");
        yield return Files("a scene against a base written 5.0", [("goblin-hall.json", Hall(("cell_ft", 5.0))), ("live.json", Obj(("base", "goblin-hall"), ("cell_ft", 30)))], "live.json");
        yield return Files("a scene against a base written 5.0, repeated", [("goblin-hall.json", Hall(("cell_ft", 5.0))), ("live.json", Obj(("base", "goblin-hall"), ("cell_ft", 5)))], "live.json");
        yield return Files("the base holds a bad scale", [("old.json", Hall(("cell_ft", null))), ("live.json", Obj(("base", "old")))], "live.json");
        yield return Files("the base holds a bad scale, the scene a good one", [("old.json", Hall(("cell_ft", null))), ("live.json", Obj(("base", "old"), ("cell_ft", 5)))], "live.json");
        yield return Files("the scene holds a bad scale", [("goblin-hall.json", Place()), ("live.json", Obj(("base", "goblin-hall"), ("cell_ft", 7)))], "live.json");
        yield return Files("both hold bad scales", [("old.json", Hall(("cell_ft", 7))), ("live.json", Obj(("base", "old"), ("cell_ft", 8)))], "live.json");
        yield return Files("a base that is a scene", [("live.json", Obj(("base", "chain"))), ("chain.json", Obj(("base", "live")))], "live.json");
        yield return Files("a base with a path in it", [("sub/hall.json", Place()), ("live.json", Obj(("base", "sub/hall")))], "live.json");
        yield return Files("a base with a missing folder", [("live.json", Obj(("base", "nowhere/hall")))], "live.json");
        yield return Files("a base that is a scene with a falsy base", [("goblin-hall.json", Hall(("base", 0))), ("live.json", Obj(("base", "goblin-hall")))], "live.json");

        yield return Files("no such file", [], "missing.json");
        yield return Files("no such file with measure", [], "missing.json", "--measure", "A1", "B2");
        yield return Files("a missing file in a folder", [("d/x.json", "{}")], "d/nothing.json");
        yield return Files("an empty file", [("live.json", "")], "live.json");
        yield return Files("a map that is a number", [("live.json", "5")], "live.json");
        yield return Files("a map that is null", [("live.json", "null")], "live.json");
        yield return Files("a map that is a list", [("live.json", "[]")], "live.json");
        yield return Files("a map that is a string", [("live.json", "\"x\"")], "live.json");
        yield return Files("a map that is an empty object", [("live.json", "{}")], "live.json");
    }

    /// <summary>Broken JSON: the script's messages ("Expecting value: line 1 column 14 (char 13)") are reproduced.</summary>
    static IEnumerable<ParityCase> Broken()
    {
        string[] texts =
        [
            " ", "\n\n  ", "{", "[", "{\"a\"", "{\"a\":", "{\"a\": ", "{\"a\":1", "{\"a\":1,", "{\"a\":1,}", "{\"a\":1 ,  }", "{,}", "{1:2}",
            "{'a': 1}", "{\"a\" 1}", "{\"a\":}", "[1,]", "[1 2]", "[,1]", "[1,,2]", "nul", "tru", "fals", "nulll", "True", "None", "{\"a\": nan}",
            "{\"a\": NaN}", "NaN", "{\"a\": Infinity, \"b\": -Infinity}", "-", "-x", "01", "1.", "1.e3", "1e", "1e+", "[1.5e+]", "+1", ".5", "0x10",
            "\"abc", "\"abc\\", "\"\\x\"", "\"\\u12\"", "\"\\u12G4\"", "\"\\u12", "\"\\ud800", "{\"a\":\"\\ud800\"}", "{\"a\":\"\\ud800\\u0041\"}",
            "{\"a\":\"\\ud83d\\ude00\"}", "{\"a\":\"tab\there\"}", "{\"a\":\"new\nline\"}", "{\"a\":\"nul\u0000\"}", "{\"a\":\"\u001f\"}",
            "{} x", "{}{}", "{} //", "{\"a\":1}\n\n  x", "/* c */ {}", "{\"a\":1} # c", "[1]\n[2]", "\ufeff{}", "\ufeff", "{\"a\":\"\u00e9\u5317\ud83d\ude00\", \"b\": ]",
            "{\"grid\": [\"..\"], \"cell_ft\": }", "{\"grid\": [\"..\" \"..\"]}", "{\"grid\": [\"..\"],\n \"title\": tru}", "\n\n\n   {\"a\": [1, 2,\n 3 4]}",
            "[" + string.Concat(Enumerable.Repeat("[", 50)) + "]", "{\"a\": 1e999}", "{\"a\": -0, \"b\": 1E5, \"c\": 12345678901234567890123}",
            "{\"grid\": [\"..\"], \"north\": 1e999}", "{\"grid\":[\"..\"],\"cell_ft\":1e999}", "{\"grid\":[\"..\"],\"cell_ft\":-0}",
            "{\"grid\":[\"..\"],\"cell_ft\":12345678901234567890123}", "{\"grid\":[\"..\"],\"cell_ft\":5.0e0}", "{\"grid\":[\"..\"],\"cell_ft\":50E-1}",
            "{\"grid\":[\"..\"],\"cell_ft\":[1e999,-0.0,0.1,1e16,1e-5,123456789.123456789,\"\u00e9\"]}",
        ];
        for (int n = 0; n < texts.Length; n++)
            yield return Files($"broken json #{n}", [("live.json", texts[n])], "live.json");
    }

    static IEnumerable<ParityCase> Arguments()
    {
        ParityCase Args(string name, params string[] args) => Files(name, [("goblin-hall.json", Place()), ("live.json", Scene_())], args);
        yield return Args("no arguments");
        yield return Args("an unknown option", "live.json", "--bogus");
        yield return Args("an unknown short option", "live.json", "-x");
        yield return Args("an unknown option and no map", "--bogus");
        yield return Args("measure with no values", "live.json", "--measure");
        yield return Args("measure with one value", "live.json", "--measure", "B2");
        yield return Args("measure with an equals sign", "live.json", "--measure=B2");
        yield return Args("two maps", "live.json", "goblin-hall.json");
        yield return Args("two maps and an unknown option", "live.json", "goblin-hall.json", "--x");
        yield return Args("a flag with a value", "live.json", "--png=yes");
        yield return Args("an abbreviation of --measure", "live.json", "--meas", "B2", "E4");
        yield return Args("png abbreviated", "live.json", "--pn");
        yield return Args("the map after the options", "--png", "live.json");
        yield return Args("a double dash", "--", "live.json");
        yield return Args("the options between", "--png", "live.json", "--measure", "A1", "B2");
        yield return Args("a negative number as the map", "-5");
        yield return Args("a map with a space in its name", "my map.json");
        yield return Args("a repeated flag", "live.json", "--png", "--png");
    }

    public static IEnumerable<ParityCase> All { get; } =
        [.. Drawn(), .. Measured(), .. Rejected(), .. Broken(), .. Arguments()];

    /// <summary>
    /// Every input that should end in an error, for the token-cost guard: it must be one line, never a stack trace.
    /// It adds what the script cannot be compared on, because it crashes with a traceback there: a compass
    /// direction or the tokens given as the wrong kind of value, an empty file name, and a folder given as the file.
    /// </summary>
    public static IEnumerable<ParityCase> ErrorCases { get; } =
    [
        .. Measured(), .. Rejected(), .. Broken(), .. Arguments(),
        // Python 3.12 (not 3.11) takes "--png" here as the second cell, so the script is not compared on it.
        Files("measure with an option as a value", [("goblin-hall.json", Place()), ("live.json", Scene_())], "live.json", "--measure", "B2", "--png"),
        Alone("north is a list", Hall(("north", Arr("up")))),
        Alone("tokens is a number", Hall(("tokens", 5))),
        Alone("tokens is null", Hall(("tokens", null))),
        Alone("labels is a number", Hall(("labels", 5))),
        Alone("a base that is a number", Obj(("base", 5), ("grid", Arr("..")))),
        Alone("an edge that is a list", Hall(("edges", Obj(("top", Arr(1)))))),
        Files("an empty map name", [], ""),
        Files("a folder instead of a file", [("d/x.json", "{}")], "d"),
        Files("a file that is not UTF-8", [("live.json", "{}")], "live.json").WithBytes("live.json", [0x7B, 0xFF, 0x7D]),
    ];

    public static IEnumerable<object[]> CaseData => All.Select(c => new object[] { c });

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    [TestMethod]
    [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
    public void MapMatchesMapRenderPy(ParityCase test)
    {
        if (test.Args(".").Any(a => a.StartsWith("--p", StringComparison.Ordinal)))
            TestEnv.Need(Legacy.HasPyMuPdf(), "PyMuPDF");
        ParityCheck.Run(MapScript, test);
    }

    [TestMethod]
    public void ThePngIsTheSizeOfTheSvg()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            WriteFile(folder, "goblin-hall.json", Place());
            WriteFile(folder, "live.json", Scene_());
            var result = DmcRunner.BlackBox(["map", "live.json", "--png"], cwd: folder);
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            Assert.AreEqual(SvgSizeOf(folder), PngSizeOf(Path.Combine(folder, "live.png")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>The width and height written in the SVG that live-data.js holds.</summary>
    internal static (int Width, int Height) SvgSizeOf(string folder)
    {
        string script = File.ReadAllText(Path.Combine(folder, "live-data.js"));
        var payload = System.Text.Json.JsonDocument.Parse(script["dmcMap(".Length..script.LastIndexOf(')')]).RootElement;
        var root = System.Xml.Linq.XElement.Parse(payload.GetProperty("svg").GetString()!);
        return (int.Parse((string)root.Attribute("width")!), int.Parse((string)root.Attribute("height")!));
    }

    /// <summary>The size in the PNG's header (IHDR): 4 bytes each, big-endian, after the 8-byte signature and the chunk header.</summary>
    internal static (int Width, int Height) PngSizeOf(string path)
    {
        byte[] png = File.ReadAllBytes(path);
        Assert.AreEqual("PNG", Encoding.ASCII.GetString(png, 1, 3));
        return (System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)),
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
    }
}

static class ParityCaseExtensions
{
    /// <summary>The same case with a different place file (goblin-hall.json).</summary>
    public static ParityCase WithPlace(this ParityCase test, PyDict place) => test with
    {
        Seed = work =>
        {
            test.Seed?.Invoke(work);
            File.WriteAllText(Path.Combine(work, "goblin-hall.json"), PyJson.Dumps(place), new UTF8Encoding(false));
        },
    };

    /// <summary>The same case with one more file, given as text.</summary>
    public static ParityCase WithFile(this ParityCase test, string name, string content) => test with
    {
        Seed = work =>
        {
            test.Seed?.Invoke(work);
            File.WriteAllText(Path.Combine(work, name), content, new UTF8Encoding(false));
        },
    };

    /// <summary>The same case with one more file, given as bytes.</summary>
    public static ParityCase WithBytes(this ParityCase test, string name, byte[] content) => test with
    {
        Seed = work =>
        {
            test.Seed?.Invoke(work);
            File.WriteAllBytes(Path.Combine(work, name), content);
        },
    };
}
