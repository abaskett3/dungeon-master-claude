using System.Text;
using Dmc.Size;

namespace Dmc.Tests.Size;

/// <summary>
/// The token measure of spec 021 (<see cref="Measurement.Tokens"/>, <see cref="Measurement.ReadCost"/>) and the
/// character count of a line it rests on: the conversion, the edge that keeps every file inside a budget it was inside
/// before (research R3), and what a line's characters count.
/// </summary>
[TestClass]
public sealed class MeasurementTests
{
    /// <summary>Two tokens for each whole five characters, rounded down to the five.</summary>
    [TestMethod]
    public void TokensAreTwoForEachFiveCharacters()
    {
        Assert.AreEqual(0, Measurement.Tokens(0));
        Assert.AreEqual(0, Measurement.Tokens(4));
        Assert.AreEqual(2, Measurement.Tokens(5));
        Assert.AreEqual(16_000, Measurement.Tokens(40_000));
    }

    /// <summary>
    /// A budget of B under bytes ÷ 4 is 1.6 × B now. The old count flagged a text from 4B + 4 bytes; the new one flags
    /// it from 4B + 5 characters, and a text never has more characters than bytes, so it never flags a text the old
    /// count let through (research R3).
    /// </summary>
    [TestMethod]
    public void NoTextIsNewlyOverABudget()
    {
        const int old = 4000;
        long budget = old * 8 / 5;
        Assert.AreEqual(Budgets.GameState, budget, "the budget is 1.6 times its number under bytes ÷ 4");
        Assert.IsLessThanOrEqualTo(budget, Measurement.Tokens(4L * old + 4), "4B + 4 characters is not over");
        Assert.IsGreaterThan(budget, Measurement.Tokens(4L * old + 5), "4B + 5 characters is over");
        Assert.IsGreaterThan(old, (4L * old + 4) / 4, "the old count flagged 4B + 4 bytes, so nothing new is flagged");
    }

    /// <summary>A Read call costs its text's tokens and <see cref="Measurement.ReadCallTokens"/> for each call.</summary>
    [TestMethod]
    public void AReadCostsItsTextAndEachCall()
    {
        Assert.AreEqual(160, Measurement.ReadCallTokens);
        Assert.AreEqual(Measurement.Tokens(10_000) + 160, Measurement.ReadCost(10_000, 1));
        Assert.AreEqual(Measurement.Tokens(10_000) + 320, Measurement.ReadCost(10_000, 2));
    }

    /// <summary>A line counts its text and one for an LF; a CR before it and a byte-order mark are not counted.</summary>
    [TestMethod]
    public void ALineCountsItsTextAndOneForItsLineEnd()
    {
        var lines = Measurement.Split(Encoding.UTF8.GetBytes("ab\r\ncd\nef"));
        CollectionAssert.AreEqual(new[] { 3, 3, 2 }, lines.Select(l => l.Characters).ToArray());
        CollectionAssert.AreEqual(new[] { 4, 3, 2 }, lines.Select(l => l.Bytes).ToArray());

        var bom = Measurement.Split([0xEF, 0xBB, 0xBF, (byte)'a', (byte)'\n']);
        Assert.AreEqual(2, bom[0].Characters, "the byte-order mark is not a character of the text");
    }

    /// <summary>Non-ASCII text has fewer characters than bytes, so it never measures more than it did as bytes.</summary>
    [TestMethod]
    public void NonAsciiTextHasFewerCharactersThanBytes()
    {
        var line = Measurement.Split(Encoding.UTF8.GetBytes("é—ü\n"))[0];
        Assert.AreEqual(4, line.Characters);
        Assert.IsGreaterThan(line.Characters, line.Bytes);
    }
}
