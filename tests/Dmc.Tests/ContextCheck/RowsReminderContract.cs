using System.Text.Json.Nodes;
using Dmc.Retrieval;
using Dmc.Tests.PersonRecords;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The rows part of <c>dmc context-check</c> (spec 032 FR-005, contracts/context-check-rows.md, tasks T013, T016, T018),
/// in this process and through the published binary, on the invented fixture: the shapes of #263's three probes (a
/// speaker's word, a word only the data makes ambiguous, a name of the campaign this one continues), every candidate for
/// a word with no speaker, once per scene, and nothing for a campaign without records.
/// </summary>
public abstract class RowsReminderContract
{
    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>The temporary DMC folder of the test.</summary>
    string _folder = "";

    /// <summary>Deletes the test's folder.</summary>
    [TestCleanup]
    public void End() => DmcFolders.Delete(_folder);

    /// <summary>The output's lines for a message, in a fixture with this Present line.</summary>
    /// <param name="prompt">The player's message.</param>
    /// <param name="present">The Present line.</param>
    /// <param name="log">The session's log; null for none.</param>
    string[] Run(string prompt, string present = "Pim Kettering, Hessa Kettering", string? log = null)
    {
        if (_folder.Length == 0)
            _folder = RecordFixtures.NewFolder(present);
        return ContextCheck(RecordFixtures.Prompt(_folder, prompt, log)).StdoutLines;
    }

    [TestMethod]
    public void ASpeakersWordGivesThePersonItMeansToThem()
    {
        var lines = Run("Pim runs to Mam.");
        CollectionAssert.Contains(lines, "\"Mam\": Hessa Kettering to Pim Kettering (when she is frightened)");
        string header = lines.Single(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal));
        Assert.EndsWith("Pim Kettering; Hessa Kettering", header);
        CollectionAssert.Contains(lines, "- Pim Kettering says: \"Mam\" for Hessa Kettering, when she is frightened");
        CollectionAssert.Contains(lines, "- Pim Kettering, limits: Cannot swim yet. (s2.5)");
    }

    [TestMethod]
    public void AWordForTwoPersonsPrintsEveryCandidate()
    {
        var lines = Run("Da?", present: "nobody here");
        CollectionAssert.Contains(lines, "\"Da\": Grumbald to Benno Kettering; Ilvane Vell to Benno Kettering (as a joke, when she bosses him about)");
    }

    [TestMethod]
    public void AWordOnlyTheDataGivesAndOneOfTheCampaignItContinuesArePrinted()
    {
        CollectionAssert.Contains(Run("Nan?", present: "nobody here"), "\"Nan\": Nettlebone (their own word), never Pim Kettering");
        var mother = Run("What do we know about Mother?", present: "nobody here");
        CollectionAssert.Contains(mother, "\"Mother\": Mother Gorse to anyone");
        Assert.IsTrue(mother.Any(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal) && l.EndsWith("Mother Gorse", StringComparison.Ordinal)), string.Join("\n", mother));
    }

    [TestMethod]
    public void APersonIsShownOncePerSceneAndAgainWhenItChanges()
    {
        var first = Run("Pim waves.");
        Assert.AreEqual(1, first.Count(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal)), string.Join("\n", first));
        string header = first.Single(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal));
        string log = Path.Combine(_folder, "session.jsonl");
        // A reply after the hook's output, so the log is not fresh (a fresh log gets no rows: the resume reads them next).
        File.WriteAllText(log, Attachment(string.Join("\n", first)) + "\n" + Reply() + "\n");
        Assert.IsFalse(Run("Pim waves again.", log: log).Any(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal)), "shown again in the same scene");

        File.WriteAllText(Path.Combine(_folder, "adventures", RecordFixtures.Campaign, "game-state.md"), "# Game state\n\n## Scene\n\n- **Present**: Pim Kettering, Ilvane Vell\n");
        string again = Run("Pim waves.", log: log).Single(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal));
        Assert.AreNotEqual(header, again, "a new scene has a new key");
        Assert.EndsWith("Pim Kettering; Ilvane Vell", again);

        File.AppendAllText(log, new JsonObject { ["type"] = "system", ["subtype"] = "compact_boundary" }.ToJsonString() + "\n");
        File.WriteAllText(Path.Combine(_folder, "adventures", RecordFixtures.Campaign, "game-state.md"), "# Game state\n\n## Scene\n\n- **Present**: Pim Kettering, Hessa Kettering\n");
        Assert.IsTrue(Run("Pim waves.", log: log).Contains(header), "shown again after a compaction");
    }

    [TestMethod]
    public void ACampaignWithoutRecordsGetsNothingFromThisPart()
    {
        _folder = RecordFixtures.NewFolder(withRecords: false);
        var lines = Run("Pim runs to Mam.");
        Assert.IsFalse(lines.Any(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal) || l.StartsWith("\"Mam\"", StringComparison.Ordinal)), string.Join("\n", lines));
    }

    /// <summary>A log record of one reply of DMC, with its usage, as Claude Code writes an assistant record.</summary>
    static string Reply() => new JsonObject
    {
        ["type"] = "assistant",
        ["message"] = new JsonObject
        {
            ["role"] = "assistant",
            ["usage"] = new JsonObject { ["input_tokens"] = 20_000, ["cache_read_input_tokens"] = 0, ["cache_creation_input_tokens"] = 0, ["output_tokens"] = 40 },
            ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = "Pim waves back." }),
        },
    }.ToJsonString();

    /// <summary>A log record of the hook's output, as Claude Code writes a <c>UserPromptSubmit</c> attachment.</summary>
    /// <param name="content">The hook's output.</param>
    static string Attachment(string content) => new JsonObject
    {
        ["type"] = "attachment",
        ["attachment"] = new JsonObject { ["type"] = "hook_additional_context", ["hookEvent"] = "UserPromptSubmit", ["content"] = content },
    }.ToJsonString();
}

