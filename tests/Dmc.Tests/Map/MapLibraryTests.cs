using System.Xml.Linq;
using Dmc.Maps;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

// The first four classes of tests/test_map_render.py (CellNames, Checking, Distances, Drawing), ported one for one:
// test_column_names is ColumnNames, and so on. In Python they call the module's functions directly, so here they
// call the library code of the CLI project in this process. The CommandLine class is in MapCommandContract.cs.
// "SystemExit" in the Python is a DmcException here: dmc prints its message as "dmc map: <message>".

[TestClass]
public sealed class MapCellNamesTests
{
    [TestMethod]
    public void ColumnNames()
    {
        CollectionAssert.AreEqual(
            new[] { "A", "B", "Z", "AA", "AB", "AZ", "BA", "ZZ", "AAA" },
            new[] { 0, 1, 25, 26, 27, 51, 52, 701, 702 }.Select(Cells.ColName).ToArray());
    }

    [TestMethod]
    public void CellName() => Assert.AreEqual("E3", Cells.CellName(4, 2));

    [TestMethod]
    public void ParseCell()
    {
        Assert.AreEqual((4, 2), Cells.Parse("E3", 10, 5, "x"));
        Assert.AreEqual((4, 2), Cells.Parse(" e3 ", 10, 5, "x"));
        Assert.AreEqual((26, 11), Cells.Parse("AA12", 30, 20, "x"));
    }

    [TestMethod]
    public void ParseCellIsTheReverseOfCellName()
    {
        foreach (int col in new[] { 0, 7, 25, 26, 60 })
        {
            foreach (int row in new[] { 0, 9, 41 })
                Assert.AreEqual((col, row), Cells.Parse(Cells.CellName(col, row), 100, 100, "x"));
        }
    }

    [TestMethod]
    public void ParseCellRejectsNonsense()
    {
        foreach (string bad in new[] { "", "E", "3", "3E", "E-3", "E3x", "E 3" })
        {
            var caught = Assert.ThrowsExactly<DmcException>(() => Cells.Parse(bad, 10, 5, "token 1"), bad);
            StringAssert.Contains(caught.Message, "is not a cell");
        }
    }

    [TestMethod]
    public void ParseCellRejectsCellsOffTheMap()
    {
        foreach (string bad in new[] { "K1", "A6", "A0" })
        {
            var caught = Assert.ThrowsExactly<DmcException>(() => Cells.Parse(bad, 10, 5, "token 1"), bad);
            StringAssert.Contains(caught.Message, "off the map (the map is A1 to J5)");
        }
    }
}

