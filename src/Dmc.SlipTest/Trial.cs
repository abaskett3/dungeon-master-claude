using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Dmc.SlipTest;

/// <summary>An arm of a trial: a rules file (fork mode) or an overlay folder (fresh mode).</summary>
/// <param name="Name">The arm's name: letters, digits and <c>-</c>.</param>
/// <param name="Source">The full path of its rules file or overlay folder.</param>
internal sealed record Arm(string Name, string Source);

/// <summary>The pass line: the trial passes when the upper end of the 95% interval of <paramref name="Arm"/>'s rate minus <paramref name="Against"/>'s is below <paramref name="Below"/>.</summary>
/// <param name="Arm">The arm that should slip less.</param>
/// <param name="Against">The arm it is compared with.</param>
/// <param name="Below">The bound the difference's upper end must be below.</param>
internal sealed record PassLine(string Arm, string Against, double Below);

/// <summary>A fork point: a player message in a play log.</summary>
/// <param name="Id">The point's id in <c>points.tsv</c>.</param>
/// <param name="Session">The session id: the log's file name without <c>.jsonl</c>.</param>
/// <param name="Message">The uuid of the player message to replay.</param>
internal sealed record ForkPoint(string Id, string Session, string Message);

/// <summary>A pilot difference the maintainer documented in <c>allow.tsv</c>.</summary>
/// <param name="Check">The check: <c>notice</c>, <c>hook</c> or <c>thinking</c>.</param>
/// <param name="Name">What differs: an attachment type, a meta message's first line, a hook's type, or <c>not recorded</c>.</param>
/// <param name="Reason">Why it cannot reach the reply.</param>
internal sealed record Allowed(string Check, string Name, string Reason);

/// <summary>
/// A trial: the contents of a work folder's <c>trial.json</c>, <c>points.tsv</c>, <c>key.tsv</c> and
/// <c>allow.tsv</c> (contract: <c>specs/024-slip-test-runner/contracts/slip-test.md</c>).
/// </summary>
internal sealed partial class Trial
{
    /// <summary>An arm or point name: letters, digits, <c>-</c> and <c>_</c>.</summary>
    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex NamePattern();

    /// <summary>The files <c>prepare</c> locks, so the runs, the pass line and the key are fixed before the run (#240 rules 1 and 2).</summary>
    public static readonly string[] LockedFiles = ["trial.json", "points.tsv", "key.tsv"];

    /// <summary>The work folder.</summary>
    public required string Work { get; init; }

    /// <summary><c>fork</c> or <c>fresh</c>.</summary>
    public required string Mode { get; init; }

    /// <summary>The DMC folder that played, full path.</summary>
    public required string Folder { get; init; }

    /// <summary>The copy folder the replays run in, full path.</summary>
    public required string Copy { get; init; }

    /// <summary>Runs per point and arm.</summary>
    public required int Runs { get; init; }

    /// <summary>The arms, in the order the file gives them.</summary>
    public required IReadOnlyList<Arm> Arms { get; init; }

    /// <summary>The pass line, or null for a measurement with no verdict.</summary>
    public PassLine? Pass { get; init; }

    /// <summary>The message that starts each base session in fresh mode.</summary>
    public string Resume { get; init; } = "/dmc-resume-campaign";

    /// <summary>The effort to use when a log records none, or null.</summary>
    public string? Effort { get; init; }

    /// <summary>The fork points, in file order.</summary>
    public required IReadOnlyList<ForkPoint> Points { get; init; }

    /// <summary>The key: what counts as a slip at each point, by point id.</summary>
    public required IReadOnlyDictionary<string, string> Key { get; init; }

    /// <summary>The documented pilot differences.</summary>
    public required IReadOnlyList<Allowed> Allow { get; init; }

