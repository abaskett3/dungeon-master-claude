using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>What <c>verify</c> was asked.</summary>
/// <param name="Backup">The backup to compare with; null for the marked backup, else the latest.</param>
/// <param name="Accounting">The accounting file; null for the backup's companion.</param>
/// <param name="List">True for <c>--list</c>: report what is still to account and write nothing.</param>
/// <param name="All">True for <c>--all</c>: print every unaccounted unit, not the first 40.</param>
internal sealed record VerifyOptions(string? Backup, string? Accounting, bool List, bool All);

/// <summary>What <c>verify</c> found.</summary>
/// <param name="Lines">The lines to print, in order.</param>
/// <param name="Failure">Null when the run passed or was a list; else the reason the migration failed verification.</param>
internal sealed record VerifyOutcome(IReadOnlyList<string> Lines, string? Failure);

/// <summary>
/// <c>dmc campaign verify</c> (contracts/campaign-commands.md § verify; research R5): proves a migration lost no content. It
/// passes only when the backup is whole; every content unit of the backup's campaign files is found in the migrated
/// campaign or accounted for (reworded with an anchor that is there, or dropped under a class a pending migration
/// declares); no file was skipped; and every check the pending migrations name passes. On a pass it records the kit's
/// standard in <c>.standard</c>, writes the verified record beside the backup and removes every <c>.migrating</c> mark of
/// the campaign. The campaign files are compared without the top-level <c>backups/</c> folder and <c>.standard</c>.
/// </summary>
internal static partial class CampaignVerifier
{
    /// <summary>How many unaccounted units are printed without <c>--all</c>.</summary>
    const int Cap = 40;

    /// <summary>How many characters of a unit are printed.</summary>
    const int Excerpt = 60;

    /// <summary>The shortest unit that counts as matched when it is contained in a longer line.</summary>
    const int ContainedFloor = 20;

    /// <summary>The extensions of the files whose lines are content units; any other file is one unit, its path.</summary>
    static readonly string[] TextExtensions = [".md", ".txt"];

    /// <summary>A run of whitespace.</summary>
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace { get; }

    /// <summary>An old unit with the first file it was found in.</summary>
    /// <param name="Text">The normalised text.</param>
    /// <param name="Id">Its id.</param>
    /// <param name="File">The first campaign file that holds it.</param>
    sealed record Unit(string Text, string Id, string File);

