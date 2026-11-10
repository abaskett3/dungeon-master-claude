using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.Size;

/// <summary>
/// What <c>dmc size</c> prints and returns (contracts/dmc-size.md), checked twice (see the two subclasses below): against
/// the library code in this process, and against the published <c>dmc</c> binary, the way DMC runs it. Both find the DMC
/// folder through <c>CLAUDE_PROJECT_DIR</c>, which the test sets to its temporary folder and the binary inherits, or
/// through <c>--folder</c>. Every test checks that the run changed nothing.
/// </summary>
public abstract class SizeCommandContract
{
    /// <summary>The test's temporary DMC folder, with a fixture campaign.</summary>
    string _root = "";

    /// <summary>A hash of the DMC folder before the test ran the command.</summary>
    string _before = "";

    /// <summary>Run <c>dmc size</c> with these arguments.</summary>
    protected abstract ProcessResult Size(params string[] args);

    /// <summary>Makes the DMC folder with a fresh campaign and points <c>CLAUDE_PROJECT_DIR</c> at it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewDmcFolderWithCampaign();
        Write(_root, "game-state.md", GameState([]));
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _root);
        _before = Hash(_root);
    }

    /// <summary>Checks the command wrote nothing (FR-012), then deletes the folder and forgets it as the project folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            Assert.AreEqual(_before, Hash(_root), "dmc size must not write, move or delete anything");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
            DmcFolders.Delete(_root);
        }
    }

    /// <summary>Changes the fixture before the run, then takes the hash the cleanup compares against.</summary>
    /// <param name="change">What to change.</param>
    void Arrange(Action change)
    {
        change();
        _before = Hash(_root);
    }

    [TestMethod]
    public void TheFullReportOfANewCampaignListsItsFilesTotalAndNothingToFlag()
    {
        var result = Size();

        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stderr);
        string[] lines = result.StdoutLines;
        Assert.AreEqual($"{Name}: what a resume reads, in estimated tokens (bytes / 4)", lines[0]);
        Assert.IsTrue(lines[1].TrimStart().StartsWith("adventures/player-options.md", StringComparison.Ordinal), lines[1]);
        Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("total ", StringComparison.Ordinal)), "a total line");
        Assert.IsTrue(lines.Any(l => l.Contains("story/canon.md") && l.Contains("limit 19k") && l.Contains("read whole")));
        Assert.AreEqual("  nothing to flag", lines[^1]);
        Assert.DoesNotContain("\r", result.Stdout, "output is LF on every system");
    }

    [TestMethod]
    public void TheCheckFormPrintsNothingWhenAllIsWell()
    {
        var result = Size("--check");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    [TestMethod]
    public void TheCheckFormPrintsOneLinePerFlagCanonFirstAndExitsZero()
    {
        Arrange(() =>
        {
            Write(_root, "game-state.md", GameState([]) + Filler(Budgets.GameState * 4));
            Write(_root, "story/canon.md", "# Canon\n\n## Rulings\n\n- one\n\n## Timeline\n\n" + Filler(Budgets.CanonLimit * 4 + 400));
        });
        var result = Size("--check", "--folder", _root, Name);

        Assert.AreEqual(0, result.ExitCode, "flags are not a failure");
        Assert.AreEqual("", result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            "story/canon.md 19.2k is over its 19k limit: read it in sections; a full save archives it down to 16k (move about 3.2k)",
            "game-state.md 4.1k is over its 4k budget",
        }, result.StdoutLines);
    }

    [TestMethod]
    public void TheFullReportShowsCanonInSectionsAndSaysTheSearchIsNotCounted()
    {
        Arrange(() => Write(_root, "story/canon.md", "# Canon\n\n## Rulings\n\n- one\n\n## Timeline\n\n" + Filler(Budgets.CanonLimit * 4 + 400)));
        string[] lines = Size().StdoutLines;
        int canon = Array.FindIndex(lines, l => l.TrimStart().StartsWith("story/canon.md", StringComparison.Ordinal));
        StringAssert.Contains(lines[canon], "read in sections");
        Assert.AreEqual("    (the search for the scene's names is not counted)", lines[canon + 1]);
        string sections = lines.Single(l => l.StartsWith("  story/canon.md: ", StringComparison.Ordinal));
        StringAssert.StartsWith(sections, "  story/canon.md: Timeline 19.2k", "canon's own sections are listed when it is over its limit, the archive step's guide");
    }

    [TestMethod]
    public void TheLargestSectionsAreListedFourAtMostLargestFirst()
    {
        Arrange(() => Write(_root, "story/dm-notes.md",
            "# Notes\n\n## A\n\n" + Filler(400) + "## B\n\n" + Filler(4000) + "## C\n\n" + Filler(800) + "## D\n\n" + Filler(1200) + "## E\n\n" + Filler(1600)));
        string line = Size().StdoutLines.Single(l => l.StartsWith("  story/dm-notes.md: ", StringComparison.Ordinal));
        Assert.AreEqual("  story/dm-notes.md: B 1.1k, E 401, D 301, C 201 (+2 more)", line);
    }

    [TestMethod]
    public void ANamedCampaignIsMeasuredInsteadOfTheActiveOne()
    {
        Arrange(() => NewCampaign(_root, "other"));
        Assert.AreEqual("other: what a resume reads, in estimated tokens (bytes / 4)", Size("other").StdoutLines[0]);
    }

    [TestMethod]
    public void ALongCampaignsFullReportFitsInAboutFortyLines()
    {
        Arrange(() =>
        {
            string[] names = ["Ada Pc", "Bram One", "Cora Two", "Dax Three", "Eli Four", "Fen Five"];
            Write(_root, "character-sheets/pc-ada.md", Sheet(names[0]));
            foreach (string n in names.Skip(1))
                Write(_root, $"character-sheets/npc-{n.Split(' ')[0].ToLowerInvariant()}.md", Sheet(n));
            Write(_root, "game-state.md", GameState(names.Skip(1).Take(4).ToArray(), present: "Fen"));
            Write(_root, "story/sessions/session-039.md", LogOf(5));
            Write(_root, "story/dm-notes.md", "# Notes\n\n## Mix tracker\n\n- s1\n\n## Opposition\n\n| Goblins | 0 / 4 | x | x | x |\n\n## Open\n\n- the mill: DONE\n" + Filler(Budgets.DmNotes * 4 + 400, lineLength: 500));
        });
        string[] lines = Size().StdoutLines;
        Assert.IsLessThanOrEqualTo(40, lines.Length, string.Join("\n", lines));
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Size("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.StartsWith(result.Stdout, "usage: dmc size [NAME] [--check] [--folder PATH]");
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("size ", StringComparison.Ordinal)
                                                  && l.Contains("what a resume reads, in estimated tokens")));
    }
}

/// <summary>The size tests against the library code, in this process.</summary>
[TestClass]
public sealed class SizeInProcessTests : SizeCommandContract
{
    /// <summary>Runs <c>dmc size</c> in this process.</summary>
    protected override ProcessResult Size(params string[] args) => DmcRunner.InProcess(["size", .. args]);
}

/// <summary>The size tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class SizeBlackBoxTests : SizeCommandContract
{
    /// <summary>Runs <c>dmc size</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Size(params string[] args) => DmcRunner.BlackBox(["size", .. args]);
}
