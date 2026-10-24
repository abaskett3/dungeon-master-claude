using System.Text;
using System.Text.Json.Nodes;
using Dmc.ContextCheck;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>The 11 context-check tests against the library code, in this process.</summary>
[TestClass]
public sealed class ContextCheckInProcessTests : ContextCheckContract
{
    /// <summary>Runs the command in this process, with <paramref name="stdin"/> as the hook's input.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The 11 context-check tests against the published <c>dmc</c> binary, in a subprocess, as the hook runs it.</summary>
[TestClass]
public sealed class ContextCheckBlackBoxTests : ContextCheckContract
{
    /// <summary>Runs the published binary, with <paramref name="stdin"/> as the hook's input.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}

/// <summary>Cases the script's tests do not cover: where the thresholds fall, and odd shapes in a real log.</summary>
[TestClass]
public sealed class ContextCheckEdgeTests
{
    /// <summary>A fresh temporary folder for each test, holding the session log it writes.</summary>
    string _folder = "";

    /// <summary>Creates this test's temporary folder.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    /// <summary>Deletes this test's temporary folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>Write exactly these bytes as the session log and return its path.</summary>
    string WriteBytes(byte[] bytes)
    {
        string path = Path.Combine(_folder, "session.jsonl");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>Write these lines as the session log, each ended by a newline, and return its path.</summary>
    string Write(params string[] lines) => WriteBytes(Encoding.UTF8.GetBytes(string.Join("\n", lines) + "\n"));

    /// <summary>An assistant record's JSON line for a context of this many tokens.</summary>
    static string Assistant(int context) => ContextCheckContract.Assistant(context).ToJsonString();

    /// <summary>A user record's JSON line.</summary>
    static string User(string text = "go on") => ContextCheckContract.User(text).ToJsonString();

    [TestMethod]
    public void TheReminderStartsExactlyAtTheThreshold()
    {
        Assert.AreEqual("", BreakReminder.For(149999));
        Assert.AreEqual("Context 150k is large, break due since 150k: at the next break point, offer a full save and a fresh "
                        + "session, unless the player has declined since 150k.", BreakReminder.For(150000));
    }

    [TestMethod]
    public void TheBandChangesExactlyEvery40k()
    {
        StringAssert.Contains(BreakReminder.For(189999), "break due since 150k");
        StringAssert.Contains(BreakReminder.For(190000), "break due since 190k");
        StringAssert.Contains(BreakReminder.For(230000), "break due since 230k");
        StringAssert.Contains(BreakReminder.For(1000000), "Context 1000k is large, break due since 990k");
    }

    // The first band is the larger of 150k and the baseline plus 40k; the bands then rise every 40k from there, so
    // a declined offer is repeated at the next one, as before.
    [TestMethod]
    public void TheFirstBandIsRelativeToTheBaselineWithA150kFloor()
    {
        Assert.AreEqual(150000, BreakReminder.FirstBand(0));
        Assert.AreEqual(150000, BreakReminder.FirstBand(31000), "a small baseline keeps the floor");
        Assert.AreEqual(150000, BreakReminder.FirstBand(110000), "baseline + 40k is exactly the floor");
        Assert.AreEqual(182000, BreakReminder.FirstBand(142000), "the resume of issue 35, 142k");

        Assert.AreEqual("", BreakReminder.For(181999, baseline: 142000));
        StringAssert.Contains(BreakReminder.For(182000, baseline: 142000), "break due since 182k");
        StringAssert.Contains(BreakReminder.For(221999, baseline: 142000), "break due since 182k");
        StringAssert.Contains(BreakReminder.For(222000, baseline: 142000), "break due since 222k");
        StringAssert.Contains(BreakReminder.For(230000, baseline: 142000), "Context 230k is large");
    }

    /// <summary>The size and baseline of the log at this path: what the hook works from.</summary>
    static ContextReading Reading(string path) => TranscriptReader.Read(path);

    /// <summary>The latest context size of the log at this path (the first part of <see cref="Reading"/>).</summary>
    static long LastContext(string path) => TranscriptReader.Read(path).Size;

    /// <summary>A user record that echoes a slash command, as Claude Code writes it.</summary>
    static string Command(string name) => ContextCheckContract.User(
        $"<command-message>{name}</command-message>\n<command-name>/{name}</command-name>").ToJsonString();

    /// <summary>A user record that holds only a tool result, which is not a player message.</summary>
    const string ToolResult =
        "{\"type\": \"user\", \"message\": {\"role\": \"user\", \"content\": [{\"type\": \"tool_result\", \"tool_use_id\": \"t\", \"content\": \"file text\"}]}}";

    /// <summary>A meta record, such as the skill text a slash command loads.</summary>
    const string Meta =
        "{\"type\": \"user\", \"isMeta\": true, \"message\": {\"role\": \"user\", \"content\": \"Base directory for this skill\"}}";

    /// <summary>The compaction boundary record and the summary record that follows it.</summary>
    const string Boundary = "{\"type\": \"system\", \"subtype\": \"compact_boundary\"}";

    /// <summary>The user record that holds a compaction summary, which is not a player message.</summary>
    const string CompactSummary =
        "{\"type\": \"user\", \"isCompactSummary\": true, \"message\": {\"role\": \"user\", \"content\": \"This session is being continued\"}}";

    // No resume: the first player message has no call before it, so the baseline is the reply to it, and a session
    // that never loads much keeps the 150k floor.
    [TestMethod]
    public void WithoutAResumeTheBaselineIsTheFirstReply() =>
        Assert.AreEqual(new ContextReading(160000, 31000),
            Reading(Write(User(), Assistant(31000), ToolResult, Assistant(120000), User(), Assistant(160000))));

    // One resume: the baseline is the end of its recap, the last call before the first player message. Sub-agent
    // calls, slash-command echoes, meta records and tool results in between do not count.
    [TestMethod]
    public void AfterOneResumeTheBaselineIsTheEndOfTheRecap() =>
        Assert.AreEqual(new ContextReading(170000, 142000), Reading(Write(
            Command("resume-campaign"), Meta, Assistant(31000), ToolResult, Assistant(120000),
            ContextCheckContract.Assistant(10000, sidechain: true).ToJsonString(), Assistant(142000),
            User(), Assistant(150000), User(), Assistant(170000))));

    // The skill is /dmc-resume-campaign in a DMC folder and was /resume-campaign before: the baseline needs no marker for the
    // resume, so both give the same answer (and a log of either kind keeps working).
    [TestMethod]
    public void TheRenamedResumeSkillGivesTheSameBaselineAsTheOldOne()
    {
        string[] rest = [Meta, Assistant(31000), ToolResult, Assistant(120000), Assistant(142000), User(), Assistant(150000), User(), Assistant(170000)];
        var expected = new ContextReading(170000, 142000);
        Assert.AreEqual(expected, Reading(Write([Command("dmc-resume-campaign"), .. rest])));
        Assert.AreEqual(expected, Reading(Write([Command("resume-campaign"), .. rest])));
    }

    // Two resumes: a resume after the first player message, which the player runs mid-session, does not move the
    // baseline, so it cannot hide that the context has grown.
    [TestMethod]
    public void AResumeMidSessionDoesNotMoveTheBaseline() =>
        Assert.AreEqual(new ContextReading(90000, 100000), Reading(Write(
            Command("resume-campaign"), Assistant(100000), User(), Assistant(130000), User(),
            Command("resume-campaign"), Assistant(60000), User(), Assistant(90000))));

    // A recap nobody has answered yet: nothing has been added since loading, so the baseline is the latest context.
    [TestMethod]
    public void ARecapNobodyHasAnsweredYetIsItsOwnBaseline() =>
        Assert.AreEqual(new ContextReading(142000, 142000),
            Reading(Write(Assistant(20000), Command("resume-campaign"), Assistant(142000))));

    // A compaction restarts the search: only the part after the latest boundary counts, whether the reload comes
    // before the player's next message or the first reply does.
    [TestMethod]
    public void OnlyThePartAfterTheLatestCompactionCounts()
    {
        Assert.AreEqual(new ContextReading(130000, 120000), Reading(Write(
            Command("resume-campaign"), Assistant(100000), User(), Assistant(250000), Boundary, CompactSummary,
            Assistant(30000), ToolResult, Assistant(120000), User(), Assistant(130000))), "the reload before the next message");
        Assert.AreEqual(new ContextReading(100000, 40000), Reading(Write(
            Command("resume-campaign"), Assistant(100000), User(), Assistant(250000), Boundary, CompactSummary,
            User(), Assistant(40000), User(), Assistant(100000))), "the first reply when the message comes first");
        Assert.AreEqual(new ContextReading(70000, 60000), Reading(Write(
            User(), Assistant(100000), Boundary, User(), Assistant(50000), Boundary, CompactSummary,
            Assistant(60000), User(), Assistant(70000))), "the latest of two boundaries");
        Assert.AreEqual(new ContextReading(0, 0), Reading(Write(Assistant(250000), Boundary)), "a boundary as the latest thing");
    }

    // Records that are not what the player typed must not end the search for the first player message.
    [TestMethod]
    public void InjectedRecordsAreNotPlayerMessages()
    {
        string[] injected =
        [
            User("[Request interrupted by user]"), User("<bash-input>ls</bash-input>"), User("<bash-stdout>a b</bash-stdout>"),
            User("<system-reminder>a hook said so</system-reminder>"), User("<local-command-stdout>ok</local-command-stdout>"),
            User("<task-notification>done</task-notification>"), Meta, CompactSummary, ToolResult,
        ];
        Assert.AreEqual(new ContextReading(150000, 120000), Reading(Write(
            [Assistant(31000), .. injected, Assistant(120000), User("I attack"), Assistant(150000)])));
    }

    // Every text block counts, not only the first: a message whose first block is an injection and whose second is
    // typed text is a player message.
    [TestMethod]
    public void AnyTextBlockOfAMessageCanBeThePlayersText()
    {
        string blocks = new JsonObject
        {
            ["type"] = "user",
            ["message"] = new JsonObject
            {
                ["role"] = "user",
                ["content"] = new JsonArray(
                    new JsonObject { ["type"] = "text", ["text"] = "<system-reminder>x</system-reminder>" },
                    new JsonObject { ["type"] = "text", ["text"] = "I attack" }),
            },
        }.ToJsonString();
        Assert.AreEqual(new ContextReading(120000, 31000), Reading(Write(Assistant(31000), blocks, Assistant(120000))));
    }

    // A bad record before the baseline is settled is skipped, never an error: a shape it should not have, or a
    // token count too large to read (which makes GetDouble throw a FormatException).
    [TestMethod]
    public void ABadOlderRecordIsSkippedWhenFindingTheBaseline()
    {
        string[] bad =
        [
            "{\"type\": \"assistant\", \"message\": \"text\"}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": \"7\"}}}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": 1e999}}}",
            "{\"type\": \"user\", \"message\": 5}",
        ];
        foreach (string record in bad)
            Assert.AreEqual(new ContextReading(160000, 31000),
                Reading(Write(record, Assistant(31000), User(), Assistant(120000), record, User(), Assistant(160000))), record);
    }