    /// <summary>Runs a verification.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="options">What was asked.</param>
    /// <returns>The lines to print and, for a failed verification, the reason.</returns>
    /// <exception cref="DmcException">Verification cannot run: no such campaign, the campaign is ahead of the kit, the backup is missing, damaged or another campaign's, no migration is pending, the accounting file has a bad line, or two different units share an id.</exception>
    public static VerifyOutcome Run(string root, string name, VerifyOptions options)
    {
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        var set = MigrationSet.Read(root);
        int current = StandardRecord.Read(campaign, name);
        if (current > set.Kit)
            throw new DmcException(string.Create(CultureInfo.InvariantCulture, $"{name} is ahead (standard {current}, kit {set.Kit}): update dmc"));

        string backup = BackupFolder.Resolve(campaign, name, options.Backup);
        string backupName = Path.GetFileName(backup);
        var old = OpenBackup(backup, name, out int from);
        var pending = set.Pending(from);
        if (pending.Count == 0)
            throw new DmcException(string.Create(CultureInfo.InvariantCulture,
                $"no migration is pending: {backupName} is at standard {from} and the kit at {set.Kit}"));

        string accountingPath = options.Accounting ?? BackupFolder.Companion(backup, BackupFolder.AccountingSuffix);
        string label = options.Accounting ?? $"{BackupFolder.FolderName}/{backupName}{BackupFolder.AccountingSuffix}";
        var accounting = AccountingFile.Read(accountingPath, label);
        var currentFiles = CampaignFiles.Read(campaign);
        var newText = currentFiles.ToDictionary(f => f.Key, f => Text(f.Value), StringComparer.Ordinal);
        var drops = MigrationSet.Drops(pending);

        // The migrated campaign: every unit, and every line long enough to contain an old unit.
        var newUnits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, bytes) in currentFiles)
        {
            foreach (string unit in UnitsOf(path, bytes))
                newUnits.Add(unit);
        }
        var longLines = newUnits.Where(u => u.Length > ContainedFloor).ToList();

        var units = OldUnits(old);
        var problems = new List<string>();
        int matched = 0, reworded = 0, dropped = 0;
        foreach (var unit in units)
        {
            if (newUnits.Contains(unit.Text)
                || (unit.Text.Length >= ContainedFloor && longLines.Any(l => l.Length > unit.Text.Length && l.Contains(unit.Text, StringComparison.Ordinal))))
            {
                matched++;
                continue;
            }
            string line = $"unaccounted: {unit.Id} {unit.File} {Cut(unit.Text)}";
            if (!accounting.Entries.TryGetValue(unit.Id, out var entry))
            {
                problems.Add(line);
            }
            else if (entry.Disposition == Disposition.Reworded)
            {
                if (AnchorFound(entry.Detail, newText.Values, newUnits))
                    reworded++;
                else
                    problems.Add($"{line} (anchor not found: {Cut(entry.Detail)})");
            }
            else if (drops.Contains(entry.Class!))
            {
                dropped++;
            }
            else
            {
                problems.Add($"{line} (drop class {entry.Class} is not declared by the pending migrations)");
            }
        }

        var oldText = old.ToDictionary(f => f.Key, f => Text(f.Value), StringComparer.Ordinal);
        var input = new CheckInput(oldText, newText, accounting);
        var checkLines = MigrationSet.Checks(pending).SelectMany(c => MigrationChecks.Run(c, input)).ToList();

        var lines = new List<string>();
        lines.AddRange(options.All ? problems : problems.Take(Cap));
        if (!options.All && problems.Count > Cap)
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{problems.Count - Cap} more"));
        foreach (var skip in accounting.Skipped)
            lines.Add(options.List ? $"skipped: {skip.File}" : $"skipped: {skip.File} {skip.Reason}");
        lines.AddRange(checkLines.Select(l => "check " + l));

        if (options.List)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{problems.Count} to account in {label}"));
            return new VerifyOutcome(lines, null);
        }

        var reasons = new List<string>();
        if (problems.Count > 0)
            reasons.Add(Count(problems.Count, "unit") + " unaccounted");
        if (accounting.Skipped.Count > 0)
            reasons.Add(Count(accounting.Skipped.Count, "file") + " skipped");
        if (checkLines.Count > 0)
            reasons.Add(Count(checkLines.Count, "check failure", "check failures"));
        if (reasons.Count > 0)
            return new VerifyOutcome(lines, string.Join(", ", reasons) + $"; {backupName} is untouched");

        StandardRecord.Write(campaign, set.Kit);
        WriteVerified(BackupFolder.Companion(backup, BackupFolder.VerifiedSuffix), currentFiles);
        // Every mark, not only this backup's: --backup may name another backup than the marked one, and the migration is done.
        BackupFolder.ClearMarks(campaign);
        lines.Add(string.Create(CultureInfo.InvariantCulture,
            $"verified {name}: {units.Count} units, {matched} matched, {reworded} accounted, {dropped} dropped; standard {set.Kit}"));
        return new VerifyOutcome(lines, null);
    }

    /// <summary>Opens a backup, checks it whole and returns its campaign files and the standard it records.</summary>
    /// <param name="backup">The backup's path.</param>
    /// <param name="name">The campaign it must belong to.</param>
    /// <param name="standard">The backup's standard: its <c>.standard</c>, 1 without one.</param>
    /// <exception cref="DmcException">The backup is damaged, another campaign's, or holds an unreadable record.</exception>
    internal static SortedDictionary<string, byte[]> OpenBackup(string backup, string name, out int standard)
    {
        var all = BackupFolder.Open(backup, name).ReadCampaign();
        standard = all.TryGetValue(StandardRecord.FileName, out var record)
            ? StandardRecord.Parse(record) ?? throw new DmcException($"{Path.GetFileName(backup)} holds an unreadable {StandardRecord.FileName}")
            : StandardRecord.Baseline;
        return CampaignFiles.Of(all);
    }

    /// <summary>The distinct units of the backup's files, each with the first file (in path order) that holds it, after checking no two share an id.</summary>
    /// <param name="old">The backup's campaign files.</param>
    /// <exception cref="DmcException">Two different units share an id.</exception>
    static List<Unit> OldUnits(SortedDictionary<string, byte[]> old)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var units = new List<Unit>();
        foreach (var (path, bytes) in old)
        {
            foreach (string text in UnitsOf(path, bytes))
            {
                if (!seen.Add(text))
                    continue;
                string id = ContentUnits.Id(text);
                if (ids.TryGetValue(id, out string? other))
                    throw new DmcException($"cannot verify: two different units share the id {id} (\"{Cut(other)}\" and \"{Cut(text)}\")");
                ids[id] = text;
                units.Add(new Unit(text, id, path));
            }
        }
        return units;
    }

    /// <summary>The units of one campaign file: its normalised lines for a text file, else one unit naming the file.</summary>
    /// <param name="path">The file's campaign path.</param>
    /// <param name="bytes">Its bytes.</param>
    static List<string> UnitsOf(string path, byte[] bytes) =>
        TextExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase))
            ? ContentUnits.Split(Text(bytes))
            : ["file " + path];

    /// <summary>A file's bytes as text, without a byte-order mark.</summary>
    /// <param name="bytes">The bytes.</param>
    static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimStart('﻿');

    /// <summary>True when a rewording's anchor occurs in the migrated campaign: in a file's text, or in a unit once whitespace is collapsed.</summary>
    /// <param name="anchor">The anchor.</param>
    /// <param name="texts">The migrated campaign's files as text, decoded once for the whole run.</param>
    /// <param name="units">The migrated campaign's units.</param>
    static bool AnchorFound(string anchor, IEnumerable<string> texts, HashSet<string> units)
    {
        string collapsed = Whitespace.Replace(anchor, " ").Trim();
        return units.Any(u => u.Contains(collapsed, StringComparison.Ordinal))
            || texts.Any(t => t.Contains(anchor, StringComparison.Ordinal));
    }

    /// <summary>Writes the verified record: one <c>path\tsha256</c> line per campaign file.</summary>
    /// <param name="path">The record's path.</param>
    /// <param name="files">The migrated campaign's files.</param>
    static void WriteVerified(string path, SortedDictionary<string, byte[]> files)
    {
        var text = new StringBuilder();
        foreach (var (file, bytes) in files)
            text.Append(file).Append('\t').Append(CampaignFiles.Hash(bytes)).Append('\n');
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
    }

    /// <summary>Reads a verified record.</summary>
    /// <param name="path">The record's path.</param>
    /// <returns>Each campaign file's SHA-256 by path, or null when there is no record.</returns>
    internal static Dictionary<string, string>? ReadVerified(string path)
    {
        if (!File.Exists(path))
            return null;
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllText(path, Encoding.UTF8).Split('\n'))
        {
            string[] fields = line.TrimEnd('\r').Split('\t');
            if (fields.Length == 2)
                files[fields[0]] = fields[1];
        }
        return files;
    }

    /// <summary>The first 60 characters of a text.</summary>
    /// <param name="text">The text.</param>
    static string Cut(string text) => text.Length <= Excerpt ? text : text[..Excerpt];

    /// <summary>A count with its noun.</summary>
    /// <param name="n">The count.</param>
    /// <param name="one">The noun for one.</param>
    /// <param name="many">The noun for more than one; <paramref name="one"/> plus <c>s</c> when null.</param>
    static string Count(int n, string one, string? many = null) =>
        string.Create(CultureInfo.InvariantCulture, $"{n} {(n == 1 ? one : many ?? one + "s")}");
}
