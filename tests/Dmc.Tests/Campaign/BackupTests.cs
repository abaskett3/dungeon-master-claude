using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign backups NAME</c> and <c>dmc campaign backup NAME [--prune oldest|old]</c> (contracts/campaign-commands.md;
/// research R8; spec FR-005a to FR-005c, SC-010, SC-011): the listing and its removal question, the checked backup, the
/// <c>.migrating</c> mark, and pruning only on the flag.
/// </summary>
[TestClass]
public sealed class BackupTests
{
    /// <summary>Puts stand-in backups in a campaign's <c>backups/</c>, with names a day apart in 2020, oldest first, each with its companions.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="count">How many.</param>
    /// <returns>Their file names, oldest first.</returns>
    static List<string> Earlier(MigrationRoot root, int count)
    {
        Directory.CreateDirectory(root.Backups());
        var names = new List<string>();
        for (int i = 0; i < count; i++)
        {
            string name = $"rorn_backup_202001{i + 10:00}120000.zip";
            File.WriteAllText(root.InBackups(name), "stand-in");
            File.WriteAllText(root.InBackups(name + ".accounting.tsv"), "# x\n");
            File.WriteAllText(root.InBackups(name + ".verified.tsv"), "");
            names.Add(name);
        }
        return names;
    }

    /// <summary>The counted backups of the sample campaign, oldest first.</summary>
    static string[] Listed(MigrationRoot root) => [.. BackupFolder.List(root.Campaign(), MigrationRoot.Name).Select(b => b.FileName)];

    [TestMethod]
    public void BackupsListsNoneAsZero()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var result = MigrationRoot.Run("backups", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "0 backups" }, result.StdoutLines);
    }

    [TestMethod]
    [DataRow(2, null)]
    [DataRow(3, "ask: may the oldest backup, rorn_backup_20200110120000.zip, be removed? yes: --prune oldest")]
    [DataRow(5, "ask: may the 3 oldest backups be removed, keeping the latest two? yes: --prune old")]
    public void BackupsListsOldestFirstAndAsksWhenDue(int count, string? ask)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var names = Earlier(root, count);
        var result = MigrationRoot.Run("backups", MigrationRoot.Name);
        string[] expected = [$"{count} backups", .. names, .. ask is null ? Array.Empty<string>() : [ask]];
        CollectionAssert.AreEqual(expected, result.StdoutLines);
    }

    [TestMethod]
    public void BackupsAsksNothingWhileAMigrationIsUnfinished()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var names = Earlier(root, 3);
        File.WriteAllText(root.InBackups(names[2] + ".migrating"), "");
        var result = MigrationRoot.Run("backups", MigrationRoot.Name);
        CollectionAssert.AreEqual((string[])["3 backups", .. names], result.StdoutLines);
    }

    [TestMethod]
    public void OtherFilesInBackupsAreNeverCountedOrRemoved()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var names = Earlier(root, 3);
        File.WriteAllText(root.InBackups("notes.zip"), "x");
        File.WriteAllText(root.InBackups("x_backup_1.zip"), "x");
        File.WriteAllText(root.InBackups("rorn_backup_2020.zip"), "x");
        File.WriteAllText(root.InBackups("other_backup_20200101000000.zip"), "x");
        CollectionAssert.AreEqual(names.ToArray(), Listed(root));

        var result = MigrationRoot.Run("backup", MigrationRoot.Name, "--prune", "old");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        foreach (string other in new[] { "notes.zip", "x_backup_1.zip", "rorn_backup_2020.zip", "other_backup_20200101000000.zip" })
            Assert.IsTrue(File.Exists(root.InBackups(other)), other);
    }

    [TestMethod]
    public void BackupWritesACheckedBackupAndItsMark()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        root.WriteMigrations(2);
        int files = Directory.GetFiles(root.Campaign(), "*", SearchOption.AllDirectories).Length;
        var before = DateTime.Now;
        var result = MigrationRoot.Run("backup", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());

        string file = Listed(root).Single();
        StringAssert.Matches(file, new System.Text.RegularExpressions.Regex(@"^rorn_backup_\d{14}\.zip$"));
        string stamp = file["rorn_backup_".Length..^4];
        var taken = DateTime.ParseExact(stamp, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsLessThan(5, Math.Abs((taken - before).TotalSeconds), "the name is the local time of the backup");
        CollectionAssert.AreEqual(new[] { $"backed up rorn: {files} files to backups/{file}" }, result.StdoutLines);
        Assert.IsTrue(File.Exists(root.InBackups(file + ".migrating")), "the mark is written");

        // The backup is a whole export of the campaign, .standard included, and dmc campaign show reads it.
        var show = MigrationRoot.Run("show", root.InBackups(file));
        Assert.AreEqual(0, show.ExitCode, show.ToString());
        var summary = ExportSummary.Open(root.InBackups(file));
        CollectionAssert.Contains(summary.Manifest.Files.Select(f => f.Path).ToArray(), ".standard");
    }

    [TestMethod]
    public void BackupRefusesWhileAMigrationIsUnfinishedAndWritesNothing()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(2);
        string first = root.Backup();
        string[] before = CampaignFixtures.Snapshot(root.Campaign());
        var result = MigrationRoot.Run("backup", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc campaign: rorn has an unfinished migration from {first}: finish it or restore it", result.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, CampaignFixtures.Snapshot(root.Campaign()));
    }

    [TestMethod]
    public void ABackupOfACurrentCampaignHasNoMarkAndLeavesItCurrent()
    {
        // Nothing is pending, so verify would have nothing to clear a mark with: a mark here would hold play until a restore.
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string first = root.Backup();
        Assert.IsFalse(File.Exists(root.InBackups(first + ".migrating")), "no mark without a pending migration");
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
        Assert.AreEqual(0, MigrationRoot.Run("backup", MigrationRoot.Name).ExitCode, "another backup is not refused");
    }

    [TestMethod]
    public void BackupsAreOrderedByWhenTheyWereTakenEvenWhenTheClockWentBack()
    {
        // The clocks go back at 02:00 summer time: 01:45 summer time (05:45 UTC) is followed by 01:30 winter time (06:30 UTC),
        // so the later backup's name sorts first. The manifest's UTC time puts them right.
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var before = CampaignBackup.Take(root.Folder, MigrationRoot.Name, PruneMode.None, new DateTime(2026, 11, 1, 5, 45, 0, DateTimeKind.Utc));
        File.Move(root.InBackups(before.FileName), root.InBackups("rorn_backup_20261101014500.zip"));
        var after = CampaignBackup.Take(root.Folder, MigrationRoot.Name, PruneMode.None, new DateTime(2026, 11, 1, 6, 30, 0, DateTimeKind.Utc));
        File.Move(root.InBackups(after.FileName), root.InBackups("rorn_backup_20261101013000.zip"));
        CollectionAssert.AreEqual(new[] { "rorn_backup_20261101014500.zip", "rorn_backup_20261101013000.zip" }, Listed(root));
    }

    [TestMethod]
    public void TwoBackupsInOneSecondTakeASuffixAndBothCount()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var moment = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Local);
        string dmc = root.Folder;
        var first = CampaignBackup.Take(dmc, MigrationRoot.Name, PruneMode.None, moment);
        root.Unmark(first.FileName);
        var second = CampaignBackup.Take(dmc, MigrationRoot.Name, PruneMode.None, moment);
        root.Unmark(second.FileName);
        var third = CampaignBackup.Take(dmc, MigrationRoot.Name, PruneMode.None, moment);
        Assert.AreEqual("rorn_backup_20261110120000.zip", first.FileName);
        Assert.AreEqual("rorn_backup_20261110120000-2.zip", second.FileName);
        Assert.AreEqual("rorn_backup_20261110120000-3.zip", third.FileName);
        CollectionAssert.AreEqual(new[] { first.FileName, second.FileName, third.FileName }, Listed(root));
    }

    [TestMethod]
    [DataRow(2, null, 3)]
    [DataRow(3, null, 4)]
    [DataRow(5, null, 6)]
    [DataRow(3, "oldest", 3)]
    [DataRow(5, "old", 3)]
    [DataRow(0, "old", 1)]
    [DataRow(0, "oldest", 1)]
    public void APruneHappensOnlyOnItsFlagAndLeavesTheRuleCount(int earlier, string? prune, int left)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var names = Earlier(root, earlier);
        var result = prune is null ? MigrationRoot.Run("backup", MigrationRoot.Name) : MigrationRoot.Run("backup", MigrationRoot.Name, "--prune", prune);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        string[] kept = Listed(root);
        Assert.HasCount(left, kept);
        int removed = earlier + 1 - left;
        if (prune is not null)
            StringAssert.EndsWith(result.StdoutLines.Single(), $"; removed {removed} older");

        // The removed ones are the oldest, and their companions went with them; the kept ones keep theirs.
        foreach (string gone in names.Take(removed))
        {
            Assert.IsFalse(File.Exists(root.InBackups(gone)), gone);
            foreach (string suffix in BackupFolder.CompanionSuffixes)
                Assert.IsFalse(File.Exists(root.InBackups(gone + suffix)), gone + suffix);
        }
        foreach (string stays in names.Skip(removed))
            Assert.IsTrue(File.Exists(root.InBackups(stays + ".accounting.tsv")), stays);
        CollectionAssert.DoesNotContain(names, kept[^1], "the latest is the new backup");
    }

    [TestMethod]
    public void AFailedWriteRemovesNothingAndWritesNoMark()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        // backups/ is a file, so the folder cannot be made.
        File.WriteAllText(root.Backups(), "in the way");
        var result = MigrationRoot.Run("backup", MigrationRoot.Name, "--prune", "old");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.HasCount(1, result.StderrLines);
        StringAssert.StartsWith(result.Stderr, "dmc campaign: could not export:");
        Assert.AreEqual("in the way", File.ReadAllText(root.Backups()));
        Assert.IsEmpty(Directory.GetFiles(root.Campaign(), "*.partial-*", SearchOption.AllDirectories));
    }

    [TestMethod]
    public void APruneValueOtherThanOldestOrOldIsAUsageError()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var result = MigrationRoot.Run("backup", MigrationRoot.Name, "--prune", "all");
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc campaign: argument --prune: invalid choice: 'all' (choose from 'oldest', 'old')", result.Stderr.TrimEnd());
        Assert.IsFalse(Directory.Exists(root.Backups()));
    }
}
