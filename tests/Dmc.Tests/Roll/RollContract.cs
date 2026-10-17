using System.Text.RegularExpressions;
using Dmc.Tests.Support;

namespace Dmc.Tests.Roll;

/// <summary>
/// The 15 tests of <c>tests/test_roll.py</c>, ported one for one: <c>test_single_die_with_modifier</c> is
/// <see cref="SingleDieWithModifier"/>, and so on. They run twice, through the two subclasses
/// (<see cref="RollInProcessTests"/> and <see cref="RollBlackBoxTests"/>), as the Python ones run once per roller.
/// </summary>
public abstract partial class RollContract
{
    /// <summary>A rolled line, <c>expr: [dice] keep [kept] +mod = total</c>. Groups: the expression, the dice, the kept dice (optional), the modifier (optional), the total.</summary>
    [GeneratedRegex(@"^(\S+): \[([\d, ]+)\](?: keep \[([\d, ]+)\])?(?: ([+-]\d+))? = (-?\d+)$")]
    private static partial Regex Line();

    /// <summary>Run <c>dmc roll</c> with these arguments.</summary>
    protected abstract ProcessResult Roll(params string[] exprs);

    /// <summary>A rolled line taken apart.</summary>
    /// <param name="Expr">The expression as echoed.</param>
    /// <param name="Dice">Every die rolled.</param>
    /// <param name="Kept">The dice kept, or null when the expression keeps none out.</param>
    /// <param name="Mod">The modifier; 0 when there is none.</param>
    /// <param name="Total">The total printed.</param>
    sealed record Parsed(string Expr, int[] Dice, int[]? Kept, int Mod, int Total);

    /// <summary>The numbers of a "3, 5, 2" list.</summary>
    static int[] Numbers(string text) => text.Split(", ").Select(int.Parse).ToArray();

    /// <summary>Takes a rolled line apart, failing the test if the line has any other shape.</summary>
    static Parsed Parse(string line)
    {
        var match = Line().Match(line);
        Assert.IsTrue(match.Success, $"unexpected output line: '{line}'");
        var g = match.Groups;
        return new Parsed(g[1].Value, Numbers(g[2].Value), g[3].Success ? Numbers(g[3].Value) : null,
            g[4].Success ? int.Parse(g[4].Value) : 0, int.Parse(g[5].Value));
    }

    /// <summary>Rolls one expression, which must succeed with exactly one output line, and parses that line.</summary>
    Parsed One(string expr)
    {
        var result = Roll(expr);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.HasCount(1, result.StdoutLines, result.Stdout);
        return Parse(result.StdoutLines[0]);
    }

    [TestMethod]
    public void SingleDieWithModifier()
    {
        var r = One("1d20+5");
        Assert.AreEqual("1d20+5", r.Expr);
        Assert.HasCount(1, r.Dice);
        Assert.IsTrue(r.Dice[0] is >= 1 and <= 20);
        Assert.AreEqual(5, r.Mod);
        Assert.AreEqual(r.Dice[0] + 5, r.Total);
    }

    [TestMethod]
    public void CountDefaultsToOne()
    {
        var r = One("d6");
        Assert.HasCount(1, r.Dice);
        Assert.AreEqual(r.Dice[0], r.Total);
    }

    [TestMethod]
    public void NegativeModifier()
    {
        var r = One("3d6-2");
        Assert.HasCount(3, r.Dice);
        Assert.AreEqual(-2, r.Mod);
        Assert.AreEqual(r.Dice.Sum() - 2, r.Total);
    }

    [TestMethod]
    public void NoModifierIsNotShown()
    {
        var result = Roll("2d8");
        StringAssert.Matches(result.Stdout.Trim(), new Regex(@"^2d8: \[\d, \d\] = \d+$"));
    }

    [TestMethod]
    public void KeepHighest()
    {
        var r = One("4d6kh3");
        Assert.HasCount(4, r.Dice);
        CollectionAssert.AreEqual(r.Dice.OrderDescending().Take(3).ToArray(), r.Kept);
        Assert.AreEqual(r.Kept!.Sum(), r.Total);
    }

