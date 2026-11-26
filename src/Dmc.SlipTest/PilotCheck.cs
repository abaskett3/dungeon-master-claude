using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>The outcome of one check on one run, or on one point for the thinking check.</summary>
internal enum Outcome
{
    /// <summary>Nothing differs.</summary>
    Pass,

    /// <summary>Everything that differs is in <c>allow.tsv</c>.</summary>
    Documented,

    /// <summary>Something differs that <c>allow.tsv</c> does not name.</summary>
    Fail,
}

/// <summary>One check's result on one run.</summary>
/// <param name="Check">The check: <c>notice</c>, <c>hook</c>, <c>context</c>, <c>rules</c> or <c>thinking</c>.</param>
/// <param name="Point">The point id.</param>
/// <param name="Arm">The arm, or <c>all</c> for the thinking check.</param>
/// <param name="Run">The run number.</param>
/// <param name="Outcome">The outcome.</param>
/// <param name="Names">What differs: attachment types, hashed meta lines, counts. Never text of play.</param>
internal sealed record CheckResult(string Check, string Point, string Arm, int Run, Outcome Outcome, IReadOnlyList<string> Names);

/// <summary>
/// The pilot's four checks (FR-007, #249): (a) no notice that play did not have, (b) the hook output play had, (c)
/// the arm's rules in the request, (d) the same thinking blocks in every arm; and, for an arm with a context folder
/// (spec 029), that its text was a hook output, which check (b) leaves out. Each compares the part of the replayed
/// turn that came with the player message (its records before the first <c>assistant</c> record) with the same part
/// of the original turn.
/// </summary>
internal static class PilotCheck
{
    /// <summary>
    /// Attachment types that only record how Claude Code built a request and are not sent to the model:
    /// <c>thinking_drop</c> (research R3) is check (d)'s data, not a notice.
    /// </summary>
    static readonly HashSet<string> RecordOnly = ["thinking_drop"];

    /// <summary>
    /// The signature of each notice in the message part of a turn: <c>attachment:&lt;type&gt;</c> for an attachment
    /// that is not a hook's or record-only, <c>system:&lt;subtype&gt;</c> for a system record, and <c>meta#&lt;hash&gt;</c> for a
    /// meta user message (the first 8 hexadecimal characters of the SHA-256 of its first line, so no text of play is
    /// printed).
    /// </summary>
    /// <param name="turn">The records of a turn.</param>
    public static List<string> Notices(IEnumerable<JsonObject> turn) =>
        MessagePart(turn).Select(Notice).OfType<string>().ToList();

    /// <summary>The records of a turn before its first <c>assistant</c> record: what Claude Code sent with the message.</summary>
    /// <param name="turn">The records of a turn.</param>
    static IEnumerable<JsonObject> MessagePart(IEnumerable<JsonObject> turn) => turn.TakeWhile(r => LogFile.TypeOf(r) != "assistant");

