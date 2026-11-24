using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Dmc.SlipTest;

/// <summary>A fork point found in its log (FR-002).</summary>
/// <param name="Point">The point.</param>
/// <param name="Log">The play log that holds it.</param>
/// <param name="Index">The fork message's index in the log.</param>
/// <param name="Text">The message as the player typed it.</param>
/// <param name="Time">When the player wrote it.</param>
internal sealed record ResolvedPoint(ForkPoint Point, LogFile Log, int Index, string Text, DateTimeOffset Time);

/// <summary>One row of <c>runs.tsv</c> (contract).</summary>
/// <param name="Point">The point id, or <c>base</c> for a fresh-mode base session.</param>
/// <param name="Arm">The arm.</param>
/// <param name="Run">The run number, from 1; 0 for a base session.</param>
/// <param name="Session">The session id of the log the run wrote.</param>
/// <param name="Model">The model passed to Claude Code, or "".</param>
/// <param name="Effort">The effort passed, or "".</param>
/// <param name="Permission">The permission mode passed, or "".</param>
/// <param name="Claude">Claude Code's version.</param>
/// <param name="Kind">The original session's kind, or "".</param>
/// <param name="Result">The tokens and cost.</param>
/// <param name="Seconds">How long the run took.</param>
internal sealed record RunRow(string Point, string Arm, int Run, string Session, string Model, string Effort, string Permission,
    string Claude, string Kind, ClaudeResult Result, double Seconds)
{
    /// <summary>The header of <c>runs.tsv</c>.</summary>
    public const string Header = "point\tarm\trun\tsession\tmodel\teffort\tpermission\tclaude\tkind\tinput\toutput\tcache_read\tcache_write\tcost_usd\tseconds";

    /// <summary>The row as a line of <c>runs.tsv</c>.</summary>
    public string Line() => string.Join('\t', Point, Arm, Run.ToString(CultureInfo.InvariantCulture), Session, Model, Effort, Permission, Claude, Kind,
        Result.Input.ToString(CultureInfo.InvariantCulture), Result.Output.ToString(CultureInfo.InvariantCulture),
        Result.CacheRead.ToString(CultureInfo.InvariantCulture), Result.CacheWrite.ToString(CultureInfo.InvariantCulture),
        Result.CostUsd.ToString("0.######", CultureInfo.InvariantCulture), Seconds.ToString("0.0", CultureInfo.InvariantCulture));

    /// <summary>Reads every row of a work folder's <c>runs.tsv</c>; none when it does not exist.</summary>
    /// <param name="work">The work folder.</param>
    /// <exception cref="SlipTestException">A row is malformed.</exception>
    public static List<RunRow> ReadAll(string work)
    {
        string path = Path.Combine(work, "runs.tsv");
        var rows = new List<RunRow>();
        if (!File.Exists(path))
            return rows;
        int n = 0;
        foreach (string line in File.ReadLines(path))
        {
            n++;
            if (n == 1 || line.Trim().Length == 0)
                continue;
            string[] c = line.Split('\t');
            if (c.Length != 15 || !int.TryParse(c[2], CultureInfo.InvariantCulture, out int run))
                throw new SlipTestException($"runs.tsv line {n} is malformed");
            var result = new ClaudeResult(c[3], L(c[9]), L(c[10]), L(c[11]), L(c[12]), D(c[13]), false);
            rows.Add(new RunRow(c[0], c[1], run, c[3], c[4], c[5], c[6], c[7], c[8], result, D(c[14])));
        }
        return rows;

        static long L(string s) => long.TryParse(s, CultureInfo.InvariantCulture, out long v) ? v : 0;
        static double D(string s) => double.TryParse(s, CultureInfo.InvariantCulture, out double v) ? v : 0;
    }
}

/// <summary>
/// <c>prepare</c> and <c>run</c> (FR-002 to FR-006, FR-010): finds the fork points, makes the copy folder, and runs
/// each replay with the copy restored to its moment.
/// </summary>
/// <param name="trial">The trial.</param>
/// <param name="claudeHome">Claude Code's folder (<c>~/.claude</c>).</param>
/// <param name="claude">How to start Claude Code.</param>
/// <param name="output">Where to print one line per run.</param>
internal sealed class Runner(Trial trial, string claudeHome, IClaude? claude, TextWriter output)
{
    /// <summary>The marker <c>prepare</c> leaves in a copy folder it made, so a second <c>prepare</c> may reuse it.</summary>
    public const string Marker = ".slip-test";

    /// <summary>How to start Claude Code; only <c>run</c> needs it.</summary>
    IClaude Launcher => claude ?? throw new SlipTestException("no claude program to run");

    /// <summary>How many point-and-arm pairs a pilot runs (#249: about four runs).</summary>
    public const int PilotPairs = 4;

