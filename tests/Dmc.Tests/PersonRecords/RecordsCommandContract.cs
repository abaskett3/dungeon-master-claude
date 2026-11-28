using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// <c>dmc records show</c>, <c>check</c> and <c>draft</c> (spec 032 FR-007 to FR-009, contracts/dmc-records.md, tasks T030,
/// T034), in this process and through the published binary, on the invented fixture.
/// </summary>
public abstract class RecordsCommandContract
{
    /// <summary>Run <c>dmc records</c> with these arguments.</summary>
    protected abstract ProcessResult Records(params string[] args);

    /// <summary>The temporary DMC folder of the test.</summary>
    protected string Folder = "";

    /// <summary>The campaign folder in it.</summary>
    string Root => Path.Combine(Folder, "adventures", RecordFixtures.Campaign);

    /// <summary>A fresh copy of the fixture for each test.</summary>
    [TestInitialize]
    public void Start() => Folder = RecordFixtures.NewFolder();

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(Folder);

    [TestMethod]
    public void ShowPrintsARecordAsText()
    {
        var result = Records("show", RecordFixtures.Campaign, "Hessa", "--folder", Folder);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "Hessa Kettering, called:",
            "- \"Hessa\": anyone (setup)",
            "- \"Mam\": Pim Kettering, when she is frightened (s5.1)",
            "Relationships: Pim Kettering's mother (s1.2)",
        }, result.StdoutLines);
    }

    [TestMethod]
    public void ShowGivesTheWordsAPersonUsesAndTheirLimits()
    {
        var lines = Records("show", RecordFixtures.Campaign, "Pim Kettering", "--folder", Folder).StdoutLines;
        CollectionAssert.Contains(lines, "Pim Kettering says: \"Mam\" for Hessa Kettering, when she is frightened (s5.1); \"Auntie Ilvane\" for Ilvane Vell (s1.1); \"Cap\" for Ilvane Vell (s2.4); \"Nan Nettle\" for Nettlebone (s4.2)");
        CollectionAssert.Contains(lines, "Relationships: Hessa Kettering is their mother (s1.2)");
        CollectionAssert.Contains(lines, "Limits: Cannot swim yet. (s2.5)");
    }

    [TestMethod]
    public void CheckListsTheFindingsOfTheSamplesAndTheirCount()
    {
        var result = Records("check", RecordFixtures.Campaign, "--folder", Folder);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "sample-stale: character-sheets/npc-ilvane-vell.md:5: \"read\" against the limit \"Cannot read.\" (player: \"she's never learned her letters\")",
            "sample-breaks-rows: character-sheets/npc-pim-kettering.md:5: Pim Kettering never says \"Nan\" for Nettlebone (s4.3)",
            "2 findings",
        }, result.StdoutLines);
    }

    [TestMethod]
    public void DraftStartsFromCanonsAlsoItemsAndCheckDraftFindsTheirConflicts()
    {
        Directory.CreateDirectory(Path.Combine(Root, "backups"));
        File.WriteAllBytes(Path.Combine(Root, "backups", $"{RecordFixtures.Campaign}_backup_20261126120000.zip"), []);
        var draft = Records("draft", RecordFixtures.Campaign, "--folder", Folder);
        Assert.AreEqual(0, draft.ExitCode, draft.ToString());
        CollectionAssert.AreEqual(new[] { $"draft: adventures/{RecordFixtures.Campaign}/backups/{RecordFixtures.Campaign}_backup_20261126120000.zip.records-draft.json, 6 rows" }, draft.StdoutLines);

        var check = Records("check", RecordFixtures.Campaign, "--draft", "--folder", Folder).StdoutLines;
        Assert.AreEqual(6, check.Count(l => l.StartsWith("no-speaker: story/canon.md:", StringComparison.Ordinal)), string.Join("\n", check));
        CollectionAssert.Contains(check, "ambiguous-no-speaker: \"Nan\" names Hessa Kettering and Nettlebone, and story/canon.md:5, story/canon.md:6 says no speaker", string.Join("\n", check));

        var again = Records("draft", RecordFixtures.Campaign, "--folder", Folder);
        Assert.AreEqual(1, again.ExitCode, "a second draft must not overwrite the first");
    }

    [TestMethod]
    public void DraftSheetsHoldsEachSheetLineWithItsLabelSectionAndLine()
    {
        Directory.CreateDirectory(Path.Combine(Root, "backups"));
        File.WriteAllBytes(Path.Combine(Root, "backups", $"{RecordFixtures.Campaign}_backup_20261126120000.zip"), []);
        File.AppendAllText(Path.Combine(Root, "character-sheets", "npc-ilvane-vell.md"), "\n## History\n\n- Took the barge through the weir (s3.2).\n");
        var draft = Records("draft", RecordFixtures.Campaign, "--sheets", "--folder", Folder);
        Assert.AreEqual(0, draft.ExitCode, draft.ToString());
        string path = $"adventures/{RecordFixtures.Campaign}/backups/{RecordFixtures.Campaign}_backup_20261126120000.zip.sheets-draft.json";
        CollectionAssert.AreEqual(new[] { $"draft: {path}, 4 entries (1 with a source)" }, draft.StdoutLines);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, path)));
        var first = doc.RootElement.GetProperty("entries")[0];
        Assert.AreEqual("Ilvane Vell", first.GetProperty("to").GetString());
        Assert.AreEqual("Play card", first.GetProperty("section").GetString());
        Assert.AreEqual("Voice", first.GetProperty("label").GetString());
        Assert.AreEqual("gruff. \"I'll read the river, not your letters.\"", first.GetProperty("text").GetString());
        Assert.AreEqual("character-sheets/npc-ilvane-vell.md:5", first.GetProperty("line").GetString());
        var history = doc.RootElement.GetProperty("entries")[1];
        Assert.AreEqual(System.Text.Json.JsonValueKind.Null, history.GetProperty("label").ValueKind);
        Assert.AreEqual("s3.2", history.GetProperty("source").GetString());

        Assert.AreEqual(1, Records("draft", RecordFixtures.Campaign, "--sheets", "--folder", Folder).ExitCode, "a second draft must not overwrite the first");
        Assert.AreEqual(2, Records("draft", RecordFixtures.Campaign, "--loud", "--folder", Folder).ExitCode);
    }

    /// <summary>
    /// Spec 032 (SC-003's base resume needed six calls): a person is found by a file name, a sheet's file name, or words
    /// of their name in any order and case; a name several records fit gives them all; a name that fits none is named
    /// last, after the others' records, with exit 0.
    /// </summary>
    [TestMethod]
    public void ShowFindsAPersonByAFileNameOrWordsOfTheirNameAndNamesTheMissesLast()
    {
        string First(params string[] names) => Records(["show", RecordFixtures.Campaign, .. names, "--folder", Folder]).StdoutLines[0];
        Assert.AreEqual("Hessa Kettering, called:", First("hessa-kettering"));
        Assert.AreEqual("Hessa Kettering, called:", First("npc-hessa-kettering.md"));
        Assert.AreEqual("Hessa Kettering, called:", First("kettering HESSA"));

        var all = Records("show", RecordFixtures.Campaign, "Kettering", "--folder", Folder);
        Assert.AreEqual(0, all.ExitCode, all.ToString());
        foreach (string person in new[] { "Hessa Kettering", "Pim Kettering", "Benno Kettering" })
            Assert.IsTrue(all.StdoutLines.Any(l => l.StartsWith(person + ", called:", StringComparison.Ordinal) || l.StartsWith(person + " ", StringComparison.Ordinal)), person);

        var some = Records("show", RecordFixtures.Campaign, "Quenby", "Hessa", "--folder", Folder);
        Assert.AreEqual(0, some.ExitCode, some.ToString());
        Assert.AreEqual("Hessa Kettering, called:", some.StdoutLines[0]);
        Assert.AreEqual($"no record for \"Quenby\" (dmc records show {RecordFixtures.Campaign} prints everyone)", some.StdoutLines[^1]);
    }

    /// <summary>
    /// T076 and T083: <c>show</c> finds a record by the file name from before T063 (ASCII letters and digits only), but a
    /// record whose file name is the one asked for comes first.
    /// </summary>
    [TestMethod]
    public void ShowFindsARecordByItsOldFileNameAfterOneWithThatName()
    {
        string records = Path.Combine(Folder, "adventures", RecordFixtures.Campaign, "records");
        File.WriteAllText(Path.Combine(records, "nlé.json"), """{ "person": "Nlé", "rows": [ { "kind": "name", "from": "anyone", "word": "Nlé", "source": "setup" } ], "limits": [] }""");
        string First(string name) => Records("show", RecordFixtures.Campaign, name, "--folder", Folder).StdoutLines[0];
        Assert.AreEqual("Nlé, called:", First("nl.json"), "the old file name");
        File.WriteAllText(Path.Combine(records, "nl.json"), """{ "person": "Nl", "rows": [ { "kind": "name", "from": "anyone", "word": "Nl", "source": "setup" } ], "limits": [] }""");
        Assert.AreEqual("Nl, called:", First("nl.json"), "the name as it is now first");
    }

    [TestMethod]
    public void ShowPrintsAPersonsSheetFromTheirRecordToTheCardOrToHistory()
    {
        SheetFixtures.Put(Folder, "hessa-kettering.json", SheetFixtures.Hessa);
        var card = Records("show", RecordFixtures.Campaign, "Hessa", "--card", "--folder", Folder);
        Assert.AreEqual(0, card.ExitCode, card.ToString());
        Assert.AreEqual("# Hessa Kettering", card.StdoutLines[0]);
        Assert.AreEqual("- **Voice**: Slow, dry; ends a sentence with \"so.\" \"Sit, Pim, so.\" (to Pim Kettering) (s1.2)", card.StdoutLines[^1]);
        var sheet = Records("show", RecordFixtures.Campaign, "Hessa", "--sheet", "--folder", Folder).StdoutLines;
        Assert.AreEqual("- **STR**: 10 (setup)", sheet[^1]);
        var all = Records("show", RecordFixtures.Campaign, "Hessa", "--all", "--folder", Folder).StdoutLines;
        Assert.AreEqual("- The 5th of Mirtul: mended Pim's sail for nothing. (s5.1)", all[^1]);
        // A record that prints no sheet gives its text view for the flag.
        Assert.AreEqual("Pim Kettering, called:", Records("show", RecordFixtures.Campaign, "Pim Kettering", "--card", "--folder", Folder).StdoutLines[0]);
        Assert.AreEqual(2, Records("show", RecordFixtures.Campaign, "Hessa", "--card", "--all", "--folder", Folder).ExitCode);
        Assert.AreEqual(2, Records("show", RecordFixtures.Campaign, "Hessa", "--loud", "--folder", Folder).ExitCode);
    }

    [TestMethod]
    public void AUsageMistakeIsExitCode2AndAMissingPersonExitCode1()
    {
        Assert.AreEqual(2, Records("nope").ExitCode);
        Assert.AreEqual(2, Records("show", "--folder", Folder).ExitCode);
        Assert.AreEqual(2, Records("check", RecordFixtures.Campaign, "--loud", "--folder", Folder).ExitCode);
        var missing = Records("show", RecordFixtures.Campaign, "Quenby", "--folder", Folder);
        Assert.AreEqual(1, missing.ExitCode);
        CollectionAssert.AreEqual(new[] { "dmc records: no record for \"Quenby\"" }, missing.StderrLines);
    }
}

/// <summary><c>dmc records</c> against the library code, in this process.</summary>
[TestClass]
public sealed class RecordsCommandInProcessTests : RecordsCommandContract
{
    /// <summary>Runs <c>dmc records</c> in this process.</summary>
    protected override ProcessResult Records(params string[] args) => DmcRunner.InProcess(["records", .. args]);
}

/// <summary><c>dmc records</c> against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RecordsCommandBlackBoxTests : RecordsCommandContract
{
    /// <summary>Runs <c>dmc records</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Records(params string[] args) => DmcRunner.BlackBox(["records", .. args]);
}
