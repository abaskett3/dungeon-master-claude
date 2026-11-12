using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// What <c>dmc campaign sequel</c> prints, returns and writes (spec 011 contracts/campaign-sequel.md), checked twice (see the
/// two subclasses below): against the library code in this process, and against the published <c>dmc</c> binary. The
/// fixture campaign is set up and closed in this process; only the runs under test go through <see cref="Campaign"/>.
/// </summary>
public abstract partial class SequelCommandContract
{
    /// <summary>Run <c>dmc campaign</c> with these arguments.</summary>
    protected abstract ProcessResult Campaign(params string[] args);

    /// <summary>Runs <c>dmc campaign sequel ember-gate</c> with a form.</summary>
    /// <param name="args">The form and its options.</param>
    ProcessResult Sequel(params string[] args) => Campaign(["sequel", SequelName, .. args]);

    /// <summary>The started line.</summary>
    [GeneratedRegex(@"^started digests/grey-tarn/ \(spine: \d+ sessions, \d+ scenes; chain: (none|[a-z0-9-]+(, [a-z0-9-]+)*)\)$")]
    private static partial Regex Started();

    /// <summary>A line of <c>--list</c>.</summary>
    [GeneratedRegex(@"^unaccounted: [0-9a-f]{12} [^ ]+:\d+ \| .+$")]
    private static partial Regex Unaccounted();

