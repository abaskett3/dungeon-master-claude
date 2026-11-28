using Dmc.PersonRecords;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The rules every record must meet (spec 032 data-model.md § Rules, task T007): each break refused with its line, the
/// note for one word that means two persons to one speaker, and the invented fixture passing as it is.
/// </summary>
[TestClass]
public sealed class RecordRulesTests
{
    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>The campaign folder in it.</summary>
    string Root => Path.Combine(_folder, "adventures", RecordFixtures.Campaign);

    /// <summary>A fresh copy of the fixture for each test.</summary>
    [TestInitialize]
    public void Start() => _folder = RecordFixtures.NewFolder();

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    /// <summary>The breaks and notes of the fixture with one record replaced.</summary>
    /// <param name="file">The record's file name.</param>
    /// <param name="text">Its new text.</param>
    RecordRules.Outcome With(string file, string text)
    {
        File.WriteAllText(Path.Combine(Root, "records", file), text);
        var loaded = RecordFiles.Read(Root);
        var outcome = RecordRules.Check(SceneBook.For(_folder, RecordFixtures.Campaign), loaded.Records);
        return new RecordRules.Outcome([.. loaded.Errors, .. outcome.Errors], outcome.Notes);
    }

    /// <summary>A record of Pim with one row, the rest of the fixture as it is.</summary>
    /// <param name="row">The row's JSON.</param>
    RecordRules.Outcome PimRow(string row) =>
        With("pim-kettering.json", $$"""{ "person": "Pim Kettering", "rows": [ {{row}} ], "limits": [] }""");

    [TestMethod]
    public void TheFixturePassesWithOneNote()
    {
        var outcome = With("pim-kettering.json", RecordFixtures.Records["pim-kettering.json"]);
        Assert.HasCount(0, outcome.Errors, string.Join("\n", outcome.Errors));
        CollectionAssert.AreEqual(new[] { "to Benno Kettering, \"Da\" now means Grumbald and Ilvane Vell; the hook prints both." }, outcome.Notes);
    }

