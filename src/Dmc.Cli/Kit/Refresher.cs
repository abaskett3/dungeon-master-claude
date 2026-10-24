using System.Globalization;

namespace Dmc.Kit;

/// <summary>What a refresh did to a DMC folder.</summary>
/// <param name="Updated">How many generated files it wrote, replaced or removed. Zero when the folder was already current.</param>
/// <param name="Kept">How many of them had been edited by hand, so that the edited copy was put in <c>.dmc/replaced/&lt;stamp&gt;/</c> first.</param>
/// <param name="Stamp">The <c>yyyyMMdd-HHmmss</c> folder name under <c>.dmc/replaced/</c> that holds the kept copies; null when <paramref name="Kept"/> is 0.</param>
public sealed record RefreshResult(int Updated, int Kept, string? Stamp);

/// <summary>
/// Writes the generated files into a DMC folder and keeps the folder's manifest, by the states of
/// data-model § "A generated file's states". For each file of the program's table: missing, then written; the same as the
/// program's copy, then left; the same as the manifest's hash (an older generated version), then replaced; different from
/// both (edited by hand), then copied to <c>.dmc/replaced/&lt;yyyyMMdd-HHmmss&gt;/&lt;path&gt;</c> and replaced. A file in the
/// old manifest that the program no longer has is removed when unchanged and moved to <c>.dmc/replaced/</c> when edited.
/// With no manifest (a first install), a file that exists and is not the program's copy is not <c>dmc</c>'s: nothing is written
/// and the first such file is named. Nothing under a player folder is ever touched (<see cref="GeneratedFiles.Guard"/>), and
/// the manifest is written last, so an interrupted refresh leaves the old one and the next repairs the folder.
/// </summary>
public static class Refresher
{
    /// <summary>
    /// Brings the generated files of a DMC folder in line with the program's, as the class summary describes.
    /// </summary>
    /// <param name="folder">The DMC folder; created if it does not exist.</param>
    /// <param name="files">The program's generated files (<see cref="GeneratedFiles.All"/> in the real program; a test passes its own).</param>
    /// <param name="version">The version to record in the manifest (<see cref="GeneratedFiles.Version"/>).</param>
    /// <param name="now">The moment that names the <c>.dmc/replaced/</c> folder (local time).</param>
    /// <returns>What it did.</returns>
    /// <exception cref="DmcException">
    /// A file is in the way of a first install (<c>&lt;file&gt; already exists and was not written by dmc: move it, or choose another
    /// folder</c>); a path cannot be written (<c>cannot write &lt;path&gt;: &lt;reason&gt;</c>, with the old manifest left in place);
    /// or the old manifest cannot be read.
    /// </exception>
    /// <exception cref="InvalidOperationException">A target or an old manifest's path is under a player folder or outside the folder (a program error: FR-012).</exception>
    public static RefreshResult Refresh(string folder, IReadOnlyList<GeneratedFile> files, string version, DateTime now)
    {
        foreach (var file in files)
            GeneratedFiles.Guard(file.Path);
        var old = Manifest.Read(folder);
        if (old is not null)
        {
            foreach (var entry in old.Files)
                GeneratedFiles.Guard(entry.Path);
        }

        if (old is null)
            RefuseForeignFiles(folder, files);

        string stamp = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        int updated = 0, kept = 0;
        string? path = null;
        try
        {
            Directory.CreateDirectory(folder);
            foreach (var file in files)
            {
                path = Absolute(folder, file.Path);
                byte[]? current = ReadOrNull(path);
                if (current is null)
                {
                    Write(path, file.Bytes);
                    updated++;
                    continue;
                }
                string hash = Manifest.Hash(current);
                if (hash == file.Sha256)
                    continue;
                if (old?.HashOf(file.Path) != hash)
                {
                    Keep(folder, stamp, file.Path, current);
                    kept++;
                }
                Write(path, file.Bytes);
                updated++;
            }

            var wanted = files.Select(f => f.Path).ToHashSet(StringComparer.Ordinal);
            foreach (var entry in old?.Files ?? [])
            {
                if (wanted.Contains(entry.Path))
                    continue;
                path = Absolute(folder, entry.Path);
                byte[]? current = ReadOrNull(path);
                if (current is null)
                    continue;
                if (Manifest.Hash(current) != entry.Sha256)
                {
                    Keep(folder, stamp, entry.Path, current);
                    kept++;
                }
                File.Delete(path);
                RemoveEmptyFolders(folder, Path.GetDirectoryName(path)!);
                updated++;
            }

            path = Manifest.FileOf(folder);
            var manifest = new Manifest(version, files.Select(f => new ManifestEntry(f.Path, f.Sha256)).OrderBy(e => e.Path, StringComparer.Ordinal).ToList());
            if (old is null || old.ToJson() != manifest.ToJson())
                Manifest.Write(folder, manifest);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {path}: {e.Message}");
        }
        return new RefreshResult(updated, kept, kept > 0 ? stamp : null);
    }

