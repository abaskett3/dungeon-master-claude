using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// A migration interrupted part way (spec FR-011, SC-005; research R8): the rerun is told the migration is unfinished, takes
/// no new backup of half-migrated files, and verifies or restores against the first attempt's backup.
/// </summary>
[TestClass]
public sealed class InterruptedMigrationTests
{
    /// <summary>A backup, then a half-done migration: one content line deleted and another edited.</summary>
    static (MigrationRoot Root, string Backup, string DeletedId) HalfDone()
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        root.WriteMigrations(2, ["mix-tracker"]);
        string backup = root.Backup();
        root.Write("story/canon.md", root.Read("story/canon.md").Replace("- The party crossed the river. (s1.2)\n", ""));
        root.Write("story/dm-notes.md", root.Read("story/dm-notes.md").Replace("the marsh", "the old marsh road"));
        return (root, backup, ContentUnits.Id("The party crossed the river. (s1.2)"));
    }

    [TestMethod]
    public void TheRerunIsToldItIsUnfinishedAndTakesNoBackup()
    {
        var (root, backup, _) = HalfDone();
        using var __ = root;
        CollectionAssert.AreEqual(new[] { $"unfinished: {backup}" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
        Assert.AreEqual(1, MigrationRoot.Run("backup", MigrationRoot.Name).ExitCode);
        CollectionAssert.AreEqual(new[] { "1 backups", backup }, MigrationRoot.Run("backups", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void TheRerunVerifiesAgainstTheFirstBackup()
    {
        var (root, backup, deleted) = HalfDone();
        using var __ = root;
        var list = MigrationRoot.Run("verify", MigrationRoot.Name, "--list");
        Assert.AreEqual(0, list.ExitCode, list.ToString());
        Assert.IsTrue(list.StdoutLines.Any(l => l.StartsWith($"unaccounted: {deleted} story/canon.md", StringComparison.Ordinal)), list.Stdout);
        string edited = ContentUnits.Id("| Bog trolls | 2 / 2 | the marsh | AC 15, HP 84 | rumours |");

        var fail = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, fail.ExitCode);
        StringAssert.Contains(fail.Stdout, deleted);

        root.Account(backup, $"{deleted}\tdropped:mix-tracker\tin the session log", $"{edited}\treworded\tthe old marsh road");
        var pass = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, pass.ExitCode, pass.ToString());
        Assert.IsTrue(File.Exists(root.InBackups(backup + ".verified.tsv")), "the first backup was the one used");
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void RestoreWithNoFromRestoresTheFirstBackupAndClearsTheMark()
    {
        var (root, backup, _) = HalfDone();
        using var __ = root;
        string later = "rorn_backup_29991231235959.zip";
        File.Copy(root.InBackups(backup), root.InBackups(later));
        var result = MigrationRoot.Run("restore", MigrationRoot.Name, "--yes");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.StartsWith(result.StdoutLines[^1], $"restored rorn from {backup}:");
        StringAssert.Contains(root.Read("story/canon.md"), "The party crossed the river. (s1.2)");
        CollectionAssert.AreEqual(new[] { "behind: standard 1, kit 2: 002-test-2.md" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
        Assert.IsEmpty(Directory.GetFiles(root.Backups(), "*.migrating"));
        _ = ReadFiles(root.Campaign());
    }
}
