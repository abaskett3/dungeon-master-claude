using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>The trial's input files and the lock (spec 024, FR-002): each malformed input is refused with one line.</summary>
[TestClass]
public sealed class TrialTests
{
    [TestMethod]
    public void AWellFormedTrialLoads()
    {
        using var f = new SlipTestFixtures();
        var trial = Trial.Load(f.Work);
        Assert.AreEqual("fork", trial.Mode);
        Assert.HasCount(2, trial.Arms);
        Assert.HasCount(2, trial.Points);
        Assert.AreEqual("the sword is a dagger", trial.Key["p1"]);
        Assert.AreEqual(new PassLine("treatment", "control", 0), trial.Pass);
    }

    [TestMethod]
    [DataRow("trial.json", "{ not json", "not valid JSON")]
    [DataRow("points.tsv", "p1\tonly-two\n", "points.tsv line 1")]
    [DataRow("points.tsv", "p1\ts\tm\np1\ts\tm2\n", "listed twice")]
    [DataRow("key.tsv", "p1\tthe sword\n", "no row for point p2")]
    [DataRow("key.tsv", "p1\tx\np2\ty\np3\tz\n", "key.tsv line 3")]
    [DataRow("allow.tsv", "other\tx\twhy\n", "allow.tsv line 1")]
    public void AMalformedFileIsRefusedWithOneLine(string file, string content, string expected)
    {
        using var f = new SlipTestFixtures();
        File.WriteAllText(Path.Combine(f.Work, file), content);
        var (code, _, error) = f.Run(null, "prepare");
        Assert.AreEqual(1, code);
        Assert.Contains(expected, error);
        Assert.AreEqual(1, error.Trim().Split('\n').Length, error);
        Assert.IsFalse(File.Exists(Path.Combine(f.Work, "lock.json")));
    }

    [TestMethod]
    public void TrialSettingsAreChecked()
    {
        using var f = new SlipTestFixtures();
        f.WriteTrial(runs: 0);
        StringAssert.Contains(f.Run(null, "prepare").Error, "runs must be");
        f.WriteTrial(mode: "replay");
        StringAssert.Contains(f.Run(null, "prepare").Error, "mode must be");
        f.WriteTrial(arms: new JsonObject { ["control"] = "arms/missing.md" });
        StringAssert.Contains(f.Run(null, "prepare").Error, "no rules file");
    }

    [TestMethod]
    public void ACopyFolderInsideTheDmcFolderIsRefused()
    {
        using var f = new SlipTestFixtures();
        var trial = JsonNode.Parse(File.ReadAllText(Path.Combine(f.Work, "trial.json")))!.AsObject();
        trial["copy"] = Path.Combine(f.Folder, "copy");
        File.WriteAllText(Path.Combine(f.Work, "trial.json"), trial.ToJsonString());
        StringAssert.Contains(f.Run(null, "prepare").Error, "must be separate");
    }

    [TestMethod]
    public void ALockedFileThatChangesStopsTheRunAndTheScore()
    {
        using var f = new SlipTestFixtures();
        Assert.AreEqual(0, f.Run(null, "prepare").Code);
        File.AppendAllText(Path.Combine(f.Work, "key.tsv"), "# a later thought\n");
        var run = f.Run(new FakeClaude(f.ClaudeHome), "run");
        Assert.AreEqual(1, run.Code);
        StringAssert.Contains(run.Error, "key.tsv changed after prepare");
        StringAssert.Contains(f.Run(null, "score").Error, "key.tsv changed after prepare");
    }

    [TestMethod]
    public void RunBeforePrepareIsRefused()
    {
        using var f = new SlipTestFixtures();
        StringAssert.Contains(f.Run(new FakeClaude(f.ClaudeHome), "run").Error, "not prepared");
    }

    [TestMethod]
    public void AFixedPointThatIsNotATypedMessageIsRefused()
    {
        using var f = new SlipTestFixtures();
        File.WriteAllText(Path.Combine(f.Work, "points.tsv"), $"p1\t{SlipTestFixtures.Session}\tu-tool\np2\t{SlipTestFixtures.Session}\tnone\n");
        StringAssert.Contains(f.Run(null, "prepare").Error, "point p1: message u-tool is not text the player typed");
    }

    [TestMethod]
    public void AnUnknownCommandOrMissingWorkFolderPrintsTheUsage()
    {
        var error = new StringWriter();
        Assert.AreEqual(1, Dmc.SlipTest.Program.Run(["run"], new StringWriter(), error, "home", null, DateTimeOffset.UnixEpoch, new Random(1)));
        StringAssert.StartsWith(error.ToString(), "slip-test: usage:");
    }
}
