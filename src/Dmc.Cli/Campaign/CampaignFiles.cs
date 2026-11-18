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
    public static bool Counts(string path) => path != StandardRecord.FileName && !InBackups(path);

    /// <summary>
    /// True for the top-level <c>backups/</c> folder or a path under it. An export never holds one, and an import or restore of
    /// an archive an older <c>dmc</c> wrote with one leaves it out.
    /// </summary>
    /// <param name="path">A path inside the campaign, with <c>/</c> separators.</param>
    public static bool InBackups(string path) =>
        path == BackupFolder.FolderName || path.StartsWith(BackupFolder.FolderName + "/", StringComparison.Ordinal);

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

    /// <summary>
    /// The map page's files, relative to the campaign: written by <c>dmc map</c> (<see cref="Maps.MapRedraw"/>) and drawn
    /// again from <c>maps/live.json</c> by <c>dmc redraw</c>. They are derived, not campaign memory, and the git rules
    /// leave them out (<c>kit/templates/adventures.gitignore</c>, which a test keeps in step with this list).
    /// </summary>
    public static IReadOnlyList<string> DerivedFiles { get; } =
        ["maps/" + Maps.MapRedraw.PageFile, "maps/" + Maps.MapRedraw.DataFile, "maps/" + Maps.MapRedraw.PictureFile,
         "maps/" + Maps.MapRedraw.SourceFile];

    /// <summary>
    /// True for a derived file (<see cref="DerivedFiles"/>). The checks that look for play since a backup or a close skip
    /// them, so a campaign checked out from git without its map page, or with one drawn again, is not changed (spec 018
    /// FR-005). Backups and exports still carry them.
    /// </summary>
    /// <param name="path">A path inside the campaign, with <c>/</c> separators.</param>
    public static bool IsDerived(string path) => DerivedFiles.Contains(path, StringComparer.Ordinal);

    /// <summary>
    /// How a campaign now differs from an earlier copy of it (a closing backup, or a backup with no verified record): one
    /// <c>missing: </c>, <c>added: </c> or <c>changed: </c> line per path, in ordinal order. Derived files are skipped and
    /// files are compared with <see cref="SameContent"/>, so line endings alone are no change (spec 018 FR-005). The one
    /// comparison the closed-campaign check and the restore's warning share, so they cannot disagree.
    /// </summary>
    /// <param name="held">The earlier copy's files, by path.</param>
    /// <param name="now">The campaign's files now, by path.</param>
    public static List<string> Differences(IReadOnlyDictionary<string, byte[]> held, IReadOnlyDictionary<string, byte[]> now)
    {
        var lines = new List<string>();
        foreach (string path in held.Keys.Union(now.Keys).Where(p => !IsDerived(p)).Order(StringComparer.Ordinal))
        {
            if (!now.TryGetValue(path, out var bytes))
                lines.Add("missing: " + path);
            else if (!held.TryGetValue(path, out var old))
                lines.Add("added: " + path);
            else if (!SameContent(bytes, old))
                lines.Add("changed: " + path);
        }
        return lines;
    }

    /// <summary>How many leading bytes <see cref="IsText"/> looks at: git's own limit for <c>text=auto</c>.</summary>
    const int TextProbe = 8000;

    /// <summary>
    /// True when bytes are text: no NUL byte in the first 8,000 (spec 018 contract line-ending-compare.md). This is only the
    /// first of git's tests for <c>text=auto</c>, which also reads the whole file and takes a lone CR or many control bytes as
    /// binary, so it passes some files git would not convert. That is safe: two files that differ only in line endings hold
    /// the same content whichever test is used, and a file with a NUL is still compared byte for byte.
    /// </summary>
    /// <param name="bytes">A file's bytes.</param>
    public static bool IsText(byte[] bytes) => bytes.AsSpan(0, Math.Min(bytes.Length, TextProbe)).IndexOf((byte)0) < 0;

    /// <summary>
    /// True when two files hold the same content: equal byte for byte, or both text and equal once every CRLF is read as
    /// LF. A git checkout or a Windows write that only changes line endings is no change to the campaign (spec 018 FR-005).
    /// A lone CR is content, not a line ending.
    /// </summary>
    /// <param name="a">One file's bytes.</param>
    /// <param name="b">The other's.</param>
    public static bool SameContent(byte[] a, byte[] b) =>
        a.AsSpan().SequenceEqual(b) || (IsText(a) && IsText(b) && ToLf(a).AsSpan().SequenceEqual(ToLf(b)));

    /// <summary>
    /// True when a file matches a SHA-256 recorded from it earlier, line endings aside: the hash is of the bytes as they are,
    /// or, for text, of them with every CRLF turned into LF, or of that with every LF turned into CRLF. So a record of a
    /// CRLF file still matches the LF file a checkout gives back, and the reverse (spec 018 FR-005). A file whose endings
    /// were mixed when recorded matches only as it was, since the mix cannot be rebuilt from a hash.
    /// </summary>
    /// <param name="bytes">The file's bytes now.</param>
    /// <param name="hash">The recorded SHA-256, as <see cref="Hash"/> writes it.</param>
    public static bool MatchesHash(byte[] bytes, string hash)
    {
        if (Hash(bytes) == hash)
            return true;
        if (!IsText(bytes))
            return false;
        byte[] lf = ToLf(bytes);
        return Hash(lf) == hash || Hash(ToCrlf(lf)) == hash;
    }

    /// <summary>The bytes with every CRLF turned into LF; a lone CR is kept.</summary>
    /// <param name="bytes">Text bytes.</param>
    static byte[] ToLf(byte[] bytes)
    {
        if (bytes.AsSpan().IndexOf("\r\n"u8) < 0)
            return bytes;
        var lf = new List<byte>(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == '\r' && i + 1 < bytes.Length && bytes[i + 1] == '\n')
                continue;
            lf.Add(bytes[i]);
        }
        return [.. lf];
    }

    /// <summary>The bytes with every LF turned into CRLF.</summary>
    /// <param name="lf">Text bytes with no CRLF in them (the output of <see cref="ToLf"/>).</param>
    static byte[] ToCrlf(byte[] lf)
    {
        var crlf = new List<byte>(lf.Length + lf.Length / 32);
        foreach (byte b in lf)
        {
            if (b == '\n')
                crlf.Add((byte)'\r');
            crlf.Add(b);
        }
        return [.. crlf];
    }
}
