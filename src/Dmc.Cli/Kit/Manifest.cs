using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dmc.Kit;

/// <summary>One file of a manifest: its path in the DMC folder (with <c>/</c>) and the SHA-256 of what <c>dmc</c> wrote there, lowercase hex.</summary>
/// <param name="Path">The path relative to the DMC folder, with <c>/</c> as the separator.</param>
/// <param name="Sha256">The SHA-256 of the file's content as <c>dmc</c> wrote it, as lowercase hex.</param>
public sealed record ManifestEntry(string Path, string Sha256);

/// <summary>
/// <c>.dmc/manifest.json</c> of a DMC folder: the version of <c>dmc</c> that last wrote it and, for every generated file,
/// its path and hash. It is the only list of files <c>dmc</c> may rewrite or remove, and its presence is what makes a folder
/// a DMC folder. It is written last by every refresh, so an interrupted refresh leaves the old one and the next repairs the
/// folder. Read and written with <see cref="JsonDocument"/> and <see cref="Utf8JsonWriter"/>, so no reflection is needed.
/// </summary>
/// <param name="Version">The <c>dmc</c> version that wrote the files (<c>"dmc"</c> in the JSON).</param>
/// <param name="Files">The generated files, each with the hash of what was written.</param>
public sealed record Manifest(string Version, IReadOnlyList<ManifestEntry> Files)
{
    /// <summary>The manifest's path in a DMC folder: <c>.dmc/manifest.json</c>.</summary>
    /// <param name="folder">The DMC folder.</param>
    public static string FileOf(string folder) => Path.Combine(folder, ".dmc", "manifest.json");

    /// <summary>The lowercase hex SHA-256 of some bytes.</summary>
    /// <param name="bytes">The bytes to hash.</param>
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>The recorded hash of a path, or null when the manifest does not list it.</summary>
    /// <param name="path">A path relative to the DMC folder, with <c>/</c>.</param>
    public string? HashOf(string path) => Files.FirstOrDefault(f => f.Path == path)?.Sha256;

    /// <summary>The manifest as JSON text: <c>{ "dmc": ..., "files": [ { "path": ..., "sha256": ... } ] }</c>, indented, LF, ending in a newline.</summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            writer.WriteStartObject();
            writer.WriteString("dmc", Version);
            writer.WriteStartArray("files");
            foreach (var file in Files)
            {
                writer.WriteStartObject();
                writer.WriteString("path", file.Path);
                writer.WriteString("sha256", file.Sha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    /// <summary>Reads a manifest from its JSON text.</summary>
    /// <param name="json">The text of a <c>manifest.json</c>.</param>
    /// <exception cref="FormatException">The text is not JSON, or is not a manifest: no <c>dmc</c> string, no <c>files</c> list, or an entry without a string <c>path</c> and <c>sha256</c>.</exception>
    public static Manifest Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("dmc", out var version) || version.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
                throw new FormatException("it has no \"dmc\" version and \"files\" list");
            var entries = new List<ManifestEntry>();
            foreach (var file in files.EnumerateArray())
            {
                if (file.ValueKind != JsonValueKind.Object
                    || !file.TryGetProperty("path", out var path) || path.ValueKind != JsonValueKind.String
                    || !file.TryGetProperty("sha256", out var hash) || hash.ValueKind != JsonValueKind.String)
                    throw new FormatException("a file entry has no \"path\" and \"sha256\"");
                entries.Add(new ManifestEntry(path.GetString()!, hash.GetString()!));
            }
            return new Manifest(version.GetString()!, entries);
        }
        catch (JsonException e)
        {
            throw new FormatException(e.Message, e);
        }
    }

    /// <summary>Reads the manifest of a DMC folder; null when the folder has none (it is not a DMC folder yet).</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <exception cref="DmcException">The file exists but cannot be read or is not a manifest (<c>cannot read &lt;file&gt;: ...</c>).</exception>
    public static Manifest? Read(string folder)
    {
        string file = FileOf(folder);
        if (!File.Exists(file))
            return null;
        try
        {
            return Parse(File.ReadAllText(file, Encoding.UTF8));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
        {
            throw new DmcException($"cannot read {file}: {e.Message}");
        }
    }

    /// <summary>
    /// Writes the manifest of a DMC folder: to a temporary file beside it, then moved over the old one, so that a reader
    /// sees the old manifest or the new, never half of one.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="manifest">What to write.</param>
    /// <exception cref="DmcException">The file cannot be written (<c>cannot write &lt;file&gt;: ...</c>).</exception>
    public static void Write(string folder, Manifest manifest)
    {
        string file = FileOf(folder), temporary = file + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(temporary, manifest.ToJson(), new UTF8Encoding(false));
            File.Move(temporary, file, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {file}: {e.Message}");
        }
    }
}