    [TestMethod]
    public void AdvantageAndDisadvantage()
    {
        // Repeated so that a roller keeping the wrong die cannot pass by luck.
        var result = Roll(Enumerable.Repeat(new[] { "2d20kh1+5", "2d20kl1+5" }, 10).SelectMany(x => x).ToArray());
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        foreach (string line in result.StdoutLines)
        {
            var r = Parse(line);
            int pick = r.Expr.Contains("kh") ? r.Dice.Max() : r.Dice.Min();
            CollectionAssert.AreEqual(new[] { pick }, r.Kept, line);
            Assert.AreEqual(pick + 5, r.Total, line);
        }
    }

    [TestMethod]
    public void UpperCaseIsAccepted()
    {
        var r = One("2D20KH1");
        Assert.AreEqual("2D20KH1", r.Expr);
        CollectionAssert.AreEqual(new[] { r.Dice.Max() }, r.Kept);
    }

    [TestMethod]
    public void LeadingZeros()
    {
        var r = One("02d06");
        Assert.HasCount(2, r.Dice);
        Assert.IsTrue(r.Dice.All(d => d is >= 1 and <= 6));
    }

    [TestMethod]
    public void SeveralExpressionsInOrder()
    {
        var result = Roll("1d20+5", "2d6+3", "4d6kh3");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var rolls = result.StdoutLines.Select(Parse).ToArray();
        CollectionAssert.AreEqual(new[] { "1d20+5", "2d6+3", "4d6kh3" }, rolls.Select(r => r.Expr).ToArray());
    }

    [TestMethod]
    public void EveryDieIsInRangeAndEveryFaceTurnsUp()
    {
        var result = Roll("100d4", "100d4", "100d1000", "100d2");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var lines = result.StdoutLines.Select(Parse).ToArray();
        int[] d4a = lines[0].Dice, d4b = lines[1].Dice, d1000 = lines[2].Dice, d2 = lines[3].Dice;
        Assert.HasCount(100, d1000);
        Assert.IsTrue(d1000.All(d => d is >= 1 and <= 1000));
        // 200 d4s missing a face has odds of about 1 in 10^24.
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, d4a.Concat(d4b).Distinct().ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, d2.Distinct().ToArray());
    }

    [TestMethod]
    public void NoArgumentsPrintsUsage()
    {
        var result = Roll();
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stdout, "usage:");
    }

    [TestMethod]
    public void RejectsWhatIsNotDice()
    {
        foreach (string expr in new[] { "banana", "1d", "d", "1d20+", "1d20kh", "2d6+1d4", "1d20 +5" })
        {
            var result = Roll(expr);
            Assert.AreEqual(1, result.ExitCode, expr);
            StringAssert.Contains(result.Stdout, "not a dice expression", expr);
            Assert.DoesNotContain(" = ", result.Stdout, expr);
        }
    }

    [TestMethod]
    public void RejectsOutOfRange()
    {
        foreach (string expr in new[] { "101d6", "0d6", "1d1", "1d1001" })
        {
            var result = Roll(expr);
            Assert.AreEqual(1, result.ExitCode, expr);
            StringAssert.Contains(result.Stdout, "out of range", expr);
            Assert.DoesNotContain(" = ", result.Stdout, expr);
        }
    }

    [TestMethod]
    public void RejectsKeepingMoreDiceThanRolled()
    {
        foreach (var (expr, text) in new[] { ("2d20kh3", "cannot keep 3 of 2 dice"), ("2d20kl0", "cannot keep 0 of 2 dice") })
        {
            var result = Roll(expr);
            Assert.AreEqual(1, result.ExitCode, expr);
            StringAssert.Contains(result.Stdout, text, expr);
            Assert.DoesNotContain(" = ", result.Stdout, expr);
        }
    }

    [TestMethod]
    public void ABadExpressionDoesNotStopTheGoodOnes()
    {
        var result = Roll("1d20", "banana", "1d6");
        Assert.AreEqual(1, result.ExitCode);
        var lines = result.StdoutLines;
        Assert.HasCount(3, lines, result.Stdout);
        Assert.AreEqual("1d20", Parse(lines[0]).Expr);
        StringAssert.Contains(lines[1], "banana: not a dice expression");
        Assert.AreEqual("1d6", Parse(lines[2]).Expr);
    }
}
