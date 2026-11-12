using System.Globalization;

namespace Dmc.Campaign;

/// <summary>
/// <c>dmc campaign restore NAME [--from FILE] [--yes]</c> (contracts/campaign-commands.md § restore; research R9): puts a
/// campaign back as a backup holds it. Without <c>--yes</c> it prints the plan (files replaced, removed and added, and a
/// warning when the campaign has changed since the migration was verified) and changes nothing. With <c>--yes</c> it
/// extracts the backup, checked, into a staging folder in <c>adventures/</c>, moves the campaign's <c>backups/</c> into it,
/// renames the campaign aside, renames the staging into place, removes every <c>.migrating</c> mark of the campaign and
/// deletes the aside. A failure before the rename into place puts the campaign back. What an interrupted restore left is
/// put back by <see cref="Recover"/>, which every migration verb runs first.
/// </summary>
internal static class CampaignRestorer
{
    /// <summary>
    /// A test's hook, called with the name of each step as it is done (<c>staged</c>, <c>backups-moved</c>, <c>aside</c>,
    /// <c>in-place</c>): an <see cref="IOException"/> thrown from it before <c>in-place</c> is a failure the restore recovers
    /// from; any other exception stops the restore where it is, as if the process had been killed. Null in the program.
    /// </summary>
    internal static Action<string>? Hook { get; set; }

    /// <summary>What every staging folder's name starts with, before the campaign's name.</summary>
    const string StagingPrefix = ".restore-";

    /// <summary>What the campaign renamed aside is called, before the campaign's name.</summary>
    const string AsidePrefix = ".replaced-";

    /// <summary>How many hexadecimal digits end a staging or aside folder's name: the id the two of one restore share.</summary>
    const int IdLength = 8;

    /// <summary>Plans or makes a restore.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="from">The backup named by <c>--from</c>; null for the marked backup, else the latest.</param>
    /// <param name="yes">True to restore; false to print the plan only.</param>
    /// <returns>The lines to print.</returns>
    /// <exception cref="DmcException">No such campaign; the campaign is closed (spec 010: refused, plan or not, before anything is read); the backup is missing, damaged or another campaign's; or the restore failed, in which case the campaign is as it was.</exception>
    public static IReadOnlyList<string> Run(string root, string name, string? from, bool yes)
    {
        string adventures = Path.Combine(root, "adventures");
        var lines = new List<string>();
        if (Recover(root, name) is { } recovered)
            lines.Add(recovered);

        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        ClosedCampaign.Refuse(root, campaign);
        string backup = BackupFolder.Resolve(campaign, name, from);
        string file = Path.GetFileName(backup);
        var summary = BackupFolder.Open(backup, name);

        if (!yes)
        {
            lines.AddRange(Plan(name, campaign, backup, summary));
            return lines;
        }

        // The extraction checks every file's size and SHA-256 as it writes it, so the backup is read once.
        var (files, stuck) = Apply(adventures, name, campaign, summary);
        if (stuck is not null)
            lines.Add(stuck);
        lines.Add(string.Create(CultureInfo.InvariantCulture, $"restored {name} from {file}: {files} files"));
        return lines;
    }

