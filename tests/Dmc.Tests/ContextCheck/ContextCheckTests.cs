using System.Text;
using System.Text.Json.Nodes;
using Dmc.ContextCheck;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>The 11 context-check tests against the library code, in this process.</summary>
[TestClass]
public sealed class ContextCheckInProcessTests : ContextCheckContract
{
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The 11 context-check tests against the published <c>dmc</c> binary, in a subprocess, as the hook runs it.</summary>
[TestClass]
public sealed class ContextCheckBlackBoxTests : ContextCheckContract
{
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}

/// <summary>Cases the script's tests do not cover: where the thresholds fall, and odd shapes in a real log.</summary>
[TestClass]
public sealed class ContextCheckEdgeTests
{
    string _folder = "";

    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    string WriteBytes(byte[] bytes)
    {
        string path = Path.Combine(_folder, "session.jsonl");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    string Write(params string[] lines) => WriteBytes(Encoding.UTF8.GetBytes(string.Join("\n", lines) + "\n"));

    static string Assistant(int context) => ContextCheckContract.Assistant(context).ToJsonString();

    static string User() => ContextCheckContract.User("go on").ToJsonString();

    [TestMethod]
    public void TheReminderStartsExactlyAtTheThreshold()
    {
        Assert.AreEqual("", BreakReminder.For(149999));
        Assert.AreEqual("Context 150k, break due since 150k: at the next break point, offer a full save and a fresh "
                        + "session, unless the player has declined since 150k.", BreakReminder.For(150000));
    }

    [TestMethod]
    public void TheBandChangesExactlyEvery40k()
    {
        StringAssert.Contains(BreakReminder.For(189999), "break due since 150k");
        StringAssert.Contains(BreakReminder.For(190000), "break due since 190k");
        StringAssert.Contains(BreakReminder.For(230000), "break due since 230k");
        StringAssert.Contains(BreakReminder.For(1000000), "Context 1000k, break due since 990k");
    }

    [TestMethod]
    public void CrLfLineEndingsAreRead() =>
        Assert.AreEqual(180000, TranscriptReader.LastContext(WriteBytes(Encoding.UTF8.GetBytes(
            Assistant(180000) + "\r\n" + User() + "\r\n"))));

    [TestMethod]
    public void ALogWithoutAFinalNewlineIsRead() =>
        Assert.AreEqual(180000, TranscriptReader.LastContext(WriteBytes(Encoding.UTF8.GetBytes(Assistant(180000)))));

    [TestMethod]
    public void BytesThatAreNotUtf8AreSkippedLikeAnyOtherJunk()
    {
        var bytes = Encoding.UTF8.GetBytes(Assistant(180000) + "\n").Concat(new byte[] { 0xFF, 0xFE, 0x80, (byte)'\n' }).ToArray();
        Assert.AreEqual(180000, TranscriptReader.LastContext(WriteBytes(bytes)));
    }

    [TestMethod]
    public void AJsonValueThatIsNotAnObjectIsSkipped() =>
        Assert.AreEqual(180000, TranscriptReader.LastContext(Write(Assistant(180000), "[1, 2]", "5", "\"text\"", "null")));

    [TestMethod]
    public void ACompactionBoundaryStopsTheSearch() =>
        Assert.AreEqual(0, TranscriptReader.LastContext(Write(
            Assistant(240000), "{\"type\": \"system\", \"subtype\": \"compact_boundary\"}", User())));

    [TestMethod]
    public void ASystemRecordThatIsNotACompactionIsIgnored() =>
        Assert.AreEqual(240000, TranscriptReader.LastContext(Write(
            Assistant(240000), "{\"type\": \"system\", \"subtype\": \"other\"}", User())));

    [TestMethod]
    public void AnEmptyFileHasNoContext() =>
        Assert.AreEqual(0, TranscriptReader.LastContext(WriteBytes([])));

    [TestMethod]
    public void MissingUsageFieldsCountAsZero() =>
        Assert.AreEqual(5000, TranscriptReader.LastContext(Write(
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
            Assert.ThrowsExactly<InvalidDataException>(() => TranscriptReader.LastContext(path), record);

            var result = DmcRunner.InProcess(["context-check"],
                new JsonObject { ["transcript_path"] = path }.ToJsonString());
            Assert.AreEqual(0, result.ExitCode, record);
            Assert.AreEqual("", result.Stdout, record);
            Assert.AreEqual("", result.Stderr, record);
        }
    }

    [TestMethod]
    public void AnEmptyMessageOrUsageIsNotAnError() =>
        Assert.AreEqual(180000, TranscriptReader.LastContext(Write(
            Assistant(180000), "{\"type\": \"assistant\", \"message\": null}",
            "{\"type\": \"assistant\", \"message\": {\"usage\": []}}", "{\"type\": \"assistant\", \"message\": {}}")));

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

    sealed class ExplodingHook : ICommand
    {
        public string Name => "boom";
        public string Summary => "";
        public bool NeverFails => true;
        public int Run(string[] args, Io io) => throw new InvalidOperationException("unexpected");
    }
}
