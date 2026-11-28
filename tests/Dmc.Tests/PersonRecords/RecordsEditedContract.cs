using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// <c>dmc records edited</c>, the check after each edit (spec 032 FR-006, FR-007 class 4, contracts/dmc-records.md § edited,
/// task T022), in this process and through the published binary: a record that breaks a rule is refused with one line and
/// exit code 2; a new quotation of the player must be in the player's messages; an edit of a sheet that changes the text
/// and credits the player is refused; a new word for two persons to one speaker stands with a note; anything else passes
/// silently, bad input included.
/// </summary>
public abstract class RecordsEditedContract
{
    /// <summary>Run <c>dmc records edited</c> with this on stdin.</summary>
    protected abstract ProcessResult Edited(string stdin);

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

    /// <summary>Edits a record on disk as the Edit tool would, and runs the hook on that edit.</summary>
    /// <param name="file">The record's file name.</param>
    /// <param name="oldText">The text replaced.</param>
    /// <param name="newText">The text it becomes.</param>
    /// <param name="log">The session's log; null for none.</param>
    ProcessResult EditRecord(string file, string oldText, string newText, string? log = null)
    {
        string path = Path.Combine(Root, "records", file);
        string text = File.ReadAllText(path);
        Assert.Contains(oldText, text);
        File.WriteAllText(path, text.Replace(oldText, newText));
        return Edited(RecordFixtures.Edited(_folder, "Edit", path, oldText, newText, log));
    }

    /// <summary>The row of Pim's record the edits below change.</summary>
    const string PimRow = """{ "kind": "name", "from": "anyone", "word": "Pim", "source": "setup" }""";

