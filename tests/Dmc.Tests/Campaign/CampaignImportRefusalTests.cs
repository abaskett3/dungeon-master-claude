using System.Text;
using System.Text.Json.Nodes;
using Dmc.Campaign;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// Every refusal of <c>dmc campaign import</c> in the contract's failure table, in-process against a temporary toolkit root.
/// After each one the target name does not exist, no <c>.dmc-import-</c> staging folder remains, the general player options
/// are as they were (and are not made when the archive held some), and nothing was written outside the root's
/// <c>adventures/</c>. The bad archives are written by hand by <see cref="CampaignFixtures"/>.
/// </summary>
[TestClass]
public sealed class CampaignImportRefusalTests
{
    /// <summary>What the toolkit root held before the import, to compare with after.</summary>
    sealed class Scene : IDisposable
    {
        /// <summary>The source root, holding the sample campaign.</summary>
        public TempRoot Source { get; } = new();

        /// <summary>The root the import runs in.</summary>
        public TempRoot Target { get; } = new();

        /// <summary>The sample campaign's folder in the source root.</summary>
        public string Campaign { get; }

        /// <summary>The archive being imported.</summary>
        public string Zip { get; }

        /// <summary>Makes the sample campaign.</summary>
        public Scene()
        {
            Campaign = WriteCampaign(Source.Folder);
            Zip = Path.Combine(Target.Folder, "export.zip");
        }

        /// <summary>Writes the correct archive of the sample campaign, then lets <paramref name="mutate"/> spoil it.</summary>
        /// <param name="mutate">Changes the manifest or the entries.</param>
        /// <param name="generalOptions">The general player options to include, or null.</param>
        public Scene Build(Action<ArchiveParts>? mutate = null, byte[]? generalOptions = null)
        {
            BuildArchive(Zip, Campaign, generalOptions: generalOptions, mutate: mutate);
            return this;
        }

        /// <summary>Deletes both roots.</summary>
        public void Dispose()
        {
            Source.Dispose();
            Target.Dispose();
        }
    }