    /// <summary>The plan a restore without <c>--yes</c> prints: what would change, and a warning when play since would be lost.</summary>
    /// <param name="name">The campaign's name.</param>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="backup">The backup's path.</param>
    /// <param name="summary">The backup, opened.</param>
    /// <exception cref="DmcException">The backup is damaged.</exception>
    static List<string> Plan(string name, string campaign, string backup, ExportSummary summary)
    {
        var backupFiles = CampaignFiles.Of(summary.ReadCampaign());
        var current = CampaignFiles.Read(campaign);
        int replaced = backupFiles.Count(f => current.TryGetValue(f.Key, out var now) && !now.AsSpan().SequenceEqual(f.Value));
        int removed = current.Keys.Count(p => !backupFiles.ContainsKey(p));
        int added = backupFiles.Keys.Count(p => !current.ContainsKey(p));
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"restore {name} from {Path.GetFileName(backup)}: {replaced} files replaced, {removed} removed, {added} added"),
        };
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
    /// <param name="summary">The backup, opened.</param>
    /// <returns>How many files were restored, and a line to print when the campaign renamed aside could not be deleted afterwards (else null).</returns>
    /// <exception cref="DmcException">The restore failed; the campaign is as it was.</exception>
    static (int Files, string? Stuck) Apply(string adventures, string name, string campaign, ExportSummary summary)
    {
        string id = Guid.NewGuid().ToString("N")[..IdLength];
        string staging = Path.Combine(adventures, StagingPrefix + name + "-" + id);
        string aside = Path.Combine(adventures, AsidePrefix + name + "-" + id);
        string backups = BackupFolder.Of(campaign);
        string stagedBackups = BackupFolder.Of(staging);
        bool backupsMoved = false, asideDone = false;
        int files;
        try
        {
            files = CampaignImporter.ExtractCampaign(summary, staging);
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
                // The staging is deleted only once it holds no backups; otherwise the next run's recovery puts them back.
                if (!Directory.Exists(stagedBackups))
                    CampaignImporter.DeleteStaging(staging);
            }
            catch (Exception inner) when (inner is IOException or UnauthorizedAccessException)
            {
                problem = $"; putting it back also failed ({Message(inner)}): run the restore again to recover it";
            }
            string message = e is DmcException d ? d.Message : Message(e);
            throw new DmcException($"could not restore {name}: {message}" + (problem ?? "; the campaign is as it was"));
        }

        Hook?.Invoke("in-place");
        BackupFolder.ClearMarks(campaign);
        return (files, CampaignImporter.DeleteStaging(aside) is null ? null : $"could not delete adventures/{Path.GetFileName(aside)}; it is safe to delete");
    }

    /// <summary>
    /// Puts back what an interrupted restore of this campaign left, before anything else reads the campaign. Each restore's
    /// staging and aside folders share an id, so the folders say how far it got:
    /// <list type="bullet">
    /// <item>an aside with no campaign folder: it stopped before the rename into place, so the aside is the campaign and goes back;</item>
    /// <item>an aside, a campaign folder and no staging of the same id: it finished the rename into place, so its marks go and the aside is deleted;</item>
    /// <item>an aside, a campaign folder and a staging of the same id: something made a new campaign folder after the rename aside, so
    /// nothing is touched and the player is asked which to keep;</item>
    /// <item>a staging: its <c>backups/</c>, if it has one, goes back into the campaign's <c>backups/</c> (file by file when that
    /// folder exists again), and the staging is deleted only when it holds none of them.</item>
    /// </list>
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's name.</param>
    /// <returns>A line saying what was recovered and anything kept, or null when there was nothing to recover.</returns>
    /// <exception cref="DmcException">The campaign could not be put back, or the folders left do not say which one is the campaign.</exception>
    internal static string? Recover(string root, string name)
    {
        string adventures = Path.Combine(root, "adventures");
        if (!Directory.Exists(adventures))
            return null;
        string campaign = Path.Combine(adventures, name);
        var asides = Leftovers(adventures, AsidePrefix, name);
        var stagings = Leftovers(adventures, StagingPrefix, name);
        if (asides.Count == 0 && stagings.Count == 0)
            return null;

        var kept = new List<string>();
        try
        {
            foreach (string aside in asides)
            {
                string staging = Path.Combine(adventures, StagingPrefix + name + "-" + aside[^IdLength..]);
                if (!Directory.Exists(campaign))
                {
                    Directory.Move(aside, campaign);
                }
                else if (!Directory.Exists(staging))
                {
                    BackupFolder.ClearMarks(campaign);
                    if (CampaignImporter.DeleteStaging(aside) is not null)
                        kept.Add($"adventures/{Path.GetFileName(aside)} could not be deleted; it is safe to delete");
                }
                else
                {
                    throw new DmcException($"an interrupted restore left {name} as adventures/{Path.GetFileName(aside)}, and adventures/{name} "
                        + "exists too: keep the one to play, delete the other, and run the command again");
                }
            }
            foreach (string staging in stagings)
            {
                string staged = BackupFolder.Of(staging);
                if (Directory.Exists(staged) && Directory.Exists(campaign))
                    PutBack(staged, BackupFolder.Of(campaign));
                if (Directory.Exists(staged))
                    kept.Add($"adventures/{Path.GetFileName(staging)}/{BackupFolder.FolderName}/ still holds backups that could not go back into "
                        + $"adventures/{name}/{BackupFolder.FolderName}/: move them by hand");
                else if (CampaignImporter.DeleteStaging(staging) is not null)
                    kept.Add($"adventures/{Path.GetFileName(staging)} could not be deleted; it is safe to delete");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"could not recover {name} from an interrupted restore: {Message(e)}");
        }
        return $"recovered {name} from an interrupted restore" + string.Concat(kept.Select(k => "; " + k));
    }

    /// <summary>
    /// Moves a staged <c>backups/</c> folder back into the campaign: whole when the campaign has none, else entry by entry,
    /// leaving in place any entry whose name the campaign's folder already has. The staged folder is deleted once empty.
    /// </summary>
    /// <param name="staged">The staging folder's <c>backups/</c>.</param>
    /// <param name="backups">The campaign's <c>backups/</c>.</param>
    static void PutBack(string staged, string backups)
    {
        if (!Directory.Exists(backups))
        {
            Directory.Move(staged, backups);
            return;
        }
        foreach (string file in Directory.EnumerateFiles(staged))
        {
            string to = Path.Combine(backups, Path.GetFileName(file));
            if (!File.Exists(to) && !Directory.Exists(to))
                File.Move(file, to);
        }
        foreach (string folder in Directory.EnumerateDirectories(staged))
        {
            string to = Path.Combine(backups, Path.GetFileName(folder));
            if (!File.Exists(to) && !Directory.Exists(to))
                Directory.Move(folder, to);
        }
        if (!Directory.EnumerateFileSystemEntries(staged).Any())
            Directory.Delete(staged);
    }

    /// <summary>The folders of <c>adventures/</c> named <c>&lt;prefix&gt;&lt;name&gt;-&lt;8 hex digits&gt;</c>.</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="prefix">The staging or aside prefix.</param>
    /// <param name="name">The campaign's name.</param>
    static List<string> Leftovers(string adventures, string prefix, string name)
    {
        string start = prefix + name + "-";
        return [.. new DirectoryInfo(adventures).EnumerateDirectories()
            .Where(d => d.Name.StartsWith(start, StringComparison.Ordinal) && d.Name.Length == start.Length + IdLength
                && d.Name[start.Length..].All(char.IsAsciiHexDigitLower))
            .Select(d => d.FullName)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>An exception's message as one clause.</summary>
    /// <param name="e">The exception.</param>
    static string Message(Exception e) => e.Message.TrimEnd('.', '\r', '\n', ' ');
}
