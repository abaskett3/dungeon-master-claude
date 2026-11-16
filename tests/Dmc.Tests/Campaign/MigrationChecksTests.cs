using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>The built-in shape checks a migration names (spec 005 research R7), as unit tests over hand-made campaign files.</summary>
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

    /// <summary>
    /// A log of Last played lines, or a Mix tracker section left over, fails <c>one-last-played-line</c>.
    /// </summary>
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

    /// <summary>
    /// A Continuity section with its three lines passes <c>continuity-section</c>, whether they say none, one name or
    /// several, with or without a comment.
    /// </summary>
    [TestMethod]
    [DataRow("old-tale", "none", "none")]
    [DataRow("none", "none", "none")]
    [DataRow("none <!-- a sequel -->", "main-tale", "far-tale, near-tale")]
    public void AContinuitySectionWithItsThreeLinesPasses(string continues, string side, string shares) =>
        Assert.IsEmpty(Run("continuity-section", now: Options(
            $"## Continuity\n\n- **Continues**: {continues}\n- **Side campaign of**: {side}\n- **Shares a world with**: {shares}\n\n## Difficulty\n\n- **Lethality**: balanced\n")));

    /// <summary>A Predecessor line left beside the Continuity section fails.</summary>
    [TestMethod]
    public void ALeftoverPredecessorLineFails()
    {
        CollectionAssert.AreEqual(new[] { "continuity-section: player-options.md still has a Predecessor line: the Continuity section replaces it" },
            Run("continuity-section", now: Options("- **Predecessor**: old-tale\n\n## Continuity\n\n- **Continues**: old-tale\n- **Side campaign of**: none\n- **Shares a world with**: none\n")));
    }

    /// <summary>A file with no Continuity section fails, and its Predecessor line fails too.</summary>
    [TestMethod]
    public void AFileWithNoContinuitySectionFails()
    {
        CollectionAssert.AreEqual(new[]
        {
            "continuity-section: player-options.md still has a Predecessor line: the Continuity section replaces it",
            "continuity-section: player-options.md has no ## Continuity section",
        }, Run("continuity-section", now: Options("- **Predecessor**: none\n")));
    }

    /// <summary>
    /// A section missing a line fails once per missing line; a line under a later heading is not in the section.
    /// </summary>
    [TestMethod]
    public void AContinuitySectionMissingALineFails()
    {
        CollectionAssert.AreEqual(new[]
        {
            "continuity-section: player-options.md's Continuity section has no Side campaign of line",
            "continuity-section: player-options.md's Continuity section has no Shares a world with line",
        }, Run("continuity-section", now: Options("## Continuity\n\n- **Continues**: old-tale\n\n## Difficulty\n\n- **Shares a world with**: far-tale\n")));
    }

    /// <summary>A campaign with no <c>player-options.md</c> is not checked.</summary>
    [TestMethod]
    public void ACampaignWithNoPlayerOptionsIsNotChecked() =>
        Assert.IsEmpty(Run("continuity-section", now: new() { ["story/canon.md"] = "# Canon\n" }));

    /// <summary>
    /// The check reads the lines as <c>dmc</c> does when it follows links: a line name in another case passes, and a
    /// Predecessor line inside a comment over several lines is not a leftover.
    /// </summary>
    [TestMethod]
    public void TheCheckReadsTheLinesAsTheLinksAreRead() =>
        Assert.IsEmpty(Run("continuity-section", now: Options(
            "<!--\n- **Predecessor**: old-tale\n-->\n\n## Continuity\n\n- **continues**: old-tale\n- **Side Campaign Of**: none\n- **Shares a world with**: none\n")));

    /// <summary>A Continuity section inside a comment is no section.</summary>
    [TestMethod]
    public void AContinuitySectionInsideACommentFails() =>
        CollectionAssert.AreEqual(new[] { "continuity-section: player-options.md has no ## Continuity section" },
            Run("continuity-section", now: Options("<!--\n## Continuity\n\n- **Continues**: none\n- **Side campaign of**: none\n- **Shares a world with**: none\n-->\n")));

    [TestMethod]
    public void AnUnknownCheckIsAnError() =>
        Assert.ThrowsExactly<DmcException>(() => Run("no-such-check"));

    // ---------- companions-have-play-card and people-entries-named (spec 013, migration 006) ----------

    /// <summary>A game state whose Party status has the PC, two companions, a block with no sheet and the not-companions block.</summary>
    const string PartyState = "# Game state\n\n## Party status\n\n### Wren\n\n- **HP**: 9\n\n### Mara Quill\n\n- **HP**: 9\n\n### Vex\n\n- **HP**: 9\n\n"
        + "### The chest (s4.2)\n\n- locked\n\n### With the party, not companions\n\n- Tobin\n\n## Pending\n\n- none\n";

    /// <summary>A companion's sheet, with or without a play card as its first section.</summary>
    static string Companion(string name, bool card) =>
        $"# {name}\n\n- **Status**: alive\n\n" + (card ? "## Play card\n\n- **Who**: x\n\n" : "") + "## Playing them\n\n- **Voice**: y\n\n## History\n\n- s1\n";

    [TestMethod]
    public void CompanionsWithAPlayCardFirstPassAndThePcAndABlockWithNoSheetAreNotChecked() =>
        Assert.IsEmpty(Run("companions-have-play-card", now: new()
        {
            ["game-state.md"] = PartyState,
            ["character-sheets/pc-wren.md"] = "# Wren\n\n## Combat\n\n## History\n",
            ["character-sheets/npc-mara.md"] = Companion("Mara Quill", card: true),
            ["character-sheets/npc-vex.md"] = Companion("Vex", card: true),
            ["character-sheets/npc-tobin.md"] = Companion("Tobin", card: false),
        }));

    [TestMethod]
    public void ACompanionWithNoPlayCardOrOneNotFirstFails()
    {
        string late = "# Vex\n\n## Playing them\n\n- y\n\n## Play card\n\n- x\n";
        CollectionAssert.AreEqual(new[]
        {
            "companions-have-play-card: character-sheets/npc-mara.md: the companion Mara Quill has no ## Play card as the sheet's first section",
            "companions-have-play-card: character-sheets/npc-vex.md: the companion Vex has no ## Play card as the sheet's first section",
        }, Run("companions-have-play-card", now: new()
        {
            ["game-state.md"] = PartyState,
            ["character-sheets/npc-mara.md"] = Companion("Mara Quill", card: false),
            ["character-sheets/npc-vex.md"] = late,
        }));
    }

    /// <summary>The check finds companions in the same sheets <c>dmc size</c> does, so a sheet not named npc- is still checked.</summary>
    [TestMethod]
    public void ACompanionSheetNotNamedNpcIsCheckedAsDmcSizeFindsIt() =>
        CollectionAssert.AreEqual(new[]
        {
            "companions-have-play-card: character-sheets/mara.md: the companion Mara Quill has no ## Play card as the sheet's first section",
        }, Run("companions-have-play-card", now: new()
        {
            ["game-state.md"] = PartyState,
            ["character-sheets/mara.md"] = Companion("Mara Quill", card: false),
            ["character-sheets/npc-vex.md"] = Companion("Vex", card: true),
        }));

    /// <summary>An indented line under People is a detail of the entry above it, not an entry with no bold name.</summary>
    [TestMethod]
    public void AnIndentedPeopleLineIsNotAnEntry() =>
        Assert.IsEmpty(Run("people-entries-named", now: new()
        {
            ["story/canon.md"] = "# Canon\n\n## People\n\n- **Oswin Reed**: the ferry (s3).\n  - lives by the mill (s4).\n",
        }));

    [TestMethod]
    public void PeopleEntriesThatStartWithABoldNamePassAndOthersFail()
    {
        string canon = "# Canon\n\n## People\n\n<!-- Who exists. -->\n\n- **Oswin Reed** (also: the ferryman): the ferry (s3).\n"
            + "- Brenna runs the mill (s1).\n\n## Places\n\n- the mill on the hill (s1).\n";
        CollectionAssert.AreEqual(new[] { "people-entries-named: story/canon.md People: an entry with no bold name first: - Brenna runs the mill (s1)." },
            Run("people-entries-named", now: new() { ["story/canon.md"] = canon }), "Places is not checked");
        Assert.IsEmpty(Run("people-entries-named", now: new() { ["story/canon.md"] = "# Canon\n\n## Rulings\n" }), "no People section");
    }

    // ---------- dm-notes-have-arc-plan (migration 007, spec 014) ----------

    /// <summary>The template's Arc plan comment, whose own Character arcs line is guidance, not a plan.</summary>
    const string ArcPlanComment = "<!--\nThe current arc of a homebrew campaign.\n\n- Character arcs: for the PC and each companion, what they want.\n-->\n";

    /// <summary>Runs <c>dm-notes-have-arc-plan</c> over one <c>story/dm-notes.md</c>.</summary>
    /// <param name="notes">The file's text.</param>
    static string[] RunArcPlan(string notes) =>
        Run("dm-notes-have-arc-plan", now: new() { ["story/dm-notes.md"] = notes });

    /// <summary>A campaign with no <c>story/dm-notes.md</c> is not checked.</summary>
    [TestMethod]
    public void ACampaignWithoutDmNotesIsNotCheckedForAnArcPlan() =>
        Assert.IsEmpty(Run("dm-notes-have-arc-plan", now: new() { ["game-state.md"] = "# Game state\n" }));

    /// <summary>DM notes with no <c>## Arc plan</c> section fail, an arc under another heading included.</summary>
    [TestMethod]
    public void DmNotesWithoutAnArcPlanSectionFail() =>
        CollectionAssert.AreEqual(new[] { "dm-notes-have-arc-plan: story/dm-notes.md has no ## Arc plan section" },
            RunArcPlan("# DM notes\n\n## The central conflict\n\n## The three acts\n\n### Act 1\n\n- Character arcs: Wren: a boat.\n"));

    /// <summary>An Arc plan whose only Character arcs line is the one inside the template's comment fails.</summary>
    [TestMethod]
    public void AnArcPlanWithOnlyTheCommentsCharacterArcsLineFails() =>
        CollectionAssert.AreEqual(new[] { "dm-notes-have-arc-plan: story/dm-notes.md's Arc plan has no Character arcs line" },
            RunArcPlan($"# DM notes\n\n## Arc plan\n\n{ArcPlanComment}\n## Antagonists\n\n- Character arcs: not in the Arc plan.\n"));

    /// <summary>
    /// An Arc plan with a Character arcs line passes, plain or bold, in any case, or as a subheading (one step 2 of
    /// migration 007 may have demoted), and after a <c>### </c> subsection, which does not end the section.
    /// </summary>
    /// <param name="line">The Character arcs line.</param>
    [TestMethod]
    [DataRow("- Character arcs: Wren: a boat of her own; the ferry war is how she earns it.")]
    [DataRow("- **Character arcs**: Wren: a boat of her own.")]
    [DataRow("- character arcs: none yet")]
    [DataRow("### Character arcs\n\n- Wren: a boat of her own.")]
    [DataRow("#### Character arcs\n\n- Wren: a boat of her own.")]
    public void AnArcPlanWithACharacterArcsLinePasses(string line)
    {
        Assert.IsEmpty(RunArcPlan($"# DM notes\n\n## arc plan\n\n{ArcPlanComment}\n{line}\n\n## Antagonists\n"));
        Assert.IsEmpty(RunArcPlan($"# DM notes\n\n## Arc plan\n\n{ArcPlanComment}\n### The three acts\n\n#### Act 1\n\n{line}\n\n## Antagonists\n"),
            "a ### subsection stays inside the Arc plan");
    }

    // ---------- arc-plan-guidance-current (migration 008, spec 015) ----------

    /// <summary>The Arc plan comment of standard 8, as far as the check reads it.</summary>
    const string CurrentArcComment = "<!--\nThe current arc of a homebrew campaign.\n\n- Shape: the structure (Five Commandments: inciting incident; …).\n-->\n";

    /// <summary>The Antagonists comment of standard 8, as far as the check reads it.</summary>
    const string CurrentAntagonistsComment = "<!-- For each: what they want, their next move and the session by which it happens (`by s14`). -->\n";

    /// <summary>Runs <c>arc-plan-guidance-current</c> over one <c>story/dm-notes.md</c>.</summary>
    /// <param name="notes">The file's text.</param>
    static string[] RunGuidance(string notes) =>
        Run("arc-plan-guidance-current", now: new() { ["story/dm-notes.md"] = notes });

    /// <summary>A campaign with no <c>story/dm-notes.md</c> is not checked.</summary>
    [TestMethod]
    public void ACampaignWithoutDmNotesIsNotCheckedForTheArcGuidance() =>
        Assert.IsEmpty(Run("arc-plan-guidance-current", now: new() { ["game-state.md"] = "# Game state\n" }));

    /// <summary>DM notes with no Arc plan pass this check: <c>dm-notes-have-arc-plan</c> reports that.</summary>
    [TestMethod]
    public void DmNotesWithoutAnArcPlanPassTheGuidanceCheck() =>
        Assert.IsEmpty(RunGuidance($"# DM notes\n\n## The central conflict\n\n## Antagonists\n\n{CurrentAntagonistsComment}"));

    /// <summary>An Arc plan whose comment does not name the Five Commandments holds an older template comment.</summary>
    [TestMethod]
    public void AnArcPlanWithAnOlderCommentFails() =>
        CollectionAssert.AreEqual(new[] { "arc-plan-guidance-current: story/dm-notes.md's Arc plan has an older template comment" },
            RunGuidance($"# DM notes\n\n## Arc plan\n\n{ArcPlanComment}\n- Character arcs: none yet\n\n## Antagonists\n\n{CurrentAntagonistsComment}"));

    /// <summary>
    /// "Five Commandments" outside a comment does not count: the check looks for the template's guidance, not for an arc
    /// that names its structure.
    /// </summary>
    [TestMethod]
    public void TheFiveCommandmentsOutsideACommentDoNotCount() =>
        CollectionAssert.AreEqual(new[] { "arc-plan-guidance-current: story/dm-notes.md's Arc plan has an older template comment" },
            RunGuidance($"# DM notes\n\n## Arc plan\n\nStructure: the Five Commandments.\n\n## Antagonists\n\n{CurrentAntagonistsComment}"));

    /// <summary>DM notes with an Arc plan and no <c>## Antagonists</c> section fail.</summary>
    [TestMethod]
    public void DmNotesWithoutAnAntagonistsSectionFail() =>
        CollectionAssert.AreEqual(new[] { "arc-plan-guidance-current: story/dm-notes.md has no ## Antagonists section" },
            RunGuidance($"# DM notes\n\n## Arc plan\n\n{CurrentArcComment}\n## Opposition\n"));

    /// <summary>An Antagonists comment that does not ask for the next move is an older template comment.</summary>
    [TestMethod]
    public void AnAntagonistsSectionWithAnOlderCommentFails() =>
        CollectionAssert.AreEqual(new[] { "arc-plan-guidance-current: story/dm-notes.md's Antagonists has an older template comment" },
            RunGuidance($"# DM notes\n\n## Arc plan\n\n{CurrentArcComment}\n## Antagonists\n\n<!-- For each: what they want, what they know about the PC. -->\n\n- Mara: next move, the bell, by s7.\n"));

    /// <summary>Both comments current pass, the headings compared ignoring case, a <c>### </c> subsection kept inside.</summary>
    [TestMethod]
    public void DmNotesWithBothCommentsCurrentPass() =>
        Assert.IsEmpty(RunGuidance($"# DM notes\n\n## arc plan\n\n### Stages\n\n{CurrentArcComment}\n## ANTAGONISTS\n\n{CurrentAntagonistsComment}\n- Mara.\n"));
}
