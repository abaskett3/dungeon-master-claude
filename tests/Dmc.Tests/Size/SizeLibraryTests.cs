using System.Text;
using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.Size;

/// <summary>
/// The code under <c>dmc size</c>, called in this process on fixture campaigns: the estimate and the sections
/// (<see cref="Measurement"/>), which files a resume reads and which part of each (<see cref="ReadSet"/>, against the
/// rules of <c>dmc-resume-campaign</c> step 2), canon's limit, and every flag, each on a fixture built to trigger it
/// and on one built not to.
/// </summary>
[TestClass]
public sealed class SizeLibraryTests
{
    /// <summary>The test's DMC folder, with a fresh fixture campaign.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder and its campaign.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewDmcFolderWithCampaign();
        // A campaign as dmc-new-campaign leaves it: no placeholder Party status block.
        Write(_root, "game-state.md", GameState([]));
    }

    /// <summary>Deletes the DMC folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>The fixture campaign, measured.</summary>
    SizeResult Measure() => SizeReport.Measure(_root, Name);

    /// <summary>The paths of the read set, in order.</summary>
    string[] Paths() => [.. Measure().Parts.Select(p => p.Path)];

    /// <summary>The part with this path; fails when there is none.</summary>
    /// <param name="path">The path the report shows.</param>
    Part PartOf(string path) => Measure().Parts.SingleOrDefault(p => p.Path == path)
                                ?? throw new AssertFailedException($"{path} is not in the read set: {string.Join(", ", Paths())}");

    /// <summary>The flags of the fixture campaign of one kind.</summary>
    /// <param name="kind">The kind.</param>
    Flag[] FlagsOf(FlagKind kind) => [.. Measure().Flags.Where(f => f.Kind == kind)];

    // ---------- the estimate and the sections ----------

    /// <summary>A sequel's digests are searched, never read at resume, so they add nothing to the resume total (spec 011 SC-001, FR-020a).</summary>
    [TestMethod]
    public void ADigestFolderAddsNothingToTheResumeTotal()
    {
        long before = Measure().Total;
        string[] paths = Paths();
        foreach (string file in new[] { "index.md", "people.md", "places.md", "world.md", "spine.md", "secrets.md" })
            Write(_root, $"digests/old-tale/{file}", "# Digest\n\n## Subject\n\n" + Filler(20_000));
        Assert.AreEqual(before, Measure().Total);
        CollectionAssert.AreEqual(paths, Paths());
    }

    /// <summary>A part's tokens are 2 for each whole 5 of its characters (spec 021), and one Read of it costs 160 more.</summary>
    [TestMethod]
    public void TokensAreTwoForEachWholeFiveCharacters()
    {
        var lines = Measurement.Split("abcdef\n"u8.ToArray());
        var part = new Part("x", PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, null, lines);
        Assert.AreEqual(7, part.Bytes);
        Assert.AreEqual(7, part.Characters);
        Assert.AreEqual(2, part.Tokens);
        Assert.AreEqual(162, part.ReadCost);
    }

    [TestMethod]
    public void ACrlfFileCountsItsCarriageReturnsAndMatchesWithoutThem()
    {
        var lines = Measurement.Split("ab\r\ncd\r\n"u8.ToArray());
        Assert.HasCount(2, lines);
        Assert.AreEqual("ab", lines[0].Text);
        Assert.AreEqual(8, lines.Sum(l => l.Bytes));
    }

    [TestMethod]
    public void AccentedTextCountsItsBytesNotItsCharacters()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("Château\n");
        Assert.AreEqual(9, Measurement.Split(bytes).Sum(l => l.Bytes));
    }

    [TestMethod]
    public void AByteOrderMarkCountsButIsNotPartOfTheFirstLine()
    {
        var lines = Measurement.Split([0xEF, 0xBB, 0xBF, (byte)'#', (byte)'\n']);
        Assert.AreEqual("#", lines[0].Text);
        Assert.AreEqual(5, lines[0].Bytes);
    }

    [TestMethod]
    public void SectionsAreCutAtEachLevelTwoHeadingWithTheTextBeforeAsTop()
    {
        var sections = Measurement.Sections(Measurement.Split("# T\nintro\n## A\nx\n### sub\ny\n## B\n"u8.ToArray()));
        CollectionAssert.AreEqual(new[] { "(top)", "A", "B" }, sections.Select(s => s.Heading).ToArray());
        Assert.AreEqual("# T\nintro\n".Length, sections[0].Bytes);
        Assert.AreEqual("## A\nx\n### sub\ny\n".Length, sections[1].Bytes);
    }

    [TestMethod]
    public void TokenCountsAreWrittenInThousandsRoundedUp()
    {
        Assert.AreEqual("0", Measurement.FormatTokens(0));
        Assert.AreEqual("812", Measurement.FormatTokens(812));
        Assert.AreEqual("1k", Measurement.FormatTokens(1000));
        Assert.AreEqual("4.1k", Measurement.FormatTokens(4001));
        Assert.AreEqual("6.7k", Measurement.FormatTokens(6640));
        Assert.AreEqual("19k", Measurement.FormatTokens(19000));
        Assert.AreEqual("20.5k", Measurement.FormatTokens(20448));
    }

    /// <summary>
    /// Every budget a full save trims to is under the near-cap mark, so no file inside its budget is cut at resume. Canon
    /// is the exception by design: within its limit it is read whole, in two reads when it must be, and never flagged near
    /// the cap (spec 021 research R5).
    /// </summary>
    [TestMethod]
    public void EveryBudgetSitsWellUnderTheReadCap()
    {
        int[] budgets = [Budgets.GameState, Budgets.DmNotes, Budgets.Summary, Budgets.PcSheet, Budgets.OtherSheet, Budgets.Legacy];
        foreach (int budget in budgets)
            Assert.IsLessThan(Budgets.NearCapTokens, budget, "every budget is under the near-cap mark, so no file is paged at resume");
        Assert.IsGreaterThan(Budgets.NearCapTokens, Budgets.CanonLimit, "canon's limit is past the mark, which is why canon is left out of the flag");
        Assert.IsLessThan(Budgets.CanonLimit, Budgets.CanonArchiveMark);
        Assert.IsLessThan(Budgets.ReadCapTokens, Budgets.NearCapTokens);
    }

    // ---------- which files, in which order ----------

    [TestMethod]
    public void ANewCampaignListsTheResumeFilesInItsOrderAndNothingElse()
    {
        CollectionAssert.AreEqual(new[]
        {
            "adventures/player-options.md", "player-options.md", "game-state.md", "story/summary.md", "story/dm-notes.md",
            "story/sessions/", "story/canon.md",
        }, Paths());
        Assert.AreEqual(EntryState.Missing, PartOf("story/sessions/").State, "a new campaign has no session log yet");
        Assert.IsEmpty(Measure().Flags, "a new campaign has nothing to flag: " + string.Join("; ", Measure().Flags.Select(f => f.Line)));
    }

    /// <summary>
    /// The total is what the resume pays to read the parts, each one's text and its calls; a part's tokens are 2 for each
    /// whole 5 of its characters, a CR and a byte-order mark left out (spec 021 FR-002, FR-006).
    /// </summary>
    [TestMethod]
    public void TheTotalIsTheReadCostOfThePartsAndEachIsTwoTokensForFiveCharacters()
    {
        var result = Measure();
        Assert.AreEqual(result.Parts.Where(p => p.InTotal).Sum(p => p.ReadCost), result.Total);
        Assert.AreEqual(result.Parts.Where(p => p.InTotal).Sum(p => p.Calls), result.Calls);
        string summary = Path.Combine(CampaignFolder(_root), "story", "summary.md");
        int characters = File.ReadAllText(summary).TrimStart('\uFEFF').Replace("\r", "").Length;
        Assert.AreEqual(2L * (characters / 5), PartOf("story/summary.md").Tokens);
    }

    [TestMethod]
    public void AMissingFileIsListedAndCountsNothing()
    {
        File.Delete(Path.Combine(CampaignFolder(_root), "story", "dm-notes.md"));
        var notes = PartOf("story/dm-notes.md");
        Assert.AreEqual(EntryState.Missing, notes.State);
        Assert.AreEqual(0, notes.Tokens);
    }

    [TestMethod]
    public void ArchivesOlderLogsMinorNpcsMovedHistoryAndMapsAreNeverListed()
    {
        Write(_root, "story/canon-archive.md", "# Archive\n");
        Write(_root, "story/dm-notes-archive.md", "# Archive\n");
        Write(_root, "story/sessions/session-001.md", LogOf(1));
        Write(_root, "story/sessions/session-002.md", LogOf(1));
        Write(_root, "character-sheets/minor-npcs.md", "# Minor NPCs\n\n- Bob, a farmer\n");
        Write(_root, "maps/live.json", "{}");
        Write(_root, "character-sheets/npc-kibil-rornson.md", Sheet("Kibil Rornson"));
        Write(_root, "character-sheets/npc-kibil-rornson-history.md", "# Kibil Rornson: history\n\n- s1: met\n");
        Write(_root, "game-state.md", GameState(["Kibil Rornson"], present: "Kibil"));
        string[] paths = Paths();
        CollectionAssert.DoesNotContain(paths, "story/canon-archive.md");
        CollectionAssert.DoesNotContain(paths, "story/dm-notes-archive.md");
        CollectionAssert.DoesNotContain(paths, "story/sessions/session-001.md");
        CollectionAssert.Contains(paths, "story/sessions/session-002.md");
        Assert.IsFalse(paths.Any(p => p.Contains("minor-npcs") || p.Contains("-history.md") || p.StartsWith("maps/", StringComparison.Ordinal)));
        CollectionAssert.Contains(paths, "character-sheets/npc-kibil-rornson.md");
    }

    // ---------- the sheets ----------

    [TestMethod]
    public void EveryPcAndEachCompanionAndEachNpcPresentIsReadAndNoOtherSheet()
    {
        Write(_root, "character-sheets/pc-shale.md", Sheet("Shalefire Stoutheart"));
        Write(_root, "character-sheets/npc-kibil-rornson.md", Sheet("Kibil Rornson"));
        Write(_root, "character-sheets/npc-dessa-birch.md", Sheet("Dessa Birch"));
        Write(_root, "character-sheets/npc-goblin-boss.md", Sheet("Goblin Boss"));
        Write(_root, "character-sheets/npc-tisk.md", Sheet("Tisk"));
        Write(_root, "character-sheets/npc-troll.md", Sheet("Troll"));
        Write(_root, "character-sheets/npc-faraway.md", Sheet("Mira Faraway"));
        Write(_root, "game-state.md", GameState(["Shalefire Stoutheart", "Kibil"], present: "Shale, Kibil and Dessa in the kitchen",
            tracker: "| 15 | Goblin Boss | 14 | 20 |", notCompanions: "Tisk, a guest (HP 4)"));

        string[] sheets = [.. Paths().Where(p => p.StartsWith("character-sheets/", StringComparison.Ordinal))];
        CollectionAssert.AreEqual(new[]
        {
            "character-sheets/pc-shale.md", "character-sheets/npc-kibil-rornson.md",
            "character-sheets/npc-dessa-birch.md", "character-sheets/npc-goblin-boss.md",
        }, sheets, "PCs, then companions, then those present; Tisk (not a companion, not present), the Troll (only in a comment) and Mira (absent) are not read");
    }

    [TestMethod]
    public void ACompanionIsMatchedByTheFirstOrLastWordOfTheSheetsTitleIgnoringCase()
    {
        Assert.IsTrue(ReadSet.Mentions("Kibil", "Kibil Rornson"));
        Assert.IsTrue(ReadSet.Mentions("at anselm's shrine", "Brother Anselm"));
        Assert.IsFalse(ReadSet.Mentions("Kibilson", "Kibil Rornson"), "a whole word only");
        Assert.IsFalse(ReadSet.Mentions("an ox", "Ox Lee"), "words under three letters are not tried");
        Assert.IsFalse(ReadSet.Mentions("Brann, the innkeeper", "The Grey Lady"), "an article is not a name");
        Assert.IsFalse(ReadSet.Mentions("Captain Mira at the gate", "Captain Rhys of the Watch"), "nor is a title");
        Assert.IsTrue(ReadSet.Mentions("the Grey Lady waits", "The Grey Lady"), "the whole title still matches");
    }

    [TestMethod]
    public void ACompanionIsNotMatchedToAnotherSheetThroughATitleTheyShare()
    {
        Write(_root, "character-sheets/npc-ivy.md", Sheet("Sister Ivy"));
        Write(_root, "character-sheets/npc-maren.md", Sheet("Maren, cleric of Lathander"));
        Write(_root, "game-state.md", GameState(["Sister Maren"]));
        string[] sheets = [.. Paths().Where(p => p.StartsWith("character-sheets/", StringComparison.Ordinal))];
        CollectionAssert.AreEqual(new[] { "character-sheets/npc-maren.md" }, sheets);
    }

    /// <summary>A lone word matches only at the block name's edge, so a treasure named after a family names no one (seen on Rorn's Landing, 2026-11-15).</summary>
    [TestMethod]
    public void ACompanionIsMatchedByAWordOnlyAtTheBlockNamesStartOrEnd()
    {
        string[] titles = ["Warden Brunna Kelderak", "Captain Emmajeen Kole", "Kibil Rornson"];
        Assert.AreEqual(-1, ReadSet.MatchCompanion("The Kelderak treasure (s48.3)", titles), "a word inside the name");
        Assert.AreEqual(1, ReadSet.MatchCompanion("Captain Kole", titles), "the last word");
        Assert.AreEqual(2, ReadSet.MatchCompanion("Kibil (s12)", titles), "the trailing bracket is left out");
        Assert.AreEqual(0, ReadSet.MatchCompanion("Brunna the warden of Kelderak", titles), "the first word");
    }

    [TestMethod]
    public void ASheetThatIsBothCompanionAndPresentIsListedOnce()
    {
        Write(_root, "character-sheets/npc-pip.md", Sheet("Pip Vance"));
        Write(_root, "game-state.md", GameState(["Pip Vance"], present: "Pip"));
        Assert.AreEqual(1, Paths().Count(p => p == "character-sheets/npc-pip.md"));
    }

    [TestMethod]
    public void ASheetIsReadDownToItsHistoryWhenTheHistoryIsLast()
    {
        foreach (string heading in new[] { "## History", "## Notes", "## History with the PC" })
        {
            Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: 1000, history: heading));
            var part = PartOf("character-sheets/pc-a.md");
            Assert.AreEqual(PartKind.ToHistory, part.Kind, heading);
            Assert.AreEqual(1000, part.Bytes, heading);
        }
    }

    [TestMethod]
    public void ASheetWithItsHistoryInTheMiddleIsReadWhole()
    {
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: 1000, historyFirst: true));
        var part = PartOf("character-sheets/pc-a.md");
        Assert.AreEqual(PartKind.Whole, part.Kind);
        Assert.AreEqual(new FileInfo(Path.Combine(CampaignFolder(_root), "character-sheets", "pc-a.md")).Length, part.Bytes);
    }

    [TestMethod]
    public void ACompanionWithNoSheetIsListedAsNotFound()
    {
        Write(_root, "game-state.md", GameState(["Nobody Here"]));
        Assert.AreEqual(EntryState.NotFound, PartOf("character-sheets: Nobody Here").State);
    }

    [TestMethod]
    public void AGameStateWithoutPartyStatusOrPresentSaysTheSheetsCouldNotBeTold()
    {
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada"));
        Write(_root, "game-state.md", "# Game state\n\n- **Last full save**: never\n\n## Pending\n\n- none\n");
        string[] paths = Paths();
        CollectionAssert.Contains(paths, "character-sheets/pc-a.md", "the PC sheets are still counted");
        var note = PartOf("character-sheets: companions and NPCs present");
        Assert.AreEqual(EntryState.NotFound, note.State);
        StringAssert.Contains(note.Note, "no Party status or Present line");
    }

    // ---------- the session log ----------

    [TestMethod]
    public void TheLatestLogIsChosenByItsNumber()
    {
        Write(_root, "story/sessions/session-009.md", LogOf(1));
        Write(_root, "story/sessions/session-010.md", LogOf(1));
        CollectionAssert.Contains(Paths(), "story/sessions/session-010.md");
        CollectionAssert.DoesNotContain(Paths(), "story/sessions/session-009.md");
    }

    [TestMethod]
    public void ALogWithFewerThanThreeScenesIsReadWhole()
    {
        Write(_root, "story/sessions/session-003.md", LogOf(2));
        Write(_root, "game-state.md", GameState([], lastFullSave: "2026-11-09 18:40, session 3, through scene 2"));
        Assert.AreEqual(PartKind.Whole, PartOf("story/sessions/session-003.md").Kind);
    }

    [TestMethod]
    public void ACoveredLogIsReadFromItsThirdLastSceneAndTheCommentedExampleIsNotAScene()
    {
        Write(_root, "story/sessions/session-003.md", LogOf(5, sceneBytes: 300));
        Write(_root, "game-state.md", GameState([], lastFullSave: "2026-11-09 18:40, session 3, through scene 5"));
        var part = PartOf("story/sessions/session-003.md");
        Assert.AreEqual(PartKind.Tail, part.Kind);
        Assert.AreEqual(900, part.Bytes, "scenes 3, 4 and 5");
        Assert.AreEqual("from scene 3", part.Note);
    }

    [TestMethod]
    public void ALogTheLastFullSaveDoesNotCoverIsReadWhole()
    {
        Write(_root, "story/sessions/session-003.md", LogOf(5));
        foreach (string stamp in new[] { "never", "2026-11-09 18:40, session 3", "2026-11-09 18:40, session 2, through scene 9", "2026-11-09 18:40, session 3, through scene 4" })
        {
            Write(_root, "game-state.md", GameState([], lastFullSave: stamp));
            Assert.AreEqual(PartKind.Whole, PartOf("story/sessions/session-003.md").Kind, stamp);
        }
    }

    // ---------- canon ----------

    /// <summary>A canon of about <paramref name="bytes"/> bytes: a Rulings, a Prices and an "Always read" section of 1,000 bytes each, and a Timeline filling the rest.</summary>
    /// <param name="bytes">The canon's size.</param>
    /// <param name="lineLength">The size of each Timeline line.</param>
    static string CanonOf(int bytes, int lineLength = 80)
    {
        string fixedPart = "# Canon\n\n## Rulings\n\n" + Filler(1000) + "## Prices, debts and promises\n\n" + Filler(1000)
                           + "## Cast\n\n<!-- Always read. -->\n" + Filler(1000) + "## Timeline\n\n";
        return fixedPart + Filler(bytes - Encoding.UTF8.GetByteCount(fixedPart), lineLength);
    }

    /// <summary>Canon under the whole mark is read whole; at it, its always-read sections and what the scene read prints, which is what the resume reads (spec 013 FR-007, FR-009, SC-004). Since the canon rollback (FR-026) the mark is the archive limit, so canon at it is also flagged for the full save.</summary>
    [TestMethod]
    public void CanonUnderTheWholeMarkIsReadWholeAndAtItByItsAlwaysReadSectionsAndTheScene()
    {
        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonWholeBelow) - 5));
        Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind);

        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonWholeBelow)));
        var canon = PartOf("story/canon.md");
        Assert.AreEqual(PartKind.ByScene, canon.Kind);
        CollectionAssert.AreEqual(new[] { "Rulings", "Prices, debts and promises", "Cast" }, canon.Sections.Select(s => s.Heading).ToArray(),
            "the two always-read sections and the one whose comment says Always read; the Timeline is read by scene");
        long scene = global::Dmc.Retrieval.SceneRead.Run(_root, Name).Sum(l => (long)global::Dmc.Retrieval.Recall.Characters(l));
        Assert.IsGreaterThan(0L, scene);
        Assert.AreEqual(canon.Lines.Sum(l => (long)l.Characters) + scene, canon.Characters, "the sections plus the characters the scene read prints");
        Assert.AreEqual(2, canon.Calls, "the sections and the scene read's command");
        Assert.HasCount(1, FlagsOf(FlagKind.CanonOverLimit), "the mark is the archive limit (FR-026)");
    }

    /// <summary>The scene read's bytes are its real output: a scene that matches Timeline lines adds them (spec 013 SC-004).</summary>
    [TestMethod]
    public void CanonReadByScenePaysForTheLinesTheSceneMatches()
    {
        string canon = CanonOf(TokenSizes.Chars(Budgets.CanonWholeBelow)) + "- The ferryman Oswin charges three silver a head at the Marrow ford (s3.2).\n";
        Write(_root, "story/canon.md", canon);
        long before = PartOf("story/canon.md").Characters;
        Write(_root, "game-state.md", GameState([]).Replace("- **Location**: a road", "- **Location**: the Marrow ford, Oswin's hut"));
        Assert.IsGreaterThan(before, PartOf("story/canon.md").Characters, "the matching line is read and counted");
    }

    /// <summary>Two canons of about the same line count are read by their size, not their lines: short lines whole, long lines past the mark by scene.</summary>
    [TestMethod]
    public void TwoCanonsOfTheSameLineCountAreTreatedByTheirSize()
    {
        Write(_root, "story/canon.md", CanonOf(20000, lineLength: 160));
        Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind, "5k of short lines");
        Write(_root, "story/canon.md", CanonOf(80000, lineLength: 640));
        Assert.AreEqual(PartKind.ByScene, PartOf("story/canon.md").Kind, "20k of long lines");
    }

    /// <summary>The long campaign's canon (15.5k, about 17k once fight stories reach it), read whole before spec 013 and by scene in it, is read whole again since the canon rollback (FR-026).</summary>
    [TestMethod]
    public void TheLongCampaignsCanonIsReadWholeAgainSinceTheCanonRollback()
    {
        foreach (int tokens in new[] { 15500, 17000 })
        {
            Write(_root, "story/canon.md", CanonOf(tokens * 4));
            Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind, $"{tokens} tokens");
            Assert.IsEmpty(FlagsOf(FlagKind.CanonOverLimit), $"{tokens} tokens is under the archive limit");
        }
    }

    /// <summary>Canon over the archive limit is still flagged for the full save, read by scene as below it.</summary>
    [TestMethod]
    public void CanonOverItsLimitIsFlaggedForTheArchiveAndStillReadByScene()
    {
        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonLimit)));
        Assert.AreEqual(PartKind.ByScene, PartOf("story/canon.md").Kind);
        Assert.HasCount(1, FlagsOf(FlagKind.CanonOverLimit));
    }

    /// <summary>Canon read by scene counts its long lines across the whole file, since the scene read may print any of them.</summary>
    [TestMethod]
    public void CanonReadBySceneCountsLongLinesInTheWholeFile()
    {
        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonWholeBelow)) + new string('b', 500) + "\n");
        StringAssert.Contains(FlagsOf(FlagKind.LongLines).Single().Line, "story/canon.md: 1 line over 400 characters");
    }

    // ---------- play cards (spec 013) ----------

    /// <summary>A sheet with a play card first: a title, a top field, the card of <paramref name="cardBytes"/>, a Playing them section and a history.</summary>
    /// <param name="title">The character's name.</param>
    /// <param name="cardBytes">The card's filler.</param>
    static string CardSheet(string title, int cardBytes = 600) =>
        $"# {title}\n\n- **Role in the story**: a friend\n\n## Play card\n\n" + Filler(cardBytes)
        + "## Playing them\n\n" + Filler(1000) + "## History\n\n" + Filler(400);

    [TestMethod]
    public void ACompanionWithAPlayCardIsReadThroughItAgainstItsBudget()
    {
        Write(_root, "game-state.md", GameState(["Kibil Rornson"]));
        string sheet = CardSheet("Kibil Rornson");
        Write(_root, "character-sheets/npc-kibil-rornson.md", sheet);
        var part = PartOf("character-sheets/npc-kibil-rornson.md");
        Assert.AreEqual(PartKind.ToPlayCard, part.Kind);
        Assert.AreEqual(BudgetKind.PlayCard, part.Budget);
        Assert.AreEqual("to its play card", part.Note);
        Assert.AreEqual(sheet.IndexOf("## Playing them", StringComparison.Ordinal), part.Bytes, "from the top to the heading after the card");
        Assert.IsEmpty(FlagsOf(FlagKind.NoPlayCard));
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget));

        Write(_root, "character-sheets/npc-kibil-rornson.md", CardSheet("Kibil Rornson", cardBytes: TokenSizes.Chars(Budgets.PlayCard)));
        StringAssert.Contains(FlagsOf(FlagKind.OverBudget).Single().Line, "is over its 1.6k budget");
    }

    [TestMethod]
    public void ACompanionWithNoPlayCardIsReadToItsHistoryAndFlaggedForTheFullSave()
    {
        Write(_root, "game-state.md", GameState(["Kibil Rornson"]));
        Write(_root, "character-sheets/npc-kibil-rornson.md", Sheet("Kibil Rornson"));
        Assert.AreEqual(PartKind.ToHistory, PartOf("character-sheets/npc-kibil-rornson.md").Kind);
        var flag = FlagsOf(FlagKind.NoPlayCard).Single();
        Assert.AreEqual("character-sheets/npc-kibil-rornson.md: a companion with no play card: the full save writes one", flag.Line);
        Assert.IsTrue(flag.HoldsTheSave);
    }

    [TestMethod]
    public void ThePcAndAnNpcPresentAreReadToTheirHistoryEvenWithAPlayCard()
    {
        Write(_root, "game-state.md", GameState(["Ada Stone"], present: "Dessa Birch"));
        Write(_root, "character-sheets/pc-ada-stone.md", CardSheet("Ada Stone"));
        Write(_root, "character-sheets/npc-dessa-birch.md", CardSheet("Dessa Birch"));
        Assert.AreEqual(PartKind.ToHistory, PartOf("character-sheets/pc-ada-stone.md").Kind, "the PC's sheet is read as before (FR-006a)");
        Assert.AreEqual(PartKind.ToHistory, PartOf("character-sheets/npc-dessa-birch.md").Kind, "cards are for companions only (FR-006a)");
        Assert.IsEmpty(FlagsOf(FlagKind.NoPlayCard), "neither is a companion");
    }

    // ---------- the flags ----------

    /// <summary>A <c>dm-notes.md</c> with the template's section names and the given text in each.</summary>
    static string Notes(string arcPlan = "", string opposition = "", string about = "", string extra = "") =>
        "# DM notes\n\n## Arc plan\n\n" + arcPlan + "\n## Opposition\n\n| Who | How many (alive / started) | Where now | Stat block | The party knows |\n|---|---|---|---|---|\n"
        + opposition + "\n## Open threads\n\n" + extra + "\n## About this player\n\n" + about;

    /// <summary>A <c>dm-notes.md</c> of exactly <paramref name="bytes"/> bytes, the filler in its last section.</summary>
    static string NotesOfSize(int bytes)
    {
        string head = Notes();
        return head + Filler(bytes - Encoding.UTF8.GetByteCount(head));
    }

    /// <summary>A <c>dm-notes.md</c> of exactly <paramref name="lines"/> lines, the extra ones two bytes each.</summary>
    static string NotesOfLines(int lines)
    {
        string head = Notes();
        int headLines = head.Count(c => c == '\n');
        return head + Filler((lines - headLines) * 2, lineLength: 2);
    }

    /// <summary>The near-cap flag compares what one Read of the part costs, its text and the call, with the 18,000 mark (spec 021 FR-003).</summary>
    [TestMethod]
    public void AFileNearTheReadCapIsFlaggedFromEighteenThousandTokensOfReadCostOrSixteenHundredLines()
    {
        Write(_root, "story/dm-notes.md", NotesOfSize(TokenSizes.ReadChars(Budgets.NearCapTokens) - 1));
        Assert.IsEmpty(FlagsOf(FlagKind.NearCap), "17,998 tokens of Read cost");
        Write(_root, "story/dm-notes.md", NotesOfSize(TokenSizes.ReadChars(Budgets.NearCapTokens)));
        Assert.AreEqual("story/dm-notes.md 18k is near the Read cap (25k): it may be cut short at resume", FlagsOf(FlagKind.NearCap).Single().Line,
            "a file the save trims carries no 'not the save's' ending");

        Write(_root, "story/dm-notes.md", NotesOfLines(Budgets.NearCapLines - 1));
        Assert.IsEmpty(FlagsOf(FlagKind.NearCap), "1,599 lines");
        Write(_root, "story/dm-notes.md", NotesOfLines(Budgets.NearCapLines));
        Assert.HasCount(1, FlagsOf(FlagKind.NearCap), "1,600 lines");
    }

    [TestMethod]
    public void AFileWithoutABudgetNearTheCapIsFlaggedButDoesNotHoldTheSave()
    {
        Write(_root, "story/sessions/session-001.md", LogOf(1, sceneBytes: TokenSizes.ReadChars(Budgets.NearCapTokens) + 100));
        var flag = FlagsOf(FlagKind.NearCap).Single();
        Assert.AreEqual("story/sessions/session-001.md", flag.Path);
        Assert.IsFalse(flag.HoldsTheSave);
        StringAssert.EndsWith(flag.Line, Flags.NotTheSaves, "the full save is told to leave a file it never trims");
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget), "a session log has no budget");
    }

    /// <summary>
    /// Canon within its limit is read whole past the near-cap mark, in two reads by line range when the Read tool says it
    /// is too large, so it is not flagged near the cap: the flag would send the full save to trim canon below its limit
    /// (spec 021 research R5). The table shows no near-cap note for it either.
    /// </summary>
    [TestMethod]
    public void CanonWithinItsLimitIsNotFlaggedNearTheCap()
    {
        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonLimit) - 5));
        var canon = PartOf("story/canon.md");
        Assert.AreEqual(PartKind.Whole, canon.Kind);
        Assert.IsGreaterThan(Budgets.NearCapTokens, canon.ReadCost, "past the near-cap mark");
        Assert.IsEmpty(FlagsOf(FlagKind.NearCap));
        Assert.IsEmpty(FlagsOf(FlagKind.CanonOverLimit));
        string row = SizeReport.FullReport(Measure()).Single(l => l.TrimStart().StartsWith("story/canon.md ", StringComparison.Ordinal));
        Assert.DoesNotContain("near the Read cap", row);
    }

    [TestMethod]
    public void AFileOverItsBudgetIsFlaggedAndOneAtItIsNot()
    {
        Write(_root, "game-state.md", Filler(TokenSizes.Chars(Budgets.GameState)));
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget), "exactly at the budget");
        Write(_root, "game-state.md", Filler(TokenSizes.Chars(Budgets.GameState) + 5));
        Assert.AreEqual("game-state.md 6.5k is over its 6.4k budget", FlagsOf(FlagKind.OverBudget).Single().Line);
    }

    /// <summary>
    /// A campaign's own <c>legacy.md</c> is measured against its 4.8k budget and flagged over it, for the full save to trim,
    /// but its own resume never reads it, so it is never part of the resume total (spec 011 FR-025, FR-020a).
    /// </summary>
    [TestMethod]
    public void ALegacyIsFlaggedOverItsBudgetAndNeverCountsInTheTotal()
    {
        long before = Measure().Total;
        Write(_root, "legacy.md", Filler(TokenSizes.Chars(Budgets.Legacy)));
        Assert.AreEqual(BudgetKind.Legacy, PartOf("legacy.md").Budget);
        Assert.AreEqual(Budgets.Legacy, PartOf("legacy.md").Tokens);
        Assert.IsFalse(PartOf("legacy.md").InTotal);
        Assert.AreEqual(before, Measure().Total, "the legacy is read by other campaigns, never by this one's resume");
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget), "exactly at the budget");
        Write(_root, "legacy.md", Filler(TokenSizes.Chars(Budgets.Legacy) + 5));
        var flag = FlagsOf(FlagKind.OverBudget).Single();
        Assert.AreEqual("legacy.md 4.9k is over its 4.8k budget", flag.Line);
        Assert.IsTrue(flag.HoldsTheSave);
        Assert.AreEqual(before, Measure().Total);
    }

    /// <summary>
    /// A <c>legacy.md</c> past the near-cap mark gets no near-cap flag, which would tell the resume a file it reads may be
    /// cut short: its own resume never reads it, and its budget flag already holds the save.
    /// </summary>
    [TestMethod]
    public void ALegacyIsNeverFlaggedNearTheReadCap()
    {
        Write(_root, "legacy.md", Filler(TokenSizes.Chars(Budgets.NearCapTokens) + 5));
        Assert.IsFalse(FlagsOf(FlagKind.NearCap).Any(f => f.Path == "legacy.md"));
        Assert.AreEqual("legacy.md", FlagsOf(FlagKind.OverBudget).Single().Path);
    }

    /// <summary>A campaign with no <c>legacy.md</c> lists none.</summary>
    [TestMethod]
    public void ACampaignWithNoLegacyListsNone() => Assert.DoesNotContain("legacy.md", Paths());

    [TestMethod]
    public void APcSheetHasALargerBudgetThanOtherSheets()
    {
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: TokenSizes.Chars(Budgets.OtherSheet) + 400));
        Write(_root, "character-sheets/npc-b.md", Sheet("Bram", bodyBytes: TokenSizes.Chars(Budgets.OtherSheet) + 400));
        Write(_root, "game-state.md", GameState(["Bram"]));
        CollectionAssert.AreEqual(new[] { "character-sheets/npc-b.md" }, FlagsOf(FlagKind.OverBudget).Select(f => f.Path).ToArray());
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: TokenSizes.Chars(Budgets.PcSheet) + 400));
        CollectionAssert.Contains(FlagsOf(FlagKind.OverBudget).Select(f => f.Path).ToArray(), "character-sheets/pc-a.md");
    }

    [TestMethod]
    public void ThePlayerOptionsHaveNoBudget()
    {
        File.WriteAllText(Path.Combine(_root, "adventures", "player-options.md"), Filler(TokenSizes.Chars(Budgets.OtherSheet) * 3));
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget));
    }

    [TestMethod]
    public void ASectionOfTheNotesOverItsOwnBudgetIsFlagged()
    {
        Write(_root, "story/dm-notes.md", Notes(about: Filler(TokenSizes.Chars(Budgets.DmNotesAboutThePlayer) - 40), arcPlan: Filler(TokenSizes.Chars(Budgets.DmNotesArcPlan) - 40)));
        Assert.IsEmpty(FlagsOf(FlagKind.SectionOverBudget));
        Write(_root, "story/dm-notes.md", Notes(about: Filler(TokenSizes.Chars(Budgets.DmNotesAboutThePlayer) + 400), arcPlan: Filler(TokenSizes.Chars(Budgets.DmNotesArcPlan) + 400)));
        CollectionAssert.AreEqual(new[] { "story/dm-notes.md Arc plan: 2.6k, over its 2.4k budget", "story/dm-notes.md About this player: 3.4k, over its 3.2k budget" },
            FlagsOf(FlagKind.SectionOverBudget).Select(f => f.Line).ToArray());
    }

    /// <summary>
    /// Spec 015: the Arc plan's budget counts its own content, not the template's comment, so a full save is never pushed
    /// to delete the guidance to fit; the comment still counts toward the file's total.
    /// </summary>
    [TestMethod]
    public void TheArcPlansBudgetLeavesItsCommentOut()
    {
        string comment = "<!--\n" + Filler(TokenSizes.Chars(Budgets.DmNotesArcPlan)) + "\n-->\n";
        Write(_root, "story/dm-notes.md", Notes(arcPlan: comment + Filler(TokenSizes.Chars(Budgets.DmNotesArcPlan) - 400)));
        Assert.IsEmpty(FlagsOf(FlagKind.SectionOverBudget), "a large comment and a plan under budget");
        Write(_root, "story/dm-notes.md", Notes(arcPlan: comment + Filler(TokenSizes.Chars(Budgets.DmNotesArcPlan) + 400)));
        StringAssert.StartsWith(FlagsOf(FlagKind.SectionOverBudget).Single().Line, "story/dm-notes.md Arc plan: 2.6k");
    }

    [TestMethod]
    public void ALogOfLastPlayedLinesOrAMixTrackerIsFlaggedAndOneLastPlayedLineIsNot()
    {
        Write(_root, "story/dm-notes.md", Notes(arcPlan: "- Stage 1: done s4\n\nLast played (as of s12): fight s10.3\n"));
        Assert.IsEmpty(FlagsOf(FlagKind.LastPlayedLog));
        Write(_root, "story/dm-notes.md", Notes(arcPlan: "Last played (as of s11): fight s10.3\n- **Last played (as of s12)**: fight s10.3\n"));
        Assert.AreEqual("story/dm-notes.md: 2 Last played lines, where one belongs", FlagsOf(FlagKind.LastPlayedLog).Single().Line);
        Write(_root, "story/dm-notes.md", Notes() + "\n## Mix tracker\n\n- s1: a fight is due\n");
        StringAssert.Contains(FlagsOf(FlagKind.LastPlayedLog).Single().Line, "Mix tracker: a log of sessions");
    }

    [TestMethod]
    public void OppositionRowsWithNoneLeftAliveAreCounted()
    {
        Write(_root, "story/dm-notes.md", Notes(opposition: "| Goblins | 0 / 4 | dead | - | all |\n| Wolves | 0/3 | dead | - | all |\n| Hag | 1 / 1 | fen | - | name |\n"));
        Assert.AreEqual("story/dm-notes.md Opposition: 2 rows with none left alive", FlagsOf(FlagKind.FinishedOpposition).Single().Line);
        Write(_root, "story/dm-notes.md", Notes(opposition: "| Hag | 1 / 1 | fen | - | name |\n"));
        Assert.IsEmpty(FlagsOf(FlagKind.FinishedOpposition));
    }

    [TestMethod]
    public void DoneLinesAreFlaggedOutsideTheArcPlanOnly()
    {
        Write(_root, "story/dm-notes.md", Notes(arcPlan: "- Stage 1: DONE s28, the hag dead\n", extra: "- the mill plan is done\n"));
        Assert.IsEmpty(FlagsOf(FlagKind.DoneLines), "a done stage in the Arc plan, and lowercase prose");
        Write(_root, "story/dm-notes.md", Notes(extra: "- the mill: DONE\n- the debt: RESOLVED s12\n"));
        Assert.AreEqual("story/dm-notes.md: 2 lines marked DONE or RESOLVED", FlagsOf(FlagKind.DoneLines).Single().Line);
    }

    [TestMethod]
    public void ALargePricesSectionOfCanonIsFlagged()
    {
        Write(_root, "story/canon.md", "# Canon\n\n## Prices, debts and promises\n\n" + Filler(TokenSizes.Chars(Budgets.CanonPricesSection) - 200));
        Assert.IsEmpty(FlagsOf(FlagKind.PricesSection));
        Write(_root, "story/canon.md", "# Canon\n\n## Prices, debts and promises\n\n" + Filler(TokenSizes.Chars(Budgets.CanonPricesSection) + 400));
        StringAssert.Contains(FlagsOf(FlagKind.PricesSection).Single().Line, "which suggests paid debts or kept promises not deleted");
    }

    [TestMethod]
    public void LongLinesAreCountedInTheContentOfTrimmedFilesOnly()
    {
        string line400 = new('x', Budgets.LongLineCharacters), line401 = new('x', Budgets.LongLineCharacters + 1);
        Write(_root, "story/summary.md", $"# Summary\n\n{line401}\n\n## Recent\n\n{line400}\n<!--\n{line401}\n-->\n");
        Assert.IsEmpty(FlagsOf(FlagKind.LongLines), "the preamble and comments are the template's guidance, and 400 characters is allowed");
        Write(_root, "story/summary.md", $"# Summary\n\n## Recent\n\n{line401}\n{line401}\n");
        Assert.AreEqual("story/summary.md: 2 lines over 400 characters", FlagsOf(FlagKind.LongLines).Single().Line);
        Write(_root, "character-sheets/pc-a.md", "# Ada\n\n## About\n\n" + line401 + "\n\n## History\n\n" + line401 + "\n");
        Assert.AreEqual("character-sheets/pc-a.md: 1 line over 400 characters", FlagsOf(FlagKind.LongLines).Single(f => f.Path == "character-sheets/pc-a.md").Line,
            "a sheet read to its history is checked in the part read, not in the history");
        Write(_root, "story/sessions/session-001.md", "# Session 1\n\n## Scenes\n\n" + line401 + "\n");
        Assert.IsFalse(FlagsOf(FlagKind.LongLines).Any(f => f.Path.StartsWith("story/sessions/", StringComparison.Ordinal)), "a session log is prose nobody splits");
    }

    [TestMethod]
    public void FlagsComeInKindOrderWithCanonFirst()
    {
        Write(_root, "game-state.md", Filler(TokenSizes.Chars(Budgets.GameState) + 400));
        Write(_root, "story/canon.md", CanonOf(TokenSizes.Chars(Budgets.CanonLimit) + 400));
        var kinds = Measure().Flags.Select(f => f.Kind).ToArray();
        Assert.AreEqual(FlagKind.CanonOverLimit, kinds[0]);
        CollectionAssert.AreEqual(kinds.Order().ToArray(), kinds);
    }

    [TestMethod]
    public void TheCheckLinesStopAtTwelveAndSayHowManyMore()
    {
        var flags = Enumerable.Range(1, 14).Select(i => new Flag(FlagKind.OverBudget, "x", $"line {i}", true)).ToList();
        var lines = Flags.CheckLines(flags, Name);
        Assert.HasCount(Budgets.CheckLines, lines);
        Assert.AreEqual($"... and 3 more: run dmc size {Name}", lines[^1]);
        Assert.IsEmpty(Flags.CheckLines([], Name));
    }

    // ---------- the module files ----------

    /// <summary>Makes the fixture a module campaign: a <c>module-progress.md</c> naming <c>modules/lmop/</c> and the digest section, and the module's files.</summary>
    /// <param name="chapterStatus">The Chapter status lines.</param>
    /// <param name="digest">The digest line's value.</param>
    void MakeModuleCampaign(string chapterStatus = "- 1. Goblin Arrows: complete\n- 2. Phandalin: in progress\n",
        string digest = "`chapters/02-phandalin.md`, section Area 3: The Inn")
    {
        Write(_root, "module-progress.md", $"# Module progress\n\n- **Module folder**: `modules/lmop/`\n\n## Current position\n\n- **Digest to read before continuing**: {digest}\n\n## Chapter status\n\n{chapterStatus}");
        string module = Path.Combine(_root, "modules", "lmop");
        WriteAt(module, "index.md", "# Index\n\n" + Filler(400));
        WriteAt(module, "chapters/02-phandalin.md", "# Phandalin\n\n## Area 2: The Square\n\n" + Filler(400)
            + "## Area 3: The Inn\n\n" + Filler(800) + "### Upstairs\n\n" + Filler(200) + "## Area 4: The Mill\n\n" + Filler(400));
    }

    [TestMethod]
    public void AModuleCampaignCountsItsProgressItsIndexAndTheDigestSectionNamed()
    {
        MakeModuleCampaign();
        string[] paths = Paths();
        CollectionAssert.IsSubsetOf(new[] { "module-progress.md", "modules/lmop/index.md", "modules/lmop/chapters/02-phandalin.md" }, paths);
        var section = PartOf("modules/lmop/chapters/02-phandalin.md");
        Assert.AreEqual(PartKind.Section, section.Kind);
        Assert.AreEqual("## Area 3: The Inn\n\n".Length + 800 + "### Upstairs\n\n".Length + 200, section.Bytes, "to the next heading of the same level, its subsections included");
    }

    [TestMethod]
    public void ADigestSectionNotFoundCountsTheChapterWholeAndSaysSo()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md, section The Lighthouse");
        var part = PartOf("modules/lmop/chapters/02-phandalin.md");
        Assert.AreEqual(PartKind.Whole, part.Kind);
        StringAssert.Contains(part.Note, "The Lighthouse");
    }

    [TestMethod]
    public void AChapterNotYetIngestedIsListedAsNotIngested()
    {
        MakeModuleCampaign(digest: "chapters/03-cragmaw.md, section Area 1");
        Assert.AreEqual(EntryState.NotIngested, PartOf("modules/lmop/chapters/03-cragmaw.md").State);
    }

    [TestMethod]
    public void AFinishedModuleCountsOnlyItsProgress()
    {
        MakeModuleCampaign(chapterStatus: "- 1. Goblin Arrows: complete\n- 2. Phandalin: complete\n");
        string[] paths = Paths();
        CollectionAssert.Contains(paths, "module-progress.md");
        Assert.IsFalse(paths.Any(p => p.StartsWith("modules/", StringComparison.Ordinal)));
    }

    // ---------- spec 012: the pointer names areas ----------

    /// <summary>The bytes of a section of the fixture's chapter, heading line to the end of its subsections.</summary>
    static readonly int TheInn = "## Area 3: The Inn\n\n".Length + 800 + "### Upstairs\n\n".Length + 200;

    [TestMethod]
    public void APointerInTheNewFormMeasuresTheHeadingItNames()
    {
        MakeModuleCampaign(digest: "`chapters/02-phandalin.md` § Area 3: The Inn");
        var part = PartOf("modules/lmop/chapters/02-phandalin.md");
        Assert.AreEqual(PartKind.Section, part.Kind);
        Assert.AreEqual(TheInn, part.Bytes);
        Assert.IsEmpty(FlagsOf(FlagKind.PointerMiss));
    }

    [TestMethod]
    public void APointerNamingTwoHeadingsMeasuresEachOnceAsItsOwnPart()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § Area 3: The Inn; § Area 4: The Mill; § The Inn");
        var parts = Measure().Parts.Where(p => p.Path.StartsWith("modules/lmop/chapters/", StringComparison.Ordinal)).ToList();
        CollectionAssert.AreEqual(new[] { "modules/lmop/chapters/02-phandalin.md § Area 3: The Inn", "modules/lmop/chapters/02-phandalin.md § Area 4: The Mill" },
            parts.Select(p => p.Path).ToArray(), "the third names the first again, so it is counted once");
        Assert.AreEqual(TheInn, parts[0].Bytes);
        Assert.AreEqual("## Area 4: The Mill\n\n".Length + 400, parts[1].Bytes);
    }

    [TestMethod]
    public void TheTotalStaysTheSameWhenTheOtherChaptersGrowTenfold()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § Area 3: The Inn");
        long before = Measure().Total;
        string module = Path.Combine(_root, "modules", "lmop");
        WriteAt(module, "chapters/01-goblin-arrows.md", "# Goblin Arrows\n\n## Area 1\n\n" + Filler(40_000));
        WriteAt(module, "monsters.md", "# Monsters\n\n## Goblin\n\n" + Filler(60_000));
        Assert.AreEqual(before, Measure().Total);
    }

    [TestMethod]
    public void TheOldFormsMeasureAsBefore()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md, section Area 3: The Inn");
        Assert.AreEqual(TheInn, PartOf("modules/lmop/chapters/02-phandalin.md").Bytes);
        MakeModuleCampaign(digest: "chapters/02-phandalin.md The Inn");
        Assert.AreEqual(TheInn, PartOf("modules/lmop/chapters/02-phandalin.md").Bytes, "a heading's words find it, as lookup finds it");
        MakeModuleCampaign(digest: "chapters/02-phandalin.md");
        Assert.AreEqual(PartKind.Whole, PartOf("modules/lmop/chapters/02-phandalin.md").Kind);
        MakeModuleCampaign(digest: "none");
        Assert.IsFalse(Paths().Any(p => p.Contains("chapters/", StringComparison.Ordinal)));
        Assert.IsEmpty(FlagsOf(FlagKind.PointerMiss));
    }

    [TestMethod]
    public void AHeadingThePointerNamesIsFoundAsLookupFindsItNotByTheFirstThatContainsIt()
    {
        // "Inn" is inside "Innkeeper's Cellar", which comes first; lookup's exact match finds "The Inn".
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § The Inn");
        WriteAt(Path.Combine(_root, "modules", "lmop"), "chapters/02-phandalin.md",
            "# Phandalin\n\n## The Innkeeper's Cellar\n\n" + Filler(400) + "## The Inn\n\n" + Filler(800));
        var part = PartOf("modules/lmop/chapters/02-phandalin.md");
        Assert.AreEqual("## The Inn\n\n".Length + 800, part.Bytes);
    }

    [TestMethod]
    public void AHeadingThatMatchesNothingMeasuresTheChapterWholeAndHoldsTheSaveWithTheNearest()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § The Old Mill");
        Assert.AreEqual(PartKind.Whole, PartOf("modules/lmop/chapters/02-phandalin.md").Kind);
        var miss = FlagsOf(FlagKind.PointerMiss).Single();
        Assert.AreEqual("module-progress.md: no heading \"The Old Mill\" in chapters/02-phandalin.md (nearest: Area 4: The Mill): name one", miss.Line);
        Assert.IsTrue(miss.HoldsTheSave);
    }

    [TestMethod]
    public void AHeadingThatMatchesSeveralSaysHowMany()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § Area");
        Assert.AreEqual("module-progress.md: \"Area\" matches 3 headings in chapters/02-phandalin.md: name one", FlagsOf(FlagKind.PointerMiss).Single().Line);
    }

    /// <summary>
    /// SC-001: on a module of the measured shape (38 chapters, a hub chapter of six areas, since one of eight is past the
    /// Read cap mark in spec 021's measure; a 37k monster file), a pointer to one hub area measures that area, about 2.7k,
    /// not the hub chapter.
    /// </summary>
    [TestMethod]
    public void OnAModuleOfTheMeasuredShapeAResumeReadsTheIndexAndOneAreaNotTheHubChapter()
    {
        Modules.ModuleFixtures.WriteSoundModule(_root, monsters: 57);
        string m = Modules.ModuleFixtures.Module;
        Write(_root, "module-progress.md", $"# Module progress\n\n- **Module folder**: `modules/{m}/`\n\n## Current position\n\n"
            + "- **Digest to read before continuing**: chapters/05-the-hub.md § 3. Area 5-3\n\n## Chapter status\n\n- 5. The Hub: in progress\n");
        var parts = Measure().Parts.Where(p => p.Path.StartsWith($"modules/{m}/", StringComparison.Ordinal)).ToList();
        CollectionAssert.AreEqual(new[] { $"modules/{m}/index.md", $"modules/{m}/chapters/05-the-hub.md" }, parts.Select(p => p.Path).ToArray());
        var area = parts[1];
        Assert.AreEqual(PartKind.Section, area.Kind);
        Assert.IsLessThanOrEqualTo(3_200L, area.Tokens, "one area, about 3k at most");
        Assert.IsGreaterThan(16_000L, area.FileTokens, "where the hub chapter is over 16k");
        Assert.IsEmpty(FlagsOf(FlagKind.PointerMiss));
    }

    /// <summary>
    /// FR-003: the-silver-egg's pointer, "none" followed by prose that names a file, names no digest, so no module file
    /// is measured beyond the index and nothing holds the save, whether or not the module is finished.
    /// </summary>
    [TestMethod]
    public void APointerOfNoneWithProseNamesNoDigest()
    {
        const string pointer = "none. Consult `modules/lmop/` only when the lair or the Draconic page (`items.md`) come up again.";
        foreach (string status in new[] { "- 2. Phandalin: in progress\n", "- 1. Goblin Arrows: complete\n- 2. Phandalin: complete\n" })
        {
            MakeModuleCampaign(digest: pointer, chapterStatus: status);
            WriteAt(Path.Combine(_root, "modules", "lmop"), "items.md", "# Items\n\n## Draconic page\n\n" + Filler(4000));
            Assert.IsFalse(Paths().Any(p => p.StartsWith("modules/lmop/", StringComparison.Ordinal) && p != "modules/lmop/index.md"), status + string.Join(", ", Paths()));
            Assert.IsEmpty(FlagsOf(FlagKind.PointerMiss), status);
        }
    }

    [TestMethod]
    public void APointerToAFileWhoseNameStartsWithNoneIsMeasured()
    {
        MakeModuleCampaign(digest: "nonesuch-annex.md § Area 3: The Inn");
        File.Move(Path.Combine(_root, "modules", "lmop", "chapters", "02-phandalin.md"), Path.Combine(_root, "modules", "lmop", "nonesuch-annex.md"));
        Assert.AreEqual(TheInn, PartOf("modules/lmop/nonesuch-annex.md").Bytes, "a word that starts with none is not none");
    }

    [TestMethod]
    public void SeveralHeadingsWithAMissMeasureTheChapterWholeOnceAndOneFlagNamesEveryMiss()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § The Lighthouse; § Area 3: The Inn; § The Dock");
        var parts = Measure().Parts.Where(p => p.Path.StartsWith("modules/lmop/chapters/", StringComparison.Ordinal)).ToList();
        Assert.HasCount(1, parts, "the whole chapter holds the matched section, so it is counted once and alone");
        Assert.AreEqual("modules/lmop/chapters/02-phandalin.md", parts[0].Path);
        Assert.AreEqual(PartKind.Whole, parts[0].Kind);
        var miss = FlagsOf(FlagKind.PointerMiss).Single();
        StringAssert.StartsWith(miss.Line, "module-progress.md: no heading \"The Lighthouse\"");
        StringAssert.Contains(miss.Line, "; no heading \"The Dock\"");
    }

    [TestMethod]
    public void ASectionOfAUtf16DigestIsMeasuredAsLookupPrintsIt()
    {
        MakeModuleCampaign(digest: "chapters/02-phandalin.md § Area 3: The Inn");
        string text = "# Phandalin\n\n## Area 2: The Square\n\n" + Filler(400) + "## Area 3: The Inn\n\n" + Filler(800)
            + "## Area 4: The Mill\n\n" + Filler(400);
        File.WriteAllText(Path.Combine(_root, "modules", "lmop", "chapters", "02-phandalin.md"), text, new System.Text.UnicodeEncoding(false, true));
        var part = PartOf("modules/lmop/chapters/02-phandalin.md");
        Assert.AreEqual(PartKind.Section, part.Kind);
        Assert.AreEqual("## Area 3: The Inn\n\n".Length + 800, part.Bytes, "the UTF-8 bytes of the section, as lookup prints it");
    }

    // ---------- the campaign ----------

    [TestMethod]
    public void TheActiveCampaignIsUsedWhenNoneIsNamed() => Assert.AreEqual(Name, SizeReport.Target(_root, null));

    [TestMethod]
    public void ACampaignThatCannotBeMeasuredFailsWithTheContractsMessage()
    {
        Assert.AreEqual("no campaign nope in adventures/", Assert.Throws<DmcException>(() => SizeReport.Target(_root, "nope")).Message);
        Directory.CreateDirectory(Path.Combine(_root, "adventures", ".staging"));
        Assert.AreEqual(".staging is not a campaign (a folder starting with a dot is a staging folder)",
            Assert.Throws<DmcException>(() => SizeReport.Target(_root, ".staging")).Message);
        foreach (string bad in new[] { "", " ", "C:", "a/b", "a\\b" })
            Assert.AreEqual($"no campaign {bad} in adventures/", Assert.Throws<DmcException>(() => SizeReport.Target(_root, bad)).Message, $"\"{bad}\"");
        File.Delete(Path.Combine(_root, "adventures", ".active-campaign"));
        Assert.AreEqual("no active campaign: name one (dmc size NAME)", Assert.Throws<DmcException>(() => SizeReport.Target(_root, null)).Message);
    }

    [TestMethod]
    public void MeasuringWritesNothing()
    {
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada"));
        string before = Hash(_root);
        _ = SizeReport.FullReport(Measure());
        Assert.AreEqual(before, Hash(_root));
    }
}