    /// <summary>Finds every fork point in its log and checks it is a message the player typed.</summary>
    /// <exception cref="SlipTestException">A log or message is missing, or the message is not typed text.</exception>
    public List<ResolvedPoint> Resolve()
    {
        string project = Folders.Project(claudeHome, trial.Folder);
        var logs = new Dictionary<string, LogFile>();
        var resolved = new List<ResolvedPoint>();
        foreach (var point in trial.Points)
        {
            if (!logs.TryGetValue(point.Session, out var log))
                logs[point.Session] = log = LogFile.Read(Path.Combine(project, point.Session + ".jsonl"));
            int index = log.IndexOf(point.Message);
            if (index < 0)
                throw new SlipTestException($"point {point.Id}: no message {point.Message} in session {point.Session}");
            if (!LogFile.IsTyped(log.Records[index]))
                throw new SlipTestException($"point {point.Id}: message {point.Message} is not text the player typed");
            if (LogFile.TimeOf(log.Records[index]) is not { } time)
                throw new SlipTestException($"point {point.Id}: message {point.Message} has no timestamp");
            resolved.Add(new ResolvedPoint(point, log, index, LogFile.MessageText(log.Records[index])!, time));
        }
        return resolved;
    }

    /// <summary><c>prepare</c>: resolves the points, makes the copy folder from the DMC folder, and locks the trial.</summary>
    /// <param name="now">The time the lock records.</param>
    /// <exception cref="SlipTestException">A point does not resolve, or the copy folder holds files <c>prepare</c> did not put there.</exception>
    public void Prepare(DateTimeOffset now)
    {
        Resolve();
        if (Directory.Exists(trial.Copy) && Directory.EnumerateFileSystemEntries(trial.Copy).Any() && !File.Exists(Path.Combine(trial.Copy, Marker)))
            throw new SlipTestException($"the copy folder {trial.Copy} is not empty: name an empty or new folder");
        Folders.CopyTree(trial.Folder, trial.Copy, skip: trial.Work);
        File.WriteAllText(Path.Combine(trial.Copy, Marker), "made by slip-test prepare (spec 024); safe to delete with the folder\n");
        trial.WriteLock(now);
        output.WriteLine($"prepared {trial.Points.Count} points, {trial.Arms.Count} arms, {trial.Runs} runs each: {trial.Points.Count * trial.Arms.Count * trial.Runs} runs");
    }

    /// <summary>
    /// The runs in the order they happen: for each point and run number, every arm in a random order, so the arms run at
    /// the same time (#240 rule 5). The pilot takes the first <see cref="PilotPairs"/> point-and-arm pairs of run 1.
    /// </summary>
    /// <param name="random">The source of the arm order.</param>
    /// <param name="pilot">Whether this is the pilot.</param>
    public List<(ForkPoint Point, Arm Arm, int Run)> Plan(Random random, bool pilot)
    {
        var plan = new List<(ForkPoint, Arm, int)>();
        if (pilot)
        {
            foreach (var point in trial.Points)
                foreach (var arm in trial.Arms)
                    if (plan.Count < PilotPairs)
                        plan.Add((point, arm, 1));
            return plan;
        }
        foreach (var point in trial.Points)
            for (int run = 1; run <= trial.Runs; run++)
                foreach (var arm in trial.Arms.OrderBy(_ => random.Next()))
                    plan.Add((point, arm, run));
        return plan;
    }

    /// <summary><c>run</c>: every planned run that <c>runs.tsv</c> does not hold yet.</summary>
    /// <param name="pilot">Whether to run only the pilot's pairs.</param>
    /// <param name="random">The source of the arm order.</param>
    /// <exception cref="SlipTestException">A locked file changed, or a run fails.</exception>
    public void Run(bool pilot, Random random)
    {
        trial.CheckLock();
        var points = Resolve().ToDictionary(p => p.Point.Id);
        var done = RunRow.ReadAll(trial.Work).Select(r => (r.Point, r.Arm, r.Run)).ToHashSet();
        string version = Launcher.Version();
        List<Backup> backups = trial.Mode == "fork"
            ? FileHistory.Collect(Folders.Project(claudeHome, trial.Folder), Path.Combine(claudeHome, "file-history"))
            : [];
        if (trial.Mode == "fresh")
            foreach (var arm in trial.Arms)
                if (!done.Contains(("base", arm.Name, 0)))
                    Base(arm, points.Values.First(), version);
        foreach (var (point, arm, run) in Plan(random, pilot))
        {
            if (done.Contains((point.Id, arm.Name, run)))
                continue;
            var p = points[point.Id];
            if (trial.Mode == "fork")
                RestoreFork(arm, p, backups);
            else
                RestoreFresh(arm);
            string id = Guid.NewGuid().ToString();
            var (lines, model, effort, permission) = BaseLog(arm, p, id);
            var row = Replay(lines, id, new ClaudeCall(id, model, effort, permission), p.Text, point.Id, arm.Name, run, version, p.Log.Kind());
            Append(row);
            output.WriteLine($"{point.Id} {arm.Name} {run}: {row.Seconds:0}s ${row.Result.CostUsd:0.0000}");
        }
    }