[TestClass]
public sealed class MapCheckingTests
{
    static void Rejected(string expected, params (string, object?)[] changes)
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => Checked(changes));
        StringAssert.Contains(caught.Message, expected);
    }

    static (string, object?) WithTokens(List<object?> tokens) => ("tokens", tokens);

    static (string, object?) One(params (string, object?)[] token) => ("tokens", Arr(Obj(token)));

    // Issue #6, finding 5: the optional "initiative" and "current" keys of a fight.
    [TestMethod]
    public void TheTurnOrderIsCheckedAgainstTheTokens()
    {
        var tokens = WithTokens(SceneTokens());
        var m = Checked(tokens, ("initiative", Arr("Goblin boss", "Thorin")), ("current", "Thorin"));
        CollectionAssert.AreEqual(new[] { "Goblin boss", "Thorin" }, m.Initiative!.Select(t => t.Name).ToArray());
        Assert.AreEqual("Thorin", m.Current);

        // Neither key (or both null) is no turn order at all.
        foreach (var none in new[] { Checked(tokens), Checked(tokens, ("initiative", null), ("current", null)) })
        {
            Assert.IsEmpty(none.Initiative!);
            Assert.IsNull(none.Current);
        }
        // Between turns: an order and no one in particular.
        Assert.IsNull(Checked(tokens, ("initiative", Arr("Thorin"))).Current);

        Rejected("is not the name of any token", tokens, ("initiative", Arr("Thorin", "Zed")));
        Rejected("lists \"Thorin\" twice", tokens, ("initiative", Arr("Thorin", "Thorin")));
        Rejected("must be a list of creature names", tokens, ("initiative", Arr("Thorin", 5)));
        Rejected("must be a list of creature names", tokens, ("initiative", "Thorin"));
        Rejected("\"current\" is \"Goblin boss\", who is not in \"initiative\"", tokens, ("initiative", Arr("Thorin")), ("current", "Goblin boss"));
        Rejected("who is not in \"initiative\"", tokens, ("current", "Thorin"));
        Rejected("\"current\" must be the name", tokens, ("initiative", Arr("Thorin")), ("current", 3));
    }

    [TestMethod]
    public void AValidMap()
    {
        var m = Checked(WithTokens(SceneTokens()));
        Assert.AreEqual((10, 5), (m.Cols, m.Rows));
        Assert.AreEqual("Goblin hall", m.Title);
        Assert.AreEqual(5, m.CellFt);
        Assert.AreEqual("up", m.North);
        CollectionAssert.AreEqual(new[] { ("@", 1, 1), ("1", 4, 1) }, m.Tokens.Select(t => (t.Id, t.Col, t.Row)).ToArray());
        CollectionAssert.AreEqual(new[] { new Label(8, 2, "to the cellar") }, m.Labels.ToArray());
    }

    [TestMethod]
    public void TokenDefaults()
    {
        var token = Checked(One(("id", 7), ("at", "b2"))).Tokens[0];
        Assert.AreEqual("7", token.Id);
        Assert.AreEqual("7", token.Name);
        Assert.AreEqual("neutral", token.Kind);
        Assert.AreEqual(1, token.Size);
        Assert.AreEqual(1, token.Span);
        Assert.AreEqual((1, 1), (token.Col, token.Row));
        CollectionAssert.AreEqual(new[] { (1, 1) }, token.Cells.ToArray());
        Assert.AreEqual("", token.Note);
        Assert.IsFalse(token.Down);
    }

    [TestMethod]
    public void ALargeTokenCoversASquareOfCells()
    {
        var token = Checked(One(("id", "O"), ("at", "B2"), ("size", 2))).Tokens[0];
        CollectionAssert.AreEqual(new[] { (1, 1), (1, 2), (2, 1), (2, 2) }, token.Cells.Order().ToArray());
    }

    [TestMethod]
    public void GridMustBeRowsOfEqualLength()
    {
        Rejected("must be a list of strings", ("grid", "####"));
        Rejected("must be a list of strings", ("grid", Arr()));
        Rejected("first grid row is empty", ("grid", Arr("", "")));
        Rejected("grid row 2 is 3 cells wide but row 1 is 4", ("grid", Arr("####", "#.#", "####")));
    }

    [TestMethod]
    public void UnknownSymbol() => Rejected("the grid uses \"X\"", ("grid", Arr("#X#")));

    [TestMethod]
    public void Legend()
    {
        var m = Checked(("grid", Arr("#.Xo~")), ("legend", Obj(
            ("X", "altar"), ("o", "statue"), ("~", Obj(("name", "lava"), ("as", "hazard"), ("color", "#ff0000"))))));
        Assert.AreEqual(new LegendEntry("feature", "altar", null), m.Legend['X']);
        Assert.AreEqual(new LegendEntry("pillar", "statue", null), m.Legend['o']);
        Assert.AreEqual(new LegendEntry("hazard", "lava", "#ff0000"), m.Legend['~']);
        Assert.AreEqual(new LegendEntry("wall", "wall", null), m.Legend['#']);
    }

    [TestMethod]
    public void LegendMistakes()
    {
        Rejected("must be an object", ("legend", Arr("altar")));
        Rejected("must be a single character", ("legend", Obj(("XY", "altar"))));
        Rejected("must be a single character", ("legend", Obj((" ", "gap"))));
        Rejected("unknown terrain \"lava\"", ("legend", Obj((".", Obj(("name", "lava"), ("as", "lava"))))));
        Rejected("must be a name or an object", ("legend", Obj((".", 5))));
        Rejected("needs a name", ("grid", Arr("X")), ("legend", Obj(("X", Obj(("as", "wall"))))));
    }

    [TestMethod]
    public void ALegendCanTurnASymbolIntoAWall()
    {
        Rejected("which is a wall", ("grid", Arr("X.")), ("legend", Obj(("X", Obj(("name", "cliff"), ("as", "wall"))))),
            One(("id", "1"), ("at", "A1")));
    }

    [TestMethod]
    public void ScaleDefaultsToFiveFeet() => Assert.AreEqual(5, Checked().CellFt);

    [TestMethod]
    public void EveryStandardScaleIsAccepted()
    {
        foreach (int cellFt in MapRules.Scales)
            Assert.AreEqual(cellFt, Checked(("cell_ft", cellFt)).CellFt, $"cell_ft {cellFt}");
        Assert.AreEqual(5, Checked(("cell_ft", 5.0)).CellFt);
    }

    [TestMethod]
    public void AnyTokenMaySitOnAnyMap()
    {
        var tokens = Arr(Obj(("id", "@"), ("at", "B2"), ("kind", "pc")), Obj(("id", "K"), ("at", "C2"), ("kind", "ally")),
            Obj(("id", "N"), ("at", "D2"), ("kind", "neutral")), Obj(("id", "1"), ("at", "E2"), ("kind", "enemy")));
        foreach (int cellFt in MapRules.Scales)
            Assert.HasCount(4, Checked(("cell_ft", cellFt), WithTokens(tokens)).Tokens, $"cell_ft {cellFt}");
    }

    [TestMethod]
    public void North()
    {
        Assert.IsNull(Checked(("north", null)).North);
        Rejected("\"north\" must be", ("north", "north"));
    }

    [TestMethod]
    public void TokenMistakes()
    {
        Rejected("needs at least \"id\" and \"at\"", One(("id", "1")));
        Rejected("needs at least \"id\" and \"at\"", WithTokens(Arr("B2")));
        Rejected("must be one or two characters", One(("id", "abc"), ("at", "B2")));
        Rejected("must be one or two characters", One(("id", ""), ("at", "B2")));
        Rejected("two tokens share the id \"1\"", WithTokens(Arr(Obj(("id", "1"), ("at", "B2")), Obj(("id", 1), ("at", "C2")))));
        Rejected("kind \"monster\" must be one of", One(("id", "1"), ("at", "B2"), ("kind", "monster")));
        Rejected("size must be 1, 2, 3 or 4", One(("id", "1"), ("at", "B2"), ("size", 5)));
    }

    [TestMethod]
    public void TokensCannotStandWhereNothingCan()
    {
        Rejected("is on A1, which is a wall", One(("id", "1"), ("at", "A1")));
        Rejected("is on B1, which is empty space", ("grid", Arr("# .", "#..")), One(("id", "1"), ("at", "B1")));
        Rejected("off the map", One(("id", "1"), ("at", "Z9")));
        Rejected("with size 2 runs off the map", ("grid", Arr("..", "..")), One(("id", "1"), ("at", "B2"), ("size", 2)));
        // A large creature is checked on every cell it covers, not only its corner.
        Rejected("is on C4, which is a wall", ("grid", Arr("....", "....", "....", "..#.")),
            One(("id", "O"), ("at", "B2"), ("size", 3)));
    }

    [TestMethod]
    public void TokensCannotShareACell()
    {
        Rejected("tokens 0 and 1 are both on B2", WithTokens(Tokens(Spec("B2"), Spec("B2"))));
        Rejected("tokens 0 and 1 are both on C3", WithTokens(Tokens(Spec("B2", ("size", 2)), Spec("C3"))));
    }

    [TestMethod]
    public void TokensMayShareACellOnAnyMapBiggerThan5Ft()
    {
        var crowd = Tokens(Spec("B2"), Spec("B2"), Spec("B2"), Spec("B2"));
        foreach (int cellFt in new[] { 30, 5280, 31680, 316800 })
            Assert.HasCount(4, Checked(("cell_ft", cellFt), WithTokens(crowd)).Tokens, $"cell_ft {cellFt}");
        Rejected("tokens 0 and 1 are both on B2", ("cell_ft", 5), WithTokens(crowd));
        // Walls and the map's edge still stop a token on a coarse map.
        foreach (int cellFt in new[] { 30, 5280 })
            Rejected("is on A1, which is a wall", ("cell_ft", cellFt), One(("id", "1"), ("at", "A1")));
    }

    [TestMethod]
    public void SizeCountsAsOneOnAnyMapBiggerThan5Ft()
    {
        foreach (int cellFt in new[] { 30, 5280, 31680 })
        {
            var ogre = Checked(("cell_ft", cellFt), One(("id", "O"), ("at", "B2"), ("size", 3))).Tokens[0];
            Assert.AreEqual((3, 1), (ogre.Size, ogre.Span), $"cell_ft {cellFt}");
            CollectionAssert.AreEqual(new[] { (1, 1) }, ogre.Cells.ToArray());
            // A big creature at the map's edge no longer runs off it.
            var edge = Checked(("cell_ft", cellFt), ("grid", Arr("..", "..")), One(("id", "O"), ("at", "B2"), ("size", 4)));
            CollectionAssert.AreEqual(new[] { (1, 1) }, edge.Tokens[0].Cells.ToArray());
        }
        Rejected("size must be 1, 2, 3 or 4", ("cell_ft", 30), One(("id", "1"), ("at", "B2"), ("size", 5)));
        // At 5 ft it still covers its square of cells.
        var big = Checked(One(("id", "O"), ("at", "B2"), ("size", 3))).Tokens[0];
        Assert.AreEqual((3, 9), (big.Span, big.Cells.Count));
    }

    [TestMethod]
    public void ThePanelStillShowsTheDeclaredSizeOnACoarseMap()
    {
        var m = Checked(("cell_ft", 30), One(("id", "O"), ("name", "Ogre"), ("at", "B2"), ("size", 2)));
        StringAssert.Contains(SvgText(SvgDrawing.Render(m)), "Ogre · B2 (2×2)");
        var small = Checked(("cell_ft", 30), One(("id", "G"), ("name", "Goblin"), ("at", "B2")));
        Assert.DoesNotContain("×", SvgText(SvgDrawing.Render(small)));
    }

    [TestMethod]
    public void AScaleMustBeStandard()
    {
        foreach (object? bad in new object?[] { null, 2.5, 0, 1, -5, 10, 50, 500, "5", true, false, Arr(5), Obj() })
            Rejected("the map: \"cell_ft\" must be a standard map scale", ("cell_ft", bad));
    }

    [TestMethod]
    public void TheErrorListsTheScalesFromSCALES()
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => Checked(("cell_ft", 7)));
        foreach (int scale in MapRules.Scales)
            StringAssert.Contains(caught.Message, scale.ToString());
        StringAssert.Contains(caught.Message, "5280 (1 mile), 31680 (6 miles), 316800 (60 miles)");
    }

    [TestMethod]
    public void TokensMayStandOnDoorsStairsAndWater()
    {
        var m = Checked(("grid", Arr("+>~,_")), WithTokens(Tokens(Spec("A1"), Spec("B1"), Spec("C1"), Spec("D1"), Spec("E1"))));
        Assert.HasCount(5, m.Tokens);
    }

    [TestMethod]
    public void LabelAndEdgeMistakes()
    {
        Rejected("every label needs \"at\" and \"text\"", ("labels", Arr(Obj(("at", "B2")))));
        Rejected("off the map", ("labels", Arr(Obj(("at", "Z9"), ("text", "x")))));
        Rejected("\"edges\" may only have the keys", ("edges", Obj(("north", "road"))));
    }
}

