using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]</c> (contracts/campaign-commands.md § verify;
/// research R5; spec FR-009, SC-013): a migration passes only when every unit of the backup is matched or accounted for, no
/// file was skipped and the pending migrations' checks pass. Each test takes a real backup first.
/// </summary>
[TestClass]
public sealed partial class VerifyTests
{
    /// <summary>The pass line, with its counts.</summary>
    [GeneratedRegex(@"^verified rorn: (\d+) units, (\d+) matched, (\d+) accounted, (\d+) dropped; standard (\d+)$")]
    private static partial Regex PassLine();

    /// <summary>An unaccounted line: id, file, text.</summary>
    [GeneratedRegex(@"^unaccounted: ([0-9a-f]{12}) (\S+) (.*)$")]
    private static partial Regex UnaccountedLine();

    /// <summary>A campaign at standard 1, the kit at 2 with the given drops and checks, and a backup taken.</summary>
    static (MigrationRoot Root, string Backup) Start(string[]? drops = null, string[]? checks = null)
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        root.WriteMigrations(2, drops ?? ["mix-tracker"], checks);
        return (root, root.Backup());
    }

    /// <summary>Runs verify on the sample campaign.</summary>
    static ProcessResult Verify(params string[] extra) => MigrationRoot.Run(["verify", MigrationRoot.Name, .. extra]);

    /// <summary>The ids of the unaccounted lines a run printed.</summary>
    static string[] Ids(ProcessResult result) =>
        [.. result.StdoutLines.Select(l => UnaccountedLine().Match(l)).Where(m => m.Success).Select(m => m.Groups[1].Value)];

    // ---------- pass ----------

    [TestMethod]
    public void AnUnchangedCampaignPassesAndIsRecorded()
    {
        var (root, backup) = Start();
        using var _ = root;
        var result = Verify();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var pass = PassLine().Match(result.StdoutLines.Single());
        Assert.IsTrue(pass.Success, result.Stdout);
        Assert.AreEqual(pass.Groups[1].Value, pass.Groups[2].Value, "every unit matched");
        Assert.AreEqual("2", pass.Groups[5].Value);
        Assert.AreEqual("2\n", root.Read(".standard"));
        Assert.IsFalse(File.Exists(root.InBackups(backup + ".migrating")), "the mark is removed");

        // The verified record names every campaign file with its SHA-256, and not .standard or backups/.
        string[] record = File.ReadAllLines(root.InBackups(backup + ".verified.tsv"));
        var files = CampaignFixtures.ReadFiles(root.Campaign()).Where(f => f.Key != ".standard" && !f.Key.StartsWith("backups/", StringComparison.Ordinal));
        CollectionAssert.AreEquivalent(files.Select(f => $"{f.Key}\t{CampaignFixtures.Sha(f.Value)}").ToArray(), record);
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void AUnitMovedVerbatimToAnotherFileIsMatched()
    {
        var (root, _) = Start();
        using var __ = root;
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("- Who paid the smugglers?\n", ""));
        root.Write("story/dm-notes-archive.md", "# DM notes archive\n\n## Closed threads\n\n* Who paid the smugglers?\n");
        Assert.AreEqual(0, Verify().ExitCode);
    }

    [TestMethod]
    public void ALongUnitContainedInALongerLineIsMatchedAndAShortOneIsNot()
    {
        var (root, backup) = Start();
        using var __ = root;
        // "Mara keeps the ferry and charges 2 sp. (s1)" is 43 characters: merged into a longer line, it is matched.
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- Mara keeps the ferry and charges 2 sp. (s1)", "- Mara keeps the ferry and charges 2 sp. (s1); her brother runs the mill."));
        Assert.AreEqual(0, Verify("--list").StdoutLines.Count(l => l.StartsWith("unaccounted:", StringComparison.Ordinal)));

        // "Bog trolls" style short units are not: "People" (a heading) inside a longer heading does not match.
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("## People", "## People and places"));
        var list = Verify("--list");
        CollectionAssert.AreEqual(new[] { ContentUnits.Id("People") }, Ids(list));
        _ = backup;
    }

    [TestMethod]
    public void ADroppedHeadingIsUnaccounted()
    {
        var (root, _) = Start();
        using var __ = root;
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("## Open threads\n\n", ""));
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { ContentUnits.Id("Open threads") }, Ids(result));
        StringAssert.Contains(result.Stdout, $"unaccounted: {ContentUnits.Id("Open threads")} story/dm-notes.md Open threads");
    }

    [TestMethod]
    public void TheStandardRecordIsNeverAUnit()
    {
        var (root, _) = Start();
        using var __ = root;
        File.Delete(Path.Combine(root.Campaign(), ".standard"));
        Assert.AreEqual(0, Verify().ExitCode);
    }

    // ---------- what cannot account for a unit ----------

    [TestMethod]
    public void AUnitWhoseOnlyCopyIsInTheAccountingFileIsUnaccounted()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("- Who paid the smugglers?\n", ""));
        root.Account(backup, "# Who paid the smugglers?");
        Assert.AreEqual(1, Verify().ExitCode);
    }

    [TestMethod]
    public void ARewordingWhoseAnchorIsOnlyInTheAccountingFileFails()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("- Who paid the smugglers?\n", ""));
        string id = ContentUnits.Id("Who paid the smugglers?");
        root.Account(backup, $"{id}\treworded\tthe smugglers' paymaster");
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stdout, "(anchor not found: the smugglers' paymaster)");
    }

    // ---------- fail ----------

    [TestMethod]
    public void AnUnaccountedUnitFailsNamingItAndChangesNothing()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", ""));
        string id = ContentUnits.Id("The party crossed the river. (s1.2)");
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { $"unaccounted: {id} story/canon.md The party crossed the river. (s1.2)" }, result.StdoutLines);
        Assert.AreEqual($"dmc campaign: verification failed: 1 unit unaccounted; {backup} is untouched", result.Stderr.TrimEnd());
        Assert.AreEqual("1\n", root.Read(".standard"));
        Assert.IsTrue(File.Exists(root.InBackups(backup + ".migrating")), "the mark stays");
        Assert.IsFalse(File.Exists(root.InBackups(backup + ".verified.tsv")));
    }

    [TestMethod]
    public void FortyUnaccountedArePrintedAndTheRestCounted()
    {
        var root = new MigrationRoot();
        using var __ = root;
        root.WriteCampaign();
        root.Write("story/extra.md", string.Concat(Enumerable.Range(1, 45).Select(i => $"- fact number {i}\n")));
        root.WriteMigrations(2);
        root.Backup();
        File.Delete(Path.Combine(root.Campaign(), "story", "extra.md"));

        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        Assert.HasCount(40, Ids(result));
        Assert.AreEqual("5 more", result.StdoutLines[^1]);
        var all = Verify("--all");
        Assert.HasCount(45, Ids(all));
        Assert.IsFalse(all.StdoutLines.Any(l => l.EndsWith(" more", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ARewordingWithItsAnchorAndADeclaredDropPass()
    {
        var (root, backup) = Start(drops: ["mix-tracker"]);
        using var __ = root;
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("- Who paid the smugglers?\n", "- The smugglers' paymaster is still unknown.\n"));
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", ""));
        root.Account(backup,
            "# accounting",
            $"{ContentUnits.Id("Who paid the smugglers?")}\treworded\tsmugglers' paymaster",
            $"{ContentUnits.Id("The party crossed the river. (s1.2)")}\tdropped:mix-tracker\tin the session log");
        var result = Verify();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var pass = PassLine().Match(result.StdoutLines.Single());
        Assert.AreEqual("1", pass.Groups[3].Value, "one reworded");
        Assert.AreEqual("1", pass.Groups[4].Value, "one dropped");
        Assert.AreEqual(int.Parse(pass.Groups[1].Value), int.Parse(pass.Groups[2].Value) + 2);
    }

    [TestMethod]
    public void ADropUnderAClassNoPendingMigrationDeclaresFails()
    {
        var (root, backup) = Start(drops: ["mix-tracker"]);
        using var __ = root;
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", ""));
        root.Account(backup, $"{ContentUnits.Id("The party crossed the river. (s1.2)")}\tdropped:old-fights\tgone");
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stdout, "(drop class old-fights is not declared by the pending migrations)");
    }

    [TestMethod]
    public void ASkippedFileFailsEvenWhenEveryUnitMatches()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Account(backup, "skipped\tstory/dm-notes.md\thand-edited, shape not recognised");
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "skipped: story/dm-notes.md hand-edited, shape not recognised" }, result.StdoutLines);
        Assert.AreEqual($"dmc campaign: verification failed: 1 file skipped; {backup} is untouched", result.Stderr.TrimEnd());
    }

    [TestMethod]
    public void AFailingCheckIsPrintedAndFailsTheRun()
    {
        var (root, _) = Start(checks: ["sheets-end-with-history"]);
        using var __ = root;
        root.Write("character-sheets/pc-wren.md", root.Read("character-sheets/pc-wren.md") + "\n## Inventory\n\n- rope\n");
        var result = Verify();
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.Contains(result.StdoutLines, "check sheets-end-with-history: character-sheets/pc-wren.md: ## History is not the last section (## Inventory follows it)");
        StringAssert.Contains(result.Stderr, "1 check failure");
    }

    // ---------- list ----------

    [TestMethod]
    public void ListPrintsWhatIsLeftWritesNothingAndItsIdsAccountTheRun()
    {
        var (root, backup) = Start(drops: ["mix-tracker"]);
        using var __ = root;
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", "").Replace("## People\n", ""));
        root.Account(backup, "skipped\tgame-state.md\tleft as it was");
        string[] before = CampaignFixtures.Snapshot(root.Campaign());
        var list = Verify("--list");
        Assert.AreEqual(0, list.ExitCode, list.ToString());
        CollectionAssert.AreEqual(before, CampaignFixtures.Snapshot(root.Campaign()), "list writes nothing");
        Assert.HasCount(2, Ids(list));
        CollectionAssert.Contains(list.StdoutLines, "skipped: game-state.md");
        Assert.AreEqual($"2 to account in backups/{backup}.accounting.tsv", list.StdoutLines[^1]);

        root.Account(backup, [.. Ids(list).Select(id => $"{id}\tdropped:mix-tracker\tnot needed")]);
        var pass = Verify();
        Assert.AreEqual(0, pass.ExitCode, pass.ToString());
    }

    // ---------- defaults ----------

    [TestMethod]
    public void TheBackupDefaultsToTheMarkedOneOverALaterOne()
    {
        var (root, backup) = Start();
        using var __ = root;
        string later = "rorn_backup_29991231235959.zip";
        File.Copy(root.InBackups(backup), root.InBackups(later));
        Assert.AreEqual(0, Verify().ExitCode);
        Assert.IsTrue(File.Exists(root.InBackups(backup + ".verified.tsv")), "the marked backup was used");
        Assert.IsFalse(File.Exists(root.InBackups(later + ".verified.tsv")));
    }

    [TestMethod]
    public void WithNoMarkTheBackupDefaultsToTheLatest()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Unmark(backup);
        string later = "rorn_backup_29991231235959.zip";
        File.Copy(root.InBackups(backup), root.InBackups(later));
        Assert.AreEqual(0, Verify().ExitCode);
        Assert.IsTrue(File.Exists(root.InBackups(later + ".verified.tsv")));
    }

    [TestMethod]
    public void APassClearsEveryMarkEvenWhenAnotherBackupIsNamed()
    {
        var (root, backup) = Start();
        using var __ = root;
        string other = "rorn_backup_20200101000000.zip";
        File.Copy(root.InBackups(backup), root.InBackups(other));
        Assert.AreEqual(0, Verify("--backup", other).ExitCode);
        Assert.IsFalse(File.Exists(root.InBackups(backup + ".migrating")), "the marked backup's mark goes too");
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void BackupAndAccountingCanBeNamed()
    {
        var (root, backup) = Start(drops: ["mix-tracker"]);
        using var __ = root;
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", ""));
        string accounting = Path.Combine(root.Folder, "elsewhere.tsv");
        File.WriteAllText(accounting, $"{ContentUnits.Id("The party crossed the river. (s1.2)")}\tdropped:mix-tracker\tlogged\n");
        Assert.AreEqual(0, Verify("--backup", backup, "--accounting", accounting).ExitCode);
    }

    // ---------- cannot verify ----------

    [TestMethod]
    public void ADamagedBackupCannotBeVerified()
    {
        var (root, backup) = Start();
        using var __ = root;
        byte[] bytes = File.ReadAllBytes(root.InBackups(backup));
        bytes[bytes.Length / 2] ^= 0xFF;
        File.WriteAllBytes(root.InBackups(backup), bytes);
        AssertCannot(Verify(), null);
        Assert.AreEqual("1\n", root.Read(".standard"));
    }

    [TestMethod]
    public void ABackupOfAnotherCampaignCannotBeVerified()
    {
        var (root, _) = Start();
        using var __ = root;
        root.WriteCampaign("other");
        string theirs = root.Backup("other");
        AssertCannot(Verify("--backup", root.InBackups(theirs, "other")), $"dmc campaign: {theirs} is a backup of other, not rorn");
    }

    [TestMethod]
    public void AMissingBackupCannotBeVerified()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(2);
        AssertCannot(Verify(), "dmc campaign: rorn has no backup: dmc campaign backup rorn takes one");
        AssertCannot(Verify("--backup", "nope.zip"), "dmc campaign: no backup nope.zip: dmc campaign backups rorn lists them");
    }

    [TestMethod]
    public void NoPendingMigrationCannotBeVerified()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string backup = root.Backup();
        AssertCannot(Verify(), $"dmc campaign: no migration is pending: {backup} is at standard 1 and the kit at 1");
    }

    [TestMethod]
    public void TwoDifferentUnitsWithOneIdCannotBeVerified()
    {
        var (root, _) = Start();
        using var __ = root;
        ContentUnits.IdOverride = _ => "000000000000";
        try
        {
            var result = Verify();
            AssertCannot(result, null);
            StringAssert.StartsWith(result.Stderr, "dmc campaign: cannot verify: two different units share the id 000000000000");
        }
        finally
        {
            ContentUnits.IdOverride = null;
        }
    }

    [TestMethod]
    public void ABadAccountingLineCannotBeVerified()
    {
        var (root, backup) = Start();
        using var __ = root;
        root.Account(backup, "# x", "not a line");
        AssertCannot(Verify(), $"dmc campaign: backups/{backup}.accounting.tsv line 2: expected three fields separated by tabs: id, disposition, detail");
    }

    /// <summary>Asserts a run that cannot verify: one line on standard error, nothing on standard output, exit 1.</summary>
    static void AssertCannot(ProcessResult result, string? message)
    {
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.HasCount(1, result.StderrLines);
        if (message is not null)
            Assert.AreEqual(message, result.Stderr.TrimEnd());
    }

    // ---------- scale ----------

    [TestMethod]
    public void TenThousandUnitsVerifyInUnderFifteenSeconds()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        for (int f = 0; f < 20; f++)
        {
            var text = new StringBuilder($"# Session {f}\n\n");
            for (int i = 0; i < 500; i++)
                text.Append($"- In session {f} the party met traveller number {i} on the road to the ferry.\n");
            root.Write($"story/sessions/session-{f:000}.md", text.ToString());
        }
        root.WriteMigrations(2);
        string backup = root.Backup();
        // A migration that rewrites one file of 500 units, all moved verbatim elsewhere.
        string moved = root.Read("story/sessions/session-000.md");
        File.Delete(Path.Combine(root.Campaign(), "story", "sessions", "session-000.md"));
        root.Write("story/sessions/session-000-moved.md", moved);

        var clock = Stopwatch.StartNew();
        var result = Verify();
        clock.Stop();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.IsLessThan(15.0, clock.Elapsed.TotalSeconds, $"verify of 10 000 units took {clock.Elapsed.TotalSeconds:0.0} s");
        Console.WriteLine($"verify of 10 000 units: {clock.Elapsed.TotalMilliseconds:0} ms");
        _ = backup;
    }
}
