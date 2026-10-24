using System.Security.Cryptography;
using Dmc.Kit;

namespace Dmc.Tests.Support;

/// <summary>
/// Temporary DMC folders and the other things the tests of <c>dmc install</c>, <c>refresh</c>, <c>play</c> and <c>folder</c>
/// share: a folder that counts as a DMC folder without a real install, a machine over temporary folders and fakes (so that
/// nothing a test does can reach the real home folder, the real PATH or the real registry), and a hash of everything in a
/// set of folders, to prove that nothing in them changed.
/// </summary>
public static class DmcFolders
{
    /// <summary>A new folder under the system's temporary folder, made empty; the test deletes it.</summary>
    public static string NewTemp()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// Makes a folder a DMC folder the cheap way: writes an empty <c>.dmc/manifest.json</c> (version <c>0.0.0-test</c>, no files)
    /// into it, which is all <see cref="DmcFolder.IsDmcFolder"/> looks for.
    /// </summary>
    /// <param name="folder">The folder, made if it does not exist.</param>
    /// <returns>The folder, for chaining.</returns>
    public static string MakeDmcFolder(string folder)
    {
        Directory.CreateDirectory(Path.Combine(folder, ".dmc"));
        File.WriteAllText(Path.Combine(folder, ".dmc", "manifest.json"), "{ \"dmc\": \"0.0.0-test\", \"files\": [] }\n");
        return folder;
    }

    /// <summary>
    /// Makes a folder a real, current DMC folder in this process: the generated files of this build and a manifest of this
    /// version, as <c>dmc install</c> writes them, so that the published <c>dmc</c> (the same sources) finds nothing to refresh.
    /// </summary>
    /// <param name="folder">The folder, made if it does not exist.</param>
    /// <returns>The folder, for chaining.</returns>
    public static string MakeCurrentDmcFolder(string folder)
    {
        Refresher.Refresh(folder, GeneratedFiles.All, GeneratedFiles.Version, DateTime.Now);
        return folder;
    }

    /// <summary>
    /// For a test that runs the published <c>dmc</c> and expects it to find no DMC folder: skips it when <c>dmc.json</c> beside the
    /// binary already lists folders (a developer who tried <c>artifacts/bin/dmc install</c> has one), because the binary would then fall
    /// back to a recorded folder. CI never has one.
    /// </summary>
    public static void NeedNoRecordedFolders()
    {
        string record = Path.Combine(Path.GetDirectoryName(DmcRunner.Binary())!, "dmc.json");
        if (File.Exists(record) && DmcFolder.Recorded(Path.GetDirectoryName(record)!).Any(DmcFolder.IsDmcFolder))
            Assert.Inconclusive($"{record} lists DMC folders, so the binary would find one: delete it to run this test");
    }

    /// <summary>Deletes a folder and everything in it, clearing read-only attributes first; does nothing when it is not there.</summary>
    /// <param name="folder">The folder.</param>
    public static void Delete(string folder)
    {
        if (!Directory.Exists(folder))
            return;
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(folder, recursive: true);
    }

    /// <summary>
    /// One line per file under the given folders (relative path and SHA-256, in ordinal order of path), or per folder that is
    /// missing: equal snapshots mean no file was added, removed or changed. Empty folders are listed too, so an added folder shows.
    /// </summary>
    /// <param name="root">The folder the paths are relative to.</param>
    /// <param name="relativeFolders">The folders under <paramref name="root"/> to take, as relative paths; a missing one is recorded as missing.</param>
    public static string[] Snapshot(string root, params string[] relativeFolders)
    {
        var lines = new List<string>();
        foreach (string relative in relativeFolders)
        {
            string folder = Path.Combine(root, relative);
            if (!Directory.Exists(folder))
            {
                lines.Add($"{relative}/ (missing)");
                continue;
            }
            foreach (string entry in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetRelativePath(root, entry).Replace('\\', '/');
                lines.Add(Directory.Exists(entry) ? name + "/" : $"{name} {Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(entry)))}");
            }
            lines.Add($"{relative}/");
        }
        lines.Sort(StringComparer.Ordinal);
        return [.. lines];
    }

    /// <summary>
    /// One line per file anywhere under a folder (relative path and SHA-256, in ordinal order), for comparing whole folders. The
    /// files under <c>.dmc/</c> and the generated files themselves are included, so use <see cref="Snapshot"/> for the player's.
    /// </summary>
    /// <param name="root">The folder.</param>
    public static string[] SnapshotAll(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => $"{Path.GetRelativePath(root, f).Replace('\\', '/')} {Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(f)))}")
            .Order(StringComparer.Ordinal).ToArray();
}