[TestClass]
public sealed class MapDistancesTests
{
    [TestMethod]
    public void SquaresBetween()
    {
        Assert.AreEqual(0, Distances.SquaresBetween([(0, 0)], [(0, 0)]));
        Assert.AreEqual(3, Distances.SquaresBetween([(0, 0)], [(3, 0)]));
        Assert.AreEqual(4, Distances.SquaresBetween([(0, 0)], [(0, 4)]));
        // A diagonal step costs the same as a straight one.
        Assert.AreEqual(3, Distances.SquaresBetween([(0, 0)], [(3, 3)]));
        Assert.AreEqual(5, Distances.SquaresBetween([(0, 0)], [(2, 5)]));
    }

    [TestMethod]
    public void BigCreaturesMeasureFromTheirNearestCell()
    {
        (int, int)[] ogre = [(1, 1), (2, 1), (1, 2), (2, 2)];
        Assert.AreEqual(2, Distances.SquaresBetween(ogre, [(4, 1)]));
        Assert.AreEqual(2, Distances.SquaresBetween([(4, 1)], ogre));
        Assert.AreEqual(1, Distances.SquaresBetween(ogre, [(3, 3)]));
    }

    [TestMethod]
    public void FormatDistance()
    {
        Assert.AreEqual("15 ft", Distances.FormatDistance(3, 5));
        Assert.AreEqual("0 ft", Distances.FormatDistance(0, 5));
        Assert.AreEqual("90 ft", Distances.FormatDistance(3, 30));
    }

