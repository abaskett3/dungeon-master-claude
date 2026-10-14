using System.Text;
using System.Text.Json.Nodes;
using Dmc.ContextCheck;
using Dmc.Tests.ContextCheck;
using Dmc.Tests.Support;

namespace Dmc.Tests.Parity;

/// <summary>
/// <c>dmc context-check</c> against <c>scripts/context_check.py</c>, on the same hook input and the same log:
/// below the threshold, above it, in a later band, at a compaction boundary, and with bad input. stdout,
/// stderr, the exit code and the files in the working folder must be identical.
/// </summary>
[TestClass]
public sealed class ContextCheckParityTests
{
    const string Log = "session.jsonl";

    static readonly ParityTool ContextCheckPy = new("context_check.py", "context-check", (work, args, stdin) =>
    {
        string? python = Legacy.Python();
        TestEnv.Need(python is not null, "Python 3");
        return TestEnv.Run(python!, [Legacy.Script("context_check.py"), .. args], stdin, work);
    });

    static string Assistant(int context, bool sidechain = false) => ContextCheckContract.Assistant(context, sidechain).ToJsonString();

    static string User() => ContextCheckContract.User("go on").ToJsonString();

    /// <summary>A case whose hook input names the log, with these lines (and no final newline unless one is included).</summary>
    static ParityCase WithLog(string name, string content) => new(name, _ => [],
        work => new JsonObject { ["session_id"] = "s", ["transcript_path"] = Path.Combine(work, Log) }.ToJsonString(),
        work => File.WriteAllText(Path.Combine(work, Log), content, new UTF8Encoding(false)));

    static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    /// <summary>A case with this exact stdin and no log.</summary>
    static ParityCase WithInput(string name, string stdin) => new(name, _ => [], _ => stdin);

    static ParityCase[] Cases { get; } =
    [
        // Below the threshold: nothing.
        WithLog("below the threshold", Lines(User(), Assistant(149999), User())),
        WithLog("only the last call counts", Lines(Assistant(200000), Assistant(80000), User())),
        // From the threshold: one line, naming the band.
        WithLog("exactly at the threshold", Lines(Assistant(150000), User())),
        WithLog("above the threshold", Lines(Assistant(90000), User(), Assistant(163400), User())),
        // In a later band.
        WithLog("in the second band", Lines(Assistant(195000), User())),
        WithLog("just below the second band", Lines(Assistant(189999))),
        WithLog("in the fifth band", Lines(Assistant(310000), User())),
        // Which records count.
        WithLog("subagents are skipped", Lines(Assistant(160000), Assistant(20000, sidechain: true))),
        WithLog("empty usage is skipped", Lines(Assistant(160000),
            "{\"type\": \"assistant\", \"message\": {\"id\": \"x\", \"model\": \"<synthetic>\", \"usage\": {}, \"content\": []}}")),
        WithLog("a cut last line and junk", Lines(Assistant(170000)) + "{\"type\": \"assistant\", \"mess"),
        WithLog("a log with CRLF line endings", Lines(Assistant(180000), User()).Replace("\n", "\r\n")),
        WithLog("no final newline", Assistant(180000)),
        // At a compaction boundary.
        WithLog("right after a compaction", Lines(Assistant(240000), "{\"type\": \"system\", \"subtype\": \"compact_boundary\"}", User())),
        WithLog("a call after a compaction", Lines(Assistant(240000), "{\"type\": \"system\", \"subtype\": \"compact_boundary\"}", Assistant(160000))),
        WithLog("a system record that is not a compaction", Lines(Assistant(240000), "{\"type\": \"system\", \"subtype\": \"x\"}")),
        // A log longer than one chunk read from the end.
        WithLog("a log longer than one chunk", Lines([Assistant(175000), .. Enumerable.Repeat(ContextCheckContract.User(new string('x', TranscriptReader.ChunkSize / 2)).ToJsonString(), 5), User()])),
        // Records of an unexpected shape: the script gives up, and so does dmc.
        WithLog("a message that is a string", Lines(Assistant(180000), "{\"type\": \"assistant\", \"message\": \"text\"}")),
        WithLog("a token count that is a string", Lines(Assistant(180000), "{\"type\": \"assistant\", \"message\": {\"usage\": {\"input_tokens\": \"7\"}}}")),
        WithLog("an empty message and usage", Lines(Assistant(180000), "{\"type\": \"assistant\", \"message\": null}", "{\"type\": \"assistant\", \"message\": {\"usage\": []}}")),
        WithLog("values that are not objects", Lines(Assistant(180000), "[1, 2]", "5", "\"text\"", "null", "")),
        // No log, an empty one, and bad input: never an error, never any output.
        WithLog("an empty log", ""),
        WithLog("a log with no usage", Lines(User())),
        new("a missing log", _ => [], work => new JsonObject { ["transcript_path"] = Path.Combine(work, "missing.jsonl") }.ToJsonString()),
        new("a folder instead of a log", _ => [], work => new JsonObject { ["transcript_path"] = work }.ToJsonString()),
        WithInput("empty input", ""),
        WithInput("not json", "not json"),
        WithInput("an array", "[1, 2]"),
        WithInput("a path that is a number", "{\"transcript_path\": 5}"),
        WithInput("an empty path", "{\"transcript_path\": \"\"}"),
        WithInput("no path", "{\"session_id\": \"s\"}"),
        WithInput("only spaces", "   "),
    ];

    public static IEnumerable<object[]> CaseData => Cases.Select(c => new object[] { c });

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    [TestMethod]
    [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
    public void ContextCheckMatchesContextCheckPy(ParityCase test) => ParityCheck.Run(ContextCheckPy, test);
}