    /// <summary>A record's notice signature, or null when it is not a notice (a hook's output, or an ordinary record).</summary>
    /// <param name="record">A record.</param>
    static string? Notice(JsonObject record)
    {
        switch (LogFile.TypeOf(record))
        {
            case "attachment":
                string type = LogFile.Text(record["attachment"] as JsonObject, "type") ?? "";
                return type.StartsWith("hook_", StringComparison.Ordinal) || RecordOnly.Contains(type) ? null : "attachment:" + type;
            case "system":
                return "system:" + (LogFile.Text(record, "subtype") ?? "");
            case "user" when record["isMeta"]?.GetValueKind() == System.Text.Json.JsonValueKind.True:
                string first = (LogFile.MessageText(record) ?? "").Split('\n')[0].Trim();
                return "meta#" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(first)))[..8];
            default:
                return null;
        }
    }

    /// <summary>The hook outputs in the message part of a turn: each hook attachment's type and its text (<c>content</c>, else <c>stdout</c>).</summary>
    /// <param name="turn">The records of a turn.</param>
    public static List<(string Type, string Text)> Hooks(IEnumerable<JsonObject> turn)
    {
        var hooks = new List<(string, string)>();
        foreach (var record in MessagePart(turn).Where(r => LogFile.TypeOf(r) == "attachment"))
        {
            var a = record["attachment"] as JsonObject;
            string type = LogFile.Text(a, "type") ?? "";
            if (!type.StartsWith("hook_", StringComparison.Ordinal))
                continue;
            var content = a!["content"];
            string text = content is JsonValue v && v.TryGetValue(out string? s) && s.Length > 0 ? s
                : content is JsonArray arr ? string.Join("\n", arr.Select(x => x is JsonValue xv && xv.TryGetValue(out string? xs) ? xs : x?.ToJsonString() ?? ""))
                : LogFile.Text(a, "stdout") ?? "";
            hooks.Add((type, text));
        }
        return hooks;
    }

    /// <summary>Check (a): every notice of the replayed turn also occurs in the original turn.</summary>
    /// <param name="original">The original turn.</param>
    /// <param name="replay">The replayed turn.</param>
    /// <param name="allow">The documented differences.</param>
    public static (Outcome, List<string>) CheckNotices(IEnumerable<JsonObject> original, IEnumerable<JsonObject> replay, IReadOnlyList<Allowed> allow)
    {
        var known = Notices(original).ToHashSet();
        var extra = Notices(replay).Where(n => !known.Contains(n)).Distinct().ToList();
        return (Judge(extra, "notice", allow), extra);
    }

    /// <summary>Check (b): the hook outputs of the replayed turn equal the original's, in order; a difference is named by its hook type.</summary>
    /// <param name="original">The original turn.</param>
    /// <param name="replay">The replayed turn.</param>
    /// <param name="allow">The documented differences.</param>
    /// <param name="folder">The DMC folder, whose path in the original's hook output becomes <paramref name="copy"/>; null to compare as is.</param>
    /// <param name="copy">The copy folder.</param>
    /// <param name="context">The arm's text at this point (spec 029), whose one hook output is left out of the replay's before they are compared; null or blank for none.</param>
    public static (Outcome, List<string>) CheckHooks(IEnumerable<JsonObject> original, IEnumerable<JsonObject> replay, IReadOnlyList<Allowed> allow, string? folder = null, string? copy = null, string? context = null)
    {
        var a = Hooks(original);
        if (folder is not null && copy is not null)
            a = a.Select(h => (h.Type, h.Text.Replace(folder, copy, Folders.PathComparison).Replace(folder.Replace('\\', '/'), copy.Replace('\\', '/'), Folders.PathComparison))).ToList();
        var b = Hooks(replay);
        if (ContextAt(b, context) is int at)
            b.RemoveAt(at);
        var differ = new List<string>();
        for (int i = 0; i < Math.Max(a.Count, b.Count); i++)
        {
            if (i < a.Count && i < b.Count && a[i] == b[i])
                continue;
            string type = i < b.Count ? b[i].Type : a[i].Type;
            if (!differ.Contains(type))
                differ.Add(type);
        }
        return (Judge(differ, "hook", allow), differ);
    }

    /// <summary>The index of the hook output whose text is an arm's context text, ignoring white space at either end; null when there is none or the text is blank.</summary>
    /// <param name="hooks">The hook outputs of a turn (<see cref="Hooks"/>).</param>
    /// <param name="context">The context text, or null.</param>
    static int? ContextAt(List<(string Type, string Text)> hooks, string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return null;
        int at = hooks.FindIndex(h => h.Text.Trim() == context.Trim());
        return at < 0 ? null : at;
    }

    /// <summary>
    /// The context check (spec 029 FR-005): the arm's text at this point is one of the replayed turn's hook outputs,
    /// or the text is blank, so no hook was added.
    /// </summary>
    /// <param name="replay">The replayed turn.</param>
    /// <param name="context">The context text.</param>
    public static bool CheckContext(IEnumerable<JsonObject> replay, string context) =>
        string.IsNullOrWhiteSpace(context) || ContextAt(Hooks(replay), context) is not null;

    /// <summary>Check (c): some string in the fork log equals the arm's rules text as Claude Code records it, so the rules reached the request.</summary>
    /// <param name="fork">The fork log.</param>
    /// <param name="rules">The arm's rules text.</param>
    public static bool CheckRules(LogFile fork, string rules) => fork.Records.Any(r => Holds(r, LogRewriter.Recorded(rules)));

    /// <summary>Whether a node holds a string equal to a text.</summary>
    /// <param name="node">The node.</param>
    /// <param name="text">The text.</param>
    static bool Holds(JsonNode? node, string text) => node switch
    {
        JsonObject o => o.Any(p => Holds(p.Value, text)),
        JsonArray a => a.Any(x => Holds(x, text)),
        JsonValue v => v.TryGetValue(out string? s) && s == text,
        _ => false,
    };

    /// <summary>
    /// The thinking blocks of a replayed turn's first request, from its first <c>thinking_drop</c> attachment
    /// (research R3): the blocks sent and the blocks newly dropped; null when the turn records none.
    /// </summary>
    /// <param name="replay">The replayed turn.</param>
    public static (long Sent, long Dropped)? Thinking(IEnumerable<JsonObject> replay)
    {
        foreach (var record in replay.Where(r => LogFile.TypeOf(r) == "attachment"))
        {
            var a = record["attachment"] as JsonObject;
            if (LogFile.Text(a, "type") != "thinking_drop")
                continue;
            long sent = a!["thinkingBlocksSent"] is JsonValue s && s.TryGetValue(out long n) ? n : 0;
            long dropped = (a["newlyDropped"] as JsonObject)?["blockCount"] is JsonValue d && d.TryGetValue(out long m) ? m : 0;
            return (sent, dropped);
        }
        return null;
    }

    /// <summary>
    /// Check (d) at one point: the thinking counts of every arm's run are equal. When no run records them, the outcome
    /// is <c>not recorded</c>, which passes only as documented.
    /// </summary>
    /// <param name="byArm">Each arm's counts, or null when its turn records none.</param>
    /// <param name="allow">The documented differences.</param>
    public static (Outcome, List<string>) CheckThinking(IReadOnlyDictionary<string, (long Sent, long Dropped)?> byArm, IReadOnlyList<Allowed> allow)
    {
        if (byArm.Values.All(v => v is null))
            return (Judge(["not recorded"], "thinking", allow), ["not recorded"]);
        var distinct = byArm.Values.Distinct().ToList();
        if (distinct.Count == 1)
            return (Outcome.Pass, []);
        var names = byArm.Select(p => p.Value is { } v ? $"{p.Key} sent {v.Sent} dropped {v.Dropped}" : $"{p.Key} not recorded").ToList();
        return (Outcome.Fail, names);
    }

    /// <summary>Pass when nothing differs, documented when <c>allow.tsv</c> names every difference, else fail.</summary>
    /// <param name="differences">What differs.</param>
    /// <param name="check">The check's name in <c>allow.tsv</c>.</param>
    /// <param name="allow">The documented differences.</param>
    static Outcome Judge(IReadOnlyList<string> differences, string check, IReadOnlyList<Allowed> allow)
    {
        if (differences.Count == 0)
            return Outcome.Pass;
        return differences.All(d => allow.Any(a => a.Check == check && a.Name == d)) ? Outcome.Documented : Outcome.Fail;
    }

    /// <summary>
    /// Runs every check on the pilot's runs and writes <c>pilot.md</c> (contract): a table of runs, passes,
    /// documented differences and failures per check, the verdict, then one line per difference.
    /// </summary>
    /// <param name="trial">The trial.</param>
    /// <param name="points">The resolved points.</param>
    /// <param name="runs">The finished runs (<c>runs.tsv</c>).</param>
    /// <returns>The report, and whether every check passed.</returns>
    /// <exception cref="SlipTestException">No pilot run has finished.</exception>
    public static (string Report, bool Passed) Run(Trial trial, IReadOnlyList<ResolvedPoint> points, IReadOnlyList<RunRow> runs)
    {
        var results = new List<CheckResult>();
        var pilotPairs = new List<(ResolvedPoint, Arm)>();
        foreach (var p in points)
            foreach (var arm in trial.Arms)
                if (pilotPairs.Count < Runner.PilotPairs)
                    pilotPairs.Add((p, arm));
        var thinking = new Dictionary<string, Dictionary<string, (long, long)?>>();
        foreach (var (point, arm) in pilotPairs)
        {
            if (!runs.Any(r => r.Point == point.Point.Id && r.Arm == arm.Name && r.Run == 1))
                continue;
            var fork = LogFile.Read(Path.Combine(trial.Work, "runs", arm.Name, $"{point.Point.Id}-1.jsonl"));
            int at = fork.LastTyped(point.Text);
            var replay = at < 0 ? [] : fork.TurnAfter(at);
            var original = point.Log.TurnAfter(point.Index);
            string id = point.Point.Id;
            var (n, nn) = CheckNotices(original, replay, trial.Allow);
            results.Add(new CheckResult("notice", id, arm.Name, 1, at < 0 ? Outcome.Fail : n, at < 0 ? ["replayed message not found"] : nn));
            if (trial.Mode == "fork")
            {
                string? context = trial.ContextFile(arm.Name, id) is { } file ? File.ReadAllText(file) : null;
                var (h, hn) = CheckHooks(original, replay, trial.Allow, trial.Folder, trial.Copy, context);
                results.Add(new CheckResult("hook", id, arm.Name, 1, h, hn));
                if (context is not null)
                {
                    bool arrived = CheckContext(replay, context);
                    results.Add(new CheckResult("context", id, arm.Name, 1, arrived ? Outcome.Pass : Outcome.Fail, arrived ? [] : ["context text not in the fork's hook output"]));
                }
                bool rules = CheckRules(fork, File.ReadAllText(arm.Source));
                results.Add(new CheckResult("rules", id, arm.Name, 1, rules ? Outcome.Pass : Outcome.Fail, rules ? [] : ["rules text not in the fork log"]));
            }
            if (!thinking.TryGetValue(id, out var byArm))
                thinking[id] = byArm = [];
            byArm[arm.Name] = Thinking(replay);
        }
        if (results.Count == 0)
            throw new SlipTestException("no pilot run has finished: run slip-test run --pilot first");
        foreach (var (id, byArm) in thinking.Where(t => t.Value.Count > 1))
        {
            var (t, tn) = CheckThinking(byArm, trial.Allow);
            results.Add(new CheckResult("thinking", id, "all", 1, t, tn));
        }

        var o = new StringBuilder();
        o.Append("check     runs  pass  documented  fail\n");
        foreach (string check in trial.Context.Count > 0 ? ["notice", "hook", "context", "rules", "thinking"] : new[] { "notice", "hook", "rules", "thinking" })
        {
            var of = results.Where(r => r.Check == check).ToList();
            o.Append($"{check,-9} {of.Count,4}  {of.Count(r => r.Outcome == Outcome.Pass),4}  {of.Count(r => r.Outcome == Outcome.Documented),10}  {of.Count(r => r.Outcome == Outcome.Fail),4}\n");
        }
        bool passed = results.All(r => r.Outcome != Outcome.Fail);
        o.Append($"verdict: {(passed ? "pass" : "fail")}\n");
        foreach (var r in results.Where(r => r.Outcome != Outcome.Pass))
            o.Append($"{r.Check} {r.Point} {r.Arm} {r.Run}: {(r.Outcome == Outcome.Fail ? "fail" : "documented")}: {string.Join(", ", r.Names)}\n");
        return (o.ToString(), passed);
    }
}
