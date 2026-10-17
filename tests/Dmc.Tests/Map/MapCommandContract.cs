using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Maps;
using Dmc.Tests.Support;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary>
/// The <c>CommandLine</c> class of <c>tests/test_map_render.py</c>, ported one for one (<c>test_png</c> is
/// <see cref="Png"/>, and so on). They run twice, through the two subclasses below: against the library code in this
/// process, and against the published <c>dmc</c> binary, the way DMC runs it. In Python they run the script as a
/// subprocess; the error prefix is "dmc map: " here, where the script said "map_render: ".
/// </summary>
public abstract partial class MapCommandContract
{
    string _folder = "";
    string _place = "";
    string _scene = "";

    /// <summary>Run <c>dmc map</c> with these arguments.</summary>
    protected abstract ProcessResult Map(params string[] args);

    [GeneratedRegex(@"@[A-Z]+@")]
    private static partial Regex Placeholder();

    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _place = Write("goblin-hall.json", Place());
        _scene = Write("live.json", Scene());
    }

    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    string Write(string name, object data)
    {
        WriteFile(_folder, name, data);
        return Path.Combine(_folder, name);
    }

    /// <summary>The map with these keys set (the Python <c>dict(PLACE, cell_ft=...)</c>); a key already there keeps its place.</summary>
    static PyDict With(PyDict map, params (string Key, object? Value)[] changes)
    {
        foreach (var (key, value) in changes)
            map[key] = value is int i ? (long)i : value;
        return map;
    }

    string[] Outputs() => Directory.GetFileSystemEntries(_folder).Select(Path.GetFileName).OfType<string>()
        .Except(["goblin-hall.json", "live.json", "overland.json", "old.json"]).Order(StringComparer.Ordinal).ToArray();

    /// <summary>Write a scene on a place file saved with its own scale, and return its path.</summary>
    string SceneOn(string place, params (string, object?)[] scene)
    {
        Write(place + ".json", With(Place(), ("cell_ft", place == "overland" ? 31680 : null)));
        Write("live.json", With(With(Obj(), scene), ("base", place)));
        return _scene;
    }

    void AssertFails(string expected, params string[] args)
    {
        var before = Outputs();
        var result = Map(args);
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        StringAssert.Contains(result.Stderr, "dmc map: ");
        StringAssert.Contains(result.Stderr, expected);
        CollectionAssert.AreEqual(before, Outputs(), "a rejected map must not be drawn");
    }

    string ReadFile(string name) => File.ReadAllText(Path.Combine(_folder, name), Encoding.UTF8).Replace("\r\n", "\n");

    JsonElement Payload()
    {
        string script = ReadFile("live-data.js");
        Assert.IsTrue(script.StartsWith("dmcMap(") && script.EndsWith(");\n"), script[..Math.Min(80, script.Length)]);
        return JsonDocument.Parse(script["dmcMap(".Length..^");\n".Length]).RootElement.Clone();
    }

    [TestMethod]
    public void DrawsASceneOnItsBase()
    {
        var result = Map(_scene);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "Drew \"Goblin hall\": 10 x 5 cells, 2 creatures.");
        StringAssert.Contains(result.Stdout, "@ Thorin (B2)  <->  1 Goblin boss (E2):  15 ft");
        CollectionAssert.AreEqual(new[] { "live-data.js", "live.html" }, Outputs());

        var payload = Payload();
        CollectionAssert.AreEqual(new[] { "stamp", "svg", "time", "title" },
            payload.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.AreEqual("Goblin hall", payload.GetProperty("title").GetString());
        StringAssert.Contains(SvgText(payload.GetProperty("svg").GetString()!), "Goblin boss");

        string page = ReadFile("live.html");
        StringAssert.Contains(page, "live-data.js");
        StringAssert.Contains(page, "function dmcMap(");
        Assert.IsFalse(Placeholder().IsMatch(page), "a placeholder was left in the page");
    }

    [TestMethod]
    public void DrawsAPlaceWithNoCreatures()
    {
        var result = Map(_place);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "0 creatures");
        Assert.DoesNotContain("Distances", result.Stdout);
    }

    [TestMethod]
    public void EachRunHasANewStamp()
    {
        var stamps = new List<string>();
        for (int n = 0; n < 2; n++)
        {
            Map(_scene);
            stamps.Add(Payload().GetProperty("stamp").GetString()!);
        }
        Assert.AreNotEqual(stamps[0], stamps[1], "the page only redraws when the stamp changes");
    }

    [TestMethod]
    public void ASceneOverridesItsBase()
    {
        Write("live.json", With(Scene(), ("title", "Goblin hall, on fire")));
        var result = Map(_scene);
        StringAssert.Contains(result.Stdout, "Drew \"Goblin hall, on fire\"");
        StringAssert.Contains(result.Stdout, ":  15 ft");
    }

    [TestMethod]
    public void ASceneCannotChangeItsBasesScale()
    {
        foreach (int cellFt in new[] { 30, 31680, 5280 })
        {
            Write("live.json", With(Scene(), ("cell_ft", cellFt)));
            AssertFails($"sets \"cell_ft\": {cellFt}, but its base \"goblin-hall\" is drawn at 5: "
                        + "a scene cannot change its place's scale", _scene);
        }
        Write("overland.json", With(Place(), ("cell_ft", 31680)));
        Write("live.json", Obj(("base", "overland"), ("cell_ft", 5)));
        AssertFails("but its base \"overland\" is drawn at 31680", _scene);
    }

    [TestMethod]
    public void ASceneMayRepeatItsBasesScale()
    {
        Write("live.json", With(Scene(), ("cell_ft", 5)));
        Assert.AreEqual(0, Map(_scene).ExitCode);
        Write("overland.json", With(Place(), ("cell_ft", 31680)));
        Write("live.json", Obj(("base", "overland"), ("cell_ft", 31680)));
        Assert.AreEqual(0, Map(_scene).ExitCode);
    }

    [TestMethod]
    public void ABadScaleNamesTheFileThatHoldsIt()
    {
        // In the place file, reached through a scene: the error points at the place.
        AssertFails("old.json: \"cell_ft\" must be a standard map scale", SceneOn("old"));
        Write("live.json", Obj(("base", "old"), ("cell_ft", 5))); // the scene's own good value changes nothing
        AssertFails("old.json: \"cell_ft\" must be a standard map scale", _scene);
        // In the scene file.
        Write("live.json", With(Scene(), ("cell_ft", 7)));
        AssertFails("live.json: \"cell_ft\" must be a standard map scale", _scene);
    }

    [TestMethod]
    public void ABadScaleIsShownTheWayJsonWritesIt()
    {
        foreach (var (value, shown) in new (object?, string)[]
                 {
                     (null, "null"), (true, "true"), (false, "false"), ("5", "\"5\""), (2.5, "2.5"),
                 })
        {
            Write("live.json", With(Scene(), ("cell_ft", value)));
            AssertFails($"not {shown}: every map is drawn to scale", _scene);
        }
    }

    [TestMethod]
    public void AMapNotToScaleIsRejected()
    {
        Write("bad.json", With(Place(), ("cell_ft", null)));
        var caught = Assert.ThrowsExactly<DmcException>(() => MapChecker.LoadMap(Path.Combine(_folder, "bad.json")));
        StringAssert.Contains(caught.Message, "every map is drawn to scale");
    }

    [TestMethod]
    public void AScaleOffTheStandardListIsRejected()
    {
        foreach (object? bad in new object?[] { 2.5, 0, 1, -5, 10, 50, 500, "5", true, false, Arr(5) })
        {
            Write("bad.json", With(Place(), ("cell_ft", bad)));
            var caught = Assert.ThrowsExactly<DmcException>(() => MapChecker.LoadMap(Path.Combine(_folder, "bad.json")));
            StringAssert.Contains(caught.Message, "bad.json: \"cell_ft\" must be a standard map scale");
        }
    }

    [TestMethod]
    public void MeasureDrawsNothing()
    {
        var result = Map(_scene, "--measure", "b2", "E4");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("B2 to E4: 3 squares, 15 ft (straight line, walls ignored)", result.Stdout.Trim());
        CollectionAssert.AreEqual(Array.Empty<string>(), Outputs());
    }

    [TestMethod]
    public void MeasureSaysOneSquareNotOneSquares()
    {
        var result = Map(_scene, "--measure", "B2", "C3");
        Assert.AreEqual("B2 to C3: 1 square, 5 ft (straight line, walls ignored)", result.Stdout.Trim());
    }

    [TestMethod]
    public void MeasureLinesAtEveryScale()
    {
        var lines = new Dictionary<int, (string Apart, string Same)>
        {
            [5] = ("B2 to E4: 3 squares, 15 ft (straight line, walls ignored)",
                   "B2 to B2: 0 squares, 0 ft (straight line, walls ignored)"),
            [30] = ("B2 to E4: 3 squares, about 90 ft (straight line, walls ignored)",
                    "B2 to B2: same square (within 30 ft)"),
            [31680] = ("B2 to E4: about 22 miles (straight line, walls ignored)",
                       "B2 to B2: same square (within 6 miles)"),
            [5280] = ("B2 to E4: about 4 miles (straight line, walls ignored)",
                      "B2 to B2: same square (within 1 mile)"),
        };
        foreach (var (cellFt, (apart, same)) in lines)
        {
            Write("goblin-hall.json", With(Place(), ("cell_ft", cellFt)));
            Assert.AreEqual(apart, Map(_scene, "--measure", "B2", "e4").Stdout.Trim(), $"cell_ft {cellFt}");
            Assert.AreEqual(same, Map(_scene, "--measure", "B2", "b2").Stdout.Trim(), $"cell_ft {cellFt}");
        }
    }

    [TestMethod]
    public void A30FtSceneDrawsTokensSharingACell()
    {
        Write("goblin-hall.json", With(Place(), ("cell_ft", 30)));
        Write("live.json", With(Scene(), ("tokens", Arr(
            Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
            Obj(("id", "1"), ("name", "Goblin boss"), ("kind", "enemy"), ("at", "B2"))))));
        var result = Map(_scene);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "@ Thorin (B2)  <->  1 Goblin boss (B2):  same square (within 30 ft)");
    }

    [TestMethod]
    public void MeasureMistakes()
    {
        AssertFails("off the map", _scene, "--measure", "B2", "Z99");
        AssertFails("every map is drawn to scale", SceneOn("old"), "--measure", "B2", "E4");
    }

    [TestMethod]
    public void MeasureOnAMileScaleMapIsAStraightLine()
    {
        string scene = SceneOn("overland");
        var result = Map(scene, "--measure", "B2", "E5");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("B2 to E5: about 25 miles (straight line, walls ignored)", result.Stdout.Trim());
        CollectionAssert.AreEqual(Array.Empty<string>(), Outputs());
    }

    [TestMethod]
    public void AMapNotToScaleIsNotDrawn() => AssertFails("\"cell_ft\" must be a standard map scale", SceneOn("old"));

    [TestMethod]
    public void AFightAndAPartyAreDrawnOnAMileScaleMap()
    {
        string scene = SceneOn("overland", ("tokens", Arr(
            Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
            Obj(("id", "1"), ("name", "Wyvern"), ("kind", "enemy"), ("at", "G2")))));
        var result = Map(scene);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "Distances (1 cell = 6 miles, straight line, walls ignored):");
        StringAssert.Contains(result.Stdout, "@ Thorin (B2)  <->  1 Wyvern (G2):  about 30 miles");
    }

    [TestMethod]
    public void Png()
    {
        var result = Map(_scene, "--png");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "Picture to check:");
        byte[] png = File.ReadAllBytes(Path.Combine(_folder, "live.png"));
        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png[..8]);
    }

    /// <summary>The turn order is in the page data when the scene has one and not otherwise; a bad one draws nothing.</summary>
    [TestMethod]
    public void TheTurnOrderReachesThePageAndABadOneIsNotDrawn()
    {
        Assert.AreEqual(0, Map(_scene).ExitCode);
        StringAssert.DoesNotMatch(Payload().GetProperty("svg").GetString()!, new Regex("TURN"));

        Write("live.json", With(Scene(), ("initiative", Arr("Goblin boss", "Thorin")), ("current", "Thorin")));
        var result = Map(_scene);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(SvgText(Payload().GetProperty("svg").GetString()!), "TURN ORDER");

        Write("live.json", With(Scene(), ("initiative", Arr("Thorin")), ("current", "Goblin boss")));
        AssertFails("\"current\" is \"Goblin boss\", who is not in \"initiative\"", _scene);

        // A stale order never blocks a distance: --measure draws nothing and does not check it.
        Write("live.json", With(Scene(), ("initiative", Arr("Gone")), ("current", "Gone")));
        var measured = Map(_scene, "--measure", "B2", "E2");
        Assert.AreEqual(0, measured.ExitCode, measured.ToString());
        StringAssert.Contains(measured.Stdout, "B2 to E2");
    }

    [TestMethod]
    public void NonAsciiNamesSurvive()
    {
        // Written as UTF-8 text, not as \u escapes: the file is read as UTF-8 and the page gets it back whole.
        Write("live.json", "{\"base\": \"goblin-hall\", \"title\": \"Thr\u00f3r's hall \u2014 \u5317\"}");
        var result = Map(_scene);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("Thrór's hall — 北", Payload().GetProperty("title").GetString());
        // The page data is ASCII, as json.dumps writes it, so any program can read it.
        Assert.IsTrue(File.ReadAllBytes(Path.Combine(_folder, "live-data.js")).All(b => b < 0x80));
        StringAssert.Contains(result.Stdout, "Drew \"Thrór's hall — 北\"");
    }

    [TestMethod]
    public void FileMistakes()
    {
        AssertFails("no such file", Path.Combine(_folder, "missing.json"));
        AssertFails("is not valid JSON", Write("broken.json", "{\"grid\": ["));
        AssertFails("must hold a JSON object", Write("list.json", "[]"));
        AssertFails("has no \"grid\" and no \"base\"", Write("empty.json", Obj(("title", "x"))));
        AssertFails("which does not exist", Write("orphan.json", Obj(("base", "nowhere"))));
        AssertFails("base must be a place file", Write("chain.json", Obj(("base", "live"))));
    }

    [TestMethod]
    public void ABadSceneIsRejected()
    {
        Write("live.json", With(Scene(), ("tokens", Arr(Obj(("id", "@"), ("name", "Thorin"), ("at", "A1"))))));
        AssertFails("token @ (Thorin) is on A1, which is a wall", _scene);
    }
}

/// <summary>The map tests against the library code, in this process.</summary>
[TestClass]
public sealed class MapInProcessTests : MapCommandContract
{
    protected override ProcessResult Map(params string[] args) => DmcRunner.InProcess(["map", .. args]);
}

/// <summary>The map tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class MapBlackBoxTests : MapCommandContract
{
    protected override ProcessResult Map(params string[] args) => DmcRunner.BlackBox(["map", .. args]);
}
