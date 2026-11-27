using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign compare</c> (spec 033, issue #189; contracts/campaign-compare.md): a campaign against any export of it, by
/// path and content, writing nothing, pass or fail. Also the closed campaign's lookup of its closing backup in
/// <c>exports/</c>, which <c>verify</c> shares.
/// </summary>
[TestClass]
public sealed class CampaignCompareTests
{
    /// <summary>Everything compare could write: the campaigns, their backups and <c>exports/</c>.</summary>
    static string[] Everything(MigrationRoot root) => DmcFolders.Snapshot(root.Folder, "adventures", "exports");

    /// <summary>A sample campaign with one backup, and the backup's file name.</summary>
    static (MigrationRoot Root, string Backup) BackedUp()
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        return (root, root.Backup());
    }

    /// <summary>A campaign unchanged since its backup matches it, and nothing is written (US1 scenario 1, SC-002).</summary>
    [TestMethod]
    public void AnUnchangedCampaignMatchesItsLatestBackupAndNothingIsWritten()
    {
        var (root, backup) = BackedUp();
        using var _ = root;
        string[] before = Everything(root);
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"rorn matches {backup}: 4 files" }, result.StdoutLines);
        CollectionAssert.AreEqual(before, Everything(root));
    }

    /// <summary>An edit, a new file and a deleted one are each named, and it fails, writing nothing (US1 scenario 2).</summary>
    [TestMethod]
    public void EachChangedAddedAndMissingFileIsNamedAndItFailsWritingNothing()
    {
        var (root, backup) = BackedUp();
        using var _ = root;
        root.Write("story/canon.md", root.Read("story/canon.md") + "- Played since the backup.\n");
        root.Write("story/new.md", "# New\n");
        File.Delete(Path.Combine(root.Campaign(), "character-sheets", "pc-wren.md"));
        string[] before = Everything(root);
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "missing: character-sheets/pc-wren.md", "changed: story/canon.md", "added: story/new.md" }, result.StdoutLines);
        Assert.AreEqual($"dmc campaign: 3 files differ from {backup}", result.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, Everything(root));
    }

    /// <summary>The latest backup is the default; <c>--with</c> names an earlier one (FR-003, FR-004).</summary>
    [TestMethod]
    public void TheLatestBackupIsTheDefaultAndWithNamesAnEarlierOne()
    {
        var (root, first) = BackedUp();
        using var _ = root;
        root.Write("story/canon.md", root.Read("story/canon.md") + "- Played since the first backup.\n");
        string second = root.Backup();
        Assert.AreNotEqual(first, second);
        Assert.AreEqual(0, MigrationRoot.Run("compare", MigrationRoot.Name).ExitCode, "the latest holds the edit");

        var earlier = MigrationRoot.Run("compare", MigrationRoot.Name, "--with", first);
        Assert.AreEqual(1, earlier.ExitCode);
        CollectionAssert.AreEqual(new[] { "changed: story/canon.md" }, earlier.StdoutLines);
        Assert.AreEqual($"dmc campaign: 1 file differs from {first}", earlier.Stderr.TrimEnd());
    }

    /// <summary><c>.standard</c> is compared, so a standard recorded since the backup shows (FR-001).</summary>
    [TestMethod]
    public void AStandardRecordedSinceTheBackupIsAChange()
    {
        var (root, _) = BackedUp();
        using var __ = root;
        root.SetStandard("2\n");
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "added: .standard" }, result.StdoutLines);
    }

    /// <summary>Line endings alone and the map page are no change (spec 018 FR-005).</summary>
    [TestMethod]
    public void LineEndingsAndTheMapPageAreNoChange()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        foreach (string path in CampaignFiles.DerivedFiles)
            root.Write(path, "drawn at the backup\n");
        string backup = root.Backup();
        Assert.IsGreaterThan(0, CampaignFixtures.SetLineEndings(root.Campaign(), crlf: true));
        File.Delete(Path.Combine(root.Campaign(), "maps", "live.html"));
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"rorn matches {backup}: 4 files" }, result.StdoutLines);
    }

    /// <summary>An import made <c>--as</c> another name matches its file, after a note naming the other campaign (US2 scenario 1).</summary>
    [TestMethod]
    public void AnImportUnderAnotherNameMatchesTheFileItCameFromWithANote()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var export = MigrationRoot.Run("export", MigrationRoot.Name);
        Assert.AreEqual(0, export.ExitCode, export.ToString());
        string file = Directory.EnumerateFiles(Path.Combine(root.Folder, "exports")).Single();
        Assert.AreEqual(0, MigrationRoot.Run("import", file, "--as", "rorn-copy").ExitCode);

        var result = MigrationRoot.Run("compare", "rorn-copy", "--with", file);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        string name = Path.GetFileName(file);
        CollectionAssert.AreEqual(new[] { $"note: {name} is an export of rorn", $"rorn-copy matches {name}: 4 files" }, result.StdoutLines);
    }

    /// <summary>An open campaign with no backup and no <c>--with</c> is one line.</summary>
    [TestMethod]
    public void NoBackupNeedsWith()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc campaign: rorn has no backup: name an export with --with FILE", result.Stderr.TrimEnd());
    }

    /// <summary>A <c>--with</c> that names no file says so in one line.</summary>
    [TestMethod]
    public void AWithThatNamesNoFileSaysSo()
    {
        var (root, _) = BackedUp();
        using var __ = root;
        var result = MigrationRoot.Run("compare", MigrationRoot.Name, "--with", "nothing.zip");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc campaign: no backup nothing.zip: dmc campaign backups rorn lists them", result.Stderr.TrimEnd());
    }

    /// <summary>A damaged export is refused with the export check's one line, and nothing is written (US2 scenario 2).</summary>
    [TestMethod]
    public void ADamagedExportIsRefusedAndNothingIsWritten()
    {
        var (root, backup) = BackedUp();
        using var _ = root;
        string path = root.InBackups(backup);
        byte[] bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..(bytes.Length / 2)]);
        string[] before = Everything(root);
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.StartsWith(result.Stderr, "dmc campaign: ");
        Assert.AreEqual(1, result.Stderr.TrimEnd().Split('\n').Length, "one line");
        CollectionAssert.AreEqual(before, Everything(root));
    }

    /// <summary>Forty difference lines, then a count, unless <c>--all</c>.</summary>
    [TestMethod]
    public void FortyLinesThenACountUnlessAll()
    {
        var (root, _) = BackedUp();
        using var __ = root;
        for (int i = 0; i < 45; i++)
            root.Write($"story/extra-{i:00}.md", "x\n");
        var capped = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, capped.ExitCode);
        Assert.HasCount(41, capped.StdoutLines);
        Assert.AreEqual("5 more", capped.StdoutLines[^1]);
        var all = MigrationRoot.Run("compare", MigrationRoot.Name, "--all");
        Assert.HasCount(45, all.StdoutLines);
        StringAssert.Contains(all.Stderr, "45 files differ");
    }

    /// <summary>A closed campaign is compared with its closing backup by default, writing nothing (US3 scenario 1).</summary>
    [TestMethod]
    public void AClosedCampaignIsComparedWithItsClosingBackup()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        string backup = ClosedCampaign.Read(root.Campaign())!.Backup;
        string[] before = Everything(root);
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { $"rorn matches {backup}: 5 files" }, result.StdoutLines, "four files and .closed");
        CollectionAssert.AreEqual(before, Everything(root));
    }

    /// <summary>
    /// An import of the closing backup's copy in <c>exports/</c> has no <c>backups/</c>: compare and verify both find the copy
    /// with no file named (spec 033 FR-003, FR-007).
    /// </summary>
    [TestMethod]
    public void AnImportedClosedCampaignFindsItsClosingBackupInExports()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        string backup = ClosedCampaign.Read(root.Campaign())!.Backup;
        Assert.AreEqual(0, MigrationRoot.Run("import", Path.Combine(root.Folder, "exports", backup), "--as", "rorn-copy").ExitCode);
        Assert.IsFalse(File.Exists(root.InBackups(backup, "rorn-copy")));

        var compare = MigrationRoot.Run("compare", "rorn-copy");
        Assert.AreEqual(0, compare.ExitCode, compare.ToString());
        CollectionAssert.AreEqual(new[] { $"note: {backup} is an export of rorn", $"rorn-copy matches {backup}: 5 files" }, compare.StdoutLines);

        var verify = MigrationRoot.Run("verify", "rorn-copy");
        Assert.AreEqual(0, verify.ExitCode, verify.ToString());
        CollectionAssert.AreEqual(new[] { $"verified rorn-copy: closed, 5 files match {backup}" }, verify.StdoutLines);
    }

    /// <summary>A closing backup in neither <c>backups/</c> nor <c>exports/</c> needs <c>--with</c>, which then works.</summary>
    [TestMethod]
    public void AClosingBackupInNeitherFolderNeedsWith()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        string backup = ClosedCampaign.Read(root.Campaign())!.Backup;
        string moved = Path.Combine(root.Folder, "moved.zip");
        File.Move(root.InBackups(backup), moved);
        File.Delete(Path.Combine(root.Folder, "exports", backup));

        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc campaign: no backup {backup} in backups/ or exports/: name it with --with", result.Stderr.TrimEnd());
        Assert.AreEqual(0, MigrationRoot.Run("compare", MigrationRoot.Name, "--with", moved).ExitCode);
    }

    /// <summary>A closed campaign whose mark cannot be read needs <c>--with</c>.</summary>
    [TestMethod]
    public void AnUnreadableMarkNeedsWith()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        root.Write(".closed", "garbled");
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc campaign: adventures/rorn/.closed is unreadable: name the closing backup with --with", result.Stderr.TrimEnd());
    }

    /// <summary>A migration in progress is left alone: its mark and the campaign are untouched.</summary>
    [TestMethod]
    public void AMigrationInProgressIsLeftAlone()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(2);
        string backup = root.Backup();
        Assert.IsTrue(File.Exists(root.InBackups(backup + ".migrating")));
        string[] before = Everything(root);
        var result = MigrationRoot.Run("compare", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(before, Everything(root), "the mark and the campaign are untouched");
    }

    /// <summary><c>verify</c>'s options are not <c>compare</c>'s: a usage mistake, exit 2.</summary>
    [TestMethod]
    public void TheOptionsAreItsOwn()
    {
        var (root, _) = BackedUp();
        using var __ = root;
        var result = MigrationRoot.Run("compare", MigrationRoot.Name, "--list");
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc campaign: unrecognized arguments: --list", result.Stderr.TrimEnd());
    }

    /// <summary>On a closed campaign, a bare file name given to <c>--with</c> is found in <c>exports/</c> too, as the default is.</summary>
    [TestMethod]
    public void ABareWithNameOnAClosedCampaignIsFoundInExports()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        string backup = ClosedCampaign.Read(root.Campaign())!.Backup;
        Assert.AreEqual(0, MigrationRoot.Run("import", Path.Combine(root.Folder, "exports", backup), "--as", "rorn-copy").ExitCode);
        var compare = MigrationRoot.Run("compare", "rorn-copy", "--with", backup);
        Assert.AreEqual(0, compare.ExitCode, compare.ToString());
        var verify = MigrationRoot.Run("verify", "rorn-copy", "--backup", backup);
        Assert.AreEqual(0, verify.ExitCode, verify.ToString());
    }
}
