using Dmc.PersonRecords;
using Dmc.Retrieval;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The index over the records (spec 032 FR-002, FR-004, task T008): by word, by speaker, the longest word first, no list
/// of title words, the campaign it continues, and a relationship that never becomes a word someone says.
/// </summary>
[TestClass]
public sealed class RecordIndexTests
{
    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>The fixture's index.</summary>
    RecordIndex Index => RecordIndex.Build(_folder, RecordFixtures.Campaign);

    /// <summary>A fresh copy of the fixture for each test.</summary>
    [TestInitialize]
    public void Start() => _folder = RecordFixtures.NewFolder();

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    [TestMethod]
    public void AWordGivesEveryRowOfItAndASpeakerTheirRows()
    {
        CollectionAssert.AreEquivalent(new[] { "Grumbald", "Ilvane Vell" }, Index.ByWord("da").Select(e => e.Person).ToArray());
        CollectionAssert.AreEquivalent(new[] { "Mam", "mother", "Auntie Ilvane", "Cap", "Nan Nettle" }, Index.BySpeaker("Pim Kettering").Select(e => e.Row.Word).ToArray());
    }

    [TestMethod]
    public void TheLongestWordWinsAndAWordNoRowHoldsIsNotFound()
    {
        var found = Index.WordsIn("Auntie Ilvane waves; the Captain and Nan Nettle laugh, and nobody says auntie");
        CollectionAssert.AreEqual(new[] { "Auntie Ilvane", "Captain", "Nan Nettle" }, found.Select(f => f.Word).ToArray());
    }

    [TestMethod]
    public void AWordMeaningOnePersonNamesThemAndOneMeaningTwoNamesNone()
    {
        CollectionAssert.AreEqual(new[] { "Hessa Kettering" }, Index.Named("Mam?"), string.Join("|", Index.Named("Mam?")));
        CollectionAssert.AreEqual(Array.Empty<string>(), Index.Named("Da?"), string.Join("|", Index.Named("Da?")));
        CollectionAssert.AreEqual(new[] { "Mother Gorse" }, Index.Named("What do we know about Mother?"), string.Join("|", Index.Named("What do we know about Mother?")));
    }

    [TestMethod]
    public void TheCampaignItContinuesIsLinkedAndACampaignWithoutRecordsHasNone()
    {
        CollectionAssert.AreEqual(new[] { RecordFixtures.Earlier }, RecordIndex.Earlier(_folder, RecordFixtures.Campaign).Keys.ToArray());
        Assert.HasCount(0, RecordIndex.Build(_folder, RecordFixtures.Earlier).Records);
    }

    [TestMethod]
    public void ARelationshipIsNeverAWordTheSpeakerSays()
    {
        var block = RowsReminder.Block(Index, "Pim Kettering", ["Pim Kettering", "Hessa Kettering"]);
        string says = block.Single(l => l.StartsWith("- Pim Kettering says:", StringComparison.Ordinal));
        StringAssert.Contains(says, "\"Mam\" for Hessa Kettering");
        Assert.DoesNotContain("mother", says);
    }
}
