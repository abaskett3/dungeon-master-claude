using System.Globalization;

namespace Dmc.Campaign;

/// <summary>What <c>compare</c> found.</summary>
/// <param name="Lines">The lines to print, in order.</param>
/// <param name="Failure">Null when every file matches; else how many files differ, and from which file.</param>
internal sealed record CompareOutcome(IReadOnlyList<string> Lines, string? Failure);

/// <summary>
/// <c>dmc campaign compare</c> (spec 033, issue #189; contracts/campaign-compare.md): checks a campaign against any export of
/// it, a backup, a closing backup, the file it was imported from or any other copy, by path and content, and writes nothing,
/// pass or fail. The comparison is <see cref="CampaignVerifier.ClosedDifferences"/>, the one the closed campaign's
/// <c>verify</c> and the sequel use, so the three cannot disagree. Unlike <c>verify</c> it never checks or records a
/// migration.
/// </summary>
internal static class CampaignComparer
{
    /// <summary>How many difference lines are printed without <c>--all</c>, as <c>verify</c> prints.</summary>
    const int Cap = 40;

    /// <summary>Compares a campaign with an export of it.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="with">What <c>--with</c> named (a file name in <c>backups/</c> or a path); null for the default.</param>
    /// <param name="all">True for <c>--all</c>: print every difference, not the first 40.</param>
    /// <returns>The lines to print and, when any file differs, the failure.</returns>
    /// <exception cref="DmcException">No such campaign; no file to compare with; or the export is missing or damaged.</exception>
    public static CompareOutcome Run(string root, string name, string? with, bool all)
    {
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        string file = with is not null ? BackupFolder.Resolve(campaign, name, with) : Default(root, campaign, name);
        string fileName = Path.GetFileName(file);

        // Opened without the name check, as the closed verify opens it: an import made --as another name is compared with
        // the file it came from, which names the campaign it was taken of. The note says so; the comparison is the check.
        var export = ExportSummary.Open(file);
        var lines = new List<string>();
        if (export.Manifest.Campaign != name)
            lines.Add($"note: {fileName} is an export of {export.Manifest.Campaign}");

        var problems = CampaignVerifier.ClosedDifferences(campaign, export.ReadCampaign(), out int files);
        if (problems.Count == 0)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{name} matches {fileName}: {files} files"));
            return new CompareOutcome(lines, null);
        }

        lines.AddRange(all ? problems : problems.Take(Cap));
        if (!all && problems.Count > Cap)
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{problems.Count - Cap} more"));
        string differ = problems.Count == 1 ? "file differs" : "files differ";
        return new CompareOutcome(lines, string.Create(CultureInfo.InvariantCulture, $"{problems.Count} {differ} from {fileName}"));
    }

    /// <summary>
    /// The file compared with when <c>--with</c> names none (spec 033 FR-003): a closed campaign's closing backup, in
    /// <c>backups/</c> or else <c>exports/</c>; an open campaign's latest counted backup, never the marked one of a
    /// migration in progress unless it is the latest.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="campaign">The campaign folder.</param>
    /// <param name="name">The campaign's name.</param>
    /// <returns>The file's full path.</returns>
    /// <exception cref="DmcException">A closed campaign's mark cannot be read or names a file in neither folder; an open campaign has no backup.</exception>
    static string Default(string root, string campaign, string name)
    {
        if (ClosedCampaign.IsClosed(campaign))
            return CampaignVerifier.ClosingBackup(root, campaign, name, "--with");
        var backups = BackupFolder.List(campaign, name);
        return backups.Count > 0
            ? Path.Combine(BackupFolder.Of(campaign), backups[^1].FileName)
            : throw new DmcException($"{name} has no backup: name an export with --with FILE");
    }
}