    [TestMethod]
    public void OverlandDistancesAreGivenInMiles()
    {
        Assert.AreEqual("1 mile", Distances.FormatDistance(1, 5280));
        Assert.AreEqual("24 miles", Distances.FormatDistance(4, 31680));
        Assert.AreEqual("0 miles", Distances.FormatDistance(0, 5280));
        Assert.AreEqual("120 miles", Distances.FormatDistance(2, 316800));
    }

    [TestMethod]
    public void UnitsFollowTheScaleNotTheSizeOfTheDistance()
    {
        Assert.AreEqual("10000 ft", Distances.FormatDistance(2000, 5));
        Assert.AreEqual("9000 ft", Distances.FormatDistance(300, 30));
    }

    [TestMethod]
    public void OnlyTheMileScalesAreOverland()
    {
        CollectionAssert.AreEqual(new[] { false, false, true, true, true }, MapRules.Scales.Select(MapRules.IsOverland).ToArray());
    }

    [TestMethod]
    public void MileScaleDistancesAreStraightLines()
    {
        // B2 to E5 is 3 squares across and 3 down: 4.24 squares, 25.5 miles at 6 miles a square.
        (int, int)[] b2 = [(1, 1)], e5 = [(4, 4)];
        Assert.AreEqual("about 25 miles", Distances.Distance(b2, e5, 31680));
        Assert.AreEqual("about 18 miles", Distances.Distance(b2, [(4, 1)], 31680));
        Assert.AreEqual("about 1 mile", Distances.Distance(b2, [(2, 2)], 5280)); // 1.41 miles
    }

