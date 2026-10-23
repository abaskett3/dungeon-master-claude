using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign export</c>, in-process against a temporary toolkit root: the archive holds the campaign byte for byte
/// with a manifest that tells the truth, nothing in <c>adventures/</c> changes, and every refusal of the contract leaves
/// nothing behind.
/// </summary>
[TestClass]
public sealed class CampaignExportTests
{
    /// <summary>The local-time stamp the default file name carries for <see cref="Moment"/> (or a later minute).</summary>
    static string Stamp(DateTime moment) => moment.ToLocalTime().ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);

    /// <summary>Runs the exporter on the temporary root with the fixed moment.</summary>
    static ExportResult Export(TempRoot root, string name = CampaignName, string? output = null, bool force = false, DateTime? now = null) =>
        CampaignExporter.Export(root.Folder, new ExportRequest(name, output, force), now ?? Moment);

    /// <summary>The names of the entries of an archive, in order, and the bytes of each.</summary>
    static (List<string> Names, Dictionary<string, byte[]> Bytes) Open(string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Read, Encoding.UTF8);
        var names = zip.Entries.Select(e => e.FullName).ToList();
        var bytes = new Dictionary<string, byte[]>();
        foreach (var entry in zip.Entries)
        {
            using var stream = entry.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            bytes[entry.FullName] = copy.ToArray();
        }
        return (names, bytes);
    }

    /// <summary>The manifest of an archive as a JSON object.</summary>
    static JsonObject Manifest(string zipPath) =>
        JsonNode.Parse(Open(zipPath).Bytes["manifest.json"])!.AsObject();

    // ---------- what the archive holds ----------

    [TestMethod]
    public void TheManifestIsTheFirstEntry()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var result = Export(root);
        Assert.AreEqual("manifest.json", Open(result.Archive).Names[0]);
    }

    [TestMethod]
    public void EveryCampaignFileIsInTheArchiveByteForByte()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        var result = Export(root);

        var (names, bytes) = Open(result.Archive);
        var files = ReadFiles(campaign);
        foreach (var (path, expected) in files)
            CollectionAssert.AreEqual(expected, bytes["campaign/" + path], $"{path} differs");
        // The CRLF file stayed CRLF, and the binary file stayed binary.
        StringAssert.Contains(Encoding.UTF8.GetString(bytes["campaign/character-sheets/pip.md"]), "\r\n");
        CollectionAssert.AreEqual(BinaryBytes(), bytes["campaign/maps/live.png"]);
        // Nothing else is in the archive but the manifest and the directory entry of the empty folder.
        CollectionAssert.AreEquivalent(
            new[] { "manifest.json", "campaign/story/empty-folder/" }.Concat(files.Keys.Select(p => "campaign/" + p)).ToArray(),
            names.ToArray());
    }

    [TestMethod]
    public void EveryManifestSizeAndChecksumIsRight()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        var manifest = Manifest(Export(root).Archive);

        var files = ReadFiles(campaign);
        var records = manifest["files"]!.AsArray().Select(f => f!.AsObject()).ToList();
        CollectionAssert.AreEqual(files.Keys.ToArray(), records.Select(r => (string)r["path"]!).ToArray(), "sorted by path, ordinal");
        foreach (var record in records)
        {
            byte[] expected = files[(string)record["path"]!];
            Assert.AreEqual(expected.Length, (long)record["size"]!);
            Assert.AreEqual(Sha(expected), (string)record["sha256"]!);
        }
    }

    [TestMethod]
    public void EmptyFoldersAreInTheManifestAndAsDirectoryEntries()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        Directory.CreateDirectory(Path.Combine(root.Campaign(CampaignName), "zzz", "deep", "empty"));
        var result = Export(root);

        var folders = Manifest(result.Archive)["folders"]!.AsArray().Select(f => (string)f!).ToArray();
        CollectionAssert.AreEqual(new[] { "story/empty-folder", "zzz/deep/empty" }, folders);
        var names = Open(result.Archive).Names;
        CollectionAssert.Contains(names, "campaign/story/empty-folder/");
        CollectionAssert.Contains(names, "campaign/zzz/deep/empty/");
    }

    [TestMethod]
    public void TheManifestSaysWhoWhatAndWhen()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var manifest = Manifest(Export(root).Archive);

        Assert.AreEqual(1, (int)manifest["formatVersion"]!);
        Assert.AreEqual(CampaignName, (string)manifest["campaign"]!);
        Assert.AreEqual("2026-10-21T19:04:05Z", (string)manifest["exportedAt"]!);
        Assert.AreEqual(CommandLine.Version, (string)manifest["dmcVersion"]!);
    }

    [TestMethod]
    public void ThePathsAreSortedByOrdinalComparisonOfThePath()
    {
        using var root = new TempRoot();
        string campaign = WritePlainCampaign(root.Folder);
        // "a-b.md" sorts before "a/b.md" by ordinal comparison of the path, though the folder "a" sorts before "a-b.md" by name.
        WriteText(campaign, "a/b.md", "x");
        WriteText(campaign, "a-b.md", "y");
        var paths = Manifest(Export(root).Archive)["files"]!.AsArray().Select(f => (string)f!["path"]!).ToArray();
        CollectionAssert.AreEqual(paths.Order(StringComparer.Ordinal).ToArray(), paths);
        Assert.IsLessThan(Array.IndexOf(paths, "a/b.md"), Array.IndexOf(paths, "a-b.md"));
    }

    [TestMethod]
    public void ANonToolkitFolderIsExportedLikeAnyOther()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        WriteText(campaign, "import/chat-2026-03.txt", "an earlier chat export\r\n");
        var result = Export(root);
        CollectionAssert.AreEqual(
            Encoding.UTF8.GetBytes("an earlier chat export\r\n"),
            Open(result.Archive).Bytes["campaign/import/chat-2026-03.txt"]);
    }

    [TestMethod]
    public void TheResultCountsTheCampaignFilesOnly()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        WriteGeneralOptions(root.Folder);
        var result = Export(root);
        Assert.AreEqual(ReadFiles(campaign).Count, result.Files);
        Assert.IsTrue(result.GeneralOptions);
    }

    // ---------- the module ----------

    [TestMethod]
    public void AModuleCampaignRecordsItsModuleFolder()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var module = Manifest(Export(root).Archive)["module"]!.AsObject();
        Assert.AreEqual(ModuleName, (string)module["folder"]!);
    }

    [TestMethod]
    public void ACampaignWithoutModuleProgressRecordsNoModule()
    {
        using var root = new TempRoot();
        WritePlainCampaign(root.Folder);
        Assert.IsNull(Manifest(Export(root).Archive)["module"]);
    }

    [TestMethod]
    [DataRow("# Module progress\n\n- **Module**: Something\n", DisplayName = "no Module folder line")]
    [DataRow("- **Module folder**:\n", DisplayName = "an empty Module folder line")]
    [DataRow("- **Module folder**: `modules/<module>/`\n", DisplayName = "the template's placeholder")]
    [DataRow("- **Module folder**: `modules/../../etc/`\n", DisplayName = "a folder that is not one safe name")]
    public void AModuleProgressFileThatNamesNoFolderRecordsAnUnnamedModule(string progress)
    {
        using var root = new TempRoot();
        string campaign = WritePlainCampaign(root.Folder);
        WriteText(campaign, "module-progress.md", progress);
        var module = Manifest(Export(root).Archive)["module"]!.AsObject();
        Assert.IsTrue(module.ContainsKey("folder"));
        Assert.IsNull(module["folder"]);
    }

    [TestMethod]
    [DataRow("- **Module folder**: modules/lost-mine/\n", "lost-mine")]
    [DataRow("- **Module folder**: `modules/lost-mine`\n", "lost-mine")]
    [DataRow("- **Module folder**:   `modules/lost-mine/`  \r\n", "lost-mine")]
    public void TheModuleFolderIsReadFromTheLine(string progress, string folder)
    {
        using var root = new TempRoot();
        string campaign = WritePlainCampaign(root.Folder);
        WriteText(campaign, "module-progress.md", progress);
        var module = Manifest(Export(root).Archive)["module"]!.AsObject();
        Assert.AreEqual(folder, (string)module["folder"]!);
    }

    // ---------- the general player options ----------

    [TestMethod]
    public void TheGeneralPlayerOptionsTravelWithTheExport()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        WriteGeneralOptions(root.Folder);
        var result = Export(root);

        var (names, bytes) = Open(result.Archive);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(GeneralOptions), bytes["player-options.md"]);
        var record = Manifest(result.Archive)["playerOptions"]!.AsObject();
        Assert.AreEqual("player-options.md", (string)record["path"]!);
        Assert.AreEqual(Sha(Encoding.UTF8.GetBytes(GeneralOptions)), (string)record["sha256"]!);
        Assert.AreEqual(GeneralOptions.Length, (long)record["size"]!);
        Assert.IsTrue(result.GeneralOptions);
        // The campaign's own player-options.md is a campaign file, apart from the general one.
        Assert.IsFalse(names.Contains("campaign/player-options.md"));
    }

    [TestMethod]
    public void WithoutGeneralPlayerOptionsThereIsNoEntryAndNoRecord()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var result = Export(root);
        Assert.IsFalse(Open(result.Archive).Names.Contains("player-options.md"));
        Assert.IsNull(Manifest(result.Archive)["playerOptions"]);
        Assert.IsFalse(result.GeneralOptions);
    }

    // ---------- nothing in adventures/ changes ----------

    [TestMethod]
    public void ExportingChangesNothingUnderAdventures()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        WritePlainCampaign(root.Folder, "other");
        WriteGeneralOptions(root.Folder);
        File.WriteAllText(Path.Combine(root.Adventures, ".active-campaign"), CampaignName);
        string[] before = Snapshot(root.Adventures);

        Export(root);

        CollectionAssert.AreEqual(before, Snapshot(root.Adventures));
    }

    // ---------- where the file goes and what it is called ----------

    [TestMethod]
    public void TheDefaultDestinationIsExportsNamedWithTheLocalMinute()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        Assert.IsFalse(Directory.Exists(root.At("exports")));

        var result = Export(root);

        string expected = root.At("exports", $"{CampaignName}-{Stamp(Moment)}.zip");
        Assert.AreEqual(expected, result.Archive);
        Assert.IsTrue(File.Exists(expected));
        Assert.AreEqual(12, Stamp(Moment).Length);
    }

    [TestMethod]
    public void ASecondExportInTheSameMinuteGetsTwoAndAThirdThree()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string stem = root.At("exports", $"{CampaignName}-{Stamp(Moment)}");

        Assert.AreEqual(stem + ".zip", Export(root).Archive);
        Assert.AreEqual(stem + "-2.zip", Export(root, now: Moment.AddSeconds(10)).Archive);
        Assert.AreEqual(stem + "-3.zip", Export(root, now: Moment.AddSeconds(20)).Archive);
    }

    [TestMethod]
    public void AnExportAMinuteLaterGetsANewStampWithNoSuffix()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        Export(root);
        var later = Moment.AddMinutes(1);
        var result = Export(root, now: later);
        Assert.AreEqual(root.At("exports", $"{CampaignName}-{Stamp(later)}.zip"), result.Archive);
        Assert.AreNotEqual(Stamp(Moment), Stamp(later));
    }

    [TestMethod]
    public void TheStampAndTheRecordedTimeComeFromOneReading()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        // Local time can fall on the previous or next day; the manifest is always UTC.
        var moment = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var result = Export(root, now: moment);
        StringAssert.EndsWith(result.Archive, $"-{Stamp(moment)}.zip");
        Assert.AreEqual("2026-12-31T23:59:59Z", (string)Manifest(result.Archive)["exportedAt"]!);
    }

    [TestMethod]
    public void OutAsAnExistingFolderUsesTheDefaultNameInside()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string folder = Directory.CreateDirectory(root.At("backups")).FullName;
        var result = Export(root, output: folder);
        Assert.AreEqual(Path.Combine(folder, $"{CampaignName}-{Stamp(Moment)}.zip"), result.Archive);
        Assert.IsFalse(Directory.Exists(root.At("exports")), "the default folder is not made when --out names another");
    }

    [TestMethod]
    public void OutAsAZipPathUsesThatPathAndMakesItsFolder()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("backups", "monday", "egg.zip");
        var result = Export(root, output: target);
        Assert.AreEqual(target, result.Archive);
        Assert.IsTrue(File.Exists(target));
    }

    [TestMethod]
    public void OutWithAnUpperCaseExtensionIsAFileToo()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("EGG.ZIP");
        Assert.AreEqual(target, Export(root, output: target).Archive);
    }

    [TestMethod]
    public void TheArchiveOpensInAnyZipReaderAndNothingIsLeftBesideIt()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var result = Export(root);
        using (ZipFile.OpenRead(result.Archive)) { }
        Assert.AreEqual(1, Directory.GetFileSystemEntries(root.At("exports")).Length, "no partial file is left");
    }

    // ---------- refusals ----------

    /// <summary>Asserts that an export is refused with exactly this message and exit code 1, and left no archive, no exports folder and no partial file under the root.</summary>
    static void AssertRefused(TempRoot root, string message, string name = CampaignName, string? output = null, bool force = false,
        bool exportsMayExist = false)
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => Export(root, name, output, force));
        Assert.AreEqual(message, caught.Message);
        Assert.AreEqual(1, caught.ExitCode);
        AssertNothingWritten(root, exportsMayExist);
    }

    /// <summary>Asserts that no <c>.partial-</c> file exists anywhere under the root and, unless allowed, that no <c>exports</c> folder was made.</summary>
    static void AssertNothingWritten(TempRoot root, bool exportsMayExist = false)
    {
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(root.Folder, "*.partial-*", SearchOption.AllDirectories).ToArray(), "a partial file was left");
        if (!exportsMayExist)
            Assert.IsFalse(Directory.Exists(root.At("exports")) || File.Exists(root.At("exports")), "an exports folder was made");
    }

    [TestMethod]
    public void NoSuchCampaignListsTheOnesThatExist()
    {
        using var root = new TempRoot();
        WritePlainCampaign(root.Folder, "b-camp");
        WritePlainCampaign(root.Folder, "a-camp");
        Directory.CreateDirectory(root.At("adventures", ".dmc-import-abc123")); // a staging folder is not a campaign
        File.WriteAllText(root.At("adventures", "player-options.md"), "x"); // nor is a file
        AssertRefused(root, "no campaign \"x\" in adventures/ (there are: a-camp, b-camp)", name: "x");
    }

    [TestMethod]
    public void NoSuchCampaignWithNoneAtAll()
    {
        using var root = new TempRoot();
        AssertRefused(root, "no campaign \"x\" in adventures/ (there are none)", name: "x");
    }

    [TestMethod]
    public void NoSuchCampaignWhenThereIsNoAdventuresFolderAtAll()
    {
        using var root = new TempRoot();
        Directory.Delete(root.Adventures);
        AssertRefused(root, "no campaign \"x\" in adventures/ (there are none)", name: "x");
    }

    [TestMethod]
    [DataRow("a/b")]
    [DataRow("a\\b")]
    [DataRow("..")]
    [DataRow(".")]
    [DataRow("")]
    [DataRow(".dmc-import-abc123")]
    public void ANameThatIsNotOneCampaignFolderIsNoCampaign(string name)
    {
        using var root = new TempRoot();
        WritePlainCampaign(root.Folder, "a-camp");
        Directory.CreateDirectory(root.At("adventures", "a-camp", "b"));
        Directory.CreateDirectory(root.At("adventures", ".dmc-import-abc123"));
        AssertRefused(root, $"no campaign \"{name}\" in adventures/ (there are: a-camp)", name: name);
    }

    [TestMethod]
    public void WillNotWriteInsideAdventures()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        const string message = "will not write an export inside adventures/: choose another --out";
        AssertRefused(root, message, output: root.Adventures);
        AssertRefused(root, message, output: root.At("adventures", "egg.zip"));
        AssertRefused(root, message, output: Path.Combine(campaign, "egg.zip"));
        AssertRefused(root, message, output: Path.Combine(campaign, "story"));
        AssertRefused(root, message, output: root.At("adventures", "new-folder", "egg.zip"));
        // A path that reaches adventures/ by way of "..".
        AssertRefused(root, message, output: root.At("exports", "..", "adventures", "egg.zip"));
    }

    [TestMethod]
    public void AFolderThatOnlyStartsLikeAdventuresIsNotInsideIt()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("adventures-backup", "egg.zip");
        Assert.AreEqual(target, Export(root, output: target).Archive);
    }

    [TestMethod]
    public void AnOutFileThatExistsIsRefusedWithoutForce()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("egg.zip");
        File.WriteAllText(target, "precious");
        AssertRefused(root, $"{target} exists: add --force to replace it, or choose another --out", output: target);
        Assert.AreEqual("precious", File.ReadAllText(target));
    }

    [TestMethod]
    public void ForceReplacesAnOutFile()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("egg.zip");
        File.WriteAllText(target, "old");
        var result = Export(root, output: target, force: true);
        Assert.AreEqual(target, result.Archive);
        Assert.AreEqual("manifest.json", Open(target).Names[0]);
        AssertNothingWritten(root);
    }

    [TestMethod]
    public void ForceDoesNotReplaceTheDefaultName()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string first = Export(root).Archive;
        long size = new FileInfo(first).Length;
        byte[] bytes = File.ReadAllBytes(first);

        var second = Export(root, force: true, now: Moment.AddSeconds(5));

        Assert.AreEqual(first[..^4] + "-2.zip", second.Archive);
        CollectionAssert.AreEqual(bytes, File.ReadAllBytes(first), "the first export is untouched");
        Assert.AreEqual(size, new FileInfo(first).Length);
    }

    [TestMethod]
    public void OutThatIsNeitherAFolderNorAZipNameIsRefused()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string target = root.At("backups-not-made");
        AssertRefused(root, $"{target} is not an existing folder or a file name ending in .zip: choose another --out", output: target);
        Assert.IsFalse(Directory.Exists(target));

        string text = root.At("notes.txt");
        File.WriteAllText(text, "x");
        AssertRefused(root, $"{text} is not an existing folder or a file name ending in .zip: choose another --out", output: text);
    }

    [TestMethod]
    public void ASymbolicLinkToAFileInTheCampaignIsRefused()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        string outside = root.At("secret.txt");
        File.WriteAllText(outside, "not part of the campaign");
        MakeLink(() => File.CreateSymbolicLink(Path.Combine(campaign, "maps", "link.md"), outside));
        AssertRefused(root, "maps/link.md is a link: exports hold only the campaign's own files");
    }

    [TestMethod]
    public void ASymbolicLinkToAFolderInTheCampaignIsRefused()
    {
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        string outside = Directory.CreateDirectory(root.At("elsewhere")).FullName;
        File.WriteAllText(Path.Combine(outside, "x.md"), "x");
        MakeLink(() => Directory.CreateSymbolicLink(Path.Combine(campaign, "story", "linked"), outside));
        AssertRefused(root, "story/linked is a link: exports hold only the campaign's own files");
    }

    [TestMethod]
    public void AJunctionInTheCampaignIsRefused()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Junctions are a Windows feature; Windows runs this");
        using var root = new TempRoot();
        string campaign = WriteCampaign(root.Folder);
        string outside = Directory.CreateDirectory(root.At("elsewhere")).FullName;
        File.WriteAllText(Path.Combine(outside, "x.md"), "x");
        // A junction needs no special permission, unlike a symbolic link.
        var made = TestEnv.Run("cmd.exe", ["/c", "mklink", "/J", Path.Combine(campaign, "story", "junction"), outside]);
        Assert.AreEqual(0, made.ExitCode, made.ToString());
        AssertRefused(root, "story/junction is a link: exports hold only the campaign's own files");
    }

    [TestMethod]
    public void AnOutPathTheSystemCannotReadIsOneLine()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        var caught = Assert.ThrowsExactly<DmcException>(() => Export(root, output: root.At("a b.zip")));
        StringAssert.StartsWith(caught.Message, "could not export: ");
        AssertNothingWritten(root);
    }

    [TestMethod]
    public void ACampaignFolderThatIsAJunctionIsRefused()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Junctions are a Windows feature; Windows runs this");
        using var root = new TempRoot();
        string real = WriteCampaign(root.Folder, "real-camp");
        var made = TestEnv.Run("cmd.exe", ["/c", "mklink", "/J", root.Campaign("linked-camp"), real]);
        Assert.AreEqual(0, made.ExitCode, made.ToString());
        AssertRefused(root, "linked-camp is a link: exports hold only the campaign's own files", name: "linked-camp");
    }

    [TestMethod]
    public void AGeneralPlayerOptionsFileThatIsALinkIsRefused()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        string outside = root.At("secret.md");
        File.WriteAllText(outside, "not part of any campaign");
        MakeLink(() => File.CreateSymbolicLink(root.At("adventures", "player-options.md"), outside));
        AssertRefused(root, "adventures/player-options.md is a link: exports hold only the campaign's own files");
    }

    /// <summary>Makes a link, or skips the test where this account may not (Windows without developer mode); on CI the account may, so a failure there fails the test.</summary>
    /// <param name="make">Creates the link.</param>
    static void MakeLink(Action make)
    {
        try
        {
            make();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            TestEnv.Need(false, $"permission to create symbolic links ({e.Message})");
        }
    }

    [TestMethod]
    public void ACampaignHoldingAPathThatCouldNotBeImportedIsRefused()
    {
        if (OperatingSystem.IsWindows())
            Assert.Inconclusive("Windows cannot hold a file name with a backslash; Linux and macOS run this");
        using var root = new TempRoot();
        string campaign = WritePlainCampaign(root.Folder);
        WriteText(campaign, "story/a\\b.md", "x");
        AssertRefused(root, "story/a\\b.md cannot be exported: contains a backslash");
    }

    [TestMethod]
    public void AWriteThatFailsLeavesNoPartialFile()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        // The destination's parent is a file, so the folder for the archive cannot be made.
        string blocker = root.At("blocker.txt");
        File.WriteAllText(blocker, "x");
        var caught = Assert.ThrowsExactly<DmcException>(() => Export(root, output: Path.Combine(blocker, "egg.zip")));
        StringAssert.StartsWith(caught.Message, "could not export: ");
        Assert.AreEqual(1, caught.ExitCode);
        AssertNothingWritten(root);
        Assert.AreEqual("x", File.ReadAllText(blocker));
    }

    [TestMethod]
    public void AnExportsFolderThatIsAFileFailsCleanly()
    {
        using var root = new TempRoot();
        WriteCampaign(root.Folder);
        File.WriteAllText(root.At("exports"), "in the way");
        var caught = Assert.ThrowsExactly<DmcException>(() => Export(root));
        StringAssert.StartsWith(caught.Message, "could not export: ");
        AssertNothingWritten(root, exportsMayExist: true);
        Assert.AreEqual("in the way", File.ReadAllText(root.At("exports")));
    }
}
