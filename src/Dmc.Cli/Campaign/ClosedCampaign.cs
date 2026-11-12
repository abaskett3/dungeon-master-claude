using System.Globalization;
using System.Text;

namespace Dmc.Campaign;

/// <summary>
/// The closed mark, <c>adventures/&lt;campaign&gt;/.closed</c> (spec 010, research R1 and R8): one line, the UTC time of the close
/// and the closing backup's file name, separated by one space, then a newline. Only <c>dmc campaign close --yes</c> writes
/// it, as its last step, and nothing removes it but the same run's rollback. Its presence is the state: a campaign with the file is closed
/// whatever the file holds. This class is the one home of the mark's name, its reading and writing, and the refusal every
/// writing command gives for a closed campaign.
/// </summary>
internal static class ClosedCampaign
{
    /// <summary>The mark's file name in the campaign folder.</summary>
    public const string FileName = ".closed";

    /// <summary>How the mark writes the time: UTC, to the second, as a manifest's <c>exportedAt</c>.</summary>
    const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>What a readable mark holds.</summary>
    /// <param name="ClosedAt">The moment of the close, UTC.</param>
    /// <param name="Backup">The closing backup's file name in <c>backups/</c>, with no path.</param>
    internal sealed record Mark(DateTime ClosedAt, string Backup)
    {
        /// <summary>The time as the mark and <c>dmc campaign standard</c> write it.</summary>
        public string Time => ClosedAt.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>The refusal every writing command gives for a closed campaign; the <c>dmc &lt;command&gt;:</c> prefix is the caller's.</summary>
    /// <param name="name">The campaign's folder name.</param>
    public static string Refusal(string name) => $"{name} is closed: start a sequel to continue its story";

    /// <summary>The path of a campaign's mark.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static string PathOf(string campaignFolder) => Path.Combine(campaignFolder, FileName);

    /// <summary>True when the campaign holds a mark, whatever it says.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static bool IsClosed(string campaignFolder) => File.Exists(PathOf(campaignFolder));

    /// <summary>Reads a campaign's mark.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <returns>The mark, or null when there is none or it cannot be read.</returns>
    public static Mark? Read(string campaignFolder)
    {
        try
        {
            string path = PathOf(campaignFolder);
            return File.Exists(path) ? Parse(File.ReadAllBytes(path)) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Reads a mark from its bytes, as held on disk or in an archive.</summary>
    /// <param name="bytes">The mark's bytes.</param>
    /// <returns>The mark, or null unless the bytes are a UTC time, one space, a plain <c>.zip</c> file name and at most one line end.</returns>
    public static Mark? Parse(byte[] bytes)
    {
        string text = Encoding.UTF8.GetString(bytes);
        if (text.EndsWith("\r\n", StringComparison.Ordinal))
            text = text[..^2];
        else if (text.EndsWith('\n'))
            text = text[..^1];
        string[] fields = text.Split(' ');
        if (fields.Length != 2 || !ExportSummary.TryParseTimestamp(fields[0], out var time))
            return null;
        string backup = fields[1];
        bool plain = backup.EndsWith(".zip", StringComparison.Ordinal)
            && backup.IndexOfAny(['/', '\\']) < 0
            && !backup.Any(char.IsControl)
            && backup is not (".zip" or "..zip");
        return plain ? new Mark(time, backup) : null;
    }

    /// <summary>The line <c>dmc campaign standard</c> prints for a closed campaign.</summary>
    /// <param name="mark">The campaign's mark; null when it cannot be read.</param>
    public static string Line(Mark? mark) => "closed: " + (mark?.Time ?? "(mark unreadable)");

    /// <summary>The mark's bytes: the time, one space, the closing backup's file name and a newline, in UTF-8.</summary>
    /// <param name="utc">The moment of the close.</param>
    /// <param name="backupFileName">The closing backup's file name.</param>
    public static byte[] Bytes(DateTime utc, string backupFileName) =>
        Encoding.UTF8.GetBytes(new Mark(utc.ToUniversalTime(), backupFileName).Time + " " + backupFileName + "\n");

    /// <summary>
    /// Writes the mark through a temporary file renamed into place, so a reader never sees half a mark. The bytes are
    /// <see cref="Bytes"/>, the same the closing backup already holds.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="utc">The moment of the close.</param>
    /// <param name="backupFileName">The closing backup's file name.</param>
    /// <exception cref="IOException">The mark could not be written; nothing is left under its name.</exception>
    public static void Write(string campaignFolder, DateTime utc, string backupFileName)
    {
        string path = PathOf(campaignFolder);
        string partial = path + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            File.WriteAllBytes(partial, Bytes(utc, backupFileName));
            File.Move(partial, path, overwrite: false);
        }
        finally
        {
            CampaignBackup.Remove(partial);
        }
    }

    /// <summary>Removes the mark. Only a close's own rollback calls this, for the mark the same run wrote.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <returns>True when the mark is gone.</returns>
    internal static bool Remove(string campaignFolder) => CampaignBackup.Remove(PathOf(campaignFolder));

    /// <summary>
    /// Refuses a write into a closed campaign: when <paramref name="path"/> is a campaign folder under <c>adventures/</c>, or
    /// anything under one, and that campaign holds a mark, throws the refusal. A path anywhere else (outside
    /// <c>adventures/</c>, <c>adventures/</c> itself, a file directly in it, or a folder whose name starts with a dot, such as
    /// <c>.transcript-review</c>) is never refused.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="path">The path a command is about to write, or the campaign folder.</param>
    /// <exception cref="DmcException">The path is in a closed campaign.</exception>
    public static void Refuse(string root, string path)
    {
        if (CampaignOf(root, path) is { } campaign && IsClosed(campaign))
            throw new DmcException(Refusal(Path.GetFileName(campaign)));
    }

    /// <summary>
    /// <see cref="Refuse"/> for a command that writes to a path it was given (<c>map</c>, <c>pdf --out</c>, <c>digest show
    /// --out</c>), without a DMC folder of its own: the DMC folder is the one the path lies in, found by walking up to an
    /// <c>adventures/</c> folder whose parent is a DMC folder. A path in no DMC folder is never refused.
    /// </summary>
    /// <param name="path">The path the command is about to write.</param>
    /// <exception cref="DmcException">The path is in a closed campaign.</exception>
    public static void RefuseAt(string path)
    {
        var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        string full = Path.GetFullPath(path);
        for (var folder = new DirectoryInfo(full); folder.Parent is { } parent; folder = parent)
        {
            if (folder.Name.Equals("adventures", comparison) && DmcFolder.IsDmcFolder(parent.FullName))
            {
                Refuse(parent.FullName, full);
                return;
            }
        }
    }

    /// <summary>The campaign folder a path lies in, or null when it lies in none.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="path">A path, relative to the working folder or full.</param>
    internal static string? CampaignOf(string root, string path)
    {
        string adventures = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(root, "adventures")));
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (!CampaignExporter.IsInside(full, adventures) || full.Length == adventures.Length)
            return null;
        string first = full[(adventures.Length + 1)..].Split(Path.DirectorySeparatorChar)[0];
        string campaign = Path.Combine(adventures, first);
        return first.StartsWith('.') || !Directory.Exists(campaign) ? null : campaign;
    }
}
