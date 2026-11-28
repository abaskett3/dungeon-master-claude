using Dmc.PersonRecords;
using Dmc.Sheets;
using Dmc.Size;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// A record printed as a sheet (spec 036 FR-008, FR-010, tasks T011 and T017): the print in its template's shape, the card
/// as a view, the ability and attack tables, the parts the resume reads, and the readers of a sheet (the read set, the
/// hook's sheet lines, the full save's check) reading the print where the Markdown sheet is gone.
/// </summary>
[TestClass]
public sealed class RecordSheetTests
{
    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>The campaign folder in it.</summary>
    string Root => Path.Combine(_folder, "adventures", RecordFixtures.Campaign);

    /// <summary>A fresh copy of the fixture, with Hessa's and Benno's records holding facts and lists.</summary>
    [TestInitialize]
    public void Start()
    {
        _folder = RecordFixtures.NewFolder(present: "Pim Kettering, Hessa Kettering");
        SheetFixtures.Put(_folder, "hessa-kettering.json", SheetFixtures.Hessa);
        SheetFixtures.Put(_folder, "benno-kettering.json", SheetFixtures.Benno);
    }

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    /// <summary>A record of the fixture campaign, read.</summary>
    /// <param name="person">The person's full name.</param>
    PersonRecord Record(string person) => RecordFiles.Read(Root).Records.Single(r => r.Person == person);

    [TestMethod]
    public void TheRecordPrintsInItsTemplatesShapeWithItsSources()
    {
        CollectionAssert.AreEqual(new[]
        {
            "# Hessa Kettering",
            "",
            "- **Role in the story**: The harbour's net-mender (setup)",
            "- **Status**: alive (s5.1)",
            "",
            "## Play card",
            "",
            "- **AC, max HP, speed, saves, senses**: AC 11; HP 9; speed 30 ft (setup)",
            "- **Looks**: Grey shawl, a net needle behind one ear. (s1.2)",
            "- **Voice**: Slow, dry; ends a sentence with \"so.\" \"Sit, Pim, so.\" (to Pim Kettering) (s1.2)",
            "",
            "## What they know",
            "",
            "- The smugglers land at the north stair on the 3rd of Mirtul, 1492 DR. (s4.1)",
            "",
            "## Stat block",
            "",
            "- **STR**: 10 (setup)",
            "",
            "## History",
            "",
            "- The 5th of Mirtul, 1492 DR: mended Pim's sail for nothing. (s5.1)",
        }, RecordSheet.Print(Record("Hessa Kettering"), sources: true));
    }

    [TestMethod]
    public void WithoutSourcesNoEntryEndsWithOne()
    {
        var print = RecordSheet.Print(Record("Hessa Kettering"), sources: false);
        Assert.Contains("- **Status**: alive", print);
        Assert.IsFalse(print.Any(l => l.EndsWith("(setup)", StringComparison.Ordinal) || l.EndsWith("(s1.2)", StringComparison.Ordinal)), string.Join("\n", print));
    }

    [TestMethod]
    public void TheCardAndTheSheetAreTheResumesParts()
    {
        var print = RecordSheet.Print(Record("Hessa Kettering"), sources: true);
        var card = RecordSheet.Part(print, PrintPart.Card);
        Assert.AreEqual("- **Voice**: Slow, dry; ends a sentence with \"so.\" \"Sit, Pim, so.\" (to Pim Kettering) (s1.2)", card[^1]);
        var sheet = RecordSheet.Part(print, PrintPart.Sheet);
        Assert.AreEqual("- **STR**: 10 (setup)", sheet[^1]);
        Assert.HasCount(print.Count, RecordSheet.Part(print, PrintPart.All));
        // A print with no card gives all of it for the card's part.
        var benno = RecordSheet.Print(Record("Benno Kettering"), sources: false);
        Assert.HasCount(benno.Count, RecordSheet.Part(benno, PrintPart.Card));
    }

