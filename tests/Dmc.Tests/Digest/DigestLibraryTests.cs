using System.Text.RegularExpressions;
using Dmc.Digest;
using Dmc.Maps;
using static Dmc.Tests.Digest.DigestFixtures;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Digest;

/// <summary>
/// The <c>Helpers</c> class of <c>tests/test_transcript_digest.py</c>, ported one for one (<c>test_cut</c> is
/// <see cref="Cut"/>, and so on), against the library code. After them come two tests of what the script gets for
/// free from Python: text counted in code points, and one for Claude Code's folder name, which counts UTF-16 units.
/// </summary>
[TestClass]
public sealed class DigestHelperTests
{
    [TestMethod]
    public void Cut()
    {
        Assert.AreEqual("abcdef", DigestText.Cut("abcdef", 6));
        Assert.AreEqual("abc ...[+3 chars]", DigestText.Cut("abcdef", 3));
        Assert.AreEqual("a\nb", DigestText.Cut("a\r\nb", 10));
    }

    [TestMethod]
    public void OneLine()
    {
        Assert.AreEqual("a / b / c", DigestText.OneLine("a\nb\nc", 20));
        Assert.AreEqual("a / b ...[+4 chars]", DigestText.OneLine("a\nb\nc", 5));
    }

    [TestMethod]
    public void BaseName()
    {
        Assert.AreEqual("game-state.md", DigestText.BaseName("D:\\dmc\\adventures\\x\\game-state.md"));
        Assert.AreEqual("canon.md", DigestText.BaseName("/home/x/adventures/canon.md"));
        Assert.AreEqual("", DigestText.BaseName(""));
    }

    [TestMethod]
    public void DescribeCall()
    {
        Assert.AreEqual("b.md", DigestText.DescribeCall("Read", Obj(("file_path", "a/b.md"))));
        Assert.AreEqual("b.md (part)", DigestText.DescribeCall("Read", Obj(("file_path", "a/b.md"), ("limit", 50))));
        Assert.AreEqual("b.pdf (part)", DigestText.DescribeCall("Read", Obj(("file_path", "a/b.pdf"), ("pages", "1-3"))));
        Assert.AreEqual("b.md [5 chars]", DigestText.DescribeCall("Write", Obj(("file_path", "a/b.md"), ("content", "12345"))));
        Assert.AreEqual("b.md [-2 +3]", DigestText.DescribeCall("Edit", Obj(("file_path", "a\\b.md"), ("old_string", "xx"), ("new_string", "yyy"))));
        Assert.AreEqual("{\"command\": \"ls\"}", DigestText.DescribeCall("Bash", Obj(("command", "ls"))));
        Assert.IsLessThan(260, DigestText.DescribeCall("Bash", Obj(("command", new string('x', 5000)))).Length);
    }

    [TestMethod]
    public void ResultText()
    {
        Assert.AreEqual("plain", DigestText.ResultText("plain"));
        Assert.AreEqual("a <image>", DigestText.ResultText(Arr(Obj(("type", "text"), ("text", "a")), Obj(("type", "image")), "stray")));
        Assert.AreEqual("None", DigestText.ResultText(null));
    }

    [TestMethod]
    public void FrictionMarks()
    {
        foreach (string text in new[] { "No, that is wrong", "you said he was dead", "I told you already", "Don't do that",
                     "[Request interrupted by user]", "why did the goblin know that", "rewind please" })
            Assert.IsTrue(Report.Friction().IsMatch(text), text);
        foreach (string text in new[] { "I attack the goblin", "18 and 4", "I open the door", "Wrongful accusations abound" })
            Assert.IsFalse(Report.Friction().IsMatch(text), text);
    }

    [TestMethod]
    public void Table()
    {
        var counter = new Counter();
        counter.Add("Read", 3);
        counter.Add("Edit", 1);
        counter.Add("", 2);
        Assert.AreEqual("Read x3, ? x2, Edit x1", DigestText.Table(counter));

        var chars = new Counter();
        chars.Add("Read", 90);
        chars.Add("Edit", 5);
        chars.Add("", 0);
        Assert.AreEqual("Read x3 (90 chars)", DigestText.Table(counter, chars, top: 1));
        Assert.AreEqual("none", DigestText.Table(new Counter()));
    }

