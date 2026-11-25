using System.Text;
using System.Text.Json.Nodes;
using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The canon reminder <c>dmc context-check</c> prints after its break line (spec 013 FR-015 to FR-017,
/// contracts/context-check-canon.md), in this process and through the published binary: new people only, once per
/// session part, within its cap, from canon outside the always-read sections and the archive, and silent whenever it
/// should be.
/// </summary>
public abstract class CanonReminderContract
{
    /// <summary>The test's DMC folder, with the fixture campaign (active).</summary>
    string _root = "";

    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>A canon whose People hold Oswin and Kole; <paramref name="padded"/> adds a Places section naming neither, to take it past <see cref="Budgets.CanonWholeBelow"/>.</summary>
    /// <param name="padded">Whether canon is large enough that the resume does not read it whole.</param>
    static string CanonText(bool padded) =>
        "# Canon\n\n## Rulings\n\n- Oswin's toll is waived for the militia (s2).\n\n## People\n\n"
        + "- **Oswin Reed** (also: the ferryman): keeps the Marrow ferry and owes Ada a crossing (s3).\n"
        + "- **Captain Emmajeen Kole** (also: Em): captain of the Pinebrook guard (s1).\n\n## Timeline\n\n"
        + "- Oswin rowed the party over in the fog and asked after his brother (s4.2).\n- Kole posted Pip on the north wall (s5.1).\n"
        + (padded ? Places : "");

    /// <summary>A Places section naming no one, about 83k bytes, that takes a canon past <see cref="Budgets.CanonWholeBelow"/> (30,400 tokens, 76,000 characters, since spec 021).</summary>
    static readonly string Places =
        "\n## Places\n\n" + string.Concat(Enumerable.Range(1, 1100).Select(i => $"- The mill at Hollow Ford number {i} grinds barley for the lower farms (s9).\n"));

    /// <summary>Makes the DMC folder with a canon, past the size read whole, whose People hold Oswin and Kole.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewDmcFolderWithCampaign();
        Write(_root, "story/canon.md", CanonText(padded: true));
        Write(_root, "story/canon-archive.md", "# Canon archive\n\n## Timeline\n\n- Oswin's old raft sank at the weir in spring (s1.4).\n");
    }

    /// <summary>Deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>The hook's JSON for a prompt in the test's DMC folder.</summary>
    /// <param name="prompt">The player's message.</param>
    /// <param name="log">The session log's path, or empty.</param>
    string Input(string prompt, string log = "") =>
        new JsonObject { ["prompt"] = prompt, ["cwd"] = _root, ["transcript_path"] = log }.ToJsonString();

    /// <summary>A session log of these records, one JSON object a line.</summary>
    string Log(params JsonObject[] records)
    {
        string path = Path.Combine(_root, "session.jsonl");
        File.WriteAllText(path, string.Join("\n", records.Select(r => r.ToJsonString())) + "\n");
        return path;
    }

    /// <summary>The log record of a reminder the hook printed.</summary>
    static JsonObject Printed(string text) => new()
    {
        ["type"] = "attachment",
        ["attachment"] = new JsonObject { ["type"] = "hook_success", ["hookEvent"] = "UserPromptSubmit", ["content"] = text },
    };

    /// <summary>A compaction boundary record.</summary>
    static JsonObject Boundary() => new() { ["type"] = "system", ["subtype"] = "compact_boundary" };

    [TestMethod]
    public void ANamedPersonGetsAHeaderAndTheirBestLinesFromCanonAndTheArchive()
    {
        var result = ContextCheck(Input("Let's ask Oswin about the fog"));
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        var lines = result.StdoutLines;
        Assert.AreEqual("Canon on Oswin Reed (a reminder of some lines, not all canon says; recall for more):", lines[0]);
        Assert.HasCount(4, lines, "the header and three lines");
        Assert.IsTrue(lines.Skip(1).All(l => l.StartsWith($"{Name} story/canon", StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(l => l.Contains("story/canon-archive.md", StringComparison.Ordinal)), "the archive is searched too");
        Assert.IsFalse(lines.Any(l => l.Contains("toll is waived", StringComparison.Ordinal)), "Rulings were read at resume");
    }

    /// <summary>A canon under the size read whole was read whole at resume, so only the archive is left to remind of (FR-015).</summary>
    [TestMethod]
    public void ASmallCanonWasReadWholeSoOnlyTheArchiveIsReminded()
    {
        Write(_root, "story/canon.md", CanonText(padded: false));
        var lines = ContextCheck(Input("Let's ask Oswin about the fog")).StdoutLines;
        Assert.HasCount(2, lines, "the header and the archive's one line");
        Assert.Contains("story/canon-archive.md", lines[1]);
    }

    /// <summary>The active campaign is the first line of <c>.active-campaign</c>, as <c>dmc size</c> reads it.</summary>
    [TestMethod]
    public void TheActiveCampaignIsTheMarksFirstLine()
    {
        File.WriteAllText(Path.Combine(_root, "adventures", ".active-campaign"), Name + "\nleft here by hand\n");
        var lines = ContextCheck(Input("Let's ask Oswin about the fog")).StdoutLines;
        Assert.AreEqual("Canon on Oswin Reed (a reminder of some lines, not all canon says; recall for more):", lines[0]);
    }

    [TestMethod]
    public void TwoPeopleAreRemindedInOneHeaderAnAliasCounts()
    {
        var lines = ContextCheck(Input("Ask the ferryman and Em")).StdoutLines;
        Assert.AreEqual("Canon on Oswin Reed; Captain Emmajeen Kole (a reminder of some lines, not all canon says; recall for more):", lines[0]);
    }

    [TestMethod]
    public void APersonRemindedInThisPartOfTheLogIsNotRemindedAgain()
    {
        string log = Log(Printed("Canon on Oswin Reed (a reminder of some lines, not all canon says; recall for more):\nx"));
        var result = ContextCheck(Input("Oswin again", log));
        Assert.AreEqual("", result.Stdout);
        StringAssert.StartsWith(ContextCheck(Input("Oswin and Kole", log)).Stdout, "Canon on Captain Emmajeen Kole (");
    }

    [TestMethod]
    public void AReminderBeforeTheLatestCompactionDoesNotCount()
    {
        string log = Log(Printed("Canon on Oswin Reed (a reminder of some lines, not all canon says; recall for more):"), Boundary());
        StringAssert.StartsWith(ContextCheck(Input("Oswin again", log)).Stdout, "Canon on Oswin Reed (");
    }

    [TestMethod]
    public void TheReminderStaysWithinItsCap()
    {
        var people = new StringBuilder();
        var timeline = new StringBuilder();
        for (int i = 0; i < 30; i++)
        {
            people.Append($"- **Person{i} Reed**: a long-winded neighbour of the ferry who talks about the weather and the eels at length (s{i + 1}).\n");
            timeline.Append($"- Person{i} rowed over again and told the same long story about the eels and the weir and the fog (s{i + 1}.1).\n");
        }
        Write(_root, "story/canon.md", "# Canon\n\n## People\n\n" + people + "\n## Timeline\n\n" + timeline + timeline.ToString().Replace("rowed", "walked") + Places);
        string prompt = string.Join(" and ", Enumerable.Range(0, 30).Select(i => $"Person{i}"));
        var result = ContextCheck(Input(prompt));
        Assert.IsLessThanOrEqualTo(4000, Encoding.UTF8.GetByteCount(result.Stdout));
        StringAssert.StartsWith(result.Stdout, "Canon on Person0 Reed; ");
    }

    [TestMethod]
    public void NothingIsPrintedWhenNoOneIsNamedOrTheMessageIsACommand()
    {
        Assert.AreEqual("", ContextCheck(Input("I look around the camp")).Stdout);
        Assert.AreEqual("", ContextCheck(Input("/dmc-campaign-full-save Oswin")).Stdout);
    }

    [TestMethod]
    public void NothingIsPrintedForAClosedCampaignNoActiveCampaignOrNoCanon()
    {
        Write(_root, ".closed", "2026-11-15T00:00:00Z closing.zip\n");
        Assert.AreEqual("", ContextCheck(Input("Oswin")).Stdout, "closed");
        File.Delete(Path.Combine(CampaignFolder(_root), ".closed"));
        File.Delete(Path.Combine(CampaignFolder(_root), "story", "canon.md"));
        Assert.AreEqual("", ContextCheck(Input("Oswin")).Stdout, "no canon");
        File.Delete(Path.Combine(_root, "adventures", ".active-campaign"));
        Assert.AreEqual("", ContextCheck(Input("Oswin")).Stdout, "no active campaign");
    }

    [TestMethod]
    public void BadInputPrintsNothingAndExitsZero()
    {
        foreach (string input in new[] { "not json", "[]", "{\"prompt\": 3}", "{\"prompt\":\"Oswin\",\"cwd\":\"/no/such/folder\"}" })
        {
            var result = ContextCheck(input);
            Assert.AreEqual(0, result.ExitCode, input);
            Assert.AreEqual("", result.Stdout + result.Stderr, input);
        }
    }
}

/// <summary>The reminder's tests against the library code, in this process.</summary>
[TestClass]
public sealed class CanonReminderInProcessTests : CanonReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> in this process.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The reminder's tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class CanonReminderBlackBoxTests : CanonReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}
