using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dmc.Digest;
using Dmc.Tests.Digest;
using Dmc.Tests.Support;
using static Dmc.Tests.Digest.DigestFixtures;

namespace Dmc.Tests.Parity;

/// <summary>
/// <c>dmc digest</c> against <c>scripts/transcript_digest.py</c> on the same session logs: stdout, stderr, the exit code
/// and every file written, case by case. The two must agree to the byte, apart from what is meant to differ:
/// <list type="bullet">
/// <item>The "Generated 2026-10-15 by ..." line of <c>stats.md</c> and of the printed report holds today's date and the
///   name of the program, so it is compared as "Generated &lt;date&gt; by &lt;tool&gt;." (the timestamps rule of issue #3).
///   Nothing else in the output depends on the time: every time shown comes from the log.</item>
/// <item>Error messages, exit codes and the files written (or not) on a failure are compared exactly, apart from the
///   prefix: the script's <c>sys.exit</c> messages have none, and dmc says "dmc digest: " first, as the dispatcher does for
///   every command; argparse's usage text before an argument error is left out by dmc (one line only).</item>
/// <item>The files are compared with their line ends as LF: the script writes CRLF on Windows, dmc LF everywhere.</item>
/// </list>
/// Every case names its folders as relative paths ("logs", "out"), so nothing in the output depends on where the test runs.
/// The default folder (<c>adventures/.transcript-review</c>) is never used here: it is inside the repository.
/// </summary>
[TestClass]
public sealed partial class DigestParityTests
{
    [GeneratedRegex(@"^usage: transcript_digest\.py .*\n( {5,}.*\n)*", RegexOptions.Multiline)]
    private static partial Regex ArgparseUsage();

    [GeneratedRegex(@"^(No session logs found|No matching sessions)", RegexOptions.Multiline)]
    private static partial Regex ExitMessage();

    [GeneratedRegex(@"Generated \d{4}-\d\d-\d\d by (scripts/transcript_digest\.py|dmc digest)\.")]
    private static partial Regex Generated();

    static string Normalize(string text)
    {
        // Python 3.12.something and later list the choices bare; 3.11 and the first 3.12s quote each one. dmc writes them bare.
        text = ArgparseUsage().Replace(text, "")
            .Replace("transcript_digest.py digest: error: ", "dmc digest: ", StringComparison.Ordinal)
            .Replace("transcript_digest.py list: error: ", "dmc digest: ", StringComparison.Ordinal)
            .Replace("transcript_digest.py: error: ", "dmc digest: ", StringComparison.Ordinal)
            .Replace("(choose from 'list', 'digest')", "(choose from list, digest)", StringComparison.Ordinal);
        return Generated().Replace(ExitMessage().Replace(text, "dmc digest: $1"), "Generated <date> by <tool>.");
    }

    static ProcessResult RunScript(string work, string[] args, string? stdin)
    {
        string? python = Legacy.Python();
        TestEnv.Need(python is not null, "Python 3");
        var result = TestEnv.Run(python!, [Legacy.Script("transcript_digest.py"), .. args], stdin, work,
            environment: new Dictionary<string, string> { ["PYTHONIOENCODING"] = "utf-8" });
        // The one deliberate difference in what is printed: "list" prints the heading of its table before it looks for the
        // logs, so with no log folder the script prints the heading and then the error. dmc looks first and prints the
        // error alone (the one-line rule). So on a failure the heading is taken off the script's output.
        return result.ExitCode != 0 ? result with { Stdout = ListHeading().Replace(result.Stdout, "") } : result;
    }

    [GeneratedRegex(@"^id +start \(UTC\) +end \(UTC\) .*\r?\n")]
    private static partial Regex ListHeading();

    static readonly ParityTool Tool = new("transcript_digest.py", "digest", RunScript,
        Normalize: Normalize,
        NormalizeFile: (_, text) => Normalize(text.Replace("\r\n", "\n", StringComparison.Ordinal)));

    // ---------- building cases ----------

    /// <summary>
    /// A working folder with a folder "logs" that <paramref name="write"/> fills. The logs are given modification times in
    /// reverse name order, so that "oldest first" is not the order of the names.
    /// </summary>
    static Action<string> Logs(Action<string> write) => work =>
    {
        string logs = Directory.CreateDirectory(Path.Combine(work, "logs")).FullName;
        write(logs);
        AgeInReverseNameOrder(logs);
    };