/// <summary>The rows part's tests against the library code, in this process.</summary>
[TestClass]
public sealed class RowsReminderInProcessTests : RowsReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> in this process.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The rows part's tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RowsReminderBlackBoxTests : RowsReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}

/// <summary>The part's cap, against the library code (task T015).</summary>
[TestClass]
public sealed class RowsReminderCapTests
{
    [TestMethod]
    public void ThePartStaysWithinItsCapWordLinesFirst()
    {
        var rows = Enumerable.Range(0, 300).Select(i => new Dmc.PersonRecords.Row("usage", "Ann", $"word{i}", "a long note on when this word is used, to fill the cap", null, "s1.1", [])).ToList();
        var index = new Dmc.PersonRecords.RecordIndex([
            new Dmc.PersonRecords.PersonRecord("c", "bo.json", "Bo", null, rows, [], []),
            new Dmc.PersonRecords.PersonRecord("c", "ann.json", "Ann", null, [new Dmc.PersonRecords.Row("name", "anyone", "Ann", null, null, "setup", [])], [], []),
        ]);
        var lines = RowsReminder.Lines(index, string.Join(" ", Enumerable.Range(0, 300).Select(i => $"word{i}")), "Ann, Bo", new HashSet<string>());
        Assert.IsLessThanOrEqualTo(RowsReminder.CapBytes, lines.Sum(l => System.Text.Encoding.UTF8.GetByteCount(l) + 1));
        Assert.IsTrue(lines.Count > 0 && lines.All(l => l.StartsWith("\"word", StringComparison.Ordinal)), "word lines fill the cap before any person block");
    }

    /// <summary>
    /// SC-003's run (2026-11-27): a person whose short name is only another's word for them is present, and the persons
    /// a bracketed note names (who is away) are not.
    /// </summary>
    [TestMethod]
    public void APersonIsPresentByAnyWordThatMeansOnlyThemAndABracketedNoteNamesNoOne()
    {
        var index = new Dmc.PersonRecords.RecordIndex([
            new Dmc.PersonRecords.PersonRecord("c", "ann.json", "Ann Reedley", null, [new Dmc.PersonRecords.Row("usage", "Bo", "Annie", null, null, "s1.1", [])], [], []),
            new Dmc.PersonRecords.PersonRecord("c", "bo.json", "Bo", null, [new Dmc.PersonRecords.Row("name", "anyone", "Bo", null, null, "setup", [])], [], []),
            new Dmc.PersonRecords.PersonRecord("c", "cy.json", "Cy", null, [new Dmc.PersonRecords.Row("name", "anyone", "Cy", null, null, "setup", [])], [], []),
        ]);
        CollectionAssert.AreEqual(new[] { "Ann Reedley", "Bo" }, RowsReminder.Present(index, "Annie, Bo. (Cy at the mill (since noon).)"));
    }

