using System.Text;
using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign import</c> of an archive that is right, in-process against a temporary toolkit root: the campaign comes
/// back byte for byte, an existing general player options file and the active campaign are never touched, and the
/// result says what the contract's lines need. The archives are written by <see cref="CampaignFixtures"/>, not by the
/// exporter, so these tests do not depend on it; the last two build on it to prove the whole round trip.
/// </summary>
[TestClass]
public sealed class CampaignRoundTripTests
{
    /// <summary>Imports an archive into a root, collecting what the importer wrote to standard error.</summary>
    static ImportResult Import(TempRoot root, string zip, string? name = null) =>
        CampaignImporter.Import(root.Folder, new ImportRequest(zip, name), TextWriter.Null);

    /// <summary>Writes the sample campaign in one root and its archive beside another root; returns the archive path.</summary>
    static string Archive(TempRoot source, string where, byte[]? generalOptions = null, string? name = null, Action<ArchiveParts>? mutate = null)
    {
        string campaign = WriteCampaign(source.Folder);
        string zip = Path.Combine(where, "export.zip");
        BuildArchive(zip, campaign, name, generalOptions, mutate);
        return zip;
    }

    [TestMethod]
    public void ACampaignComesBackUnderItsRecordedName()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);

        var result = Import(target, zip);

        Assert.AreEqual(CampaignName, result.Name);
        Assert.AreEqual(target.Campaign(CampaignName), result.Folder);
        Assert.AreEqual(ReadFiles(source.Campaign(CampaignName)).Count, result.Files);
        AssertSameFolder(source.Campaign(CampaignName), target.Campaign(CampaignName));
    }

    [TestMethod]
    public void ACampaignComesBackUnderAnotherNameWithAs()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);

        var result = Import(target, zip, "silver-egg-copy");

        Assert.AreEqual("silver-egg-copy", result.Name);
        AssertSameFolder(source.Campaign(CampaignName), target.Campaign("silver-egg-copy"));
        Assert.IsFalse(Directory.Exists(target.Campaign(CampaignName)), "the recorded name is not also made");
    }

    [TestMethod]
    public void TheNameOfTheFileDoesNotMatter()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);
        string renamed = Path.Combine(target.Folder, "backup.zip");
        File.Move(zip, renamed);
        Assert.AreEqual(CampaignName, Import(target, renamed).Name);
    }

    [TestMethod]
    public void BytesNamesAndEmptyFoldersSurvive()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string campaign = WriteCampaign(source.Folder);
        string zip = Path.Combine(target.Folder, "export.zip");
        BuildArchive(zip, campaign);
        Import(target, zip);

        string copy = target.Campaign(CampaignName);
        Assert.IsTrue(File.Exists(Path.Combine(copy, "story", "Écu.md")), "non-ASCII name");
        Assert.IsTrue(File.Exists(Path.Combine(copy, "notes", "two words.md")), "spaces in the name");
        CollectionAssert.AreEqual(BinaryBytes(), File.ReadAllBytes(Path.Combine(copy, "maps", "live.png")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(copy, "character-sheets", "pip.md")), "\r\n");
        Assert.IsTrue(Directory.Exists(Path.Combine(copy, "story", "empty-folder")));
        Assert.AreEqual(0, new FileInfo(Path.Combine(copy, "maps", ".gitkeep")).Length);
    }

    [TestMethod]
    public void AHandNamedFolderWithSpacesAndCapitalsSurvives()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string campaign = WriteCampaign(source.Folder, "The Silver Egg");
        string zip = Path.Combine(target.Folder, "export.zip");
        BuildArchive(zip, campaign);
        Assert.AreEqual("The Silver Egg", Import(target, zip).Name);
        AssertSameFolder(campaign, target.Campaign("The Silver Egg"));
    }

    [TestMethod]
    public void AnEmptyCampaignComesBackAsAnEmptyFolder()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string campaign = Directory.CreateDirectory(source.Campaign("blank")).FullName;
        string zip = Path.Combine(target.Folder, "export.zip");
        BuildArchive(zip, campaign);
        var result = Import(target, zip);
        Assert.AreEqual(0, result.Files);
        Assert.IsTrue(Directory.Exists(target.Campaign("blank")));
    }

    [TestMethod]
    public void NothingIsLeftBesideTheCampaign()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);
        Import(target, zip);
        CollectionAssert.AreEqual(new[] { CampaignName }, Directory.GetFileSystemEntries(target.Adventures).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public void TheActiveCampaignIsNeitherCreatedNorChanged()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);
        string active = Path.Combine(target.Adventures, ".active-campaign");

        Import(target, zip);
        Assert.IsFalse(File.Exists(active), "no .active-campaign is made");

        File.WriteAllText(active, "another-campaign");
        Import(target, zip, "second");
        Assert.AreEqual("another-campaign", File.ReadAllText(active));
    }

    [TestMethod]
    public void AnExistingCampaignIsNotTouched()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);
        string other = WritePlainCampaign(target.Folder, "other");
        string[] before = Snapshot(other);
        Import(target, zip);
        CollectionAssert.AreEqual(before, Snapshot(other));
    }

    // ---------- the general player options ----------

    [TestMethod]
    public void TheGeneralPlayerOptionsAreWrittenWhenThereAreNone()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder, Encoding.UTF8.GetBytes(GeneralOptions));

        var result = Import(target, zip);

        Assert.AreEqual(OptionsOutcome.Written, result.Options);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(GeneralOptions), File.ReadAllBytes(Path.Combine(target.Adventures, "player-options.md")));
    }

    [TestMethod]
    public void AnExistingGeneralPlayerOptionsFileIsLeftByteForByte()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder, Encoding.UTF8.GetBytes(GeneralOptions));
        byte[] mine = [0x6d, 0x69, 0x6e, 0x65, 0x0d, 0x0a, 0xc3, 0xa9];
        File.WriteAllBytes(Path.Combine(target.Adventures, "player-options.md"), mine);

        var result = Import(target, zip);

        Assert.AreEqual(OptionsOutcome.Kept, result.Options);
        CollectionAssert.AreEqual(mine, File.ReadAllBytes(Path.Combine(target.Adventures, "player-options.md")));
        Assert.IsTrue(Directory.Exists(target.Campaign(CampaignName)));
    }

    [TestMethod]
    public void AnExportWithoutGeneralPlayerOptionsSaysNothingAboutThem()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string zip = Archive(source, target.Folder);
        Assert.AreEqual(OptionsOutcome.None, Import(target, zip).Options);
        Assert.IsFalse(File.Exists(Path.Combine(target.Adventures, "player-options.md")));
    }

    [TestMethod]
    public void TheCampaignsOwnPlayerOptionsIsACampaignFile()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string campaign = WriteCampaign(source.Folder);
        WriteText(campaign, "player-options.md", "this campaign only\n");
        string zip = Path.Combine(target.Folder, "export.zip");
        BuildArchive(zip, campaign);

        Import(target, zip);

        Assert.AreEqual("this campaign only\n", File.ReadAllText(Path.Combine(target.Campaign(CampaignName), "player-options.md")));
        Assert.IsFalse(File.Exists(Path.Combine(target.Adventures, "player-options.md")));
    }

    // ---------- the module ----------

    [TestMethod]
    public void AModuleThatIsNotInModulesIsReported()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        var result = Import(target, Archive(source, target.Folder));
        Assert.AreEqual(new ModuleState(ModuleKind.Named, ModuleName, Installed: false), result.Module);
    }

    [TestMethod]
    public void AModuleThatIsInModulesIsNotReported()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        Directory.CreateDirectory(target.At("modules", ModuleName));
        File.WriteAllText(target.At("modules", ModuleName, "index.md"), "# index");
        var result = Import(target, Archive(source, target.Folder));
        Assert.AreEqual(new ModuleState(ModuleKind.Named, ModuleName, Installed: true), result.Module);
    }

    [TestMethod]
    public void AModuleCampaignWithNoModuleFolderIsReportedAsUnnamed()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        var result = Import(target, Archive(source, target.Folder, mutate: p => p.Manifest["module"] = new System.Text.Json.Nodes.JsonObject { ["folder"] = null }));
        Assert.AreEqual(new ModuleState(ModuleKind.Unnamed, null, Installed: false), result.Module);
    }

    [TestMethod]
    public void ACampaignWithoutAModuleHasNone()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        var result = Import(target, Archive(source, target.Folder, mutate: p => p.Manifest["module"] = null));
        Assert.AreEqual(new ModuleState(ModuleKind.None, null, Installed: false), result.Module);
    }

    // ---------- the whole round trip, through the exporter ----------

    [TestMethod]
    public void ExportThenImportGivesAnIdenticalFolder()
    {
        using var source = new TempRoot();
        using var target = new TempRoot();
        string campaign = WriteCampaign(source.Folder);
        WriteGeneralOptions(source.Folder);

        string zip = CampaignExporter.Export(source.Folder, new ExportRequest(CampaignName, null, false), Moment).Archive;
        var result = Import(target, zip, "other");

        AssertSameFolder(campaign, target.Campaign("other"));
        Assert.AreEqual(OptionsOutcome.Written, result.Options);
    }

    [TestMethod]
    public void ExportThenImportIntoTheSameToolkitMakesACopy()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        string[] before = Snapshot(campaign);

        string zip = CampaignExporter.Export(root.Folder, new ExportRequest(CampaignName, null, false), Moment).Archive;
        Import(root, zip, "silver-egg-copy");

        AssertSameFolder(campaign, root.Campaign("silver-egg-copy"));
        CollectionAssert.AreEqual(before, Snapshot(campaign), "the original is untouched");
    }
}
