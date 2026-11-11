using System.Text;
using Dmc.Retrieval;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// The code under <c>dmc recall</c> (<see cref="Recall"/>, <see cref="RecallScope"/>, <see cref="LineUnits"/>), called
/// in this process on the fixture chain: the scope and what it leaves out, line units, ranking by weighted relevance,
/// labels, the trim, the cap and the notices (research R1 to R6, R11).
/// </summary>
[TestClass]
public sealed class RecallLibraryTests
{
    /// <summary>The test's DMC folder, with the fixture chain.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = NewChain();

    /// <summary>Deletes it.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>Recalls in the fixture's sequel campaign.</summary>
    /// <param name="query">The words.</param>
    List<string> Run(string query) => Recall.Run(_root, Sequel, query);

    /// <summary>The index of the first output line that contains <paramref name="text"/>, or -1.</summary>
    /// <param name="lines">The output.</param>
    /// <param name="text">What to look for.</param>
    static int IndexOf(List<string> lines, string text) => lines.FindIndex(l => l.Contains(text, StringComparison.Ordinal));

    [TestMethod]
    public void ALineOnlyThePredecessorHoldsIsFoundAndLabelledWithIt()
    {
        var lines = Run("giant eel free crossing");
        Assert.AreEqual($"{Predecessor} story/canon-archive.md:3 s12.3 | - Oswin owes the party a free crossing for the giant eel they killed under his raft (s12.3).", lines[0]);
    }

    [TestMethod]
    public void TheModuleOfThePredecessorIsSearchedAndItsLinesAreMarkedDm()
    {
        var lines = Run("reed hut stilts");
        Assert.AreEqual($"modules/{Module} chapters/01-the-ford.md:5 DM | A reed hut on stilts; the ferryman's tally of crossings is cut into its door.", lines[0]);
    }

    [TestMethod]
    public void ImportsExportsBackupsDotFoldersSourceAndMapsAreNeverSearched()
    {
        AddNoise(_root);
        var lines = Run("Oswin ferryman ferry cart price charges crossing Hedda smith axle");
        Assert.IsFalse(lines.Any(l => l.Contains("NOISE")), string.Join('\n', lines));
        Assert.IsGreaterThan(0, lines.Count);
    }

    [TestMethod]
    public void ShortLinesHeadingsCommentsTableRowsAndFencesAreNotUnits()
    {
        Write(_root, $"adventures/{Sequel}/story/summary.md",
            "# Barnacle heading\n\nBarnacle short.\n<!-- barnacle barnacle barnacle barnacle comment -->\n|---|---|\n" +
            "```\nbarnacle barnacle barnacle barnacle in a fence\n```\n| barnacle | a table row that is long enough |\n");
        var lines = Run("barnacle");
        CollectionAssert.AreEqual(new[] { $"{Sequel} story/summary.md:9 | | barnacle | a table row that is long enough |" }, lines);
    }

    [TestMethod]
    public void OfTwoLinesThatMatchAlikeCanonComesBeforeTheSessionLog()
    {
        Write(_root, $"adventures/{Sequel}/story/canon.md", "# Canon\n\n- The lantern on the ferry post is lit at dusk every evening.\n");
        Write(_root, $"adventures/{Sequel}/story/sessions/session-032.md", "# Session 32\n\n### Scene 1: Dusk\n\nThe lantern on the ferry post is lit at dusk every evening.\n");
        var lines = Run("lantern dusk evening");
        Assert.IsLessThan(IndexOf(lines, "session-032.md"), IndexOf(lines, "story/canon.md"));
    }

    [TestMethod]
    public void AStrongSessionLogMatchStillBeatsAWeakCanonOne()
    {
        Write(_root, $"adventures/{Sequel}/story/canon.md", "# Canon\n\n- Barges pay double at the green lantern post (s30.1).\n");
        Write(_root, $"adventures/{Sequel}/story/sessions/session-032.md",
            "# Session 32\n\n### Scene 2: The signal\n\nOswin lights the green lantern at dusk to signal the barges downriver.\n");
        var lines = Run("green lantern dusk signal downriver");
        Assert.AreEqual(0, IndexOf(lines, "session-032.md:5 s32.2"));
    }

    [TestMethod]
    public void TheSameLineRanksBelowInThePredecessorAndLowestInTheModule()
    {
        const string line = "- The ford stones are slick with moss after the spring rain.";
        Write(_root, $"adventures/{Sequel}/story/summary.md", "# Summary\n\n" + line + "\n");
        Write(_root, $"adventures/{Predecessor}/story/summary.md", "# Summary\n\n" + line + "\n");
        Write(_root, $"modules/{Module}/chapters/02-stones.md", "# Stones\n\n" + line + "\n");
        var lines = Run("ford stones moss spring rain");
        int own = IndexOf(lines, $"{Sequel} story/summary.md"), earlier = IndexOf(lines, $"{Predecessor} story/summary.md"), module = IndexOf(lines, "02-stones.md");
        Assert.IsTrue(own >= 0 && own < earlier && earlier < module, string.Join('\n', lines));
    }

