using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>
/// One Claude Code session log (<c>~/.claude/projects/&lt;slug&gt;/&lt;session id&gt;.jsonl</c>): one JSON object per
/// line. The shapes it relies on are in <c>specs/024-slip-test-runner/research.md</c> R2: <c>user</c> and
/// <c>assistant</c> records with <c>uuid</c>, <c>timestamp</c> and <c>message</c>, and <c>attachment</c> records for
/// what Claude Code adds around a turn.
/// </summary>
internal sealed class LogFile
{
    /// <summary>The log's path.</summary>
    public string Path { get; }

    /// <summary>The records in file order. A line that is not a JSON object is left out, as Claude Code skips it.</summary>
    public IReadOnlyList<JsonObject> Records { get; }

    /// <summary>A log made from records already read (the tests, and a rewritten log).</summary>
    /// <param name="path">Where the log is, or would be.</param>
    /// <param name="records">Its records, in order.</param>
    public LogFile(string path, IReadOnlyList<JsonObject> records)
    {
        Path = path;
        Records = records;
    }

    /// <summary>Reads a log from disk.</summary>
    /// <param name="path">The <c>.jsonl</c> file.</param>
    /// <exception cref="SlipTestException">The file does not exist.</exception>
    public static LogFile Read(string path)
    {
        if (!File.Exists(path))
            throw new SlipTestException($"no log at {path}");
        var records = new List<JsonObject>();
        foreach (string line in File.ReadLines(path))
            if (Parse(line) is { } record)
                records.Add(record);
        return new LogFile(path, records);
    }