    [TestMethod]
    public void TheAbilityAndAttackTablesPrintFromFactsAndAList()
    {
        var print = RecordSheet.Print(Record("Benno Kettering"), sources: true);
        var sections = Dmc.Sheets.SheetText.Sections(print);
        CollectionAssert.AreEqual(new[] { "", "Abilities", "Combat", "Story" }, sections.Select(s => s.Heading).ToList());
        var tables = new List<string>();
        var fields = Dmc.Sheets.SheetText.Fields(Dmc.Sheets.SheetText.Find(sections, "Abilities")!.Lines, tables: tables);
        var rows = SheetNumbers.AbilityRows(Dmc.Sheets.SheetText.TableCells(tables));
        CollectionAssert.AreEqual(new[] { "16", "12", "14", "8", "10", "13" }, rows["Score"]);
        CollectionAssert.AreEqual(new[] { "+3", "+1", "+2", "-1", "+0", "+1" }, rows["Modifier"]);
        CollectionAssert.AreEqual(new[] { "+5", "", "", "", "", "" }, rows["Save"]);
        Assert.AreEqual("+2 (setup)", fields.Single(f => f.Label == "Proficiency bonus").Value);
        Assert.Contains("| Boathook | +5 | 1d8+3 piercing | reach 10 ft |", print);
        Assert.Contains("### Attacks", print);
        Assert.Contains("- **Personality**: Laughs at his own jokes first. (s1.4)", print);
    }

    [TestMethod]
    public void TheSheetsOfACampaignHoldThePrintOnlyWhereTheMarkdownSheetIsGone()
    {
        var sheets = RecordSheet.Sheets(Root);
        CollectionAssert.AreEqual(new[] { "npc-hessa-kettering.md", "npc-ilvane-vell.md", "npc-pim-kettering.md", "pc-benno-kettering.md" }, sheets.Select(s => s.File).ToList());
        Assert.IsNotNull(sheets[0].Record);
        Assert.IsNull(sheets[1].Record);

        File.WriteAllText(Path.Combine(Root, "character-sheets", "npc-hessa-kettering.md"), "# Hessa Kettering\n\n- **Status**: alive\n");
        Assert.IsNull(RecordSheet.Sheets(Root)[0].Record, "the Markdown sheet is read while it exists");
        CollectionAssert.Contains(RecordCheck.Records(_folder, RecordFixtures.Campaign),
            "sheet-and-record: character-sheets/npc-hessa-kettering.md: Hessa Kettering's record prints this sheet; remove the Markdown sheet once its lines are in the record");
    }

    [TestMethod]
    public void TheReadSetCountsTheSourceMarksButNotAgainstTheBudget()
    {
        var parts = ReadSet.Build(_folder, RecordFixtures.Campaign);
        var hessa = parts.Single(p => p.Path == "records/hessa-kettering.json");
        Assert.AreEqual(PartKind.ToHistory, hessa.Kind);
        Assert.IsTrue(hessa.Lines.Any(l => l.Text == "- The smugglers land at the north stair on the 3rd of Mirtul, 1492 DR. (s4.1)"), "the resume reads the marks");
        Assert.IsFalse(hessa.Lines.Any(l => l.Text.StartsWith("## History", StringComparison.Ordinal)));
        // T035: " (s4.1)" and the rest are counted in what the resume reads and left out of what the budget sees (#262).
        long marks = RecordSheet.Part(RecordSheet.Print(Record("Hessa Kettering"), sources: true), PrintPart.Sheet).Sum(l => l.Length)
                     - RecordSheet.Part(RecordSheet.Print(Record("Hessa Kettering"), sources: false), PrintPart.Sheet).Sum(l => l.Length);
        Assert.AreEqual(marks, hessa.MarkCharacters);
        Assert.AreEqual(Measurement.Tokens(hessa.Characters - marks), hessa.BudgetTokens);
        Assert.IsGreaterThan(hessa.BudgetTokens, hessa.Tokens);
        Assert.IsTrue(parts.Any(p => p.Path == "records/benno-kettering.json"), "a PC's printed sheet is always read");
    }

    [TestMethod]
    public void ACompanionsPrintedSheetIsReadToItsCardOrMarkedAsHavingNone()
    {
        File.AppendAllText(Path.Combine(Root, "game-state.md"), "\n## Party status\n\n### Hessa Kettering\n\n- **HP**: 9 / 9\n");
        var card = ReadSet.Build(_folder, RecordFixtures.Campaign).Single(p => p.Path == "records/hessa-kettering.json");
        Assert.AreEqual(PartKind.ToPlayCard, card.Kind);
        Assert.AreEqual(BudgetKind.PlayCard, card.Budget);
        Assert.IsFalse(card.NoPlayCard);

        // T033: with no card entry the print has no card, and the read set marks it for the full save to write one.
        SheetFixtures.Put(_folder, "hessa-kettering.json", SheetFixtures.Hessa.Replace("\"card\": true, ", ""));
        var none = ReadSet.Build(_folder, RecordFixtures.Campaign).Single(p => p.Path == "records/hessa-kettering.json");
        Assert.IsTrue(none.NoPlayCard);
        Assert.AreEqual(PartKind.ToHistory, none.Kind);
    }

