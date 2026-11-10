namespace Dmc.Campaign;

/// <summary>What a backup did.</summary>
/// <param name="FileName">The backup's file name in <c>backups/</c>.</param>
/// <param name="Files">How many campaign files it holds.</param>
/// <param name="Removed">How many older backups <c>--prune</c> removed.</param>
/// <param name="NotRemoved">The older backups or companions <c>--prune</c> chose but could not remove, by file name.</param>
internal sealed record BackupResult(string FileName, int Files, int Removed, IReadOnlyList<string> NotRemoved);

/// <summary>
/// <c>dmc campaign backup NAME [--prune oldest|old]</c> (contracts/campaign-commands.md § backup; research R8): refuses while
/// a migration is unfinished; writes the backup with the exporter; checks it whole; marks the migration under way; and only
/// then removes the older backups the player agreed to, each with its companion files. A removal that fails is reported and
/// does not fail the backup.
/// </summary>
internal static class CampaignBackup
{
    /// <summary>Takes a backup.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="prune">What older backups the player agreed to remove.</param>
    /// <param name="now">The moment of the backup; the clock when null.</param>
    /// <returns>What was written and removed.</returns>
    /// <exception cref="DmcException">There is no such campaign, a migration is unfinished, or the backup could not be written or checked. Nothing is removed and no file is left under the backup's name.</exception>
    public static BackupResult Take(string root, string name, PruneMode prune, DateTime? now = null)
    {
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        if (BackupFolder.Marked(campaign, name) is { } marked)
            throw new DmcException($"{name} has an unfinished migration from {marked}: finish it or restore it");

        var earlier = BackupFolder.List(campaign, name);
        var export = CampaignExporter.Backup(root, name, now);
        string fileName = Path.GetFileName(export.Archive);
        try
        {
            var summary = ExportSummary.Open(export.Archive);
            summary.ReadCampaign();
            File.WriteAllText(BackupFolder.Companion(export.Archive, BackupFolder.MigratingSuffix), "");
        }
        catch (Exception e) when (e is DmcException or IOException or UnauthorizedAccessException)
        {
            Remove(export.Archive);
            Remove(BackupFolder.Companion(export.Archive, BackupFolder.MigratingSuffix));
            throw e as DmcException ?? new DmcException($"could not back up {name}: {e.Message.TrimEnd('.', '\r', '\n', ' ')}");
        }

        int removed = 0;
        var stuck = new List<string>();
        foreach (var old in BackupFolder.PruneSelection(earlier, prune))
        {
            string path = Path.Combine(BackupFolder.Of(campaign), old.FileName);
            if (Remove(path))
                removed++;
            else
                stuck.Add(old.FileName);
            foreach (string suffix in BackupFolder.CompanionSuffixes)
            {
                if (!Remove(BackupFolder.Companion(path, suffix)))
                    stuck.Add(old.FileName + suffix);
            }
        }
        return new BackupResult(fileName, export.Files, removed, stuck);
    }

    /// <summary>Deletes a file if it is there.</summary>
    /// <param name="path">The file.</param>
    /// <returns>True when the file is gone (or was never there); false when it could not be deleted.</returns>
    internal static bool Remove(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