    [TestMethod]
    public void LogDirFollowsTheClaudeConfigDir()
    {
        string root = Path.Combine(Path.GetTempPath(), "my_toolkit.v2");
        string folder = LogFolder.Of(root, name => name == "CLAUDE_CONFIG_DIR" ? Path.Combine("some", "config") : null);
        Assert.AreEqual(Path.Combine("some", "config", "projects"), Path.GetDirectoryName(folder));
        string project = Path.GetFileName(folder);
        Assert.IsTrue(Regex.IsMatch(project, "^[A-Za-z0-9-]+$"), project);
        Assert.IsTrue(project.EndsWith("my-toolkit-v2", StringComparison.Ordinal), project);

        // Unset or empty: the "~/.claude" of the account.
        string home = Path.Combine(Path.GetTempPath(), "somebody");
        string? Env(string name) => name is "HOME" or "USERPROFILE" ? home : name == "CLAUDE_CONFIG_DIR" ? "" : null;
        Assert.AreEqual(Path.Combine(home, ".claude", "projects", project), LogFolder.Of(root, Env));
    }

    [TestMethod]
    public void TheFolderNameCountsUtf16Units()
    {
        // Claude Code names the folder with JavaScript's replace(/[^a-zA-Z0-9]/g, "-"), which works on UTF-16 units: a letter outside the BMP is two "-" (the script's re.sub, on code points, would write one, and miss the folder).
        Assert.AreEqual("D--Thr-r-s-----", LogFolder.ProjectName("D:\\Thrór's \U0001D54F\u5317\\"));
    }

    [TestMethod]
    public void TextIsCutAndCountedInCodePoints()
    {
        string clefs = string.Concat(Enumerable.Repeat("\U0001D11E", 5)); // 5 characters, 10 UTF-16 units
        Assert.AreEqual("\U0001D11E\U0001D11E ...[+3 chars]", DigestText.Cut(clefs, 2));
        Assert.AreEqual(clefs, DigestText.Cut(clefs, 5));
        Assert.AreEqual("\U0001D11E\U0001D11E", DigestText.Slice(clefs, 1, 3));
        Assert.AreEqual("  ", DigestText.PadLeft("", 2));
        Assert.AreEqual("\U0001D11E ", DigestText.PadRight("\U0001D11E", 2));
        Assert.AreEqual(-2, DigestText.FloorDiv(-3, 2), "Python's // rounds towards minus infinity");
    }
}

/// <summary>
/// The <c>Parsing</c> class of <c>tests/test_transcript_digest.py</c>, ported one for one: a small made-up log with two
/// player messages, a compaction between them and one failed edit.
/// </summary>
[TestClass]
public sealed class DigestParsingTests
{
    /// <summary>The temporary folder holding the log, made once for the whole class.</summary>
    static string _folder = "";

    /// <summary>The session read from the made-up log <see cref="DigestFixtures.Records"/>.</summary>
    static Session _s = null!;

