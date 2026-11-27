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
