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

    [TestMethod]
    public void TokensAreBytesDividedByFourRoundedDown()
    {
        var lines = Measurement.Split("abcdef\n"u8.ToArray());
        var part = new Part("x", PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, null, lines);
        Assert.AreEqual(7, part.Bytes);
        Assert.AreEqual(1, part.Tokens);
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

    [TestMethod]
    public void EveryBudgetSitsWellUnderTheReadCap()
    {
        int[] budgets = [Budgets.GameState, Budgets.DmNotes, Budgets.Summary, Budgets.PcSheet, Budgets.OtherSheet, Budgets.CanonLimit];
        foreach (int budget in budgets)
            Assert.IsLessThan(Budgets.NearCapTokens, budget, "every budget is under the near-cap mark, so no file is paged at resume");
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

    [TestMethod]
    public void TheTotalIsTheSumOfThePartsAndEachIsBytesOverFour()
    {
        var result = Measure();
        Assert.AreEqual(result.Parts.Sum(p => p.Tokens), result.Total);
        string summary = Path.Combine(CampaignFolder(_root), "story", "summary.md");
        Assert.AreEqual(new FileInfo(summary).Length / 4, PartOf("story/summary.md").Tokens);
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

    [TestMethod]
    public void CanonJustUnderItsLimitIsReadWholeAndAtItIsReadInItsAlwaysReadSections()
    {
        Write(_root, "story/canon.md", CanonOf(Budgets.CanonLimit * 4 - 4));
        Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind);
        Assert.IsEmpty(FlagsOf(FlagKind.CanonOverLimit));

        Write(_root, "story/canon.md", CanonOf(Budgets.CanonLimit * 4));
        var canon = PartOf("story/canon.md");
        Assert.AreEqual(PartKind.AlwaysReadSections, canon.Kind);
        CollectionAssert.AreEqual(new[] { "Rulings", "Prices, debts and promises", "Cast" }, canon.Sections.Select(s => s.Heading).ToArray(),
            "the two always-read sections and the one whose comment says Always read; the Timeline is searched, not read");
        Assert.HasCount(1, FlagsOf(FlagKind.CanonOverLimit));
    }

    [TestMethod]
    public void TwoCanonsOfTheSameLineCountAreTreatedByTheirSize()
    {
        Write(_root, "story/canon.md", CanonOf(32000, lineLength: 160));
        Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind, "8k of short lines");
        Write(_root, "story/canon.md", CanonOf(96000, lineLength: 480));
        Assert.AreEqual(PartKind.AlwaysReadSections, PartOf("story/canon.md").Kind, "24k of long lines");
    }

    [TestMethod]
    public void TheLongCampaignsCanonIsReadWholeNowAndAfterTheReviewsAdditions()
    {
        foreach (int tokens in new[] { 15500, 17000 })
        {
            Write(_root, "story/canon.md", CanonOf(tokens * 4));
            Assert.AreEqual(PartKind.Whole, PartOf("story/canon.md").Kind, $"{tokens} tokens");
        }
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

    [TestMethod]
    public void AFileNearTheReadCapIsFlaggedFromTwentyThousandTokensOrSixteenHundredLines()
    {
        Write(_root, "story/dm-notes.md", NotesOfSize(Budgets.NearCapTokens * 4 - 1));
        Assert.IsEmpty(FlagsOf(FlagKind.NearCap), "19,999 tokens");
        Write(_root, "story/dm-notes.md", NotesOfSize(Budgets.NearCapTokens * 4));
        Assert.AreEqual("story/dm-notes.md 20k is near the Read cap (25k): it may be cut short at resume", FlagsOf(FlagKind.NearCap).Single().Line);

        Write(_root, "story/dm-notes.md", NotesOfLines(Budgets.NearCapLines - 1));
        Assert.IsEmpty(FlagsOf(FlagKind.NearCap), "1,599 lines");
        Write(_root, "story/dm-notes.md", NotesOfLines(Budgets.NearCapLines));
        Assert.HasCount(1, FlagsOf(FlagKind.NearCap), "1,600 lines");
    }

    [TestMethod]
    public void AFileWithoutABudgetNearTheCapIsFlaggedButDoesNotHoldTheSave()
    {
        Write(_root, "story/sessions/session-001.md", LogOf(1, sceneBytes: Budgets.NearCapTokens * 4 + 100));
        var flag = FlagsOf(FlagKind.NearCap).Single();
        Assert.AreEqual("story/sessions/session-001.md", flag.Path);
        Assert.IsFalse(flag.HoldsTheSave);
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget), "a session log has no budget");
    }

    [TestMethod]
    public void AFileOverItsBudgetIsFlaggedAndOneAtItIsNot()
    {
        Write(_root, "game-state.md", Filler(Budgets.GameState * 4));
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget), "exactly at the budget");
        Write(_root, "game-state.md", Filler(Budgets.GameState * 4 + 4));
        Assert.AreEqual("game-state.md 4.1k is over its 4k budget", FlagsOf(FlagKind.OverBudget).Single().Line);
    }

    [TestMethod]
    public void APcSheetHasALargerBudgetThanOtherSheets()
    {
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: Budgets.OtherSheet * 4 + 400));
        Write(_root, "character-sheets/npc-b.md", Sheet("Bram", bodyBytes: Budgets.OtherSheet * 4 + 400));
        Write(_root, "game-state.md", GameState(["Bram"]));
        CollectionAssert.AreEqual(new[] { "character-sheets/npc-b.md" }, FlagsOf(FlagKind.OverBudget).Select(f => f.Path).ToArray());
        Write(_root, "character-sheets/pc-a.md", Sheet("Ada", bodyBytes: Budgets.PcSheet * 4 + 400));
        CollectionAssert.Contains(FlagsOf(FlagKind.OverBudget).Select(f => f.Path).ToArray(), "character-sheets/pc-a.md");
    }

    [TestMethod]
    public void ThePlayerOptionsHaveNoBudget()
    {
        File.WriteAllText(Path.Combine(_root, "adventures", "player-options.md"), Filler(Budgets.OtherSheet * 4 * 3));
        Assert.IsEmpty(FlagsOf(FlagKind.OverBudget));
    }

    [TestMethod]
    public void ASectionOfTheNotesOverItsOwnBudgetIsFlagged()
    {
        Write(_root, "story/dm-notes.md", Notes(about: Filler(Budgets.DmNotesAboutThePlayer * 4 - 40), arcPlan: Filler(Budgets.DmNotesArcPlan * 4 - 40)));
        Assert.IsEmpty(FlagsOf(FlagKind.SectionOverBudget));
        Write(_root, "story/dm-notes.md", Notes(about: Filler(Budgets.DmNotesAboutThePlayer * 4 + 400), arcPlan: Filler(Budgets.DmNotesArcPlan * 4 + 400)));
        CollectionAssert.AreEqual(new[] { "story/dm-notes.md Arc plan: 1.7k, over its 1.5k budget", "story/dm-notes.md About this player: 2.2k, over its 2k budget" },
            FlagsOf(FlagKind.SectionOverBudget).Select(f => f.Line).ToArray());
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
        Write(_root, "story/canon.md", "# Canon\n\n## Prices, debts and promises\n\n" + Filler(Budgets.CanonPricesSection * 4 - 200));
        Assert.IsEmpty(FlagsOf(FlagKind.PricesSection));
        Write(_root, "story/canon.md", "# Canon\n\n## Prices, debts and promises\n\n" + Filler(Budgets.CanonPricesSection * 4 + 400));
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
        Write(_root, "story/sessions/session-001.md", "# Session 1\n\n## Scenes\n\n" + line401 + "\n");
        Assert.IsFalse(FlagsOf(FlagKind.LongLines).Any(f => f.Path.StartsWith("story/sessions/", StringComparison.Ordinal)), "a session log is prose nobody splits");
    }

    [TestMethod]
    public void FlagsComeInKindOrderWithCanonFirst()
    {
        Write(_root, "game-state.md", Filler(Budgets.GameState * 4 + 400));
        Write(_root, "story/canon.md", CanonOf(Budgets.CanonLimit * 4 + 400));
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
