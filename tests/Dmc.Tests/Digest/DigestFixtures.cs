using System.Text;
using System.Text.Json.Nodes;

namespace Dmc.Tests.Digest;

/// <summary>
/// The made-up Claude Code logs the tests of <c>tests/test_transcript_digest.py</c> start from (<c>RECORDS</c>,
/// <c>CALL_RECORDS</c> and the helpers that build them), and a way to write them to a folder. Every method returns fresh
/// nodes, because a JSON node can only belong to one parent.
/// </summary>
public static class DigestFixtures
{
    /// <summary>The id, and so the file name, of the main made-up session.</summary>
    public const string SessionId = "abcd1234-0000-4000-8000-000000000000";

    /// <summary>The DM's first line of narration in the main session.</summary>
    public const string Narration = "You stand in the goblin hall.";

    /// <summary>A Windows path to <c>game-state.md</c> of the made-up campaign, as a log written on Windows holds it.</summary>
    public const string State = "D:\\dmc\\adventures\\silver-egg\\game-state.md";

    /// <summary>A Bash command that rolls dice with the old script, which the digest must still class as a roll.</summary>
    public const string RollLine = "bash scripts/roll.sh 1d20+5";

    /// <summary>A timestamp on 2026-01-02, at 10:<paramref name="minute"/>:00 UTC.</summary>
    public static string Stamp(int minute) => $"2026-01-02T10:{minute:00}:00.000Z";

    /// <summary>A user record at this minute with this content (a string, or an array of blocks); <paramref name="extra"/> adds top-level fields such as <c>isMeta</c>.</summary>
    public static JsonObject User(int minute, JsonNode content, params (string Key, JsonNode Value)[] extra)
    {
        var record = new JsonObject
        {
            ["type"] = "user",
            ["timestamp"] = Stamp(minute),
            ["message"] = new JsonObject { ["role"] = "user", ["content"] = content },
        };
        foreach (var (key, value) in extra)
            record[key] = value;
        return record;
    }

    /// <summary>An assistant record with the API message <paramref name="id"/> and these content blocks.</summary>
    /// <param name="minute">When it was written.</param>
    /// <param name="id">The API message id; records that repeat it are one call.</param>
    /// <param name="content">The content blocks (see <see cref="Blocks"/>).</param>
    /// <param name="fresh">Input tokens that were not cached.</param>
    /// <param name="cached">Input tokens read from the cache.</param>
    /// <param name="written">Input tokens written to the cache.</param>
    /// <param name="output">Output tokens.</param>
    public static JsonObject Assistant(int minute, string id, JsonArray content, int fresh, int cached, int written, int output) => new()
    {
        ["type"] = "assistant",
        ["timestamp"] = Stamp(minute),
        ["message"] = new JsonObject
        {
            ["id"] = id,
            ["model"] = "claude-test",
            ["role"] = "assistant",
            ["usage"] = new JsonObject
            {
                ["input_tokens"] = fresh,
                ["cache_read_input_tokens"] = cached,
                ["cache_creation_input_tokens"] = written,
                ["output_tokens"] = output,
            },
            ["content"] = content,
        },
    };

    /// <summary>A <c>text</c> content block.</summary>
    public static JsonObject Text(string text) => new() { ["type"] = "text", ["text"] = text };

    /// <summary>A <c>tool_use</c> block: the call's id, the tool's name, and its input as key and value pairs.</summary>
    public static JsonObject ToolUse(string id, string name, params (string Key, JsonNode Value)[] args)
    {
        var input = new JsonObject();
        foreach (var (key, value) in args)
            input[key] = value;
        return new JsonObject { ["type"] = "tool_use", ["id"] = id, ["name"] = name, ["input"] = input };
    }

    /// <summary>A <c>tool_result</c> block for the call with this id; <c>is_error</c> is set only when <paramref name="isError"/> is true.</summary>
    public static JsonObject ToolResult(string id, JsonNode content, bool isError = false)
    {
        var block = new JsonObject { ["type"] = "tool_result", ["tool_use_id"] = id, ["content"] = content };
        if (isError)
            block["is_error"] = true;
        return block;
    }

    /// <summary>An array of content blocks.</summary>
    public static JsonArray Blocks(params JsonNode[] blocks) => new(blocks);

    /// <summary>One session: two player messages, a compaction between them, one failed edit.</summary>
    public static List<JsonObject> Records() =>
    [
        User(0, "Resume adventures/silver-egg/ please"),
        User(0, "<system-reminder>be good</system-reminder>"),
        Assistant(1, "m1", Blocks(Text(Narration), ToolUse("t1", "Read", ("file_path", "D:\\dmc\\adventures\\silver-egg\\game-state.md"))),
            100, 900, 0, 50),
        // Claude Code writes one record per content block, repeating the message id.
        Assistant(1, "m1", Blocks(ToolUse("t2", "Edit", ("file_path", "D:\\dmc\\adventures\\silver-egg\\story\\canon.md"),
            ("old_string", "ab"), ("new_string", "abcde"))), 100, 900, 0, 60),
        User(2, Blocks(ToolResult("t1", "HP 10/12"))),
        User(2, Blocks(ToolResult("t2", Blocks(Text("String to replace\nnot found")), isError: true))),
        new JsonObject { ["type"] = "system", ["subtype"] = "compact_boundary", ["timestamp"] = Stamp(3) },
        User(3, "The story so far: goblins.", ("isCompactSummary", true)),
        User(3, "Caveat: local command output", ("isMeta", true)),
        User(4, Blocks(Text("No, that is wrong.\n[Request interrupted by user]"))),
        Assistant(5, "m2", Blocks(ToolUse("t3", "Skill", ("skill", "draw-map")),
            ToolUse("t4", "Write", ("file_path", "adventures/silver-egg/maps/live.json"), ("content", "{}")),
            ToolUse("t5", "Bash", ("command", "bash scripts/roll.sh 1d20"))), 200, 2500, 300, 40),
        User(6, Blocks(ToolResult("t5", "1d20: [7] = 7"))),
        Assistant(7, "m3", Blocks(Text("Sorry.")), 10, 3400, 90, 5),
    ];

