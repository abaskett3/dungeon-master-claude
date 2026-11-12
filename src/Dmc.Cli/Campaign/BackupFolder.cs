using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>How <c>dmc campaign backup --prune</c> removes older backups, on the player's yes (FR-005a).</summary>
internal enum PruneMode
{
    /// <summary>Remove nothing.</summary>
    None,

    /// <summary>Remove the single oldest backup counted before the new one.</summary>
    Oldest,

    /// <summary>Remove every backup counted before the new one except the latest two.</summary>
    Old,
}

/// <summary>One counted backup of a campaign.</summary>
/// <param name="FileName">Its file name in <c>backups/</c>.</param>
/// <param name="Stamp">The 14-digit local time in its name (ordering uses the manifest's UTC time; see <see cref="BackupFolder.List"/>).</param>
/// <param name="Suffix">The clash suffix: 1 without one, else the <c>-N</c>.</param>
internal sealed record Backup(string FileName, string Stamp, int Suffix);

/// <summary>
/// The backups of a campaign, <c>adventures/&lt;campaign&gt;/backups/</c> (data-model.md § Backup; research R8): their names,
/// their order, their companion files and the retention rule. Every number of the retention rule lives here and nowhere
/// else: <c>dmc campaign backups</c> prints the question this class words, and <c>backup --prune</c> applies the yes.
/// </summary>
internal static partial class BackupFolder
{
    /// <summary>The name of the backups folder in a campaign.</summary>
    public const string FolderName = "backups";

    /// <summary>How many backups make the removal question due.</summary>
    const int Limit = 3;

    /// <summary>How many old backups a <c>--prune old</c> keeps beside the new one.</summary>
    const int Keep = Limit - 1;

    /// <summary>The companion written by DMC during a migration: the accounting of what changed.</summary>
    public const string AccountingSuffix = ".accounting.tsv";

    /// <summary>The companion written by a passing <c>verify</c>: the path and SHA-256 of every campaign file.</summary>
    public const string VerifiedSuffix = ".verified.tsv";

    /// <summary>The companion written by <c>backup</c> when a migration is pending, and removed by a passing <c>verify</c> or a <c>restore</c>: a migration is under way.</summary>
    public const string MigratingSuffix = ".migrating";

    /// <summary>Every companion suffix, in the order they are removed.</summary>
    public static readonly string[] CompanionSuffixes = [AccountingSuffix, VerifiedSuffix, MigratingSuffix];

    /// <summary>A counted backup's name, after the campaign's name: <c>_backup_</c>, 14 digits, an optional <c>-N</c>, <c>.zip</c>.</summary>
    [GeneratedRegex(@"^_backup_(\d{14})(?:-(\d+))?\.zip$", RegexOptions.CultureInvariant)]
    private static partial Regex Tail { get; }

    /// <summary>The backups folder of a campaign folder.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static string Of(string campaignFolder) => Path.Combine(campaignFolder, FolderName);

    /// <summary>The name of a backup taken at a moment, without <c>.zip</c> or a clash suffix: <c>&lt;campaign&gt;_backup_&lt;yyyyMMddHHmmss&gt;</c>, local time.</summary>
    /// <param name="campaign">The campaign's name.</param>
    /// <param name="moment">The moment of the backup.</param>
    public static string Stem(string campaign, DateTime moment) =>
        campaign + "_backup_" + moment.ToLocalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

    /// <summary>A closing backup's name, after the campaign's name: <c>_closed_</c>, 14 digits, <c>.zip</c>, never a suffix (spec 010 research R4).</summary>
    [GeneratedRegex(@"^_closed_(\d{14})\.zip$", RegexOptions.CultureInvariant)]
    private static partial Regex ClosedTail { get; }

    /// <summary>
    /// The file name of the closing backup taken at a moment: <c>&lt;campaign&gt;_closed_&lt;yyyyMMddHHmmss&gt;.zip</c>, local time,
    /// never suffixed (spec 010 research R4). <see cref="Parse"/> does not match it, so a closing backup is never listed,
    /// counted, offered for removal, pruned, resolved as the latest, or taken for a marked migration.
    /// </summary>
    /// <param name="campaign">The campaign's name.</param>
    /// <param name="moment">The moment of the close.</param>
    public static string ClosedName(string campaign, DateTime moment) =>
        campaign + "_closed_" + moment.ToLocalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".zip";

