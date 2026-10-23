using System.Text;
using System.Text.Json.Nodes;
using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign show</c>: up to three lines about an export, read from its manifest alone, and nothing written anywhere.
/// In-process against a temporary toolkit root and archives written by <see cref="CampaignFixtures"/>.
/// </summary>
[TestClass]
public sealed class CampaignShowTests
{
    /// <summary>The lines <c>show</c> prints for an archive, with the toolkit root to look for the module in.</summary>
    static string[] Show(TempRoot root, string zip) => [.. ExportSummary.Open(zip).ShowLines(root.Folder)];

    /// <summary>Writes the sample campaign's archive into the root and returns its path.</summary>
    static string Archive(TempRoot root, byte[]? generalOptions = null, Action<ArchiveParts>? mutate = null)
    {
        string campaign = WriteCampaign(root.Folder);
        string zip = root.At("export.zip");
        BuildArchive(zip, campaign, generalOptions: generalOptions, mutate: mutate);
        return zip;
    }

    /// <summary>The size of the sample campaign's files, as <c>show</c> should say it.</summary>
    static string SampleSize(TempRoot root) => ExportSummary.Size(ReadFiles(root.Campaign(CampaignName)).Values.Sum(b => (long)b.Length));

    [TestMethod]
    public void ThreeLinesForAModuleCampaignWithGeneralPlayerOptions()
    {
        using var root = new TempRoot();
        string zip = Archive(root, Encoding.UTF8.GetBytes(GeneralOptions));
        int files = ReadFiles(root.Campaign(CampaignName)).Count;

        string[] lines = Show(root, zip);

        CollectionAssert.AreEqual(new[]
        {
            "the-silver-egg: exported 2026-10-21 19:04 UTC by dmc 0.0.0-dev, format 1",
            $"{files} files, {SampleSize(root)}, plus the general player options",
            $"module: {ModuleName} (not in modules/)",
        }, lines);
    }

    [TestMethod]
    public void TheGeneralPlayerOptionsClauseIsLeftOutWithoutThem()
    {
        using var root = new TempRoot();
        string zip = Archive(root);
        string[] lines = Show(root, zip);
        Assert.AreEqual($"{ReadFiles(root.Campaign(CampaignName)).Count} files, {SampleSize(root)}", lines[1]);
    }

    [TestMethod]
    public void TheModuleLineSaysWhenTheModuleIsInModules()
    {
        using var root = new TempRoot();
        string zip = Archive(root);
        Directory.CreateDirectory(root.At("modules", ModuleName));
        File.WriteAllText(root.At("modules", ModuleName, "index.md"), "# index");
        Assert.AreEqual($"module: {ModuleName} (in modules/)", Show(root, zip)[2]);
    }

    [TestMethod]
    public void AModuleFolderWithoutItsIndexIsNotInModules()
    {
        using var root = new TempRoot();
        string zip = Archive(root);
        Directory.CreateDirectory(root.At("modules", ModuleName)); // only the digest's index.md counts
        Assert.AreEqual($"module: {ModuleName} (not in modules/)", Show(root, zip)[2]);
    }