    [TestMethod]
    public void FootScaleDistancesCountSquaresThe5eWay()
    {
        // A diagonal costs the same as a straight step, at 5 ft and at 30 ft.
        (int, int)[] b2 = [(1, 1)], e5 = [(4, 4)];
        Assert.AreEqual("15 ft", Distances.Distance(b2, e5, 5));
        Assert.AreEqual("about 90 ft", Distances.Distance(b2, e5, 30));
    }

    [TestMethod]
    public void DistancesAt30FtAreApproximate()
    {
        Assert.AreEqual("same square (within 30 ft)", Distances.Distance([(1, 1)], [(1, 1)], 30));
        Assert.AreEqual("0 ft", Distances.Distance([(1, 1)], [(1, 1)], 5));
        Assert.AreEqual("about 30 ft", Distances.Distance([(1, 1)], [(2, 1)], 30));
        Assert.AreEqual("0 ft", Distances.Distance([(1, 1)], [(1, 1)], 5)); // 5 ft stays exact
    }

    [TestMethod]
    public void TheSameCellOnAMileMapIsTheSameSquareNotZeroMiles()
    {
        foreach (var (cellFt, within) in new[] { (5280, "1 mile"), (31680, "6 miles"), (316800, "60 miles") })
        {
            Assert.AreEqual($"same square (within {within})", Distances.Distance([(1, 1)], [(1, 1)], cellFt));
            var pair = Tokens(Spec("B2", ("name", "Thorin")), Spec("B2", ("name", "Wyvern")));
            var lines = Distances.DistanceLines(Checked(("cell_ft", cellFt), ("tokens", pair)));
            Assert.IsTrue(lines[1].EndsWith($":  same square (within {within})"), string.Join("\n", lines));
        }
    }

    [TestMethod]
    public void The30FtDistanceLinesSayTheyAreCoarseAndNameNoAdjacentDistance()
    {
        var pair = Arr(Obj(("id", "@"), ("name", "Thorin"), ("at", "B2"), ("kind", "pc")),
            Obj(("id", "1"), ("name", "Boss"), ("at", "B2"), ("kind", "enemy")),
            Obj(("id", "2"), ("name", "Archer"), ("at", "E4"), ("kind", "enemy")));
        var lines = Distances.DistanceLines(Checked(("cell_ft", 30), ("tokens", pair)));
        Assert.AreEqual("Distances (1 cell = 30 ft, so about ±30 ft; straight line, walls ignored):", lines[0]);
        Assert.DoesNotContain("adjacent", lines[0]);
        Assert.AreEqual("  @ Thorin (B2)  <->  1 Boss (B2):  same square (within 30 ft)", lines[1]);
        Assert.IsTrue(lines[2].EndsWith(":  about 90 ft"), string.Join("\n", lines));
    }

    [TestMethod]
    public void MileScaleDistancesMeasureBetweenTheNearestCells()
    {
        (int, int)[] ogre = [(1, 1), (2, 1), (1, 2), (2, 2)];
        Assert.AreEqual("about 3 miles", Distances.Distance(ogre, [(5, 1)], 5280));
    }