    // The scan from the start reads across chunk boundaries: the first player message is a line longer than two
    // chunks, and a boundary comes after more than two chunks of tool results.
    [TestMethod]
    public void TheBaselineIsFoundInALogLongerThanSeveralChunks()
    {
        string huge = User(new string('x', 2 * TranscriptReader.ChunkSize + 7));
        string filler = ToolResult.Replace("file text", new string('y', TranscriptReader.ChunkSize / 2)); // each line crosses a chunk boundary
        Assert.AreEqual(new ContextReading(150000, 31000), Reading(Write(
            Assistant(31000), huge, Assistant(120000), filler, filler, filler, filler, filler, filler, User(), Assistant(150000))));
        Assert.AreEqual(new ContextReading(150000, 60000), Reading(Write(
            Assistant(31000), huge, Assistant(120000), filler, filler, filler, filler, filler, filler, Boundary, CompactSummary,
            Assistant(60000), User(), Assistant(150000))));
    }

    // The reminder end to end: a resume that settles above 150k moves the first band up with it.
    [TestMethod]
    public void ABaselineAbove150kMovesTheFirstBandUp()
    {
        string Run(params string[] lines) => DmcRunner.InProcess(["context-check"],
            new JsonObject { ["transcript_path"] = Write(lines) }.ToJsonString()).Stdout.Trim();

        string[] loaded = [Command("resume-campaign"), Assistant(165000), User()];
        Assert.AreEqual("", Run([.. loaded, Assistant(204999)]), "165k + 40k = 205k is where the first band starts");
        StringAssert.StartsWith(Run([.. loaded, Assistant(205000)]), "Context 205k is large, break due since 205k:");
        StringAssert.StartsWith(Run([.. loaded, Assistant(250000)]), "Context 250k is large, break due since 245k:");

        // A resume that settles low keeps the 150k floor.
        StringAssert.StartsWith(Run(Command("resume-campaign"), Assistant(60000), User(), Assistant(150000)),
            "Context 150k is large, break due since 150k:");
    }