    /// <summary>Fork mode: the copy's <c>adventures/</c> as at the fork message, and its <c>CLAUDE.md</c> the arm's rules.</summary>
    /// <param name="arm">The arm.</param>
    /// <param name="point">The point.</param>
    /// <param name="backups">The file history of the DMC folder.</param>
    void RestoreFork(Arm arm, ResolvedPoint point, List<Backup> backups)
    {
        Folders.Replace(Path.Combine(trial.Folder, "adventures"), Path.Combine(trial.Copy, "adventures"));
        FileHistory.Apply(FileHistory.StateAt(backups, trial.Folder, point.Time), trial.Copy);
        File.Copy(arm.Source, Path.Combine(trial.Copy, "CLAUDE.md"), overwrite: true);
    }

    /// <summary>
    /// Fresh mode: every file any overlay names put back as the DMC folder has it (or removed), then this arm's overlay
    /// laid over the copy and its base session's <c>adventures/</c> restored.
    /// </summary>
    /// <param name="arm">The arm.</param>
    /// <param name="withBase">Whether to restore the base's <c>adventures/</c> (false before the base session itself).</param>
    void RestoreFresh(Arm arm, bool withBase = true)
    {
        foreach (var other in trial.Arms)
            foreach (string file in Directory.EnumerateFiles(other.Source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(other.Source, file);
                string original = Path.Combine(trial.Folder, relative), target = Path.Combine(trial.Copy, relative);
                if (File.Exists(original))
                    File.Copy(original, target, overwrite: true);
                else if (File.Exists(target))
                    File.Delete(target);
            }
        Folders.CopyTree(arm.Source, trial.Copy);
        if (withBase)
            Folders.Replace(Path.Combine(trial.Work, "base", arm.Name + "-adventures"), Path.Combine(trial.Copy, "adventures"));
        else
            Folders.Replace(Path.Combine(trial.Folder, "adventures"), Path.Combine(trial.Copy, "adventures"));
    }

    /// <summary>Fresh mode: one base session for an arm, started with the trial's resume message (FR-005).</summary>
    /// <param name="arm">The arm.</param>
    /// <param name="first">The first point, whose session gives the model, effort and permission mode.</param>
    /// <param name="version">Claude Code's version.</param>
    void Base(Arm arm, ResolvedPoint first, string version)
    {
        RestoreFresh(arm, withBase: false);
        var call = new ClaudeCall(null, first.Log.ModelBefore(first.Index), first.Log.EffortBefore(first.Index) ?? trial.Effort, first.Log.PermissionAt(first.Index));
        var watch = Stopwatch.StartNew();
        ClaudeResult result;
        string raw;
        try
        {
            result = Launcher.Run(trial.Copy, call, trial.Resume, out raw);
            if (result.IsError)
                throw new SlipTestException($"base {arm.Name}: claude reported an error; the next run repeats it");
        }
        catch
        {
            ClearCopyLogs();
            throw;
        }
        string baseDir = Path.Combine(trial.Work, "base");
        Directory.CreateDirectory(baseDir);
        File.WriteAllText(Path.Combine(baseDir, arm.Name + ".json"), raw);
        MoveLog(result.SessionId, Path.Combine(baseDir, arm.Name + ".jsonl"));
        Folders.Replace(Path.Combine(trial.Copy, "adventures"), Path.Combine(baseDir, arm.Name + "-adventures"));
        var row = new RunRow("base", arm.Name, 0, result.SessionId, call.Model ?? "", call.Effort ?? "", call.Permission ?? "", version, "", result, watch.Elapsed.TotalSeconds);
        Append(row);
        output.WriteLine($"base {arm.Name}: {row.Seconds:0}s ${result.CostUsd:0.0000}");
    }

    /// <summary>The lines of the log a run resumes, and the model, effort and permission mode to run it with.</summary>
    /// <param name="arm">The arm.</param>
    /// <param name="point">The point.</param>
    /// <param name="id">The base log's new session id.</param>
    (IReadOnlyList<string> Lines, string? Model, string? Effort, string? Permission) BaseLog(Arm arm, ResolvedPoint point, string id)
    {
        string? model = point.Log.ModelBefore(point.Index), effort = point.Log.EffortBefore(point.Index) ?? trial.Effort;
        string? permission = point.Log.PermissionAt(point.Index);
        if (trial.Mode == "fork")
        {
            var rewrite = LogRewriter.Rewrite(point.Log, point.Index, id, trial.Folder, trial.Copy, File.ReadAllText(arm.Source));
            if (rewrite.RulesReplaced == 0)
                throw new SlipTestException($"point {point.Point.Id}: the log records no CLAUDE.md of {trial.Folder}, so the rules cannot be rewritten (research R2)");
            return (rewrite.Lines, model, effort, permission);
        }
        var baseLog = LogFile.Read(Path.Combine(trial.Work, "base", arm.Name + ".jsonl"));
        return (LogRewriter.Rewrite(baseLog, baseLog.Records.Count, id, trial.Copy, trial.Copy, null).Lines, model, effort, permission);
    }

    /// <summary>Writes a base log into the copy folder's project folder under a session id, runs the replay, and keeps its log and output in the work folder.</summary>
    /// <param name="lines">The base log.</param>
    /// <param name="id">The base log's session id.</param>
    /// <param name="call">The call.</param>
    /// <param name="message">The player message.</param>
    /// <param name="point">The point id.</param>
    /// <param name="arm">The arm's name.</param>
    /// <param name="run">The run number.</param>
    /// <param name="version">Claude Code's version.</param>
    /// <param name="kind">The original session's kind, or null.</param>
    RunRow Replay(IReadOnlyList<string> lines, string id, ClaudeCall call, string message, string point, string arm, int run, string version, string? kind)
    {
        string project = Folders.Project(claudeHome, trial.Copy);
        Directory.CreateDirectory(project);
        string basePath = Path.Combine(project, id + ".jsonl");
        Folders.GuardWrite(basePath, trial.Folder, claudeHome);
        File.WriteAllLines(basePath, lines, new UTF8Encoding(false));
        var watch = Stopwatch.StartNew();
        ClaudeResult result;
        string raw;
        try
        {
            result = Launcher.Run(trial.Copy, call, message, out raw);
            if (result.IsError)
                throw new SlipTestException($"{point} {arm} {run}: claude reported an error; the run is not recorded, and the next run repeats it");
        }
        catch
        {
            ClearCopyLogs();
            throw;
        }
        finally
        {
            File.Delete(basePath);
            string history = Path.Combine(claudeHome, "file-history", id);
            if (Directory.Exists(history))
                Directory.Delete(history, recursive: true);
        }
        string runs = Path.Combine(trial.Work, "runs", arm);
        Directory.CreateDirectory(runs);
        string stem = Path.Combine(runs, $"{point}-{run}");
        File.WriteAllText(stem + ".json", raw);
        MoveLog(result.SessionId, stem + ".jsonl");
        return new RunRow(point, arm, run, result.SessionId, call.Model ?? "", call.Effort ?? "", call.Permission ?? "", version, kind ?? "", result, watch.Elapsed.TotalSeconds);
    }

    /// <summary>Moves a log the copy folder's sessions wrote into the work folder, and deletes its file history (FR-010).</summary>
    /// <param name="session">The session id.</param>
    /// <param name="target">Where to keep the log.</param>
    /// <exception cref="SlipTestException">Claude Code wrote no log for the session.</exception>
    void MoveLog(string session, string target)
    {
        string log = Path.Combine(Folders.Project(claudeHome, trial.Copy), session + ".jsonl");
        if (!File.Exists(log))
            throw new SlipTestException($"claude wrote no log for session {session} in {Path.GetDirectoryName(log)}");
        File.Copy(log, target, overwrite: true);
        File.Delete(log);
        string history = Path.Combine(claudeHome, "file-history", session);
        if (Directory.Exists(history))
            Directory.Delete(history, recursive: true);
    }

    /// <summary>
    /// After a failed run: deletes every log in the copy folder's project folder, and each one's file history, so no
    /// fork of a failed run is left behind (FR-010). Only the runner starts sessions in the copy folder.
    /// </summary>
    void ClearCopyLogs()
    {
        string project = Folders.Project(claudeHome, trial.Copy);
        if (!Directory.Exists(project))
            return;
        foreach (string log in Directory.EnumerateFiles(project, "*.jsonl"))
        {
            File.Delete(log);
            string history = Path.Combine(claudeHome, "file-history", Path.GetFileNameWithoutExtension(log));
            if (Directory.Exists(history))
                Directory.Delete(history, recursive: true);
        }
    }

    /// <summary>Appends a row to <c>runs.tsv</c>, writing the header first when the file is new.</summary>
    /// <param name="row">The row.</param>
    void Append(RunRow row)
    {
        string path = Path.Combine(trial.Work, "runs.tsv");
        if (!File.Exists(path))
            File.WriteAllText(path, RunRow.Header + "\n");
        File.AppendAllText(path, row.Line() + "\n");
    }
}