    /// <summary>
    /// Give the logs modification times a minute apart, the last name the oldest. Logs with the same time come out in
    /// the script's directory order, which is the file system's and not a thing to compare.
    /// </summary>
    static void AgeInReverseNameOrder(string logs)
    {
        var names = Directory.GetFiles(logs, "*.jsonl").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).Reverse().ToList();
        for (int i = 0; i < names.Count; i++)
            File.SetLastWriteTimeUtc(Path.Combine(logs, names[i]), new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i));
    }

    static readonly Action<string> StandardLogs = Logs(WriteLogFolder);

    static ParityCase Case(string name, Action<string> seed, params string[] args) => new(name, _ => args, null, seed);

    // ---------- the cases: same output ----------

    /// <summary>Records of an odd kind, as a log can hold them: the script must treat each as it does and so must dmc.</summary>
    static void OddLog(string logs)
    {
        string Line(JsonObject record) => record.ToJsonString();
        var lines = new List<string>
        {
            // The byte-order mark makes the first line not JSON (as in Python, which reads it as a character).
            "\uFEFF" + Line(User(0, "lost to the byte-order mark")),
            "{\"type\":\"summary\",\"summary\":\"x\"}",
            "",
            "   ",
            Line(new JsonObject { ["type"] = "user", ["message"] = new JsonObject { ["role"] = "user", ["content"] = "No timestamp here, STOP" } }),
            // An assistant record with no id or model, a usage with one count, and a blank text that is not counted. Its
            // context (9000) is the biggest, so that the context ends below where it started: Python's // rounds that down.
            Line(new JsonObject
            {
                ["type"] = "assistant", ["timestamp"] = Stamp(1),
                ["message"] = new JsonObject { ["usage"] = new JsonObject { ["cache_read_input_tokens"] = 9000 }, ["content"] = Blocks(Text("   "), Text("Hello there")) },
            }),
            // Tool calls with missing parts: no input, no file_path, a partial read, a notebook, a search.
            Line(Assistant(2, "n1", Blocks(
                new JsonObject { ["type"] = "tool_use", ["id"] = "u1", ["name"] = "Skill" },
                ToolUse("u2", "Edit", ("old_string", "ab"), ("new_string", "cd")),
                ToolUse("u3", "Read", ("file_path", "D:\\dmc\\adventures\\aaa\\game-state.md"), ("limit", 20)),
                ToolUse("u4", "NotebookEdit", ("notebook_path", "n.ipynb")),
                ToolUse("u5", "Grep", ("pattern", "x"), ("path", "adventures/bbb/")),
                ToolUse("u6", "Bash", ("command", "echo adventures/bbb/ and adventures/aaa/"))), 1, 4998, 1, 9)),
            // Results: for a call with no content, for an id nobody asked for, and for a failed Bash.
            Line(User(3, Blocks(new JsonObject { ["type"] = "tool_result", ["tool_use_id"] = "u1" },
                ToolResult("zzz", Blocks(new JsonObject { ["type"] = "image" }, Text("seen")), isError: false),
                ToolResult("u6", "bash: boom\r\nsecond line", isError: true)))),
            // Two records on one physical line, parted by a lone carriage return, which Python reads as a line end.
            Line(User(4, "Why did that happen? I told you")) + "\r" + Line(User(5, Blocks(Text("Don\u2019t \u2014 never mind"), Text("plain")))),
            // The context shrinks after a compaction (Python's // rounds down), and the compaction has no time.
            "{\"type\":\"system\",\"subtype\":\"compact_boundary\"}",
            Line(User(6, Blocks(Text("the story so far")), ("isCompactSummary", true))),
            Line(Assistant(7, "n2", Blocks(Text("Back again")), 0, 900, 0, 3)),
            Line(User(8, "after the compaction")),
        };
        // Thirteen tools (the table shows twelve) and eleven errors (the list shows ten), with ties in every count.
        for (int i = 0; i < 13; i++)
            lines.Add(Line(Assistant(9, "t" + i, Blocks(ToolUse("x" + i, "Tool" + (char)('A' + i), ("file_path", $"f{i}.md"))), 0, 1000 + i, 0, 1)));
        for (int i = 0; i < 13; i++)
            lines.Add(Line(User(10, Blocks(ToolResult("x" + i, "bad " + i, isError: i < 11)))));
        File.WriteAllText(Path.Combine(logs, "0dd00000-aaaa.jsonl"), string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));
        File.WriteAllBytes(Path.Combine(logs, "0dd00001-empty.jsonl"), []);
        File.WriteAllText(Path.Combine(logs, "0dd00002-short.jsonl"), "{\"type\":\"user\",\"timestamp\":\"2026-02-01\",\"message\":{\"content\":\"\"}}", new UTF8Encoding(false));
    }

    /// <summary>Text with letters outside the BMP at every place a count or a cut can fall, and an escaped and a raw non-ASCII record.</summary>
    static void UnicodeLog(string logs)
    {
        const string clef = "\U0001D11E";
        string over(int length) => new string('a', length - 1) + clef + "tail";
        var records = new List<JsonObject>
        {
            User(0, over(70)), // the biggest jumps cut the message at 70 characters, with the clef the 70th
            Assistant(1, "m1", Blocks(Text(over(6000)), ToolUse("t1", "Read", ("file_path", "D:\\dmc\\adventures\\th\u00f3r\\\u5317-" + clef + ".md"))), 1, 2, 3, 4), // cut at 6000
            User(2, Blocks(ToolResult("t1", "北" + clef + " é"))), // json.dumps writes each as an escape, so the size counts them
            User(3, over(3000)), // cut at 3000 characters, with the clef the 3000th
            Assistant(4, "m2", Blocks(Text("⚔ " + clef + clef), ToolUse("t2", "Bash", ("command", "echo " + clef + " é " + new string('x', 230)))), 1, 2, 3, 4),
            User(5, over(600) + "\n" + over(10)), // the turn index cuts at 600
            Assistant(6, "m3", Blocks(Text("done")), 0, 5000, 0, 1),
            User(7, "end"),
        };
        File.WriteAllText(Path.Combine(logs, "00000001-unicode.jsonl"),
            string.Concat(records.Select(r => r.ToJsonString() + "\n")), new UTF8Encoding(false));
        // The same kind of text written as it is, not escaped.
        var raw = User(0, "Thr\u00f3r says \u201chello\u201d " + clef).ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        File.WriteAllText(Path.Combine(logs, "00000002-raw.jsonl"), raw + "\n", new UTF8Encoding(false));
    }

    /// <summary>Turns that grow the context by 1000, 300, 300, 700, 700, 100 and 20 (equal jumps keep the order of the messages), and two logs that share a short id.</summary>
    static void GrowthLog(string logs)
    {
        int[] growth = [1000, 1300, 1600, 2300, 3000, 3100, 3120, 3170];
        var records = new List<JsonObject>();
        for (int i = 0; i < growth.Length; i++)
        {
            records.Add(User(i, $"message {i}"));
            records.Add(Assistant(i, "g" + i, Blocks(Text($"reply {i}"), ToolUse("w" + i, "Edit", ("file_path", i % 2 == 0 ? State : "canon.md"), ("old_string", "a"), ("new_string", "b"))), 0, growth[i], 0, 1));
        }
        WriteLog(logs, records, "abcd1234-aaaa", garbage: false);
        WriteLog(logs, [User(0, "the other log that starts abcd1234"), Assistant(1, "o1", Blocks(Text("ok")), 1, 1, 1, 1)], "abcd1234-bbbb", garbage: false);
    }

    /// <summary>One assistant record as JSON text, so that an id, a model or a field can be null, empty or a number.</summary>
    static string Msg(int minute, string id, string model, string usage, string content = "[]") =>
        "{\"type\":\"assistant\",\"timestamp\":\"" + Stamp(minute) + "\",\"message\":{\"id\":" + id + ",\"model\":" + model
        + ",\"usage\":" + usage + ",\"content\":" + content + "}}";

    static string ToolJson(string id, string name, string input) => "{\"type\":\"tool_use\",\"id\":" + id + ",\"name\":" + name + ",\"input\":" + input + "}";

    /// <summary>
    /// Python keeps <c>None</c>, <c>""</c>, <c>1</c> and <c>"1"</c> apart as keys and joins <c>True</c>, <c>1</c> and <c>1.0</c>,
    /// and a null where text is expected is "None" in <c>str()</c> and a key of its own. The ids, models, tool names and skills
    /// of this log are all of those, so the calls counted, the Models and Skills rows and the tool output rows show it.
    /// A time that is 0 is "" (<c>or ""</c>), and a path that is a number is its text.
    /// </summary>
    static void KeysLog(string logs)
    {
        const string usage = "{\"input_tokens\":1,\"output_tokens\":2}";
        string[] lines =
        [
            // Before anyone speaks: a tool with no name and a null path, and a result that names no tool (so it finds the null id).
            Msg(0, "null", "null", usage, "[" + ToolJson("null", "null", "{\"file_path\":null}") + "]"),
            "{\"type\":\"user\",\"timestamp\":\"" + Stamp(1) + "\",\"message\":{\"content\":[{\"type\":\"tool_result\",\"content\":\"boom\",\"is_error\":true}]}}",
            "{\"type\":\"user\",\"timestamp\":0,\"message\":{\"role\":\"user\",\"content\":\"hello\"}}",
            // Ids: null is seen, "" and "1" are new, 1 is new and then true and 1.0 are the same call.
            Msg(2, "\"\"", "\"\"", usage),
            Msg(2, "1", "1", usage),
            Msg(2, "true", "true", usage),
            Msg(2, "1.0", "1.0", usage),
            Msg(2, "\"1\"", "\"1\"", usage),
            // Models: 1, true and 1.0 are one key; the first used is shown. No model is None, like null.
            Msg(3, "\"m1\"", "1", usage),
            Msg(3, "\"m2\"", "true", usage),
            Msg(3, "\"m3\"", "1.0", usage),
            Msg(3, "\"m4\"", "\"1\"", usage),
            Msg(3, "\"m5\"", "null", usage),
            // Skills and files of tool calls: null, "" and 1; a path and a command that are not text.
            Msg(4, "\"m6\"", "\"claude-test\"", usage, "[" + string.Join(",",
                ToolJson("\"k1\"", "\"Skill\"", "{\"skill\":null}"), ToolJson("\"k2\"", "\"Skill\"", "{\"skill\":\"\"}"),
                ToolJson("\"k3\"", "\"Skill\"", "{\"skill\":1}"), ToolJson("\"k4\"", "\"Skill\"", "{}"),
                ToolJson("\"k5\"", "\"Read\"", "{\"file_path\":5}"), ToolJson("\"k6\"", "\"Bash\"", "{\"command\":null}"),
                ToolJson("\"k7\"", "\"Edit\"", "{\"file_path\":null,\"old_string\":\"a\",\"new_string\":\"b\"}")) + "]"),
            "{\"type\":\"user\",\"timestamp\":\"" + Stamp(5) + "\",\"message\":{\"content\":[{\"type\":\"tool_result\",\"tool_use_id\":\"k5\",\"content\":null},"
                + "{\"type\":\"tool_result\",\"tool_use_id\":\"k7\",\"content\":\"nope\",\"is_error\":true}]}}",
        ];
        File.WriteAllText(Path.Combine(logs, "0000aaaa-keys.jsonl"), string.Join("\n", lines) + "\n", new UTF8Encoding(false));
    }

    /// <summary>
    /// Token counts that Python adds without complaint: floats (shown with <c>%d</c>, which cuts towards zero), a bool
    /// (<c>True</c> is 1) and counts that are missing. The context ends below where it started, by a fraction, so that
    /// Python's float <c>//</c> (round down) is what makes "-2" of -1.25.
    /// </summary>
    static void NumbersLog(string logs)
    {
        string[] lines =
        [
            User(0, "first").ToJsonString(),
            Msg(1, "\"a\"", "\"claude-test\"", "{\"input_tokens\":1.5,\"cache_read_input_tokens\":true,\"cache_creation_input_tokens\":2.5,\"output_tokens\":2.5}",
                "[" + ToolJson("\"t1\"", "\"Bash\"", "{\"command\":\"bash scripts/roll.sh 1d20\"}") + "]"),
            User(2, "second").ToJsonString(),
            Msg(3, "\"b\"", "\"claude-test\"", "{\"input_tokens\":3.7}"),
            Msg(3, "\"c\"", "\"claude-test\"", "{\"input_tokens\":1e3,\"cache_read_input_tokens\":false,\"output_tokens\":0.5}"),
            Msg(3, "\"d\"", "\"claude-test\"", "{\"input_tokens\":2.5}"),
            User(4, "third").ToJsonString(),
            Msg(5, "\"e\"", "\"claude-test\"", "{\"input_tokens\":2.5,\"cache_read_input_tokens\":1500.9}"),
        ];
        File.WriteAllText(Path.Combine(logs, "0000bbbb-numbers.jsonl"), string.Join("\n", lines) + "\n", new UTF8Encoding(false));
    }

    static IEnumerable<ParityCase> SameOutput()
    {
        yield return Case("list", StandardLogs, "list", "--logs", "logs");
        yield return Case("list with shortened options and an equals sign", StandardLogs, "list", "--logs=logs", "--camp=silver-egg", "--af", "2026-01-01");
        yield return Case("digest of everything", StandardLogs, "digest", "--logs", "logs", "--out", "out");
        yield return Case("digest of two session ids", StandardLogs, "digest", "--logs", "logs", "--out", "out", "ffff", "abcd");
        yield return Case("digest with the ids after the options", StandardLogs, "digest", "--logs", "logs", "--out", "out", "--campaign", "silver-egg", "abcd");
        yield return Case("digest with the ids after a double dash", StandardLogs, "digest", "--logs", "logs", "--out", "out", "--", "ffff");
        yield return Case("digest into a folder that does not exist yet", StandardLogs, "digest", "--logs", "logs", "--out", "deep/er/out", "--after", "2026-02-01");
        yield return Case("every class of third and later call", Logs(l => WriteLog(l, CallRecords())), "digest", "--logs", "logs", "--out", "out");
        yield return Case("text with letters outside the BMP", Logs(UnicodeLog), "digest", "--logs", "logs", "--out", "out");
        yield return Case("odd records, listed", Logs(OddLog), "list", "--logs", "logs");
        yield return Case("odd records, digested", Logs(OddLog), "digest", "--logs", "logs", "--out", "out");
        yield return Case("growth, ties and a short id shared by two logs", Logs(GrowthLog), "digest", "--logs", "logs", "--out", "out");
        yield return Case("ids, models, names and skills that are null, empty or numbers", Logs(KeysLog), "digest", "--logs", "logs", "--out", "out");
        yield return Case("token counts that are floats and bools", Logs(NumbersLog), "digest", "--logs", "logs", "--out", "out");
        yield return Case("growth, listed", Logs(GrowthLog), "list", "--logs", "logs", "--after", "2026-01-02");
    }

    // ---------- the cases: errors ----------

    /// <summary>Every way a run fails that the script reports, or argparse does: exit code, message, and nothing written.</summary>
    public static IEnumerable<ParityCase> ErrorCases()
    {
        yield return Case("no arguments", StandardLogs);
        yield return Case("a command that does not exist", StandardLogs, "extract");
        yield return Case("only an option that does not exist", StandardLogs, "--bogus");
        yield return Case("an option before the command takes the next word for the command", StandardLogs, "--logs", "logs", "list");
        yield return Case("an option that does not exist", StandardLogs, "list", "--logs", "logs", "--bogus");
        yield return Case("an option without its value", StandardLogs, "list", "--logs", "logs", "--campaign");
        yield return Case("a missing value comes before an unknown option", StandardLogs, "list", "--bogus", "--campaign");
        yield return Case("a flag given a value", StandardLogs, "list", "--logs", "logs", "--help=x");
        yield return Case("session ids after an option are not recognized", StandardLogs, "digest", "--logs", "logs", "abcd", "--campaign", "silver-egg", "ffff");
        yield return Case("an option of digest given to list", StandardLogs, "list", "--logs", "logs", "--out", "out");
        yield return Case("a double dash does not stand for the value", StandardLogs, "digest", "--logs", "--", "logs");
        yield return Case("a folder that is not there", StandardLogs, "list", "--logs", "nowhere");
        yield return Case("nothing to show", StandardLogs, "digest", "--logs", "logs", "--out", "out", "--campaign", "nowhere");
    }

    /// <summary>
    /// Runs that fail in dmc and are not compared with the script, because the script crashes with a traceback on them (a
    /// record that is not an object, a log that is not UTF-8, an output path that is a file). They are in the one-line guard
    /// of <c>ErrorPathTests</c>, with <see cref="ErrorCases"/>.
    /// </summary>
    public static IEnumerable<ParityCase> DmcOnlyErrorCases()
    {
        ParityCase Bad(string name, string fileName, byte[] content, params string[] extra) => new(name,
            _ => ["digest", "--logs", "logs", "--out", "out", .. extra], null,
            Logs(logs => File.WriteAllBytes(Path.Combine(logs, fileName), content)));

        yield return Bad("a log that is not UTF-8", "bad-bytes.jsonl", [0x7B, 0xFF, 0xFE, 0x7D, 0x0A]);
        yield return Bad("a record that is not an object", "list.jsonl", "[1, 2]\n"u8.ToArray());
        yield return Bad("a message that is a number", "number.jsonl", "{\"type\": \"user\", \"message\": 5}\n"u8.ToArray());
        yield return Bad("a content block that is a string", "string.jsonl", "{\"type\": \"user\", \"message\": {\"content\": [\"x\"]}}\n"u8.ToArray());
        yield return Bad("a token count that is text", "count.jsonl",
            "{\"type\": \"assistant\", \"message\": {\"id\": \"m\", \"usage\": {\"input_tokens\": \"5\"}}}\n"u8.ToArray());
        yield return Bad("a tool name that is a number, after a player spoke", "name.jsonl",
            Encoding.UTF8.GetBytes("{\"type\": \"user\", \"message\": {\"content\": \"hi\"}}\n{\"type\": \"assistant\", \"message\": {\"id\": \"m\", \"content\": [{\"type\": \"tool_use\", \"name\": 5, \"input\": {}}]}}\n"));
        yield return Bad("a tool call whose input is a list", "input.jsonl",
            "{\"type\": \"assistant\", \"message\": {\"id\": \"m\", \"content\": [{\"type\": \"tool_use\", \"name\": \"Bash\", \"input\": [1]}]}}\n"u8.ToArray());
        yield return new ParityCase("an output path that is a file", _ => ["digest", "--logs", "logs", "--out", "out"], null, work =>
        {
            StandardLogs(work);
            File.WriteAllText(Path.Combine(work, "out"), "in the way");
        });
    }

    public static IEnumerable<object[]> SameData => SameOutput().Select(c => new object[] { c });

    public static IEnumerable<object[]> ErrorData => ErrorCases().Select(c => new object[] { c });

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    [TestMethod]
    [DynamicData(nameof(SameData), DynamicDataDisplayName = nameof(CaseName))]
    public void DigestMatchesTranscriptDigestPy(ParityCase test) => ParityCheck.Run(Tool, test);

    [TestMethod]
    [DynamicData(nameof(ErrorData), DynamicDataDisplayName = nameof(CaseName))]
    public void DigestFailsLikeTranscriptDigestPy(ParityCase test) => ParityCheck.Run(Tool, test);

    /// <summary>
    /// With no <c>--logs</c>, both look in <c>$CLAUDE_CONFIG_DIR/projects/&lt;this folder, with every character outside
    /// A-Za-z0-9 turned into "-"&gt;</c>: the error that names the folder when it is missing, and the table when it is there.
    /// The environment variable is the test process's own, which both programs inherit.
    /// </summary>
    [TestMethod]
    public void TheDefaultLogFolderIsTheOneTheScriptUses()
    {
        using var config = new TempFolder();
        string project = Path.Combine(config.Path, "projects", LogFolder.ProjectName(TestEnv.Root));
        Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", config.Path);
        try
        {
            ParityCheck.Run(Tool, new ParityCase("the folder is missing", "list"));
            WriteLogFolder(Directory.CreateDirectory(project).FullName);
            AgeInReverseNameOrder(project);
            ParityCheck.Run(Tool, new ParityCase("the folder is there", "list"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", null);
        }
    }
}