    [TestMethod]
    public void EachBreakIsRefusedWithItsLine()
    {
        var cases = new (string Row, string Expected)[]
        {
            ("""{ "kind": "usage", "word": "the Salt-wife", "source": "s3.1" }""", "records/pim-kettering.json row 1 (\"the Salt-wife\"): no speaker (from): say who uses the word, or anyone for a name"),
            ("""{ "kind": "usage", "from": "Grumbald", "word": "Thimble" }""", "records/pim-kettering.json row 1 (\"Thimble\"): no source (a scene such as s4.2, player: \"…\", setup, or inference)"),
            ("""{ "kind": "alias", "from": "anyone", "word": "the Heron", "source": "s3.2" }""", "records/pim-kettering.json row 1 (\"the Heron\"): kind \"alias\" is not usage, name or relationship"),
            ("""{ "kind": "name", "from": "anyone", "word": "Pimkin", "source": "s9.9" }""", "records/pim-kettering.json row 1 (\"Pimkin\"): scene s9.9 is not in the session logs"),
            ("""{ "kind": "name", "from": "anyone", "word": "Pimkin", "source": "s2.7" }""", "records/pim-kettering.json row 1 (\"Pimkin\"): scene s2.7 is not in the session logs"),
            ("""{ "kind": "name", "from": "anyone", "word": "Pimkin", "source": "the dock" }""", "records/pim-kettering.json row 1 (\"Pimkin\"): source \"the dock\" is not a scene (s4.2), player: \"…\", setup, or inference"),
            ("""{ "kind": "usage", "from": "Benno", "word": "Pip", "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"Pip\"): speaker \"Benno\" is not a person of the records, anyone, or group: … (did you mean \"Benno Kettering\"?)"),
            ("""{ "kind": "usage", "from": "anyone", "word": "Pip", "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"Pip\"): a word anyone says is kind name, not usage"),
            ("""{ "kind": "name", "from": "Grumbald", "word": "Pip", "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"Pip\"): kind name is from anyone; a person's word is kind usage"),
            ("""{ "kind": "relationship", "from": "group: her crew", "word": "ward", "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"ward\"): a relationship is from a person, not a group"),
            ("""{ "kind": "usage", "from": "Grumbald", "word": "Pip", "nickname": true, "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"Pip\"): unknown key \"nickname\" (a row has kind, from, word, when, never, source)"),
            ("""{ "kind": "usage", "from": "Grumbald", "word": "Pip", "never": ["Somebody"], "source": "s1.1" }""", "records/pim-kettering.json row 1 (\"Pip\"): never \"Somebody\" is not a person of the records or group: …"),
            ("""{ "kind": "name", "from": "anyone", "word": "Pimkin", "source": "kettle-before s2.1" }""", ""),
            ("""{ "kind": "name", "from": "anyone", "word": "Pimkin", "source": "elsewhere s2.1" }""", "records/pim-kettering.json row 1 (\"Pimkin\"): elsewhere is not a campaign this one continues"),
        };
        foreach (var (row, expected) in cases)
        {
            var errors = PimRow(row).Errors;
            if (expected.Length == 0)
                Assert.HasCount(0, errors, $"{row}: {string.Join("\n", errors)}");
            else
                Assert.AreEqual(expected, errors.FirstOrDefault(), row);
        }
    }

    [TestMethod]
    public void ARowOfASpeakerOnTheWordsNeverListIsRefused()
    {
        const string da = """{ "kind": "usage", "from": "Benno Kettering", "word": "Da",""";
        const string cap = """{ "kind": "usage", "from": "Benno Kettering", "word": "Cap", "source": "s4.1" },""";
        string ilvane = RecordFixtures.Records["ilvane-vell.json"].Replace(da, cap + "\n" + da);
        var errors = With("ilvane-vell.json", ilvane).Errors;
        Assert.AreEqual("records/ilvane-vell.json row 5 (\"Cap\"): Benno Kettering never says \"Cap\" for Ilvane Vell (row 4, s2.4)", errors.Single());
    }

    [TestMethod]
    public void ARepeatedRowAWrongFileNameAndBadJsonAreRefused()
    {
        Assert.AreEqual("records/pim-kettering.json row 2 (\"pim\"): the same row as row 1",
            PimRow("""{ "kind": "name", "from": "anyone", "word": "Pim", "source": "setup" }, { "kind": "name", "from": "anyone", "word": "pim", "source": "s1.1" }""").Errors.Single());
        CollectionAssert.Contains(With("pim-kettering.json", """{ "person": "Pim Vane", "rows": [], "limits": [] }""").Errors,
            "records/pim-kettering.json: the record of \"Pim Vane\" must be records/pim-vane.json");
        CollectionAssert.Contains(With("pim-kettering.json", "{ \"person\": ").Errors, "records/pim-kettering.json: not valid JSON (line 1)");
        Assert.AreEqual("records/pim-kettering.json limit 1: no text",
            With("pim-kettering.json", """{ "person": "Pim Kettering", "rows": [], "limits": [ { "source": "s1.1" } ] }""").Errors.Single());
    }

    [TestMethod]
    public void AnOfThatNamesNoCampaignThisOneContinuesIsRefused() =>
        Assert.AreEqual("records/mother-gorse.json: \"of\" names nowhere, which is not a campaign this one continues",
            With("mother-gorse.json", """{ "person": "Mother Gorse", "of": "nowhere", "rows": [], "limits": [] }""").Errors.Single());

    /// <summary>
    /// T061, T064 and T074: a scene is one of the log's real headings, not the template's commented <c>### Scene N:</c>
    /// nor one whose number overflows, and the scene in play (the one after the latest session's last heading) counts
    /// before its block is written, in every check: the rewind runs the full save's check mid-session. A session file
    /// whose number overflows is not the latest session (T081).
    /// </summary>
    [TestMethod]
    public void AScenesHeadingMustBeRealAndTheSceneInPlayCounts()
    {
        File.WriteAllText(Path.Combine(Root, "story", "sessions", "session-007.md"),
            "# Session 7\n\n## Scenes\n\n<!--\n### Scene N: short title (location, in-world time)\n-->\n\n### Scene 1: an invented scene (the lock)\n\n### Scene 99999999999: a typo\n");
        File.WriteAllText(Path.Combine(Root, "story", "sessions", "session-99999999999.md"), "# Session ?\n");
        var book = SceneBook.For(_folder, RecordFixtures.Campaign);
        Assert.IsNull(book.Missing(new RecordSource.SceneRef(null, 7, 1)));
        Assert.IsNull(book.Missing(new RecordSource.SceneRef(null, 7, 2)), "the scene in play");
        Assert.AreEqual("scene s7.3 is not in the session logs", book.Missing(new RecordSource.SceneRef(null, 7, 3)));
        Assert.AreEqual("scene s6.7 is not in the session logs", book.Missing(new RecordSource.SceneRef(null, 6, 7)), "not the latest session");
        Assert.AreEqual("scene kettle-before s2.3 is not in the session logs", book.Missing(new RecordSource.SceneRef(RecordFixtures.Earlier, 2, 3)), "not a campaign this one continues' scene in play");
    }

    /// <summary>T060: a person whose record is in a campaign this one continues may be a speaker and in a never list.</summary>
    [TestMethod]
    public void APersonOfTheCampaignThisOneContinuesMayBeASpeaker()
    {
        string earlier = Path.Combine(_folder, "adventures", RecordFixtures.Earlier, "records");
        Directory.CreateDirectory(earlier);
        File.WriteAllText(Path.Combine(earlier, "old-wick.json"), """{ "person": "Old Wick", "rows": [ { "kind": "name", "from": "anyone", "word": "Wick", "source": "setup" } ], "limits": [] }""");
        var outcome = PimRow("""{ "kind": "usage", "from": "Old Wick", "word": "Minnow", "source": "kettle-before s2.1" }, { "kind": "usage", "from": "Grumbald", "word": "Pimmy", "never": ["Old Wick"], "source": "s3.3" }""");
        Assert.HasCount(0, outcome.Errors, string.Join("\n", outcome.Errors));
    }

    /// <summary>T059: after an edit, a speaker with a sheet and no record yet is a note; the full save's check still refuses the row.</summary>
    [TestMethod]
    public void ASpeakerWithASheetAndNoRecordIsANoteAfterAnEditAndABreakAtTheFullSave()
    {
        const string row = """{ "kind": "usage", "from": "Orrin Flax", "word": "Pip", "source": "s3.3" }""";
        StringAssert.StartsWith(PimRow(row).Errors.Single(), "records/pim-kettering.json row 1 (\"Pip\"): speaker \"Orrin Flax\" is not a person of the records");
        var loaded = RecordFiles.Read(Root);
        var afterEdit = RecordRules.Check(SceneBook.For(_folder, RecordFixtures.Campaign), loaded.Records, ["Orrin Flax", "Pim Kettering"]);
        Assert.HasCount(0, afterEdit.Errors, string.Join("\n", afterEdit.Errors));
        CollectionAssert.Contains(afterEdit.Notes, "records/pim-kettering.json row 1 (\"Pip\"): \"Orrin Flax\" has a sheet and no record yet; write theirs (the full save's check refuses the row until then).");
    }

    /// <summary>T063: a record's file name keeps letters outside ASCII, so two names never share one.</summary>
    [TestMethod]
    public void AFileNameKeepsEveryLetter()
    {
        Assert.AreEqual("pim-kettering.json", RecordFiles.Slug("Pim Kettering"));
        Assert.AreEqual("éla.json", RecordFiles.Slug("Éla"));
        Assert.AreEqual("íla.json", RecordFiles.Slug("Íla"));
        Assert.AreEqual("ясна.json", RecordFiles.Slug("Ясна"));
        Assert.AreEqual("shalefire-shale-stoutheart.json", RecordFiles.Slug("Shalefire \"Shale\" Stoutheart"));
        Assert.HasCount(0, With("la.json", """{ "person": "Éla", "rows": [ { "kind": "name", "from": "anyone", "word": "Éla", "source": "setup" } ], "limits": [] }""").Errors,
            "a record written under the old file name still passes");
        CollectionAssert.Contains(With(".json", """{ "person": "Ясна", "rows": [], "limits": [] }""").Errors,
            "records/.json: the record of \"Ясна\" must be records/ясна.json", "an old file name with no stem does not pass");
    }

    /// <summary>
    /// T070 and T075: a check of shape alone (migration 010's records-in-place) cannot see a linked campaign, so in a
    /// campaign that continues another it takes any named speaker; in one that continues none, a speaker with no record
    /// is still refused.
    /// </summary>
    [TestMethod]
    public void AShapeOnlyCheckTakesASpeakerItCannotSeeOnlyInALinkedCampaign()
    {
        var record = new PersonRecord(RecordFixtures.Campaign, "pim-kettering.json", "Pim Kettering", null,
            [new Row("usage", "Old Wick", "Minnow", null, ["Old Wick"], "kettle-before s2.1", [])], [], []);
        Assert.HasCount(0, RecordRules.Check(SceneBook.Lenient(continues: true), [record]).Errors);
        StringAssert.StartsWith(RecordRules.Check(SceneBook.Lenient(), [record]).Errors.First(), "records/pim-kettering.json row 1 (\"Minnow\"): speaker \"Old Wick\" is not a person of the records");
    }

    /// <summary>A minor-NPC list or a history file is no person with a sheet.</summary>
    [TestMethod]
    public void TheSheetTitlesNameOnlyPersons()
    {
        File.WriteAllText(Path.Combine(Root, "character-sheets", "minor-npcs.md"), "# Minor NPCs\n");
        File.WriteAllText(Path.Combine(Root, "character-sheets", "npc-pim-kettering-history.md"), "# Pim Kettering history\n");
        CollectionAssert.AreEquivalent(new[] { "Pim Kettering", "Ilvane Vell" }, RecordCheck.SheetTitles(Root).ToList());
    }
}
