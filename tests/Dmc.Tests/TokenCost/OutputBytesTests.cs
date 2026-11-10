using System.Text;
using System.Text.Json.Nodes;
using Dmc.Tests.Support;

namespace Dmc.Tests.TokenCost;

/// <summary>
/// What dmc writes, byte for byte, through the real process: UTF-8, no byte-order mark, and "\n" line ends. A BOM
/// or a CRLF would land in DMC's context, and a BOM would break a hook's output. The helper that runs the
/// process keeps the raw bytes, because a text reader would drop a leading BOM before the test saw it.
/// </summary>
[TestClass]
public sealed class OutputBytesTests
{
    /// <summary>The UTF-8 byte-order mark.</summary>
    static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    /// <summary>Asserts that captured output exists and does not start with a byte-order mark.</summary>
    /// <param name="bytes">The raw bytes of a stream; null (a result from an in-process run) fails the test.</param>
    /// <param name="what">The stream and command, for the failure message.</param>
    static void AssertNoBom(byte[]? bytes, string what)
    {
        Assert.IsNotNull(bytes, what);
        Assert.IsFalse(bytes.AsSpan().StartsWith(Bom), $"{what} starts with a byte-order mark");
    }

    [TestMethod]
    public void NeitherStdoutNorStderrStartsWithAByteOrderMark()
    {
        string[][] commands =
        [
            ["roll", "1d20+5"],
            ["roll", "banana"],
            ["roll", "é中"], // non-ASCII is echoed
            ["roll"],
            ["--version"],
            ["--help"],
            ["nope"],
            [],
        ];
        foreach (string[] args in commands)
        {
            var result = DmcRunner.BlackBox(args);
            string name = "dmc " + string.Join(' ', args);
            AssertNoBom(result.StdoutBytes, name + " stdout");
            AssertNoBom(result.StderrBytes, name + " stderr");
            Assert.IsGreaterThan(0, result.StdoutBytes!.Length + result.StderrBytes!.Length, name + " printed nothing");
            Assert.IsFalse(result.Stdout.Contains('\r') || result.Stderr.Contains('\r'), name + " wrote a CR");
        }
    }

    [TestMethod]
    public void TheHookReminderIsPlainUtf8WithAnLfLineEnd()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string log = Path.Combine(folder, "session.jsonl");
            File.WriteAllText(log, ContextCheck.ContextCheckContract.Assistant(31000).ToJsonString() + "\n"
                                   + ContextCheck.ContextCheckContract.User("go on").ToJsonString() + "\n"
                                   + ContextCheck.ContextCheckContract.Assistant(163400).ToJsonString() + "\n");
            var result = DmcRunner.BlackBox(["context-check"], new JsonObject { ["transcript_path"] = log }.ToJsonString());
            AssertNoBom(result.StdoutBytes, "context-check stdout");
            Assert.AreEqual("Context 163k is large, break due since 150k: at the next break point, offer a full save and a fresh "
                            + "session, unless the player has declined since 150k.\n", Encoding.UTF8.GetString(result.StdoutBytes!));
            Assert.AreEqual(0, result.StderrBytes!.Length);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// <c>dmc size</c>'s full report and its <c>--check</c> form, through the real process, on a fixture with a non-ASCII
    /// name and a flag so both print: no byte-order mark, no CR, exit 0.
    /// </summary>
    [TestMethod]
    public void TheSizeReportAndItsCheckFormArePlainUtf8WithLfLineEnds()
    {
        string root = Size.SizeFixtures.NewDmcFolderWithCampaign();
        try
        {
            // A Party status block with an accent, and a file over its budget, so both forms print something.
            Size.SizeFixtures.Write(root, "game-state.md", Size.SizeFixtures.GameState(["Élodie"]) + Size.SizeFixtures.Filler(20000));
            foreach (string[] args in new[] { new[] { "size", "--folder", root }, ["size", "--folder", root, "--check"] })
            {
                var result = DmcRunner.BlackBox(args);
                string name = string.Join(' ', args[..1]) + (args.Length > 3 ? " --check" : "");
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                AssertNoBom(result.StdoutBytes, name + " stdout");
                Assert.IsGreaterThan(0, result.StdoutBytes!.Length, name + " printed nothing");
                Assert.IsFalse(result.Stdout.Contains('\r'), name + " wrote a CR");
            }
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }

    [TestMethod]
    public void NonAsciiIsWrittenAsUtf8()
    {
        var result = DmcRunner.BlackBox(["roll", "é中"]);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("é中: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)\n"),
            result.StdoutBytes);
    }

    [TestMethod]
    public void LineBreaksInAnExpressionAreEscapedInTheRealProcessToo()
    {
        var result = DmcRunner.BlackBox(["roll", "a\r\nb"]);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("a\\r\\nb: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)\n"),
            result.StdoutBytes);
    }
}
