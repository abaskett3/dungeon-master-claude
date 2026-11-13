using System.Text;
using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The exact parts of a sequel's import (spec 011 research R8 to R10; contracts/campaign-sequel.md): <c>--start</c>'s index,
/// spine, chain copies and parts plan; the accounted units, read from the closing backup; and <c>--check</c>'s proof, on the
/// fixture of T050. Run in this process against a temporary DMC folder; the contract's output shapes are checked against the
/// binary in <c>SequelCommandContract</c>.
/// </summary>
[TestClass]
public sealed class SequelDigestTests
{
    /// <summary>Runs <c>dmc campaign sequel ember-gate</c> with a form.</summary>
    /// <param name="args">The form and its options.</param>
    static ProcessResult Sequel(params string[] args) => MigrationRoot.Run(["sequel", SequelName, .. args]);

    /// <summary>A file of the digest's folder, as text.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="file">The file's name.</param>
    static string Digest(MigrationRoot root, string file) => File.ReadAllText(Path.Combine(DigestFolder(root), file));

    /// <summary>Writes a file of the digest's folder.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="file">The file's name.</param>
    /// <param name="text">What it holds.</param>
    static void WriteDigest(MigrationRoot root, string file, string text) =>
        File.WriteAllBytes(Path.Combine(DigestFolder(root), file), new UTF8Encoding(false).GetBytes(text));

    /// <summary>The closed campaign's closing backup: its file name and SHA-256.</summary>
    /// <param name="root">The DMC folder.</param>
    static (string File, string Sha) ClosingBackup(MigrationRoot root)
    {
        string file = ClosedCampaign.Read(root.Campaign(ClosedName))!.Backup;
        return (file, Sha(File.ReadAllBytes(root.InBackups(file, ClosedName))));
    }

    /// <summary>The accounted units of the fixture, from its closing backup.</summary>
    /// <param name="root">The DMC folder.</param>
    static List<AccountedUnit> Units(MigrationRoot root) => SequelDigest.Units(SequelDigest.Open(root.Folder, SequelName).Files);