    [TestMethod]
    public void AGoodEditPassesSilently()
    {
        var result = EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + """{ "kind": "usage", "from": "Grumbald", "word": "Thimble", "source": "s3.3" }""");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout + result.Stderr);
    }

    [TestMethod]
    public void ABadRecordIsRefusedInOneLineWithExitCode2()
    {
        var result = EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + """{ "kind": "usage", "word": "Thimble", "source": "s3.3" }""");
        Assert.AreEqual(2, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        CollectionAssert.AreEqual(new[] { "dmc records: records/pim-kettering.json row 2 (\"Thimble\"): no speaker (from): say who uses the word, or anyone for a name. Fix the record now, or put it back as it was." }, result.StderrLines);
    }

    [TestMethod]
    public void ANewQuotationOfThePlayerMustBeInTheirMessages()
    {
        string log = RecordFixtures.Log(_folder, "OOC: Grumbald calls Pim \"Thimble\", he said it when she sewed his glove.");
        const string row = """{ "kind": "usage", "from": "Grumbald", "word": "Thimble", "source": "player: \"he said it when she sewed his glove\"" }""";
        var said = EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + row, log);
        Assert.AreEqual(0, said.ExitCode, said.ToString());

        const string invented = """{ "kind": "usage", "from": "Benno Kettering", "word": "Pipsqueak", "source": "player: \"Benno always calls her that\"" }""";
        var notSaid = EditRecord("pim-kettering.json", row, row + ",\n    " + invented, log);
        Assert.AreEqual(2, notSaid.ExitCode, notSaid.ToString());
        StringAssert.StartsWith(notSaid.Stderr, "dmc records: records/pim-kettering.json row 3: the player's words \"Benno always calls her that\" are not in this session's messages");
    }

    /// <summary>
    /// T058: a quotation of the player from an earlier session stays when its row moves or another row is deleted; only
    /// a quotation the edit's new text adds is looked for in this session's messages.
    /// </summary>
    [TestMethod]
    public void AnOldQuotationIsNotNewWhenItsRowMovesOrAnotherIsDeleted()
    {
        string earlier = RecordFixtures.Log(_folder, "OOC: Grumbald calls Pim \"Thimble\", he said it when she sewed his glove.");
        const string quoted = """{ "kind": "usage", "from": "Grumbald", "word": "Thimble", "source": "player: \"he said it when she sewed his glove\"" }""";
        Assert.AreEqual(0, EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + quoted, earlier).ExitCode);

        string today = RecordFixtures.Log(_folder, "We pole on downriver.");
        const string added = """{ "kind": "usage", "from": "Benno Kettering", "word": "Pipsqueak", "source": "s6.2" }""";
        var moved = EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + added, today);
        Assert.AreEqual(0, moved.ExitCode, "the quoted row is now row 3: " + moved);
        var deleted = EditRecord("pim-kettering.json", ",\n    " + added, "", today);
        Assert.AreEqual(0, deleted.ExitCode, "a deleting edit adds no quotation: " + deleted);
    }

    /// <summary>Edits a record on disk as the Edit tool would, and runs the hook with the file as it was (<c>tool_response.originalFile</c>).</summary>
    /// <param name="file">The record's file name.</param>
    /// <param name="oldText">The text replaced.</param>
    /// <param name="newText">The text it becomes.</param>
    /// <param name="log">The session's log.</param>
    ProcessResult EditRecordGivenTheOriginal(string file, string oldText, string newText, string log)
    {
        string path = Path.Combine(Root, "records", file);
        string original = File.ReadAllText(path);
        Assert.Contains(oldText, original);
        File.WriteAllText(path, original.Replace(oldText, newText));
        var input = System.Text.Json.Nodes.JsonNode.Parse(RecordFixtures.Edited(_folder, "Edit", path, oldText, newText, log))!;
        input["tool_response"] = new System.Text.Json.Nodes.JsonObject { ["filePath"] = path, ["originalFile"] = original };
        return Edited(input.ToJsonString());
    }

    /// <summary>
    /// T066 and T073: given the file as it was, a quotation whose words an edit changes is new and must be in this
    /// session's messages, an Edit of part of it or a deletion inside it; without it, the edit's own text is counted,
    /// which does not catch those (a known limit).
    /// </summary>
    [TestMethod]
    public void AQuotationAnEditChangesIsNewWhenTheHookIsGivenTheFileAsItWas()
    {
        string earlier = RecordFixtures.Log(_folder, "OOC: Grumbald calls Pim \"Thimble\", he said it when she sewed his glove.");
        const string quoted = """{ "kind": "usage", "from": "Grumbald", "word": "Thimble", "source": "player: \"he said it when she sewed his glove\"" }""";
        Assert.AreEqual(0, EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + quoted, earlier).ExitCode);

        string today = RecordFixtures.Log(_folder, "We pole on downriver.");
        var changed = EditRecordGivenTheOriginal("pim-kettering.json", "his glove", "her glove", today);
        Assert.AreEqual(2, changed.ExitCode, changed.ToString());
        StringAssert.Contains(changed.Stderr, "the player's words \"he said it when she sewed her glove\" are not in this session's messages");
        var deleted = EditRecordGivenTheOriginal("pim-kettering.json", "sewed her glove", "sewed glove", today);
        Assert.AreEqual(2, deleted.ExitCode, "a deletion inside a quotation: " + deleted);

        Assert.AreEqual(0, EditRecord("pim-kettering.json", "sewed glove", "sewed the glove", today).ExitCode, "without the file as it was, a part of a quotation is not checked");
    }

    /// <summary>
    /// T073: a record the edit before left broken can be fixed: with no record to compare against, the fixing edit's own
    /// text is counted, so an earlier session's quotation does not have to be in this session's messages.
    /// </summary>
    [TestMethod]
    public void ARecordLeftBrokenCanBeFixed()
    {
        string earlier = RecordFixtures.Log(_folder, "OOC: Grumbald calls Pim \"Thimble\", he said it when she sewed his glove.");
        const string quoted = """{ "kind": "usage", "from": "Grumbald", "word": "Thimble", "source": "player: \"he said it when she sewed his glove\"" }""";
        Assert.AreEqual(0, EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + quoted, earlier).ExitCode);

        string today = RecordFixtures.Log(_folder, "We pole on downriver.");
        var broken = EditRecordGivenTheOriginal("pim-kettering.json", PimRow + ",", PimRow, today);
        Assert.AreEqual(2, broken.ExitCode, broken.ToString());
        StringAssert.Contains(broken.Stderr, "not valid JSON");
        var fixedAgain = EditRecordGivenTheOriginal("pim-kettering.json", PimRow, PimRow + ",", today);
        Assert.AreEqual(0, fixedAgain.ExitCode, "the fix stands: " + fixedAgain);
    }

    /// <summary>T059: a row whose speaker has a sheet and no record yet stands with a note, so two records that name each other can be written one at a time.</summary>
    [TestMethod]
    public void ASpeakerWithASheetAndNoRecordYetStandsWithANote()
    {
        File.WriteAllText(Path.Combine(Root, "character-sheets", "npc-orrin-flax.md"), "# Orrin Flax\n\n## Play card\n");
        var result = EditRecord("pim-kettering.json", PimRow, PimRow + ",\n    " + """{ "kind": "usage", "from": "Orrin Flax", "word": "Pip", "source": "s3.3" }""");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var output = System.Text.Json.Nodes.JsonNode.Parse(result.Stdout)!["hookSpecificOutput"]!;
        Assert.AreEqual("dmc records: records/pim-kettering.json row 2 (\"Pip\"): \"Orrin Flax\" has a sheet and no record yet; write theirs (the full save's check refuses the row until then).", (string?)output["additionalContext"]);

        const string hessa = """{ "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" }""";
        var other = EditRecord("hessa-kettering.json", hessa, hessa + ",\n    " + """{ "kind": "usage", "from": "Grumbald", "word": "Cook", "source": "s6.1" }""");
        Assert.AreEqual(0, other.ExitCode, other.ToString());
        Assert.AreEqual("", other.Stdout, "the note is for the record edited only");

        string path = Path.Combine(Root, "records", "pim-kettering.json");
        string original = File.ReadAllText(path);
        File.WriteAllText(path, original.Replace("\"s3.3\" }", "\"s3.4\" }"));
        var input = System.Text.Json.Nodes.JsonNode.Parse(RecordFixtures.Edited(_folder, "Edit", path, "\"s3.3\" }", "\"s3.4\" }"))!;
        input["tool_response"] = new System.Text.Json.Nodes.JsonObject { ["filePath"] = path, ["originalFile"] = original };
        var again = Edited(input.ToJsonString());
        Assert.AreEqual(0, again.ExitCode, again.ToString());
        Assert.AreEqual("", again.Stdout, "the note is given once, not on every edit of the record (T078)");
    }

    [TestMethod]
    public void AWordForTwoPersonsToOneSpeakerStandsWithANote()
    {
        const string hessa = """{ "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" }""";
        var result = EditRecord("hessa-kettering.json", hessa, hessa + ",\n    " + """{ "kind": "usage", "from": "Pim Kettering", "word": "Cap", "source": "s6.1" }""");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var output = System.Text.Json.Nodes.JsonNode.Parse(result.Stdout)!["hookSpecificOutput"]!;
        Assert.AreEqual("PostToolUse", (string?)output["hookEventName"]);
        Assert.AreEqual("dmc records: to Pim Kettering, \"Cap\" now means Hessa Kettering and Ilvane Vell; the hook prints both.", (string?)output["additionalContext"]);
    }

    [TestMethod]
    public void ASheetEditThatChangesTheTextAndCreditsThePlayerIsRefused()
    {
        string sheet = Path.Combine(Root, "character-sheets", "npc-ilvane-vell.md");
        const string oldText = "gruff. \"I'll read the river, not your letters.\"";
        const string credited = "gruff and slow. \"The river reads me.\" (rewritten by the player, sheet audit)";
        var refused = Edited(RecordFixtures.Edited(_folder, "Edit", sheet, oldText, credited));
        Assert.AreEqual(2, refused.ExitCode, refused.ToString());
        StringAssert.Contains(refused.Stderr, "credits the player in the same edit");

        var plain = Edited(RecordFixtures.Edited(_folder, "Edit", sheet, oldText, "gruff and slow. \"The river reads me.\""));
        Assert.AreEqual(0, plain.ExitCode, plain.ToString());
    }

    [TestMethod]
    public void AnythingElsePassesSilently()
    {
        foreach (string input in new[]
        {
            "", "not json", "{}",
            RecordFixtures.Edited(_folder, "Edit", Path.Combine(_folder, "notes.md"), "a", "b (the player said so)"),
            RecordFixtures.Edited(_folder, "Write", Path.Combine(Root, "game-state.md"), null, "# Game state\n"),
        })
        {
            var result = Edited(input);
            Assert.AreEqual(0, result.ExitCode, input);
            Assert.AreEqual("", result.Stdout + result.Stderr, input);
        }
    }
}

/// <summary>The check after each edit, against the library code, in this process.</summary>
[TestClass]
public sealed class RecordsEditedInProcessTests : RecordsEditedContract
{
    /// <summary>Runs <c>dmc records edited</c> in this process.</summary>
    protected override ProcessResult Edited(string stdin) => DmcRunner.InProcess(["records", "edited"], stdin);
}

/// <summary>The check after each edit, against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RecordsEditedBlackBoxTests : RecordsEditedContract
{
    /// <summary>Runs <c>dmc records edited</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Edited(string stdin) => DmcRunner.BlackBox(["records", "edited"], stdin);
}
