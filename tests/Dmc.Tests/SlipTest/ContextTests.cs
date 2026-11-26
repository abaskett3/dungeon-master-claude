using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>
/// An arm's text next to the player's message at each fork point (spec 029, #284, for the oracle test #270): the
/// context folder in <c>trial.json</c>, the hook in the copy's local settings, the <c>context</c> command it runs, and
/// the pilot's checks. Nothing here starts a model.
/// </summary>
[TestClass]
public sealed class ContextTests
{
    /// <summary>The treatment arm's text at point p1.</summary>
    const string P1Text = "Hala calls Nytha \"Tam\". Hala cannot fly.";

    /// <summary>The treatment arm's text at point p2.</summary>
    const string P2Text = "Tam calls Dessa \"Ma\".";

    /// <summary>Gives the treatment arm a context folder with a file for each point, and names it in <c>trial.json</c>.</summary>
    /// <param name="f">The fixtures.</param>
    /// <param name="p2">The text at p2.</param>
    static void AddContext(SlipTestFixtures f, string p2 = P2Text)
    {
        string folder = Path.Combine(f.Work, "context", "treatment");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "p1.md"), P1Text + "\n");
        File.WriteAllText(Path.Combine(folder, "p2.md"), p2);
        SetContext(f, new JsonObject { ["treatment"] = "context/treatment" });
    }

    /// <summary>Sets <c>context</c> in <c>trial.json</c>.</summary>
    /// <param name="f">The fixtures.</param>
    /// <param name="context">The value.</param>
    static void SetContext(SlipTestFixtures f, JsonNode context)
    {
        string path = Path.Combine(f.Work, "trial.json");
        var trial = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        trial["context"] = context;
        File.WriteAllText(path, trial.ToJsonString());
    }

    /// <summary>The hook outputs a run's fork recorded with the replayed message.</summary>
    /// <param name="f">The fixtures.</param>
    /// <param name="arm">The arm.</param>
    /// <param name="point">The point id.</param>
    static List<string> ForkHooks(SlipTestFixtures f, string arm, string point)
    {
        var fork = LogFile.Read(Path.Combine(f.Work, "runs", arm, $"{point}-1.jsonl"));
        int at = fork.LastTyped(point == "p1" ? SlipTestFixtures.ForkText : "We go down to the cellar.");
        Assert.IsGreaterThanOrEqualTo(0, at);
        return PilotCheck.Hooks(fork.TurnAfter(at)).Select(h => h.Text).ToList();
    }

    [TestMethod]
    public void AnArmWithContextGetsItsTextAtEachPoint()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        Assert.AreEqual(0, f.Run(null, "prepare").Code);
        var fake = new FakeClaude(f.ClaudeHome);
        var (code, _, error) = f.Run(fake, "run");
        Assert.AreEqual(0, code, error);

        Assert.HasCount(4, fake.Calls);
        for (int i = 0; i < fake.Calls.Count; i++)
        {
            string? settings = fake.LocalSettings[i];
            bool treatment = fake.Calls[i].Call.Resume is not null && settings is not null;
            if (!treatment)
            {
                Assert.IsNull(settings, "the control's call saw a hook");
                continue;
            }
            string point = fake.Calls[i].Message == SlipTestFixtures.ForkText ? "p1" : "p2";
            StringAssert.Contains(settings, $"context/treatment/{point}.md");
            StringAssert.Contains(settings, "UserPromptSubmit");
        }
        Assert.HasCount(2, fake.LocalSettings.OfType<string>());

        CollectionAssert.Contains(ForkHooks(f, "treatment", "p1"), P1Text);
        CollectionAssert.Contains(ForkHooks(f, "treatment", "p2"), P2Text);
        Assert.IsFalse(ForkHooks(f, "control", "p1").Contains(P1Text));
        Assert.IsFalse(File.Exists(ContextHook.SettingsPath(f.Copy)), "the copy kept a hook after the run");
    }

    [TestMethod]
    public void TheLocalSettingsAreAsTheDmcFolderHasThemAfterARun()
    {
        using var f = new SlipTestFixtures();
        string original = ContextHook.SettingsPath(f.Folder);
        Directory.CreateDirectory(Path.GetDirectoryName(original)!);
        File.WriteAllText(original, "{\"permissions\":{\"allow\":[\"Bash(ls)\"]},\"hooks\":{\"UserPromptSubmit\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"echo mine\"}]}]}}");
        AddContext(f);
        f.Run(null, "prepare");
        // A run that was stopped left a hook behind in the copy.
        ContextHook.Add(f.Copy, Path.Combine(f.Work, "context", "treatment", "p1.md"), SlipTestFixtures.HookCommand);

        var fake = new FakeClaude(f.ClaudeHome);
        Assert.AreEqual(0, f.Run(fake, "run").Code);
        var seen = fake.LocalSettings.OfType<string>().ToList();
        Assert.HasCount(4, seen);
        Assert.HasCount(2, seen.Where(s => s == File.ReadAllText(original)));
        foreach (string s in seen.Where(s => s != File.ReadAllText(original)))
        {
            var prompt = JsonNode.Parse(s)!["hooks"]!["UserPromptSubmit"]!.AsArray();
            Assert.HasCount(2, prompt);
            StringAssert.Contains(s, "echo mine");
            StringAssert.Contains(s, "Bash(ls)");
        }
        Assert.AreEqual(File.ReadAllText(original), File.ReadAllText(ContextHook.SettingsPath(f.Copy)));

        // A run that fails puts the file back too.
        File.Delete(Path.Combine(f.Work, "runs.tsv"));
        Assert.AreEqual(1, f.Run(new FakeClaude(f.ClaudeHome) { Fails = true }, "run").Code);
        Assert.AreEqual(File.ReadAllText(original), File.ReadAllText(ContextHook.SettingsPath(f.Copy)));
    }

    [TestMethod]
    public void ABlankContextFileAddsNoHook()
    {
        using var f = new SlipTestFixtures();
        AddContext(f, p2: "  \n");
        f.Run(null, "prepare");
        var fake = new FakeClaude(f.ClaudeHome);
        f.Run(fake, "run");
        Assert.HasCount(1, fake.LocalSettings.OfType<string>());
        f.Run(null, "run", "--pilot");
        var (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(0, code, output);
    }

    [TestMethod]
    public void TheContextCommandPrintsTheFile()
    {
        using var f = new SlipTestFixtures();
        string file = Path.Combine(f.Root, "p1.md");
        Directory.CreateDirectory(f.Root);
        File.WriteAllText(file, P1Text);
        var output = new StringWriter();
        Assert.AreEqual(0, Dmc.SlipTest.Program.Run(["context", file], output, new StringWriter(), f.ClaudeHome, null, DateTimeOffset.UtcNow, new Random(1)));
        var json = JsonNode.Parse(output.ToString())!["hookSpecificOutput"]!;
        Assert.AreEqual("UserPromptSubmit", json["hookEventName"]!.GetValue<string>());
        Assert.AreEqual(P1Text, json["additionalContext"]!.GetValue<string>());

        // A file that is itself JSON is still printed as text.
        File.WriteAllText(file, "{\"facts\": [1]}");
        output = new StringWriter();
        Dmc.SlipTest.Program.Run(["context", file], output, new StringWriter(), f.ClaudeHome, null, DateTimeOffset.UtcNow, new Random(1));
        Assert.AreEqual("{\"facts\": [1]}", JsonNode.Parse(output.ToString())!["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>());

        output = new StringWriter();
        Assert.AreEqual(0, Dmc.SlipTest.Program.Run(["context", Path.Combine(f.Root, "none.md")], output, new StringWriter(), f.ClaudeHome, null, DateTimeOffset.UtcNow, new Random(1)));
        Assert.AreEqual("", output.ToString());
        Assert.AreEqual(1, Dmc.SlipTest.Program.Run(["context"], output, new StringWriter(), f.ClaudeHome, null, DateTimeOffset.UtcNow, new Random(1)));
    }

    [TestMethod]
    public void TheHookCommandUsesForwardSlashesAndSingleQuotes()
    {
        Assert.AreEqual("'D:/r/bin/slip-test.exe' context {file}", ContextHook.CommandLine(@"D:\r\bin\slip-test.exe", @"D:\r\bin\slip-test.dll"));
        Assert.AreEqual("'/usr/bin/dotnet' '/r/bin/slip-test.dll' context {file}", ContextHook.CommandLine("/usr/bin/dotnet", "/r/bin/slip-test.dll"));
        Assert.AreEqual("'/w/$trial/it'\\''s `x` \"y\"'", ContextHook.Quoted("/w/$trial/it's `x` \"y\""));

        using var f = new SlipTestFixtures();
        ContextHook.Add(f.Copy, @"D:\w\context\oracle\p1.md", ContextHook.CommandLine(@"D:\r\bin\slip-test.exe", ""));
        string command = JsonNode.Parse(File.ReadAllText(ContextHook.SettingsPath(f.Copy)))!["hooks"]!["UserPromptSubmit"]![0]!["hooks"]![0]!["command"]!.GetValue<string>();
        Assert.AreEqual("'D:/r/bin/slip-test.exe' context 'D:/w/context/oracle/p1.md'", command);
    }

    [TestMethod]
    public void TheQuotedPathReachesTheProgramUnchangedThroughAShell()
    {
        if (OperatingSystem.IsWindows())
            Assert.Inconclusive("needs a POSIX shell on the PATH");
        string path = "/w/$trial/it's `x` \"y\"";
        var info = new System.Diagnostics.ProcessStartInfo("sh") { RedirectStandardOutput = true };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("printf %s " + ContextHook.Quoted(path));
        using var shell = System.Diagnostics.Process.Start(info)!;
        string printed = shell.StandardOutput.ReadToEnd();
        shell.WaitForExit();
        Assert.AreEqual(path, printed);
    }

    [TestMethod]
    public void PrepareRefusesAContextFileClaudeCodeWouldShorten()
    {
        using var f = new SlipTestFixtures();
        AddContext(f, p2: new string('x', ContextHook.MaxLength + 1));
        StringAssert.Contains(f.Run(null, "prepare").Error, "p2.md holds more than 10,000 characters");
    }

    [TestMethod]
    public void PrepareRefusesLocalSettingsTheHookCannotBeAddedTo()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        Directory.CreateDirectory(Path.Combine(f.Folder, ".claude"));
        File.WriteAllText(ContextHook.SettingsPath(f.Folder), "{\"a\": 1,}");
        StringAssert.Contains(f.Run(null, "prepare").Error, "is not valid JSON, so the context hook cannot be added to it");
    }

    [TestMethod]
    public void TheHookCheckLeavesOutEveryRecordOfTheContextHook()
    {
        var original = new[] { SlipTestFixtures.Hook("Scene: the cellar.", "s") };
        var once = new[] { SlipTestFixtures.Hook("Scene: the cellar.", "s"), SlipTestFixtures.Hook(P1Text, "s") };
        var twice = once.Append(SlipTestFixtures.Attachment(new JsonObject { ["type"] = "hook_success", ["content"] = P1Text + "\n", ["hookEvent"] = "UserPromptSubmit" }, "s")).ToArray();
        Assert.AreEqual(Outcome.Pass, PilotCheck.CheckHooks(original, once, [], context: P1Text).Item1);
        Assert.AreEqual(Outcome.Pass, PilotCheck.CheckHooks(original, twice, [], context: P1Text).Item1);
        Assert.AreEqual(Outcome.Fail, PilotCheck.CheckHooks(original, twice, []).Item1);
    }

    [TestMethod]
    public void PrepareRefusesAContextFolderWithoutAPointsFile()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        File.Delete(Path.Combine(f.Work, "context", "treatment", "p2.md"));
        var (code, _, error) = f.Run(null, "prepare");
        Assert.AreEqual(1, code);
        StringAssert.Contains(error, "context of arm treatment: no p2.md in");
    }

    [TestMethod]
    public void AContextFileChangedAfterPrepareStopsTheRun()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        f.Run(null, "prepare");
        File.WriteAllText(Path.Combine(f.Work, "context", "treatment", "p1.md"), "Hala can fly.");
        var fake = new FakeClaude(f.ClaudeHome);
        var (code, _, error) = f.Run(fake, "run");
        Assert.AreEqual(1, code);
        StringAssert.Contains(error, "context/treatment/p1.md changed after prepare locked it");
        Assert.IsEmpty(fake.Calls);
    }

    [TestMethod]
    public void ContextNeedsForkModeAndAKnownArm()
    {
        using var f = new SlipTestFixtures();
        Directory.CreateDirectory(Path.Combine(f.Work, "context", "x"));
        SetContext(f, new JsonObject { ["oracle"] = "context/x" });
        StringAssert.Contains(f.Run(null, "prepare").Error, "context names arm oracle, which arms does not list");

        Directory.CreateDirectory(Path.Combine(f.Work, "overlays", "a"));
        f.WriteTrial(mode: "fresh", arms: new JsonObject { ["control"] = "overlays/a", ["treatment"] = "overlays/a" });
        SetContext(f, new JsonObject { ["treatment"] = "context/x" });
        StringAssert.Contains(f.Run(null, "prepare").Error, "context needs fork mode");
    }

    [TestMethod]
    public void ThePilotPassesWhenTheContextArrived()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        f.Run(null, "prepare");
        f.Run(new FakeClaude(f.ClaudeHome), "run", "--pilot");
        var (code, output, error) = f.Run(null, "pilot");
        Assert.AreEqual(0, code, output + error);
        StringAssert.Contains(output, "hook         4     4           0     0");
        StringAssert.Contains(output, "context      2     2           0     0");
    }

    [TestMethod]
    public void ThePilotFailsWhenTheContextIsMissing()
    {
        using var f = new SlipTestFixtures();
        AddContext(f);
        f.Run(null, "prepare");
        f.Run(new FakeClaude(f.ClaudeHome) { RunsContextHooks = false }, "run", "--pilot");
        var (code, output, _) = f.Run(null, "pilot");
        Assert.AreEqual(1, code);
        StringAssert.Contains(output, "context p1 treatment 1: fail: context text not in the fork's hook output");
        Assert.DoesNotContain("Nytha", output);
    }

    [TestMethod]
    public void APilotWithNoContextShowsNoContextRow()
    {
        using var f = new SlipTestFixtures();
        f.Run(null, "prepare");
        f.Run(new FakeClaude(f.ClaudeHome), "run", "--pilot");
        Assert.DoesNotContain("context ", f.Run(null, "pilot").Output);
    }
}
