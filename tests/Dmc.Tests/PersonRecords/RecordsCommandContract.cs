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
