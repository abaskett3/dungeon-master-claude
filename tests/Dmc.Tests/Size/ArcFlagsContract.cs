using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.Size;

/// <summary>
/// The Arc plan flags of <c>dmc size NAME --arc</c> (spec 016 FR-026, contracts/size-arc-flags.md), in this process and
/// through the published binary: each fires and stays silent once fixed, none print for a plan that is only Character
/// arcs, and <c>--check</c>, which the resume runs, prints none of them.
/// </summary>
public abstract class ArcFlagsContract
{
    /// <summary>The test's temporary DMC folder, with a fixture campaign at session 3.</summary>
    string _root = "";

    /// <summary>Run <c>dmc size</c> with these arguments.</summary>
    protected abstract ProcessResult Size(params string[] args);

    /// <summary>A current stage that is well: begun at s2 with a budget of 4, at session 3.</summary>
    const string Current = "- Stage 2, rising action (current since s2, about 4 sessions): the tower goes up.";

    /// <summary>An antagonist whose next move has a session.</summary>
    const string Gloam = "### Gloam\n\n- **Next move**: a lure at the north ridge, by s5.\n";

    /// <summary>Makes the DMC folder with a campaign at session 3 whose plan is well, and Auntie Hush's sheet, dead.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewDmcFolderWithCampaign();
        Write(_root, "game-state.md", GameState([]));
        Write(_root, "character-sheets/npc-auntie-hush.md", "# Auntie Hush\n\n- **Status**: DEAD (s28.1, in the Cold Chamber)\n");
        Notes("- Stage 1: DONE (the raid; Hush unmasked)\n" + Current, Gloam);
    }

    /// <summary>Deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>Writes <c>dm-notes.md</c> with this Arc plan after its comment and Character arcs, and these Antagonists.</summary>
    void Notes(string plan, string antagonists) =>
        Write(_root, "story/dm-notes.md",
            "# DM notes\n\n## Arc plan\n\n<!--\nThe template's comment: current since s9, about 4 sessions, Hush.\n-->\n\n"
            + "- Character arcs:\n  - Shale: keep Kibil home.\n\n" + plan + "\n\n## Antagonists\n\n" + antagonists + "\n## Opposition\n\n| Who |\n|---|\n");

    /// <summary>The lines <c>dmc size test-campaign --arc</c> prints.</summary>
    List<string> Arc()
    {
        var result = Size(Name, "--arc", "--folder", _root);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        return [.. result.StdoutLines];
    }

    /// <summary>The fixture's plan, current and with a clock, prints nothing.</summary>
    [TestMethod]
    public void AWellKeptPlanPrintsNothing() => Assert.HasCount(0, Arc());

    /// <summary>A plan of only Character arcs and Last played, a module campaign's before its end, prints nothing.</summary>
    [TestMethod]
    public void APlanOfOnlyCharacterArcsPrintsNothing()
    {
        Write(_root, "story/dm-notes.md", "# DM notes\n\n## Arc plan\n\n- Character arcs:\n  - Shale: Auntie Hush wants Kibil.\n- Last played (as of s2): fight s2.1\n\n## Antagonists\n\n### Gloam\n\n- wants the valley\n");
        Assert.HasCount(0, Arc(), "a module campaign before its end: no file says it is finished");
        Write(_root, "story/dm-notes.md", File.ReadAllText(Path.Combine(KitTemplates, "story", "dm-notes.md")));
        Assert.HasCount(0, Arc(), "the template as it is");
    }

    /// <summary>A dead person named on a live line fires; a line that says dead or DONE does not.</summary>
    [TestMethod]
    public void ADeadPersonNamedAheadFiresAndALineSayingDeadOrDoneDoesNot()
    {
        Notes("- Stage 1: DONE (Hush unmasked)\n- Gloam's lures: Hush is dead (s28.1), so they need Gloam.\n" + Current, Gloam);
        Assert.HasCount(0, Arc());
        Notes(Current + "\n- The raising: Hush prepares the quench.", Gloam);
        CollectionAssert.AreEqual(new[] { "arc: the plan still names Auntie Hush, whose sheet says dead: revise the stages ahead (step 5, Revise)" }, Arc(),
            "named by her own name only");
        Notes(Current, Gloam + "### Auntie Hush (green hag)\n\n- **Next move**: sing Kibil away.\n");
        CollectionAssert.AreEqual(new[] { "arc: the plan still names Auntie Hush, whose sheet says dead: revise the stages ahead (step 5, Revise)" }, Arc(),
            "still in Antagonists, and a dead antagonist gets no clock line");
    }

    /// <summary>A plan with no current stage fires, then one whose current stage has no budget.</summary>
    [TestMethod]
    public void NoCurrentStageAndNoBudgetFire()
    {
        Notes("- Stage 2, rising action: the tower goes up.", Gloam);
        CollectionAssert.AreEqual(new[] { "arc: no current stage (current since sN): add it (step 5, Arc plan)" }, Arc());
        Notes("- Stage 2, rising action (current since s2): the tower goes up.", Gloam);
        CollectionAssert.AreEqual(new[] { "arc: the current stage has no budget in sessions: add one (step 5, Arc plan)" }, Arc());
    }

    /// <summary>A stage of N sessions from sK covers sK to sK+N-1: at sK+N it is past its budget.</summary>
    [TestMethod]
    public void AStagePastItsBudgetFires()
    {
        Notes("- Stage 2 (current since s1, about 2 sessions): the tower.", Gloam);
        CollectionAssert.AreEqual(new[] { "arc: the current stage began at s1 with a budget of 2 and this is s3: tighten or revise (step 5, Pace)" }, Arc(),
            "s1 and s2 were its two sessions");
        Notes("- Stage 2 (current since s1, about 3 sessions): the tower.", Gloam);
        Assert.HasCount(0, Arc(), "s3 is the budget's last session, not past it");
    }

    /// <summary>A dead person's surname or place name alone, or a living relative who shares it, does not fire; their own name does.</summary>
    [TestMethod]
    public void ASurnameOrPlaceAloneDoesNotNameTheDead()
    {
        Write(_root, "character-sheets/npc-brunna-kelderak.md", "# Warden Brunna Kelderak\n\n- **Status**: dead (a ghost, bound to her post)\n");
        Notes(Current + "\n- The climb: a coal from a Kelderak hearth lights the waystone; Hala Kelderak carries it.", Gloam);
        Assert.HasCount(0, Arc(), "Kelderak Deep and a living Kelderak are not Brunna");
        Notes(Current + "\n- The climb: Brunna's counsel on the 15th.", Gloam);
        CollectionAssert.AreEqual(new[] { "arc: the plan still names Warden Brunna Kelderak, whose sheet says dead: revise the stages ahead (step 5, Revise)" }, Arc());
    }

    /// <summary>A next move's session is read in any case: <c>By s5</c> is a clock.</summary>
    [TestMethod]
    public void AClockInCapitalsIsAClock()
    {
        Notes(Current, "### Gloam\n\n- **Next move**: By S5, a lure at the north ridge.\n");
        Assert.HasCount(0, Arc());
    }

    /// <summary>An antagonist whose next move has no session fires, in either form of the section.</summary>
    [TestMethod]
    public void AnAntagonistWithNoSessionFires()
    {
        Notes(Current, "### Gloam\n\n- **Next move**: a lure at the north ridge, when the frost comes.\n");
        CollectionAssert.AreEqual(new[] { "arc: Gloam's next move has no session (by sN): add one (step 5, Clocks)" }, Arc());
        Notes(Current, "- **Gloam** (the eldest): a lure at the north ridge.\n- **The wyvern**: hunts, by s4.\n");
        CollectionAssert.AreEqual(new[] { "arc: Gloam's next move has no session (by sN): add one (step 5, Clocks)" }, Arc(), "entries as list items");
    }

    /// <summary><c>--check</c> prints no arc line, and <c>--check</c> with <c>--arc</c> is refused.</summary>
    [TestMethod]
    public void CheckPrintsNoArcLineAndTheTwoFormsDoNotMix()
    {
        Notes("- Stage 2: Hush prepares the quench.", "### Gloam\n\n- wants the valley\n");
        Assert.HasCount(3, Arc(), "Hush named, no current stage, Gloam with no session");
        var check = Size(Name, "--check", "--folder", _root);
        Assert.AreEqual(0, check.ExitCode, check.Stderr);
        Assert.IsFalse(check.StdoutLines.Any(l => l.StartsWith("arc:", StringComparison.Ordinal)), check.Stdout);
        var both = Size(Name, "--check", "--arc", "--folder", _root);
        Assert.AreEqual(2, both.ExitCode);
        StringAssert.Contains(both.Stderr, "argument --arc: not allowed with argument --check");
    }

    /// <summary>The kit's campaign templates.</summary>
    static string KitTemplates => Path.Combine(TestEnv.Root, "kit", "templates", "campaign");
}

/// <summary>The arc flags' tests against the library code, in this process.</summary>
[TestClass]
public sealed class ArcFlagsInProcessTests : ArcFlagsContract
{
    /// <summary>Runs <c>dmc size</c> in this process.</summary>
    protected override ProcessResult Size(params string[] args) => DmcRunner.InProcess(["size", .. args]);
}

/// <summary>The arc flags' tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class ArcFlagsBlackBoxTests : ArcFlagsContract
{
    /// <summary>Runs <c>dmc size</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Size(params string[] args) => DmcRunner.BlackBox(["size", .. args]);
}
