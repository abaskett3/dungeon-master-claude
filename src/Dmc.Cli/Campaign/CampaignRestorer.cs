using System.Globalization;

namespace Dmc.Campaign;

/// <summary>
/// <c>dmc campaign restore NAME [--from FILE] [--yes]</c> (contracts/campaign-commands.md § restore; research R9): puts a
/// campaign back as a backup holds it. Without <c>--yes</c> it prints the plan (files replaced, removed and added, and a
/// warning when the campaign has changed since the migration was verified) and changes nothing. With <c>--yes</c> it
/// extracts the backup, checked, into a staging folder in <c>adventures/</c>, moves the campaign's <c>backups/</c> into it,
/// renames the campaign aside, renames the staging into place and deletes the aside; every <c>.migrating</c> mark of the
/// campaign goes. A failure before the rename into place puts the campaign back; a run that finds the campaign renamed aside
/// by an interrupted restore puts it back before anything else.
/// </summary>
internal static class CampaignRestorer
{
    /// <summary>
    /// A test's hook, called with the name of each step as it is done (<c>staged</c>, <c>backups-moved</c>, <c>aside</c>): an
    /// <see cref="IOException"/> thrown from it is a failure the restore recovers from; any other exception stops the restore
    /// where it is, as if the process had been killed. Null in the program.
    /// </summary>
    internal static Action<string>? Hook { get; set; }

    /// <summary>What every staging folder's name starts with, before the campaign's name.</summary>
    const string StagingPrefix = ".restore-";

    /// <summary>What the campaign renamed aside is called, before the campaign's name.</summary>
    const string AsidePrefix = ".replaced-";

