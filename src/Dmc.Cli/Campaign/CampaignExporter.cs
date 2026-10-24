using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dmc.Campaign;

/// <summary>What an export did.</summary>
/// <param name="Archive">The full path of the file written.</param>
/// <param name="Files">How many campaign files it holds (not counting the general player options).</param>
/// <param name="GeneralOptions">True when the general <c>adventures/player-options.md</c> is in it.</param>
internal sealed record ExportResult(string Archive, int Files, bool GeneralOptions);

/// <summary>
/// <c>dmc campaign export</c>: writes one campaign folder, and the general <c>adventures/player-options.md</c> if there is one,
/// to a ZIP with a <c>manifest.json</c> of every file's size and SHA-256 (research R2, R3). Every file is read once, into memory,
/// and the same bytes give the checksum and go into the archive, so a file that changes during the export cannot make the
/// two disagree. The archive is written under a <c>.partial-</c> name and renamed when complete, so a file under the final
/// name is always whole (R7). Nothing under <c>adventures/</c> is ever changed. The library takes the DMC folder as a
/// parameter, so a test can aim it at a temporary folder.
/// </summary>
internal static class CampaignExporter
{
    /// <summary>The marker of the line in <c>module-progress.md</c> that names the module (<c>templates/module-progress.md</c>).</summary>
    const string ModuleFolderMarker = "**Module folder**:";

    /// <summary>The folder name <c>module-progress.md</c> puts before the module's name.</summary>
    const string ModulesPrefix = "modules/";

    /// <summary>A file read into memory, with its path relative to the campaign folder (<c>/</c> separators).</summary>
    /// <param name="Path">The relative path.</param>
    /// <param name="Bytes">The file's bytes.</param>
    sealed record Item(string Path, byte[] Bytes);

    /// <summary>Where the archive goes: the folder, and either the exact file or the stem of the default name.</summary>
    /// <param name="Folder">The folder the file is written in.</param>
    /// <param name="File">The file named by <c>--out</c>, or null to use the default name in <paramref name="Folder"/>.</param>
    /// <param name="Stem">The default name without <c>.zip</c> or <c>-N</c>.</param>
    sealed record Destination(string Folder, string? File, string Stem);

    /// <summary>Exports a campaign.</summary>
    /// <param name="root">The DMC folder: <c>adventures/</c> is read here, and <c>exports/</c> is written here by default.</param>
    /// <param name="request">What was asked.</param>
    /// <param name="now">The moment of the export; the clock when null. It is read once: the file name and the manifest's <c>exportedAt</c> come from this one value.</param>
    /// <returns>The file written and what it holds.</returns>
    /// <exception cref="DmcException">The campaign or the destination cannot be used, the campaign holds a link or a path that could not be imported, or the file could not be written. Nothing is left under the final name.</exception>
    public static ExportResult Export(string root, ExportRequest request, DateTime? now = null)
    {
        DateTime moment = (now ?? DateTime.UtcNow).ToUniversalTime();
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, FindCampaign(adventures, request.Name));
        // The campaign folder itself may be a link or junction; Walk checks only what is inside it.
        if (new DirectoryInfo(campaign).LinkTarget is not null)
            throw new DmcException($"{request.Name} is a link: exports hold only the campaign's own files");

        try
        {
            // Inside the try: a --out the system cannot make sense of (a ":" in a name on Windows, a NUL) throws ArgumentException or NotSupportedException.
            var destination = Plan(root, adventures, request, moment);
            var files = new List<Item>();
            var folders = new List<string>();
            Walk(campaign, "", files, folders);
            files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            folders.Sort(StringComparer.Ordinal);
            if (ArchivePaths.CaseClash(files.Select(f => f.Path).Concat(folders)) is { } clash)
                throw new DmcException($"{clash.Second} cannot be exported: duplicate when case is ignored");

            var module = ReadModule(files);
            byte[]? general = ReadGeneralOptions(adventures);
            var manifest = new Manifest
            {
                FormatVersion = Manifest.CurrentFormatVersion,
                Campaign = request.Name,
                ExportedAt = moment.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                DmcVersion = CommandLine.Version,
                Module = module,
                PlayerOptions = general is null ? null : Describe(ExportSummary.PlayerOptionsEntry, general),
                Files = [.. files.Select(f => Describe(f.Path, f.Bytes))],
                Folders = folders,
            };
            string archive = Write(destination, request, manifest, files, folders, general, moment);
            return new ExportResult(archive, files.Count, general is not null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new DmcException($"could not export: {e.Message.TrimEnd('.', '\r', '\n', ' ')}");
        }
    }