    /// <summary>T056: the scene key follows who is present, not the rest of the text, so a round of a fight is not a new scene.</summary>
    [TestMethod]
    public void TheSceneKeyChangesWithWhoIsPresentAndNothingElse()
    {
        var index = new Dmc.PersonRecords.RecordIndex([
            new Dmc.PersonRecords.PersonRecord("c", "bo.json", "Bo", null, [new Dmc.PersonRecords.Row("name", "anyone", "Bo", null, null, "setup", [])], [], []),
            new Dmc.PersonRecords.PersonRecord("c", "cy.json", "Cy", null, [new Dmc.PersonRecords.Row("name", "anyone", "Cy", null, null, "setup", [])], [], []),
        ]);
        string Key(string text) => RowsReminder.SceneKey(RowsReminder.Present(index, text));
        Assert.AreEqual(Key("Bo, Cy\n| Bo | 12/20 HP | initiative 14 |"), Key("Cy, Bo\n| Bo | 7/20 HP | initiative 14 | (Cy hidden)"));
        Assert.AreNotEqual(Key("Bo, Cy"), Key("Bo"));
    }

    /// <summary>
    /// SC-003's run (2026-11-27): the first person present has a long block (what many others call them), and in one
    /// pass it left no room for the next person's block, so that speaker's own words were never printed. Every "says"
    /// line now comes first, and the rest while it fits, within the cap.
    /// </summary>
    [TestMethod]
    public void EverySpeakersSaysLineIsPrintedBeforeTheRestOfAnyBlock()
    {
        var calledBo = Enumerable.Range(0, 40).Select(i => new Dmc.PersonRecords.Row("name", $"Speaker{i:00}", $"Bo-name-{i:00}", "a note on when", null, "s1.1", [])).ToList();
        calledBo.Add(new Dmc.PersonRecords.Row("usage", "Ann", "Bobo", null, null, "s1.2", []));
        var calledAnn = Enumerable.Range(0, 20).Select(i => new Dmc.PersonRecords.Row("name", $"Speaker{i:00}", $"Ann-name-{i:00}", "a note on when", null, "s1.1", [])).ToList();
        calledAnn.Add(new Dmc.PersonRecords.Row("usage", "Bo", "Sis", null, null, "s1.3", []));
        var index = new Dmc.PersonRecords.RecordIndex([
            new Dmc.PersonRecords.PersonRecord("c", "bo.json", "Bo", null, calledBo, [], []),
            new Dmc.PersonRecords.PersonRecord("c", "ann.json", "Ann", null, calledAnn, [new Dmc.PersonRecords.Limit("Cannot swim.", "s1.4", [])], []),
        ]);
        var lines = RowsReminder.Lines(index, "They wave.", "Bo, Ann", new HashSet<string>());
        Assert.IsLessThanOrEqualTo(RowsReminder.CapBytes, lines.Sum(l => System.Text.Encoding.UTF8.GetByteCount(l) + 1));
        CollectionAssert.Contains(lines, "- Bo says: \"Sis\" for Ann");
        CollectionAssert.Contains(lines, "- Ann says: \"Bobo\" for Bo");
        CollectionAssert.Contains(lines, "- Ann, limits: Cannot swim. (s1.4)", "a limit prints in the first pass (T057)");
        Assert.IsLessThan(lines.IndexOf("- Ann, limits: Cannot swim. (s1.4)"), lines.IndexOf("- Ann says: \"Bobo\" for Bo"), "in the block's order");
        Assert.EndsWith("Bo; Ann", lines.Single(l => l.StartsWith(RowsReminder.HeaderStart, StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(l => l.StartsWith("- Bo, called:", StringComparison.Ordinal)), "the first block's rest fits");
        Assert.IsFalse(lines.Any(l => l.StartsWith("- Ann, called:", StringComparison.Ordinal)), "the second block's rest does not fit: " + string.Join("\n", lines));
        Assert.IsLessThan(lines.FindIndex(l => l.StartsWith("- Ann", StringComparison.Ordinal)), lines.FindIndex(l => l.StartsWith("- Bo, called:", StringComparison.Ordinal)), "a person's lines stay together, in the header's order");
    }
}
