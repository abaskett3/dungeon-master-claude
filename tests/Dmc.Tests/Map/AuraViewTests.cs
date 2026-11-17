using System.Xml.Linq;
using Dmc.Maps;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary>
/// A token's <c>"aura"</c> and a map's <c>"view"</c> (spec 016 FR-025, contracts/map-aura-view.md), in this process: the
/// aura moves with its creature in the picture and the distance list, a view draws only its cells under their own
/// names and lists the creatures outside it, a view of the whole map draws what no view draws, and each bad value is
/// refused in one line.
/// </summary>
[TestClass]
public sealed class AuraViewTests
{
    /// <summary>Thorin on B2, a grey stone with a 5 ft rime on <paramref name="stone"/>, Kibil on F3 and Pip on I4, on the goblin hall.</summary>
    static List<object?> Creatures(string stone) => Arr(
        Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
        Obj(("id", "1"), ("name", "Grey stone"), ("kind", "enemy"), ("at", stone), ("aura", Obj(("ft", 5), ("name", "rime")))),
        Obj(("id", "2"), ("name", "Kibil"), ("kind", "ally"), ("at", "F3")),
        Obj(("id", "3"), ("name", "Pip"), ("kind", "ally"), ("at", "I4")));

    /// <summary>The <c>rect</c> the aura is drawn as, by its tooltip.</summary>
    static XElement AuraRect(string svg) =>
        XElement.Parse(svg).Descendants().Single(e => e.Name.LocalName == "rect" && e.Value == "Grey stone: rime, 5 ft");

    [TestMethod]
    public void AnAuraMovesWithItsCreatureInThePictureAndTheDistanceList()
    {
        var before = Checked(("tokens", Creatures("E2")));
        var after = Checked(("tokens", Creatures("H3")));
        CollectionAssert.AreEqual(new[] { "Inside auras (reach from the creature's edge):", "  1 Grey stone's rime (5 ft):  2 Kibil" },
            Distances.AuraLines(before));
        CollectionAssert.AreEqual(new[] { "Inside auras (reach from the creature's edge):", "  1 Grey stone's rime (5 ft):  3 Pip" },
            Distances.AuraLines(after));
        var (b, a) = (AuraRect(SvgDrawing.Render(before)), AuraRect(SvgDrawing.Render(after)));
        Assert.AreEqual(120, int.Parse(a.Attribute("x")!.Value) - int.Parse(b.Attribute("x")!.Value), "three cells to the right");
        Assert.AreEqual(40, int.Parse(a.Attribute("y")!.Value) - int.Parse(b.Attribute("y")!.Value), "one row down");
        StringAssert.Contains(SvgText(SvgDrawing.Render(after)), "rime, 5 ft", "the key names it");
    }

    [TestMethod]
    public void AViewDrawsOnlyItsCellsUnderTheirOwnNamesAndNamesTheCreaturesOutside()
    {
        var m = Checked(("tokens", Creatures("E2")), ("view", Obj(("cols", "C-F"), ("rows", "2-4"))));
        string svg = SvgDrawing.Render(m);
        var texts = XElement.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToList();
        foreach (string column in new[] { "C", "D", "E", "F" })
            Assert.Contains(column, texts);
        foreach (string column in new[] { "A", "B", "G", "J" })
            Assert.DoesNotContain(column, texts);
        StringAssert.Contains(SvgText(svg), "Not drawn, outside the view: @ Thorin (B2), 3 Pip (I4)");
        Assert.HasCount(6, Distances.DistanceLines(m).Where(l => l.Contains("<->", StringComparison.Ordinal)).ToList(),
            "distances still cover every creature");
    }

    [TestMethod]
    public void AViewOfTheWholeMapDrawsWhatNoViewDraws()
    {
        string plain = SvgDrawing.Render(Checked(("tokens", SceneTokens())));
        Assert.AreEqual(plain, SvgDrawing.Render(Checked(("tokens", SceneTokens()), ("view", Obj(("cols", "A-J"), ("rows", "1-5"))))));
        Assert.HasCount(0, Distances.AuraLines(Checked(("tokens", SceneTokens()))), "no aura, no lines");
    }

    [TestMethod]
    public void BadAurasAndViewsAreRefusedInOneLine()
    {
        void Refused(string expected, params (string, object?)[] changes)
        {
            var caught = Assert.ThrowsExactly<DmcException>(() => Checked(changes));
            StringAssert.Contains(caught.Message, expected);
            Assert.DoesNotContain("\n", caught.Message);
        }
        static (string, object?) Stone(object? aura) =>
            ("tokens", Arr(Obj(("id", "1"), ("name", "Grey stone"), ("at", "E2"), ("aura", aura))));

        Refused("aura \"ft\" must be a positive multiple of the map's 5 ft squares", Stone(Obj(("ft", 7), ("name", "rime"))));
        Refused("aura \"ft\" must be a positive multiple", Stone(Obj(("ft", 0), ("name", "rime"))));
        Refused("the aura needs a \"name\"", Stone(Obj(("ft", 10))));
        Refused("\"aura\" must be an object", Stone("rime"));
        Refused("view \"cols\" C-Z is off the grid, which has columns A to J", ("view", Obj(("cols", "C-Z"), ("rows", "1-5"))));
        Refused("view \"rows\" 0-3 is off the grid, which has rows 1 to 5", ("view", Obj(("cols", "A-J"), ("rows", "0-3"))));
        Refused("view \"cols\" F-C is reversed", ("view", Obj(("cols", "F-C"), ("rows", "1-5"))));
        Refused("view \"cols\" must be a range like \"C-R\"", ("view", Obj(("cols", "C"), ("rows", "1-5"))));
        Refused("\"view\" must be an object", ("view", "C-F"));
    }
}