    [TestMethod]
    public void CrLfLineEndingsAreRead() =>
        Assert.AreEqual(180000, LastContext(WriteBytes(Encoding.UTF8.GetBytes(
            Assistant(180000) + "\r\n" + User() + "\r\n"))));

    [TestMethod]
    public void ALogWithoutAFinalNewlineIsRead() =>
        Assert.AreEqual(180000, LastContext(WriteBytes(Encoding.UTF8.GetBytes(Assistant(180000)))));

    [TestMethod]
    public void BytesThatAreNotUtf8AreSkippedLikeAnyOtherJunk()
    {
        var bytes = Encoding.UTF8.GetBytes(Assistant(180000) + "\n").Concat(new byte[] { 0xFF, 0xFE, 0x80, (byte)'\n' }).ToArray();
        Assert.AreEqual(180000, LastContext(WriteBytes(bytes)));
    }

    [TestMethod]
    public void AJsonValueThatIsNotAnObjectIsSkipped() =>
        Assert.AreEqual(180000, LastContext(Write(Assistant(180000), "[1, 2]", "5", "\"text\"", "null")));

    [TestMethod]
    public void ACompactionBoundaryStopsTheSearch() =>
        Assert.AreEqual(0, LastContext(Write(
            Assistant(240000), "{\"type\": \"system\", \"subtype\": \"compact_boundary\"}", User())));

