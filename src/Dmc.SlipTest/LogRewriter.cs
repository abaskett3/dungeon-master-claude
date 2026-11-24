using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>
/// Writes the base log a fork-mode replay continues (FR-003): the play log up to the fork message, under a new
/// session id, with the DMC folder's path replaced by the copy folder's and the recorded text of the DMC folder's
/// <c>CLAUDE.md</c> replaced by the arm's rules. A continued conversation keeps the instructions its log records, so
/// the log is where a change to the rules has to go (#249; research R1, R2).
/// </summary>
internal static class LogRewriter
{
    /// <summary>How records are written back: no escaping of non-ASCII text, so a line stays as Claude Code wrote it.</summary>
    public static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>The result of a rewrite.</summary>
    /// <param name="Lines">The base log's lines, one record each.</param>
    /// <param name="RulesReplaced">How many recorded copies of the DMC folder's <c>CLAUDE.md</c> were replaced.</param>
    internal sealed record Result(IReadOnlyList<string> Lines, int RulesReplaced);

    /// <summary>Rewrites a play log into a base log.</summary>
    /// <param name="log">The play log.</param>
    /// <param name="forkIndex">The index of the fork message; the base log ends before it.</param>
    /// <param name="sessionId">The new session id.</param>
    /// <param name="folder">The DMC folder's full path.</param>
    /// <param name="copy">The copy folder's full path.</param>
    /// <param name="rules">The arm's rules text, or null to leave the recorded rules as they are (fresh mode).</param>
    public static Result Rewrite(LogFile log, int forkIndex, string sessionId, string folder, string copy, string? rules)
    {
        var pairs = PathPairs(folder, copy);
        string rulesPath = Path.Combine(copy, "CLAUDE.md");
        int replaced = 0;
        var lines = new List<string>();
        var chain = Chain(log, forkIndex);
        bool linked = chain.Count > 0;
        for (int i = 0; i < forkIndex; i++)
        {
            // A record off the fork message's chain (an abandoned branch after a rewind, a subagent's) is left out, so the
            // resume continues from the fork message's parent.
            if (linked && LogFile.Text(log.Records[i], "uuid") is { } uuid && !chain.Contains(uuid))
                continue;
            var record = (JsonObject)Map(log.Records[i], pairs)!;
            // The record of the last prompt points at a leaf that may be after the fork; the resume needs only the chain.
            if (LogFile.TypeOf(record) == "last-prompt")
                continue;
            if (record.ContainsKey("sessionId"))
                record["sessionId"] = sessionId;
            if (rules is not null)
                replaced += ReplaceRules(record, rulesPath, rules);
            lines.Add(record.ToJsonString(Options));
        }
        return new Result(lines, replaced);
    }

    /// <summary>
    /// The uuids of the conversation the fork message continues: its parent, the parent's parent and so on. When the
    /// fork index is the end of the log (a fresh-mode base), the chain runs back from the last record with a uuid.
    /// Empty when the log records no parents, and then every record is kept.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <param name="forkIndex">The fork message's index, or the number of records.</param>
    static HashSet<string> Chain(LogFile log, int forkIndex)
    {
        var byUuid = new Dictionary<string, JsonObject>();
        foreach (var r in log.Records)
            if (LogFile.Text(r, "uuid") is { } u)
                byUuid.TryAdd(u, r);
        string? next = forkIndex < log.Records.Count
            ? LogFile.Text(log.Records[forkIndex], "parentUuid")
            : log.Records.Select(r => LogFile.Text(r, "uuid")).LastOrDefault(u => u is not null);
        var chain = new HashSet<string>();
        while (next is not null && chain.Add(next) && byUuid.TryGetValue(next, out var record))
            next = LogFile.Text(record, "parentUuid");
        return chain;
    }

    /// <summary>
    /// The text replacements for the folder path: the path as given, and on Windows the same with <c>/</c>, since
    /// tools write both forms.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="copy">The copy folder.</param>
    static List<(string From, string To)> PathPairs(string folder, string copy)
    {
        var pairs = new List<(string, string)> { (folder, copy) };
        if (folder.Contains('\\'))
            pairs.Add((folder.Replace('\\', '/'), copy.Replace('\\', '/')));
        return pairs;
    }

    /// <summary>A copy of a node with the folder path replaced in every string value and property name (the file history's snapshots name files by path).</summary>
    /// <param name="node">The node.</param>
    /// <param name="pairs">The replacements.</param>
    static JsonNode? Map(JsonNode? node, List<(string From, string To)> pairs)
    {
        switch (node)
        {
            case JsonObject o:
                var obj = new JsonObject();
                foreach (var (k, v) in o)
                    obj[Replace(k, pairs)] = Map(v, pairs);
                return obj;
            case JsonArray a:
                var arr = new JsonArray();
                foreach (var v in a)
                    arr.Add(Map(v, pairs));
                return arr;
            case JsonValue v when v.TryGetValue(out string? s):
                return JsonValue.Create(Replace(s, pairs));
            default:
                return node?.DeepClone();
        }
    }

    /// <summary>A text with every folder path replaced.</summary>
    /// <param name="s">The text.</param>
    /// <param name="pairs">The replacements.</param>
    static string Replace(string s, List<(string From, string To)> pairs)
    {
        foreach (var (from, to) in pairs)
            s = s.Replace(from, to, Folders.PathComparison);
        return s;
    }

    /// <summary>
    /// Replaces the content of every recorded file whose path is the copy folder's <c>CLAUDE.md</c> in an
    /// <c>instructions</c> attachment (research R2).
    /// </summary>
    /// <param name="record">A record, already moved to the copy folder's path.</param>
    /// <param name="rulesPath">The copy folder's <c>CLAUDE.md</c>.</param>
    /// <param name="rules">The arm's rules.</param>
    /// <returns>How many were replaced.</returns>
    static int ReplaceRules(JsonObject record, string rulesPath, string rules)
    {
        if (LogFile.TypeOf(record) != "attachment" || record["attachment"] is not JsonObject a || LogFile.Text(a, "type") != "instructions")
            return 0;
        if (a["files"] is not JsonArray files)
            return 0;
        int n = 0;
        foreach (var file in files.OfType<JsonObject>())
        {
            string? path = LogFile.Text(file, "path");
            if (path is null || !SamePath(path, rulesPath))
                continue;
            file["content"] = Recorded(rules);
            n++;
        }
        return n;
    }

    /// <summary>
    /// A rules text as Claude Code records it: without its trailing white space. A recorded text that differs from what
    /// Claude Code reads at the start of the replay makes it record the file again as changed (a live probe on
    /// 2.1.331), a notice the pilot reports.
    /// </summary>
    /// <param name="rules">The rules file's text.</param>
    public static string Recorded(string rules) => rules.TrimEnd();

    /// <summary>Whether two paths name the same file, ignoring the separator and, on Windows, case.</summary>
    /// <param name="a">One path.</param>
    /// <param name="b">The other.</param>
    static bool SamePath(string a, string b) =>
        a.Replace('\\', '/').Equals(b.Replace('\\', '/'), Folders.PathComparison);
}
