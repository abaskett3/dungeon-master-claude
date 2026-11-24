using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>
/// An invented trial for the slip test runner's tests (spec 024): a DMC folder with one campaign file, a Claude Code
/// folder with one play log and its file history, and a work folder with the trial's input files. Nothing here comes
/// from real play. The play log has two player messages: <see cref="FirstMessage"/> at <see cref="T1"/> and the fork
/// point <see cref="ForkMessage"/> at <see cref="T2"/>. The campaign file <c>adventures/c/a.md</c> read <c>start</c>
/// at T1, <c>middle</c> at T2 and <c>after</c> now; <c>adventures/c/new.md</c> was made after T2.
/// </summary>
internal sealed class SlipTestFixtures : IDisposable
{
    /// <summary>The session id of the play log.</summary>
    public const string Session = "aaaaaaaa-0000-4000-8000-000000000001";

    /// <summary>The uuid of the first player message.</summary>
    public const string FirstMessage = "u-first";

    /// <summary>The uuid of the fork point's player message.</summary>
    public const string ForkMessage = "u-fork";

    /// <summary>The text of the fork point's player message.</summary>
    public const string ForkText = "I draw my sword.";

    /// <summary>The rules the play log records.</summary>
    public const string RecordedRules = "# Old rules\n";

    /// <summary>The hook output the play log records for the fork message.</summary>
    public const string HookText = "Scene: the cellar. Present: Hala.";

    /// <summary>The time of the first player message.</summary>
    public static readonly DateTimeOffset T1 = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The time of the fork message.</summary>
    public static readonly DateTimeOffset T2 = new(2026, 11, 1, 10, 5, 0, TimeSpan.Zero);

    /// <summary>The temporary folder that holds everything.</summary>
    public string Root { get; }

    /// <summary>The DMC folder that played.</summary>
    public string Folder { get; }

    /// <summary>The copy folder.</summary>
    public string Copy { get; }

    /// <summary>The Claude Code folder.</summary>
    public string ClaudeHome { get; }

    /// <summary>The work folder.</summary>
    public string Work { get; }

    /// <summary>The play log's path.</summary>
    public string LogPath => Path.Combine(Folders.Project(ClaudeHome, Folder), Session + ".jsonl");

    /// <summary>Makes the folders and writes the play log, the file history, the campaign files and a fork-mode trial of two arms and two points, one run each.</summary>
    public SlipTestFixtures()
    {
        Root = Path.Combine(Path.GetTempPath(), "slip-test-" + Guid.NewGuid().ToString("N"));
        Folder = Path.Combine(Root, "DMC");
        Copy = Path.Combine(Root, "DMX");
        ClaudeHome = Path.Combine(Root, "home", ".claude");
        Work = Path.Combine(Root, "work");
        Directory.CreateDirectory(Path.Combine(Folder, "adventures", "c"));
        File.WriteAllText(Path.Combine(Folder, "CLAUDE.md"), RecordedRules);
        File.WriteAllText(Path.Combine(Folder, "adventures", "c", "a.md"), "after");
        File.WriteAllText(Path.Combine(Folder, "adventures", "c", "new.md"), "made later");
        string history = Path.Combine(ClaudeHome, "file-history", Session);
        Directory.CreateDirectory(history);
        File.WriteAllText(Path.Combine(history, "h1@v1"), "start");
        File.WriteAllText(Path.Combine(history, "h1@v2"), "middle");
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        File.WriteAllLines(LogPath, Linked(PlayLog()).Select(r => r.ToJsonString()));

        Directory.CreateDirectory(Path.Combine(Work, "arms"));
        File.WriteAllText(Path.Combine(Work, "arms", "control.md"), "# Control rules\n");
        File.WriteAllText(Path.Combine(Work, "arms", "treatment.md"), "# Treatment rules\n");
        WriteTrial();
        File.WriteAllText(Path.Combine(Work, "points.tsv"), $"# id\tsession\tmessage\np1\t{Session}\t{ForkMessage}\np2\t{Session}\t{FirstMessage}\n");
        File.WriteAllText(Path.Combine(Work, "key.tsv"), "p1\tthe sword is a dagger\np2\tHala is not there\n");
    }

