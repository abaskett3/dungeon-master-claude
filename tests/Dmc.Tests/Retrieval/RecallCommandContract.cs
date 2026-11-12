using Dmc.Tests.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// What <c>dmc recall</c> prints and returns (contracts/dmc-recall.md), checked against the library code in this process
/// and against the published binary (the two subclasses below): the campaign argument rule of research R10, exit codes,
/// one-line failures and LF output. Every test checks that the run changed nothing.
/// </summary>
public abstract class RecallCommandContract
{
    /// <summary>The test's temporary DMC folder, with the fixture chain.</summary>
    string _root = "";

    /// <summary>A hash of the DMC folder before the test ran the command.</summary>
    string _before = "";

    /// <summary>Run <c>dmc recall</c> with these arguments.</summary>
    protected abstract ProcessResult Recall(params string[] args);

    /// <summary>Makes the DMC folder and points <c>CLAUDE_PROJECT_DIR</c> at it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewChain();
        // A second campaign, active, so the tests can tell which one a recall searched.
        Write(_root, "adventures/side-trip/story/canon.md", "# Canon\n\n- The ferry at Side Trip is a rowing boat with one oar (s1.1).\n");
        Write(_root, "adventures/.active-campaign", "side-trip\n");
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _root);
        _before = SizeFixtures.Hash(_root);
    }

    /// <summary>Checks the command wrote nothing, then deletes the folder and forgets it as the project folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            Assert.AreEqual(_before, SizeFixtures.Hash(_root), "dmc recall must not write, move or delete anything");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
            DmcFolders.Delete(_root);
        }
    }

    [TestMethod]
    public void AFirstWordThatNamesACampaignIsTheCampaign()
    {
        var result = Recall(Sequel, "ferryman", "cart");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.StartsWith(result.StdoutLines[0], $"{Sequel} story/canon.md:9 s31.4 | ");
        Assert.DoesNotContain("side-trip", result.Stdout);
    }

    [TestMethod]
    public void WithoutACampaignTheActiveOneIsSearched()
    {
        var result = Recall("rowing boat oar");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("side-trip story/canon.md:3 s1.1 | - The ferry at Side Trip is a rowing boat with one oar (s1.1).\n", result.Stdout);
    }

    [TestMethod]
    public void ACampaignNameAloneIsAWordInTheActiveCampaign()
    {
        var result = Recall(Sequel);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual($"nothing matched: {Sequel}\n", result.Stdout);
    }

    [TestMethod]
    public void AMisspelledCampaignIsAWordInTheActiveCampaign()
    {
        var result = Recall("new-raod", "rowing", "oar");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.StartsWith(result.Stdout, "side-trip story/canon.md:3");
    }

    [TestMethod]
    public void NothingMatchedExitsZero()
    {
        var result = Recall(Sequel, "zebra");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("nothing matched: zebra\n", result.Stdout);
        Assert.AreEqual("", result.Stderr);
    }

    [TestMethod]
    public void OnlyCommonWordsIsOneLineWithExitOne()
    {
        var result = Recall(Sequel, "the", "and");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc recall: nothing to search for in \"the and\": every word is a common one\n", result.Stderr);
    }

    [TestMethod]
    public void NoWordsIsAUsageMistake()
    {
        var result = Recall();
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc recall: the following arguments are required: WORDS\n", result.Stderr);
    }

    /// <summary>
    /// <c>--folder</c> names the DMC folder when no project folder is set, and the output's line ends are LF.
    /// </summary>
    [TestMethod]
    public void TheFolderOptionIsUsedInsteadOfTheProjectFolder()
    {
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
        var result = Recall("--folder", _root, Sequel, "giant", "eel");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.StartsWith(result.Stdout, $"{Predecessor} (continues) story/canon-archive.md:3 s12.3 | ");
        Assert.DoesNotContain("\r", result.Stdout);
    }

    /// <summary>
    /// Over a linked set, a side campaign's hit and a digest's hit carry their link, and many matches across the set
    /// still keep to the 6,000-byte cap.
    /// </summary>
    [TestMethod]
    public void HitsFromOtherCampaignsAndDigestsCarryTheirLinkAndTheOutputKeepsItsCap()
    {
        string root = NewLinkedSet();
        try
        {
            var side = Recall("--folder", root, Last, "Thornwick", "bridge", "burned", "eclipse");
            Assert.AreEqual(0, side.ExitCode, side.Stderr);
            StringAssert.StartsWith(side.Stdout, $"{SideTale} (side) story/canon.md:3 s3.1 | - The Thornwick bridge burned");

            var digest = Recall("--folder", root, Last, "miller", "Tobin", "millpond");
            Assert.AreEqual(0, digest.ExitCode, digest.Stderr);
            StringAssert.StartsWith(digest.Stdout, $"{Last} digests/{First}/places.md:5 (continues) s4.2 | {FirstLine}\n");

            // Many matching lines across every campaign of the set: the output still keeps to its cap.
            foreach (string campaign in new[] { First, Middle, Last, SideTale, FarTale })
            {
                var log = new System.Text.StringBuilder("# Session 50\n\n### Scene 1: Barnacles\n\n");
                for (int i = 0; i < 60; i++)
                    log.Append($"Line {i} of {campaign}: the barnacles on the raft were scraped off again, a long and tedious morning's job.\n");
                Write(root, $"adventures/{campaign}/story/sessions/session-050.md", log.ToString());
            }
            var many = Recall("--folder", root, Last, "barnacles");
            Assert.AreEqual(0, many.ExitCode, many.Stderr);
            Assert.IsLessThanOrEqualTo(Dmc.Retrieval.Recall.CapBytes, System.Text.Encoding.UTF8.GetByteCount(many.Stdout));
            StringAssert.Matches(many.StdoutLines[^1], new System.Text.RegularExpressions.Regex(@"^\(\d+ more lines matched\)$"));
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Recall("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.StartsWith(result.Stdout, "usage: dmc recall [NAME] WORDS... [--folder PATH]");
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("recall ", StringComparison.Ordinal) && l.Contains("best match")));
    }
}

/// <summary>The recall tests against the library code, in this process.</summary>
[TestClass]
public sealed class RecallInProcessTests : RecallCommandContract
{
    /// <summary>Runs <c>dmc recall</c> in this process.</summary>
    protected override ProcessResult Recall(params string[] args) => DmcRunner.InProcess(["recall", .. args]);
}

/// <summary>The recall tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RecallBlackBoxTests : RecallCommandContract
{
    /// <summary>Runs <c>dmc recall</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Recall(params string[] args) => DmcRunner.BlackBox(["recall", .. args]);
}
