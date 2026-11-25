using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>Reading a play log and writing the base log a fork-mode replay continues (spec 024, FR-003).</summary>
[TestClass]
public sealed class LogRewriterTests
{
    [TestMethod]
    public void OnlyTextThePlayerTypedIsATypedMessage()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        Assert.IsTrue(LogFile.IsTyped(log.Records[log.IndexOf(SlipTestFixtures.ForkMessage)]));
        Assert.IsFalse(LogFile.IsTyped(log.Records[log.IndexOf("u-tool")]));
        var meta = SlipTestFixtures.Typed("m", "x", SlipTestFixtures.T1, f.Folder, "s");
        meta["isMeta"] = true;
        Assert.IsFalse(LogFile.IsTyped(meta));
        Assert.IsFalse(LogFile.IsTyped(SlipTestFixtures.Typed("c", "<command-name>/dmc-resume-campaign</command-name>", SlipTestFixtures.T1, f.Folder, "s")));
        Assert.IsFalse(LogFile.IsTyped(SlipTestFixtures.Typed("i", "[Request interrupted by user]", SlipTestFixtures.T1, f.Folder, "s")));
        var side = SlipTestFixtures.Typed("sc", "Find the map.", SlipTestFixtures.T1, f.Folder, "s");
        side["isSidechain"] = true;
        Assert.IsFalse(LogFile.IsTyped(side));
    }

    [TestMethod]
    public void TheModelEffortAndPermissionComeFromBeforeTheForkMessage()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        int at = log.IndexOf(SlipTestFixtures.ForkMessage);
        Assert.AreEqual("claude-test-1", log.ModelBefore(at));
        Assert.AreEqual("high", log.EffortBefore(at));
        Assert.AreEqual("auto", log.PermissionAt(at));
        Assert.AreEqual("claude-test-1", log.ModelBefore(log.IndexOf(SlipTestFixtures.FirstMessage)), "a first message takes the model that answered it");
    }

    [TestMethod]
    public void TheBaseLogEndsBeforeTheForkMessageUnderANewIdWithTheCopysPathAndTheArmsRules()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        int at = log.IndexOf(SlipTestFixtures.ForkMessage);
        var result = LogRewriter.Rewrite(log, at, "new-id", f.Folder, f.Copy, "# Arm rules\n");

        Assert.AreEqual(1, result.RulesReplaced);
        var records = result.Lines.Select(l => JsonNode.Parse(l)!.AsObject()).ToList();
        Assert.HasCount(at, records);
        Assert.IsFalse(records.Any(r => LogFile.Text(r, "uuid") == SlipTestFixtures.ForkMessage));
        Assert.IsTrue(records.Where(r => r.ContainsKey("sessionId")).All(r => LogFile.Text(r, "sessionId") == "new-id"));
        string all = string.Join("\n", result.Lines);
        Assert.DoesNotContain(JsonValue.Create(f.Folder + Path.DirectorySeparatorChar)!.ToJsonString().Trim('"'), all);
        Assert.Contains(JsonValue.Create(Path.Combine(f.Copy, "adventures"))!.ToJsonString().Trim('"'), all);
        var instructions = records[0]["attachment"]!["files"]![0]!;
        Assert.AreEqual("# Arm rules", LogFile.Text(instructions.AsObject(), "content"));
        Assert.AreEqual(Path.Combine(f.Copy, "CLAUDE.md"), LogFile.Text(instructions.AsObject(), "path"));
    }

    [TestMethod]
    public void ABranchOffTheForkMessagesChainIsLeftOut()
    {
        using var f = new SlipTestFixtures();
        var records = SlipTestFixtures.Linked(f.PlayLog());
        int at = records.FindIndex(r => LogFile.Text(r, "uuid") == SlipTestFixtures.ForkMessage);
        var abandoned = SlipTestFixtures.Typed("u-abandoned", "A message the player rewound.", SlipTestFixtures.T2, f.Folder, SlipTestFixtures.Session);
        abandoned["parentUuid"] = SlipTestFixtures.FirstMessage;
        records.Insert(at, abandoned);
        var side = SlipTestFixtures.Assistant(SlipTestFixtures.Session, SlipTestFixtures.TextBlock("a subagent"));
        side["isSidechain"] = true;
        side["parentUuid"] = "u-abandoned";
        records.Insert(at, side);
        var log = new LogFile(f.LogPath, records);
        var lines = LogRewriter.Rewrite(log, log.IndexOf(SlipTestFixtures.ForkMessage), "id", f.Folder, f.Copy, "x").Lines;
        Assert.IsFalse(lines.Any(l => l.Contains("u-abandoned", StringComparison.Ordinal) || l.Contains("a subagent", StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(l => l.Contains("The stairs creak.", StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(l => l.Contains("file-history-snapshot", StringComparison.Ordinal)), "records with no uuid are kept");
    }

    [TestMethod]
    public void TheRecordOfTheLastPromptIsLeftOut()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        var result = LogRewriter.Rewrite(log, log.Records.Count, "id", f.Folder, f.Copy, null);
        Assert.IsFalse(result.Lines.Any(l => l.Contains("\"last-prompt\"", StringComparison.Ordinal)));
        Assert.AreEqual(0, result.RulesReplaced);
    }

    [TestMethod]
    public void ALogWithNoRecordedRulesReplacesNone()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        var without = new LogFile(f.LogPath, log.Records.Skip(1).ToList());
        Assert.AreEqual(0, LogRewriter.Rewrite(without, 3, "id", f.Folder, f.Copy, "x").RulesReplaced);
    }

    [TestMethod]
    public void AReplyIsItsTextBlocksOnly()
    {
        using var f = new SlipTestFixtures();
        var log = LogFile.Read(f.LogPath);
        var turn = log.TurnAfter(log.IndexOf(SlipTestFixtures.FirstMessage));
        Assert.AreEqual("The stairs creak.", LogFile.Reply(turn));
        Assert.IsFalse(turn.Any(LogFile.IsTyped));
    }

    [TestMethod]
    public void TheSlugIsThePathWithEveryOtherCharacterADash()
    {
        if (OperatingSystem.IsWindows())
            Assert.AreEqual("D--Claude-DMC", Folders.Slug(@"D:\Claude\DMC"));
        else
            Assert.AreEqual("-tmp-claude-0--home-user-x-probe", Folders.Slug("/tmp/claude-0/-home-user/x/probe"));
    }
}
