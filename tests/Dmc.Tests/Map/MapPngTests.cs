using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dmc.Maps;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary>
/// The map PNG, drawn by Svg.Skia from the SVG. Pixels differ a little between operating systems, so these tests
/// look at the size and at whether text is drawn at all, not at pixels.
/// </summary>
[TestClass]
public sealed partial class MapPngTests
{
    [GeneratedRegex("<text[^>]*>.*?</text>", RegexOptions.Singleline)]
    private static partial Regex TextElement();

    static string SceneSvg(int cellFt = 5) => SvgDrawing.Render(Checked(("cell_ft", cellFt), ("tokens", SceneTokens())));

    [TestMethod]
    public void ThePngIsExactlyAsBigAsTheSvg()
    {
        foreach (int cellFt in new[] { 5, 30, 31680 })
        {
            string svg = SceneSvg(cellFt);
            var root = XElement.Parse(svg);
            var (png, width, height) = MapPng.Render(svg);
            Assert.AreEqual(int.Parse((string)root.Attribute("width")!), width);
            Assert.AreEqual(int.Parse((string)root.Attribute("height")!), height);
            Assert.AreEqual((width, height), (System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)),
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20))));
        }
    }

    [TestMethod]
    public void TheTurnOrderIsInThePngToo()
    {
        var scene = ("tokens", (object?)SceneTokens());
        string plain = SvgDrawing.Render(Checked(scene));
        string svg = SvgDrawing.Render(Checked(scene, ("initiative", Arr("Goblin boss", "Thorin")), ("current", "Thorin")));
        var (png, width, height) = MapPng.Render(svg);
        var root = XElement.Parse(svg);
        Assert.AreEqual((int.Parse((string)root.Attribute("width")!), int.Parse((string)root.Attribute("height")!)), (width, height));
        CollectionAssert.AreNotEqual(MapPng.Render(plain).Png, png);
    }

    [TestMethod]
    public void TheTextIsDrawnWithTheEmbeddedFont()
    {
        string svg = SceneSvg();
        string withoutText = TextElement().Replace(svg, "");
        Assert.AreNotEqual(svg, withoutText);
        // If no text were drawn (no font), the two pictures would be the same, pixel for pixel.
        CollectionAssert.AreNotEqual(MapPng.Render(svg).Png, MapPng.Render(withoutText).Png);
    }

    [TestMethod]
    public void TheEmbeddedFontsAreInTheProgram()
    {
        var names = typeof(MapPng).Assembly.GetManifestResourceNames();
        CollectionAssert.IsSubsetOf(new[] { "Dmc.Maps.Fonts.LiberationSans-Regular.ttf", "Dmc.Maps.Fonts.LiberationSans-Bold.ttf" }, names);
        Assert.IsTrue(File.Exists(Path.Combine(Support.TestEnv.Root, "src", "Dmc.Cli", "Maps", "Fonts", "LICENSE-LiberationSans.txt")),
            "the licence of the font sits next to it");
    }

    [TestMethod]
    public void AScriptInTheSvgIsNotRun()
    {
        // A picture is only ever drawn; scripts in it (which Svg.Skia could run, if asked) must not be.
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 20 20'>"
                     + "<script>throw new Error('ran')</script><rect width='20' height='20' fill='#fff'/></svg>";
        var (png, width, height) = MapPng.Render(svg);
        Assert.AreEqual((20, 20), (width, height));
        Assert.IsNotEmpty(png);
    }
}