    [TestMethod]
    public void ASystemRecordThatIsNotACompactionIsIgnored() =>
        Assert.AreEqual(240000, LastContext(Write(
            Assistant(240000), "{\"type\": \"system\", \"subtype\": \"other\"}", User())));

    // A BOM on stdin is not skipped (the script read it as a character and rejected the JSON), so the hook says nothing.
    [TestMethod]
    public void AByteOrderMarkOnStdinMakesTheInputUnreadable()
    {
        string input = new System.Text.Json.Nodes.JsonObject { ["transcript_path"] = Write(Assistant(90000), User(), Assistant(180000)) }.ToJsonString();
        Assert.AreNotEqual("", DmcRunner.BlackBox(["context-check"], input).Stdout, "the same input without a BOM gives the reminder");
        var result = DmcRunner.BlackBox(["context-check"], "﻿" + input);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    [TestMethod]
    public void AnEmptyFileHasNoContext() =>
        Assert.AreEqual(0, LastContext(WriteBytes([])));

    [TestMethod]
    public void MissingUsageFieldsCountAsZero() =>
        Assert.AreEqual(5000, LastContext(Write(
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": 5000}}}")));

    // The script gives up (prints nothing) when a record has a shape it cannot take apart. dmc does the same, so
    // the two never disagree about whether a reminder is due.
    [TestMethod]
    public void ARecordWithAnUnexpectedShapeGivesUpLikeTheScript()
    {
        string[] records =
        [
            "{\"type\": \"assistant\", \"message\": \"text\"}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": \"text\"}}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": \"7\"}}}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": null}}}",
        ];
        foreach (string record in records)
        {
            string path = Write(Assistant(180000), record);
            Assert.ThrowsExactly<InvalidDataException>(() => LastContext(path), record);

            var result = DmcRunner.InProcess(["context-check"],
                new JsonObject { ["transcript_path"] = path }.ToJsonString());
            Assert.AreEqual(0, result.ExitCode, record);
            Assert.AreEqual("", result.Stdout, record);
            Assert.AreEqual("", result.Stderr, record);
        }
    }

    [TestMethod]
    public void AnEmptyMessageOrUsageIsNotAnError() =>
        Assert.AreEqual(180000, LastContext(Write(
            Assistant(180000), "{\"type\": \"assistant\", \"message\": null}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": []}}", "{\"type\": \"assistant\", \"message\": {}}")));

    // Item 1: a line that is not UTF-8 anywhere is junk, as Python's json.loads(bytes) treats it. The check goes on
    // to the record before it. (Before, the JSON parsed and the bad bytes blew up later, ending the whole check.)
    [TestMethod]
    public void InvalidUtf8InsideAStringSkipsOnlyThatLine()
    {
        byte[] good = Encoding.UTF8.GetBytes(Assistant(90000) + "\n" + User() + "\n" + Assistant(180000) + "\n");
        byte[] bad = [.. Encoding.UTF8.GetBytes("{\"type\": \"assistant\", \"message\": {\"content\": \"x"), 0xFF, 0xFE,
            .. Encoding.UTF8.GetBytes("y\", \"usage\": {\"input_tokens\": 5}}}\n")];
        string path = WriteBytes([.. good, .. bad]);
        Assert.AreEqual(180000, LastContext(path));

        var result = DmcRunner.InProcess(["context-check"], new JsonObject { ["transcript_path"] = path }.ToJsonString());
        StringAssert.StartsWith(result.Stdout, "Context 180k");
    }

    [TestMethod]
    public void InvalidUtf8OutsideAStringSkipsOnlyThatLine()
    {
        byte[] good = Encoding.UTF8.GetBytes(Assistant(180000) + "\n");
        byte[] bad = [.. Encoding.UTF8.GetBytes("{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": 5}}}"), 0x80, (byte)'\n'];
        Assert.AreEqual(180000, LastContext(WriteBytes([.. good, .. bad])));
    }

    // Item 5: a BOM at the start of a line is dropped, as Python's utf-8-sig does.
    [TestMethod]
    public void AByteOrderMarkAtTheStartOfTheFirstLineIsDropped() =>
        Assert.AreEqual(180000, LastContext(WriteBytes([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Assistant(180000) + "\n" + User() + "\n")])));

    // Item 5: Python's json.loads reads NaN and Infinity as floats; the record then trips the script up when it
    // prints, so nothing is printed. dmc gives up on the same records.
    [TestMethod]
    public void NaNAndInfinityInAUsageCountGiveUpLikeTheScript()
    {
        foreach (string value in new[] { "NaN", "Infinity", "-Infinity" })
        {
            string path = Write(Assistant(180000),
                $"{{\"type\": \"assistant\", \"message\": {{\"usage\": {{\"input_tokens\": {value}}}}}}}");
            Assert.ThrowsExactly<InvalidDataException>(() => LastContext(path), value);
            var result = DmcRunner.InProcess(["context-check"], new JsonObject { ["transcript_path"] = path }.ToJsonString());
            Assert.AreEqual("", result.Stdout + result.Stderr, value);
        }
    }

    [TestMethod]
    public void NaNInAFieldThatIsNotUsedDoesNotSpoilTheRecord()
    {
        Assert.AreEqual(170000, LastContext(Write(Assistant(180000),
            "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": 170000, \"output_tokens\": NaN}, \"x\": [Infinity, -Infinity]}}")));
        // Inside a string it is just text.
        Assert.AreEqual(170000, LastContext(Write(
            "{\"type\": \"assistant\", \"message\": {\"content\": \"NaN Infinity\", \"usage\": {\"input_tokens\": 170000}}}")));
        // A compaction boundary that holds one still counts.
        Assert.AreEqual(0, LastContext(Write(Assistant(180000),
            "{\"type\": \"system\", \"subtype\": \"compact_boundary\", \"metadata\": {\"x\": NaN}}")));
    }

    // Item 6: only Windows treats a backslash as a separator after the tilde.
    [TestMethod]
    public void ExpandUserFollowsThePlatformsPathRules()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.AreEqual(home, Commands.ContextCheckCommand.ExpandUser("~", windows: false));
        Assert.AreEqual(home + "/logs/s.jsonl", Commands.ContextCheckCommand.ExpandUser("~/logs/s.jsonl", windows: false));
        Assert.AreEqual(home + "/logs/s.jsonl", Commands.ContextCheckCommand.ExpandUser("~/logs/s.jsonl", windows: true));
        Assert.AreEqual(home + "\\logs\\s.jsonl", Commands.ContextCheckCommand.ExpandUser("~\\logs\\s.jsonl", windows: true));
        Assert.AreEqual("~\\logs\\s.jsonl", Commands.ContextCheckCommand.ExpandUser("~\\logs\\s.jsonl", windows: false));
        Assert.AreEqual("~user/x", Commands.ContextCheckCommand.ExpandUser("~user/x", windows: false));
        Assert.AreEqual("/a/~/b", Commands.ContextCheckCommand.ExpandUser("/a/~/b", windows: true));
    }

    // Item 9: lines longer than a chunk, which used to be copied again for every chunk.
    [TestMethod]
    public void ALineLongerThanOneChunkAtTheEndOfTheLogIsSkipped()
    {
        string huge = ContextCheckContract.User(new string('x', 3 * TranscriptReader.ChunkSize)).ToJsonString();
        string path = Write(Assistant(180000), huge);
        Assert.IsGreaterThan(3L * TranscriptReader.ChunkSize, new FileInfo(path).Length);
        Assert.AreEqual(180000, LastContext(path));
    }

    [TestMethod]
    public void AnAssistantLineLongerThanOneChunkAtTheEndOfTheLogCounts()
    {
        string huge = ContextCheckContract.Assistant(170000).ToJsonString().Replace("\"content\":[]",
            "\"content\":\"" + new string('x', 2 * TranscriptReader.ChunkSize + 123) + "\"");
        string path = Write(Assistant(180000), huge);
        Assert.IsGreaterThan(2L * TranscriptReader.ChunkSize, new FileInfo(path).Length);
        Assert.AreEqual(170000, LastContext(path));
    }

    [TestMethod]
    public void ALineSpanningSeveralChunksInTheMiddleOfTheLogIsPutTogetherInOrder()
    {
        string huge = ContextCheckContract.Assistant(170000).ToJsonString().Replace("\"content\":[]",
            "\"content\":\"" + string.Concat(Enumerable.Range(0, 3 * TranscriptReader.ChunkSize / 10).Select(i => (i % 10).ToString())) + "\"");
        string path = Write(Assistant(160000), huge, User(), User());
        Assert.AreEqual(170000, LastContext(path));
    }

    [TestMethod]
    public void ALongLineAtTheStartOfTheLogIsRead()
    {
        string huge = ContextCheckContract.Assistant(170000).ToJsonString().Replace("\"content\":[]",
            "\"content\":\"" + new string('x', 2 * TranscriptReader.ChunkSize + 123) + "\"");
        Assert.AreEqual(170000, LastContext(Write(huge, User(), User())));
    }

    // Item 9: the cheap test that skips lines without parsing them must never skip a record that counts. JSON
    // lets any letter of "assistant" or "compact_boundary" be written as a \uXXXX escape.
    [TestMethod]
    public void ARecordWhoseTypeIsWrittenWithEscapesIsStillRead()
    {
        Assert.AreEqual(170000, LastContext(Write(
            "{\"type\": \"\\u0061ssistant\", \"message\": {\"usage\": {\"input_tokens\": 170000}}}")));
        Assert.AreEqual(170000, LastContext(Write(
            "{\"t\\u0079pe\": \"assist\\u0061nt\", \"message\": {\"usage\": {\"input_tokens\": 170000}}}")));
        Assert.AreEqual(0, LastContext(Write(Assistant(180000),
            "{\"type\": \"system\", \"subtype\": \"compact\\u005fboundary\"}")));
        Assert.AreEqual(0, LastContext(Write(Assistant(180000),
            "{\"type\": \"s\\u0079stem\", \"subtype\": \"compact_boundary\"}")));
    }

    [TestMethod]
    public void ALineThatCannotBeRelevantIsNotParsed()
    {
        // Not valid JSON, and no sign of either record type: skipped before parsing, with no effect on the answer.
        Assert.AreEqual(180000, LastContext(Write(Assistant(180000), "{\"type\": \"user\", \"content\": [1, 2,")));
    }

    // Item 10: the dispatcher alone enforces "never fails", for a failure inside the real command too.
    [TestMethod]
    public void AFailureReadingStdinPrintsNothingAndExitsZero()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();
        int exit = CommandLine.Run(["context-check"], new Io(new ThrowingReader(), output, errors), CommandTable.All);
        Assert.AreEqual(0, exit);
        Assert.AreEqual("", output.ToString() + errors);
    }

    /// <summary>A standard input whose every read fails, as a pipe does when the other end breaks.</summary>
    sealed class ThrowingReader : TextReader
    {
        public override string ReadToEnd() => throw new IOException("the pipe broke");

        public override int Peek() => throw new IOException("the pipe broke");

        public override int Read() => throw new IOException("the pipe broke");
    }

    [TestMethod]
    public void AnUnreadableLogPrintsNothing()
    {
        // A folder where the log should be: opening it fails, and the hook stays silent.
        var result = DmcRunner.InProcess(["context-check"],
            new JsonObject { ["transcript_path"] = _folder }.ToJsonString());
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    [TestMethod]
    public void AnythingThatFailsPrintsNothingAndExitsZero()
    {
        // The wrapper, not the command, is the last line of defence for a command that says it never fails.
        var result = DmcRunner.InProcess(["boom"], commands: [new ExplodingHook()]);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    /// <summary>A command that claims it never fails and then throws, to prove <see cref="CommandLine"/> contains it.</summary>
    sealed class ExplodingHook : ICommand
    {
        public string Name => "boom";
        public string Summary => "";
        public bool NeverFails => true;
        public int Run(string[] args, Io io) => throw new InvalidOperationException("unexpected");
    }
}