    /// <summary>Reads and checks every input file of a work folder.</summary>
    /// <param name="work">The work folder.</param>
    /// <exception cref="SlipTestException">A file is missing or malformed; the message names the file and, for a table, the line.</exception>
    public static Trial Load(string work)
    {
        work = Folders.Full(work);
        string trialPath = Path.Combine(work, "trial.json");
        if (!File.Exists(trialPath))
            throw new SlipTestException($"no trial.json in {work}");
        JsonObject json;
        try
        {
            json = JsonNode.Parse(File.ReadAllText(trialPath)) as JsonObject ?? throw new SlipTestException("trial.json is not a JSON object");
        }
        catch (JsonException e)
        {
            throw new SlipTestException($"trial.json is not valid JSON: {e.Message}");
        }

        string mode = Required(json, "mode");
        if (mode is not ("fork" or "fresh"))
            throw new SlipTestException("trial.json: mode must be fork or fresh");
        string folder = Folders.Full(Required(json, "folder"));
        string copy = Folders.Full(Required(json, "copy"));
        Folders.Guard(folder, copy);
        int runs = json["runs"] is JsonValue r && r.TryGetValue(out int n) ? n : 0;
        if (runs is < 1 or > 20)
            throw new SlipTestException("trial.json: runs must be a whole number from 1 to 20");

        if (json["arms"] is not JsonObject armsJson || armsJson.Count == 0)
            throw new SlipTestException("trial.json: arms must name at least one arm");
        var arms = new List<Arm>();
        foreach (var (name, value) in armsJson)
        {
            if (!NamePattern().IsMatch(name))
                throw new SlipTestException($"trial.json: arm name {name} may hold only letters, digits, - and _");
            string? source = value is JsonValue v && v.TryGetValue(out string? s) ? s : null;
            if (string.IsNullOrWhiteSpace(source))
                throw new SlipTestException($"trial.json: arm {name} must name a file or folder");
            string full = Path.GetFullPath(Path.Combine(work, source));
            bool exists = mode == "fork" ? File.Exists(full) : Directory.Exists(full);
            if (!exists)
                throw new SlipTestException($"trial.json: arm {name}: no {(mode == "fork" ? "rules file" : "overlay folder")} at {full}");
            arms.Add(new Arm(name, full));
        }

        PassLine? pass = null;
        if (json["pass"] is JsonObject p)
        {
            string arm = Required(p, "arm", "pass."), against = Required(p, "against", "pass.");
            double below = p["below"] is JsonValue b && b.TryGetValue(out double d) ? d : double.NaN;
            if (double.IsNaN(below))
                throw new SlipTestException("trial.json: pass.below must be a number");
            if (!arms.Any(a => a.Name == arm) || !arms.Any(a => a.Name == against) || arm == against)
                throw new SlipTestException("trial.json: pass.arm and pass.against must be two different arms");
            pass = new PassLine(arm, against, below);
        }

        var points = ReadPoints(Path.Combine(work, "points.tsv"));
        return new Trial
        {
            Work = work,
            Mode = mode,
            Folder = folder,
            Copy = copy,
            Runs = runs,
            Arms = arms,
            Pass = pass,
            Resume = LogFile.Text(json, "resume") ?? "/dmc-resume-campaign",
            Effort = LogFile.Text(json, "effort"),
            Points = points,
            Key = ReadKey(Path.Combine(work, "key.tsv"), points),
            Allow = ReadAllow(Path.Combine(work, "allow.tsv")),
        };
    }

    /// <summary>A string property that must be present and not blank.</summary>
    /// <param name="json">The object.</param>
    /// <param name="name">The property.</param>
    /// <param name="prefix">What goes before the name in the message (<c>pass.</c>).</param>
    static string Required(JsonObject json, string name, string prefix = "") =>
        LogFile.Text(json, name) is { Length: > 0 } s ? s : throw new SlipTestException($"trial.json: {prefix}{name} is missing");

    /// <summary>The rows of a tab-separated file: blank lines and lines starting with <c>#</c> skipped, each row with its line number.</summary>
    /// <param name="path">The file.</param>
    static IEnumerable<(int Line, string[] Cells)> Rows(string path)
    {
        int n = 0;
        foreach (string line in File.ReadLines(path, Encoding.UTF8))
        {
            n++;
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
                continue;
            yield return (n, line.TrimEnd('\r').Split('\t'));
        }
    }

