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

    /// <summary>A context past 150,000 gets the break line for the first band, word for word, with the command to offer.</summary>
    [TestMethod]
    public void TheFirstBandFrom150k()
    {
        string line = Check(Log(Assistant(90000), User("go on"), Assistant(163400), User("I attack")));
        Assert.AreEqual("Context 163k is large, break due since 150k: at the next break point, offer a full save (`/dmc-campaign-full-save`) "
                        + "and a fresh session, unless the player has declined since 150k.", line);
    }

    /// <summary>The band named rises every 50,000 tokens: 195k is still the 150k band, 205k the 200k band.</summary>
    [TestMethod]
    public void TheBandRisesEvery50k()
    {
        StringAssert.StartsWith(Check(Log(Assistant(90000), User("go on"), Assistant(195000))), "Context 195k is large, break due since 150k:");
        string line = Check(Log(Assistant(90000), User("go on"), Assistant(205000)));
        StringAssert.StartsWith(line, "Context 205k is large, break due since 200k:");
        StringAssert.EndsWith(line, "declined since 200k.");
    }

    /// <summary>An assistant record whose one content block, of <paramref name="type"/>, holds <paramref name="text"/>.</summary>
    /// <param name="context">The context size, as <see cref="Assistant"/> takes it.</param>
    /// <param name="text">The block's text.</param>
    /// <param name="type"><c>text</c> for the reply, <c>thinking</c> for its thinking.</param>
    static JsonObject Saying(int context, string text, string type = "text")
    {
        var record = Assistant(context);
        var block = new JsonObject { ["type"] = type, [type == "thinking" ? "thinking" : "text"] = text };
        record["message"]!["content"] = new JsonArray(block);
        return record;
    }

    /// <summary>A reply that offers the break, as DMC words one.</summary>
    const string Offer = "(OOC: this is a break point. I can run `/dmc-campaign-full-save` now. Playing on counts as declining.)";

    /// <summary>A reply in the band that offered the break spends its line: no line again until the next band.</summary>
    [TestMethod]
    public void AnOfferInTheBandSpendsItsLine() =>
        Assert.AreEqual("", Check(Log(Assistant(90000), User("go on"), Saying(212000, Offer), User("I look around"),
            Assistant(222000))));

    /// <summary>An offer in an earlier band does not silence the next band's line.</summary>
    [TestMethod]
    public void AnOfferInAnEarlierBandDoesNotSpendTheNext() =>
        StringAssert.StartsWith(Check(Log(Assistant(90000), User("go on"), Saying(205000, Offer), User("on"),
            Assistant(255000))), "Context 255k is large, break due since 250k:");

    /// <summary>Thinking that names the full save is not an offer: the line still prints.</summary>
    [TestMethod]
    public void OnlyTheReplysTextIsAnOffer() =>
        StringAssert.StartsWith(Check(Log(Assistant(90000), User("go on"),
            Saying(212000, "I should offer /dmc-campaign-full-save soon.", "thinking"), User("on"), Assistant(222000))),
            "Context 222k is large, break due since 200k:");

    /// <summary>Only the latest call's context counts, not a larger one before it.</summary>
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
        string line = Check(Log(Assistant(90000), User("go on"), Assistant(160000), Assistant(20000, sidechain: true), empty));
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
        StringAssert.StartsWith(Check(LogWithTail("{\"type\": \"assistant\", \"mess", Assistant(90000), User("go on"), Assistant(170000))), "Context 170k");

    [TestMethod]
    public void ALogLongerThanOneChunk()
    {
        var filler = User(new string('x', TranscriptReader.ChunkSize / 2)); // each line crosses a chunk boundary
        string path = Log(Assistant(90000), User("go on"), Assistant(175000), filler, filler, filler, filler, filler, User("go on"));
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
