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

    /// <summary>
    /// A closed campaign whose files lost their CRLF in a git checkout still matches its closing backup: line endings
    /// alone are not a change (spec 018 FR-005).
    /// </summary>
    [TestMethod]
    public void ACrlfCampaignCheckedOutAsLfStillVerifies()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.IsGreaterThan(0, CampaignFixtures.SetLineEndings(root.Campaign(), crlf: true));
        root.SetStandard("1\r\n");
        var closed = MigrationRoot.Run("close", MigrationRoot.Name, "--yes");
        Assert.AreEqual(0, closed.ExitCode, closed.ToString());
        Assert.IsGreaterThan(0, CampaignFixtures.SetLineEndings(root.Campaign(), crlf: false));
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        StringAssert.StartsWith(result.Stdout, "verified rorn: closed, 6 files match ");
    }

    /// <summary>The reverse: a campaign closed with LF, then turned to CRLF by an editor or git on Windows, still verifies.</summary>
    [TestMethod]
    public void AnLfCampaignTurnedToCrlfStillVerifies()
    {
        var (root, backup) = Closed();
        using var _ = root;
        Assert.IsGreaterThan(0, CampaignFixtures.SetLineEndings(root.Campaign(), crlf: true));
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"verified rorn: closed, 6 files match {backup}" }, result.StdoutLines);
    }

    /// <summary>With every line ending changed, a real edit is still the one file named.</summary>
    [TestMethod]
    public void AnEditIsStillNamedAmongLineEndingChanges()
    {
        var (root, _) = Closed();
        using var __ = root;
        CampaignFixtures.SetLineEndings(root.Campaign(), crlf: true);
        root.Write("story/canon.md", root.Read("story/canon.md") + "- An edit by hand.\r\n");
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "changed: story/canon.md" }, result.StdoutLines);
    }

    /// <summary>A binary file (it holds a NUL) is compared byte for byte: a CR taken out of it is a change.</summary>
    [TestMethod]
    public void ABinaryFileDifferingByACrIsStillChanged()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string bin = Path.Combine(root.Campaign(), "maps", "picture.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(bin)!);
        File.WriteAllBytes(bin, [0x00, 0x41, 0x0D, 0x0A, 0x42]);
        root.SetStandard("1\n");
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        File.WriteAllBytes(bin, [0x00, 0x41, 0x0A, 0x42]);
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "changed: maps/picture.bin" }, result.StdoutLines);
    }

    /// <summary>
    /// The map page is derived: a closed campaign checked out from git without it, or with it drawn again, still verifies
    /// (spec 018 FR-005), and it is not in the count of files that match.
    /// </summary>
    [TestMethod]
    public void TheMapPageIsNotComparedSoACheckoutWithoutItVerifies()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        foreach (string path in CampaignFiles.DerivedFiles)
            root.Write(path, "drawn at close\n");
        root.SetStandard("1\n");
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        string backup = ClosedCampaign.Read(root.Campaign())!.Backup;
        File.Delete(Path.Combine(root.Campaign(), "maps", "live.html"));
        File.Delete(Path.Combine(root.Campaign(), "maps", "live-data.js"));
        root.Write("maps/live-source.txt", "drawn again\n");
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"verified rorn: closed, 6 files match {backup}" }, result.StdoutLines);
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

    /// <summary>
    /// With the closing backup gone from <c>backups/</c>, verify finds the copy in <c>exports/</c> (spec 033 FR-007); with that
    /// gone too, it says so, and <c>--backup</c> names the file.
    /// </summary>
    [TestMethod]
    public void AMarkNamingABackupThatIsNotThereFallsBackToExportsThenSaysSo()
    {
        var (root, backup) = Closed();
        using var _ = root;
        string moved = Path.Combine(root.Folder, "moved.zip");
        File.Move(root.InBackups(backup), moved);
        var fromExports = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(0, fromExports.ExitCode, fromExports.ToString());
        CollectionAssert.AreEqual(new[] { $"verified rorn: closed, 6 files match {backup}" }, fromExports.StdoutLines);

        File.Delete(Path.Combine(root.Folder, "exports", backup));
        var result = MigrationRoot.Run("verify", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc campaign: no backup {backup} in backups/ or exports/: name it with --backup", result.Stderr.TrimEnd());
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name, "--backup", moved).ExitCode);
    }
}