    /// <summary>The name of the campaign folder: the one folder of <c>adventures/</c> with exactly this name that does not start with a dot (those are the toolkit's own, such as an import's staging folder).</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="name">What the user typed.</param>
    /// <exception cref="DmcException">There is no such campaign; the message lists the ones there are.</exception>
    static string FindCampaign(string adventures, string name)
    {
        string[] campaigns = !Directory.Exists(adventures)
            ? []
            : [.. new DirectoryInfo(adventures).EnumerateDirectories().Select(d => d.Name).Where(n => !n.StartsWith('.')).Order(StringComparer.Ordinal)];
        if (campaigns.Contains(name, StringComparer.Ordinal))
            return name;
        string there = campaigns.Length == 0 ? "there are none" : "there are: " + string.Join(", ", campaigns);
        throw new DmcException($"no campaign \"{name}\" in adventures/ ({there})");
    }

    /// <summary>Decides where the archive goes, and refuses a destination that cannot be used. Makes nothing.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="request">What was asked.</param>
    /// <param name="moment">The moment of the export (UTC), for the default name.</param>
    /// <exception cref="DmcException">The destination is inside <c>adventures/</c>, is neither a folder nor a <c>.zip</c> name, or is an existing file and <c>--force</c> was not given.</exception>
    static Destination Plan(string root, string adventures, ExportRequest request, DateTime moment)
    {
        string stem = request.Name + "-" + moment.ToLocalTime().ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        if (request.Out is null)
            return new Destination(Path.Combine(root, "exports"), null, stem);

        if (request.Out.Length == 0)
            throw new DmcException("--out is empty: give a folder or a file name ending in .zip");
        string output = Path.GetFullPath(request.Out);
        if (IsInside(output, adventures))
            throw new DmcException("will not write an export inside adventures/: choose another --out");
        if (Directory.Exists(output))
            return new Destination(output, null, stem);
        if (!output.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new DmcException($"{output} is not an existing folder or a file name ending in .zip: choose another --out");
        if (File.Exists(output) && !request.Force)
            throw new DmcException($"{output} exists: add --force to replace it, or choose another --out");
        return new Destination(Path.GetDirectoryName(output)!, output, stem);
    }

    /// <summary>True when a path is the folder or anything under it, compared the way this system compares paths.</summary>
    /// <param name="path">A full path.</param>
    /// <param name="folder">The folder, which need not be a full path.</param>
    static bool IsInside(string path, string folder)
    {
        var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        string target = Path.TrimEndingDirectorySeparator(path);
        return target.Equals(root, comparison)
            || target.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>
    /// Reads every file under a folder into memory, in name order, refusing the first link or junction and the first path
    /// that an import would refuse, and records every folder that holds nothing.
    /// </summary>
    /// <param name="folder">The folder to read.</param>
    /// <param name="relative">Its path relative to the campaign (empty for the campaign itself).</param>
    /// <param name="files">Receives the files.</param>
    /// <param name="empty">Receives the relative paths of the empty folders.</param>
    /// <exception cref="DmcException">A link, or a path that could not be imported.</exception>
    static void Walk(string folder, string relative, List<Item> files, List<string> empty)
    {
        var entries = new DirectoryInfo(folder).EnumerateFileSystemInfos().OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
        if (entries.Count == 0 && relative.Length > 0)
            empty.Add(relative);
        foreach (var entry in entries)
        {
            string path = relative.Length == 0 ? entry.Name : relative + "/" + entry.Name;
            // A link would export something from outside the campaign. LinkTarget is set for symbolic links and junctions;
            // a Windows file that is only a cloud placeholder has the reparse-point attribute but no target, and is a plain file.
            if (entry.LinkTarget is not null)
                throw new DmcException($"{path} is a link: exports hold only the campaign's own files");
            if (ArchivePaths.Refusal(path) is { } why)
                throw new DmcException($"{path} cannot be exported: {why}");
            if (entry is DirectoryInfo directory)
                Walk(directory.FullName, path, files, empty);
            else
                files.Add(new Item(path, File.ReadAllBytes(entry.FullName)));
        }
    }

    /// <summary>The manifest's record of a file: its path, size and checksum, from the bytes that go into the archive.</summary>
    /// <param name="path">The path as spelled in the archive.</param>
    /// <param name="bytes">The file's bytes.</param>
    static FileEntry Describe(string path, byte[] bytes) =>
        new() { Path = path, Size = bytes.Length, Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) };

    /// <summary>The general <c>adventures/player-options.md</c>, or null when there is none.</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    static byte[]? ReadGeneralOptions(string adventures)
    {
        var file = new FileInfo(Path.Combine(adventures, ExportSummary.PlayerOptionsEntry));
        if (!file.Exists)
            return null;
        if (file.LinkTarget is not null)
            throw new DmcException($"adventures/{ExportSummary.PlayerOptionsEntry} is a link: exports hold only the campaign's own files");
        return File.ReadAllBytes(file.FullName);
    }

    /// <summary>The module the campaign runs, from its <c>module-progress.md</c> (research R8): null without the file, a module with no folder when the file names none.</summary>
    /// <param name="files">The campaign's files.</param>
    static ModuleInfo? ReadModule(List<Item> files)
    {
        var progress = files.FirstOrDefault(f => f.Path == "module-progress.md");
        return progress is null ? null : new ModuleInfo { Folder = ModuleFolder(Encoding.UTF8.GetString(progress.Bytes)) };
    }

    /// <summary>
    /// The module folder named by the <c>**Module folder**:</c> line, as <c>templates/module-progress.md</c> writes it
    /// (<c>`modules/&lt;module&gt;/`</c>); null when there is no such line, it names nothing, or it still holds the template's
    /// placeholder or anything that is not one plain folder name.
    /// </summary>
    /// <param name="text">The text of <c>module-progress.md</c>.</param>
    internal static string? ModuleFolder(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            int at = line.IndexOf(ModuleFolderMarker, StringComparison.Ordinal);
            if (at < 0)
                continue;
            string value = line[(at + ModuleFolderMarker.Length)..].Replace("`", "").Trim();
            if (value.StartsWith(ModulesPrefix, StringComparison.Ordinal))
                value = value[ModulesPrefix.Length..];
            value = value.TrimEnd('/', '\\').Trim();
            return value.Length == 0 || value.Contains('/') || ArchivePaths.Refusal(value) is not null ? null : value;
        }
        return null;
    }