    [TestMethod]
    public void TheModuleLineIsAbsentWithoutAModule()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.Manifest["module"] = null);
        Assert.HasCount(2, Show(root, zip));
    }

    [TestMethod]
    public void TheUnnamedModuleIsSaidSo()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.Manifest["module"] = new JsonObject { ["folder"] = null });
        Assert.AreEqual("module: unknown (module-progress.md names no module folder)", Show(root, zip)[2]);
    }

    [TestMethod]
    public void AnEmptyModuleFolderIsTheUnnamedModule()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.Manifest["module"] = new JsonObject { ["folder"] = "" });
        Assert.AreEqual("module: unknown (module-progress.md names no module folder)", Show(root, zip)[2]);
    }

    [TestMethod]
    public void NeverMoreThanThreeLines()
    {
        using var root = new TempRoot();
        string zip = Archive(root, Encoding.UTF8.GetBytes(GeneralOptions));
        Assert.IsLessThanOrEqualTo(3, Show(root, zip).Length);
    }

    [TestMethod]
    public void ShowCreatesAndChangesNothingAnywhere()
    {
        using var root = new TempRoot();
        string zip = Archive(root, Encoding.UTF8.GetBytes(GeneralOptions));
        WriteGeneralOptions(root.Folder);
        string[] before = Snapshot(root.Folder);

        Show(root, zip);

        CollectionAssert.AreEqual(before, Snapshot(root.Folder));
    }

    [TestMethod]
    public void AnArchiveWhoseContentIsDamagedStillShowsBecauseShowOnlyReadsTheManifest()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.FileRecord("game-state.md")["sha256"] = Sha([9]));
        Assert.HasCount(3, Show(root, zip));
    }

    [TestMethod]
    public void TheNameIsReadFromTheManifestNotTheFile()
    {
        using var root = new TempRoot();
        string zip = Archive(root);
        string renamed = root.At("backup.zip");
        File.Move(zip, renamed);
        StringAssert.StartsWith(Show(root, renamed)[0], "the-silver-egg: ");
    }

    [TestMethod]
    public void ControlCharactersInTheManifestCannotAddLines()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p =>
        {
            p.Manifest["campaign"] = "bad\nname\r\nhere";
            p.Manifest["dmcVersion"] = "1.0\n2.0";
        });
        string[] lines = Show(root, zip);
        Assert.HasCount(3, lines);
        Assert.AreEqual("bad?name??here: exported 2026-10-21 19:04 UTC by dmc 1.0?2.0, format 1", lines[0]);
    }

    // ---------- the refusals are the same as import's ----------

    [TestMethod]
    public void AMissingFileIsRefusedWithOneLine()
    {
        using var root = new TempRoot();
        string zip = root.At("missing.zip");
        var caught = Assert.ThrowsExactly<DmcException>(() => Show(root, zip));
        StringAssert.StartsWith(caught.Message, $"cannot read {zip}: ");
        Assert.AreEqual(1, caught.ExitCode);
    }

    [TestMethod]
    public void AFileThatIsNotAZipIsRefused()
    {
        using var root = new TempRoot();
        string zip = root.At("not.zip");
        File.WriteAllText(zip, "hello");
        var caught = Assert.ThrowsExactly<DmcException>(() => Show(root, zip));
        Assert.AreEqual($"{zip} is not a dmc campaign export", caught.Message);
    }

    [TestMethod]
    public void ANewerFormatIsRefused()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.Manifest["formatVersion"] = 2);
        var caught = Assert.ThrowsExactly<DmcException>(() => Show(root, zip));
        Assert.AreEqual($"{zip} was made by a newer dmc (format 2): update dmc and try again", caught.Message);
    }

    [TestMethod]
    public void AnArchiveThatDisagreesWithItsManifestIsRefusedLikeImportDoes()
    {
        using var root = new TempRoot();
        string zip = Archive(root, mutate: p => p.DropEntry("game-state.md"));
        var caught = Assert.ThrowsExactly<DmcException>(() => Show(root, zip));
        Assert.AreEqual($"{zip} is damaged: campaign/game-state.md is missing", caught.Message);
    }

    // ---------- the size ----------

    [TestMethod]
    [DataRow(0L, "0 bytes")]
    [DataRow(1L, "1 bytes")]
    [DataRow(1023L, "1023 bytes")]
    [DataRow(1024L, "1.0 KB")]
    [DataRow(1536L, "1.5 KB")]
    [DataRow(1048575L, "1.0 MB")] // would round to 1024.0 KB, which is a megabyte
    [DataRow(1048576L, "1.0 MB")]
    [DataRow(1468006L, "1.4 MB")]
    [DataRow(52428800L, "50.0 MB")]
    public void TheSizeIsInBytesKilobytesOrMegabytes(long bytes, string expected) => Assert.AreEqual(expected, ExportSummary.Size(bytes));
}