    /// <summary>One API call of a reply, with the whole context read from the cache.</summary>
    public static JsonObject Call(int minute, string id, int ctx, params JsonNode[] blocks) =>
        Assistant(minute, id, blocks.Length > 0 ? new JsonArray(blocks) : Blocks(Text("Done.")), 0, ctx, 0, 1);

    /// <summary>One player message for each class of third and later call, with distinct context sizes.</summary>
    public static List<JsonObject> CallRecords() =>
    [
        User(0, "I attack"),
        Call(0, "a1", 100, ToolUse("a1t", "Bash", ("command", RollLine))),
        Call(0, "a2", 110, ToolUse("a2t", "Edit", ("file_path", State), ("old_string", "HP 10"), ("new_string", "HP 7"))),
        Call(0, "a3", 120),
        User(1, "I drink the potion"),
        Call(1, "b1", 200, ToolUse("b1t", "Edit", ("file_path", State), ("old_string", "abc"), ("new_string", "abcdef"))),
        Call(1, "b2", 210, ToolUse("b2t", "Edit", ("file_path", State), ("old_string", "x"), ("new_string", "y"))),
        Call(1, "b3", 220),
        User(2, "They both attack"),
        Call(2, "c1", 300, ToolUse("c1t", "Bash", ("command", RollLine))),
        Call(2, "c2", 310, ToolUse("c2t", "PowerShell", ("command", ".\\scripts\\roll.ps1 1d6"))),
        Call(2, "c3", 320),
        User(3, "Who was the smith?"),
        Call(3, "d1", 400, ToolUse("d1t", "Grep", ("pattern", "smith"))),
        Call(3, "d2", 410, ToolUse("d2t", "Edit", ("file_path", "canon.md"), ("old_string", ""), ("new_string", "Smith: Bera"))),
        Call(3, "d3", 420),
        User(4, "<command-message>campaign-full-save</command-message>\n<command-name>/campaign-full-save</command-name>"),
        Call(4, "e1", 500, ToolUse("e1t", "Write", ("file_path", State), ("content", "1234567890"))),
        Call(4, "e2", 510, ToolUse("e2t", "Read", ("file_path", State)), ToolUse("e2u", "Read", ("file_path", "canon.md"))),
        Call(4, "e3", 520, ToolUse("e3t", "Edit", ("file_path", "summary.md"), ("old_string", "a"), ("new_string", "b"))),
        Call(4, "e4", 530),
        User(5, "resume please"),
        Call(5, "f1", 600, ToolUse("f1t", "Skill", ("skill", "resume-campaign"))),
        Call(5, "f2", 610, ToolUse("f2t", "Read", ("file_path", State))),
        Call(5, "f3", 620),
        User(6, "Show me the map"),
        Call(6, "g1", 700, ToolUse("g1t", "Bash", ("command", "python scripts/map_render.py live.json"))),
        Call(6, "g2", 710, ToolUse("g2t", "Bash", ("command", "ls"))),
        Call(6, "g3", 720),
        User(7, "Next"),
        Call(7, "h1", 800, ToolUse("h1t", "Edit", ("file_path", State), ("old_string", "gone"), ("new_string", "here"))),
        User(7, Blocks(ToolResult("h1t", "String to replace not found in file.", isError: true))),
        Call(7, "h2", 810),
    ];

    /// <summary>The cached context of every API call in the records, the figure "all context read" counts.</summary>
    public static long CallTotal() =>
        CallRecords().Where(r => (string?)r["type"] == "assistant").Sum(r => (long)r["message"]!["usage"]!["cache_read_input_tokens"]!.GetValue<int>());

    /// <summary>Write a log of these records, one JSON line each, and (by default) a last line that is not JSON.</summary>
    public static string WriteLog(string folder, IEnumerable<JsonObject> records, string sessionId = SessionId, bool garbage = true)
    {
        string path = Path.Combine(folder, sessionId + ".jsonl");
        var text = new StringBuilder();
        foreach (var record in records)
            text.Append(record.ToJsonString()).Append('\n');
        if (garbage)
            text.Append("this line is not JSON\n");
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>The folder of logs of the command-line tests: the session above, one of another campaign, one where the player never spoke.</summary>
    public static void WriteLogFolder(string folder)
    {
        WriteLog(folder, Records());
        var other = new List<JsonObject>
        {
            User(0, "Start adventures/red-keep/ now"),
            Assistant(1, "m9", Blocks(Text("Ok.")), 5, 5, 5, 5),
        };
        foreach (var record in other)
            record["timestamp"] = ((string)record["timestamp"]!).Replace("2026-01-02", "2026-03-04", StringComparison.Ordinal);
        WriteLog(folder, other, "ffff9999-1111", garbage: false);
        WriteLog(folder, [User(0, "<system-reminder>nobody spoke</system-reminder>")], "eeee0000-2222");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a session log");
    }
}
