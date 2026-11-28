using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>The blind export, the score and the command line of a run (spec 024, FR-003, FR-008, FR-009).</summary>
[TestClass]
public sealed class ScoringTests
{
    /// <summary>A prepared and run trial of the fixtures, two runs per point and arm.</summary>
    /// <param name="f">The fixtures.</param>
    static void RunAll(SlipTestFixtures f)
    {
        f.WriteTrial(runs: 2);
        Assert.AreEqual(0, f.Run(null, "prepare").Code);
        Assert.AreEqual(0, f.Run(new FakeClaude(f.ClaudeHome), "run").Code);
    }

    [TestMethod]
    public void TheExportHidesTheArmAndHoldsNoToolOrHookText()
    {
        using var f = new SlipTestFixtures();
        RunAll(f);
        Assert.AreEqual(0, f.Run(null, "export").Code);
        string replies = File.ReadAllText(Path.Combine(f.Work, "export", "replies.md"));
        Assert.DoesNotContain("control", replies);
        Assert.DoesNotContain("treatment", replies);
        Assert.DoesNotContain("dmc roll", replies);
        Assert.DoesNotContain(SlipTestFixtures.HookText, replies);
        Assert.Contains("Reply to: " + SlipTestFixtures.ForkText, replies);
        Assert.Contains("A slip here: the sword is a dagger", replies);

        var map = File.ReadAllLines(Path.Combine(f.Work, "export", ".map.tsv")).Skip(1).ToList();
        Assert.HasCount(8, map);
        Assert.HasCount(8, map.Select(l => l.Split('\t')[0]).Distinct());
        var order = map.Select(l => l.Split('\t')[1] + l.Split('\t')[2]).ToList();
        var runOrder = RunRow.ReadAll(f.Work).Select(r => r.Point + r.Arm).ToList();
        Assert.IsFalse(order.SequenceEqual(runOrder), "the export keeps the run order");
        Assert.HasCount(9, File.ReadAllLines(Path.Combine(f.Work, "export", "scores.tsv")));
    }

    [TestMethod]
    public void TheScoreGivesEachArmsRateAndThePassLine()
    {
        using var f = new SlipTestFixtures();
        RunAll(f);
        f.Run(null, "export");
        Assert.AreEqual(1, f.Run(null, "score").Code, "a scores file with no scores is refused");
        var map = File.ReadAllLines(Path.Combine(f.Work, "export", ".map.tsv")).Skip(1).Select(l => l.Split('\t')).ToList();
        File.WriteAllLines(Path.Combine(f.Work, "export", "scores.tsv"),
            ["id\tslip\tclass\tnote", .. map.Select(c => $"{c[0]}\t{(c[2] == "control" && c[1] == "p1" ? 1 : 0)}\tfact\t")]);
        var (code, output, error) = f.Run(null, "score");
        Assert.AreEqual(0, code, error);
        StringAssert.Contains(output, "control       4       2      2  0.500");
        StringAssert.Contains(output, "treatment     4       2      0  0.000  0.000 to 0.000");
        StringAssert.Contains(output, "treatment - control: -0.500");
        StringAssert.Contains(output, "pass line: treatment - control below 0:");
        StringAssert.Contains(output, "cost: 8 runs, $0.16, 8,440 input tokens (95% from the cache), 800 output tokens");
        Assert.AreEqual(output, File.ReadAllText(Path.Combine(f.Work, "result.md")));
        Assert.AreEqual(1, f.Run(null, "export").Code, "a second export would overwrite the scores");

        var lines = File.ReadAllLines(Path.Combine(f.Work, "export", "scores.tsv")).ToList();
        lines[2] = lines[1];
        File.WriteAllLines(Path.Combine(f.Work, "export", "scores.tsv"), lines);
        StringAssert.Contains(f.Run(null, "score").Error, "is scored twice");
    }

    [TestMethod]
    public void TheBootstrapIsOverForkPointsAndRepeatable()
    {
        var points = Enumerable.Range(1, 10).Select(i => $"p{i}").ToList();
        var slips = points.ToDictionary(p => p, p => int.Parse(p[1..], System.Globalization.CultureInfo.InvariantCulture) <= 5 ? 1.0 : 0.0);
        double Rate(IReadOnlyList<string> drawn) => drawn.Average(p => slips[p]);
        var a = Scoring.Bootstrap(points, Rate);
        var b = Scoring.Bootstrap(points, Rate);
        Assert.AreEqual(a, b);
        Assert.AreEqual(0.5, a.Rate);
        Assert.IsTrue(a.Low is > 0.1 and < 0.5 && a.High is > 0.5 and < 0.9, $"{a.Low} to {a.High}");
    }

    [TestMethod]
    public void TheCommandLineCarriesTheOriginalsSettings()
    {
        CollectionAssert.AreEqual(
            new[] { "-p", "--output-format", "json", "--resume", "id", "--model", "m", "--effort", "high", "--permission-mode", "auto" },
            ClaudeRunner.Arguments(new ClaudeCall("id", "m", "high", "auto")));
        CollectionAssert.AreEqual(new[] { "-p", "--output-format", "json" }, ClaudeRunner.Arguments(new ClaudeCall(null, null, null, null)));
    }

    [TestMethod]
    public void TheJsonResultIsReadAndAnythingElseRefused()
    {
        var r = ClaudeRunner.Parse("""{"type":"result","session_id":"s1","total_cost_usd":0.0046,"usage":{"input_tokens":6,"output_tokens":446,"cache_read_input_tokens":108018,"cache_creation_input_tokens":13620}}""");
        Assert.AreEqual(new ClaudeResult("s1", 6, 446, 108018, 13620, 0.0046, false), r);
        Assert.Throws<SlipTestException>(() => ClaudeRunner.Parse("Error: not signed in"));
    }
}
