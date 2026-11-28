using Dmc.PersonRecords;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The rules spec 036 adds to a record (data-model.md rules 8 to 12, task T005): one value a key, the checked keys'
/// values, a source on every fact and entry, full dates, and the <c>sheet</c> a record with facts names.
/// </summary>
[TestClass]
public sealed class RecordFactsTests
{
    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>The campaign folder in it.</summary>
    string Root => Path.Combine(_folder, "adventures", RecordFixtures.Campaign);

    /// <summary>A fresh copy of the fixture, with Hessa's record holding facts and lists.</summary>
    [TestInitialize]
    public void Start()
    {
        _folder = RecordFixtures.NewFolder();
        SheetFixtures.Put(_folder, "hessa-kettering.json", SheetFixtures.Hessa);
    }

    /// <summary>Deletes the copy.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    /// <summary>The breaks of the campaign's records with Hessa's record changed.</summary>
    /// <param name="change">Turns her record's text into the one checked.</param>
    List<string> Errors(Func<string, string>? change = null)
    {
        if (change is not null)
            SheetFixtures.Put(_folder, "hessa-kettering.json", change(SheetFixtures.Hessa.Replace("\r\n", "\n")));
        var loaded = RecordFiles.Read(Root);
        return [.. loaded.Errors, .. RecordRules.Check(SceneBook.For(_folder, RecordFixtures.Campaign), loaded.Records).Errors];
    }

    /// <summary>Hessa's record with one more fact after her Status.</summary>
    /// <param name="fact">The fact's JSON.</param>
    static Func<string, string> AddFact(string fact) => text => text.Replace(
        """{ "key": "Status", "value": "alive", "source": "s5.1" },""",
        """{ "key": "Status", "value": "alive", "source": "s5.1" },""" + "\n    " + fact + ",");

    [TestMethod]
    public void TheRecordWithFactsAndListsPasses()
    {
        var errors = Errors();
        Assert.HasCount(0, errors, string.Join("\n", errors));
        var hessa = RecordFiles.Read(Root).Records.Single(r => r.Person == "Hessa Kettering");
        Assert.AreEqual("npc-hessa-kettering.md", hessa.Sheet);
        Assert.HasCount(4, hessa.Facts);
        Assert.AreEqual("10", hessa.Facts[3].Value, "a number value is taken as its text");
        Assert.IsTrue(hessa.Lists[0].Card);
    }