    [TestMethod]
    public void LookupFindsAPersonsSectionInARecordsPrint()
    {
        // T036: a sheet's heading is found in the print, as in a Markdown sheet.
        var lines = Dmc.Retrieval.Lookup.Run(_folder, RecordFixtures.Campaign, "What they know");
        Assert.IsTrue(lines.Any(l => l.Contains("north stair", StringComparison.Ordinal)), string.Join("\n", lines));
    }

    [TestMethod]
    public void TheSequelDigestAndTheClosingSampleReadAClosedCampaignsRecords()
    {
        // T032: a closed campaign's files as its closing backup gives them.
        var files = Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).ToDictionary(
            p => Path.GetRelativePath(Root, p).Replace('\\', '/'), File.ReadAllBytes);
        var units = Dmc.Campaign.SequelDigest.Units(files);
        CollectionAssert.Contains(units.Where(u => u.Path == "records/hessa-kettering.json").Select(u => u.Text).ToList(), "**Status**: alive");
        CollectionAssert.Contains(units.Select(u => u.Text).ToList(), "The smugglers land at the north stair on the 3rd of Mirtul, 1492 DR.");
        CollectionAssert.Contains(units.Select(u => u.Text).ToList(), "**Looks**: Grey shawl, a net needle behind one ear.");
        Assert.IsFalse(units.Any(u => u.Path == "records/pim-kettering.json"), "a record of rows and limits alone is not a source");
        var people = Dmc.Campaign.SampleDraw.PeopleOf(files, []);
        Assert.IsTrue(people.Any(p => p.Text == "Hessa Kettering" && p.Where == "records/hessa-kettering.json"), string.Join(", ", people.Select(p => p.Text)));
    }

    [TestMethod]
    public void TheHooksSheetLinesComeFromThePrint()
    {
        var people = Dmc.Retrieval.SheetReminder.Present(Root, "- **Present**: Hessa Kettering");
        // "Kettering" also names Pim's Markdown sheet, as the resume would match it.
        var hessa = people.Single(p => p.File == "npc-hessa-kettering.md");
        Assert.IsTrue(hessa.Lines.Any(l => l.Contains("Grey shawl", StringComparison.Ordinal)), string.Join("\n", hessa.Lines));
    }

    [TestMethod]
    public void RecallFindsAFactThatIsOnlyInARecordsPrint()
    {
        var hits = Dmc.Retrieval.Recall.Run(_folder, RecordFixtures.Campaign, "smugglers north stair");
        Assert.IsTrue(hits.Any(h => h.Contains("character-sheets/npc-hessa-kettering.md:", StringComparison.Ordinal) && h.Contains("north stair", StringComparison.Ordinal)),
            string.Join("\n", hits));
    }

    [TestMethod]
    public void DmcSheetFindsAndChecksASheetHeldInARecord()
    {
        // Benno's printed sheet is short of the PC template's fields, so the check names what it lacks: it was found and read.
        var result = DmcRunner.InProcess(["sheet", "Benno Kettering", "--campaign", RecordFixtures.Campaign, "--check", "--folder", _folder]);
        Assert.IsTrue(result.StdoutLines.Concat(result.StderrLines).Any(l => l.StartsWith("pc-benno-kettering.md: ", StringComparison.Ordinal)), result.ToString());
        Assert.IsFalse(result.Stderr.Contains("no sheet named", StringComparison.Ordinal), result.ToString());
    }

    [TestMethod]
    public void ShowPrintsTheFactsAndListsAfterPartAsLines()
    {
        var index = RecordIndex.Build(_folder, RecordFixtures.Campaign);
        var lines = RecordText.Of(index, Record("Hessa Kettering"));
        Assert.AreEqual("Facts: Role in the story: The harbour's net-mender (setup); Status: alive (s5.1); AC, max HP, speed, saves, senses: AC 11; HP 9; speed 30 ft (setup); STR: 10 (setup)",
            lines.Single(l => l.StartsWith("Facts: ", StringComparison.Ordinal)));
        Assert.AreEqual("Looks: Grey shawl, a net needle behind one ear. (s1.2)", lines.Single(l => l.StartsWith("Looks: ", StringComparison.Ordinal)));
    }
}
