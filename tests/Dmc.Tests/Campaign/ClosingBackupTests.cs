using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The closing backup survives (spec 010 US3, SC-002, FR-012, FR-018; research R4, R5): no backup or prune ever counts or
/// removes it, and its copy in <c>exports/</c> stands alone: with the campaign folder gone it shows, imports, and the
/// import is closed too.
/// </summary>
[TestClass]
public sealed class ClosingBackupTests
{
    [TestMethod]
    public void TenRoundsOfBackupAndPruneNeverTouchAClosingBackup()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign("closed-one");
        Assert.AreEqual(0, MigrationRoot.Run("close", "closed-one", "--yes").ExitCode);
        string real = ClosedCampaign.Read(root.Campaign("closed-one"))!.Backup;

        // A real closing backup, renamed for the open campaign, sits among its ordinary backups.
        root.WriteCampaign();
        Directory.CreateDirectory(root.Backups());
        string closing = "rorn" + real["closed-one".Length..];
        File.Copy(root.InBackups(real, "closed-one"), root.InBackups(closing));
        byte[] before = File.ReadAllBytes(root.InBackups(closing));

        for (int round = 0; round < 10; round++)
        {
            Assert.AreEqual(0, MigrationRoot.Run("backup", MigrationRoot.Name).ExitCode, $"round {round}");
            var prune = MigrationRoot.Run("backup", MigrationRoot.Name, "--prune", "old");
            Assert.AreEqual(0, prune.ExitCode, prune.ToString());
            CollectionAssert.AreEqual(before, File.ReadAllBytes(root.InBackups(closing)), $"round {round}");
            var listed = MigrationRoot.Run("backups", MigrationRoot.Name).StdoutLines;
            Assert.DoesNotContain(closing, listed, "backups never counts it");
            Assert.IsFalse(BackupFolder.List(root.Campaign(), MigrationRoot.Name).Any(b => b.FileName == closing));
        }
        Assert.IsTrue(File.Exists(root.InBackups(closing)));
    }

    [TestMethod]
    public void TheCopyInExportsStandsAloneAndItsImportIsClosed()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.Write("module-progress.md", "# Module progress\n\n- **Module folder**: `modules/the-silver-egg/`\n");
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        var mark = ClosedCampaign.Read(root.Campaign())!;
        string copy = Path.Combine(root.Folder, "exports", mark.Backup);
        DmcFolders.Delete(root.Campaign());

        var show = MigrationRoot.Run("show", copy);
        Assert.AreEqual(0, show.ExitCode, show.ToString());
        StringAssert.StartsWith(show.StdoutLines[0], "rorn: exported ");
        CollectionAssert.Contains(show.StdoutLines, "module: the-silver-egg (not in modules/)");
        Assert.AreEqual("closed: " + mark.Time, show.StdoutLines[^1]);
        var summary = ExportSummary.Open(copy);
        Assert.IsFalse(summary.Manifest.Files.Any(f => f.Path.StartsWith("modules/", StringComparison.Ordinal)), "no module files");

        var import = MigrationRoot.Run("import", copy, "--as", "rorn-copy");
        Assert.AreEqual(0, import.ExitCode, import.ToString());
        Assert.AreEqual("rorn-copy is closed: start a sequel to continue its story", import.StdoutLines[^1]);
        Assert.IsTrue(root.Has(".closed", "rorn-copy"));
        CollectionAssert.AreEqual(new[] { "closed: " + mark.Time }, MigrationRoot.Run("standard", "rorn-copy").StdoutLines);

        var verify = MigrationRoot.Run("verify", "rorn-copy", "--backup", copy);
        Assert.AreEqual(0, verify.ExitCode, verify.ToString());
        StringAssert.StartsWith(verify.StdoutLines.Single(), "verified rorn-copy: closed, ");
    }
}
