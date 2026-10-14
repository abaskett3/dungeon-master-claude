using Dmc.Dice;
using Dmc.Tests.Support;

namespace Dmc.Tests.Roll;

/// <summary>The 15 roll tests against the library code, in this process.</summary>
[TestClass]
public sealed class RollInProcessTests : RollContract
{
    protected override ProcessResult Roll(params string[] exprs) => DmcRunner.InProcess(["roll", .. exprs]);
}

/// <summary>The 15 roll tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RollBlackBoxTests : RollContract
{
    protected override ProcessResult Roll(params string[] exprs) => DmcRunner.BlackBox(["roll", .. exprs]);
}

/// <summary>
/// Exact-value tests that the random-looking contract tests cannot make: they feed the roller a scripted source
/// of bits. They pin down the rejection rule, which is what keeps every face equally likely.
/// </summary>
[TestClass]
public sealed class DiceRollerTests
{
    /// <summary>Hands out the given 32-bit draws in order, as little-endian bytes.</summary>
    sealed class Scripted(params uint[] draws) : IRandomBytes
    {
        int _next;
        public int Drawn => _next;

        public void Fill(Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, draws[_next++]);
    }

    static RollOutcome Roll(string expr, params uint[] draws) => new DiceRoller(new Scripted(draws)).Roll(expr);

    [TestMethod]
    public void ADrawBecomesTheFaceByModulo()
    {
        Assert.AreEqual("1d6: [4] = 4", Roll("1d6", 9).Line); // 9 % 6 + 1
        Assert.AreEqual("1d20+5: [1] +5 = 6", Roll("1d20+5", 0).Line);
        Assert.AreEqual("1d20-5: [20] -5 = 15", Roll("1d20-5", 19).Line);
    }

    [TestMethod]
    public void DrawsAboveTheLargestMultipleOfTheDieSizeAreRedrawn()
    {
        // For a d6 the largest multiple of 6 that fits is 4294967292: 4294967292 and up are thrown away.
        var source = new Scripted(uint.MaxValue, 4294967292, 4294967291);
        var outcome = new DiceRoller(source).Roll("1d6");
        Assert.AreEqual("1d6: [6] = 6", outcome.Line); // 4294967291 % 6 = 5
        Assert.AreEqual(3, source.Drawn);
    }

    [TestMethod]
    public void EveryDrawBelowTheLimitIsAccepted()
    {
        // A d4 divides 2^32 evenly: nothing is ever redrawn.
        var source = new Scripted(uint.MaxValue);
        Assert.AreEqual("1d4: [4] = 4", new DiceRoller(source).Roll("1d4").Line);
        Assert.AreEqual(1, source.Drawn);
    }

    [TestMethod]
    public void KeepHighestAndLowestTakeTheRightDice()
    {
        Assert.AreEqual("4d6kh3: [1, 6, 3, 5] keep [6, 5, 3] = 14", Roll("4d6kh3", 0, 5, 2, 4).Line);
        Assert.AreEqual("4d6kl3: [1, 6, 3, 5] keep [1, 3, 5] = 9", Roll("4d6kl3", 0, 5, 2, 4).Line);
        Assert.AreEqual("2d20kh1+5: [3, 17] keep [17] +5 = 22", Roll("2d20kh1+5", 2, 16).Line);
        Assert.AreEqual("2d20kl1+5: [3, 17] keep [3] +5 = 8", Roll("2d20kl1+5", 2, 16).Line);
    }

    [TestMethod]
    public void TheExpressionIsEchoedAsTyped()
    {
        Assert.AreEqual("2D20KH1: [3, 17] keep [17] = 17", Roll("2D20KH1", 2, 16).Line);
    }

    [TestMethod]
    public void TheMinimumTotalCanBeNegative()
    {
        Assert.AreEqual("1d4-9: [1] -9 = -8", Roll("1d4-9", 0).Line);
    }

    [TestMethod]
    public void NumbersTooLongForAnIntAreOutOfRange()
    {
        const string text = "out of range (1-100 dice, 2-1000 sides)";
        Assert.AreEqual($"99999999999d6: {text}", Roll("99999999999d6").Line);
        Assert.AreEqual($"1d99999999999: {text}", Roll("1d99999999999").Line);
        Assert.AreEqual($"1d20+99999999999: {text}", Roll("1d20+99999999999").Line);
    }

    [TestMethod]
    public void KeepCountIsShownWithoutLeadingZeros()
    {
        Assert.AreEqual("2d20kh007: cannot keep 7 of 2 dice", Roll("2d20kh007").Line);
        Assert.AreEqual("2d20kl00: cannot keep 0 of 2 dice", Roll("2d20kl00").Line);
    }

    [TestMethod]
    public void OnlyAsciiDigitsAreDice()
    {
        // Arabic-Indic digits are not dice: the scripts' regex has [0-9] semantics in bash.
        Assert.IsFalse(Roll("1d٢٠").Rolled);
        // A trailing newline is not part of the expression.
        Assert.IsFalse(Roll("1d20\n").Rolled);
    }

    [TestMethod]
    public void ARejectedExpressionDrawsNoDice()
    {
        var source = new Scripted();
        var roller = new DiceRoller(source);
        Assert.IsFalse(roller.Roll("101d6").Rolled);
        Assert.IsFalse(roller.Roll("2d20kh3").Rolled);
        Assert.IsFalse(roller.Roll("banana").Rolled);
        Assert.AreEqual(0, source.Drawn);
    }

    [TestMethod]
    public void TheOsSourceGivesEveryFaceOfAD20()
    {
        using var random = new OsRandomBytes();
        var roller = new DiceRoller(random);
        var seen = new HashSet<int>();
        for (int i = 0; i < 2000; i++)
            seen.Add(roller.Die(20));
        // 2000 d20s missing a face has odds of about 1 in 10^34.
        CollectionAssert.AreEquivalent(Enumerable.Range(1, 20).ToArray(), seen.ToArray());
    }
}