    /// <summary>
    /// <c>--start</c> writes the index's six lines: Format 1, the campaign, the closing backup with its SHA-256, the chain,
    /// Carried <c>none</c>, and the files.
    /// </summary>
    [TestMethod]
    public void TheIndexHoldsItsSixLines()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var (file, sha) = ClosingBackup(root);
        Assert.AreEqual(
            $"# Digest of {ClosedName}\n\n- **Format**: 1\n- **Campaign**: {ClosedName}\n- **Closing backup**: {file} sha256:{sha}\n" +
            "- **Chain**: none <!-- the digests copied with it, nearest first -->\n" +
            "- **Carried**: none <!-- the characters this sequel carried over, as the closed campaign's sheets title them -->\n" +
            "- **Files**: people.md, places.md, factions.md, items.md, world.md, spine.md, secrets.md, accounting.tsv\n",
            Digest(root, "index.md"));
    }

    /// <summary>
    /// The spine holds, per log, its title and file, then each scene heading's text after <c>Scene M: </c>, word for word in
    /// every shape the logs use; a log with no scene headings has its session line alone.
    /// </summary>
    [TestMethod]
    public void TheSpineHoldsEachScenesHeadingWordForWord()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        Assert.AreEqual(0, Sequel("--start").ExitCode);
        Assert.AreEqual(
            $"# Spine of {ClosedName}\n\n" +
            "## Session 1 (session-001.md)\n\n" +
            "- s1.1 Arrival (Tarnhold, Day 1 morning)\n- s1.2 The yard bout (Day 1)\n- s1.3 Thin ice (Grey Tarn, north shore, Day 1, dusk, snow)\n\n" +
            "## Session 2: The Ember Wood (session-002.md)\n\n" +
            "- s2.1 South road (Ember Wood, Day 3)\n- s2.2 The witch's hut (Ember Wood, Day 3, noon)\n- s2.3 The curse (Ember Wood, Day 3, dusk)\n\n" +
            "## Session 3 (void) (session-003.md)\n",
            Digest(root, "spine.md"));
    }

    /// <summary>The started line counts the sessions and scenes; with every source under 100 KB the plan is the brief, then one part.</summary>
    [TestMethod]
    public void TheStartedLineAndThePlanCoverEverySourceOnce()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            $"started digests/{ClosedName}/ (spine: 3 sessions, 6 scenes; chain: none)",
            "part 1: player-options.md, story/dm-notes.md (the brief)",
            "part 2: story/canon.md, story/canon-archive.md, character-sheets/ (5 files), story/summary.md, module-progress.md, story/sessions/ (3 files)",
        }, result.StdoutLines);
    }

    /// <summary>A source over 100 KB is split only at its <c>## </c> headings, its ranges cover it once, and a source the campaign lacks is left out.</summary>
    [TestMethod]
    public void ALargeSourceIsSplitAtItsHeadingsAndAMissingOneIsLeftOut()
    {
        using var root = new MigrationRoot();
        var archive = new StringBuilder("# Canon archive\n");
        for (int section = 1; section <= 30; section++)
        {
            archive.Append("\n## Subject ").Append(section).Append('\n');
            for (int line = 0; line < 50; line++)
                archive.Append("- Fact ").Append(section).Append('.').Append(line).Append(": ").Append(new string('x', 80)).Append(" (s1)\n");
        }
        WriteSequel(root, beforeClose: campaign =>
        {
            File.WriteAllText(Path.Combine(campaign, "story", "canon-archive.md"), archive.ToString());
            File.Delete(Path.Combine(campaign, "module-progress.md"));
        });
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());

        string[] lines = archive.ToString().TrimEnd('\n').Split('\n');
        var ranges = result.StdoutLines.SelectMany(l => l.Split(", ")).Select(p => p.Replace("part 2: ", "").Replace("part 3: ", "").Replace("part 4: ", ""))
            .Where(p => p.StartsWith("story/canon-archive.md ", StringComparison.Ordinal))
            .Select(p => p["story/canon-archive.md ".Length..].Split('-').Select(int.Parse).ToArray()).ToList();
        Assert.IsGreaterThan(1, ranges.Count, result.Stdout);
        int next = 1;
        foreach (var range in ranges)
        {
            Assert.AreEqual(next, range[0], "the ranges follow on");
            Assert.IsTrue(range[0] == 1 || lines[range[0] - 1].StartsWith("## ", StringComparison.Ordinal), $"line {range[0]} starts a section");
            next = range[1] + 1;
        }
        Assert.AreEqual(lines.Length + 1, next, "the ranges cover the file");
        Assert.DoesNotContain("module-progress.md", result.Stdout);
        Assert.IsFalse(Units(root).Any(u => u.Path == "module-progress.md"), "a missing source counts no units");
    }

    /// <summary>The digests the closed campaign holds are copied byte for byte and named on the Chain line.</summary>
    [TestMethod]
    public void TheClosedCampaignsDigestsAreCopiedByteForByte()
    {
        using var root = new MigrationRoot();
        byte[] people = Encoding.UTF8.GetBytes("# People of old-tale\r\n\r\n## Mara\r\n\r\n- Mara keeps the ferry. (s1)\r\n");
        WriteSequel(root, beforeClose: campaign =>
        {
            Write(campaign, "digests/old-tale/index.md", Encoding.UTF8.GetBytes("# Digest of old-tale\n\n- **Campaign**: old-tale\n- **Chain**: none\n"));
            Write(campaign, "digests/old-tale/people.md", people);
        });
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.EndsWith(result.StdoutLines[0], "chain: old-tale)");
        CollectionAssert.AreEqual(people, File.ReadAllBytes(Path.Combine(root.Campaign(SequelName), "digests", "old-tale", "people.md")));
        StringAssert.Contains(Digest(root, "index.md"), "- **Chain**: old-tale <!--");
    }

    /// <summary>A second <c>--start</c> keeps the Carried line and rewrites the lines it generates.</summary>
    [TestMethod]
    public void ARerunKeepsCarriedAndRewritesTheRest()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        Assert.AreEqual(0, Sequel("--start").ExitCode);
        string spine = Digest(root, "spine.md");
        WriteDigest(root, "index.md", Digest(root, "index.md").Replace("- **Carried**: none", "- **Carried**: Rook, Sela Frostborn").Replace("- **Format**: 1", "- **Format**: 9"));
        WriteDigest(root, "spine.md", "spoiled\n");

        Assert.AreEqual(0, Sequel("--start").ExitCode);
        string index = Digest(root, "index.md");
        StringAssert.Contains(index, "- **Carried**: Rook, Sela Frostborn <!--");
        StringAssert.Contains(index, "- **Format**: 1\n");
        Assert.AreEqual(spine, Digest(root, "spine.md"));
    }

    /// <summary><c>--start</c> refuses a digest folder whose index names another campaign.</summary>
    [TestMethod]
    public void AnotherCampaignsDigestIsNeverStartedOver()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        Directory.CreateDirectory(DigestFolder(root));
        WriteDigest(root, "index.md", "# Digest of other\n\n- **Campaign**: other\n");
        var result = Sequel("--start");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual($"dmc campaign: digests/{ClosedName}/ holds a digest of other: it cannot be started over for {ClosedName}", result.Stderr.TrimEnd());
    }

    /// <summary>No form of the verb changes a byte of the closed campaign (FR-016).</summary>
    [TestMethod]
    public void NoFormChangesTheClosedCampaign()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        string[] before = DmcFolders.SnapshotAll(root.Campaign(ClosedName));
        Sequel("--start");
        Sequel("--list");
        Sequel("--check");
        Sequel("--sample");
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(root.Campaign(ClosedName)));
    }

    /// <summary>The units come from the closing backup: a closed campaign edited after closing is refused, not read.</summary>
    [TestMethod]
    public void AClosedCampaignEditedAfterClosingIsRefused()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        File.AppendAllText(Path.Combine(root.Campaign(ClosedName), "story", "canon.md"), "- A line added after the close. (s3)\n");
        var result = Sequel("--list");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual($"dmc campaign: {ClosedName} has changed since it closed: run dmc campaign verify {ClosedName}", result.Stderr.TrimEnd());
    }

    /// <summary>
    /// Only the FR-021 sources count: every line of every sheet; § Secrets of the DM notes and § How it ended of the summary
    /// only; never the game state, the player options or a session log.
    /// </summary>
    [TestMethod]
    public void OnlyTheAccountedSourcesCount()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        var units = Units(root);
        var texts = units.Select(u => u.Text).ToHashSet();
        Assert.Contains("Playing them", texts, "a sheet's headings count");
        Assert.Contains("| Name | Role | Where | Distinguishing trait or voice | Attitude to the PC | First met |", texts, "a table's header counts");
        Assert.Contains("Marta the ferrywoman is the Ember Witch's sister, and she knows who killed her.", texts);
        Assert.Contains("The yard bout of s1.2 was a test fight and is not canon.", texts);
        Assert.DoesNotContain("Enjoys bargaining for prices.", texts, "the DM notes count only their secrets");
        Assert.DoesNotContain("The witch is dead; the bell under the tarn rings at night.", texts, "the summary counts only How it ended");
        Assert.DoesNotContain("**Location**: Tarnhold, the Drowned Lantern inn", texts, "the game state does not count");
        Assert.IsFalse(units.Any(u => u.Path.StartsWith("story/sessions/", StringComparison.Ordinal) || u.Path == "player-options.md"));
        var secret = units.Single(u => u.Text.StartsWith("Marta the ferrywoman", StringComparison.Ordinal));
        Assert.AreEqual("story/dm-notes.md", secret.Path);
        Assert.AreEqual(25, secret.Line, "a section's line keeps its number in the whole file");
    }

    /// <summary>The fixture's digest passes, with the counts line.</summary>
    [TestMethod]
    public void TheFixturesDigestPasses()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var result = Sequel("--check");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual($"checked digests/{ClosedName}/: 109 units (75 copied, 0 merged, 34 dropped), 9 files, largest 754 tokens", result.StdoutLines.Single());
    }

    /// <summary>A line of 20 characters or more inside a longer digest line counts as copied.</summary>
    [TestMethod]
    public void ALineInsideALongerOneIsCopied()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        WriteDigest(root, "places.md", Digest(root, "places.md").Replace(
            "- Tarnhold is a walled village on the north shore of Grey Tarn. (s1.1)",
            "- Tarnhold is a walled village on the north shore of Grey Tarn. (s1.1) Its gate faces the water."));
        Assert.AreEqual(0, Sequel("--check").ExitCode);
    }

    /// <summary>One copied line deleted is one unaccounted unit, and <c>--list</c> names it with its source line.</summary>
    [TestMethod]
    public void ALostLineIsUnaccounted()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        WriteDigest(root, "people.md", Digest(root, "people.md").Replace("- **Personality**: gruff, fair, proud of his work\n", ""));
        var check = Sequel("--check");
        Assert.AreEqual(1, check.ExitCode, check.ToString());
        CollectionAssert.AreEqual(new[] { $"unaccounted: 1 units (dmc campaign sequel {SequelName} --list)" }, check.StdoutLines);
        Assert.AreEqual("dmc campaign: check failed: 1 problem", check.Stderr.TrimEnd());

        var list = Sequel("--list");
        Assert.AreEqual(0, list.ExitCode, list.ToString());
        string id = ContentUnits.Id("**Personality**: gruff, fair, proud of his work");
        CollectionAssert.AreEqual(new[] { $"unaccounted: {id} character-sheets/npc-brannoc.md:14 | **Personality**: gruff, fair, proud of his work" }, list.StdoutLines);
    }

    /// <summary>A merged unit is accounted when its anchor is in the digest, and named when it is not.</summary>
    [TestMethod]
    public void AMergedUnitNeedsItsAnchorInTheDigest()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        string line = "- **Personality**: gruff, fair, proud of his work\n";
        string id = ContentUnits.Id("**Personality**: gruff, fair, proud of his work");
        WriteDigest(root, "people.md", Digest(root, "people.md").Replace(line, "- Gruff and fair, and proud of his work.\n"));
        File.AppendAllText(Path.Combine(DigestFolder(root), "accounting.tsv"), $"{id}\tmerged\tGruff and fair\n");
        var pass = Sequel("--check");
        Assert.AreEqual(0, pass.ExitCode, pass.ToString());
        StringAssert.Contains(pass.Stdout, "(74 copied, 1 merged, 34 dropped)");

        WriteDigest(root, "people.md", Digest(root, "people.md").Replace("- Gruff and fair, and proud of his work.\n", ""));
        var fail = Sequel("--check");
        CollectionAssert.AreEqual(new[] { $"accounting: {id}: anchor not found in the digest: Gruff and fair" }, fail.StdoutLines);
    }

    /// <summary>An accounting line for an unknown id, or with a class no digest declares, is named.</summary>
    [TestMethod]
    public void AnUnknownIdOrClassIsNamed()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        string id = ContentUnits.Id("**Personality**: gruff, fair, proud of his work");
        WriteDigest(root, "people.md", Digest(root, "people.md").Replace("- **Personality**: gruff, fair, proud of his work\n", ""));
        File.AppendAllText(Path.Combine(DigestFolder(root), "accounting.tsv"), $"{id}\tdropped:mix-tracker\tno reason\n000000000000\tdropped:structure\tnothing\n");
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            $"accounting: {id}: dropped:mix-tracker is not a declared class (superseded, not-canon, structure, bookkeeping, stat-block)",
            $"accounting: 000000000000: not a unit of {ClosedName}'s accounted files",
        }, result.StdoutLines);
    }

    /// <summary>One byte changed in a chain copy is named.</summary>
    [TestMethod]
    public void AChangedChainCopyIsNamed()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true, beforeClose: campaign => Write(campaign, "digests/old-tale/people.md", Encoding.UTF8.GetBytes("# People\n\n## Mara\n")));
        string copy = Path.Combine(root.Campaign(SequelName), "digests", "old-tale", "people.md");
        File.WriteAllText(copy, "# People\n\n## Mora\n");
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "copy differs: digests/old-tale/people.md" }, result.StdoutLines);
    }

    /// <summary>An index line missing, or a checksum that is not the closing backup's, is named.</summary>
    [TestMethod]
    public void TheIndexMustBeWholeAndItsChecksumTheBackups()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var (file, sha) = ClosingBackup(root);
        string index = Digest(root, "index.md");
        WriteDigest(root, "index.md", index.Replace($"sha256:{sha}", "sha256:" + new string('0', 64)).Replace("- **Carried**: none <!-- the characters this sequel carried over, as the closed campaign's sheets title them -->\n", ""));
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "index: no Carried line", $"index: Closing backup's checksum differs from backups/{file}" }, result.StdoutLines);
    }

    /// <summary>A file over 20,000 estimated tokens, and a subject heading twice across a kind's parts, are named.</summary>
    [TestMethod]
    public void ALargeFileAndAHeadingTwiceAreNamed()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        var big = new StringBuilder("# People, part 2\n\n## Rook\n\n");
        while (big.Length < 84_000)
            big.Append("- Rook walked the tarn road again and again. (s1)\n");
        WriteDigest(root, "people-2.md", big.ToString());
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.HasCount(2, result.StdoutLines);
        StringAssert.Matches(result.StdoutLines[0], new System.Text.RegularExpressions.Regex($@"^too large: digests/{ClosedName}/people-2\.md 21(\.\d)?k tokens \(split it by subject\)$"));
        Assert.AreEqual($"heading twice: digests/{ClosedName}/people: Rook", result.StdoutLines[1]);
    }

    /// <summary>The spine takes the logs in session order, however their numbers are padded (the code review's finding).</summary>
    [TestMethod]
    public void TheSpineTakesTheLogsInSessionOrder()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, beforeClose: campaign =>
        {
            File.WriteAllText(Path.Combine(campaign, "story", "sessions", "session-10.md"), "# Session 10\n\n### Scene 1: Late (Day 9)\n");
            File.WriteAllText(Path.Combine(campaign, "story", "sessions", "session-9.md"), "# Session 9\n\n### Scene 1: Early (Day 8)\n");
        });
        Assert.AreEqual(0, Sequel("--start").ExitCode);
        string spine = Digest(root, "spine.md");
        Assert.IsLessThan(spine.IndexOf("(session-9.md)", StringComparison.Ordinal), spine.IndexOf("(session-003.md)", StringComparison.Ordinal));
        Assert.IsLessThan(spine.IndexOf("(session-10.md)", StringComparison.Ordinal), spine.IndexOf("(session-9.md)", StringComparison.Ordinal));
        StringAssert.Contains(spine, "- s9.1 Early (Day 8)\n");
        StringAssert.Contains(spine, "- s10.1 Late (Day 9)\n");
    }

    /// <summary>A heading inside a comment over several lines is no section: not of the DM notes' secrets, not a subject twice (the code review's finding).</summary>
    [TestMethod]
    public void AHeadingInsideACommentIsNotASection()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true, beforeClose: campaign =>
        {
            string notes = Path.Combine(campaign, "story", "dm-notes.md");
            File.WriteAllText(notes, File.ReadAllText(notes).Replace("## The central conflict", "<!--\n## Secrets\n- a template example, not a secret\n-->\n\n## The central conflict"));
        });
        var texts = Units(root).Select(u => u.Text).ToList();
        Assert.DoesNotContain("a template example, not a secret", texts);
        Assert.Contains("Marta the ferrywoman is the Ember Witch's sister, and she knows who killed her.", texts);

        WriteDigest(root, "people-2.md", "# People, part 2\n\n<!--\nOne subject a heading, for example:\n## Rook\n-->\n");
        var result = Sequel("--check");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
    }

    /// <summary>A Chain line that is not the chain the copies follow is named (the code review's finding).</summary>
    [TestMethod]
    public void AWrongChainLineIsNamed()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        WriteDigest(root, "index.md", Digest(root, "index.md").Replace("- **Chain**: none", "- **Chain**: old-tale"));
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "index: Chain names old-tale, not none" }, result.StdoutLines);
    }

    /// <summary>
    /// A closed campaign under another name than its backup's (a copy imported with <c>--as</c>, its backup put back) is
    /// digested: the bytes are the check, as for <c>verify</c> (the code review's finding).
    /// </summary>
    [TestMethod]
    public void AClosedCampaignUnderAnotherNameIsDigested()
    {
        using var root = new MigrationRoot();
        WriteSequel(root);
        Directory.Move(root.Campaign(ClosedName), root.Campaign("grey-copy"));
        root.Write("player-options.md", SequelOptions.Replace(ClosedName + " <!--", "grey-copy <!--"), SequelName);
        var result = Sequel("--start");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.StartsWith(result.StdoutLines[0], "started digests/grey-copy/");
    }

    /// <summary>More than 40 problems print 40, then a count.</summary>
    [TestMethod]
    public void MoreThanFortyProblemsAreCut()
    {
        using var root = new MigrationRoot();
        WriteSequel(root, digest: true);
        File.AppendAllLines(Path.Combine(DigestFolder(root), "accounting.tsv"), Enumerable.Range(0, 45).Select(i => $"{i:x12}\tdropped:structure\tnothing"));
        var result = Sequel("--check");
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.HasCount(41, result.StdoutLines);
        Assert.AreEqual("(+5 more)", result.StdoutLines[^1]);
        Assert.AreEqual("dmc campaign: check failed: 45 problems", result.Stderr.TrimEnd());
    }
}