    /// <summary>Imports and asserts the refusal: this exact message, exit code 1 (or 2), and a root left as it was.</summary>
    static void AssertRefused(Scene scene, string message, string? name = null, int exitCode = 1, string? target = null, bool archiveHeldOptions = false)
    {
        string[] before = Snapshot(scene.Target.Folder);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, name), TextWriter.Null));
        Assert.AreEqual(message, caught.Message);
        Assert.AreEqual(exitCode, caught.ExitCode);
        AssertUntouched(scene, before, target ?? name ?? CampaignName, archiveHeldOptions);
    }

    /// <summary>Asserts that a refused import changed nothing under the target root.</summary>
    static void AssertUntouched(Scene scene, string[] before, string target, bool archiveHeldOptions)
    {
        CollectionAssert.AreEqual(before, Snapshot(scene.Target.Folder), "the refused import changed the toolkit root");
        Assert.IsEmpty(Directory.GetDirectories(scene.Target.Adventures, ".dmc-import-*").ToArray(), "a staging folder was left");
        if (archiveHeldOptions)
            Assert.IsFalse(File.Exists(Path.Combine(scene.Target.Adventures, "player-options.md")), "player-options.md was made by a failed import");
    }

    /// <summary>The refusal for an archive that is not an export, with the archive's path.</summary>
    static string NotAnExport(Scene scene) => $"{scene.Zip} is not a dmc campaign export";

    // ---------- the file ----------

    [TestMethod]
    public void AFileThatIsMissing()
    {
        using var scene = new Scene();
        string[] before = Snapshot(scene.Target.Folder);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        StringAssert.StartsWith(caught.Message, $"cannot read {scene.Zip}: ");
        Assert.AreEqual(1, caught.ExitCode);
        AssertUntouched(scene, before, CampaignName, false);
    }

    [TestMethod]
    public void AFileThatIsAFolder()
    {
        using var scene = new Scene();
        Directory.CreateDirectory(scene.Zip);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        StringAssert.StartsWith(caught.Message, $"cannot read {scene.Zip}: ");
    }

    [TestMethod]
    public void AFileThatIsNotAZip()
    {
        using var scene = new Scene();
        File.WriteAllText(scene.Zip, "this is not a zip file at all");
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    public void AnEmptyFile()
    {
        using var scene = new Scene();
        File.WriteAllBytes(scene.Zip, []);
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    public void AZipWithNoManifest()
    {
        using var scene = new Scene().Build(p => p.OmitManifest = true);
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    [DataRow("{ this is not json")]
    [DataRow("")]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("\"text\"")]
    [DataRow("{}")]
    public void AManifestThatDoesNotParse(string text)
    {
        using var scene = new Scene().Build(p => p.ManifestBytes = Encoding.UTF8.GetBytes(text));
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    [DataRow("formatVersion")]
    [DataRow("campaign")]
    [DataRow("exportedAt")]
    [DataRow("dmcVersion")]
    [DataRow("module")]
    [DataRow("playerOptions")]
    [DataRow("files")]
    [DataRow("folders")]
    public void AManifestMissingAField(string field)
    {
        using var scene = new Scene().Build(p => p.Manifest.Remove(field));
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    [DataRow("formatVersion", "\"1\"")]
    [DataRow("formatVersion", "1.5")]
    [DataRow("campaign", "7")]
    [DataRow("campaign", "null")]
    [DataRow("files", "\"x\"")]
    [DataRow("files", "null")]
    [DataRow("files", "[null]")]
    [DataRow("files", "[{\"path\":\"a\",\"size\":\"1\",\"sha256\":\"x\"}]")]
    [DataRow("folders", "[1]")]
    [DataRow("module", "\"lost-mine\"")]
    [DataRow("exportedAt", "\"yesterday\"")]
    [DataRow("formatVersion", "0")]
    [DataRow("playerOptions", "{\"path\":\"elsewhere.md\",\"size\":1,\"sha256\":\"0000000000000000000000000000000000000000000000000000000000000000\"}")]
    public void AManifestFieldOfTheWrongTypeOrValue(string field, string json)
    {
        using var scene = new Scene().Build(p => p.Manifest[field] = JsonNode.Parse(json));
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("0000000000000000000000000000000000000000000000000000000000000000x")]
    [DataRow("ABCDEF0000000000000000000000000000000000000000000000000000000000")]
    public void AChecksumThatIsNotSixtyFourLowerCaseHexDigits(string sha)
    {
        using var scene = new Scene().Build(p => p.FileRecord("game-state.md")["sha256"] = sha);
        AssertRefused(scene, NotAnExport(scene));
    }

    [TestMethod]
    public void ANewerFormat()
    {
        using var scene = new Scene().Build(p => p.Manifest["formatVersion"] = 2);
        AssertRefused(scene, $"{scene.Zip} was made by a newer dmc (format 2): update dmc and try again");
    }

    [TestMethod]
    public void ANewerFormatWhoseFieldsHaveChanged()
    {
        using var scene = new Scene().Build(p => p.ManifestBytes = Encoding.UTF8.GetBytes("{\"formatVersion\": 7, \"campaign\": 12, \"things\": []}"));
        AssertRefused(scene, $"{scene.Zip} was made by a newer dmc (format 7): update dmc and try again");
    }

    // ---------- the name ----------

    [TestMethod]
    public void TheNameIsTaken()
    {
        using var scene = new Scene().Build();
        string taken = WriteCampaign(scene.Target.Folder, CampaignName);
        WriteText(taken, "mine.md", "do not lose me");
        AssertRefused(scene, $"adventures/{CampaignName} already exists: import under another name with --as NAME");
        Assert.AreEqual("do not lose me", File.ReadAllText(Path.Combine(taken, "mine.md")));
    }

    [TestMethod]
    public void TheNameIsTakenByAnEmptyFolder()
    {
        // Moving a folder onto an empty one succeeds in the operating system on Linux and macOS; the import must still refuse.
        using var scene = new Scene().Build();
        Directory.CreateDirectory(scene.Target.Campaign(CampaignName));
        AssertRefused(scene, $"adventures/{CampaignName} already exists: import under another name with --as NAME");
        Assert.IsEmpty(Directory.GetFileSystemEntries(scene.Target.Campaign(CampaignName)).ToArray());
    }

    [TestMethod]
    public void TheNameIsTakenByAFile()
    {
        using var scene = new Scene().Build();
        File.WriteAllText(scene.Target.Campaign(CampaignName), "a file");
        AssertRefused(scene, $"adventures/{CampaignName} already exists: import under another name with --as NAME");
    }

    [TestMethod]
    public void TheNameGivenWithAsIsTaken()
    {
        using var scene = new Scene().Build();
        Directory.CreateDirectory(scene.Target.Campaign("mine"));
        AssertRefused(scene, "adventures/mine already exists: import under another name with --as NAME", name: "mine");
    }

    [TestMethod]
    public void AsThatIsNotAValidName()
    {
        using var scene = new Scene().Build();
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, "Bad_Name"), TextWriter.Null));
        Assert.AreEqual("--as must be lowercase letters, digits and hyphens, starting with a letter or digit", caught.Message);
        Assert.AreEqual(2, caught.ExitCode);
    }

    [TestMethod]
    [DataRow("../x", "contains ..")]
    [DataRow("a/b", "contains /")]
    [DataRow("a\\b", "contains a backslash")]
    [DataRow(".hidden", "starts with a dot")]
    [DataRow("con", "reserved name con")]
    [DataRow("name.", "ends with a dot or a space")]
    [DataRow("", "empty path")]
    public void ANameInTheManifestThatIsNotOneSafeFolderName(string name, string reason)
    {
        using var scene = new Scene().Build(p => p.Manifest["campaign"] = name);
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign name \"{name}\" ({reason})", target: "x");
    }

    // ---------- the paths ----------

    [TestMethod]
    public void AnUnsafePathIsRefusedAndNothingIsWrittenOutsideTheCampaign()
    {
        using var scene = new Scene().Build(p => p.AddFile("../evil.txt", Encoding.UTF8.GetBytes("evil")));
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/../evil.txt (contains ..)");
        Assert.IsEmpty(Directory.GetFiles(scene.Target.Folder, "evil.txt", SearchOption.AllDirectories).ToArray());
        Assert.IsEmpty(Directory.GetFiles(Path.GetTempPath(), "evil.txt", SearchOption.TopDirectoryOnly).ToArray());
    }

    [TestMethod]
    [DataRow("/abs.md", "campaign//abs.md", "absolute path")]
    [DataRow("C:/x.md", "campaign/C:/x.md", "has a drive")]
    [DataRow("a\\b.md", "campaign/a\\b.md", "contains a backslash")]
    [DataRow("story/a?.md", "campaign/story/a?.md", "contains ?")]
    [DataRow("story/trailing.", "campaign/story/trailing.", "ends with a dot or a space")]
    [DataRow("story//x.md", "campaign/story//x.md", "has an empty segment")]
    public void AnUnportablePathIsRefused(string path, string entry, string reason)
    {
        using var scene = new Scene().Build(p => p.AddFile(path, [1, 2, 3]));
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: {entry} ({reason})");
    }

    [TestMethod]
    public void AReservedName()
    {
        using var scene = new Scene().Build(p => p.AddFile("story/con.md", [1]));
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/story/con.md (reserved name con)");
    }

    [TestMethod]
    public void AnEmptyFolderWithAnUnsafeName()
    {
        using var scene = new Scene().Build(p =>
        {
            p.Folders.Add("../outside");
            p.Entries.Add(("campaign/../outside/", []));
        });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/../outside/ (contains ..)");
    }

    [TestMethod]
    public void TwoPathsThatAreEqualWhenCaseIsIgnored()
    {
        using var scene = new Scene().Build(p =>
        {
            p.AddFile("Notes.md", [1]);
            p.AddFile("notes.md", [2]);
        });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/notes.md (duplicate when case is ignored)");
    }

    [TestMethod]
    public void AnEmptyFolderAndAFileThatDifferOnlyByCase()
    {
        using var scene = new Scene().Build(p =>
        {
            p.Folders.Add("STORY/CANON.MD");
            p.Entries.Add(("campaign/STORY/CANON.MD/", []));
        });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/STORY/CANON.MD/ (duplicate when case is ignored)");
    }

    [TestMethod]
    public void APathThatIsBothAFileAndTheStartOfAnotherPath()
    {
        using var scene = new Scene().Build(p => p.AddFile("game-state.md/inside.md", [1]));
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: campaign/game-state.md (is both a file and a folder)");
    }

    [TestMethod]
    public void AModuleFolderThatIsNotOneSafeName()
    {
        using var scene = new Scene().Build(p => p.Manifest["module"] = new JsonObject { ["folder"] = "../../etc" });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: module folder ../../etc (contains ..)");
    }

    [TestMethod]
    public void AManifestThatDeclaresMoreThanAGigabyteInAll()
    {
        using var scene = new Scene().Build(p => p.FileRecord("game-state.md")["size"] = 2L << 30);
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: manifest.json (more than 1 GB in all)");
    }

    [TestMethod]
    public void AManifestThatDeclaresSizesThatOverflowWhenAdded()
    {
        using var scene = new Scene().Build(p =>
        {
            p.FileRecord("game-state.md")["size"] = long.MaxValue;
            p.FileRecord("story/canon.md")["size"] = long.MaxValue;
        });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: manifest.json (more than 1 GB in all)");
    }

    [TestMethod]
    public void AManifestThatDeclaresTooManyFiles()
    {
        using var scene = new Scene().Build(p =>
        {
            for (int i = 0; i < 100_000; i++)
                p.Files.Add(new JsonObject { ["path"] = $"f/{i}.md", ["size"] = 1, ["sha256"] = Sha([1]) });
        });
        AssertRefused(scene, $"{scene.Zip} holds an entry that cannot be imported: manifest.json (more than 100000 files)");
    }

    // ---------- the archive and the manifest disagree ----------

    [TestMethod]
    public void AnEntryThatIsNotInTheManifest()
    {
        using var scene = new Scene().Build(p => p.Entries.Add(("campaign/extra.md", [1])));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/extra.md is not in the manifest");
    }

    [TestMethod]
    public void AnEntryOutsideTheCampaignFolderThatIsNotInTheManifest()
    {
        using var scene = new Scene().Build(p => p.Entries.Add(("../evil.txt", [1])));
        AssertRefused(scene, $"{scene.Zip} is damaged: ../evil.txt is not in the manifest");
    }

    [TestMethod]
    public void APlayerOptionsEntryThatTheManifestDoesNotListIsNotInTheManifest()
    {
        using var scene = new Scene().Build(p => p.Entries.Add(("player-options.md", [1])));
        AssertRefused(scene, $"{scene.Zip} is damaged: player-options.md is not in the manifest");
    }

    [TestMethod]
    public void AManifestFileThatIsNotInTheArchive()
    {
        using var scene = new Scene().Build(p => p.DropEntry("game-state.md"));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md is missing");
    }

    [TestMethod]
    public void AFolderThatIsNotInTheArchive()
    {
        using var scene = new Scene().Build(p => p.Entries.RemoveAll(e => e.Name == "campaign/story/empty-folder/"));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/story/empty-folder/ is missing");
    }

    [TestMethod]
    public void AGeneralPlayerOptionsFileThatIsNotInTheArchive()
    {
        using var scene = new Scene().Build(p =>
        {
            p.Manifest["playerOptions"] = new JsonObject { ["path"] = "player-options.md", ["size"] = 1, ["sha256"] = Sha([1]) };
        });
        AssertRefused(scene, $"{scene.Zip} is damaged: player-options.md is missing");
    }

    [TestMethod]
    public void AnEntryThatAppearsTwice()
    {
        using var scene = new Scene().Build(p => p.Entries.Add(("campaign/game-state.md", [1])));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md appears twice");
    }

    [TestMethod]
    public void AWrongChecksum()
    {
        using var scene = new Scene().Build(p => p.FileRecord("game-state.md")["sha256"] = Sha([9, 9, 9]));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md does not match its checksum in the manifest");
    }

    [TestMethod]
    public void AChangedByteWithTheSameSize()
    {
        using var scene = new Scene();
        byte[] original = File.ReadAllBytes(Path.Combine(scene.Campaign, "game-state.md"));
        byte[] changed = [.. original];
        changed[^3] ^= 0x01; // one bit of one byte; the size is the same
        scene.Build(p =>
        {
            p.DropEntry("game-state.md");
            p.Entries.Add(("campaign/game-state.md", changed));
            p.FileRecord("game-state.md")["size"] = original.Length;
            p.FileRecord("game-state.md")["sha256"] = Sha(original);
        });
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md does not match its checksum in the manifest");
    }

    [TestMethod]
    public void ASizeSmallerThanTheEntryReallyIs()
    {
        using var scene = new Scene().Build(p => p.FileRecord("game-state.md")["size"] = 3);
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md is larger than the size in the manifest");
    }

    [TestMethod]
    public void ASizeLargerThanTheEntryReallyIs()
    {
        using var scene = new Scene().Build(p => p.FileRecord("game-state.md")["size"] = 100000);
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/game-state.md is smaller than the size in the manifest");
    }

    [TestMethod]
    public void AnEntryFarLargerThanItsDeclaredSizeIsStoppedAtTheDeclaredSize()
    {
        // 16 MB of zeros compresses to about 16 KB; extraction must stop after the declared 10 bytes, not write it all.
        byte[] huge = new byte[16 * 1024 * 1024];
        using var scene = new Scene().Build(p => p.AddFile("story/bomb.bin", huge, declaredSize: 10));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/story/bomb.bin is larger than the size in the manifest");
    }

    [TestMethod]
    public void AWrongChecksumOnTheGeneralPlayerOptionsLeavesNoPlayerOptionsFile()
    {
        using var scene = new Scene().Build(
            p => p.Manifest["playerOptions"]!["sha256"] = Sha([7]), Encoding.UTF8.GetBytes(GeneralOptions));
        AssertRefused(scene, $"{scene.Zip} is damaged: player-options.md does not match its checksum in the manifest", archiveHeldOptions: true);
    }

    [TestMethod]
    public void ADamageLateInTheListLeavesNothingOfTheEarlierFiles()
    {
        using var scene = new Scene().Build(p => p.FileRecord("story/sessions/session-001.md")["sha256"] = Sha([1]));
        AssertRefused(scene, $"{scene.Zip} is damaged: campaign/story/sessions/session-001.md does not match its checksum in the manifest");
        Assert.IsFalse(Directory.Exists(scene.Target.Campaign(CampaignName)));
    }

    // ---------- a write fails ----------

    /// <summary>A file or folder name that is one safe path segment but longer than any file system stores (255).</summary>
    static readonly string TooLong = new('x', 300);

    [TestMethod]
    public void AWriteThatFailsInTheMiddleOfExtraction()
    {
        // The name passes every check of the manifest, and then no file system can make it: a real failure, with no test hook.
        using var scene = new Scene().Build(p => p.AddFile($"story/{TooLong}.md", [1, 2, 3]), Encoding.UTF8.GetBytes(GeneralOptions));
        string[] before = Snapshot(scene.Target.Folder);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        StringAssert.StartsWith(caught.Message, "could not import: ");
        Assert.AreEqual(1, caught.ExitCode);
        AssertUntouched(scene, before, CampaignName, archiveHeldOptions: true);
        Assert.IsFalse(Directory.Exists(scene.Target.Campaign(CampaignName)));
    }

    [TestMethod]
    public void ACommitThatFailsAfterThePlayerOptionsWereMovedPutsThemBack()
    {
        // The folder cannot be made under this name, but only the move finds out: the options file has been moved by then.
        using var scene = new Scene().Build(p => p.Manifest["campaign"] = TooLong, Encoding.UTF8.GetBytes(GeneralOptions));
        string[] before = Snapshot(scene.Target.Folder);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        StringAssert.StartsWith(caught.Message, "could not import: ");
        AssertUntouched(scene, before, TooLong, archiveHeldOptions: true);
    }

    [TestMethod]
    public void ACommitThatFailsLeavesAnExistingPlayerOptionsFileAlone()
    {
        using var scene = new Scene().Build(p => p.Manifest["campaign"] = TooLong, Encoding.UTF8.GetBytes(GeneralOptions));
        File.WriteAllText(Path.Combine(scene.Target.Adventures, "player-options.md"), "mine");
        string[] before = Snapshot(scene.Target.Folder);
        Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        AssertUntouched(scene, before, TooLong, archiveHeldOptions: false);
        Assert.AreEqual("mine", File.ReadAllText(Path.Combine(scene.Target.Adventures, "player-options.md")));
    }

    [TestMethod]
    public void NothingIsWrittenWhenAdventuresDoesNotExistAndTheArchiveIsBad()
    {
        using var scene = new Scene().Build(p => p.OmitManifest = true);
        Directory.Delete(scene.Target.Adventures);
        var caught = Assert.ThrowsExactly<DmcException>(() =>
            CampaignImporter.Import(scene.Target.Folder, new ImportRequest(scene.Zip, null), TextWriter.Null));
        Assert.AreEqual(NotAnExport(scene), caught.Message);
        Assert.IsFalse(Directory.Exists(scene.Target.Adventures), "a refused archive makes no folder");
    }
}
