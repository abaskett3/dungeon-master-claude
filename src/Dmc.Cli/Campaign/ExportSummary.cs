using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Dmc.Campaign;

/// <summary>Whether a campaign runs a published module.</summary>
internal enum ModuleKind
{
    /// <summary>The campaign has no <c>module-progress.md</c>.</summary>
    None,

    /// <summary>It has one, but the file names no module folder.</summary>
    Unnamed,

    /// <summary>It runs the module in a named folder under <c>modules/</c>.</summary>
    Named,
}

/// <summary>The module an export needs, and whether this toolkit has it.</summary>
/// <param name="Kind">Whether there is a module, and whether its folder is known.</param>
/// <param name="Folder">The folder under <c>modules/</c>; null unless <paramref name="Kind"/> is <see cref="ModuleKind.Named"/>.</param>
/// <param name="Installed">True when <c>modules/&lt;folder&gt;/index.md</c> exists under the toolkit root that was asked; false otherwise.</param>
internal readonly record struct ModuleState(ModuleKind Kind, string? Folder, bool Installed);

/// <summary>
/// What <c>dmc campaign show</c> prints, and the checks that <c>dmc campaign import</c> makes before it writes anything
/// (data-model § Validation, 1 to 4). <see cref="Open"/> reads the manifest and the list of entries of an export file and
/// refuses it with one line if it is not a usable export; it never reads the contents of a campaign file, so it cannot
/// check checksums (import does, while it extracts, check 5).
/// </summary>
internal sealed class ExportSummary
{
    /// <summary>The name of the manifest in the archive; always the first entry.</summary>
    public const string ManifestEntry = "manifest.json";

    /// <summary>The name of the general player options in the archive, when it holds them.</summary>
    public const string PlayerOptionsEntry = "player-options.md";

    /// <summary>The folder of the archive that holds the campaign, with its trailing slash.</summary>
    public const string CampaignPrefix = "campaign/";

    /// <summary>The largest manifest read: far more than a campaign of thousands of files needs, and a bound on what a crafted archive can make us read.</summary>
    const int MaxManifestBytes = 16 * 1024 * 1024;

    /// <summary>The most files an export may declare: far more than any campaign, and a bound on what a crafted manifest can make import create.</summary>
    const int MaxFiles = 100_000;

    /// <summary>The most bytes an export may declare in all (1 GiB): a campaign is a few MB, and extraction stops at each declared size, so this bounds what a crafted manifest can make import write.</summary>
    const long MaxTotalBytes = 1L << 30;

    /// <summary>The path of the export file as the user gave it, for the messages.</summary>
    public string Path { get; }

    /// <summary>The manifest, with every field present and of the right type.</summary>
    public Manifest Manifest { get; }

    /// <summary>Creates the summary of an export that has passed <see cref="Open"/>.</summary>
    /// <param name="path">The export file as the user gave it.</param>
    /// <param name="manifest">Its manifest.</param>
    ExportSummary(string path, Manifest manifest)
    {
        Path = path;
        Manifest = manifest;
    }