    [TestMethod]
    public void DistanceLines()
    {
        var tokens = SceneTokens();
        tokens.Add(Obj(("id", "O"), ("name", "Ogre"), ("at", "G2"), ("size", 2)));
        var lines = Distances.DistanceLines(Checked(("tokens", tokens)));
        Assert.HasCount(4, lines); // a heading and one line for each pair
        StringAssert.Contains(lines[0], "1 cell = 5 ft");
        Assert.AreEqual("  @ Thorin (B2)  <->  1 Goblin boss (E2):  15 ft", lines[1]);
        Assert.AreEqual("  @ Thorin (B2)  <->  O Ogre (G2):  25 ft", lines[2]);
        Assert.AreEqual("  1 Goblin boss (E2)  <->  O Ogre (G2):  10 ft", lines[3]);
    }

    [TestMethod]
    public void DistanceLinesUseTheCellSize()
    {
        var m = Checked(("cell_ft", 31680), ("tokens", SceneTokens()));
        Assert.IsTrue(Distances.DistanceLines(m)[1].EndsWith(":  about 18 miles"));
    }

    [TestMethod]
    public void NoDistancesWithoutTwoCreatures()
    {
        Assert.IsEmpty(Distances.DistanceLines(Checked()));
        Assert.IsEmpty(Distances.DistanceLines(Checked(("tokens", SceneTokens().Take(1).ToList()))));
    }

    [TestMethod]
    public void DistanceLinesArePrintedAtEveryScaleForTwoOrMoreCreatures()
    {
        var pair = Arr(Obj(("id", "@"), ("at", "B2"), ("kind", "pc")), Obj(("id", "K"), ("at", "E2"), ("kind", "ally")));
        foreach (int cellFt in MapRules.Scales)
        {
            var lines = Distances.DistanceLines(Checked(("cell_ft", cellFt), ("tokens", pair)));
            Assert.HasCount(2, lines, $"cell_ft {cellFt}");
            Assert.IsTrue(lines[1].EndsWith(":  " + Distances.Distance([(1, 1)], [(4, 1)], cellFt)), string.Join("\n", lines));
        }
    }

    [TestMethod]
    public void TheDistanceHeadingGivesTheScaleOnce()
    {
        var pair = Arr(Obj(("id", "@"), ("at", "B2"), ("kind", "pc")), Obj(("id", "K"), ("at", "E4"), ("kind", "ally")));
        var miles = Distances.DistanceLines(Checked(("cell_ft", 31680), ("tokens", pair)));
        Assert.AreEqual("Distances (1 cell = 6 miles, straight line, walls ignored):", miles[0]);
        Assert.IsTrue(miles[1].EndsWith(":  about 22 miles"), string.Join("\n", miles)); // 3 across, 2 down: 3.6 squares
        Assert.AreEqual("Distances (1 cell = 5 ft, straight line, walls ignored; 5 ft is adjacent):",
            Distances.DistanceLines(Checked(("tokens", pair)))[0]);
    }
}

[TestClass]
public sealed class MapDrawingTests
{
    static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [TestMethod]
    public void Wrap()
    {
        CollectionAssert.AreEqual(new[] { "one two", "three", "four" }, SvgDrawing.Wrap("one two three four", 9));
        CollectionAssert.AreEqual(new[] { "short" }, SvgDrawing.Wrap("short", 20));
        CollectionAssert.AreEqual(new[] { "" }, SvgDrawing.Wrap("", 20));
        CollectionAssert.AreEqual(new[] { "unbreakablyLongWord", "x" }, SvgDrawing.Wrap("unbreakablyLongWord x", 5));
    }

    [TestMethod]
    public void ThePictureIsWellFormedAndNamesEverything()
    {
        var m = Checked(("tokens", SceneTokens()), ("notes", "Smoke hangs under the rafters."),
            ("edges", Obj(("top", "to the yard"), ("bottom", "cliff"), ("left", "road"), ("right", "river"))),
            ("scale_note", "Ceiling 10 ft"));
        string words = SvgText(SvgDrawing.Render(m));
        foreach (string expected in new[]
                 {
                     "Goblin hall", "Smoke hangs", "Thorin", "Goblin boss", "to the cellar", "to the yard", "cliff", "road",
                     "river", "1 square = 5 ft. Ceiling 10 ft", "ON THE MAP", "KEY", "pillar", "stairs down", "door",
                 })
            StringAssert.Contains(words, expected);
    }

