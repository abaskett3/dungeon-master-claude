using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign restore NAME [--from FILE] [--yes]</c> (contracts/campaign-commands.md § restore; research R9; spec FR-005g,
/// SC-009): a plan by default, the warning about changes since the migration was verified, a restore that makes the
/// campaign equal the backup file for file with <c>backups/</c> kept, and recovery from an interrupted restore.
/// </summary>
[TestClass]
public sealed class RestoreTests
{
    /// <summary>A campaign at standard 1, the kit at 2, a backup taken; the campaign then changed as a migration would change it.</summary>
    static (MigrationRoot Root, string Backup) Migrated()
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        root.WriteMigrations(2, ["mix-tracker"]);
        string backup = root.Backup();
        root.Write("story/canon.md", root.Read("story/canon.md") + "- A new fact. (s3)\n");
        root.Write("story/dm-notes-archive.md", "# DM notes archive\n");
        return (root, backup);
    }

    /// <summary>Runs restore on the sample campaign.</summary>
    static ProcessResult Restore(params string[] extra) => MigrationRoot.Run(["restore", MigrationRoot.Name, .. extra]);

    /// <summary>The folders of <c>adventures/</c> whose names start with a dot: staging and aside folders.</summary>
    static string[] Dotted(MigrationRoot root) =>
        [.. Directory.GetDirectories(root.Adventures).Where(d => Path.GetFileName(d).StartsWith('.'))];

    /// <summary>The backups folder of the sample campaign as path and SHA-256 of each file, for "byte-identical" checks.</summary>
    static string[] BackupsSnapshot(MigrationRoot root) => DmcFolders.Snapshot(root.Campaign(), "backups");

    [TestMethod]
    public void WithoutYesItPrintsThePlanAndChangesNothing()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        string[] before = Snapshot(root.Adventures);
        var result = Restore();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            $"restore rorn from {backup}: 1 files replaced, 1 removed, 0 added",
            "warning: rorn has changed since the backup (2 files): those changes will be lost",
            "confirm with --yes",
        }, result.StdoutLines);
        CollectionAssert.AreEqual(before, Snapshot(root.Adventures));
    }

    [TestMethod]
    public void AfterAPassingVerifyAndNoMorePlayThereIsNoWarning()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name).ExitCode);
        CollectionAssert.AreEqual(new[] { $"restore rorn from {backup}: 1 files replaced, 1 removed, 0 added", "confirm with --yes" }, Restore().StdoutLines);
    }

    [TestMethod]
    public void PlayAfterTheVerifyIsWarnedAbout()
    {
        var (root, _) = Migrated();
        using var __ = root;
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name).ExitCode);
        root.Write("game-state.md", root.Read("game-state.md") + "- **Gold**: 12 gp\n");
        CollectionAssert.Contains(Restore().StdoutLines, "warning: rorn has changed since the migration was verified (1 files): those changes will be lost");
    }

    [TestMethod]
    public void WithYesTheCampaignEqualsTheBackupAndBackupsIsKept()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        // The original, as the backup holds it, for comparison.
        string original = Path.Combine(root.Folder, "original");
        CampaignImporter.ExtractCampaign(ExportSummary.Open(root.InBackups(backup)), original);
        root.Account(backup, "# accounting");
        string[] backupsBefore = BackupsSnapshot(root).Where(l => !l.Contains(".migrating", StringComparison.Ordinal)).ToArray();

        var result = Restore("--yes");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        int files = ExportSummary.Open(root.InBackups(backup)).Manifest.Files.Count;
        CollectionAssert.AreEqual(new[] { $"restored rorn from {backup}: {files} files" }, result.StdoutLines);

        var restored = ReadFiles(root.Campaign()).Where(f => !f.Key.StartsWith("backups/", StringComparison.Ordinal)).ToDictionary();
        var want = ReadFiles(original);
        CollectionAssert.AreEquivalent(want.Keys.ToArray(), restored.Keys.ToArray(), "files added since are gone");
        foreach (var (path, bytes) in want)
            CollectionAssert.AreEqual(bytes, restored[path], path);
        Assert.AreEqual("1\n", root.Read(".standard"), "the backup's standard");
        CollectionAssert.AreEqual(backupsBefore, BackupsSnapshot(root), "backups/ is byte-identical, but for the marks");
        Assert.IsEmpty(Directory.GetFiles(root.Backups(), "*.migrating"));
        Assert.IsEmpty(Dotted(root), "no staging or aside folder is left");
        CollectionAssert.AreEqual(new[] { "behind: standard 1, kit 2: 002-test-2.md" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void FromDefaultsToTheMarkedBackupElseTheLatest()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        string later = "rorn_backup_29991231235959.zip";
        File.Copy(root.InBackups(backup), root.InBackups(later));
        StringAssert.StartsWith(Restore().StdoutLines[0], $"restore rorn from {backup}:");
        root.Unmark(backup);
        StringAssert.StartsWith(Restore().StdoutLines[0], $"restore rorn from {later}:");
        StringAssert.StartsWith(Restore("--from", backup).StdoutLines[0], $"restore rorn from {backup}:");
    }

    [TestMethod]
    public void FromABackupOfAnotherCampaignIsRefused()
    {
        var (root, _) = Migrated();
        using var __ = root;
        root.WriteCampaign("other");
        string theirs = root.Backup("other");
        string[] before = Snapshot(root.Campaign());
        var result = Restore("--from", root.InBackups(theirs, "other"), "--yes");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc campaign: {theirs} is a backup of other, not rorn", result.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, Snapshot(root.Campaign()));
    }

    [TestMethod]
    public void ADamagedBackupChangesNothing()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        byte[] bytes = File.ReadAllBytes(root.InBackups(backup));
        bytes[bytes.Length / 2] ^= 0xFF;
        File.WriteAllBytes(root.InBackups(backup), bytes);
        string[] before = Snapshot(root.Adventures);
        var result = Restore("--yes");
        Assert.AreEqual(1, result.ExitCode);
        Assert.HasCount(1, result.StderrLines);
        CollectionAssert.AreEqual(before, Snapshot(root.Adventures));
    }

    [TestMethod]
    [DataRow("staged")]
    [DataRow("backups-moved")]
    [DataRow("aside")]
    public void AFailureBeforeTheRenameIntoPlaceLeavesTheCampaignAsItWas(string step)
    {
        var (root, backup) = Migrated();
        using var _ = root;
        var before = ReadFiles(root.Campaign());
        CampaignRestorer.Hook = s =>
        {
            if (s == step)
                throw new IOException("the disk is full");
        };
        try
        {
            var result = Restore("--yes");
            Assert.AreEqual(1, result.ExitCode);
            Assert.AreEqual("dmc campaign: could not restore rorn: the disk is full; the campaign is as it was", result.Stderr.TrimEnd());
        }
        finally
        {
            CampaignRestorer.Hook = null;
        }
        var after = ReadFiles(root.Campaign());
        CollectionAssert.AreEqual(before.Keys.ToArray(), after.Keys.ToArray());
        foreach (var (path, bytes) in before)
            CollectionAssert.AreEqual(bytes, after[path], path);
        Assert.IsEmpty(Dotted(root));
        Assert.IsTrue(File.Exists(root.InBackups(backup + ".migrating")), "the mark stays when nothing was restored");
    }

    [TestMethod]
    public void AnInterruptedRestoreIsRecoveredByTheNextRun()
    {
        var (root, backup) = Migrated();
        using var _ = root;
        var before = ReadFiles(root.Campaign());
        CampaignRestorer.Hook = s =>
        {
            if (s == "aside")
                throw new InvalidOperationException("killed");
        };
        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CampaignRestorer.Run(root.Folder, MigrationRoot.Name, null, yes: true));
        }
        finally
        {
            CampaignRestorer.Hook = null;
        }
        Assert.IsFalse(Directory.Exists(root.Campaign()), "the campaign was renamed aside");
        Assert.HasCount(1, Directory.GetDirectories(root.Adventures, ".replaced-rorn-*"));

        var plan = Restore();
        Assert.AreEqual(0, plan.ExitCode, plan.ToString());
        Assert.AreEqual("recovered rorn from an interrupted restore", plan.StdoutLines[0]);
        var after = ReadFiles(root.Campaign());
        CollectionAssert.AreEqual(before.Keys.ToArray(), after.Keys.ToArray(), "the campaign, with its backups, is back as it was");
        Assert.IsEmpty(Dotted(root));
        Assert.IsTrue(File.Exists(root.InBackups(backup)));
    }

    [TestMethod]
    public void WithNoBackupItIsOneLine()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var result = Restore();
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc campaign: rorn has no backup: dmc campaign backup rorn takes one", result.Stderr.TrimEnd());
    }
}
