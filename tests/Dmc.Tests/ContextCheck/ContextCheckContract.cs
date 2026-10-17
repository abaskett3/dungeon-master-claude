using System.Text;
using System.Text.Json.Nodes;
using Dmc.ContextCheck;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The 11 tests of <c>tests/test_context_check.py</c>, ported one for one (<c>test_nothing_below_the_threshold</c>
/// is <see cref="NothingBelowTheThreshold"/>, and so on). They run twice, through the subclasses in
/// <c>ContextCheckTests.cs</c>: against the library code in this process, and against the published binary.
/// </summary>
public abstract class ContextCheckContract
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

    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>An assistant record of a log whose context size is <paramref name="context"/> tokens.</summary>
    /// <param name="context">The total of input, cache-read and cache-creation tokens.</param>
    /// <param name="sidechain">True for a record of a sub-agent's conversation, which does not count.</param>
    // Split the context the way the API reports it: a little fresh input, most of it from the cache.
    internal static JsonObject Assistant(int context, bool sidechain = false) => new()
    {
        ["type"] = "assistant",
        ["isSidechain"] = sidechain,
        ["message"] = new JsonObject
        {
            ["id"] = "m",
            ["usage"] = new JsonObject
            {
                ["input_tokens"] = 10,
                ["cache_read_input_tokens"] = context - 1010,
                ["cache_creation_input_tokens"] = 1000,
                ["output_tokens"] = 500,
            },
            ["content"] = new JsonArray(),
        },
    };

    /// <summary>A user record holding a typed message.</summary>
    internal static JsonObject User(string text) => new()
    {
        ["type"] = "user",
        ["message"] = new JsonObject { ["role"] = "user", ["content"] = text },
    };

    /// <summary>Write a session log of these records, plus <paramref name="raw"/> at the end, and return its path.</summary>
    string LogWithTail(string raw, params JsonNode[] records)
    {
        string path = Path.Combine(_folder, "session.jsonl");
        var text = new StringBuilder();
        foreach (var record in records)
            text.Append(record.ToJsonString()).Append('\n');
        text.Append(raw);
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Write a session log of these records and return its path.</summary>
    string Log(params JsonNode[] records) => LogWithTail("", records);

    /// <summary>What the hook prints when Claude Code passes it the log at <paramref name="path"/>.</summary>
    string Check(string path) =>
        CheckInput(new JsonObject { ["session_id"] = "s", ["transcript_path"] = path }.ToJsonString());

    /// <summary>What the hook prints for this exact stdin; it must exit 0 and keep stderr empty.</summary>
    string CheckInput(string stdin)
    {
        var result = ContextCheck(stdin);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stderr);
        return result.Stdout.Trim();
    }

    [TestMethod]
    public void NothingBelowTheThreshold() =>
        Assert.AreEqual("", Check(Log(User("hi"), Assistant(149999), User("go on"))));

    [TestMethod]
    public void TheFirstBandFrom150k()
    {
        string line = Check(Log(Assistant(90000), User("go on"), Assistant(163400), User("I attack")));
        Assert.AreEqual("Context 163k, break due since 150k: at the next break point, offer a full save "
                        + "and a fresh session, unless the player has declined since 150k.", line);
    }

    [TestMethod]
    public void TheBandRisesEvery40k()
    {
        string line = Check(Log(Assistant(195000), User("go on")));
        StringAssert.StartsWith(line, "Context 195k, break due since 190k:");
        StringAssert.EndsWith(line, "declined since 190k.");
    }

    [TestMethod]
    public void OnlyTheLastCallCounts() =>
        Assert.AreEqual("", Check(Log(Assistant(200000), Assistant(80000), User("go on"))));

    [TestMethod]
    public void SubagentsAndEmptyUsageAreSkipped()
    {
        var empty = new JsonObject
        {
            ["type"] = "assistant",
            ["message"] = new JsonObject { ["id"] = "x", ["model"] = "<synthetic>", ["usage"] = new JsonObject(), ["content"] = new JsonArray() },
        };
        string line = Check(Log(Assistant(160000), Assistant(20000, sidechain: true), empty));
        StringAssert.StartsWith(line, "Context 160k");
    }

    [TestMethod]
    public void NothingRightAfterACompaction()
    {
        var compacted = new JsonObject { ["type"] = "system", ["subtype"] = "compact_boundary" };
        Assert.AreEqual("", Check(Log(Assistant(240000), compacted, User("summary"))));
    }

    [TestMethod]
    public void ACutLastLineAndJunkAreIgnored() =>
        StringAssert.StartsWith(Check(LogWithTail("{\"type\": \"assistant\", \"mess", Assistant(170000))), "Context 170k");

    [TestMethod]
    public void ALogLongerThanOneChunk()
    {
        var filler = User(new string('x', TranscriptReader.ChunkSize / 2)); // each line crosses a chunk boundary
        string path = Log(Assistant(175000), filler, filler, filler, filler, filler, User("go on"));
        Assert.IsGreaterThan(2L * TranscriptReader.ChunkSize, new FileInfo(path).Length);
        StringAssert.StartsWith(Check(path), "Context 175k");
    }

    [TestMethod]
    public void NothingForAMissingLog() =>
        Assert.AreEqual("", Check(Path.Combine(_folder, "missing.jsonl")));

    [TestMethod]
    public void NothingForALogWithoutUsage()
    {
        Assert.AreEqual("", Check(Log(User("hi"))));
        Assert.AreEqual("", Check(Log()));
    }

    [TestMethod]
    public void NothingForBadHookInput()
    {
        foreach (string stdin in new[] { "", "not json", "[1, 2]", "{\"transcript_path\": 5}", "{\"session_id\": \"s\"}" })
            Assert.AreEqual("", CheckInput(stdin), $"stdin: '{stdin}'");
    }
}