    /// <summary>Plans or makes a restore.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="from">The backup named by <c>--from</c>; null for the marked backup, else the latest.</param>
    /// <param name="yes">True to restore; false to print the plan only.</param>
    /// <returns>The lines to print.</returns>
    /// <exception cref="DmcException">No such campaign; the backup is missing, damaged or another campaign's; or the restore failed, in which case the campaign is as it was.</exception>
    public static IReadOnlyList<string> Run(string root, string name, string? from, bool yes)
    {
        string adventures = Path.Combine(root, "adventures");
        var lines = new List<string>();
        if (Recover(adventures, name) is { } recovered)
            lines.Add(recovered);

        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        string backup = BackupFolder.Resolve(campaign, name, from);
        string file = Path.GetFileName(backup);
        var summary = ExportSummary.Open(backup);
        if (summary.Manifest.Campaign != name)
            throw new DmcException($"{file} is a backup of {summary.Manifest.Campaign}, not {name}");
        var backupFiles = CampaignFiles.Of(summary.ReadCampaign());
        var current = CampaignFiles.Read(campaign);

        if (!yes)
        {
            int replaced = backupFiles.Count(f => current.TryGetValue(f.Key, out var now) && !now.AsSpan().SequenceEqual(f.Value));
            int removed = current.Keys.Count(p => !backupFiles.ContainsKey(p));
            int added = backupFiles.Keys.Count(p => !current.ContainsKey(p));
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"restore {name} from {file}: {replaced} files replaced, {removed} removed, {added} added"));
            var verified = CampaignVerifier.ReadVerified(BackupFolder.Companion(backup, BackupFolder.VerifiedSuffix));
            int changed = verified is null ? replaced + removed + added : Changed(current, verified);
            if (changed > 0)
            {
                string since = verified is null ? "the backup" : "the migration was verified";
                lines.Add(string.Create(CultureInfo.InvariantCulture,
                    $"warning: {name} has changed since {since} ({changed} files): those changes will be lost"));
            }
            lines.Add("confirm with --yes");
            return lines;
        }

        if (Apply(adventures, name, campaign, summary) is { } stuck)
            lines.Add(stuck);
        lines.Add(string.Create(CultureInfo.InvariantCulture, $"restored {name} from {file}: {summary.Manifest.Files.Count} files"));
        return lines;
    }

    /// <summary>How many campaign files differ from a verified record: changed, added or removed.</summary>
    /// <param name="current">The campaign's files.</param>
    /// <param name="verified">The SHA-256 of each file when the migration was verified.</param>
    static int Changed(SortedDictionary<string, byte[]> current, Dictionary<string, string> verified) =>
        current.Count(f => !verified.TryGetValue(f.Key, out string? hash) || hash != CampaignFiles.Hash(f.Value))
        + verified.Keys.Count(p => !current.ContainsKey(p));

    /// <summary>Replaces the campaign with the backup's files, keeping its <c>backups/</c> folder.</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="name">The campaign's name.</param>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="summary">The backup, already checked.</param>
    /// <returns>A line to print when the campaign renamed aside could not be deleted afterwards; else null.</returns>
    /// <exception cref="DmcException">The restore failed; the campaign is as it was.</exception>
    static string? Apply(string adventures, string name, string campaign, ExportSummary summary)
    {
        string id = Guid.NewGuid().ToString("N")[..8];
        string staging = Path.Combine(adventures, StagingPrefix + name + "-" + id);
        string aside = Path.Combine(adventures, AsidePrefix + name + "-" + id);
        string backups = BackupFolder.Of(campaign);
        string stagedBackups = BackupFolder.Of(staging);
        bool backupsMoved = false, asideDone = false;
        try
        {
            CampaignImporter.ExtractCampaign(summary, staging);
            Hook?.Invoke("staged");
            if (Directory.Exists(backups))
            {
                Directory.Move(backups, stagedBackups);
                backupsMoved = true;
            }
            Hook?.Invoke("backups-moved");
            Directory.Move(campaign, aside);
            asideDone = true;
            Hook?.Invoke("aside");
            Directory.Move(staging, campaign);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DmcException)
        {
            string? problem = null;
            try
            {
                if (asideDone && !Directory.Exists(campaign))
                    Directory.Move(aside, campaign);
                if (backupsMoved && Directory.Exists(stagedBackups) && !Directory.Exists(backups))
                    Directory.Move(stagedBackups, backups);
                DeleteFolder(staging);
            }
            catch (Exception inner) when (inner is IOException or UnauthorizedAccessException)
            {
                problem = $"; putting it back also failed ({Message(inner)}): run the restore again to recover it";
            }
            string message = e is DmcException d ? d.Message : Message(e);
            throw new DmcException($"could not restore {name}: {message}" + (problem ?? "; the campaign is as it was"));
        }

        foreach (string mark in Directory.Exists(backups) ? Directory.EnumerateFiles(backups, "*" + BackupFolder.MigratingSuffix) : [])
            CampaignBackup.Remove(mark);
        return DeleteFolder(aside) ? null : $"could not delete adventures/{Path.GetFileName(aside)}; it is safe to delete";
    }

    /// <summary>
    /// Puts back what an interrupted restore of this campaign left: the campaign renamed aside when there is no campaign
    /// folder, and its <c>backups/</c> from a staging folder; then deletes the leftovers.
    /// </summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="name">The campaign's name.</param>
    /// <returns>A line saying what was recovered, or null when there was nothing to recover.</returns>
    /// <exception cref="DmcException">The campaign could not be put back.</exception>
    static string? Recover(string adventures, string name)
    {
        if (!Directory.Exists(adventures))
            return null;
        string campaign = Path.Combine(adventures, name);
        var asides = Leftovers(adventures, AsidePrefix, name);
        var stagings = Leftovers(adventures, StagingPrefix, name);
        if (asides.Count == 0 && stagings.Count == 0)
            return null;
        try
        {
            foreach (string aside in asides)
            {
                if (!Directory.Exists(campaign))
                    Directory.Move(aside, campaign);
                else
                    DeleteFolder(aside);
            }
            foreach (string staging in stagings)
            {
                string staged = BackupFolder.Of(staging);
                if (Directory.Exists(campaign) && Directory.Exists(staged) && !Directory.Exists(BackupFolder.Of(campaign)))
                    Directory.Move(staged, BackupFolder.Of(campaign));
                DeleteFolder(staging);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"could not recover {name} from an interrupted restore: {Message(e)}");
        }
        return $"recovered {name} from an interrupted restore";
    }

    /// <summary>The folders of <c>adventures/</c> named <c>&lt;prefix&gt;&lt;name&gt;-&lt;8 hex digits&gt;</c>.</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="prefix">The staging or aside prefix.</param>
    /// <param name="name">The campaign's name.</param>
    static List<string> Leftovers(string adventures, string prefix, string name)
    {
        string start = prefix + name + "-";
        return [.. new DirectoryInfo(adventures).EnumerateDirectories()
            .Where(d => d.Name.StartsWith(start, StringComparison.Ordinal) && d.Name.Length == start.Length + 8
                && d.Name[start.Length..].All(char.IsAsciiHexDigitLower))
            .Select(d => d.FullName)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Deletes a folder and everything in it, if it is there.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>True when it is gone; false when something in it could not be deleted.</returns>
    static bool DeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>An exception's message as one clause.</summary>
    /// <param name="e">The exception.</param>
    static string Message(Exception e) => e.Message.TrimEnd('.', '\r', '\n', ' ');
}