    /// <summary>Writes <c>trial.json</c>, by default fork mode with two arms, one run, and a pass line.</summary>
    /// <param name="mode">The mode.</param>
    /// <param name="runs">Runs per point and arm.</param>
    /// <param name="arms">The arms object, or null for control and treatment rules files.</param>
    public void WriteTrial(string mode = "fork", int runs = 1, JsonObject? arms = null)
    {
        var trial = new JsonObject
        {
            ["mode"] = mode,
            ["folder"] = Folder,
            ["copy"] = Copy,
            ["runs"] = runs,
            ["arms"] = arms ?? new JsonObject { ["control"] = "arms/control.md", ["treatment"] = "arms/treatment.md" },
            ["pass"] = new JsonObject { ["arm"] = "treatment", ["against"] = "control", ["below"] = 0 },
        };
        File.WriteAllText(Path.Combine(Work, "trial.json"), trial.ToJsonString());
    }

    /// <summary>An ISO 8601 time as Claude Code writes it.</summary>
    /// <param name="t">The time.</param>
    public static string Stamp(DateTimeOffset t) => t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A <c>user</c> record a player typed.</summary>
    /// <param name="uuid">Its uuid.</param>
    /// <param name="text">The message.</param>
    /// <param name="t">When.</param>
    /// <param name="cwd">The working directory.</param>
    /// <param name="session">The session id.</param>
    public static JsonObject Typed(string uuid, string text, DateTimeOffset t, string cwd, string session) => new()
    {
        ["type"] = "user", ["uuid"] = uuid, ["sessionId"] = session, ["cwd"] = cwd, ["timestamp"] = Stamp(t),
        ["permissionMode"] = "auto", ["message"] = new JsonObject { ["role"] = "user", ["content"] = text },
    };

    /// <summary>An attachment record.</summary>
    /// <param name="attachment">The attachment.</param>
    /// <param name="session">The session id.</param>
    public static JsonObject Attachment(JsonObject attachment, string session) => new()
    {
        ["type"] = "attachment", ["uuid"] = Guid.NewGuid().ToString(), ["sessionId"] = session, ["attachment"] = attachment,
    };

    /// <summary>An <c>assistant</c> record.</summary>
    /// <param name="session">The session id.</param>
    /// <param name="blocks">Its content blocks.</param>
    public static JsonObject Assistant(string session, params JsonObject[] blocks) => new()
    {
        ["type"] = "assistant", ["uuid"] = Guid.NewGuid().ToString(), ["sessionId"] = session, ["effort"] = "high",
        ["message"] = new JsonObject { ["role"] = "assistant", ["model"] = "claude-test-1", ["content"] = new JsonArray(blocks) },
    };

    /// <summary>A <c>text</c> block.</summary>
    /// <param name="text">The text.</param>
    public static JsonObject TextBlock(string text) => new() { ["type"] = "text", ["text"] = text };

    /// <summary>A hook's output as Claude Code records it with a player message.</summary>
    /// <param name="text">The output.</param>
    /// <param name="session">The session id.</param>
    public static JsonObject Hook(string text, string session) =>
        Attachment(new JsonObject { ["type"] = "hook_additional_context", ["content"] = new JsonArray(text), ["hookEvent"] = "UserPromptSubmit" }, session);