    /// <summary>Reads a file name as a closing backup of a campaign.</summary>
    /// <param name="campaign">The campaign's name.</param>
    /// <param name="fileName">A file name.</param>
    /// <returns>The 14-digit local time in the name, or null when it is not a closing backup of this campaign.</returns>
    public static string? ParseClosed(string campaign, string fileName)
    {
        if (!fileName.StartsWith(campaign, StringComparison.Ordinal))
            return null;
        var match = ClosedTail.Match(fileName[campaign.Length..]);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The closing backups in a campaign's <c>backups/</c>, by file name in ordinal order: usually one for a closed campaign and none for an open one, more only when a close was stopped before its mark and run again.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's name.</param>
    public static IReadOnlyList<string> Closing(string campaignFolder, string campaign)
    {
        string folder = Of(campaignFolder);
        if (!Directory.Exists(folder))
            return [];
        return [.. new DirectoryInfo(folder).EnumerateFiles()
            .Select(f => f.Name)
            .Where(n => ParseClosed(campaign, n) is not null)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Reads a file name as a counted backup of a campaign.</summary>
    /// <param name="campaign">The campaign's name.</param>
    /// <param name="fileName">A file name in <c>backups/</c>.</param>
    /// <returns>The backup, or null when the name is not one this rule counts (another campaign's, or any other file).</returns>
    public static Backup? Parse(string campaign, string fileName)
    {
        if (!fileName.StartsWith(campaign, StringComparison.Ordinal))
            return null;
        var match = Tail.Match(fileName[campaign.Length..]);
        if (!match.Success)
            return null;
        int suffix = match.Groups[2].Success && int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : 1;
        return new Backup(fileName, match.Groups[1].Value, suffix);
    }

    /// <summary>
    /// The counted backups of a campaign, oldest first: by the UTC time in each one's manifest, then the suffix. A backup
    /// whose manifest cannot be read is placed by the local time in its name. The names alone would misorder two backups
    /// taken either side of a clock going back an hour, which makes the later one's name the earlier.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's name.</param>
    public static IReadOnlyList<Backup> List(string campaignFolder, string campaign)
    {
        string folder = Of(campaignFolder);
        if (!Directory.Exists(folder))
            return [];
        return [.. new DirectoryInfo(folder).EnumerateFiles()
            .Select(f => Parse(campaign, f.Name))
            .OfType<Backup>()
            .Select(b => (Backup: b, Taken: ExportSummary.ExportedAt(Path.Combine(folder, b.FileName)) ?? NameTime(b.Stamp)))
            .OrderBy(b => b.Taken)
            .ThenBy(b => b.Backup.Stamp, StringComparer.Ordinal)
            .ThenBy(b => b.Backup.Suffix)
            .Select(b => b.Backup)];
    }

    /// <summary>The local time in a backup's name as UTC; the latest time there is when the digits are not a date.</summary>
    /// <param name="stamp">The 14 digits.</param>
    static DateTime NameTime(string stamp) =>
        DateTime.TryParseExact(stamp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var time) ? time : DateTime.MaxValue;

    /// <summary>Opens a backup with <see cref="ExportSummary.Open"/> and refuses it unless it is a backup of the campaign.</summary>
    /// <param name="backup">The backup's path.</param>
    /// <param name="campaign">The campaign it must belong to.</param>
    /// <exception cref="DmcException">The backup is not a usable export, or it is another campaign's.</exception>
    public static ExportSummary Open(string backup, string campaign)
    {
        var summary = ExportSummary.Open(backup);
        return summary.Manifest.Campaign == campaign
            ? summary
            : throw new DmcException($"{Path.GetFileName(backup)} is a backup of {summary.Manifest.Campaign}, not {campaign}");
    }

    /// <summary>Removes every <c>.migrating</c> mark in a campaign's <c>backups/</c>: the migration is finished or undone.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static void ClearMarks(string campaignFolder)
    {
        string folder = Of(campaignFolder);
        if (!Directory.Exists(folder))
            return;
        foreach (string mark in Directory.EnumerateFiles(folder, "*" + MigratingSuffix))
            CampaignBackup.Remove(mark);
    }

    /// <summary>The path of a backup's companion file.</summary>
    /// <param name="backupPath">The backup's full path.</param>
    /// <param name="suffix">One of <see cref="CompanionSuffixes"/>.</param>
    public static string Companion(string backupPath, string suffix) => backupPath + suffix;

    /// <summary>The backup of a campaign with a <c>.migrating</c> mark, the oldest if there were ever more than one.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's name.</param>
    /// <returns>The backup's file name, or null when no migration is under way.</returns>
    public static string? Marked(string campaignFolder, string campaign) =>
        List(campaignFolder, campaign)
            .FirstOrDefault(b => File.Exists(Companion(Path.Combine(Of(campaignFolder), b.FileName), MigratingSuffix)))?.FileName;

    /// <summary>
    /// The backup <c>verify</c> and <c>restore</c> use: the one named (a file name in <c>backups/</c>, or a path), else the
    /// marked backup of an unfinished migration, else the latest.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's name.</param>
    /// <param name="given">What <c>--backup</c> or <c>--from</c> named; null for the default.</param>
    /// <returns>The backup's full path.</returns>
    /// <exception cref="DmcException">The named file does not exist, or the campaign has no backup.</exception>
    public static string Resolve(string campaignFolder, string campaign, string? given)
    {
        string folder = Of(campaignFolder);
        if (given is not null)
        {
            string inFolder = Path.Combine(folder, given);
            if (given.IndexOfAny(['/', '\\']) < 0 && File.Exists(inFolder))
                return inFolder;
            string full = Path.GetFullPath(given);
            return File.Exists(full) ? full : throw new DmcException($"no backup {given}: dmc campaign backups {campaign} lists them");
        }
        if (Marked(campaignFolder, campaign) is { } marked)
            return Path.Combine(folder, marked);
        var all = List(campaignFolder, campaign);
        return all.Count > 0
            ? Path.Combine(folder, all[^1].FileName)
            : throw new DmcException($"{campaign} has no backup: dmc campaign backup {campaign} takes one");
    }

    /// <summary>The backups a prune removes, chosen from those counted before the new one.</summary>
    /// <param name="earlier">The counted backups before the new one, oldest first.</param>
    /// <param name="mode">What the player agreed to.</param>
    public static IReadOnlyList<Backup> PruneSelection(IReadOnlyList<Backup> earlier, PruneMode mode) => mode switch
    {
        PruneMode.Oldest => [.. earlier.Take(1)],
        PruneMode.Old => [.. earlier.Take(Math.Max(0, earlier.Count - Keep))],
        _ => [],
    };

    /// <summary>
    /// The removal question <c>dmc campaign backups</c> ends with, holding the flag a yes allows: none below the limit or
    /// while a migration is under way; the oldest at exactly the limit; all but the latest two above it.
    /// </summary>
    /// <param name="backups">The counted backups, oldest first.</param>
    /// <param name="marked">True while a backup carries a <c>.migrating</c> mark.</param>
    /// <returns>The <c>ask:</c> line, or null when no question is due.</returns>
    public static string? AskLine(IReadOnlyList<Backup> backups, bool marked)
    {
        if (marked || backups.Count < Limit)
            return null;
        if (backups.Count == Limit)
            return $"ask: may the oldest backup, {backups[0].FileName}, be removed? yes: --prune oldest";
        return string.Create(CultureInfo.InvariantCulture,
            $"ask: may the {backups.Count - Keep} oldest backups be removed, keeping the latest two? yes: --prune old");
    }

    /// <summary>Reads the value of <c>--prune</c>.</summary>
    /// <param name="value">What was given.</param>
    /// <returns>The mode, or null for a value that is not <c>oldest</c> or <c>old</c>.</returns>
    public static PruneMode? ParsePrune(string value) => value switch
    {
        "oldest" => PruneMode.Oldest,
        "old" => PruneMode.Old,
        _ => null,
    };
}
