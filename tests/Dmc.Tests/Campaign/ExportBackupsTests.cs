using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The export leaves a campaign's <c>backups/</c> out (FR-005d, SC-012), and a player still cannot point <c>--out</c> into
/// <c>adventures/</c>, the campaign's own <c>backups/</c> included (FR-005e).
/// </summary>
[TestClass]
public sealed class ExportBackupsTests
{
    /// <summary>Fills a campaign's <c>backups/</c> with zips and companion files, and a nested folder.</summary>
    static void FillBackups(string campaign)
    {
        for (int i = 0; i < 5; i++)
        {
            Write(campaign, $"backups/{CampaignName}_backup_2020010{i + 1}120000.zip", BinaryBytes());
            WriteText(campaign, $"backups/{CampaignName}_backup_2020010{i + 1}120000.zip.accounting.tsv", "# accounting\n");
        }
        WriteText(campaign, "backups/inner/notes.txt", "x");
    }

    [TestMethod]
    public void TheArchiveAndManifestHoldNoBackupsAndAreTheSameSize()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        var plain = CampaignExporter.Export(root.Folder, new ExportRequest(CampaignName, root.At("plain.zip"), false), Moment);
        FillBackups(campaign);
        var with = CampaignExporter.Export(root.Folder, new ExportRequest(CampaignName, root.At("with.zip"), false), Moment);

        var summary = ExportSummary.Open(with.Archive);
        Assert.IsFalse(summary.Manifest.Files.Any(f => f.Path.StartsWith("backups", StringComparison.Ordinal)));
        Assert.IsFalse(summary.Manifest.Folders.Any(f => f.StartsWith("backups", StringComparison.Ordinal)));
        Assert.AreEqual(plain.Files, with.Files);
        Assert.AreEqual(new FileInfo(plain.Archive).Length, new FileInfo(with.Archive).Length, "a campaign with backups exports to the same size");
    }

    [TestMethod]
    public void ABackupBesideFiveOthersIsNoLargerThanOneBesideNone()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        var alone = CampaignExporter.Backup(root.Folder, CampaignName, Moment);
        long size = new FileInfo(alone.Archive).Length;
        File.Delete(alone.Archive);
        FillBackups(campaign);
        var beside = CampaignExporter.Backup(root.Folder, CampaignName, Moment);
        Assert.AreEqual(size, new FileInfo(beside.Archive).Length);
    }

    [TestMethod]
    public void OutInsideAdventuresIsStillRefused()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        Directory.CreateDirectory(Path.Combine(campaign, "backups"));
        foreach (string output in new[] { root.Adventures, Path.Combine(campaign, "backups"), Path.Combine(campaign, "backups", "x.zip") })
        {
            var caught = Assert.ThrowsExactly<DmcException>(() =>
                CampaignExporter.Export(root.Folder, new ExportRequest(CampaignName, output, false), Moment));
            Assert.AreEqual("will not write an export inside adventures/: choose another --out", caught.Message, output);
        }
        Assert.IsEmpty(Directory.GetFiles(Path.Combine(campaign, "backups")));
    }

    [TestMethod]
    public void APlayersExportKeepsItsDefaultNameAndPlace()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var result = CampaignExporter.Export(root.Folder, new ExportRequest(CampaignName, null, false), Moment);
        string stamp = Moment.ToLocalTime().ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(Path.Combine(root.Folder, "exports", $"{CampaignName}-{stamp}.zip"), result.Archive);
    }
}
