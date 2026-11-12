using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// What <c>dmc campaign links</c> prints and returns (spec 011 contracts/campaign-links.md), checked twice (see the two
/// subclasses below): against the library code in this process, and against the published <c>dmc</c> binary. Both find the
/// DMC folder through <c>CLAUDE_PROJECT_DIR</c>. Every test checks that the run wrote nothing.
/// </summary>
public abstract class LinksCommandContract
{
    /// <summary>The test's temporary DMC folder.</summary>
    string _root = "";

    /// <summary>The DMC folder's files before the run.</summary>
    string[] _before = [];

    /// <summary>Run <c>dmc campaign</c> with these arguments.</summary>
    protected abstract ProcessResult Campaign(params string[] args);

    /// <summary>Makes the DMC folder and points <c>CLAUDE_PROJECT_DIR</c> at it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _root);
    }

    /// <summary>Checks the run wrote nothing, then deletes the folder and forgets it as the project folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            if (_before.Length > 0)
                CollectionAssert.AreEqual(_before, DmcFolders.SnapshotAll(_root), "dmc campaign links must not write anything");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
            DmcFolders.Delete(_root);
        }
    }

    /// <summary>Writes a campaign with a Continuity section.</summary>
    /// <param name="campaign">The folder name.</param>
    /// <param name="continues">The Continues value.</param>
    /// <param name="side">The Side campaign of value.</param>
    /// <param name="shares">The Shares a world with value.</param>
    void MakeCampaign(string campaign, string continues = "none", string side = "none", string shares = "none") =>
        Write(_root, $"adventures/{campaign}/player-options.md",
            $"# Player options\n\n## Rules\n\n- **Edition**: 5e 2014\n\n## Continuity\n\n- **Continues**: {continues}\n" +
            $"- **Side campaign of**: {side}\n- **Shares a world with**: {shares}\n");

    /// <summary>Takes the snapshot the cleanup compares against.</summary>
    void Arranged() => _before = DmcFolders.SnapshotAll(_root);

    /// <summary>
    /// <c>dmc campaign links NAME</c> lists the set nearest first, then by name, then the modules, then the notices,
    /// and writes nothing.
    /// </summary>
    [TestMethod]
    public void TheSetIsListedNearestFirstThenTheModulesThenTheNotices()
    {
        MakeCampaign("rorn", continues: "egg", shares: "old-west");
        MakeCampaign("egg");
        MakeCampaign("side-quest", side: "rorn");
        ClosedCampaign.Write(Path.Combine(_root, "adventures", "egg"), new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), "egg_closed_20261111090000.zip");
        Write(_root, "adventures/rorn/digests/egg/index.md", "# Digest of egg\n");
        Write(_root, "adventures/egg/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/moon/`\n");
        Write(_root, "modules/moon/index.md", "# Moon\n");
        Arranged();

        var result = Campaign("links", "rorn");

        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual(
            "egg continues 1 closed digest\nold-west world 1 missing\nside-quest side 1 open\nmodules/moon module 1\n" +
            "notice: rorn: Shares a world with old-west is not in adventures/\n", result.Stdout);
    }

    /// <summary>A campaign with no links prints <c>no links</c>.</summary>
    [TestMethod]
    public void ACampaignAloneSaysNoLinks()
    {
        MakeCampaign("rorn");
        Arranged();
        var result = Campaign("links", "rorn");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("no links\n", result.Stdout);
    }

    /// <summary>With no NAME, the active campaign's set is listed.</summary>
    [TestMethod]
    public void TheActiveCampaignIsTheDefault()
    {
        MakeCampaign("rorn", continues: "egg");
        MakeCampaign("egg");
        Write(_root, "adventures/.active-campaign", "rorn\n");
        Arranged();
        var result = Campaign("links");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("egg continues 1 open\n", result.Stdout);
    }

    /// <summary>A closed campaign's set can be listed: reading it changes nothing.</summary>
    [TestMethod]
    public void AClosedCampaignCanBeListed()
    {
        MakeCampaign("rorn", continues: "egg");
        MakeCampaign("egg");
        ClosedCampaign.Write(Path.Combine(_root, "adventures", "egg"), new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), "egg_closed_20261111090000.zip");
        Arranged();
        var result = Campaign("links", "egg");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("rorn sequel 1 open\n", result.Stdout);
    }

    /// <summary>
    /// A broken link in a closed campaign's file is still listed here, though <c>dmc size --check</c> leaves it out at
    /// resume.
    /// </summary>
    [TestMethod]
    public void AClosedCampaignsBrokenLinkIsStillListed()
    {
        MakeCampaign("rorn", continues: "egg");
        MakeCampaign("egg", continues: "gone");
        ClosedCampaign.Write(Path.Combine(_root, "adventures", "egg"), new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), "egg_closed_20261111090000.zip");
        Arranged();
        var result = Campaign("links", "rorn");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "egg continues 1 closed", "gone continues 2 missing", "notice: egg: Continues gone is not in adventures/",
        }, result.StdoutLines);
    }

    /// <summary>Past three notices, the rest are one <c>(+N more)</c> line.</summary>
    [TestMethod]
    public void MoreThanThreeNoticesAreCut()
    {
        MakeCampaign("rorn", continues: "x1", side: "x2", shares: "x3, x4, x5");
        Arranged();
        var result = Campaign("links", "rorn");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "x1 continues 1 missing", "x2 side 1 missing", "x3 world 1 missing", "x4 world 1 missing", "x5 world 1 missing",
            "notice: rorn: Continues x1 is not in adventures/", "notice: rorn: Side campaign of x2 is not in adventures/",
            "notice: rorn: Shares a world with x3 is not in adventures/", "(+2 more)",
        }, result.StdoutLines);
    }

    /// <summary>An unknown campaign is one line naming the campaigns there are, and exit 1.</summary>
    [TestMethod]
    public void AnUnknownCampaignIsOneLineAndExitOne()
    {
        MakeCampaign("rorn");
        Arranged();
        var result = Campaign("links", "nowhere");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("dmc campaign: no campaign \"nowhere\" in adventures/ (there are: rorn)", result.Stderr.TrimEnd());
    }

    /// <summary>No NAME and no active campaign is one line saying how to name one, and exit 1.</summary>
    [TestMethod]
    public void NoActiveCampaignIsOneLineAndExitOne()
    {
        MakeCampaign("rorn");
        Arranged();
        var result = Campaign("links");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("dmc campaign: no active campaign: name one (dmc campaign links NAME)", result.Stderr.TrimEnd());
    }

    /// <summary>
    /// A usage mistake (an extra argument, the <c>--check</c> the verb does not have, a value on <c>--help</c>) is one
    /// line and exit 2.
    /// </summary>
    [TestMethod]
    [DataRow("links rorn other", "unrecognized arguments: other")]
    [DataRow("links rorn --check", "unrecognized arguments: --check")]
    [DataRow("links --help=1", "argument -h/--help: ignored explicit argument '1'")]
    public void AMistakeIsOneLineAndExitTwo(string line, string message)
    {
        MakeCampaign("rorn");
        Arranged();
        var result = Campaign(line.Split(' '));
        Assert.AreEqual(2, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual($"dmc campaign: {message}", result.Stderr.TrimEnd());
    }

    /// <summary><c>dmc campaign links --help</c> shows the verb's usage.</summary>
    [TestMethod]
    public void HelpNamesTheVerb()
    {
        var result = Campaign("links", "--help");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.Contains(result.Stdout, "dmc campaign links [NAME]");
    }
}

/// <summary>The links tests against the library code, in this process.</summary>
[TestClass]
public sealed class LinksInProcessTests : LinksCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> in this process.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.InProcess(["campaign", .. args]);
}

/// <summary>The links tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class LinksBlackBoxTests : LinksCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.BlackBox(["campaign", .. args]);
}
