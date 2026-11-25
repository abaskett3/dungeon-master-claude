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

/// <summary>One edit of a file through Claude Code's tools (<c>Edit</c>, <c>Write</c>, <c>MultiEdit</c>, <c>NotebookEdit</c>), from a log.</summary>
/// <param name="File">The file's full path.</param>
/// <param name="Time">The time of the assistant record that holds the call.</param>
internal sealed record FileEdit(string File, DateTimeOffset Time);

/// <summary>
/// Restores the campaign files of a copy folder to their state at a moment of play (FR-004), from the
/// <c>file-history-snapshot</c> records of every log of the DMC folder and the backups under
/// <c>~/.claude/file-history/&lt;session id&gt;/</c>.
/// </summary>
internal static class FileHistory
{
    /// <summary>
    /// Every backup the logs of a project folder record, each once, and each session's first backup of a file
    /// (<c>@v1</c>, the file before that session's first edit of it), which the logs do not record: it is found in
    /// the session's file history folder under the name stem any log gives that file, and timed just before the
    /// session's first edit of it.
    /// </summary>
    /// <param name="project">The DMC folder's Claude Code project folder.</param>
    /// <param name="historyRoot">Claude Code's <c>file-history</c> folder.</param>
    public static List<Backup> Collect(string project, string historyRoot)
    {
        var seen = new HashSet<(string, string?, DateTimeOffset)>();
        var backups = new List<Backup>();
        if (!Directory.Exists(project))
            return backups;
        var stems = new Dictionary<string, string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var sessions = new List<(string Session, List<FileEdit> Edits, HashSet<string> Recorded)>();
        foreach (string path in Directory.EnumerateFiles(project, "*.jsonl"))
        {
            var log = LogFile.Read(path);
            string session = Path.GetFileNameWithoutExtension(path);
            string? cwd = log.Cwd();
            var recorded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in log.Records.Where(r => LogFile.TypeOf(r) == "file-history-snapshot"))
            {
                if ((record["snapshot"] as JsonObject)?["trackedFileBackups"] is not JsonObject tracked)
                    continue;
                foreach (var (name, value) in tracked)
                {
                    if (value is not JsonObject entry || BackupTime(entry) is not { } time)
                        continue;
                    string file = FullPath(name, cwd);
                    string? backupName = LogFile.Text(entry, "backupFileName");
                    string? backup = backupName is null ? null : Path.Combine(historyRoot, session, backupName);
                    if (backupName is not null)
                    {
                        recorded.Add(backupName);
                        if (backupName.LastIndexOf('@') is > 0 and int at)
                            stems.TryAdd(file, backupName[..at]);
                    }
                    if (seen.Add((file, backup, time)))
                        backups.Add(new Backup(file, backup, time));
                }
            }
            sessions.Add((session, EditsOf(log), recorded));
        }
        foreach (var (session, edits, recorded) in sessions)
            foreach (var first in edits.GroupBy(e => e.File, stems.Comparer).Select(g => g.MinBy(e => e.Time)!))
            {
                if (!stems.TryGetValue(first.File, out string? stem) || recorded.Contains(stem + "@v1"))
                    continue;
                string v1 = Path.Combine(historyRoot, session, stem + "@v1");
                if (File.Exists(v1) && seen.Add((first.File, v1, first.Time.AddTicks(-1))))
                    backups.Add(new Backup(first.File, v1, first.Time.AddTicks(-1)));
            }
        return backups;
    }

    /// <summary>The tools whose calls change a file, so a backup taken after one no longer shows the file before it.</summary>
    static readonly HashSet<string> EditTools = ["Edit", "Write", "MultiEdit", "NotebookEdit"];

    /// <summary>Every edit of a file through Claude Code's tools that the logs of a project folder record.</summary>
    /// <param name="project">The DMC folder's Claude Code project folder.</param>
    public static List<FileEdit> CollectEdits(string project) =>
        Directory.Exists(project)
            ? Directory.EnumerateFiles(project, "*.jsonl").SelectMany(path => EditsOf(LogFile.Read(path))).ToList()
            : [];

    /// <summary>The edits one log records: each call of an edit tool, with its file and the time of its record.</summary>
    /// <param name="log">A session's log.</param>
    static List<FileEdit> EditsOf(LogFile log)
    {
        var edits = new List<FileEdit>();
        string? cwd = log.Cwd();
        foreach (var record in log.Records.Where(r => LogFile.TypeOf(r) == "assistant"))
        {
            if (LogFile.TimeOf(record) is not { } time || (record["message"] as JsonObject)?["content"] is not JsonArray content)
                continue;
            foreach (var block in content.OfType<JsonObject>())
            {
                if (LogFile.Text(block, "type") != "tool_use" || !EditTools.Contains(LogFile.Text(block, "name") ?? ""))
                    continue;
                var input = block["input"] as JsonObject;
                if ((LogFile.Text(input, "file_path") ?? LogFile.Text(input, "notebook_path")) is { Length: > 0 } name)
                    edits.Add(new FileEdit(FullPath(name, cwd), time));
            }
        }
        return edits;
    }

    /// <summary>A path from a log as a full path: as it is when rooted, else under the session's <c>cwd</c>.</summary>
    /// <param name="name">The path as the log has it.</param>
    /// <param name="cwd">The session's working folder, or null.</param>
    static string FullPath(string name, string? cwd) =>
        Path.IsPathRooted(name) || cwd is null ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(cwd, name));

    /// <summary>A backup entry's <c>backupTime</c>: an ISO 8601 text, or milliseconds since 1970; null when it is neither.</summary>
    /// <param name="entry">An entry of <c>trackedFileBackups</c>.</param>
    static DateTimeOffset? BackupTime(JsonObject entry) =>
        entry["backupTime"] is JsonValue v && v.TryGetValue(out double ms)
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)ms)
            : LogFile.ParseTime(LogFile.Text(entry, "backupTime"));

    /// <summary>
    /// What changes a copy needs to show each file under <c>adventures/</c> as it was at a time. A backup holds the file
    /// as it was at its <c>backupTime</c>: Claude Code takes one at the first message after the file changed, and one
    /// just before a session's first edit of it. So for each file with a backup after that time, the earliest such
    /// backup, when no edit of the file falls between the time and that backup; else the latest backup at or before
    /// the time, when no edit falls between that backup and the time (null as a backup: the file did not exist then).
    /// A file with no backup after the time was not changed through Claude Code's tools since, so the copy of the DMC
    /// folder already holds it.
    /// </summary>
    /// <param name="backups">Every backup (<see cref="Collect"/>).</param>
    /// <param name="folder">The DMC folder's full path.</param>
    /// <param name="at">The fork message's time.</param>
    /// <param name="edits">Every edit (<see cref="CollectEdits"/>); null to trust the earliest backup after the time.</param>
    /// <returns>Each file's path relative to the DMC folder, with the backup that holds its content then.</returns>
    /// <exception cref="SlipTestException">No backup holds a file as it was at the time.</exception>
    public static Dictionary<string, string?> StateAt(IEnumerable<Backup> backups, string folder, DateTimeOffset at, IReadOnlyList<FileEdit>? edits = null)
    {
        string adventures = Path.Combine(folder, "adventures");
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var state = new Dictionary<string, string?>(comparer);
        var all = backups.Where(b => Folders.IsInside(b.File, adventures)).GroupBy(b => b.File, comparer);
        foreach (var group in all)
        {
            var after = group.Where(b => b.Time > at).OrderBy(b => b.Time).FirstOrDefault();
            if (after is null)
                continue;
            bool EditedBetween(DateTimeOffset from, DateTimeOffset to) =>
                edits is not null && edits.Any(e => comparer.Equals(e.File, group.Key) && e.Time > from && e.Time < to);
            var chosen = after;
            if (EditedBetween(at, after.Time))
            {
                var before = group.Where(b => b.Time <= at).OrderBy(b => b.Time).LastOrDefault();
                if (before is null && !EditedBetween(DateTimeOffset.MinValue, at.AddTicks(1)))
                {
                    // Claude Code records no backup of a file its Write creates: one an edit made after the time, with
                    // no backup or edit before it, did not exist then (a file that existed has a backup before its first edit).
                    state[Path.GetRelativePath(folder, group.Key)] = null;
                    continue;
                }
                if (before is null || EditedBetween(before.Time, at.AddTicks(1)))
                    throw new SlipTestException($"no backup holds {Path.GetRelativePath(folder, group.Key)} as it was at {at:O}: leave this fork point out");
                chosen = before;
            }
            state[Path.GetRelativePath(folder, chosen.File)] = chosen.BackupFile;
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