    /// <summary>One line as a JSON object, or null when it is blank or not a JSON object.</summary>
    /// <param name="line">A line of a log.</param>
    public static JsonObject? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;
        try
        {
            return JsonNode.Parse(line) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A record's <c>type</c>, or "".</summary>
    /// <param name="record">A record.</param>
    public static string TypeOf(JsonObject record) => Text(record, "type") ?? "";

    /// <summary>A string property of an object, or null when it is missing or not a string.</summary>
    /// <param name="node">The object.</param>
    /// <param name="name">The property.</param>
    public static string? Text(JsonObject? node, string name) =>
        node?[name] is JsonValue v && v.TryGetValue(out string? s) ? s : null;

    /// <summary>The index of the record with this <c>uuid</c>, or -1.</summary>
    /// <param name="uuid">The record's uuid.</param>
    public int IndexOf(string uuid)
    {
        for (int i = 0; i < Records.Count; i++)
            if (Text(Records[i], "uuid") == uuid)
                return i;
        return -1;
    }

    /// <summary>
    /// Whether a record is a message the player typed: a <c>user</c> record that is not meta and not a subagent's
    /// (<c>isSidechain</c>), whose content is text only (a string, or <c>text</c> blocks; no tool result, no image), and
    /// which is not a slash command or a shell command (Claude Code writes those inside <c>&lt;command-name&gt;</c>,
    /// <c>&lt;local-command</c> or <c>&lt;bash-</c> tags) or Claude Code's own "[Request interrupted" marker.
    /// </summary>
    /// <param name="record">A record.</param>
    public static bool IsTyped(JsonObject record)
    {
        if (TypeOf(record) != "user" || record["isMeta"]?.GetValueKind() == JsonValueKind.True || record["isSidechain"]?.GetValueKind() == JsonValueKind.True)
            return false;
        if (record["isCompactSummary"]?.GetValueKind() == JsonValueKind.True)
            return false;
        string? text = MessageText(record);
        if (text is null || text.Trim().Length == 0)
            return false;
        string start = text.TrimStart();
        return !start.StartsWith("<command-", StringComparison.Ordinal)
            && !start.StartsWith("[Request interrupted", StringComparison.Ordinal)
            && !start.StartsWith("<local-command", StringComparison.Ordinal)
            && !start.StartsWith("<bash-", StringComparison.Ordinal);
    }

    /// <summary>
    /// The text of a <c>user</c> record's message: the string, or its <c>text</c> blocks joined by a blank line; null
    /// when it holds any other block.
    /// </summary>
    /// <param name="record">A <c>user</c> record.</param>
    public static string? MessageText(JsonObject record)
    {
        var content = (record["message"] as JsonObject)?["content"];
        if (content is JsonValue v && v.TryGetValue(out string? s))
            return s;
        if (content is not JsonArray blocks)
            return null;
        var parts = new List<string>();
        foreach (var block in blocks)
        {
            if (block is not JsonObject b || Text(b, "type") != "text")
                return null;
            parts.Add(Text(b, "text") ?? "");
        }
        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// The model in force at a message: that of the last <c>assistant</c> record before it, or, for a session's first
    /// message, of the first one after it (the model that answered it), skipping Claude Code's own
    /// <c>&lt;synthetic&gt;</c> messages; null if none.
    /// </summary>
    /// <param name="index">The fork message's index.</param>
    public string? ModelBefore(int index) => Nearest(index, r => Text(r["message"] as JsonObject, "model") is { } m && m != "<synthetic>" ? m : null);

    /// <summary>The effort in force at a message (an <c>assistant</c> record's <c>effort</c> property), found as <see cref="ModelBefore"/> finds the model; null if no record has one.</summary>
    /// <param name="index">The fork message's index.</param>
    public string? EffortBefore(int index) => Nearest(index, r => Text(r, "effort") is { Length: > 0 } e ? e : null);

    /// <summary>A value of the last <c>assistant</c> record before an index that has one, else of the first after it.</summary>
    /// <param name="index">The index.</param>
    /// <param name="value">The value of a record, or null.</param>
    string? Nearest(int index, Func<JsonObject, string?> value)
    {
        for (int i = index - 1; i >= 0; i--)
            if (TypeOf(Records[i]) == "assistant" && value(Records[i]) is { } v)
                return v;
        for (int i = index + 1; i < Records.Count; i++)
            if (TypeOf(Records[i]) == "assistant" && value(Records[i]) is { } v)
                return v;
        return null;
    }

    /// <summary>The permission mode of a <c>user</c> record (its <c>permissionMode</c>), or null.</summary>
    /// <param name="index">The record's index.</param>
    public string? PermissionAt(int index) => Text(Records[index], "permissionMode");

    /// <summary>
    /// The session kind the log names (the first <c>sessionKind</c> property of any record, #238), or null. Not
    /// verified: the property comes from the maintainer's logs, and this container's logs have none.
    /// </summary>
    public string? Kind() => Records.Select(r => Text(r, "sessionKind")).FirstOrDefault(k => k is { Length: > 0 });

    /// <summary>
    /// The records of the turn that a message starts: every record after it, up to the next message the player typed
    /// or the end of the log.
    /// </summary>
    /// <param name="index">The message's index.</param>
    public IReadOnlyList<JsonObject> TurnAfter(int index)
    {
        var turn = new List<JsonObject>();
        for (int i = index + 1; i < Records.Count && !IsTyped(Records[i]); i++)
            turn.Add(Records[i]);
        return turn;
    }

    /// <summary>
    /// The index of the message a fork log replayed: the last message the player typed whose text equals the fork
    /// message's. A fork log is a copy of the base history followed by the replayed turn (research R1).
    /// </summary>
    /// <param name="text">The fork message's text.</param>
    /// <returns>The index, or -1.</returns>
    public int LastTyped(string text)
    {
        for (int i = Records.Count - 1; i >= 0; i--)
            if (IsTyped(Records[i]) && MessageText(Records[i]) == text)
                return i;
        return -1;
    }

    /// <summary>
    /// The reply of a turn: the <c>text</c> blocks of its <c>assistant</c> records, in order, joined by a blank line.
    /// Tool calls, tool results, thinking and attachments are left out.
    /// </summary>
    /// <param name="turn">The records of a turn (<see cref="TurnAfter"/>).</param>
    public static string Reply(IEnumerable<JsonObject> turn)
    {
        var parts = new List<string>();
        foreach (var record in turn.Where(r => TypeOf(r) == "assistant"))
            if ((record["message"] as JsonObject)?["content"] is JsonArray blocks)
                foreach (var block in blocks.OfType<JsonObject>().Where(b => Text(b, "type") == "text"))
                    if (Text(block, "text") is { Length: > 0 } t)
                        parts.Add(t.Trim());
        return string.Join("\n\n", parts);
    }

    /// <summary>A record's <c>timestamp</c>, or null when it has none or it does not parse.</summary>
    /// <param name="record">A record.</param>
    public static DateTimeOffset? TimeOf(JsonObject record) => ParseTime(Text(record, "timestamp"));

    /// <summary>An ISO 8601 time as Claude Code writes it, read as UTC when it names no offset; null when it does not parse.</summary>
    /// <param name="text">The time, or null.</param>
    public static DateTimeOffset? ParseTime(string? text) =>
        DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var t) ? t : null;

    /// <summary>The working directory the log records first (a record's <c>cwd</c>), or null.</summary>
    public string? Cwd() => Records.Select(r => Text(r, "cwd")).FirstOrDefault(c => c is { Length: > 0 });
}