    [TestMethod]
    public void ASessionLogLineCarriesItsSceneAndOtherLinesTheirCitation()
    {
        var lines = Run("Oswin tally hut door raft cart");
        Assert.IsTrue(lines.Contains($"{Sequel} story/sessions/session-031.md:9 s31.4 | Oswin taps the tally on the hut door and waves the cart aboard the raft."), string.Join('\n', lines));
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Sequel} story/canon.md:9 s31.4 | ", StringComparison.Ordinal)), string.Join('\n', lines));
    }

    [TestMethod]
    public void DmNotesAndAnNpcSheetsSecretsAreMarkedDmButItsHistoryIsNot()
    {
        var lines = Run("Oswin smugglers lookout ledger boat night");
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Sequel} story/dm-notes.md:5 DM | ", StringComparison.Ordinal)), string.Join('\n', lines));
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Sequel} character-sheets/npc-oswin.md:10 DM | ", StringComparison.Ordinal)), string.Join('\n', lines));
        var history = Run("met party usual fare");
        Assert.IsTrue(history.Any(l => l.StartsWith($"{Sequel} character-sheets/npc-oswin.md:14 s31.4 | ", StringComparison.Ordinal)), string.Join('\n', history));
    }

    [TestMethod]
    public void ALongLineIsCutAroundItsMatch()
    {
        string filler = string.Join(' ', Enumerable.Repeat("reeds", 250));
        Write(_root, $"adventures/{Sequel}/story/canon.md", $"# Canon\n\n- {filler} the smugglers hide their kegs under the jetty {filler}\n");
        var lines = Run("smugglers kegs jetty");
        string hit = lines.Single(l => l.Contains("story/canon.md"));
        StringAssert.Contains(hit, "| …smugglers hide their kegs under the jetty");
        StringAssert.EndsWith(hit, "…");
        Assert.IsLessThan(800, hit.Length);
    }

    [TestMethod]
    public void TheOutputStaysWithinItsCapAndCountsTheRest()
    {
        var log = new StringBuilder("# Session 40\n\n### Scene 1: Barnacles\n\n");
        for (int i = 0; i < 200; i++)
            log.Append($"Line {i}: the barnacles on the raft's underside were scraped off again, a long and tedious job that took the morning.\n");
        Write(_root, $"adventures/{Sequel}/story/sessions/session-040.md", log.ToString());

        var lines = Run("barnacles");
        int bytes = lines.Sum(l => Encoding.UTF8.GetByteCount(l) + 1);
        Assert.IsLessThanOrEqualTo(Recall.CapBytes, bytes);
        int shown = lines.Count - 1;
        Assert.AreEqual($"({200 - shown} more lines matched)", lines[^1]);
    }

    [TestMethod]
    public void NothingMatchedIsOneLine()
    {
        CollectionAssert.AreEqual(new[] { "nothing matched: zebra quagga" }, Run("zebra quagga"));
    }

    [TestMethod]
    public void OnlyCommonWordsFail()
    {
        var e = Assert.ThrowsExactly<DmcException>(() => Run("the and of"));
        Assert.AreEqual("nothing to search for in \"the and of\": every word is a common one", e.Message);
    }

    [TestMethod]
    public void APredecessorWrittenWithItsFolderAndSlashesIsFollowed()
    {
        Write(_root, $"adventures/{Sequel}/player-options.md", "# Options\n\n- **Predecessor**: `adventures/" + Predecessor + "/`\n");
        Assert.AreEqual(0, IndexOf(Run("giant eel free crossing"), Predecessor));
    }

    [TestMethod]
    public void AMissingPredecessorIsANoticeAndTheRestIsStillSearched()
    {
        Write(_root, $"adventures/{Sequel}/player-options.md", "# Options\n\n- **Predecessor**: gone-road\n");
        var lines = Run("Oswin ferryman charges cart");
        Assert.AreEqual($"{Sequel}: Predecessor gone-road is not in adventures/, not searched", lines[0]);
        Assert.IsTrue(lines.Skip(1).Any(l => l.Contains("story/canon.md")));
    }

    [TestMethod]
    public void ALoopInTheChainIsFollowedOnce()
    {
        Write(_root, $"adventures/{Predecessor}/player-options.md", "# Options\n\n- **Predecessor**: " + Sequel + "\n");
        var lines = Run("giant eel free crossing");
        Assert.AreEqual(1, lines.Count(l => l.Contains("canon-archive.md:3 s12.3")), string.Join('\n', lines));
    }

    [TestMethod]
    public void AModuleBothCampaignsNameIsSearchedOnceAtTheNearestRank()
    {
        Write(_root, $"adventures/{Sequel}/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/" + Module + "/`\n");
        var lines = Run("reed hut stilts");
        Assert.AreEqual(1, lines.Count(l => l.Contains("01-the-ford.md:5")), string.Join('\n', lines));
    }

    [TestMethod]
    public void AMissingModuleFolderIsANotice()
    {
        Write(_root, $"adventures/{Predecessor}/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/sunken-keep/`\n");
        var lines = Run("Oswin ferryman charges cart");
        Assert.AreEqual($"{Predecessor}: module folder modules/sunken-keep/ not found, not searched", lines[0]);
    }

    [TestMethod]
    public void TheOrderIsTheSameOnEveryRun()
    {
        CollectionAssert.AreEqual(Run("Oswin ferry raft cart crossing"), Run("Oswin ferry raft cart crossing"));
    }

    [TestMethod]
    public void TiersFollowTheFilesPaths()
    {
        Assert.AreEqual(Tier.Canon, RecallScope.TierOf("story/canon.md"));
        Assert.AreEqual(Tier.Archive, RecallScope.TierOf("story/dm-notes-archive.md"));
        Assert.AreEqual(Tier.Archive, RecallScope.TierOf("character-sheets/npc-oswin-history.md"));
        Assert.AreEqual(Tier.Sheet, RecallScope.TierOf("character-sheets/minor-npcs.md"));
        Assert.AreEqual(Tier.Session, RecallScope.TierOf("story/sessions/session-001.md"));
        Assert.AreEqual(Tier.Story, RecallScope.TierOf("maps/fen.md"));
        Assert.IsGreaterThan(RecallScope.WeightOf(Tier.Session), RecallScope.WeightOf(Tier.Canon));
        Assert.IsGreaterThan(RecallScope.WeightOf(Tier.Module), RecallScope.WeightOf(Tier.Session));
    }
}
