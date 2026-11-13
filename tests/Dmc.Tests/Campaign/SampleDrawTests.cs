using System.Text;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The sample test's draw (spec 011 research R11; contracts/campaign-sequel.md § --sample), on the fixture of T050: the
/// same backup and round give the same 25 items, a new round a new draw; the facts split between copies and session-only
/// lines in proportion; the people come from the closing backup, at least one carried.
/// </summary>
[TestClass]
public sealed partial class SampleDrawTests
{
    /// <summary>The header line of a round.</summary>
    [GeneratedRegex(@"^sample round (\d+) of digests/grey-tarn/ \(seed ([0-9a-f]{16})\+(\d+)\): (\d+) copied facts, (\d+) session-only facts, (\d+) people$")]
    private static partial Regex Header();

    /// <summary>Runs <c>--sample</c> on the fixture's sequel.</summary>
    /// <param name="extra">More options.</param>
    static Support.ProcessResult Sample(params string[] extra) => MigrationRoot.Run(["sequel", SequelName, "--sample", .. extra]);

    /// <summary>Puts names on the digest's Carried line.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="names">The value.</param>
    static void Carry(MigrationRoot root, string names)
    {
        string index = Path.Combine(DigestFolder(root), "index.md");
        File.WriteAllText(index, File.ReadAllText(index).Replace("- **Carried**: none", "- **Carried**: " + names));
    }

    /// <summary>The same backup and round give the same draw; round 2 gives another, and the seed is the backup's first 16 hex digits plus the round.</summary>
    [TestMethod]
    public void TheSameRoundGivesTheSameDrawAndANewRoundAnother()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var first = Sample();
        Assert.AreEqual(0, first.ExitCode, first.ToString());
        Assert.AreEqual(first.Stdout, Sample().Stdout);
        Assert.AreEqual(first.Stdout, Sample("--round", "1").Stdout);
        var second = Sample("--round", "2");
        Assert.AreEqual(0, second.ExitCode, second.ToString());
        Assert.AreNotEqual(first.Stdout, second.Stdout);

        string file = ClosedCampaign.Read(root.Campaign(ClosedName))!.Backup;
        string sha = Sha(File.ReadAllBytes(root.InBackups(file, ClosedName)));
        var header = Header().Match(second.StdoutLines[0]);
        Assert.IsTrue(header.Success, second.StdoutLines[0]);
        Assert.AreEqual("2", header.Groups[1].Value);
        Assert.AreEqual(sha[..16], header.Groups[2].Value);
        Assert.AreEqual("2", header.Groups[3].Value);
    }

    /// <summary>A round prints 20 facts and 5 people, each with its file and line, and counts them on its header.</summary>
    [TestMethod]
    public void ARoundPrintsTwentyFactsAndFivePeople()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var result = Sample();
        var header = Header().Match(result.StdoutLines[0]);
        Assert.IsTrue(header.Success, result.Stdout);
        Assert.AreEqual(20, int.Parse(header.Groups[4].Value) + int.Parse(header.Groups[5].Value));
        Assert.IsGreaterThanOrEqualTo(1, int.Parse(header.Groups[5].Value), "the digest has session-only lines, so one at least is drawn");
        Assert.AreEqual("5", header.Groups[6].Value);
        var facts = result.StdoutLines.Where(l => l.StartsWith("fact ", StringComparison.Ordinal)).ToList();
        var people = result.StdoutLines.Where(l => l.StartsWith("person ", StringComparison.Ordinal)).ToList();
        Assert.HasCount(20, facts);
        Assert.HasCount(5, people);
        foreach (string fact in facts)
            StringAssert.Matches(fact, new Regex(@"^fact \d+: (people|places|factions|items|world)(-\d+)?\.md:\d+ \| \S"));
        Assert.AreEqual(facts.Count, facts.Distinct().Count(), "no fact twice");
    }

    /// <summary>The facts split between copies and session-only lines in proportion, rounded, with at least one of each when both exist.</summary>
    [TestMethod]
    [DataRow(30, 10, 15, 5)]
    [DataRow(100, 1, 19, 1)]
    [DataRow(1, 100, 1, 19)]
    [DataRow(5, 3, 5, 3)]
    [DataRow(0, 30, 0, 20)]
    [DataRow(30, 0, 20, 0)]
    [DataRow(14, 7, 13, 7)]
    public void TheFactsSplitInProportion(int copies, int sessionOnly, int wantCopies, int wantOnly) =>
        Assert.AreEqual((wantCopies, wantOnly), SampleDraw.Split(copies, sessionOnly));

    /// <summary>
    /// The people come from the closing backup's sheets (their <c># </c> titles, not <c>*-history.md</c>) and the rows of
    /// <c>minor-npcs.md</c>'s table, whether or not the digest gives them a heading.
    /// </summary>
    [TestMethod]
    public void ThePeopleComeFromTheClosingBackup()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["character-sheets/pc-rook.md"] = Encoding.UTF8.GetBytes("# Rook\n\n- **Player character**\n"),
            ["character-sheets/rook-history.md"] = Encoding.UTF8.GetBytes("# Rook: history\n"),
            ["character-sheets/minor-npcs.md"] = Encoding.UTF8.GetBytes("# Minor NPCs\n\n| Name | Role |\n|---|---|\n| Old Hesketh | woodcutter |\n"),
            ["story/canon.md"] = Encoding.UTF8.GetBytes("# Canon\n"),
        };
        var people = SampleDraw.PeopleOf(files, ["rook"]);
        CollectionAssert.AreEqual(new[] { "Old Hesketh", "Rook" }, people.Select(p => p.Text).ToArray());
        Assert.AreEqual("character-sheets/minor-npcs.md:5", people[0].Where);
        Assert.IsTrue(people[1].Copy, "Rook is carried");
    }

    /// <summary>At least one of the 5 people is carried when the Carried line names anyone, whatever the round.</summary>
    [TestMethod]
    public void OneCarriedPersonIsAlwaysDrawn()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        Carry(root, "Sela Frostborn");
        for (int round = 1; round <= 6; round++)
        {
            var result = Sample("--round", round.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            Assert.IsTrue(result.StdoutLines.Any(l => l.StartsWith("person ", StringComparison.Ordinal) && l.EndsWith("| Sela Frostborn (carried)", StringComparison.Ordinal)), result.Stdout);
        }
    }

    /// <summary>With fewer fact lines than asked, every one is drawn.</summary>
    [TestMethod]
    public void FewerLinesThanAskedAreAllDrawn()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        foreach (string file in new[] { "people.md", "places.md", "items.md", "world.md" })
            File.Delete(Path.Combine(DigestFolder(root), file));
        var result = Sample();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "fact 1: factions.md:5 | The Lantern Wardens keep the beacons lit along the tarn road. (s1.1)" },
            result.StdoutLines.Where(l => l.StartsWith("fact ", StringComparison.Ordinal)).ToArray());
    }

    /// <summary>A digest with no fact lines is one line and exit 1.</summary>
    [TestMethod]
    public void NoFactLinesIsAnError()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        Assert.AreEqual(0, MigrationRoot.Run("sequel", SequelName, "--start").ExitCode);
        var result = Sample();
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("dmc campaign: the digest has no fact lines", result.Stderr.TrimEnd());
    }

    /// <summary>A closing backup with no characters is one line and exit 1.</summary>
    [TestMethod]
    public void NoCharactersIsAnError()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true, beforeClose: campaign => Directory.Delete(Path.Combine(campaign, "character-sheets"), recursive: true));
        var result = Sample();
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("dmc campaign: the closing backup has no characters", result.Stderr.TrimEnd());
    }
}