    /// <summary>The invented play log.</summary>
    public List<JsonObject> PlayLog()
    {
        string a = Path.Combine(Folder, "adventures", "c", "a.md");
        return
        [
            Attachment(new JsonObject
            {
                ["type"] = "instructions",
                ["files"] = new JsonArray(new JsonObject { ["path"] = Path.Combine(Folder, "CLAUDE.md"), ["type"] = "Project", ["content"] = RecordedRules }),
            }, Session),
            Typed(FirstMessage, "We go down to the cellar.", T1, Folder, Session),
            Hook("Scene: the stairs.", Session),
            Attachment(new JsonObject { ["type"] = "date", ["date"] = "2026-11-01" }, Session),
            Assistant(Session, new JsonObject { ["type"] = "tool_use", ["id"] = "t1", ["name"] = "Read", ["input"] = new JsonObject { ["file_path"] = a } }),
            new JsonObject
            {
                ["type"] = "user", ["uuid"] = "u-tool", ["sessionId"] = Session, ["cwd"] = Folder, ["timestamp"] = Stamp(T1.AddSeconds(2)),
                ["message"] = new JsonObject { ["role"] = "user", ["content"] = new JsonArray(new JsonObject { ["type"] = "tool_result", ["tool_use_id"] = "t1", ["content"] = "start" }) },
            },
            Snapshot(new JsonObject { [a] = Entry("h1@v1", T1.AddSeconds(3)) }),
            Assistant(Session, TextBlock("The stairs creak.")),
            Typed(ForkMessage, ForkText, T2, Folder, Session),
            Hook(HookText, Session),
            Attachment(new JsonObject { ["type"] = "date", ["date"] = "2026-11-01" }, Session),
            Assistant(Session, new JsonObject { ["type"] = "thinking", ["thinking"] = "", ["signature"] = "sig" }, TextBlock("You draw your dagger.")),
            Snapshot(new JsonObject
            {
                [a] = Entry("h1@v2", T2.AddSeconds(3)),
                [Path.Combine("adventures", "c", "new.md")] = Entry(null, T2.AddSeconds(3)),
            }),
            new JsonObject { ["type"] = "last-prompt", ["lastPrompt"] = ForkText, ["sessionId"] = Session },
        ];
    }

    /// <summary>Gives each record with a uuid the previous one's as its <c>parentUuid</c>, as Claude Code chains a conversation.</summary>
    /// <param name="records">The records, in order.</param>
    public static List<JsonObject> Linked(List<JsonObject> records)
    {
        string? parent = null;
        foreach (var r in records.Where(r => r.ContainsKey("uuid")))
        {
            r["parentUuid"] = parent;
            parent = LogFile.Text(r, "uuid");
        }
        return records;
    }

    /// <summary>A <c>file-history-snapshot</c> record.</summary>
    /// <param name="tracked">Its <c>trackedFileBackups</c>.</param>
    static JsonObject Snapshot(JsonObject tracked) => new()
    {
        ["type"] = "file-history-snapshot", ["messageId"] = Guid.NewGuid().ToString(),
        ["snapshot"] = new JsonObject { ["trackedFileBackups"] = tracked, ["timestamp"] = Stamp(T2) },
    };

    /// <summary>An entry of <c>trackedFileBackups</c>.</summary>
    /// <param name="backup">The backup's file name, or null when the file did not exist.</param>
    /// <param name="t">When the backup was taken.</param>
    static JsonObject Entry(string? backup, DateTimeOffset t) => new() { ["backupFileName"] = backup, ["version"] = 1, ["backupTime"] = Stamp(t) };

    /// <summary>Runs a command of the program in-process against these folders.</summary>
    /// <param name="fake">The stand-in for Claude Code, or null.</param>
    /// <param name="args">The command and its options; <c>--work</c> is added.</param>
    /// <returns>The exit code, standard output and standard error.</returns>
    public (int Code, string Output, string Error) Run(IClaude? fake, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = Dmc.SlipTest.Program.Run([.. args, "--work", Work], output, error, ClaudeHome, "claude", T2.AddDays(1), new Random(1), fake);
        return (code, output.ToString(), error.ToString());
    }

    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}

