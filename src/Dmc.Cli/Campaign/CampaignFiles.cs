using System.Security.Cryptography;

namespace Dmc.Campaign;

/// <summary>
/// The campaign files that <c>verify</c> and <c>restore</c> compare (data-model.md § Campaign files): every file under
/// <c>adventures/&lt;campaign&gt;/</c> except the top-level <c>backups/</c> folder and the <c>.standard</c> record, on a backup's
/// side and the campaign's alike. The one walk both use, so they cannot disagree about what counts.
/// </summary>
internal static class CampaignFiles
{
    /// <summary>True for a path, relative to the campaign with <c>/</c> separators, that is a campaign file.</summary>
    /// <param name="path">A file's path inside the campaign or inside a backup's <c>campaign/</c> folder.</param>
    public static bool Counts(string path) =>
        path != StandardRecord.FileName
        && path != BackupFolder.FolderName
        && !path.StartsWith(BackupFolder.FolderName + "/", StringComparison.Ordinal);

    /// <summary>Reads every campaign file of a campaign folder into memory, by relative path in ordinal order.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static SortedDictionary<string, byte[]> Read(string campaignFolder)
    {
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(campaignFolder, "*", SearchOption.AllDirectories))
        {
            string path = Path.GetRelativePath(campaignFolder, file).Replace(Path.DirectorySeparatorChar, '/');
            if (Counts(path))
                files[path] = File.ReadAllBytes(file);
        }
        return files;
    }

    /// <summary>The campaign files of a backup's contents, which hold the campaign's files by relative path.</summary>
    /// <param name="all">Every file of the backup's campaign, as <see cref="ExportSummary.ReadCampaign"/> returns them.</param>
    public static SortedDictionary<string, byte[]> Of(IReadOnlyDictionary<string, byte[]> all)
    {
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (path, bytes) in all)
        {
            if (Counts(path))
                files[path] = bytes;
        }
        return files;
    }

    /// <summary>The SHA-256 of some bytes as 64 lower-case hexadecimal digits, as the manifest and the verified record write it.</summary>
    /// <param name="bytes">The bytes.</param>
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
