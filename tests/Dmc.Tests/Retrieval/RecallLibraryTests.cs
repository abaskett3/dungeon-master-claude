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
        Assert.AreEqual($"{Predecessor} (continues) story/canon-archive.md:3 s12.3 | - Oswin owes the party a free crossing for the giant eel they killed under his raft (s12.3).", lines[0]);
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
    public void TextInsideHtmlCommentsIsNeverRanked()
    {
        Write(_root, $"adventures/{Sequel}/game-state.md",
            "# Game state\n\n<!--\n- Troll (an example): AC 15, HP 84, speed 30 ft, regains 10 HP a turn\n-->\n" +
            "- **Last full save**: never <!-- a troll example in an inline comment that is long enough -->\n");
        Write(_root, $"adventures/{Sequel}/story/sessions/session-033.md",
            "# Session 33\n\n### Scene 1: The bridge\n\nA troll under the old toll bridge demands a silver piece from every traveller.\n");
        var lines = Run("troll");
        CollectionAssert.AreEqual(new[] { $"{Sequel} story/sessions/session-033.md:5 s33.1 | A troll under the old toll bridge demands a silver piece from every traveller." }, lines);
    }

    [TestMethod]
    public void QuotationMarksAndPluralPossessivesDoNotHideAWord()
    {
        Write(_root, $"adventures/{Sequel}/story/canon.md", "# Canon\n\n- The order is called 'Silver Hand' and meets in the Knights' hall at dusk (s5.2).\n");
        var lines = Run("silver hand knights");
        StringAssert.StartsWith(lines[0], $"{Sequel} story/canon.md:3 s5.2 | ");
    }

    [TestMethod]
    public void OfTwoLinesThatMatchAlikeCanonComesBeforeTheSessionLog()
    {
        // Lines that differ only in a word the query does not hold match alike; identical lines would print once (spec 011 R5).
        Write(_root, $"adventures/{Sequel}/story/canon.md", "# Canon\n\n- The lantern on the ferry post is lit at dusk every evening by Oswin.\n");
        Write(_root, $"adventures/{Sequel}/story/sessions/session-032.md", "# Session 32\n\n### Scene 1: Dusk\n\nThe lantern on the ferry post is lit at dusk every evening by Hedda.\n");
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
    public void ALikeLineRanksBelowInThePredecessorAndLowestInTheModule()
    {
        // Lines that differ only in a word the query does not hold match alike; identical lines would print once (R5).
        const string line = "- The ford stones are slick with moss after the spring rain, said ";
        Write(_root, $"adventures/{Sequel}/story/summary.md", "# Summary\n\n" + line + "Oswin.\n");
        Write(_root, $"adventures/{Predecessor}/story/summary.md", "# Summary\n\n" + line + "Hedda.\n");
        Write(_root, $"modules/{Module}/chapters/02-stones.md", "# Stones\n\n" + line + "Tobin.\n");
        var lines = Run("ford stones moss spring rain");
        int own = IndexOf(lines, $"{Sequel} story/summary.md"), earlier = IndexOf(lines, $"{Predecessor} (continues) story/summary.md"), module = IndexOf(lines, "02-stones.md");
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
    public void AMissingCampaignOnTheContinuesLineIsANotice()
    {
        Write(_root, $"adventures/{Sequel}/player-options.md", "# Options\n\n" + Continuity("gone-road"));
        var lines = Run("Oswin ferryman charges cart");
        Assert.AreEqual($"{Sequel}: Continues gone-road is not in adventures/, not searched", lines[0]);
    }

    [TestMethod]
    public void TheSameLineInTwoPlacesIsPrintedOnceAndTheOtherCounted()
    {
        const string line = "- The ford stones are slick with moss after the spring rain (s30.3).";
        Write(_root, $"adventures/{Sequel}/story/summary.md", "# Summary\n\n" + line + "\n");
        Write(_root, $"adventures/{Predecessor}/story/summary.md", "# Summary\n\n" + line + "\n");
        var lines = Run("slick stones moss spring rain");
        CollectionAssert.AreEqual(new[] { $"{Sequel} story/summary.md:3 s30.3 | " + line, "(1 more lines matched)" }, lines);
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
    public void AModuleLineWithACommentABackslashOrAnotherCaseIsStillFollowed()
    {
        foreach (string value in new[] { $"`modules/{Module}/` <!-- ingested s3 -->", $"modules\\{Module}\\", $"Modules/{Module}/" })
        {
            Write(_root, $"adventures/{Predecessor}/module-progress.md", "# Module progress\n\n- **Module folder**: " + value + "\n");
            Assert.AreEqual(0, IndexOf(Run("reed hut stilts"), $"modules/{Module}"), value);
        }
    }

    [TestMethod]
    public void AStagingFolderIsNotAModule()
    {
        Write(_root, $"modules/.partial-{Module}/index.md", "# Index\n\n- A reed hut on stilts in the staging copy, long enough to rank.\n");
        Write(_root, $"adventures/{Predecessor}/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/.partial-" + Module + "/`\n");
        Assert.IsFalse(Run("reed hut stilts").Any(l => l.Contains(".partial-")));
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
        Assert.AreEqual(Tier.Digest, RecallScope.TierOf("digests/old-road/people.md"));
        Assert.AreEqual(Tier.Story, RecallScope.TierOf("digests/notes.md"));
        Assert.IsGreaterThan(RecallScope.WeightOf(Tier.Session), RecallScope.WeightOf(Tier.Canon));
        Assert.IsGreaterThan(RecallScope.WeightOf(Tier.Module), RecallScope.WeightOf(Tier.Session));
    }
}

/// <summary>
/// <c>dmc recall</c> over spec 011's linked set (<see cref="RetrievalFixtures.NewLinkedSet"/>): digests found first and
/// labelled, a line printed once, the ranking of own canon, digest and digested campaign, side and same-world labels,
/// DM-only secrets, missing folders, the old Predecessor line, and recall in a closed campaign (FR-017 to FR-018,
/// research R4 to R6).
/// </summary>
[TestClass]
public sealed class RecallLinkedSetTests
{
    /// <summary>The test's DMC folder, with the linked set.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = NewLinkedSet();

    /// <summary>Deletes it.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>Recalls in a campaign of the set.</summary>
    /// <param name="query">The words.</param>
    /// <param name="campaign">The campaign; the last of the chain by default.</param>
    List<string> Run(string query, string campaign = Last) => Recall.Run(_root, campaign, query);

    /// <summary>The index of the first output line that starts with <paramref name="start"/>, or -1.</summary>
    /// <param name="lines">The output.</param>
    /// <param name="start">What the line starts with.</param>
    static int IndexOf(List<string> lines, string start) => lines.FindIndex(l => l.StartsWith(start, StringComparison.Ordinal));

    /// <summary>The output as one string, for a failure message.</summary>
    /// <param name="lines">The output.</param>
    static string Show(List<string> lines) => string.Join('\n', lines);

    [TestMethod]
    public void AFactOnlyTheFirstCampaignHoldsComesFromTheDigestLabelledContinues()
    {
        var lines = Run("miller Tobin abbey bell millpond raiders");
        Assert.AreEqual($"{Last} digests/{First}/places.md:5 (continues) s4.2 | {FirstLine}", lines[0]);
        Assert.AreEqual(1, lines.Count(l => l.Contains("millpond")), Show(lines));
    }

    [TestMethod]
    public void ALineInTheMiddleCanonAndItsDigestIsPrintedOnceFromTheDigest()
    {
        var lines = Run("Sister Wren ferry tolls iron box chapel altar");
        Assert.AreEqual($"{Last} digests/{Middle}/places.md:5 (continues) s20.1 | {MiddleLine}", lines[0]);
        Assert.AreEqual(1, lines.Count(l => l.Contains("iron box")), Show(lines));
        StringAssert.Matches(lines[^1], new System.Text.RegularExpressions.Regex(@"^\(\d+ more lines matched\)$"));
    }

    [TestMethod]
    public void OwnCanonRanksAboveTheDigestWhichRanksAboveTheDigestedCampaignsCanon()
    {
        var lines = Run("east gate Harrowby barred sunset");
        Assert.AreEqual(0, IndexOf(lines, $"{Last} story/canon.md:5 s40.1 | "), Show(lines));
        Assert.AreEqual(1, IndexOf(lines, $"{Last} digests/{Middle}/world.md:5 (continues) s20.4 | "), Show(lines));
        Assert.AreEqual(2, IndexOf(lines, $"{Middle} (continues) story/canon.md:6 s20.4 | "), Show(lines));
    }

    [TestMethod]
    public void SideAndSameWorldHitsAreLabelledAndTheCampaignsOwnAreNot()
    {
        Assert.AreEqual(0, IndexOf(Run("Thornwick bridge burned eclipse"), $"{SideTale} (side) story/canon.md:3 s3.1 | "));
        Assert.AreEqual(0, IndexOf(Run("Greywater mine flooded valley copper prices"), $"{FarTale} (world) story/canon.md:3 s7.1 | "));
        Assert.AreEqual(0, IndexOf(Run("reeve copper chain office smiles"), $"{Last} story/canon.md:6 s41.2 | "));
    }

    [TestMethod]
    public void EveryLineOfADigestsSecretsIsDmOnly()
    {
        var lines = Run("abbot sold chapel relic smugglers gambling debts");
        Assert.AreEqual(0, IndexOf(lines, $"{Last} digests/{Middle}/secrets.md:5 (continues) s22.3 DM | "), Show(lines));
    }

    [TestMethod]
    public void AMissingFolderWhoseDigestIsHeldIsSearchedThroughTheDigestWithNoNotice()
    {
        Directory.Delete(Path.Combine(_root, "adventures", Middle), recursive: true);
        var lines = Run("Sister Wren ferry tolls iron box chapel altar");
        Assert.AreEqual($"{Last} digests/{Middle}/places.md:5 (continues) s20.1 | {MiddleLine}", lines[0]);
        // The first campaign is out of reach now, but its digest still counts as an earlier campaign's.
        Assert.AreEqual($"{Last} digests/{First}/places.md:5 (continues) s4.2 | {FirstLine}", Run("miller Tobin abbey bell millpond raiders")[0]);

        Directory.Delete(Path.Combine(_root, "adventures", Last, "digests", Middle), recursive: true);
        Assert.AreEqual($"{Last}: Continues {Middle} is not in adventures/, not searched", Run("Sister Wren ferry tolls iron box")[0]);
    }

    [TestMethod]
    public void AClosedCampaignWithOnlyAPredecessorLineIsStillFollowed()
    {
        Write(_root, $"adventures/{Middle}/player-options.md", "# Campaign options\n\n- **Predecessor**: " + First + "\n");
        Write(_root, $"adventures/{First}/story/sessions/session-005.md",
            "# Session 5\n\n### Scene 1: The well\n\nThe old well behind the mill has been dry since the earthquake.\n");
        var lines = Run("old well mill dry earthquake");
        Assert.AreEqual(0, IndexOf(lines, $"{First} (continues) story/sessions/session-005.md:5 s5.1 | "), Show(lines));
    }

    [TestMethod]
    public void RecallingInTheClosedFirstCampaignLabelsTheLaterOnesSequel()
    {
        var reeve = Run("reeve copper chain office smiles", First);
        Assert.AreEqual(0, IndexOf(reeve, $"{Last} (sequel) story/canon.md:6 s41.2 | "), Show(reeve));
        var wren = Run("Sister Wren ferry tolls iron box chapel altar", First);
        Assert.AreEqual(0, IndexOf(wren, $"{Middle} (sequel) story/canon.md:5 s20.1 | "), Show(wren));
    }

    /// <summary>Makes <c>modules/moors</c>, a module some campaigns of the set name, with one chapter line.</summary>
    /// <param name="line">The chapter's line.</param>
    /// <param name="campaigns">The campaigns whose module progress names it.</param>
    void Moors(string line, params string[] campaigns)
    {
        Write(_root, "modules/moors/index.md", "# The Moors\n");
        Write(_root, "modules/moors/chapters/01-crossroads.md", "# Chapter 1: The crossroads\n\n" + line + "\n");
        foreach (string campaign in campaigns)
            Write(_root, $"adventures/{campaign}/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/moors/`\n");
    }

    [TestMethod]
    public void AModuleReachedOnlyThroughTheSideCampaignIsSearchedAndItsHitsCarryNoLabel()
    {
        Moors("The standing stones on the moor hum at midnight when the wind is from the north.", SideTale);
        var lines = Run("standing stones moor hum midnight");
        Assert.AreEqual(0, IndexOf(lines, "modules/moors chapters/01-crossroads.md:3 DM | The standing stones"), Show(lines));
    }

    [TestMethod]
    public void AModuleTwoCampaignsOfTheSetNameIsSearchedOnce()
    {
        Moors("The standing stones on the moor hum at midnight when the wind is from the north.", SideTale, First);
        var lines = Run("standing stones moor hum midnight");
        Assert.AreEqual(1, lines.Count(l => l.Contains("01-crossroads.md:3")), Show(lines));
    }

    [TestMethod]
    public void ALinkedCampaignsCanonRanksAboveTheModuleAsPrinted()
    {
        // The party burned an inn the module describes standing (US5 scenario 2): canon wins over the module as printed.
        Moors("The Crooked Lantern inn stands at the crossroads, two storeys of grey stone with a slate roof.", Middle);
        Write(_root, $"adventures/{Middle}/story/canon.md", "# Canon\n\n## Places\n\n" + MiddleLine + "\n" +
            "- The party burned the Crooked Lantern inn at the crossroads to the ground (s21.4).\n");
        var lines = Run("Crooked Lantern inn crossroads");
        int canon = IndexOf(lines, $"{Middle} (continues) story/canon.md:6 s21.4 | "), module = IndexOf(lines, "modules/moors chapters/01-crossroads.md:3 DM | ");
        Assert.IsTrue(canon >= 0 && canon < module, Show(lines));
    }
}
