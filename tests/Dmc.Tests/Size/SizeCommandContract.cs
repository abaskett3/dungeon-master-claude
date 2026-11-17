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

    /// <summary>
    /// <c>--check</c> prints the link notices after the flags, each, and the <c>(+N more)</c> line, ending <c>(not the
    /// full save's to trim)</c>.
    /// </summary>
    [TestMethod]
    public void TheCheckFormPrintsTheLinkNoticesAfterTheFlagsMarkedAsNotTheFullSaves()
    {
        Arrange(() =>
        {
            Write(_root, "player-options.md", "# Player options\n\n## Rules\n\n- **Edition**: 5e 2014\n\n## Continuity\n\n" +
                "- **Continues**: gone\n- **Side campaign of**: lost\n- **Shares a world with**: far, away\n");
            Write(_root, "game-state.md", GameState([]) + Filler(Budgets.GameState * 4));
        });
        var result = Size("--check");

        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            "game-state.md 4.1k is over its 4k budget",
            $"notice: {Name}: Continues gone is not in adventures/ (not the full save's to trim)",
            $"notice: {Name}: Side campaign of lost is not in adventures/ (not the full save's to trim)",
            $"notice: {Name}: Shares a world with far is not in adventures/ (not the full save's to trim)",
            "(+1 more) (not the full save's to trim)",
        }, result.StdoutLines);
    }

    // ---------- spec 012: the module: line ----------

    /// <summary>Makes the campaign a module campaign on a module with <paramref name="problems"/> repeated headings in one file, or none.</summary>
    /// <param name="problems">How many problems the module has.</param>
    /// <param name="finished">True to mark every chapter complete.</param>
    void MakeModuleCampaign(int problems, bool finished = false)
    {
        Write(_root, "module-progress.md", "# Module progress\n\n- **Module folder**: `modules/marsh/`\n\n## Current position\n\n"
            + "- **Digest to read before continuing**: chapters/01-gate.md § 1. Gate\n\n## Chapter status\n\n"
            + (finished ? "- 1. The Gate: complete\n" : "- 1. The Gate: in progress\n"));
        string module = Path.Combine(_root, "modules", "marsh");
        WriteAt(module, "index.md", "# Marsh\n\n- 1. chapters/01-gate.md\n");
        WriteAt(module, "chapters/01-gate.md", "# The Gate\n\n## 1. Gate\n\nA gate.\n" + string.Concat(Enumerable.Repeat("\n## 2. Yard\n\nA yard.\n", problems + 1)));
    }

    [TestMethod]
    public void TheCheckFormPrintsOneModuleLineAfterTheFlagsAndBeforeTheLinkNotices()
    {
        Arrange(() =>
        {
            MakeModuleCampaign(problems: 2);
            Write(_root, "game-state.md", GameState([]) + Filler(Budgets.GameState * 4));
            Write(_root, "player-options.md", "# Player options\n\n## Continuity\n\n- **Continues**: gone\n");
        });
        CollectionAssert.AreEqual(new[]
        {
            "game-state.md 4.1k is over its 4k budget",
            "module: modules/marsh has 2 problems: run dmc module check marsh (not the full save's to trim)",
            $"notice: {Name}: Continues gone is not in adventures/ (not the full save's to trim)",
        }, Size("--check").StdoutLines);
    }

    [TestMethod]
    public void OneModuleProblemIsSingularAndTheFullReportPrintsTheLineToo()
    {
        Arrange(() => MakeModuleCampaign(problems: 1));
        CollectionAssert.AreEqual(new[] { "module: modules/marsh has 1 problem: run dmc module check marsh (not the full save's to trim)" },
            Size("--check").StdoutLines);
        var report = Size().StdoutLines;
        CollectionAssert.Contains(report, "  module: modules/marsh has 1 problem: run dmc module check marsh (not the full save's to trim)");
        CollectionAssert.DoesNotContain(report, "  nothing to flag");
    }

    /// <summary>
    /// SC-007: a reference file over the mark gives exactly one module line, and once it is split by hand into two files
    /// beside it, each under the mark, none. A homebrew campaign is <see cref="TheCheckFormPrintsNothingWhenAllIsWell"/>.
    /// </summary>
    [TestMethod]
    public void AnOversizedReferenceFileGivesOneModuleLineAndNoneOnceSplit()
    {
        static string Entries(int from, int to) =>
            string.Concat(Enumerable.Range(from, to - from + 1).Select(n => $"## Beast {n} (PDF {n})\n\n{Filler(2000)}"));
        string module = Path.Combine(_root, "modules", "marsh");
        Arrange(() =>
        {
            MakeModuleCampaign(problems: 0);
            WriteAt(module, "monsters.md", "# Monsters\n\n" + Entries(1, 45));
        });
        CollectionAssert.AreEqual(new[] { "module: modules/marsh has 1 problem: run dmc module check marsh (not the full save's to trim)" },
            Size("--check").StdoutLines);
        Assert.AreEqual(_before, Hash(_root), "the first run wrote nothing either");

        Arrange(() =>
        {
            File.Delete(Path.Combine(module, "monsters.md"));
            WriteAt(module, "monsters-a-m.md", "# Monsters A to M\n\n" + Entries(1, 23));
            WriteAt(module, "monsters-n-z.md", "# Monsters N to Z\n\n" + Entries(24, 45));
        });
        Assert.AreEqual("", Size("--check").Stdout, "split into two files under the mark");
    }

    [TestMethod]
    public void ASoundAFinishedAndAMissingModulePrintNoModuleLine()
    {
        Arrange(() => MakeModuleCampaign(problems: 0));
        Assert.AreEqual("", Size("--check").Stdout, "a sound module");
        Arrange(() => MakeModuleCampaign(problems: 3, finished: true));
        Assert.AreEqual("", Size("--check").Stdout, "a finished module");
        Arrange(() =>
        {
            MakeModuleCampaign(problems: 3);
            DmcFolders.Delete(Path.Combine(_root, "modules", "marsh"));
        });
        Assert.IsFalse(Size("--check").StdoutLines.Any(l => l.StartsWith("module:", StringComparison.Ordinal)), "a module not ingested");
    }

    /// <summary>
    /// A broken link in a closed campaign's file, which can no longer be changed, is not named at resume; the open
    /// campaign's own broken link still is.
    /// </summary>
    [TestMethod]
    public void TheCheckFormLeavesOutTheNoticesOfAClosedCampaignsLines()
    {
        Arrange(() =>
        {
            Write(_root, "player-options.md", "# Player options\n\n## Continuity\n\n- **Continues**: old\n- **Side campaign of**: none\n" +
                "- **Shares a world with**: lost\n");
            string old = NewCampaign(_root, "old");
            WriteAt(old, "player-options.md", "# Player options\n\n- **Predecessor**: gone\n");
            Dmc.Campaign.ClosedCampaign.Write(old, new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), "old_closed_20261111090000.zip");
        });
        var result = Size("--check", Name);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            $"notice: {Name}: Shares a world with lost is not in adventures/ (not the full save's to trim)",
        }, result.StdoutLines);
    }

    /// <summary>
    /// <c>--check</c> of a campaign whose links are all good and whose linked campaigns are all closed prints nothing
    /// (SC-007): no notice, and no legacy line, since no open campaign reads its legacy.
    /// </summary>
    [TestMethod]
    public void TheCheckFormOfACampaignWithLinksAndNoProblemPrintsNothing()
    {
        Arrange(() =>
        {
            Write(_root, "player-options.md", "# Player options\n\n## Continuity\n\n- **Continues**: none\n- **Side campaign of**: none\n" +
                "- **Shares a world with**: other\n");
            string other = NewCampaign(_root, "other");
            Dmc.Campaign.ClosedCampaign.Write(other, new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), "other_closed_20261111090000.zip");
        });
        var result = Size("--check", Name);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    /// <summary>
    /// <c>--check</c> prints the legacy lines after the notices, uncapped (contracts/campaign-links.md): a linked
    /// campaign's legacy this one has not folded, then <c>legacy due</c>, since the linked campaign is open.
    /// </summary>
    [TestMethod]
    public void TheCheckFormPrintsTheLegacyLinesAfterTheNotices()
    {
        Arrange(() =>
        {
            Write(_root, "player-options.md", "# Player options\n\n## Continuity\n\n- **Continues**: gone\n- **Side campaign of**: none\n" +
                "- **Shares a world with**: side\n");
            Write(_root, "game-state.md", GameState([]).Replace("- **Last full save**: never\n", "- **Last full save**: never\n- **Legacies folded**: side s2\n"));
            string side = NewCampaign(_root, "side");
            WriteAt(side, "legacy.md", "# Legacy of side\n\n- **Written**: session 3, 2026-11-13\n\n## What changed in the world\n\n- The bridge burned (s3.2).\n");
            WriteAt(side, "game-state.md", GameState([]).Replace("- **Last full save**: never\n", $"- **Last full save**: never\n- **Legacies folded**: {Name} s7\n"));
        });
        var result = Size("--check", Name);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            $"notice: {Name}: Continues gone is not in adventures/ (not the full save's to trim)",
            "legacy: read adventures/side/legacy.md (written session 3; record side s3) (not the full save's to trim)",
            "legacy due: at the full save, update this campaign\'s legacy.md for open linked campaigns side: add this save's changes and drop the lines from session 7 and earlier (not the full save's to trim)",
        }, result.StdoutLines);
    }

    /// <summary>
    /// Issue #202's shape: an open campaign whose sequel has written a legacy is not told to read it, since the sequel
    /// comes after its story; it is still told to write its own for the sequel to read.
    /// </summary>
    [TestMethod]
    public void TheCheckFormNamesNoLegacyOfASequelToTheCampaignItContinues()
    {
        Arrange(() =>
        {
            Write(_root, "player-options.md", "# Player options\n\n## Continuity\n\n- **Continues**: none\n- **Side campaign of**: none\n" +
                "- **Shares a world with**: none\n");
            string sequel = NewCampaign(_root, "sequel");
            WriteAt(sequel, "player-options.md", $"# Player options\n\n## Continuity\n\n- **Continues**: {Name}\n- **Side campaign of**: none\n" +
                "- **Shares a world with**: none\n");
            WriteAt(sequel, "legacy.md", "# Legacy of sequel\n\n- **Written**: session 53, 2026-11-13\n\n## What changed in the world\n\n- The ford is bridged (s53.1).\n");
        });
        var result = Size("--check", Name);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            "legacy due: at the full save, update this campaign\'s legacy.md for open linked campaigns sequel: add this save's changes and keep every line the budget allows (not the full save's to trim)",
        }, result.StdoutLines);
    }

    /// <summary><c>--check</c> of a campaign alone with a legacy file of its own prints nothing (SC-007).</summary>
    [TestMethod]
    public void TheCheckFormOfACampaignAloneWithALegacyPrintsNothing()
    {
        Arrange(() => Write(_root, "legacy.md", "# Legacy of x\n\n- **Written**: session 3, 2026-11-13\n\n## What changed in the world\n"));
        var result = Size("--check", Name);
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
            "story/canon.md 19.2k is over its 19k limit: a full save archives it down to 16k (move about 3.2k)",
            "game-state.md 4.1k is over its 4k budget",
        }, result.StdoutLines);
    }

    /// <summary>The full report shows canon read by scene (spec 013), with no note that a search is left uncounted, and still lists canon's own sections.</summary>
    [TestMethod]
    public void TheFullReportShowsCanonReadBySceneAndListsItsSections()
    {
        Arrange(() => Write(_root, "story/canon.md", "# Canon\n\n## Rulings\n\n- one\n\n## Timeline\n\n" + Filler(Budgets.CanonLimit * 4 + 400)));
        string[] lines = Size().StdoutLines;
        int canon = Array.FindIndex(lines, l => l.TrimStart().StartsWith("story/canon.md", StringComparison.Ordinal));
        StringAssert.Contains(lines[canon], "read in sections and by scene");
        Assert.DoesNotContain("is not counted", string.Join("\n", lines));
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

    /// <summary>
    /// Spec 015, from the code review of PR #228: the largest-sections line lists the Arc plan by its content, the same
    /// figure its budget flag uses, so the template's comment never makes the two disagree.
    /// </summary>
    [TestMethod]
    public void TheArcPlanIsListedByItsContentAsItsBudgetCountsIt()
    {
        Arrange(() => Write(_root, "story/dm-notes.md",
            "# Notes\n\n## Arc plan\n\n<!--\n" + Filler(4000) + "\n-->\n" + Filler(800) + "## B\n\n" + Filler(2000)));
        string line = Size().StdoutLines.Single(l => l.StartsWith("  story/dm-notes.md: ", StringComparison.Ordinal));
        StringAssert.StartsWith(line, "  story/dm-notes.md: B 501, Arc plan 200", $"the comment's 1k left out, so B comes first: {line}");
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
        StringAssert.StartsWith(result.Stdout, "usage: dmc size [NAME] [--check | --arc] [--folder PATH]");
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