    /// <summary>
    /// Opens an export file and makes checks 1 to 4 of the data model, in this order: it can be read; it is a ZIP with a
    /// manifest that parses and has every field; its format is not newer than this dmc's; every path is safe and unique
    /// when case is ignored; and the entries of the archive and the lists of the manifest match both ways. Nothing is
    /// written.
    /// </summary>
    /// <param name="path">The export file.</param>
    /// <returns>The summary of an export that is fit to import.</returns>
    /// <exception cref="DmcException">The file is not usable; the message is the one line the contract gives for the case.</exception>
    public static ExportSummary Open(string path)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // The runtime's own text for a missing file repeats the path, which the message has already given.
            string reason = e is FileNotFoundException or DirectoryNotFoundException ? "no such file" : e.Message.TrimEnd('.', '\r', '\n', ' ');
            throw new DmcException($"cannot read {path}: {reason}");
        }

        Manifest manifest;
        List<string> entries;
        using (stream)
        {
            try
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, Encoding.UTF8);
                entries = [.. zip.Entries.Select(e => e.FullName)];
                var entry = zip.Entries.FirstOrDefault(e => e.FullName == ManifestEntry) ?? throw NotAnExport(path);
                manifest = ReadManifest(path, entry);
            }
            catch (InvalidDataException)
            {
                throw NotAnExport(path);
            }
            catch (IOException e)
            {
                throw new DmcException($"cannot read {path}: {e.Message.TrimEnd('.', '\r', '\n', ' ')}");
            }
        }

        CheckPaths(path, manifest);
        CheckEntries(path, manifest, entries);
        return new ExportSummary(path, manifest);
    }

    /// <summary>The module this export needs, and whether <c>modules/&lt;folder&gt;/index.md</c> exists under a toolkit root (research R8).</summary>
    /// <param name="root">The toolkit root to look in.</param>
    public ModuleState Module(string root)
    {
        if (Manifest.Module is null)
            return new ModuleState(ModuleKind.None, null, false);
        if (string.IsNullOrEmpty(Manifest.Module.Folder))
            return new ModuleState(ModuleKind.Unnamed, null, false);
        string folder = Manifest.Module.Folder;
        return new ModuleState(ModuleKind.Named, folder, File.Exists(System.IO.Path.Combine(root, "modules", folder, "index.md")));
    }

    /// <summary>
    /// The lines <c>dmc campaign show</c> prints: who and when, how much, and (for a module campaign) which module and whether
    /// this toolkit has it. Two or three lines, built from the manifest alone. Control characters in the manifest's text
    /// print as <c>?</c>, so a crafted manifest cannot add lines.
    /// </summary>
    /// <param name="root">The toolkit root, to look for the module in.</param>
    public IEnumerable<string> ShowLines(string root)
    {
        TryParseTimestamp(Manifest.ExportedAt, out var exported);
        yield return string.Create(CultureInfo.InvariantCulture,
            $"{Printable(Manifest.Campaign)}: exported {exported:yyyy-MM-dd HH:mm} UTC by dmc {Printable(Manifest.DmcVersion)}, format {Manifest.FormatVersion}");

        long bytes = Manifest.Files.Sum(f => f.Size);
        string options = Manifest.PlayerOptions is null ? "" : ", plus the general player options";
        yield return string.Create(CultureInfo.InvariantCulture, $"{Manifest.Files.Count} files, {Size(bytes)}{options}");

        var module = Module(root);
        if (module.Kind == ModuleKind.Named)
            yield return $"module: {module.Folder} ({(module.Installed ? "in" : "not in")} modules/)";
        else if (module.Kind == ModuleKind.Unnamed)
            yield return "module: unknown (module-progress.md names no module folder)";
    }

    /// <summary>
    /// A size for people: bytes below 1024, then kilobytes (1024 bytes) with one decimal below a megabyte, then megabytes with one
    /// decimal. A size that would round up to 1024.0 KB is shown as a megabyte. Always a point, never a comma.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    internal static string Size(long bytes)
    {
        if (bytes < 1024)
            return string.Create(CultureInfo.InvariantCulture, $"{bytes} bytes");
        double kilobytes = bytes / 1024.0;
        if (Math.Round(kilobytes, 1) < 1024)
            return string.Create(CultureInfo.InvariantCulture, $"{kilobytes:0.0} KB");
        return string.Create(CultureInfo.InvariantCulture, $"{bytes / 1048576.0:0.0} MB");
    }

    /// <summary>The text with each control character (a line break, say) replaced by <c>?</c>.</summary>
    /// <param name="text">Text from the manifest.</param>
    static string Printable(string text) => string.Concat(text.Select(c => char.IsControl(c) ? '?' : c));

    /// <summary>The one-line refusal for a file that is not a dmc campaign export.</summary>
    /// <param name="path">The export file.</param>
    static DmcException NotAnExport(string path) => new($"{path} is not a dmc campaign export");

    /// <summary>Reads and checks <c>manifest.json</c> (checks 1 and 2).</summary>
    /// <param name="path">The export file, for the messages.</param>
    /// <param name="entry">The manifest's entry.</param>
    /// <exception cref="DmcException">The manifest is too big, does not parse, lacks a field, has a field of the wrong type or an impossible value, or is of a newer format.</exception>
    static Manifest ReadManifest(string path, ZipArchiveEntry entry)
    {
        var buffer = new MemoryStream();
        using (var source = entry.Open())
        {
            var chunk = new byte[81920];
            int read;
            while ((read = source.Read(chunk)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaxManifestBytes)
                    throw NotAnExport(path);
            }
        }
        byte[] bytes = buffer.ToArray();

        Manifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize(bytes, ManifestJson.Default.Manifest);
        }
        catch (JsonException)
        {
            // A newer dmc may have changed the fields, so say that before saying the file is not an export.
            if (PeekFormat(bytes) is { } newer && newer > Manifest.CurrentFormatVersion)
                throw NewerFormat(path, newer);
            throw NotAnExport(path);
        }
        if (manifest is null)
            throw NotAnExport(path);
        if (manifest.FormatVersion > Manifest.CurrentFormatVersion)
            throw NewerFormat(path, manifest.FormatVersion);

        bool sane = manifest.FormatVersion >= 1
            && manifest.Files.All(f => f is not null && IsSane(f))
            && manifest.Folders.All(f => f is not null)
            && (manifest.PlayerOptions is null || (IsSane(manifest.PlayerOptions) && manifest.PlayerOptions.Path == PlayerOptionsEntry))
            && IsTimestamp(manifest.ExportedAt);
        return sane ? manifest : throw NotAnExport(path);
    }

    /// <summary>The <c>formatVersion</c> of a manifest that does not deserialise, when it is a whole number at the top level.</summary>
    /// <param name="bytes">The manifest's bytes.</param>
    /// <returns>The number, or null when the JSON is not an object with an integer <c>formatVersion</c>.</returns>
    static int? PeekFormat(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            return document.RootElement is { ValueKind: JsonValueKind.Object } root
                && root.TryGetProperty("formatVersion", out var version)
                && version.ValueKind == JsonValueKind.Number
                && version.TryGetInt32(out int number)
                    ? number
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The one-line refusal for an export made by a newer dmc.</summary>
    /// <param name="path">The export file.</param>
    /// <param name="format">Its <c>formatVersion</c>.</param>
    static DmcException NewerFormat(string path, int format) =>
        new($"{path} was made by a newer dmc (format {format}): update dmc and try again");

    /// <summary>True for a file entry with a non-negative size and a checksum of 64 lower-case hexadecimal digits.</summary>
    /// <param name="file">The entry.</param>
    static bool IsSane(FileEntry file) =>
        file.Path is not null && file.Sha256 is not null && file.Size >= 0
        && file.Sha256.Length == 64 && file.Sha256.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    /// <summary>True for a UTC time written as <c>2026-10-21T19:04:05Z</c>.</summary>
    /// <param name="text">The manifest's <c>exportedAt</c>.</param>
    internal static bool IsTimestamp(string text) => TryParseTimestamp(text, out _);

    /// <summary>Reads the manifest's time, which is always UTC and always written in this one form.</summary>
    /// <param name="text">The manifest's <c>exportedAt</c>.</param>
    /// <param name="time">The time, when <paramref name="text"/> is in the form.</param>
    internal static bool TryParseTimestamp(string text, out DateTime time) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out time);

    /// <summary>The refusal for a path that cannot be imported, naming the entry as it is spelled in the archive.</summary>
    /// <param name="path">The export file.</param>
    /// <param name="entry">The entry name.</param>
    /// <param name="reason">Why, short and lower case.</param>
    static DmcException Unportable(string path, string entry, string reason) =>
        new($"{path} holds an entry that cannot be imported: {entry} ({reason})");

    /// <summary>Check 3: every path of the manifest is safe, none is the same as another when case is ignored, and none is both a file and a folder.</summary>
    /// <param name="path">The export file, for the messages.</param>
    /// <param name="manifest">The manifest.</param>
    /// <exception cref="DmcException">The first path that cannot be imported.</exception>
    static void CheckPaths(string path, Manifest manifest)
    {
        if (manifest.Files.Count > MaxFiles)
            throw Unportable(path, ManifestEntry, $"more than {MaxFiles} files");
        long total = manifest.PlayerOptions?.Size ?? 0;
        foreach (var file in manifest.Files)
        {
            total += file.Size;
            if (total > MaxTotalBytes || total < 0)
                throw Unportable(path, ManifestEntry, "more than 1 GB in all");
        }

        if (manifest.Module?.Folder is { Length: > 0 } module)
        {
            string? why = ArchivePaths.Refusal(module) ?? (module.Contains('/') ? "contains /" : null);
            if (why is not null)
                throw Unportable(path, $"module folder {module}", why);
        }
        foreach (string file in manifest.Files.Select(f => f.Path))
        {
            if (ArchivePaths.Refusal(file) is { } why)
                throw Unportable(path, CampaignPrefix + file, why);
        }
        foreach (string folder in manifest.Folders)
        {
            if (ArchivePaths.Refusal(folder) is { } why)
                throw Unportable(path, CampaignPrefix + folder + "/", why);
        }

        // Campaign files and folders compared together: on a case-insensitive file system they share one namespace.
        string[] all = [.. manifest.Files.Select(f => f.Path), .. manifest.Folders];
        if (ArchivePaths.CaseClash(all) is { } clash)
            throw Unportable(path, CampaignPrefix + clash.Second + (manifest.Folders.Contains(clash.Second) ? "/" : ""), "duplicate when case is ignored");

        // A file whose path is also the start of another path would have to be a folder as well.
        var parents = new HashSet<string>(StringComparer.Ordinal);
        foreach (string any in all)
        {
            for (int slash = any.IndexOf('/'); slash >= 0; slash = any.IndexOf('/', slash + 1))
                parents.Add(any[..slash]);
        }
        foreach (string file in manifest.Files.Select(f => f.Path))
        {
            if (parents.Contains(file))
                throw Unportable(path, CampaignPrefix + file, "is both a file and a folder");
        }
        foreach (string folder in manifest.Folders)
        {
            if (parents.Contains(folder))
                throw Unportable(path, CampaignPrefix + folder + "/", "is both an empty folder and a parent of another path");
        }
    }

    /// <summary>Check 4: the entries of the archive are exactly the ones the manifest lists, once each.</summary>
    /// <param name="path">The export file, for the messages.</param>
    /// <param name="manifest">The manifest.</param>
    /// <param name="entries">Every entry name of the archive, in order.</param>
    /// <exception cref="DmcException">The first entry the manifest does not list, or the first listed one the archive lacks.</exception>
    static void CheckEntries(string path, Manifest manifest, List<string> entries)
    {
        var expected = new List<string> { ManifestEntry };
        if (manifest.PlayerOptions is not null)
            expected.Add(PlayerOptionsEntry);
        expected.AddRange(manifest.Files.Select(f => CampaignPrefix + f.Path));
        expected.AddRange(manifest.Folders.Select(f => CampaignPrefix + f + "/"));
        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in entries)
        {
            if (!expectedSet.Contains(entry))
                throw new DmcException($"{path} is damaged: {entry} is not in the manifest");
            if (!seen.Add(entry))
                throw new DmcException($"{path} is damaged: {entry} appears twice");
        }
        foreach (string entry in expected)
        {
            if (!seen.Contains(entry))
                throw new DmcException($"{path} is damaged: {entry} is missing");
        }
    }
}
