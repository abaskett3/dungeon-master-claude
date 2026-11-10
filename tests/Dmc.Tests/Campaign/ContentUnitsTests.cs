using System.Security.Cryptography;
using System.Text;
using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>How a campaign file splits into content units, and their ids (data-model.md § Content unit).</summary>
[TestClass]
public sealed class ContentUnitsTests
{
    [TestMethod]
    [DataRow("  Mara   keeps\tthe ferry  ", "Mara keeps the ferry")]
    [DataRow("- Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("* Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("+ Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("12. Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("> Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("> - Mara keeps the ferry", "Mara keeps the ferry")]
    [DataRow("### Captain Vex", "Captain Vex")]
    [DataRow("# Canon", "Canon")]
    [DataRow("| Bog trolls | 2 / 2 | the marsh |", "| Bog trolls | 2 / 2 | the marsh |")]
    [DataRow("-not a list", "-not a list")]
    [DataRow("#hashtag", "#hashtag")]
    public void ALineIsNormalised(string line, string unit) => Assert.AreEqual(unit, ContentUnits.Normalise(line));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("---")]
    [DataRow("***")]
    [DataRow("- - -")]
    [DataRow("___")]
    [DataRow("|---|---|")]
    [DataRow("| --- | :---: |")]
    [DataRow("|:--|--:|")]
    [DataRow("- ")]
    public void StructureIsNotAUnit(string line) => Assert.IsNull(ContentUnits.Normalise(line));

    [TestMethod]
    public void CommentsAreSkippedEvenAcrossLines()
    {
        string text = "# Notes\n<!-- one line -->\nkept <!-- inline --> too\n<!--\nhidden\nstill hidden\n-->\nafter\n";
        CollectionAssert.AreEqual(new[] { "Notes", "kept too", "after" }, ContentUnits.Split(text));
    }

    [TestMethod]
    public void AnUnclosedCommentIsContent() =>
        CollectionAssert.AreEqual(new[] { "<!-- open", "still here" }, ContentUnits.Split("<!-- open\nstill here\n"));

    [TestMethod]
    public void CrlfAndLfGiveTheSameUnits()
    {
        string lf = "# Canon\n\n- Mara keeps the ferry. (s1)\n| a | b |\n|---|---|\n| c | d |\n";
        CollectionAssert.AreEqual(ContentUnits.Split(lf), ContentUnits.Split(lf.Replace("\n", "\r\n")));
    }

    [TestMethod]
    public void DuplicatesAreKeptInOrderAndShareAnId()
    {
        var units = ContentUnits.Split("- same\n- other\n- same\n");
        CollectionAssert.AreEqual(new[] { "same", "other", "same" }, units);
        Assert.AreEqual(ContentUnits.Id(units[0]), ContentUnits.Id(units[2]));
    }

    [TestMethod]
    public void TheIdIsTwelveHexDigitsOfTheSha256()
    {
        string unit = "Mara keeps the ferry";
        string expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(unit)))[..12];
        Assert.AreEqual(expected, ContentUnits.Id(unit));
        Assert.AreNotEqual(ContentUnits.Id("mara keeps the ferry"), ContentUnits.Id(unit), "comparison is case-sensitive");
    }
}
