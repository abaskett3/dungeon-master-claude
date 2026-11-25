using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>
/// One backup in Claude Code's file history: the content of a file just before the first edit of a turn
/// (research R4).
/// </summary>
/// <param name="File">The file's full path.</param>
/// <param name="BackupFile">The backup's full path, or null when the file did not exist then.</param>
/// <param name="Time">When the backup was taken.</param>
internal sealed record Backup(string File, string? BackupFile, DateTimeOffset Time);

/// <summary>
/// Restores the campaign files of a copy folder to their state at a moment of play (FR-004), from the
/// <c>file-history-snapshot</c> records of every log of the DMC folder and the backups under
/// <c>~/.claude/file-history/&lt;session id&gt;/</c>.
/// </summary>
internal static class FileHistory
{
    /// <summary>Every backup the logs of a project folder record, each once.</summary>
    /// <param name="project">The DMC folder's Claude Code project folder.</param>
    /// <param name="historyRoot">Claude Code's <c>file-history</c> folder.</param>
    public static List<Backup> Collect(string project, string historyRoot)
    {
        var seen = new HashSet<(string, string?, DateTimeOffset)>();
        var backups = new List<Backup>();
        if (!Directory.Exists(project))
            return backups;
        foreach (string path in Directory.EnumerateFiles(project, "*.jsonl"))
        {
            var log = LogFile.Read(path);
            string session = Path.GetFileNameWithoutExtension(path);
            string? cwd = log.Cwd();
            foreach (var record in log.Records.Where(r => LogFile.TypeOf(r) == "file-history-snapshot"))
            {
                if ((record["snapshot"] as JsonObject)?["trackedFileBackups"] is not JsonObject tracked)
                    continue;
                foreach (var (name, value) in tracked)
                {
                    if (value is not JsonObject entry || BackupTime(entry) is not { } time)
                        continue;
                    string file = Path.IsPathRooted(name) || cwd is null ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(cwd, name));
                    string? backupName = LogFile.Text(entry, "backupFileName");
                    string? backup = backupName is null ? null : Path.Combine(historyRoot, session, backupName);
                    if (seen.Add((file, backup, time)))
                        backups.Add(new Backup(file, backup, time));
                }
            }
        }
        return backups;
    }

    /// <summary>A backup entry's <c>backupTime</c>: an ISO 8601 text, or milliseconds since 1970; null when it is neither.</summary>
    /// <param name="entry">An entry of <c>trackedFileBackups</c>.</param>
    static DateTimeOffset? BackupTime(JsonObject entry) =>
        entry["backupTime"] is JsonValue v && v.TryGetValue(out double ms)
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)ms)
            : LogFile.ParseTime(LogFile.Text(entry, "backupTime"));

    /// <summary>
    /// What changes a copy needs to show each file under <c>adventures/</c> as it was at a time: for each file with a
    /// backup after that time, the earliest such backup (null when the file did not exist then). A file with none was
    /// not changed through Claude Code's tools since, so the copy of the DMC folder already holds it.
    /// </summary>
    /// <param name="backups">Every backup (<see cref="Collect"/>).</param>
    /// <param name="folder">The DMC folder's full path.</param>
    /// <param name="at">The fork message's time.</param>
    /// <returns>Each file's path relative to the DMC folder, with the backup that holds its content then.</returns>
    public static Dictionary<string, string?> StateAt(IEnumerable<Backup> backups, string folder, DateTimeOffset at)
    {
        string adventures = Path.Combine(folder, "adventures");
        var state = new Dictionary<string, string?>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var group in backups.Where(b => b.Time > at && Folders.IsInside(b.File, adventures)).GroupBy(b => b.File, state.Comparer))
        {
            var first = group.OrderBy(b => b.Time).First();
            state[Path.GetRelativePath(folder, first.File)] = first.BackupFile;
        }
        return state;
    }

    /// <summary>Writes the state into the copy folder: each file's backup copied over it, or the file deleted.</summary>
    /// <param name="state">The state (<see cref="StateAt"/>).</param>
    /// <param name="copy">The copy folder.</param>
    /// <exception cref="SlipTestException">A backup the log names is missing.</exception>
    public static void Apply(IReadOnlyDictionary<string, string?> state, string copy)
    {
        foreach (var (relative, backup) in state)
        {
            string target = Path.Combine(copy, relative);
            if (backup is null)
            {
                if (File.Exists(target))
                    File.Delete(target);
                continue;
            }
            if (!File.Exists(backup))
                throw new SlipTestException($"the file history has no backup {backup}: keep Claude Code's file history (#239)");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(backup, target, overwrite: true);
        }
    }
}