    [TestMethod]
    public void ASecondValueForAKeyIsRefusedIgnoringCase()
    {
        var errors = Errors(AddFact("""{ "key": " status ", "value": "missing", "source": "s6.1" }"""));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json fact 3 (\" status \"): a second value for \"status\" (fact 2 says \"alive\"): change that fact, or put this one back" }, errors);
    }

    [TestMethod]
    public void EachBreakOfAFactOrEntryIsRefusedWithItsLine()
    {
        var cases = new (string Fact, string Expected)[]
        {
            ("""{ "key": "Hair", "value": "brown" }""", "records/hessa-kettering.json fact 3 (\"Hair\"): no source (a scene such as s4.2, player: \"…\", setup, or inference)"),
            ("""{ "key": "Hair", "value": "brown", "source": "s9.9" }""", "records/hessa-kettering.json fact 3 (\"Hair\"): scene s9.9 is not in the session logs"),
            ("""{ "value": "brown", "source": "setup" }""", "records/hessa-kettering.json fact 3: no key"),
            ("""{ "key": "Hair", "value": " ", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"Hair\"): no value"),
            ("""{ "key": "Hair", "value": "brown", "colour": "x", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"Hair\"): unknown key \"colour\" (a fact has key, value, section, card, source)"),
            ("""{ "key": "Hair", "value": "brown", "card": "yes", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"Hair\"): \"card\" must be true or false"),
            ("""{ "key": "DEX", "value": "nimble", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"DEX\"): \"nimble\" is not a score: DEX is a whole number from 1 to 30"),
            ("""{ "key": "dex", "value": "31", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"dex\"): \"31\" is not a score: DEX is a whole number from 1 to 30"),
            ("""{ "key": "DEX save", "value": "good", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"DEX save\"): \"good\" is not a save: write the signed number (+3, -1)"),
            ("""{ "key": "Proficiency bonus", "value": "+12", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"Proficiency bonus\"): \"+12\" is not a proficiency bonus: write +2 to +9"),
            ("""{ "key": "Passive Perception", "value": "-1", "source": "setup" }""", "records/hessa-kettering.json fact 3 (\"Passive Perception\"): \"-1\" is not a passive Perception: write a whole number"),
            ("""{ "key": "Net ready", "value": "on the 14th", "source": "s6.1" }""", "records/hessa-kettering.json fact 3 (\"Net ready\"): \"the 14th\" has no month: write the full in-world date (the 14th of <month>, <year> DR)"),
        };
        foreach (var (fact, expected) in cases)
            CollectionAssert.AreEqual(new[] { expected }, Errors(AddFact(fact)), fact);
    }

    [TestMethod]
    public void AGoodCheckedKeyAndAFullDatePass()
    {
        foreach (string fact in new[]
        {
            """{ "key": "DEX", "value": "14", "source": "setup" }""",
            """{ "key": "DEX save", "value": "+2 (proficient)", "source": "setup" }""",
            """{ "key": "Proficiency bonus", "value": "+2", "source": "setup" }""",
            """{ "key": "Passive Perception", "value": 12, "source": "setup" }""",
            """{ "key": "Net ready", "value": "on the 14th of Mirtul, 1492 DR", "source": "s6.1" }""",
            """{ "key": "Hair", "value": "brown", "source": "player: \"brown hair\"" }""",
        })
            Assert.HasCount(0, Errors(AddFact(fact)), fact);
    }

    [TestMethod]
    public void AListEntryNeedsAListATextAndASource()
    {
        string Entry(string entry) => SheetFixtures.Hessa.Replace("\r\n", "\n").Replace(
            """{ "list": "Looks",""", entry + ",\n    " + """{ "list": "Looks",""");
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json list 1: no list (the list's name, such as Looks or History)" },
            Errors(_ => Entry("""{ "text": "Tall.", "source": "setup" }""")));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json list 1 (\"Looks\"): no text" },
            Errors(_ => Entry("""{ "list": "Looks", "source": "setup" }""")));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json list 1 (\"Looks\"): no source (a scene such as s4.2, player: \"…\", setup, or inference)" },
            Errors(_ => Entry("""{ "list": "Looks", "text": "Tall." }""")));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json list 1 (\"History\"): \"the 2nd\" has no month: write the full in-world date (the 2nd of <month>, <year> DR)" },
            Errors(_ => Entry("""{ "list": "History", "text": "Met Pim on the 2nd.", "source": "s1.1" }""")));
    }

    [TestMethod]
    public void ARecordWithFactsNamesItsSheet()
    {
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json: a record with facts or lists names its sheet (\"sheet\": \"npc-hessa-kettering.md\")" },
            Errors(t => t.Replace("""  "sheet": "npc-hessa-kettering.md",""" + "\n", "")));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json: sheet \"hessa.md\" must be a sheet's file name, pc-<name>.md or npc-<name>.md" },
            Errors(t => t.Replace("npc-hessa-kettering.md", "hessa.md")));
        SheetFixtures.Put(_folder, "pim-kettering.json", """
            { "person": "Pim Kettering", "sheet": "npc-hessa-kettering.md", "rows": [], "limits": [] }
            """);
        CollectionAssert.AreEqual(new[] { "two records print the sheet npc-hessa-kettering.md: records/hessa-kettering.json, records/pim-kettering.json" }, Errors(t => t));
    }

    [TestMethod]
    public void TheDateRuleCoversLimitsAndWhenNotesOnlyOnARecordWithFacts()
    {
        // Pim's record has rows and limits only, as at standard 10: its limit's bare day is not checked.
        SheetFixtures.Put(_folder, "pim-kettering.json", """
            { "person": "Pim Kettering", "rows": [ { "kind": "name", "from": "anyone", "word": "Pim", "source": "setup" } ], "limits": [ { "text": "Cannot swim until the 9th.", "source": "s2.5" } ] }
            """);
        Assert.HasCount(0, Errors());
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json limit 1: \"the 9th\" has no month: write the full in-world date (the 9th of <month>, <year> DR)" },
            Errors(t => t.Replace("\"limits\": []", "\"limits\": [ { \"text\": \"Cannot row until the 9th.\", \"source\": \"s2.5\" } ]")));
        CollectionAssert.AreEqual(new[] { "records/hessa-kettering.json row 2 (\"Mam\"): \"the 3rd\" has no month: write the full in-world date (the 3rd of <month>, <year> DR)" },
            Errors(t => t.Replace("when she is frightened", "since the 3rd")));
    }

    [TestMethod]
    public void EveryDateNeedsItsYear()
    {
        var errors = Errors(t => t.Replace(", 1492 DR", ""));
        CollectionAssert.AreEqual(new[]
        {
            "records/hessa-kettering.json list 3 (\"What they know\"): \"the 3rd of Mirtul\" has no year: write the full in-world date with its year (<year> DR)",
            "records/hessa-kettering.json list 4 (\"History\"): \"The 5th of Mirtul\" has no year: write the full in-world date with its year (<year> DR)",
        }, errors);
    }
}
