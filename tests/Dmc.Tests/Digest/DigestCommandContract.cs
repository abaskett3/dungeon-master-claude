using System.Text.Json.Nodes;
using Dmc.Tests.Support;
using static Dmc.Tests.Digest.DigestFixtures;

namespace Dmc.Tests.Digest;

/// <summary>
/// The <c>CommandLine</c> class of <c>tests/test_transcript_digest.py</c>, ported one for one (<c>test_list</c> is
/// <see cref="List"/>, and so on), against a folder of three made-up logs. They run twice, through the two subclasses
/// below: against the library code in this process, and against the published <c>dmc</c> binary, the way DMC runs it.
/// In Python they run the script as a subprocess; the error prefix is "dmc digest: " here, where the script's messages had none.
/// </summary>
public abstract class DigestCommandContract
{
    /// <summary>The test's temporary working folder, which holds <see cref="_logs"/> and <see cref="_out"/>.</summary>
    string _folder = "";

    /// <summary>The folder of made-up session logs, <c>logs</c> inside the working folder.</summary>
    string _logs = "";

    /// <summary>The folder <c>show</c> writes to, <c>out</c> inside the working folder; created by the command.</summary>
    string _out = "";

    /// <summary>Run <c>dmc digest</c> with these arguments.</summary>
    protected abstract ProcessResult Digest(params string[] args);