    /// <summary>Reads <c>points.tsv</c>.</summary>
    /// <param name="path">The file.</param>
    /// <exception cref="SlipTestException">It is missing, empty, a line is malformed, or an id repeats.</exception>
    static List<ForkPoint> ReadPoints(string path)
    {
        if (!File.Exists(path))
            throw new SlipTestException($"no points.tsv in {Path.GetDirectoryName(path)}");
        var points = new List<ForkPoint>();
        foreach (var (line, cells) in Rows(path))
        {
            if (cells.Length != 3 || !NamePattern().IsMatch(cells[0]) || cells[1].Trim().Length == 0 || cells[2].Trim().Length == 0)
                throw new SlipTestException($"points.tsv line {line} is not id<TAB>session id<TAB>message uuid");
            if (points.Any(p => p.Id == cells[0]))
                throw new SlipTestException($"points.tsv line {line}: point {cells[0]} is listed twice");
            points.Add(new ForkPoint(cells[0], cells[1].Trim(), cells[2].Trim()));
        }
        if (points.Count == 0)
            throw new SlipTestException("points.tsv lists no fork point");
        return points;
    }

    /// <summary>Reads <c>key.tsv</c>, which must have one row for every point and none for another.</summary>
    /// <param name="path">The file.</param>
    /// <param name="points">The fork points.</param>
    /// <exception cref="SlipTestException">It is missing, a line is malformed, or a point has no row.</exception>
    static Dictionary<string, string> ReadKey(string path, IReadOnlyList<ForkPoint> points)
    {
        if (!File.Exists(path))
            throw new SlipTestException("no key.tsv: write what counts as a slip at each point before the runs");
        var key = new Dictionary<string, string>();
        foreach (var (line, cells) in Rows(path))
        {
            if (cells.Length < 2 || cells[1].Trim().Length == 0 || !points.Any(p => p.Id == cells[0]) || key.ContainsKey(cells[0]))
                throw new SlipTestException($"key.tsv line {line} is not a point id<TAB>what counts as a slip, once per point");
            key[cells[0]] = string.Join('\t', cells.Skip(1));
        }
        if (points.FirstOrDefault(p => !key.ContainsKey(p.Id)) is { } missing)
            throw new SlipTestException($"key.tsv has no row for point {missing.Id}");
        return key;
    }

    /// <summary>Reads <c>allow.tsv</c>, which is optional.</summary>
    /// <param name="path">The file.</param>
    /// <exception cref="SlipTestException">A line is malformed.</exception>
    static List<Allowed> ReadAllow(string path)
    {
        var allow = new List<Allowed>();
        if (!File.Exists(path))
            return allow;
        foreach (var (line, cells) in Rows(path))
        {
            if (cells.Length != 3 || cells[0] is not ("notice" or "hook" or "thinking") || cells[1].Length == 0 || cells[2].Trim().Length == 0)
                throw new SlipTestException($"allow.tsv line {line} is not notice|hook|thinking<TAB>name<TAB>reason");
            allow.Add(new Allowed(cells[0], cells[1], cells[2].Trim()));
        }
        return allow;
    }

    /// <summary>The SHA-256 of each locked file, as lowercase hexadecimal.</summary>
    /// <param name="work">The work folder.</param>
    static Dictionary<string, string> Hashes(string work) =>
        LockedFiles.ToDictionary(f => f, f => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(work, f)))));

    /// <summary>Writes <c>lock.json</c>: the hash of each locked file and the time.</summary>
    /// <param name="now">The time to record.</param>
    public void WriteLock(DateTimeOffset now)
    {
        var json = new JsonObject { ["locked"] = now.ToString("O", System.Globalization.CultureInfo.InvariantCulture) };
        foreach (var (file, hash) in Hashes(Work))
            json[file] = hash;
        File.WriteAllText(Path.Combine(Work, "lock.json"), json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>Refuses when the trial was never prepared, or a locked file changed since.</summary>
    /// <exception cref="SlipTestException">No lock, a lock that does not parse, or a file differs from its hash.</exception>
    public void CheckLock()
    {
        string path = Path.Combine(Work, "lock.json");
        if (!File.Exists(path))
            throw new SlipTestException("the trial is not prepared: run slip-test prepare first");
        JsonObject? locked;
        try
        {
            locked = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (JsonException)
        {
            throw new SlipTestException("lock.json is not valid JSON: start a new work folder");
        }
        foreach (var (file, hash) in Hashes(Work))
            if (LogFile.Text(locked, file) != hash)
                throw new SlipTestException($"{file} changed after prepare locked it: start a new work folder");
    }
}
