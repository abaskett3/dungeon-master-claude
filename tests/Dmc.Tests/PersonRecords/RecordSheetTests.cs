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
            "- The smugglers land at the north stair on the 3rd of Mirtul. (s4.1)",
            "",
            "## Stat block",
            "",
            "- **STR**: 10 (setup)",
            "",
            "## History",
            "",
            "- The 5th of Mirtul: mended Pim's sail for nothing. (s5.1)",
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
    public void TheReadSetMeasuresThePrintWithoutSources()
    {
        var parts = ReadSet.Build(_folder, RecordFixtures.Campaign);
        var hessa = parts.Single(p => p.Path == "records/hessa-kettering.json");
        Assert.AreEqual(PartKind.ToHistory, hessa.Kind);
        Assert.IsFalse(hessa.Lines.Any(l => l.Text.Contains("(s4.1)", StringComparison.Ordinal)));
        Assert.IsTrue(hessa.Lines.Any(l => l.Text == "- The smugglers land at the north stair on the 3rd of Mirtul."));
        Assert.IsFalse(hessa.Lines.Any(l => l.Text.StartsWith("## History", StringComparison.Ordinal)));
        Assert.IsTrue(parts.Any(p => p.Path == "records/benno-kettering.json"), "a PC's printed sheet is always read");
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
    public void ShowPrintsTheFactsAndListsAfterPartAsLines()
    {
        var index = RecordIndex.Build(_folder, RecordFixtures.Campaign);
        var lines = RecordText.Of(index, Record("Hessa Kettering"));
        Assert.AreEqual("Facts: Role in the story: The harbour's net-mender (setup); Status: alive (s5.1); AC, max HP, speed, saves, senses: AC 11; HP 9; speed 30 ft (setup); STR: 10 (setup)",
            lines.Single(l => l.StartsWith("Facts: ", StringComparison.Ordinal)));
        Assert.AreEqual("Looks: Grey shawl, a net needle behind one ear. (s1.2)", lines.Single(l => l.StartsWith("Looks: ", StringComparison.Ordinal)));
    }
}
