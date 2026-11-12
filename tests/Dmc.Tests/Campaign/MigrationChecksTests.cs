using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>The five built-in shape checks a migration names (research R7), as unit tests over hand-made campaign files.</summary>
[TestClass]
public sealed class MigrationChecksTests
{
    /// <summary>The header and separator of the template's Opposition table.</summary>
    const string Table = "| Who | How many (alive / started) | Where now | Stat block (AC, HP, attacks) | The party knows |\n|---|---|---|---|---|\n";

    /// <summary>Runs one check over an old and a new campaign.</summary>
    static string[] Run(string check, Dictionary<string, string>? old = null, Dictionary<string, string>? now = null) =>
        [.. MigrationChecks.Run(check, new CheckInput(old ?? [], now ?? [], AccountingFile.Empty))];

    // ---------- sheets-end-with-history ----------

    [TestMethod]
    [DataRow("character-sheets/pc-wren.md")]
    [DataRow("character-sheets/npc-mara.md")]
    public void ASheetWhoseHistoryIsInTheMiddleFails(string path)
    {
        var failures = Run("sheets-end-with-history", now: new() { [path] = "# X\n\n## Story\n\n## History\n\n- s1\n\n## Secrets\n\n- x\n" });
        CollectionAssert.AreEqual(new[] { $"sheets-end-with-history: {path}: ## History is not the last section (## Secrets follows it)" }, failures);
    }

    [TestMethod]
    [DataRow("# X\n\n## Story\n\n## History\n\n- s1\n")]
    [DataRow("# X\n\n## Story\n\n## Notes\n\n- s1\n")]
    [DataRow("# X\n\n## Story\n\n## History with the PC\n\n- s1\n")]
    [DataRow("# X\n\n## Story\n\n## Notes\n\n## History\n")]
    [DataRow("# X\n\n## Story\n\n- no history at all\n")]
    [DataRow("# X\n\n## History\n\n<!--\n## Not a heading\n-->\n")]
    public void ASheetEndingInHistoryOrWithoutOnePasses(string text) =>
        Assert.IsEmpty(Run("sheets-end-with-history", now: new() { ["character-sheets/pc-wren.md"] = text }));

    [TestMethod]
    public void HistoryFilesAndMinorNpcsAreNotSheets() =>
        Assert.IsEmpty(Run("sheets-end-with-history", now: new()
        {
            ["character-sheets/pc-wren-history.md"] = "## History\n\n## Later\n",
            ["character-sheets/minor-npcs.md"] = "## History\n\n## Later\n",
            ["story/canon.md"] = "## History and lore\n\n## Timeline\n",
        }));

    // ---------- npc-sheets-have-names-line ----------

    [TestMethod]
    public void AnNpcSheetWithoutTheNamesLineFailsAndOneWithItPasses()
    {
        var failures = Run("npc-sheets-have-names-line", now: new()
        {
            ["character-sheets/npc-mara.md"] = "# Mara\n\n## Playing them\n\n- **Voice**: dry\n",
            ["character-sheets/npc-vex.md"] = "# Vex\n\n## Playing them\n\n- **Names they use**: calls Wren \"the ferry rat\"\n",
            ["character-sheets/pc-wren.md"] = "# Wren\n",
        });
        CollectionAssert.AreEqual(new[] { "npc-sheets-have-names-line: character-sheets/npc-mara.md has no Names they use line" }, failures);
    }

    // ---------- no-dead-opposition-rows ----------

    [TestMethod]
    [DataRow("0 / 4")]
    [DataRow("0/3")]
    public void ADeadRowFails(string count)
    {
        var failures = Run("no-dead-opposition-rows", now: new() { ["story/dm-notes.md"] = $"# DM notes\n\n## Opposition\n\n{Table}| Goblins | {count} | the ford | AC 13 | met |\n| Trolls | 2 / 2 | marsh | AC 15 | rumours |\n" });
        CollectionAssert.AreEqual(new[] { "no-dead-opposition-rows: story/dm-notes.md Opposition still has Goblins, with none left alive: move the row to story/dm-notes-archive.md" }, failures);
    }

    [TestMethod]
    public void LiveRowsAndAnEmptyTablePass()
    {
        Assert.IsEmpty(Run("no-dead-opposition-rows", now: new() { ["story/dm-notes.md"] = $"## Opposition\n\n{Table}| Trolls | 2 / 2 | marsh | AC 15 | rumours |\n| Cult | 10 / 12 | city | AC 12 | name only |\n" }));
        Assert.IsEmpty(Run("no-dead-opposition-rows", now: new() { ["story/dm-notes.md"] = $"## Opposition\n\n<!-- comment -->\n\n{Table}\n## Secrets\n" }));
    }

    [TestMethod]
    public void NoOppositionTableFails()
    {
        string message = "no-dead-opposition-rows: no Opposition table found in story/dm-notes.md";
        CollectionAssert.AreEqual(new[] { message }, Run("no-dead-opposition-rows", now: new() { ["story/dm-notes.md"] = "# DM notes\n\n## Secrets\n" }));
        CollectionAssert.AreEqual(new[] { message }, Run("no-dead-opposition-rows", now: new() { ["story/dm-notes.md"] = "## Opposition\n\nGoblins: 4, all dead\n" }));
        CollectionAssert.AreEqual(new[] { message }, Run("no-dead-opposition-rows", now: new()));
    }

    // ---------- fights-have-timeline ----------