    /// <summary>
    /// The refusal of a first install, made before anything is written (not even the program): with no manifest in the folder,
    /// a generated file that exists and is not the program's copy is not <c>dmc</c>'s. With a manifest it does nothing.
    /// </summary>
    /// <param name="folder">The DMC folder (it need not exist).</param>
    /// <param name="files">The program's generated files.</param>
    /// <exception cref="DmcException">A file is in the way; the message names the first.</exception>
    public static void Preflight(string folder, IReadOnlyList<GeneratedFile> files)
    {
        if (Manifest.Read(folder) is null)
            RefuseForeignFiles(folder, files);
    }

    /// <summary>
    /// The lines a refresh reports, as the contract gives them: nothing when nothing changed; <c>&lt;prefix&gt;: &lt;folder&gt;: &lt;n&gt; files
    /// updated for dmc X.Y.Z</c>; and, when files edited by hand were kept, <c>&lt;prefix&gt;: &lt;folder&gt;: &lt;n&gt; edited files kept in
    /// .dmc/replaced/&lt;stamp&gt;/</c>.
    /// </summary>
    /// <param name="prefix">The command that prints them: <c>dmc refresh</c> (also <c>dmc play</c> prints the refresh's own lines) or <c>dmc install</c>.</param>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="result">What the refresh did.</param>
    /// <param name="version">The version of the program.</param>
    public static IEnumerable<string> Report(string prefix, string folder, RefreshResult result, string version)
    {
        if (result.Updated > 0)
            yield return $"{prefix}: {folder}: {result.Updated} files updated for dmc {version}";
        if (result.Kept > 0)
            yield return $"{prefix}: {folder}: {result.Kept} edited files kept in .dmc/replaced/{result.Stamp}/";
    }

    /// <summary>
    /// Whether a DMC folder is already what this program would write: it has a manifest of this version and every generated
    /// file is there and the same as the program's copy. <c>dmc play</c> asks this before it starts Claude Code.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="files">The program's generated files.</param>
    /// <param name="version">The program's version.</param>
    /// <returns>False when the folder has no manifest, a manifest of another version, or a generated file that is missing or different (or cannot be read).</returns>
    public static bool IsCurrent(string folder, IReadOnlyList<GeneratedFile> files, string version)
    {
        try
        {
            if (Manifest.Read(folder) is not { } manifest || manifest.Version != version)
                return false;
            return files.All(f => ReadOrNull(Absolute(folder, f.Path)) is { } bytes && Manifest.Hash(bytes) == f.Sha256);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DmcException)
        {
            return false;
        }
    }

    /// <summary>The first file of a first install that already exists and is not the program's copy: nothing has been written yet, so the folder is left as it was.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="files">The program's generated files, in table order.</param>
    /// <exception cref="DmcException">A file is in the way; the message names the first.</exception>
    static void RefuseForeignFiles(string folder, IReadOnlyList<GeneratedFile> files)
    {
        foreach (var file in files)
        {
            string path = Absolute(folder, file.Path);
            if (Directory.Exists(path))
                throw new DmcException($"{path} already exists and was not written by dmc: move it, or choose another folder");
            byte[]? current;
            try
            {
                current = ReadOrNull(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new DmcException($"cannot write {path}: {e.Message}");
            }
            if (current is not null && Manifest.Hash(current) != file.Sha256)
                throw new DmcException($"{path} already exists and was not written by dmc: move it, or choose another folder");
        }
    }

    /// <summary>The full path of a path relative to the DMC folder, written with <c>/</c>.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="relative">The relative path with <c>/</c>.</param>
    static string Absolute(string folder, string relative) => Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The bytes of a file, or null when it does not exist.</summary>
    /// <param name="path">The file.</param>
    static byte[]? ReadOrNull(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;

    /// <summary>Writes a file, making its folders first.</summary>
    /// <param name="path">The file.</param>
    /// <param name="bytes">Its new content.</param>
    static void Write(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Copies what is in a file to <c>.dmc/replaced/&lt;stamp&gt;/&lt;path&gt;</c>, before it is replaced or removed.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="stamp">The <c>yyyyMMdd-HHmmss</c> name of this refresh's folder.</param>
    /// <param name="relative">The file's path relative to the DMC folder, with <c>/</c>.</param>
    /// <param name="bytes">What the file held.</param>
    static void Keep(string folder, string stamp, string relative, byte[] bytes) =>
        Write(Absolute(Path.Combine(folder, ".dmc", "replaced", stamp), relative), bytes);

    /// <summary>
    /// Removes the folders a removed file leaves empty, from its own folder up to (not including) <c>.claude/skills</c>, so a
    /// dropped skill leaves no empty folder behind. Anything outside <c>.claude/skills/</c> is left as it is.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="start">The folder the removed file was in.</param>
    static void RemoveEmptyFolders(string folder, string start)
    {
        string skills = Path.GetFullPath(Path.Combine(folder, ".claude", "skills")) + Path.DirectorySeparatorChar;
        string? current = start;
        while (current is not null && (Path.GetFullPath(current) + Path.DirectorySeparatorChar).StartsWith(skills, StringComparison.OrdinalIgnoreCase)
               && (Path.GetFullPath(current) + Path.DirectorySeparatorChar) != skills
               && Directory.Exists(current) && !Directory.EnumerateFileSystemEntries(current).Any())
        {
            Directory.Delete(current);
            current = Path.GetDirectoryName(current);
        }
    }
}
