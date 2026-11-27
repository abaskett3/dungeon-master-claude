using Dmc.PersonRecords;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The findings of the migration's draft (spec 032 FR-007, data-model.md § Finding, task T037), on a planted draft of the
/// invented fixture: two Names lines that give one speaker different words for one person, a <c>never</c> against a
/// usage, a row with no source; and a word for different persons to different speakers, all known, is no finding.
/// </summary>
[TestClass]
public sealed class RecordCheckTests
{
    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>A fresh copy of the fixture for each test.</summary>
    [TestInitialize]
    public void Start() => _folder = RecordFixtures.NewFolder();

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    /// <summary>The findings of a draft with these rows.</summary>
    /// <param name="rows">The draft's rows, as JSON objects.</param>
    List<string> Draft(params string[] rows)
    {
        string path = Path.Combine(_folder, "draft.json");
        File.WriteAllText(path, "{ \"rows\": [\n" + string.Join(",\n", rows) + "\n] }\n");
        return RecordCheck.Draft(_folder, RecordFixtures.Campaign, path);
    }

    [TestMethod]
    public void TwoNamesLinesThatDisagreeAreFound()
    {
        var findings = Draft(
            """{ "to": "Hessa Kettering", "line": "character-sheets/npc-pim-kettering.md:5", "kind": "usage", "from": "Pim Kettering", "word": "Mam", "source": "s5.1" }""",
            """{ "to": "Hessa Kettering", "line": "character-sheets/npc-pim-kettering.md:20", "kind": "usage", "from": "Pim Kettering", "word": "Ma", "source": "s5.1" }""");
        CollectionAssert.Contains(findings, "names-lines-differ: character-sheets/npc-pim-kettering.md:5 and character-sheets/npc-pim-kettering.md:20 give Pim Kettering different words for Hessa Kettering (mam against ma)", string.Join("\n", findings));
    }

    [TestMethod]
    public void ANeverAgainstAUsageAndARowWithNoSourceAreFound()
    {
        var findings = Draft(
            """{ "to": "Nettlebone", "line": "character-sheets/npc-nettlebone.md:4", "kind": "usage", "from": "Nettlebone", "word": "Nan", "never": ["Pim Kettering"], "source": "s4.3" }""",
            """{ "to": "Nettlebone", "line": "character-sheets/npc-pim-kettering.md:6", "kind": "usage", "from": "Pim Kettering", "word": "Nan", "source": null }""");
        CollectionAssert.Contains(findings, "rows-disagree: character-sheets/npc-pim-kettering.md:6 has Pim Kettering say \"Nan\" for Nettlebone, and character-sheets/npc-nettlebone.md:4 says Pim Kettering never does", string.Join("\n", findings));
        CollectionAssert.Contains(findings, "no-source: character-sheets/npc-pim-kettering.md:6 (\"Nan\" for Nettlebone): the line gives no scene, no words of the player, and is not the setup", string.Join("\n", findings));
    }

    [TestMethod]
    public void AWordForTwoPersonsToTwoKnownSpeakersIsNoFinding()
    {
        var findings = Draft(
            """{ "to": "Grumbald", "line": "character-sheets/npc-benno.md:4", "kind": "usage", "from": "Benno Kettering", "word": "Pa", "source": "s1.4" }""",
            """{ "to": "Hessa Kettering", "line": "character-sheets/npc-pim-kettering.md:5", "kind": "usage", "from": "Pim Kettering", "word": "Pa", "source": "s1.2" }""");
        Assert.IsFalse(findings.Any(f => f.Contains("\"Pa\"", StringComparison.Ordinal) && !f.StartsWith("sample", StringComparison.Ordinal)), string.Join("\n", findings));
    }
}