    [ClassInitialize]
    public static void MakeLog(TestContext context)
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _s = new Session(WriteLog(_folder, Records()));
    }

    [ClassCleanup]
    public static void RemoveLog() => Directory.Delete(_folder, recursive: true);

    /// <summary>Asserts that a counter holds exactly these counts, whatever the order.</summary>
    static void AssertCounts(Dictionary<string, long> expected, Counter actual) =>
        CollectionAssert.AreEquivalent(expected, actual.ToDictionary());

    [TestMethod]
    public void IdentityAndTimes()
    {
        Assert.AreEqual(SessionId, _s.Id);
        Assert.AreEqual((Stamp(0), Stamp(7)), (_s.Start, _s.End));
        Assert.AreEqual("silver-egg", _s.Campaign());
        Assert.IsGreaterThan(0, _s.Size);
    }

    [TestMethod]
    public void OnlyWhatThePlayerTypedIsATurn()
    {
        CollectionAssert.AreEqual(new[] { "Resume adventures/silver-egg/ please", "No, that is wrong.\n[Request interrupted by user]" },
            _s.Turns.Select(t => t.Text).ToArray());
        CollectionAssert.AreEqual(new[] { "10:00:00", "10:04:00" }, _s.Turns.Select(t => t.Time).ToArray());
        Assert.AreEqual(1, _s.Interrupts);
        Assert.AreEqual(1, _s.Compactions);
    }

    [TestMethod]
    public void RepliesAreCreditedToTheMessageTheyAnswer()
    {
        var (first, second) = (_s.Turns[0], _s.Turns[1]);
        Assert.AreEqual(Narration.Length, first.ReplyChars);
        CollectionAssert.AreEqual(new[] { "Read:game-state.md", "Edit:canon.md" }, first.Tools);
        Assert.AreEqual("Sorry.".Length, second.ReplyChars);
        CollectionAssert.AreEqual(new[] { "Skill", "Write:live.json", "Bash" }, second.Tools);
    }

    [TestMethod]
    public void ContextSizeCountsEachApiCallOnce()
    {
        CollectionAssert.AreEqual(new double[] { 1000, 3000, 3500 }, _s.Ctx);
        CollectionAssert.AreEqual(new double[] { 0, 1000 }, _s.Turns.Select(t => t.Ctx).ToArray());
        Assert.AreEqual(3, _s.Calls.Count);
        AssertCounts(new() { ["claude-test"] = 3 }, _s.Models);
    }

    [TestMethod]
    public void TokensTakeTheLastUsageOfEachCall()
    {
        Assert.AreEqual(new Usage(Input: 310, Output: 105, CacheRead: 6800, CacheCreation: 390), _s.Tokens());
    }

    [TestMethod]
    public void ToolCounts()
    {
        AssertCounts(new() { ["Read"] = 1, ["Edit"] = 1, ["Skill"] = 1, ["Write"] = 1, ["Bash"] = 1 }, _s.Tools);
        AssertCounts(new() { ["draw-map"] = 1 }, _s.Skills);
        AssertCounts(new() { ["game-state.md"] = 1 }, _s.Reads);
        AssertCounts(new() { ["game-state.md"] = "\"HP 10/12\"".Length }, _s.ReadChars);
        AssertCounts(new() { ["canon.md"] = 1, ["live.json"] = 1 }, _s.Writes);
        AssertCounts(new() { ["canon.md"] = 7, ["live.json"] = 2 }, _s.WriteChars);
        CollectionAssert.AreEquivalent(new[] { "Read", "Edit", "Bash" }, _s.ResultChars.Items.Select(p => p.Key.Display).ToArray());
    }

    [TestMethod]
    public void ToolErrors()
    {
        CollectionAssert.AreEqual(new[] { "10:02:00 Edit canon.md: String to replace / not found" }, _s.Errors);
    }

    [TestMethod]
    public void TranscriptLines()
    {
        string text = string.Join("\n", _s.Lines);
        foreach (string expected in new[]
                 {
                     "### PLAYER 10:00:00\nResume adventures/silver-egg/ please",
                     "### (injected) 10:00:00 <system-reminder>be good",
                     "### DM 10:01:00 [ctx 1000]\n" + Narration,
                     "  -> Read game-state.md",
                     "  -> Edit canon.md [-2 +5]",
                     "  <- 10 chars: HP 10/12",
                     " ERROR: String to replace / not found",
                     "--- CONTEXT COMPACTED 10:03:00",
                     "### COMPACTION SUMMARY 10:03:00\nThe story so far: goblins.",
                     "### (injected) 10:03:00 Caveat: local command output",
                     "  -> Write live.json [2 chars]",
                 })
            StringAssert.Contains(text, expected);
        int[] order = new[] { "### PLAYER 10:00:00", "### DM 10:01:00", "CONTEXT COMPACTED", "### PLAYER 10:04:00", "### DM 10:07:00" }
            .Select(part => text.IndexOf(part, StringComparison.Ordinal)).ToArray();
        CollectionAssert.AreEqual(order.Order().ToArray(), order);
    }

    [TestMethod]
    public void LongTextIsCutInTheTranscriptButCountedInFull()
    {
        using var folder = new TempFolder();
        var s = new Session(WriteLog(folder.Path, [User(0, "go"), Assistant(1, "m1", Blocks(Text(new string('x', 9000))), 1, 2, 3, 4)]));
        Assert.AreEqual(9000, s.Turns[0].ReplyChars);
        StringAssert.Contains(string.Join("\n", s.Lines), "...[+3000 chars]");
    }

    [TestMethod]
    public void AnEmptyLog()
    {
        using var folder = new TempFolder();
        var s = new Session(WriteLog(folder.Path, []));
        Assert.AreEqual((0, 0, "", "-"), (s.Turns.Count, s.Ctx.Count, s.Start, s.Campaign()));
        StringAssert.Contains(Report.Stats(s), "## Session abcd1234");
        Assert.AreEqual("\n", Report.TurnIndex(s));
    }

    [TestMethod]
    public void Stats()
    {
        string text = Report.Stats(_s);
        foreach (string expected in new[]
                 {
                     "## Session abcd1234",
                     "2026-01-02 10:00 to 2026-01-02 10:07 UTC. Campaign: silver-egg.",
                     "**Models**: claude-test x3",
                     "**Player messages**: 2 (1 marked \"!\", 1 interrupted). **API calls**: 3.",
                     "starts at 1000 tokens, ends at 3500, peak 3500. About 1250 tokens added per player message. Compactions: 1.",
                     "**Tokens**: output 105; input read from cache 6800, written to cache 390, uncached 310.",
                     "**Replies to the player**: average 17 chars, longest 29; 0 of 2 over 1,500 chars.",
                     "**Skills run**: draw-map x1",
                     "canon.md x1 (7 chars)",
                     "game-state.md x1 (10 chars)",
                     "+1000 at 10:00:00: \"Resume adventures/silver-egg/ please\" -> Read:game-state.md Edit:canon.md",
                     "**Tool errors** (1):",
                 })
            StringAssert.Contains(text, expected);
    }

    [TestMethod]
    public void TurnIndex()
    {
        string[] lines = Report.TurnIndex(_s).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.AreEqual(new[]
        {
            "   1 10:00:00 ctx=0k | Resume adventures/silver-egg/ please",
            $"       reply {Narration.Length} chars; tools: Read:game-state.md Edit:canon.md",
            "!  2 10:04:00 ctx=1k | No, that is wrong. / [Request interrupted by user]",
            "       reply 6 chars; tools: Skill Write:live.json Bash",
        }, lines);
    }
}