    [TestMethod]
    public void EveryTerrainDraws()
    {
        var m = Checked(("grid", Arr("#.+=~^><To_,?X")), ("legend", Obj(("X", "altar"))));
        CollectionAssert.AreEquivalent(MapRules.Terrains.ToArray(), m.Legend.Values.Select(e => e.Terrain).Distinct().ToArray());
        string words = SvgText(SvgDrawing.Render(m));
        StringAssert.Contains(words, "altar");
        Assert.DoesNotContain("ON THE MAP", words);
    }

    [TestMethod]
    public void TheTurnOrderIsDrawnBesideTheGridOnlyWhenTheMapHasOne()
    {
        var tokens = ("tokens", (object?)SceneTokens());
        string plain = SvgDrawing.Render(Checked(tokens));
        Assert.DoesNotContain("TURN", SvgText(plain));
        // An empty order is no order: the picture is the same.
        Assert.AreEqual(plain, SvgDrawing.Render(Checked(tokens, ("initiative", Arr()))));

        string svg = SvgDrawing.Render(Checked(tokens, ("initiative", Arr("Goblin boss", "Thorin")), ("current", "Thorin")));
        string words = SvgText(svg);
        StringAssert.Contains(words, "TURN ORDER");
        Assert.IsLessThan(words.IndexOf("2. Thorin", StringComparison.Ordinal), words.IndexOf("1. Goblin boss", StringComparison.Ordinal));
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(svg, ">TURN<").Count, "one current turn is marked");