/// <summary>
/// A stand-in for Claude Code: it reads the log a call resumes from the copy folder's project folder, and writes a
/// fork log under a new session id: that history, the message, the hook output and notices it is given, and a reply.
/// It records what each call saw.
/// </summary>
/// <param name="claudeHome">The Claude Code folder.</param>
internal sealed class FakeClaude(string claudeHome) : IClaude
{
    /// <summary>The hook text the fork records with a message; by default the play log's for that message.</summary>
    public Func<string, string> Hook { get; set; } = m => m == SlipTestFixtures.ForkText ? SlipTestFixtures.HookText : "Scene: the stairs.";

    /// <summary>The attachment types the fork records with the message, besides the hook.</summary>
    public List<string> Notices { get; } = ["date"];

    /// <summary>The thinking blocks the fork's first request sent, by the rules text its history holds; null to record none.</summary>
    public Func<string, (long Sent, long Dropped)?> Thinking { get; set; } = _ => (1, 1);

    /// <summary>Whether the fork reports an error result, as Claude Code does after an API error.</summary>
    public bool Fails { get; set; }

    /// <summary>What each call saw: the call, the message, and the copy folder's files under <c>adventures/</c> with their contents.</summary>
    public List<(ClaudeCall Call, string Message, Dictionary<string, string> Files)> Calls { get; } = [];

    /// <inheritdoc/>
    public string Version() => "9.9.9";

    /// <inheritdoc/>
    public ClaudeResult Run(string cwd, ClaudeCall call, string message, out string raw)
    {
        string adventures = Path.Combine(cwd, "adventures");
        var files = Directory.Exists(adventures)
            ? Directory.EnumerateFiles(adventures, "*", SearchOption.AllDirectories).ToDictionary(f => Path.GetRelativePath(cwd, f).Replace('\\', '/'), File.ReadAllText)
            : [];
        Calls.Add((call, message, files));
        string project = Folders.Project(claudeHome, cwd);
        Directory.CreateDirectory(project);
        string id = Guid.NewGuid().ToString();
        var lines = call.Resume is null ? [] : File.ReadAllLines(Path.Combine(project, call.Resume + ".jsonl")).ToList();
        string history = string.Join("\n", lines);
        lines.Add(SlipTestFixtures.Typed(Guid.NewGuid().ToString(), message, SlipTestFixtures.T2, cwd, id).ToJsonString());
        lines.Add(SlipTestFixtures.Hook(Hook(message), id).ToJsonString());
        foreach (string notice in Notices)
            lines.Add(SlipTestFixtures.Attachment(new JsonObject { ["type"] = notice }, id).ToJsonString());
        if (Thinking(history) is { } t)
            lines.Add(SlipTestFixtures.Attachment(new JsonObject
            {
                ["type"] = "thinking_drop", ["thinkingBlocksSent"] = t.Sent, ["newlyDropped"] = new JsonObject { ["blockCount"] = t.Dropped },
            }, id).ToJsonString());
        lines.Add(SlipTestFixtures.Assistant(id, new JsonObject { ["type"] = "tool_use", ["id"] = "t9", ["name"] = "Bash", ["input"] = new JsonObject { ["command"] = "dmc roll 1d20" } }).ToJsonString());
        lines.Add(SlipTestFixtures.Assistant(id, SlipTestFixtures.TextBlock($"Reply to: {message}")).ToJsonString());
        File.WriteAllLines(Path.Combine(project, id + ".jsonl"), lines);
        raw = new JsonObject
        {
            ["type"] = "result", ["session_id"] = id, ["total_cost_usd"] = 0.02, ["is_error"] = Fails,
            ["usage"] = new JsonObject { ["input_tokens"] = 5, ["output_tokens"] = 100, ["cache_read_input_tokens"] = 1000, ["cache_creation_input_tokens"] = 50 },
        }.ToJsonString();
        return ClaudeRunner.Parse(raw);
    }
}
