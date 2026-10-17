using Dmc.Dice;
using Dmc.Tests.Support;

namespace Dmc.Tests.Roll;

/// <summary>The roll tests against the library code, in this process.</summary>
[TestClass]
public sealed class RollInProcessTests : RollContract
{
    /// <summary>Runs <c>dmc roll</c> in this process.</summary>
    protected override ProcessResult Roll(params string[] exprs) => DmcRunner.InProcess(["roll", .. exprs]);
}

/// <summary>The roll tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RollBlackBoxTests : RollContract
{
    /// <summary>Runs <c>dmc roll</c> as the published binary, in a subprocess.</summary>
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
        /// <summary>The index of the next draw to hand out.</summary>
        int _next;

        /// <summary>How many draws have been used so far.</summary>
        public int Drawn => _next;

        /// <summary>Fills a 4-byte buffer with the next draw. Asking for more draws than were given fails the test with an index error.</summary>
        public void Fill(Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, draws[_next++]);
    }

    /// <summary>Rolls one expression with a <see cref="Scripted"/> source that gives these draws.</summary>
    static RollOutcome Roll(string expr, params uint[] draws) => new DiceRoller(new Scripted(draws)).Roll(expr);

    /// <summary>Roll one argument (<c>label:dice@target</c>) from scripted draws.</summary>
    static RollOutcome Arg(string argument, params uint[] draws) => new DiceRoller(new Scripted(draws)).RollArgument(argument);

    /// <summary>
    /// Issue #6, finding 4: the line for <c>label:dice@target</c> is the plain line with the label before it and the
    /// verdict after, and meeting the target succeeds. A d20 draw of n is the face n % 20 + 1 (0 is a 1, 19 is a 20).
    /// </summary>
    [TestMethod]
    public void ALabelAndATargetMakeTheLineReadyToPaste()
    {
        Assert.AreEqual("Pip, shot: 2d20kl1+5: [17, 9] keep [9] +5 = 14 vs AC 14, hit", Arg("Pip, shot:2d20kl1+5@AC14", 16, 8).Line);
        Assert.AreEqual("Pip, shot: 2d20kl1+5: [17, 8] keep [8] +5 = 13 vs AC 14, miss", Arg("Pip, shot:2d20kl1+5@AC14", 16, 7).Line);
        Assert.AreEqual("Kole, Dex save: 1d20+3: [10] +3 = 13 vs DC 13, success", Arg("Kole, Dex save:1d20+3@DC13", 9).Line);
        Assert.AreEqual("Kole, Dex save: 1d20+3: [9] +3 = 12 vs DC 13, fail", Arg("Kole, Dex save:1d20+3@DC13", 8).Line);
        // Each part is optional on its own, and a space after the colon is only for reading.
        Assert.AreEqual("Pip: 1d20+5: [1] +5 = 6", Arg("Pip: 1d20+5", 0).Line);
        Assert.AreEqual("1d20+5: [1] +5 = 6 vs AC 10, miss (natural 1)", Arg("1d20+5@ac10", 0).Line);
    }

    /// <summary>
    /// The 5e rule: a natural 20 hits and a natural 1 misses against an AC, for a single d20 or the kept one; two d20s
    /// added up have no natural roll, and checks and saves (a DC) have no automatic result.
    /// </summary>
    [TestMethod]
    public void ANaturalTwentyOrOneDecidesAnAttackWhateverTheTotal()
    {
        Assert.AreEqual("1d20-5: [20] -5 = 15 vs AC 30, hit (natural 20)", Arg("1d20-5@AC30", 19).Line);
        Assert.AreEqual("1d20+30: [1] +30 = 31 vs AC 5, miss (natural 1)", Arg("1d20+30@AC5", 0).Line);
        // Under advantage or disadvantage it is the kept die that counts.
        Assert.AreEqual("2d20kh1-9: [20, 1] keep [20] -9 = 11 vs AC 30, hit (natural 20)", Arg("2d20kh1-9@AC30", 19, 0).Line);
        Assert.AreEqual("2d20kl1+30: [20, 1] keep [1] +30 = 31 vs AC 5, miss (natural 1)", Arg("2d20kl1+30@AC5", 19, 0).Line);
        Assert.AreEqual("2d20kh1+30: [1, 2] keep [2] +30 = 32 vs AC 5, hit", Arg("2d20kh1+30@AC5", 0, 1).Line);
        // Two d20s added up have no natural roll, and 5e gives checks and saves no automatic success or failure.
        Assert.AreEqual("2d20-40: [20, 20] -40 = 0 vs AC 10, miss", Arg("2d20-40@AC10", 19, 19).Line);
        Assert.AreEqual("1d20: [20] = 20 vs DC 30, fail", Arg("1d20@DC30", 19).Line);
        Assert.AreEqual("1d20+30: [1] +30 = 31 vs DC 5, success", Arg("1d20+30@DC5", 0).Line);
    }

    /// <summary>The bare syntax is untouched: <c>RollArgument</c> gives <c>Roll</c>'s line, good or bad, byte for byte.</summary>
    [TestMethod]
    public void WithoutALabelOrATargetTheLineIsExactlyTheOldOne()
    {
        foreach (string expr in new[] { "1d20+5", "2D20KH1", "4d6kh3", "banana", "é中", "1d20 +5" })
        {
            var plain = new DiceRoller(new Scripted(9, 9, 9, 9)).Roll(expr);
            Assert.AreEqual(plain.Line, Arg(expr, 9, 9, 9, 9).Line, expr);
            Assert.AreEqual(plain.Rolled, Arg(expr, 9, 9, 9, 9).Rolled, expr);
        }
    }

    /// <summary>A bad label or target is one line that starts with the label, and no die is drawn (none are scripted).</summary>
    [TestMethod]
    public void ABadLabelOrTargetIsOneLineAndDrawsNoDice()
    {
        foreach (var (argument, line) in new[]
                 {
                     ("Pip: 1d20@14", "Pip: 1d20: target \"@14\" is not @AC<n> or @DC<n> (try @AC14, @DC13)"),
                     ("1d20@", "1d20: target \"@\" is not @AC<n> or @DC<n> (try @AC14, @DC13)"),
                     ("Pip: 1d20@AC1x", "Pip: 1d20: target \"@AC1x\" is not @AC<n> or @DC<n> (try @AC14, @DC13)"),
                     ("Pip: 1d20@AC99999999999", "Pip: 1d20: target \"@AC99999999999\" is not @AC<n> or @DC<n> (try @AC14, @DC13)"),
                     ("Pip: banana@AC14", "Pip: banana: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)"),
                     ("Pip: 2d20kh3@AC14", "Pip: 2d20kh3: cannot keep 3 of 2 dice"),
                     (": 1d20", ": 1d20: the label before ':' is empty (write label:dice, or just the dice)"),
                     ("Pip\nX: 1d20", "Pip\\nX: 1d20: a label must be on one line"),
                     ("Pip: 1d20\n@AC14", "Pip: 1d20\\n: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)"),
                 })
        {
            var outcome = Arg(argument); // no draws are scripted: drawing a die would fail
            Assert.IsFalse(outcome.Rolled, argument);
            Assert.AreEqual(line, outcome.Line, argument);
        }
    }

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
    }

    // Item 7: a modifier is a 64-bit number, as in roll.sh (roll.ps1 silently drops one that does not fit an int:
    // it prints the roll as if there were no modifier). One that does not fit even 64 bits is named in its own line.
    [TestMethod]
    public void AModifierBeyondAnIntIsKept()
    {
        Assert.AreEqual("1d20+99999999999: [1] +99999999999 = 100000000000", Roll("1d20+99999999999", 0).Line);
        Assert.AreEqual("1d20-99999999999: [1] -99999999999 = -99999999998", Roll("1d20-99999999999", 0).Line);
    }

    [TestMethod]
    public void AModifierTooLargeForAnyNumberHasItsOwnOneLineMessage()
    {
        const string max = "9223372036854775807";
        var tooBig = Roll("1d20+99999999999999999999");
        Assert.IsFalse(tooBig.Rolled);
        Assert.AreEqual($"1d20+99999999999999999999: modifier +99999999999999999999 is too large (at most {max})", tooBig.Line);
        Assert.AreEqual($"1d6-99999999999999999999: modifier -99999999999999999999 is too large (at most {max})",
            Roll("1d6-99999999999999999999").Line);
        // The modifier fits, the total does not.
        Assert.AreEqual($"1d20+{max}: modifier +{max} is too large (at most {max})", Roll($"1d20+{max}", 5).Line);
        // The largest total that does fit.
        Assert.AreEqual($"1d20+9223372036854775806: [1] +9223372036854775806 = {max}", Roll("1d20+9223372036854775806", 0).Line);
    }

    // Item 3: an error is one line, whatever was typed.
    [TestMethod]
    public void LineBreaksInAnExpressionAreWrittenOutInTheErrorLine()
    {
        Assert.AreEqual("a\\nb: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)", Roll("a\nb").Line);
        Assert.AreEqual("a\\r\\nb: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)", Roll("a\r\nb").Line);
        Assert.AreEqual("1d20\\n: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)", Roll("1d20\n").Line);
        // FF, NEL, LS and PS are line breaks to string.ReplaceLineEndings, so they are written out too.
        string odd = "x" + (char)0x2028 + "y" + (char)0x85 + "z" + (char)0x0C + (char)0x2029;
        Assert.AreEqual("x\\u2028y\\u0085z\\u000c\\u2029: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)", Roll(odd).Line);
        Assert.IsFalse(Roll("a\nb\nc").Line.Contains((char)10));
    }

    /// <summary>
    /// A stream that counts bytes up (0, 1, 2, ... wrapping at 256) and records how many bytes each read asked for. The
    /// members other than <c>Read</c> are only what a <see cref="Stream"/> needs: it cannot seek or be written.
    /// </summary>
    // Item 8: dice are read from /dev/urandom in pools, like roll.sh, not one read per die.
    sealed class CountingStream : Stream
    {
        /// <summary>The size of each read, in order.</summary>
        public List<int> Reads { get; } = [];

        /// <summary>The number of bytes handed out so far, which is also the value of the next one before wrapping.</summary>
        long _position;

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads.Add(count);
            for (int i = 0; i < count; i++)
                buffer[offset + i] = (byte)(_position++ % 256);
            return count;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    public void RandomBytesAreReadInPools()
    {
        var stream = new CountingStream();
        using var random = new OsRandomBytes(stream);
        var buffer = new byte[4];
        var all = new List<byte>();
        for (int i = 0; i < 100; i++) // 400 bytes: 6.25 pools of 64
        {
            random.Fill(buffer);
            all.AddRange(buffer);
        }
        Assert.AreEqual(7, stream.Reads.Count);
        Assert.IsTrue(stream.Reads.All(r => r == OsRandomBytes.PoolSize));
        // Every byte is used, in order, across the pool boundaries.
        CollectionAssert.AreEqual(Enumerable.Range(0, 400).Select(i => (byte)(i % 256)).ToArray(), all.ToArray());
    }

    [TestMethod]
    public void AFailureToReadTheSourceIsTheScriptsError()
    {
        using var random = new OsRandomBytes(new ThrowingStream());
        var e = Assert.ThrowsExactly<DmcException>(() => random.Fill(new byte[4]));
        Assert.AreEqual("cannot read /dev/urandom, no dice rolled", e.Message);
        Assert.AreEqual(2, e.ExitCode);
    }

    /// <summary>A stream whose every read fails, as a source of random bytes that cannot be read does. The other members are only what a <see cref="Stream"/> needs.</summary>
    sealed class ThrowingStream : Stream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("no entropy");
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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