    /// <summary>Writes the archive to a <c>.partial-</c> file and renames it into place.</summary>
    /// <param name="destination">Where it goes.</param>
    /// <param name="request">What was asked, for <c>--force</c>.</param>
    /// <param name="manifest">The manifest to write first.</param>
    /// <param name="files">The campaign's files, in manifest order.</param>
    /// <param name="folders">The campaign's empty folders, in manifest order.</param>
    /// <param name="general">The general player options, or null.</param>
    /// <param name="moment">The moment of the export, stamped on every entry.</param>
    /// <returns>The full path of the finished file.</returns>
    /// <exception cref="IOException">The folder or file cannot be made or written. The partial file has been deleted.</exception>
    static string Write(Destination destination, ExportRequest request, Manifest manifest, List<Item> files,
        List<string> folders, byte[]? general, DateTime moment)
    {
        Directory.CreateDirectory(destination.Folder);
        string target = destination.File ?? FreeName(destination.Folder, destination.Stem);
        string partial = target + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        var stamp = new DateTimeOffset(moment, TimeSpan.Zero);
        try
        {
            using (var stream = new FileStream(partial, FileMode.CreateNew, FileAccess.Write))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false, Encoding.UTF8))
            {
                Add(zip, ExportSummary.ManifestEntry, JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJson.Default.Manifest), stamp);
                if (general is not null)
                    Add(zip, ExportSummary.PlayerOptionsEntry, general, stamp);
                foreach (var file in files)
                    Add(zip, ExportSummary.CampaignPrefix + file.Path, file.Bytes, stamp);
                foreach (string folder in folders)
                    zip.CreateEntry(ExportSummary.CampaignPrefix + folder + "/").LastWriteTime = stamp;
            }
            File.Move(partial, target, overwrite: destination.File is not null && request.Force);
        }
        catch
        {
            try
            {
                File.Delete(partial);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Nothing more to do: the exception being handled is the one worth reporting.
            }
            throw;
        }
        return target;
    }

    /// <summary>Writes one file entry.</summary>
    /// <param name="zip">The archive.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="bytes">The raw bytes, never passed through a text writer.</param>
    /// <param name="stamp">The entry's modification time.</param>
    static void Add(ZipArchive zip, string name, byte[] bytes, DateTimeOffset stamp)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = stamp;
        using var target = entry.Open();
        target.Write(bytes);
    }

    /// <summary>The first of <c>stem.zip</c>, <c>stem-2.zip</c>, <c>stem-3.zip</c> ... that does not exist in a folder (research R7).</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="stem">The default name without <c>.zip</c>.</param>
    static string FreeName(string folder, string stem)
    {
        string candidate = Path.Combine(folder, stem + ".zip");
        for (int n = 2; File.Exists(candidate) || Directory.Exists(candidate); n++)
            candidate = Path.Combine(folder, $"{stem}-{n}.zip");
        return candidate;
    }
}
