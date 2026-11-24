using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>
/// <c>prepare</c>, <c>run</c> and <c>pilot</c> end to end with a stand-in for Claude Code (spec 024, FR-002 to
/// FR-007, FR-010). Nothing here starts a model.
/// </summary>
[TestClass]
public sealed class RunnerTests
{
    /// <summary>The hash of every file under a folder, by relative path, to show it did not change.</summary>
    /// <param name="folder">The folder.</param>
    static Dictionary<string, string> Hashes(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(
            p => Path.GetRelativePath(folder, p),
            p => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p))));

    [TestMethod]
    public void ForkModeRunsEveryPointInEveryArmWithTheCopyAtItsMoment()
    {
        using var f = new SlipTestFixtures();
        var before = Hashes(f.Folder);
        var beforeLogs = Hashes(Folders.Project(f.ClaudeHome, f.Folder));
        var fake = new FakeClaude(f.ClaudeHome);
        Assert.AreEqual(0, f.Run(null, "prepare").Code);
        var (code, output, error) = f.Run(fake, "run");
        Assert.AreEqual(0, code, error);

        Assert.HasCount(4, fake.Calls);
        var atFork = fake.Calls.Where(c => c.Message == SlipTestFixtures.ForkText).ToList();
        Assert.HasCount(2, atFork);
        Assert.IsTrue(atFork.All(c => c.Files["adventures/c/a.md"] == "middle" && !c.Files.ContainsKey("adventures/c/new.md")));
        Assert.IsTrue(fake.Calls.Where(c => c.Message != SlipTestFixtures.ForkText).All(c => c.Files["adventures/c/a.md"] == "start"));
        Assert.IsTrue(fake.Calls.All(c => c.Call is { Model: "claude-test-1", Effort: "high", Permission: "auto" } && c.Call.Resume is not null));

        var rows = RunRow.ReadAll(f.Work);
        Assert.HasCount(4, rows);
        Assert.IsTrue(rows.All(r => r.Claude == "9.9.9" && r.Result.CostUsd == 0.02));
        Assert.HasCount(4, output.Trim().Split('\n'));
        Assert.IsTrue(File.Exists(Path.Combine(f.Work, "runs", "treatment", "p1-1.jsonl")));

        // FR-010: the DMC folder and its logs are as they were, and the copy's project folder is left empty.
        CollectionAssert.AreEquivalent(before, Hashes(f.Folder));
        CollectionAssert.AreEquivalent(beforeLogs, Hashes(Folders.Project(f.ClaudeHome, f.Folder)));
        Assert.IsEmpty(Directory.EnumerateFiles(Folders.Project(f.ClaudeHome, f.Copy)));
    }

    [TestMethod]
    public void TheForkResumesALogThatHoldsTheArmsRules()
    {
        using var f = new SlipTestFixtures();
        var fake = new FakeClaude(f.ClaudeHome);
        f.Run(null, "prepare");
        f.Run(fake, "run");
        var fork = LogFile.Read(Path.Combine(f.Work, "runs", "treatment", "p1-1.jsonl"));
        Assert.IsTrue(PilotCheck.CheckRules(fork, "# Treatment rules\n"));
        Assert.IsFalse(PilotCheck.CheckRules(fork, "# Control rules\n"));
        Assert.IsFalse(PilotCheck.CheckRules(fork, SlipTestFixtures.RecordedRules));
    }

    [TestMethod]
    public void ASecondRunSkipsTheRunsAlreadyDone()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        var fake = new FakeClaude(f.ClaudeHome);
        f.Run(fake, "run", "--pilot");
        Assert.HasCount(4, fake.Calls);
        f.WriteTrial(runs: 1);
        f.Run(fake, "run");
        Assert.HasCount(4, fake.Calls);
        Assert.HasCount(4, RunRow.ReadAll(f.Work));
    }

    [TestMethod]
    public void PrepareRefusesACopyFolderItDidNotMake()
    {
        using var f = new SlipTestFixtures();
        Directory.CreateDirectory(f.Copy);
        File.WriteAllText(Path.Combine(f.Copy, "keep.txt"), "mine");
        StringAssert.Contains(f.Run(null, "prepare").Error, "is not empty");
        Assert.AreEqual(0, new SlipTestFixtures().Run(null, "prepare").Code);
    }

    [TestMethod]
    public void ThePilotPassesWhenTheReplaysSawWhatPlaySaw()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        f.Run(new FakeClaude(f.ClaudeHome), "run", "--pilot");
        var (code, output, error) = f.Run(null, "pilot");
        Assert.AreEqual(0, code, output + error);
        StringAssert.Contains(output, "verdict: pass");
        StringAssert.Contains(output, "notice       4     4           0     0");
        StringAssert.Contains(output, "thinking     2     2           0     0");
        Assert.AreEqual(output, File.ReadAllText(Path.Combine(f.Work, "pilot.md")));
    }

    [TestMethod]
    public void ANoticePlayDidNotHaveFailsThePilotUnlessItIsDocumented()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        var fake = new FakeClaude(f.ClaudeHome);
        fake.Notices.Add("non_interactive");
        f.Run(fake, "run", "--pilot");
        var (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(1, code);
        StringAssert.Contains(output, "notice p1 control 1: fail: attachment:non_interactive");

        File.WriteAllText(Path.Combine(f.Work, "allow.tsv"), "notice\tattachment:non_interactive\tsays only that no one answers prompts\n");
        (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(0, code, output);
        StringAssert.Contains(output, "documented: attachment:non_interactive");
        Assert.DoesNotContain(SlipTestFixtures.ForkText, output);
    }

    [TestMethod]
    public void AHookOutputThatDiffersFailsThePilot()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        var fake = new FakeClaude(f.ClaudeHome) { Hook = _ => "Scene: another day." };
        f.Run(fake, "run", "--pilot");
        var (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(1, code);
        StringAssert.Contains(output, "hook p1 control 1: fail: hook_additional_context");
        Assert.DoesNotContain("another day", output);
    }

    [TestMethod]
    public void ThinkingThatDiffersBetweenArmsFailsThePilot()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        var fake = new FakeClaude(f.ClaudeHome) { Thinking = h => h.Contains("Treatment", StringComparison.Ordinal) ? (1, 1) : (1, 0) };
        f.Run(fake, "run", "--pilot");
        var (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(1, code);
        StringAssert.Contains(output, "thinking p1 all 1: fail:");
    }

    [TestMethod]
    public void ThinkingThatIsNotRecordedPassesOnlyAsDocumented()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        f.Run(new FakeClaude(f.ClaudeHome) { Thinking = _ => null }, "run", "--pilot");
        Assert.AreEqual(1, f.Run(null, "pilot").Code);
        File.WriteAllText(Path.Combine(f.Work, "allow.tsv"), "thinking\tnot recorded\tthis version writes no thinking_drop\n");
        Assert.AreEqual(0, f.Run(null, "pilot").Code);
    }

    [TestMethod]
    public void FreshModeStartsOneBaseSessionPerArmWithItsOverlay()
    {
        using var f = new SlipTestFixtures();
        foreach (string arm in new[] { "a", "b" })
        {
            Directory.CreateDirectory(Path.Combine(f.Work, "overlays", arm, ".claude"));
            File.WriteAllText(Path.Combine(f.Work, "overlays", arm, ".claude", $"{arm}.txt"), arm);
        }
        f.WriteTrial(mode: "fresh", arms: new JsonObject { ["a"] = "overlays/a", ["b"] = "overlays/b" });
        var trial = JsonNode.Parse(File.ReadAllText(Path.Combine(f.Work, "trial.json")))!.AsObject();
        trial["pass"] = new JsonObject { ["arm"] = "b", ["against"] = "a", ["below"] = 0 };
        File.WriteAllText(Path.Combine(f.Work, "trial.json"), trial.ToJsonString());
        var fake = new FakeClaude(f.ClaudeHome);
        Assert.AreEqual(0, f.Run(null, "prepare").Code);
        var (code, _, error) = f.Run(fake, "run");
        Assert.AreEqual(0, code, error);

        Assert.HasCount(6, fake.Calls);
        Assert.HasCount(2, fake.Calls.Where(c => c.Call.Resume is null && c.Message == "/dmc-resume-campaign"));
        Assert.IsTrue(File.Exists(Path.Combine(f.Work, "base", "a.jsonl")));
        Assert.IsTrue(File.Exists(Path.Combine(f.Work, "base", "b-adventures", "c", "a.md")));
        var rows = RunRow.ReadAll(f.Work);
        Assert.HasCount(2, rows.Where(r => r.Point == "base"));
        Assert.HasCount(4, rows.Where(r => r.Point != "base"));
        // The overlay of one arm never stays for the other.
        Assert.IsFalse(File.Exists(Path.Combine(f.Copy, ".claude", "a.txt")) && File.Exists(Path.Combine(f.Copy, ".claude", "b.txt")));
    }
}