    /// <summary><c>--start</c> prints the started line and the parts, and writes only under the sequel's <c>digests/</c>.</summary>
    [TestMethod]
    public void StartPrintsThePlanAndWritesOnlyTheDigest()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        string[] before = DmcFolders.SnapshotAll(root.Folder);
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.IsTrue(Started().IsMatch(result.StdoutLines[0]), result.Stdout);
        Assert.AreEqual("part 1: player-options.md, story/dm-notes.md (the brief)", result.StdoutLines[1]);
        StringAssert.StartsWith(result.StdoutLines[2], "part 2: ");
        string digests = $"adventures/{SequelName}/digests/";
        var changed = DmcFolders.SnapshotAll(root.Folder).Except(before).ToList();
        Assert.IsNotEmpty(changed);
        Assert.IsTrue(changed.All(c => c.StartsWith(digests, StringComparison.Ordinal)), string.Join("\n", changed));
        Assert.IsEmpty(before.Except(DmcFolders.SnapshotAll(root.Folder)), "nothing removed or changed outside the digest");
    }

    /// <summary><c>--list</c>, <c>--check</c> and <c>--sample</c> write nothing.</summary>
    [TestMethod]
    [DataRow("--list")]
    [DataRow("--check")]
    [DataRow("--sample")]
    public void TheOtherFormsWriteNothing(string form)
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        string[] before = DmcFolders.SnapshotAll(root.Folder);
        Sequel(form);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(root.Folder));
    }

    /// <summary><c>--list</c> says <c>nothing unaccounted</c> on a finished digest, and <c>--check</c> passes with its one line.</summary>
    [TestMethod]
    public void AFinishedDigestListsNothingAndPasses()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var list = Sequel("--list");
        Assert.AreEqual(0, list.ExitCode, list.ToString());
        Assert.AreEqual("nothing unaccounted\n", list.Stdout);
        var check = Sequel("--check");
        Assert.AreEqual(0, check.ExitCode, check.ToString());
        StringAssert.Matches(check.StdoutLines.Single(), new Regex(@"^checked digests/grey-tarn/: [\d,]+ units \([\d,]+ copied, [\d,]+ merged, [\d,]+ dropped\), \d+ files, largest [\d.]+k? tokens$"));
    }

    /// <summary><c>--list</c> stops at 40 lines, then <c>(+N more: --all)</c>; <c>--all</c> prints them all.</summary>
    [TestMethod]
    public void TheListStopsAtFortyUnlessAll()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        File.Delete(Path.Combine(DigestFolder(root), "people.md"));
        var list = Sequel("--list");
        Assert.AreEqual(0, list.ExitCode, list.ToString());
        Assert.HasCount(41, list.StdoutLines);
        Assert.IsTrue(list.StdoutLines.Take(40).All(l => Unaccounted().IsMatch(l)), list.Stdout);
        var more = Regex.Match(list.StdoutLines[40], @"^\(\+(\d+) more: --all\)$");
        Assert.IsTrue(more.Success, list.StdoutLines[40]);

        var all = Sequel("--list", "--all");
        Assert.AreEqual(0, all.ExitCode, all.ToString());
        Assert.HasCount(40 + int.Parse(more.Groups[1].Value), all.StdoutLines);
        Assert.IsTrue(all.StdoutLines.All(l => Unaccounted().IsMatch(l)));

        var check = Sequel("--check");
        Assert.AreEqual(1, check.ExitCode, check.ToString());
        Assert.AreEqual($"unaccounted: {all.StdoutLines.Length} units (dmc campaign sequel {SequelName} --list)", check.StdoutLines[0]);
        Assert.AreEqual("dmc campaign: check failed: " + (check.StdoutLines.Length == 1 ? "1 problem" : $"{check.StdoutLines.Length} problems"), check.Stderr.TrimEnd());
    }

    /// <summary><c>--sample</c> prints its header, 20 facts and 5 people.</summary>
    [TestMethod]
    public void TheSamplePrintsItsHeaderFactsAndPeople()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var result = Sequel("--sample", "--round", "3");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.Matches(result.StdoutLines[0], new Regex(@"^sample round 3 of digests/grey-tarn/ \(seed [0-9a-f]{16}\+3\): \d+ copied facts, \d+ session-only facts, 5 people$"));
        Assert.AreEqual(20, result.StdoutLines.Count(l => Regex.IsMatch(l, @"^fact \d+: \S+:\d+ \| ")));
        Assert.AreEqual(5, result.StdoutLines.Count(l => Regex.IsMatch(l, @"^person \d+: character-sheets/\S+ \| ")));
    }

    /// <summary>The six refusals, word for word, each one line on standard error and exit 1, in the contract's order.</summary>
    [TestMethod]
    [DataRow("closed", "ember-gate is closed: start a sequel to continue its story")]
    [DataRow("none", "ember-gate continues nothing: its Continues line is none")]
    [DataRow("open", "ember-gate continues open-tale, which is not closed: close it first with dmc-close-campaign")]
    [DataRow("missing", "grey-tarn's closing backup is missing: backups/<file> (it cannot be digested without it)")]
    [DataRow("damaged", "grey-tarn's closing backup fails its check: ")]
    [DataRow("changed", "grey-tarn has changed since it closed: run dmc campaign verify grey-tarn")]
    public void EachRefusalIsOneLineAndExitOne(string refusal, string message)
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        string backup = root.InBackups(ClosedCampaign.Read(root.Campaign(ClosedName))!.Backup, ClosedName);
        switch (refusal)
        {
            case "closed":
                ClosedCampaign.Write(root.Campaign(SequelName), DateTime.UtcNow, "ember-gate_closed_20261112000000.zip");
                break;
            case "none":
                root.Write("player-options.md", SequelOptions.Replace(ClosedName + " <!--", "none <!--"), SequelName);
                break;
            case "open":
                root.WriteCampaign("open-tale");
                root.Write("player-options.md", SequelOptions.Replace(ClosedName + " <!--", "open-tale <!--"), SequelName);
                break;
            case "missing":
                File.Delete(backup);
                break;
            case "damaged":
                File.WriteAllText(backup, "not a zip file");
                break;
            default:
                File.AppendAllText(Path.Combine(root.Campaign(ClosedName), "story", "canon.md"), "- Added after the close. (s3)\n");
                break;
        }
        string[] before = DmcFolders.SnapshotAll(root.Folder);
        foreach (string form in new[] { "--start", "--list", "--check", "--sample" })
        {
            var result = Sequel(form);
            Assert.AreEqual(1, result.ExitCode, $"{form}: {result}");
            Assert.AreEqual("", result.Stdout, form);
            string line = result.Stderr.TrimEnd();
            Assert.DoesNotContain("\n", line, "one line");
            StringAssert.StartsWith(line, "dmc campaign: " + message.Replace("<file>", Path.GetFileName(backup)), form);
        }
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(root.Folder), "a refusal writes nothing");
    }

    /// <summary>A usage mistake is one line and exit 2.</summary>
    [TestMethod]
    [DataRow("sequel ember-gate", "give one of --start, --list, --check or --sample")]
    [DataRow("sequel ember-gate --start --check", "give one of --start, --list, --check or --sample")]
    [DataRow("sequel ember-gate --check --all", "argument --all: only with --list")]
    [DataRow("sequel ember-gate --list --round 2", "argument --round: only with --sample")]
    [DataRow("sequel ember-gate --sample --round 0", "argument --round: must be a whole number of 1 or more, not '0'")]
    [DataRow("sequel ember-gate --sample --round two", "argument --round: must be a whole number of 1 or more, not 'two'")]
    [DataRow("sequel ember-gate --sample --round", "argument --round: expected one argument")]
    [DataRow("sequel --start", "the following arguments are required: name")]
    [DataRow("sequel ember-gate --start --yes", "unrecognized arguments: --yes")]
    public void AMistakeIsOneLineAndExitTwo(string line, string message)
    {
        using var root = new MigrationRoot();
        var result = Campaign(line.Split(' '));
        Assert.AreEqual(2, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual($"dmc campaign: {message}", result.Stderr.TrimEnd());
    }

    /// <summary><c>dmc campaign sequel --help</c> shows the four forms.</summary>
    [TestMethod]
    public void HelpNamesTheFourForms()
    {
        var result = Campaign("sequel", "--help");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        foreach (string form in new[] { "sequel NAME --start", "sequel NAME --list [--all]", "sequel NAME --check", "sequel NAME --sample [--round N]" })
            StringAssert.Contains(result.Stdout, "dmc campaign " + form);
    }
}

/// <summary>The sequel tests against the library code, in this process.</summary>
[TestClass]
public sealed class SequelInProcessTests : SequelCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> in this process.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.InProcess(["campaign", .. args]);
}

/// <summary>The sequel tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class SequelBlackBoxTests : SequelCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.BlackBox(["campaign", .. args]);
}