    /// <summary>Creates the working folder and fills <c>logs</c> with the three made-up sessions.</summary>
    [TestInitialize]
    public void MakeFolders()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        _logs = Path.Combine(_folder, "logs");
        _out = Path.Combine(_folder, "out");
        Directory.CreateDirectory(_logs);
        WriteLogFolder(_logs);
    }

    /// <summary>Deletes the working folder.</summary>
    [TestCleanup]
    public void RemoveFolders() => Directory.Delete(_folder, recursive: true);

    /// <summary>Run a subcommand (<c>list</c> or <c>show</c>) on the made-up logs, with these further arguments.</summary>
    ProcessResult Run(string command, params string[] args) => Digest([command, "--logs", _logs, .. args]);

    /// <summary>The Python test's <c>digest</c>: the logs and the output folder, then these arguments.</summary>
    ProcessResult DigestOf(params string[] args) => Run("show", ["--out", _out, .. args]);

    /// <summary>The names of the files <c>show</c> wrote to the output folder, in ordinal order.</summary>
    string[] Listing() => Directory.GetFileSystemEntries(_out).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray();

    /// <summary>One file of the output folder, with LF line endings.</summary>
    string Read(string name) => File.ReadAllText(Path.Combine(_out, name)).Replace("\r\n", "\n");

    [TestMethod]
    public void List()
    {
        var result = Run("list");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        string[] lines = result.StdoutLines;
        Assert.HasCount(4, lines); // the heading and three sessions
        var rows = lines[1..].ToDictionary(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);
        CollectionAssert.AreEquivalent(new[] { "abcd1234", "ffff9999", "eeee0000" }, rows.Keys);
        StringAssert.Contains(rows["abcd1234"], "silver-egg");
        StringAssert.Contains(rows["abcd1234"], "Resume adventures/silver-egg/ please");
        StringAssert.Contains(rows["abcd1234"], "2026-01-02 10:00");
        StringAssert.Contains(rows["abcd1234"], "3500");
        StringAssert.Contains(rows["ffff9999"], "red-keep");
    }

    [TestMethod]
    public void ListFilters()
    {
        string[] Ids(params string[] args)
        {
            var result = Run("list", args);
            Assert.AreEqual(0, result.ExitCode, result.Stderr);
            return result.StdoutLines[1..].Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]).Order().ToArray();
        }
        CollectionAssert.AreEqual(new[] { "ffff9999" }, Ids("--campaign", "red-keep"));
        Assert.IsEmpty(Ids("--campaign", "nowhere"));
        CollectionAssert.AreEqual(new[] { "ffff9999" }, Ids("--after", "2026-02-01"));
        CollectionAssert.AreEqual(new[] { "abcd1234", "eeee0000", "ffff9999" }, Ids("--after", "2026-01-02"));
        CollectionAssert.AreEqual(new[] { "ffff9999" }, Ids("--after", "2026-01-03"));
    }

    [TestMethod]
    public void DigestWritesThreeKindsOfFile()
    {
        var result = DigestOf();
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        // The session where the player never spoke is left out.
        CollectionAssert.AreEqual(new[] { "abcd1234-transcript.md", "abcd1234-turns.txt", "ffff9999-transcript.md", "ffff9999-turns.txt", "stats.md" }, Listing());
        string report = Read("stats.md");
        Assert.IsTrue(report.StartsWith("# Transcript statistics", StringComparison.Ordinal));
        StringAssert.Contains(report, "## Session abcd1234");
        StringAssert.Contains(report, "## Session ffff9999");
        StringAssert.Contains(result.Stdout.Replace("\r\n", "\n"), report, "the statistics are also printed");
        StringAssert.Contains(Read("abcd1234-turns.txt"), "!  2 10:04:00");
        StringAssert.Contains(Read("abcd1234-transcript.md"), "### DM 10:01:00 [ctx 1000]");
    }

    [TestMethod]
    public void DigestOfChosenSessions()
    {
        var result = DigestOf("ffff");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "ffff9999-transcript.md", "ffff9999-turns.txt", "stats.md" }, Listing());
    }

    [TestMethod]
    public void DigestOfOneCampaign()
    {
        var result = DigestOf("--campaign", "silver-egg");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "abcd1234-transcript.md", "abcd1234-turns.txt", "stats.md" }, Listing());
    }

    [TestMethod]
    public void DigestWithNothingToShow()
    {
        foreach (string[] args in new string[][] { ["zzzz"], ["eeee"], ["--campaign", "nowhere"], ["--after", "2030-01-01"] })
        {
            var result = DigestOf(args);
            Assert.AreEqual(1, result.ExitCode, string.Join(' ', args));
            StringAssert.Contains(result.Stderr, "No matching sessions");
            StringAssert.Contains(result.Stderr, "dmc digest: ");
            Assert.IsFalse(Directory.Exists(_out), string.Join(' ', args));
        }
    }

    [TestMethod]
    public void MissingLogFolder()
    {
        var result = Digest("list", "--logs", Path.Combine(_folder, "nope"));
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stderr, "No session logs found");
    }

    [TestMethod]
    public void NonAsciiTextSurvives()
    {
        WriteLog(_logs, [User(0, "Thrór says “hello” — 北"), Assistant(1, "m1", Blocks(Text("⚔ Roll 1d20.")), 1, 1, 1, 1)], "cccc7777-3333");
        var result = DigestOf("cccc");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        string text = Read("cccc7777-transcript.md");
        StringAssert.Contains(text, "Thrór says “hello” — 北");
        StringAssert.Contains(text, "⚔ Roll 1d20.");
        StringAssert.Contains(result.Stdout, "cccc7777"); // the printed statistics survive a pipe too
    }

    // ---------- beyond the Python tests ----------

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        foreach (string[] args in new string[][] { ["--help"], ["list", "-h"], ["show", "--logs", "x", "--help"] })
        {
            var result = Digest(args);
            Assert.AreEqual(0, result.ExitCode, string.Join(' ', args));
            StringAssert.Contains(result.Stdout, "usage: dmc digest list");
            Assert.IsTrue(result.Stdout.EndsWith("on that call.\n", StringComparison.Ordinal), "the help ends in one line break, no blank line");
            Assert.AreEqual("", result.Stderr);
        }
    }

    [TestMethod]
    public void TheCommandMustBeListOrShow()
    {
        var result = Digest("digest"); // the old name of show, which is not kept
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc digest: argument command: invalid choice: 'digest' (choose from list, show)\n", result.Stderr);
    }

    [TestMethod]
    public void TheOutputFolderIsMadeAndItsFilesAreOverwritten()
    {
        Assert.AreEqual(0, DigestOf("ffff").ExitCode);
        File.WriteAllText(Path.Combine(_out, "ffff9999-turns.txt"), "old");
        Assert.AreEqual(0, DigestOf("ffff").ExitCode);
        StringAssert.Contains(Read("ffff9999-turns.txt"), "red-keep");
        // The raw bytes: Read() turns CRLF into LF, so it could not tell.
        foreach (string name in new[] { "stats.md", "ffff9999-turns.txt", "ffff9999-transcript.md" })
            Assert.IsFalse(File.ReadAllBytes(Path.Combine(_out, name)).Contains((byte)'\r'), $"{name}: dmc writes LF on every OS");
    }

    [TestMethod]
    public void ALogThatClaudeCodeIsStillWritingToCanBeRead()
    {
        // The log of the session in progress is open for appending while review-transcript runs. Reading it must neither
        // fail with a sharing violation (Windows) nor stop the writer from appending.
        string live = Path.Combine(_logs, SessionId + ".jsonl");
        using (var writer = new FileStream(live, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            Assert.AreEqual(0, Run("list").ExitCode);
            Assert.AreEqual(0, DigestOf("abcd").ExitCode);
            writer.Write("\n"u8);
            writer.Flush();
        }
    }
}

/// <summary>The digest tests against the library code, in this process.</summary>
[TestClass]
public sealed class DigestInProcessTests : DigestCommandContract
{
    /// <summary>Runs <c>dmc digest</c> in this process.</summary>
    protected override ProcessResult Digest(params string[] args) => DmcRunner.InProcess(["digest", .. args]);
}

/// <summary>The digest tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class DigestBlackBoxTests : DigestCommandContract
{
    /// <summary>Runs <c>dmc digest</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Digest(params string[] args) => DmcRunner.BlackBox(["digest", .. args]);
}
