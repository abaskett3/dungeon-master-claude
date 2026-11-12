using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign verify</c> on a closed campaign (spec 010 contracts/campaign-close.md § verify; research R7): a byte check of
/// every campaign file against the closing backup, reporting what changed, was added or is missing, and writing nothing,
/// pass or fail.
/// </summary>
[TestClass]
public sealed class CampaignVerifyClosedTests
{
    /// <summary>A closed sample campaign and its closing backup's file name.</summary>
    static (MigrationRoot Root, string Backup) Closed()
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        var result = MigrationRoot.Run("close", MigrationRoot.Name, "--yes");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        return (root, ClosedCampaign.Read(root.Campaign())!.Backup);
    }

    /// <summary>Everything verify could write: the campaign, its backups and <c>exports/</c>.</summary>
    static string[] Everything(MigrationRoot root) => DmcFolders.Snapshot(root.Folder, "adventures", "exports");

    [TestMethod]
    public void AnUnchangedClosedCampaignPassesAndNothingIsWritten()
    {
        var (root, backup) = Closed();
        using var _ = root;
        string[] before = Everything(root);
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"verified rorn: closed, 6 files match {backup}" }, result.StdoutLines, "four files, .standard and .closed");
        CollectionAssert.AreEqual(before, Everything(root));
        Assert.IsFalse(File.Exists(root.InBackups(backup + ".verified.tsv")));
    }

    [TestMethod]
    public void AChangedAnAddedAndAMissingFileAreNamedAndItFails()
    {
        var (root, backup) = Closed();
        using var _ = root;
        root.Write("story/canon.md", root.Read("story/canon.md") + "- An edit by hand.\n");
        root.Write("story/new.md", "# New\n");
        File.Delete(Path.Combine(root.Campaign(), "character-sheets", "pc-wren.md"));
        string[] before = Everything(root);
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "missing: character-sheets/pc-wren.md", "changed: story/canon.md", "added: story/new.md" }, result.StdoutLines);
        Assert.AreEqual($"dmc campaign: verification failed: 3 files differ from {backup}", result.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, Everything(root));
    }

    [TestMethod]
    public void AChangedMarkOrStandardIsNamedToo()
    {
        var (root, _) = Closed();
        using var __ = root;
        root.SetStandard("2\n");
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "changed: .standard" }, result.StdoutLines);
        StringAssert.Contains(result.Stderr, "1 file differs from");
    }

    [TestMethod]
    public void FortyLinesThenACountUnlessAll()
    {
        var (root, _) = Closed();
        using var __ = root;
        for (int i = 0; i < 45; i++)
            root.Write($"story/extra-{i:00}.md", "x\n");
        var capped = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, capped.ExitCode);
        Assert.HasCount(41, capped.StdoutLines);
        Assert.AreEqual("5 more", capped.StdoutLines[^1]);
        var all = MigrationRoot.Run("verify", MigrationRoot.Name, "--all");
        Assert.HasCount(45, all.StdoutLines);
        StringAssert.Contains(all.Stderr, "45 files differ");
    }

    [TestMethod]
    public void ListAndAccountingAreForMigrations()
    {
        var (root, _) = Closed();
        using var __ = root;
        string[] before = Everything(root);
        foreach (string[] extra in new[] { new[] { "--list" }, ["--accounting", "x.tsv"] })
        {
            var result = MigrationRoot.Run(["verify", MigrationRoot.Name, .. extra]);
            Assert.AreEqual(1, result.ExitCode);
            Assert.AreEqual("dmc campaign: rorn is closed: --list and --accounting are for migrations", result.Stderr.TrimEnd());
        }
        CollectionAssert.AreEqual(before, Everything(root));
    }

    [TestMethod]
    public void AGarbledMarkNeedsBackupAndThenWorks()
    {
        var (root, backup) = Closed();
        using var _ = root;
        byte[] mark = File.ReadAllBytes(Path.Combine(root.Campaign(), ".closed"));
        root.Write(".closed", "garbled");
        var refused = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, refused.ExitCode);
        Assert.AreEqual("dmc campaign: adventures/rorn/.closed is unreadable: name the closing backup with --backup", refused.Stderr.TrimEnd());

        var named = MigrationRoot.Run("verify", MigrationRoot.Name, "--backup", backup);
        Assert.AreEqual(1, named.ExitCode, "the garbled mark differs from the one the backup holds");
        CollectionAssert.AreEqual(new[] { "changed: .closed" }, named.StdoutLines);

        File.WriteAllBytes(Path.Combine(root.Campaign(), ".closed"), mark);
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name, "--backup", backup).ExitCode);
    }

    [TestMethod]
    public void AMarkNamingABackupThatIsNotThereSaysSo()
    {
        var (root, backup) = Closed();
        using var _ = root;
        File.Move(root.InBackups(backup), Path.Combine(root.Folder, "moved.zip"));
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc campaign: no backup {backup}: dmc campaign backups rorn lists them", result.Stderr.TrimEnd());
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name, "--backup", Path.Combine(root.Folder, "exports", backup)).ExitCode);
    }
}