/// <summary>The <c>CallsPerMessage</c> class of <c>tests/test_transcript_digest.py</c>, ported one for one.</summary>
[TestClass]
public sealed class DigestCallsPerMessageTests
{
    /// <summary>The temporary folder holding the log, made once for the whole class.</summary>
    static string _folder = "";

    /// <summary>The session read from the made-up log <see cref="DigestFixtures.CallRecords"/>.</summary>
    static Session _s = null!;

    [ClassInitialize]
    public static void MakeLog(TestContext context)
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _s = new Session(WriteLog(_folder, CallRecords()));
    }

    [ClassCleanup]
    public static void RemoveLog() => Directory.Delete(_folder, recursive: true);

    /// <summary>The third-and-later calls of a turn as <c>class:context</c> pairs joined with ";".</summary>
    static string Extra(Turn turn) => string.Join(";", Report.ExtraCalls(turn).Select(c => $"{c.Class}:{c.Ctx}"));

    [TestMethod]
    public void CallKinds()
    {
        Assert.AreEqual("roll", DigestText.CallKind("Bash", Obj(("command", RollLine))));
        Assert.AreEqual("roll", DigestText.CallKind("PowerShell", Obj(("command", ".\\scripts\\roll.ps1 1d6"))));
        Assert.AreEqual("other", DigestText.CallKind("Bash", Obj(("command", "python scripts/map_render.py x"))));
        // dmc roll, in the forms DMC writes it in each shell; the dice scripts above are the logs of older sessions.
        foreach (string command in new[] { "./bin/dmc roll 1d20+5", "bin/dmc.exe roll 2d20kh1", ".\\bin\\dmc roll 1d6", ".\\bin\\dmc.exe roll 1d6",
                     "& \"D:\\dmc\\bin\\dmc.exe\" roll 1d8", "cd x && ./bin/dmc  roll 4d6kh3", "dmc roll d6" })
            Assert.AreEqual("roll", DigestText.CallKind("PowerShell", Obj(("command", command))), command);
        foreach (string command in new[] { "./bin/dmc map live.json", "./bin/dmc --version", "./bin/admc roll 1d6", "./bin/dmc rolls", "echo roll",
                     "grep -n \"dmc roll\" CLAUDE.md", "echo \"use dmc roll\"", "git commit -m \"Class dmc roll\"" })
            Assert.AreEqual("other", DigestText.CallKind("Bash", Obj(("command", command))), command);
        Assert.AreEqual("write", DigestText.CallKind("Edit", Obj()));
        Assert.AreEqual("write", DigestText.CallKind("Write", Obj()));
        foreach (string name in new[] { "Read", "Grep", "Glob" })
            Assert.AreEqual("lookup", DigestText.CallKind(name, Obj()));
        Assert.AreEqual("other", DigestText.CallKind("Skill", Obj(("skill", "draw-map"))));
    }

    [TestMethod]
    public void CallsAreGroupedByPlayerMessage()
    {
        CollectionAssert.AreEqual(new[] { 3, 3, 3, 3, 4, 3, 3, 2 }, _s.Turns.Select(t => t.Calls.Count).ToArray());
        Assert.AreEqual(100, _s.Turns[0].Calls[0].Ctx);
        CollectionAssert.AreEquivalent(new[] { "roll" }, _s.Turns[0].Calls[0].Kinds.ToArray());
        CollectionAssert.AreEquivalent(new[] { "lookup" }, _s.Turns[4].Calls[1].Kinds.ToArray());
    }

    [TestMethod]
    public void SkillsComeFromTheSkillToolOrASlashCommand()
    {
        Assert.IsTrue(_s.Turns[4].Skills.Contains("campaign-full-save"));
        CollectionAssert.AreEquivalent(new[] { "resume-campaign" }, _s.Turns[5].Skills.Select(k => k.Display).ToArray());
        Assert.IsEmpty(_s.Turns[0].Skills);
    }

    [TestMethod]
    public void ThirdAndLaterCallsAreClassed()
    {
        CollectionAssert.AreEqual(new[]
        {
            "dm-roll:120", "write:220", "roll:320", "lookup:420", "save:520;save:530", "resume:620", "other:720", "",
        }, _s.Turns.Select(Extra).ToArray());
    }

    [TestMethod]
    public void FilesReadAgainAfterWriting()
    {
        CollectionAssert.AreEqual(new[] { "game-state.md" }, Report.RereadAfterWrite(_s.Turns[4]));
        Assert.IsEmpty(Report.RereadAfterWrite(_s.Turns[5]));
    }

    [TestMethod]
    public void StateFileCharactersPerMessage()
    {
        CollectionAssert.AreEqual(new long[] { 9, 11, 0, 0, 10, 0, 0, 8 }, _s.Turns.Select(t => t.StateChars).ToArray());
    }

    [TestMethod]
    public void Median()
    {
        Assert.AreEqual(2.0, DigestText.Median([3, 1, 2]));
        Assert.AreEqual(2.5, DigestText.Median([4, 1, 2, 3]));
    }

    [TestMethod]
    public void Stats()
    {
        string text = Report.Stats(_s);
        long total = CallTotal();
        long extra = 120 + 220 + 320 + 420 + 520 + 530 + 620 + 720;
        foreach (string expected in new[]
                 {
                     $"**Context read by third and later calls of a message**: {DigestText.Percent(extra, total)} of all context read ({extra} of {total}).",
                     $"  - Full save: {DigestText.Percent(1050, total)}",
                     $"  - A turn with a DM roll: roll, then write, then narrate: {DigestText.Percent(120, total)}",
                     $"  - A second write call in one reply: {DigestText.Percent(220, total)}",
                     $"  - A Read, Grep or Glob as its own call: {DigestText.Percent(420, total)}",
                     $"  - A second roll call in one reply: {DigestText.Percent(320, total)}",
                     $"  - Resume: {DigestText.Percent(620, total)}",
                     $"  - Anything else: {DigestText.Percent(720, total)}",
                     // 9, 11, 10 and 8 chars: mean 9 (rounded down), median 9.5 (rounded up).
                     "**game-state.md**: read 2 times. Edited in 4 player messages, 38 chars sent; per editing message mean 9, median 10.",
                     "**Failed edits**: 1.",
                     "**Full save at player message 5**: 4 calls; files read again after writing: game-state.md.",
                 })
            StringAssert.Contains(text, expected);
    }

    /// <summary>
    /// The player skills are called <c>dmc-campaign-full-save</c> and <c>dmc-resume-campaign</c> since they moved to the DMC
    /// folder, and the logs of sessions played before keep the old names: both are a full save and a resume, so the same log
    /// with either spelling gives the same statistics.
    /// </summary>
    [TestMethod]
    public void TheRenamedSaveAndResumeSkillsAreClassedLikeTheOldOnes()
    {
        var renamed = CallRecords().Select(r => (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(
            r.ToJsonString().Replace("campaign-full-save", "dmc-campaign-full-save", StringComparison.Ordinal)
                .Replace("\"resume-campaign\"", "\"dmc-resume-campaign\"", StringComparison.Ordinal))!).ToList();
        var session = new Session(WriteLog(_folder, renamed, "dddd6666-0000"));
        Assert.IsTrue(session.Turns[4].Skills.Contains("dmc-campaign-full-save"));
        Assert.IsFalse(session.Turns[4].Skills.Contains("campaign-full-save"));
        CollectionAssert.AreEquivalent(new[] { "dmc-resume-campaign" }, session.Turns[5].Skills.Select(k => k.Display).ToArray());
        // The lines that count the full save and the resume are the same; only the skill names differ.
        static string[] Classified(string stats) => stats.Split('\n').Where(l => l.StartsWith("  - Full save", StringComparison.Ordinal) || l.StartsWith("  - Resume", StringComparison.Ordinal) || l.Contains("Full save at", StringComparison.Ordinal)).ToArray();
        CollectionAssert.AreEqual(Classified(Report.Stats(_s)), Classified(Report.Stats(session)));
        StringAssert.Contains(Report.Stats(session), "Full save at player message 5");
        foreach (string name in new[] { "campaign-full-save", "dmc-campaign-full-save" })
            Assert.IsTrue(Session.UsedAny([name], Session.SaveSkills), name);
        foreach (string name in new[] { "resume-campaign", "dmc-resume-campaign" })
            Assert.IsTrue(Session.UsedAny([name], Session.ResumeSkills), name);
        Assert.IsFalse(Session.UsedAny(["draw-map", "dmc-draw-map"], Session.SaveSkills));
    }

    [TestMethod]
    public void StatsWithoutExtraCallsOrEdits()
    {
        string path = WriteLog(_folder, [User(0, "hi"), Call(0, "z1", 50)], "dddd5555-0000");
        string text = Report.Stats(new Session(path));
        StringAssert.Contains(text, "**Context read by third and later calls of a message**: 0.0% of all context read (0 of 50).");
        StringAssert.Contains(text, "**game-state.md**: read 0 times. Edited in 0 player messages.");
        StringAssert.Contains(text, "**Failed edits**: 0.");
        Assert.DoesNotContain("Full save at", text);
        Assert.DoesNotContain("  - Full save", text);
    }
}

/// <summary>A folder made for one test and removed with it.</summary>
public sealed class TempFolder : IDisposable
{
    /// <summary>The folder's full path.</summary>
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));

    /// <summary>Creates the folder.</summary>
    public TempFolder() => Directory.CreateDirectory(Path);

    /// <summary>Deletes the folder and everything in it.</summary>
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