    /// <summary>An old <c>dm-notes.md</c> with one finished fight and one live group.</summary>
    static readonly Dictionary<string, string> OldNotes = new()
    {
        ["story/dm-notes.md"] = $"## Opposition\n\n{Table}| **Goblins of the ford** | 0 / 4 | dead | AC 13 | met |\n| Trolls | 2 / 2 | marsh | AC 15 | rumours |\n",
    };

    [TestMethod]
    public void AFinishedFightArchivedWithATimelineLinePasses() =>
        Assert.IsEmpty(Run("fights-have-timeline", OldNotes, new()
        {
            ["story/dm-notes-archive.md"] = $"# DM notes archive\n\n## Opposition\n\n{Table}| **Goblins of the ford** | 0 / 4 | dead | AC 13 | met |\n",
            ["story/canon.md"] = "## Timeline\n\n- Wren broke the goblins of the ford with one arrow. (s2.3)\n",
        }));

    [TestMethod]
    public void AFinishedFightNotArchivedOrWithoutATimelineLineFails()
    {
        var failures = Run("fights-have-timeline", OldNotes, new()
        {
            ["story/canon.md"] = "## Timeline\n\n- Wren broke the goblins of the ford with one arrow.\n\n## People\n\n- Goblins of the ford: gone. (s2)\n",
        });
        CollectionAssert.AreEqual(new[]
        {
            "fights-have-timeline: Goblins of the ford: the finished row is not in story/dm-notes-archive.md",
            "fights-have-timeline: Goblins of the ford: no line under ## Timeline in story/canon.md names it with a session reference (sN.M)",
        }, failures);
    }

    [TestMethod]
    public void NoFinishedFightInTheBackupPasses() =>
        Assert.IsEmpty(Run("fights-have-timeline", new() { ["story/dm-notes.md"] = $"## Opposition\n\n{Table}| Trolls | 2 / 2 | marsh | AC 15 | rumours |\n" }, new()));

    // ---------- one-last-played-line ----------

    [TestMethod]
    [DataRow("## Arc plan\n\nLast played (as of s12): fight s10.3, check s12.1\n")]
    [DataRow("## Arc plan\n\n- stage 1 done\n")]
    public void OneLastPlayedLineOrNonePasses(string text) =>
        Assert.IsEmpty(Run("one-last-played-line", now: new() { ["story/dm-notes.md"] = text }));

    [TestMethod]
    public void ALogOfSessionsFails()
    {
        CollectionAssert.AreEqual(new[] { "one-last-played-line: story/dm-notes.md has 2 Last played lines, where one belongs" },
            Run("one-last-played-line", now: new() { ["story/dm-notes.md"] = "- **Last played** s11: fight\n- Last played s12: puzzle\n" }));
        CollectionAssert.AreEqual(new[] { "one-last-played-line: story/dm-notes.md still has a Mix tracker section: one Last played line replaces it" },
            Run("one-last-played-line", now: new() { ["story/dm-notes.md"] = "## Mix tracker\n\n| s | fight |\n" }));
    }

    // ---------- continuity-section (spec 011, migration 005) ----------

    /// <summary>A campaign's player options with a Rules section and then <paramref name="body"/>.</summary>
    /// <param name="body">The text after the Rules section.</param>
    static Dictionary<string, string> Options(string body) =>
        new() { ["player-options.md"] = "# Player options (this campaign)\n\n## Rules\n\n- **Edition**: 5e 2014\n- **House rules**: none\n\n" + body };

    [TestMethod]
    [DataRow("old-tale", "none", "none")]
    [DataRow("none", "none", "none")]
    [DataRow("none <!-- a sequel -->", "main-tale", "far-tale, near-tale")]
    public void AContinuitySectionWithItsThreeLinesPasses(string continues, string side, string shares) =>
        Assert.IsEmpty(Run("continuity-section", now: Options(
            $"## Continuity\n\n- **Continues**: {continues}\n- **Side campaign of**: {side}\n- **Shares a world with**: {shares}\n\n## Difficulty\n\n- **Lethality**: balanced\n")));

    [TestMethod]
    public void ALeftoverPredecessorLineFails()
    {
        CollectionAssert.AreEqual(new[] { "continuity-section: player-options.md still has a Predecessor line: the Continuity section replaces it" },
            Run("continuity-section", now: Options("- **Predecessor**: old-tale\n\n## Continuity\n\n- **Continues**: old-tale\n- **Side campaign of**: none\n- **Shares a world with**: none\n")));
    }

    [TestMethod]
    public void AFileWithNoContinuitySectionFails()
    {
        CollectionAssert.AreEqual(new[]
        {
            "continuity-section: player-options.md still has a Predecessor line: the Continuity section replaces it",
            "continuity-section: player-options.md has no ## Continuity section",
        }, Run("continuity-section", now: Options("- **Predecessor**: none\n")));
    }

    [TestMethod]
    public void AContinuitySectionMissingALineFails()
    {
        CollectionAssert.AreEqual(new[]
        {
            "continuity-section: player-options.md's Continuity section has no Side campaign of line",
            "continuity-section: player-options.md's Continuity section has no Shares a world with line",
        }, Run("continuity-section", now: Options("## Continuity\n\n- **Continues**: old-tale\n\n## Difficulty\n\n- **Shares a world with**: far-tale\n")));
    }

    [TestMethod]
    public void ACampaignWithNoPlayerOptionsIsNotChecked() =>
        Assert.IsEmpty(Run("continuity-section", now: new() { ["story/canon.md"] = "# Canon\n" }));

    [TestMethod]
    public void AnUnknownCheckIsAnError() =>
        Assert.ThrowsExactly<DmcException>(() => Run("no-such-check"));
}
