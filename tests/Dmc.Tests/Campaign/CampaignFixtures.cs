using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Dmc.Tests.Campaign;

/// <summary>
/// What the <c>dmc campaign</c> tests start from: a temporary toolkit root, a sample campaign with everything an export must
/// carry unchanged, and a way to write an export archive by hand (with a mutator for the crafted archives that import must
/// refuse). It uses only <c>System.IO</c> and <c>System.IO.Compression</c>, never the code under test, so a bug in the
/// exporter cannot hide behind the same bug in a test.
/// </summary>
internal static class CampaignFixtures
{
    /// <summary>The name of the sample campaign.</summary>
    public const string CampaignName = "the-silver-egg";

    /// <summary>The module the sample campaign's <c>module-progress.md</c> names.</summary>
    public const string ModuleName = "lost-mine-of-phandelver";

    /// <summary>A fixed moment (UTC) for exports, so a test never straddles a minute.</summary>
    public static readonly DateTime Moment = new(2026, 10, 21, 19, 4, 5, DateTimeKind.Utc);

    /// <summary>The text of the general player options the tests write.</summary>
    public const string GeneralOptions = "# Player options\n\n- Dice: I roll.\n";

    /// <summary>A temporary toolkit root with an empty <c>adventures/</c> folder, deleted on dispose.</summary>
    public sealed class TempRoot : IDisposable
    {
        /// <summary>The root folder.</summary>
        public string Folder { get; } = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));

        /// <summary>Makes the root and its <c>adventures/</c> folder.</summary>
        public TempRoot() => Directory.CreateDirectory(Adventures);

        /// <summary>The <c>adventures/</c> folder.</summary>
        public string Adventures => Path.Combine(Folder, "adventures");

        /// <summary>The folder of a campaign, whether or not it exists.</summary>
        /// <param name="name">The campaign's name.</param>
        public string Campaign(string name) => Path.Combine(Adventures, name);

        /// <summary>A path under the root.</summary>
        /// <param name="parts">The parts, joined with the system's separator.</param>
        public string At(params string[] parts) => Path.Combine([Folder, .. parts]);

        /// <summary>Deletes the root and everything in it, read-only files included.</summary>
        public void Dispose() => DeleteTree(Folder);
    }

    /// <summary>Deletes a folder and everything in it, clearing read-only attributes first. Does nothing if it is not there.</summary>
    /// <param name="folder">The folder.</param>
    public static void DeleteTree(string folder)
    {
        if (!Directory.Exists(folder))
            return;
        RemoveLinks(folder);
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(folder, recursive: true);
    }

    /// <summary>Deletes every link and junction under a folder (the link itself, never what it points to), so that deleting the folder cannot follow one out of it.</summary>
    /// <param name="folder">The folder.</param>
    static void RemoveLinks(string folder)
    {
        foreach (var info in new DirectoryInfo(folder).EnumerateFileSystemInfos().ToList())
        {
            if (info.LinkTarget is not null)
            {
                if (info is DirectoryInfo)
                    Directory.Delete(info.FullName);
                else
                    File.Delete(info.FullName);
            }
            else if (info is DirectoryInfo directory)
            {
                RemoveLinks(directory.FullName);
            }
        }
    }

    /// <summary>The bytes of the stand-in for <c>maps/live.png</c>: every byte value, twice.</summary>
    public static byte[] BinaryBytes() => [.. Enumerable.Range(0, 256).Select(b => (byte)b), .. Enumerable.Range(0, 256).Select(b => (byte)(255 - b))];

    /// <summary>Writes a file, making its folder, from exact bytes.</summary>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="relative">The path under it, with <c>/</c> separators.</param>
    /// <param name="bytes">What to write.</param>
    public static void Write(string campaign, string relative, byte[] bytes)
    {
        string path = Path.Combine(campaign, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Writes a UTF-8 text file with no byte-order mark, exactly as given (no line-ending conversion).</summary>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="relative">The path under it, with <c>/</c> separators.</param>
    /// <param name="text">What to write.</param>
    public static void WriteText(string campaign, string relative, string text) =>
        Write(campaign, relative, new UTF8Encoding(false).GetBytes(text));

    /// <summary>
    /// Writes the sample campaign under <c>adventures/&lt;name&gt;</c>: nested folders, a file with CRLF line endings, one
    /// with LF, a non-ASCII name, a name with spaces, a binary file, an empty file, an empty folder and a
    /// <c>module-progress.md</c> in the shape of the template.
    /// </summary>
    /// <param name="root">The toolkit root.</param>
    /// <param name="name">The campaign's name.</param>
    /// <returns>The campaign folder.</returns>
    public static string WriteCampaign(string root, string name = CampaignName)
    {
        string campaign = Path.Combine(root, "adventures", name);
        Directory.CreateDirectory(campaign);
        WriteText(campaign, "game-state.md", "# Game state\n\n- Location: the goblin hall\n- HP: 11/11\n");
        WriteText(campaign, "character-sheets/pip.md", "# Pip\r\n\r\nA halfling rogue.\r\nGold: 12\r\n");
        WriteText(campaign, "story/canon.md", "# Canon\n\n- The egg is silver.\n");
        WriteText(campaign, "story/sessions/session-001.md", "# Session 1\n\nPip met a goblin: “No!”\n");
        WriteText(campaign, "story/Écu.md", "# Écu du Château\n"); // not ASCII
        WriteText(campaign, "notes/two words.md", "spaces in the name\n");
        WriteText(campaign, "module-progress.md",
            $"# Module progress\n\n- **Module**: Lost Mine of Phandelver\n- **Module folder**: `modules/{ModuleName}/`\n");
        Write(campaign, "maps/live.png", BinaryBytes());
        Write(campaign, "maps/.gitkeep", []);
        Directory.CreateDirectory(Path.Combine(campaign, "story", "empty-folder"));
        return campaign;
    }

    /// <summary>Writes the sample campaign without a <c>module-progress.md</c>.</summary>
    /// <param name="root">The toolkit root.</param>
    /// <param name="name">The campaign's name.</param>
    /// <returns>The campaign folder.</returns>
    public static string WritePlainCampaign(string root, string name = CampaignName)
    {
        string campaign = WriteCampaign(root, name);
        File.Delete(Path.Combine(campaign, "module-progress.md"));
        return campaign;
    }

    /// <summary>Writes the general <c>adventures/player-options.md</c>.</summary>
    /// <param name="root">The toolkit root.</param>
    /// <param name="text">What it holds; the usual text by default.</param>
    public static void WriteGeneralOptions(string root, string text = GeneralOptions) =>
        File.WriteAllBytes(Path.Combine(root, "adventures", "player-options.md"), new UTF8Encoding(false).GetBytes(text));

    /// <summary>The files under a folder, by relative path (<c>/</c> separators), with their bytes.</summary>
    /// <param name="folder">The folder.</param>
    public static SortedDictionary<string, byte[]> ReadFiles(string folder)
    {
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            files[Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/')] = File.ReadAllBytes(file);
        return files;
    }

    /// <summary>The folders under a folder that hold nothing at all, by relative path (<c>/</c> separators).</summary>
    /// <param name="folder">The folder.</param>
    public static SortedSet<string> ReadEmptyFolders(string folder) =>
        new(Directory.EnumerateDirectories(folder, "*", SearchOption.AllDirectories)
            .Where(d => !Directory.EnumerateFileSystemEntries(d).Any())
            .Select(d => Path.GetRelativePath(folder, d).Replace(Path.DirectorySeparatorChar, '/')), StringComparer.Ordinal);

    /// <summary>Fails unless two folders hold the same files with the same bytes and the same empty folders.</summary>
    /// <param name="expected">The original folder.</param>
    /// <param name="actual">The copy.</param>
    public static void AssertSameFolder(string expected, string actual)
    {
        var want = ReadFiles(expected);
        var got = ReadFiles(actual);
        CollectionAssert.AreEqual(want.Keys.ToArray(), got.Keys.ToArray(), "the files differ");
        foreach (var (path, bytes) in want)
            CollectionAssert.AreEqual(bytes, got[path], $"{path} differs");
        CollectionAssert.AreEqual(ReadEmptyFolders(expected).ToArray(), ReadEmptyFolders(actual).ToArray(), "the empty folders differ");
    }

    /// <summary>Every file and folder under a root with its size and modification time, to see that nothing changed.</summary>
    /// <param name="folder">The root.</param>
    public static string[] Snapshot(string folder) =>
        [.. Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(e => File.Exists(e)
                ? $"{Path.GetRelativePath(folder, e)} {new FileInfo(e).Length} {File.GetLastWriteTimeUtc(e).Ticks}"
                : $"{Path.GetRelativePath(folder, e)}/")];

    /// <summary>The SHA-256 of some bytes as 64 lower-case hex digits.</summary>
    /// <param name="bytes">The bytes.</param>
    public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>
    /// An export under construction: the manifest as JSON and the entries of the archive. A test changes either before
    /// <c>BuildArchive</c> writes them, to make the archive wrong in one way.
    /// </summary>
    public sealed class ArchiveParts
    {
        /// <summary>The manifest. The helpers keep <c>files</c>, <c>folders</c> and <c>playerOptions</c> in step with <see cref="Entries"/>; a test can overwrite any of it.</summary>
        public JsonObject Manifest { get; set; } = new();

        /// <summary>The entries to write after the manifest, in order: name and bytes. A name ending in <c>/</c> is a directory entry.</summary>
        public List<(string Name, byte[] Bytes)> Entries { get; } = [];

        /// <summary>The bytes to write as <c>manifest.json</c> instead of <see cref="Manifest"/>, or null; null with <see cref="OmitManifest"/> false writes the JSON.</summary>
        public byte[]? ManifestBytes { get; set; }

        /// <summary>True to leave <c>manifest.json</c> out of the archive.</summary>
        public bool OmitManifest { get; set; }

        /// <summary>The manifest's <c>files</c> array.</summary>
        public JsonArray Files => Manifest["files"]!.AsArray();

        /// <summary>The manifest's <c>folders</c> array.</summary>
        public JsonArray Folders => Manifest["folders"]!.AsArray();

        /// <summary>Finds the manifest record of a campaign file.</summary>
        /// <param name="path">The file's path in the manifest.</param>
        public JsonObject FileRecord(string path) => Files.Select(f => f!.AsObject()).First(f => (string)f["path"]! == path);

        /// <summary>Adds a campaign file to the archive and the manifest, with the record the real size and checksum unless told otherwise.</summary>
        /// <param name="path">The path under <c>campaign/</c>.</param>
        /// <param name="bytes">The file's bytes as stored in the archive.</param>
        /// <param name="declaredSize">The size to put in the manifest; the real one when null.</param>
        /// <param name="declaredSha">The checksum to put in the manifest; the real one when null.</param>
        public void AddFile(string path, byte[] bytes, long? declaredSize = null, string? declaredSha = null)
        {
            Entries.Add(("campaign/" + path, bytes));
            Files.Add(new JsonObject { ["path"] = path, ["size"] = declaredSize ?? bytes.Length, ["sha256"] = declaredSha ?? Sha(bytes) });
        }

        /// <summary>Removes a campaign file from the archive only, leaving it in the manifest.</summary>
        /// <param name="path">The path under <c>campaign/</c>.</param>
        public void DropEntry(string path) => Entries.RemoveAll(e => e.Name == "campaign/" + path);

        /// <summary>Removes a campaign file from the manifest only, leaving its entry in the archive.</summary>
        /// <param name="path">The path under <c>campaign/</c>.</param>
        public void DropRecord(string path) => Files.Remove(FileRecord(path));
    }

    /// <summary>The manifest and entries of an export of a campaign folder, as the real exporter should write them.</summary>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="name">The campaign name to record; the folder's name by default.</param>
    /// <param name="generalOptions">The general player options to include, or null for none.</param>
    /// <param name="module">The module folder to record; null for the unnamed case. Ignored unless <paramref name="moduleCampaign"/>.</param>
    /// <param name="moduleCampaign">True to record a module (<c>"module": {...}</c>); false for <c>"module": null</c>.</param>
    public static ArchiveParts PartsOf(string campaign, string? name = null, byte[]? generalOptions = null,
        string? module = ModuleName, bool moduleCampaign = true)
    {
        var parts = new ArchiveParts
        {
            Manifest = new JsonObject
            {
                ["formatVersion"] = 1,
                ["campaign"] = name ?? Path.GetFileName(campaign),
                ["exportedAt"] = "2026-10-21T19:04:05Z",
                ["dmcVersion"] = "0.0.0-dev",
                ["module"] = moduleCampaign ? new JsonObject { ["folder"] = module } : null,
                ["playerOptions"] = null,
                ["files"] = new JsonArray(),
                ["folders"] = new JsonArray(),
            },
        };
        if (generalOptions is not null)
        {
            parts.Manifest["playerOptions"] = new JsonObject
            {
                ["path"] = "player-options.md", ["size"] = generalOptions.Length, ["sha256"] = Sha(generalOptions),
            };
            parts.Entries.Add(("player-options.md", generalOptions));
        }
        foreach (var (path, bytes) in ReadFiles(campaign))
            parts.AddFile(path, bytes);
        foreach (string folder in ReadEmptyFolders(campaign))
        {
            parts.Folders.Add(folder);
            parts.Entries.Add(("campaign/" + folder + "/", []));
        }
        return parts;
    }

    /// <summary>
    /// Writes an archive: <c>manifest.json</c> first, then the entries, with UTF-8 names. The manifest is
    /// <see cref="ArchiveParts.ManifestBytes"/> if set, else the JSON of <see cref="ArchiveParts.Manifest"/>.
    /// </summary>
    /// <param name="zipPath">Where to write it.</param>
    /// <param name="parts">What it holds.</param>
    public static void BuildArchive(string zipPath, ArchiveParts parts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(zipPath))!);
        using var stream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false, Encoding.UTF8);
        if (!parts.OmitManifest)
            Add(zip, "manifest.json", parts.ManifestBytes ?? new UTF8Encoding(false).GetBytes(parts.Manifest.ToJsonString()));
        foreach (var (name, bytes) in parts.Entries)
            Add(zip, name, bytes);
    }

    /// <summary>Writes one entry.</summary>
    /// <param name="zip">The archive.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="bytes">Its bytes.</param>
    static void Add(ZipArchive zip, string name, byte[] bytes)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var target = entry.Open();
        target.Write(bytes);
    }

    /// <summary>Writes the archive of a campaign folder as it should be, with a correct manifest. For the exporter-independent tests of import.</summary>
    /// <param name="zipPath">Where to write it.</param>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="name">The campaign name to record; the folder's name by default.</param>
    /// <param name="generalOptions">The general player options to include, or null for none.</param>
    /// <param name="mutate">Changes the parts before they are written, to make the archive wrong in one way.</param>
    public static void BuildArchive(string zipPath, string campaign, string? name = null, byte[]? generalOptions = null,
        Action<ArchiveParts>? mutate = null)
    {
        var parts = PartsOf(campaign, name, generalOptions);
        mutate?.Invoke(parts);
        BuildArchive(zipPath, parts);
    }
}