        // With no one's turn marked, there is a list and no mark.
        string between = SvgDrawing.Render(Checked(tokens, ("initiative", Arr("Goblin boss", "Thorin"))));
        StringAssert.Contains(SvgText(between), "TURN ORDER");
        Assert.DoesNotContain(">TURN<", between);
    }

    [TestMethod]
    public void TextFromTheMapFileIsEscaped()
    {
        var m = Checked(("title", "Tom & Jerry's <\"den\">"), ("legend", Obj(("o", "<b>pillar</b>"))),
            ("tokens", Arr(Obj(("id", "<"), ("name", "A & B"), ("at", "B2"), ("note", "\"prone\" <1>")))),
            ("labels", Arr(Obj(("at", "C3"), ("text", "<script>")))));
        string svg = SvgDrawing.Render(m);
        Assert.DoesNotContain("<script>", svg);
        string words = SvgText(svg);
        foreach (string expected in new[] { "Tom & Jerry's <\"den\">", "<b>pillar</b>", "A & B", "\"prone\" <1>", "<script>" })
            StringAssert.Contains(words, expected);
    }

    [TestMethod]
    public void AMapWithNoNorthHasNoCompass()
    {
        string words = SvgText(SvgDrawing.Render(Checked(("north", null))));
        Assert.DoesNotContain(" N ", $" {words} ");
    }

    [TestMethod]
    public void EveryPictureHasAScaleBarAndSaysItsScale()
    {
        var said = new Dictionary<int, string>
        {
            [5] = "1 square = 5 ft", [30] = "1 square = 30 ft", [5280] = "1 square = 1 mile",
            [31680] = "1 square = 6 miles", [316800] = "1 square = 60 miles",
        };
        foreach (int cellFt in MapRules.Scales)
        {
            string svg = SvgDrawing.Render(Checked(("cell_ft", cellFt)));
            StringAssert.Contains(svg, $"v8 h{SvgDrawing.Cell} v-8\"");
            StringAssert.Contains(SvgText(svg), said[cellFt]);
            Assert.DoesNotContain("Not to scale", svg);
        }
    }

    /// <summary>
    /// The SVG of a map, the radii of the circles on the grid (tokens and badges, before the legend panel's
    /// samples), and the font sizes of the ids drawn on them.
    /// </summary>
    static (string Svg, List<double> Radii, List<double> Fonts) Drawn(MapModel m)
    {
        string svg = SvgDrawing.Render(m);
        var root = XElement.Parse(svg);
        var radii = root.Descendants(Svg + "circle").Where(c => (string?)c.Attribute("stroke") == "#ffffff")
            .Select(c => double.Parse((string)c.Attribute("r")!, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var fonts = root.Descendants(Svg + "text").Where(t => (string?)t.Attribute("font-weight") == "bold")
            .Select(t => double.Parse((string)t.Attribute("font-size")!, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        return (svg, radii, fonts);
    }

    [TestMethod]
    public void TokensSharingACellAreDrawnSmallInsideIt()
    {
        var (_, solo, _) = Drawn(Checked(("cell_ft", 30), ("tokens", Tokens(Spec("B2", ("name", "Solo"))))));
        foreach (int count in new[] { 2, 3, 4 })
        {
            var names = Enumerable.Range(0, count).Select(n => $"Orc{n}").ToList();
            var (svg, radii, _) = Drawn(Checked(("cell_ft", 30), ("tokens", Tokens(names.Select(n => Spec("B2", ("name", n))).ToArray()))));
            foreach (string name in names)
            {
                StringAssert.Contains(svg, $"{name} (B2)");        // the tooltip
                StringAssert.Contains(SvgText(svg), name);          // the panel entry
            }
            var onMap = radii.Take(count).ToList();
            Assert.HasCount(count, onMap);
            Assert.IsLessThan(solo[0], onMap.Max());
            Assert.IsLessThanOrEqualTo(SvgDrawing.Cell, onMap.Max() * 2 * 2); // two across
        }
    }

    [TestMethod]
    public void SharedTokensKeepAReadableSizeAndACrowdBecomesABadge()
    {
        var names = Enumerable.Range(0, 10).Select(n => $"Orc{n}").ToList();
        var crowd = Tokens(names.Select(n => Spec("B2", ("name", n))).ToArray());
        foreach (int cellFt in new[] { 30, 31680 })
        {
            var (svg, radii, fonts) = Drawn(Checked(("cell_ft", cellFt), ("tokens", crowd)));
            var onMap = radii.Take(SvgDrawing.ShownInACell).ToList(); // three tokens and the badge
            CollectionAssert.AreEqual(Enumerable.Repeat(SvgDrawing.SlotRadius, SvgDrawing.ShownInACell).ToArray(), onMap);
            Assert.IsGreaterThanOrEqualTo(SvgDrawing.SlotFont, fonts.Take(SvgDrawing.ShownInACell).Min());
            Assert.AreEqual(3, svg.Split("<title>Orc").Length - 1 - names.Count); // three on the grid, ten in the panel
            StringAssert.Contains(SvgText(svg), "+7");
            string badge = svg[svg.IndexOf("<title>Also here", StringComparison.Ordinal)..].Split("</title>")[0];
            foreach (string name in names.Skip(3))
                StringAssert.Contains(badge, name);                   // the hidden ones are in the badge tooltip
            foreach (string name in names)
                StringAssert.Contains(SvgText(svg).Split("ON THE MAP")[1], name); // and every one is in the panel
        }
    }

    [TestMethod]
    public void FourTokensInACellAllFitWithoutABadge()
    {
        var (svg, radii, _) = Drawn(Checked(("cell_ft", 30), ("tokens", Tokens(Spec("B2"), Spec("B2"), Spec("B2"), Spec("B2")))));
        Assert.DoesNotContain("Also here", svg);
        CollectionAssert.AreEqual(Enumerable.Repeat(SvgDrawing.SlotRadius, 4).ToArray(), radii.Take(4).ToArray());
    }

    [TestMethod]
    public void LargeAndFallenCreaturesAreDescribed()
    {
        var m = Checked(("tokens", Arr(
            Obj(("id", "O"), ("name", "Ogre"), ("at", "B2"), ("size", 2), ("note", "prone"), ("down", true)),
            Obj(("id", "G2"), ("name", "Goblin"), ("at", "F2")))));
        string svg = SvgDrawing.Render(m);
        StringAssert.Contains(SvgText(svg), "(2×2)");
        StringAssert.Contains(svg, "opacity=\"0.45\"");
    }

    [TestMethod]
    public void ThePictureGrowsWithTheMap()
    {
        static (double, double) Size(string[] grid)
        {
            var root = XElement.Parse(SvgDrawing.Render(Checked(("grid", Arr(grid)), ("labels", Arr()))));
            return (double.Parse((string)root.Attribute("width")!), double.Parse((string)root.Attribute("height")!));
        }
        var small = Size(Enumerable.Repeat(new string('.', 10), 10).ToArray());
        var wide = Size(Enumerable.Repeat(new string('.', 30), 10).ToArray());
        var tall = Size(Enumerable.Repeat(new string('.', 10), 30).ToArray());
        Assert.AreEqual(20 * SvgDrawing.Cell, wide.Item1 - small.Item1);
        Assert.AreEqual(20 * SvgDrawing.Cell, tall.Item2 - small.Item2);
    }
}
