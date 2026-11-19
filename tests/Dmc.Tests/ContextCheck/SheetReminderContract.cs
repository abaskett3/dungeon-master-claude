using System.Text;
using System.Text.Json.Nodes;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The sheet lines <c>dmc context-check</c> prints after the canon reminder (spec 016 FR-008,
/// contracts/context-check-sheets.md), in this process and through the published binary: the people the Present line or
/// the Combat tracker names who have a sheet, in that order, the field lines of either sheet shape, within the cap, again
/// only after 100,000 tokens, and silent whenever it should be.
/// </summary>
public abstract class SheetReminderContract
{
    /// <summary>The test's DMC folder, with the fixture campaign (active).</summary>
    string _root = "";

    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>The header for these people, as the hook prints it.</summary>
    static string Header(params string[] people) => $"Sheets of {string.Join("; ", people)} (what their sheets say now; read the sheet for more):";

    /// <summary>A companion's sheet with a play card, whose card holds every field and whose older section repeats two.</summary>
    const string Hala =
        "# Hala Rornsdottir\n\n- **Usually found**: the Birch bakery\n- **Status**: alive\n\n## Play card\n\n"
        + "- **Who**: Kibil's sister\n- **Looks**: silver, grey wool hood\n- **AC, max HP, speed, saves, senses**: AC 13; speed 30 ft, no fly speed\n"
        + "- **Attacks**: bite +3\n- **Standing traits that change play**: NOT immune to cold\n- **Items used in play**: the Ember\n\n"
        + "## Playing them\n\n- **Appearance**: an older line, not printed\n- **Speed**: 30 ft, not printed\n\n## History\n\n- s59: shown the glide\n";

    /// <summary>A townsperson's sheet of the older shape: Usually found, Appearance and Traits, no Items.</summary>
    const string Tam =
        "# Tam Birch\n\n- **Usually found**: the bakery ovens\n\n## Playing them\n\n- **Appearance**: flour to the elbows\n"
        + "<!-- - **Traits**: a commented line is not a field -->\n- **Traits**: cannot swim\n\n## History\n\n- s3: met Shale\n";

    /// <summary>Makes the DMC folder with Hala's and Tam's sheets, a PC's, and a game state naming them.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewDmcFolderWithCampaign();
        Write(_root, "character-sheets/npc-hala-rornsdottir.md", Hala);
        Write(_root, "character-sheets/npc-tam-birch.md", Tam);
        Write(_root, "character-sheets/npc-hala-rornsdottir-history.md", "# Hala Rornsdottir: history\n\n- **Looks**: never read\n");
        Write(_root, "character-sheets/pc-shale.md", "# Shale\n\n- **Appearance**: the PC's, never printed\n");
        Present("Shale, Tam, Hala, the whole town");
    }

    /// <summary>Deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>Writes the game state with this Present line and Combat tracker.</summary>
    void Present(string present, string tracker = "No fight in progress.") =>
        Write(_root, "game-state.md", GameState(["Shale"], present, tracker: tracker));

    /// <summary>The hook's JSON for a prompt in the test's DMC folder.</summary>
    string Input(string prompt, string log = "") =>
        new JsonObject { ["prompt"] = prompt, ["cwd"] = _root, ["transcript_path"] = log }.ToJsonString();

    /// <summary>A session log of these records, one JSON object a line.</summary>
    string Log(params JsonObject[] records)
    {
        string path = Path.Combine(_root, "session.jsonl");
        File.WriteAllText(path, string.Join("\n", records.Select(r => r.ToJsonString())) + "\n");
        return path;
    }

    /// <summary>An assistant call at this context, optionally reading a file.</summary>
    static JsonObject Call(long context, string? reads = null)
    {
        var content = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = "ok" } };
        if (reads is not null)
            content.Add(new JsonObject { ["type"] = "tool_use", ["id"] = "t1", ["name"] = "Read", ["input"] = new JsonObject { ["file_path"] = reads } });
        return new JsonObject
        {
            ["type"] = "assistant",
            ["message"] = new JsonObject
            {
                ["content"] = content,
                ["usage"] = new JsonObject { ["input_tokens"] = 2, ["cache_read_input_tokens"] = context - 2, ["cache_creation_input_tokens"] = 0, ["output_tokens"] = 9 },
            },
        };
    }

    /// <summary>The log record of output the hook printed.</summary>
    static JsonObject Printed(string text) => new()
    {
        ["type"] = "attachment",
        ["attachment"] = new JsonObject { ["type"] = "hook_success", ["hookEvent"] = "UserPromptSubmit", ["content"] = text },
    };

    /// <summary>A compaction boundary record.</summary>
    static JsonObject Boundary() => new() { ["type"] = "system", ["subtype"] = "compact_boundary" };

    /// <summary>The sheet part of a run's output: its header and the lines after it.</summary>
    static List<string> SheetPart(ProcessResult result) =>
        result.StdoutLines.SkipWhile(l => !l.StartsWith("Sheets of ", StringComparison.Ordinal)).TakeWhile(l => !l.StartsWith("Out of character", StringComparison.Ordinal)).ToList();

    /// <summary>Everyone present with a sheet gets their field lines, in the order the Present line names them.</summary>
    [TestMethod]
    public void ThePeoplePresentGetTheirFieldLinesInThePresentLinesOrder()
    {
        var result = ContextCheck(Input("I look around"));
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[]
        {
            Header("Tam Birch", "Hala Rornsdottir"),
            "character-sheets/npc-tam-birch.md | - **Appearance**: flour to the elbows",
            "character-sheets/npc-tam-birch.md | - **Usually found**: the bakery ovens",
            "character-sheets/npc-tam-birch.md | - **Traits**: cannot swim",
            "character-sheets/npc-hala-rornsdottir.md | - **Looks**: silver, grey wool hood",
            "character-sheets/npc-hala-rornsdottir.md | - **Usually found**: the Birch bakery",
            "character-sheets/npc-hala-rornsdottir.md | - **AC, max HP, speed, saves, senses**: AC 13; speed 30 ft, no fly speed",
            "character-sheets/npc-hala-rornsdottir.md | - **Standing traits that change play**: NOT immune to cold",
            "character-sheets/npc-hala-rornsdottir.md | - **Items used in play**: the Ember",
        }, SheetPart(result));
    }

    /// <summary>A combatant in the tracker counts as present; a crowd or a name with no sheet prints nothing.</summary>
    [TestMethod]
    public void TheCombatTrackerCountsAndACrowdOrANameWithNoSheetPrintsNothing()
    {
        Present("Shale, the whole town, Oswin the ferryman", tracker: "- Hala: HP 22 / 22, AC 13");
        CollectionAssert.AreEqual(new[] { Header("Hala Rornsdottir") }, SheetPart(ContextCheck(Input("I look around"))).Take(1).ToList());
        Present("Shale, the whole town, Oswin the ferryman");
        Assert.HasCount(0, SheetPart(ContextCheck(Input("I look around"))));
    }

    /// <summary>A present person whose sheet holds none of the fields is handed to the canon reminder.</summary>
    [TestMethod]
    public void ASheetWithNoneOfTheFieldsFallsBackToItsCanonLines()
    {
        Write(_root, "character-sheets/npc-oswin-reed.md", "# Oswin Reed\n\n- **Role in the story**: the ferryman\n");
        Write(_root, "story/canon.md", "# Canon\n\n## People\n\n- **Oswin Reed** (also: the ferryman): keeps the Marrow ferry (s3).\n");
        Write(_root, "story/canon-archive.md", "# Canon archive\n\n## Timeline\n\n- Oswin's old raft sank at the weir in spring (s1.4).\n");
        Present("Shale, Oswin");
        var lines = ContextCheck(Input("I look around")).StdoutLines;
        StringAssert.StartsWith(lines[0], "Canon on Oswin Reed (", "canon covers him though the message does not name him");
        Assert.IsFalse(lines.Any(l => l.StartsWith("Sheets of ", StringComparison.Ordinal)));
    }

    /// <summary>The lines stay within 10,000 bytes; a person who does not fit is left out whole, header included.</summary>
    [TestMethod]
    public void TheLinesStayWithinTheCapAndAPersonWhoDoesNotFitIsLeftOutOfTheHeader()
    {
        var names = new List<string>();
        for (int i = 0; i < 8; i++)
        {
            string name = $"Person{i} Reed";
            names.Add($"Person{i}");
            string field = new('x', 400);
            Write(_root, $"character-sheets/npc-person{i}.md",
                $"# {name}\n\n- **Usually found**: {field}\n\n## Play card\n\n- **Looks**: {field}\n- **AC, max HP, speed, saves, senses**: {field}\n"
                + $"- **Standing traits that change play**: {field}\n- **Items used in play**: {field}\n");
        }
        Present("Shale, " + string.Join(", ", names));
        var result = ContextCheck(Input("I look around"));
        var part = SheetPart(result);
        Assert.IsLessThanOrEqualTo(10_000, Encoding.UTF8.GetByteCount(string.Join("\n", part) + "\n"));
        StringAssert.StartsWith(part[0], "Sheets of Person0 Reed; ");
        Assert.DoesNotContain("Person7 Reed", part[0], "the last does not fit");
        Assert.IsFalse(part.Any(l => l.Contains("npc-person7.md", StringComparison.Ordinal)), "and none of its lines are printed");
    }

    /// <summary>A person shown within the last 100,000 tokens is skipped, and shown again once that has passed.</summary>
    [TestMethod]
    public void APersonShownWithinTheLast100000TokensIsSkippedThenShownAgain()
    {
        string printed = Header("Tam Birch") + "\ncharacter-sheets/npc-tam-birch.md | x";
        string log = Log(Call(50_000), Printed(printed), Call(140_000));
        CollectionAssert.AreEqual(new[] { Header("Hala Rornsdottir") }, SheetPart(ContextCheck(Input("I look around", log))).Take(1).ToList());
        log = Log(Call(50_000), Printed(printed), Call(150_000));
        Assert.AreEqual(Header("Tam Birch", "Hala Rornsdottir"), SheetPart(ContextCheck(Input("I look around", log)))[0]);
    }

    /// <summary>A person whose sheet the session read within the last 100,000 tokens is skipped.</summary>
    [TestMethod]
    public void ASheetTheSessionReadWithinTheLast100000TokensIsSkipped()
    {
        string sheet = Path.Combine(CampaignFolder(_root), "character-sheets", "npc-hala-rornsdottir.md");
        string log = Log(Call(60_000, reads: sheet), Call(120_000));
        CollectionAssert.AreEqual(new[] { Header("Tam Birch") }, SheetPart(ContextCheck(Input("I look around", log))).Take(1).ToList());
        log = Log(Call(60_000, reads: sheet), Call(160_000));
        Assert.AreEqual(Header("Tam Birch", "Hala Rornsdottir"), SheetPart(ContextCheck(Input("I look around", log)))[0]);
    }

    /// <summary>After a compaction boundary, nothing shown or read before it counts.</summary>
    [TestMethod]
    public void ACompactionStartsAfresh()
    {
        string log = Log(Call(50_000), Printed(Header("Tam Birch", "Hala Rornsdottir")), Boundary(), Call(60_000));
        Assert.AreEqual(Header("Tam Birch", "Hala Rornsdottir"), SheetPart(ContextCheck(Input("I look around", log)))[0]);
    }

    /// <summary>
    /// The first message after a <c>/clear</c>, before DMC has answered, gets no sheet lines: the resume that comes next
    /// reads the sheets (spec 019 FR-005). With no log named, the lines print as before.
    /// </summary>
    [TestMethod]
    public void NoSheetLinesBeforeTheFirstReplyAfterAClear()
    {
        string log = Log(ContextCheckContract.User("<command-name>/clear</command-name>"));
        Assert.HasCount(0, SheetPart(ContextCheck(Input("Resume campaign.", log))));
        Assert.AreEqual(Header("Tam Birch", "Hala Rornsdottir"), SheetPart(ContextCheck(Input("Resume campaign.")))[0]);
    }

    /// <summary>While a compaction boundary is the latest thing in the log, no sheet lines print (spec 019 FR-005).</summary>
    [TestMethod]
    public void NoSheetLinesWhileACompactionIsTheLatestThing() =>
        Assert.HasCount(0, SheetPart(ContextCheck(Input("I look around", Log(Call(50_000), Boundary())))));

    /// <summary>A slash command, input that is not usable, or a closed campaign gets no sheet lines.</summary>
    [TestMethod]
    public void NothingIsPrintedForACommandBadInputOrAClosedCampaign()
    {
        Assert.AreEqual("", ContextCheck(Input("/dmc-campaign-full-save")).Stdout);
        foreach (string input in new[] { "not json", "[]", "{\"prompt\": 3}" })
            Assert.AreEqual("", ContextCheck(input).Stdout, input);
        Write(_root, ".closed", "2026-11-16T00:00:00Z closing.zip\n");
        Assert.HasCount(0, SheetPart(ContextCheck(Input("I look around"))));
    }
}

/// <summary>The sheet lines' tests against the library code, in this process.</summary>
[TestClass]
public sealed class SheetReminderInProcessTests : SheetReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> in this process.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The sheet lines' tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class SheetReminderBlackBoxTests : SheetReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}
