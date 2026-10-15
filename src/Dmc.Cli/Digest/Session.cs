using System.Text;
using System.Text.RegularExpressions;
using Dmc.Maps;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>One API call of a reply: the context it read, and what its tool calls did (see <c>Report.ExtraCalls</c>).</summary>
internal sealed class Call(long ctx)
{
    public long Ctx { get; } = ctx;

    public HashSet<string> Kinds { get; } = [];
}

/// <summary>A message the player typed, with the reply that followed it.</summary>
internal sealed class Turn(string time, long ctx, string text, HashSet<string> skills)
{
    public string Time { get; } = time;

    /// <summary>The context size of the last API call before this message.</summary>
    public long Ctx { get; } = ctx;

    public string Text { get; } = text;

    public long ReplyChars { get; set; }

    public List<string> Tools { get; } = [];

    public List<Call> Calls { get; } = [];

    public HashSet<string> Skills { get; } = skills;

    /// <summary>The files read, edited or written in the reply, in order, with the kind of call.</summary>
    public List<(string Kind, string Target)> Files { get; } = [];

    public long StateChars { get; set; }
}

/// <summary>The four token counts of an API call.</summary>
internal readonly record struct Usage(long Input, long Output, long CacheRead, long CacheCreation);

/// <summary>
/// One Claude Code session log (<c>Session</c> of <c>transcript_digest.py</c>): every record is read once, and what the
/// digest reports is gathered as it goes. A line that is not JSON is skipped, as the script skips it. Claude Code
/// writes one record for each content block of a message, repeating the message id, so a call is counted once by its id.
/// </summary>
internal sealed partial class Session
{
    [GeneratedRegex("adventures/([a-z0-9][a-z0-9-]*)/")]
    private static partial Regex CampaignPattern();

    [GeneratedRegex(@"<command-(?:name|message)>/?([\p{L}\p{Nd}\p{Nl}\p{No}_:-]+)")]
    private static partial Regex SlashSkill();

    public const string StateFile = "game-state.md";
    public const string SaveSkill = "campaign-full-save";
    public const string ResumeSkill = "resume-campaign";

    public string Path { get; }
    public string Id { get; }
    public long Size { get; }
    public string Start { get; private set; } = "";
    public string End { get; private set; } = "";
    public Counter Campaigns { get; } = new();
    public Counter Models { get; } = new();
    public Dictionary<string, Usage> Calls { get; } = [];
    public List<long> Ctx { get; } = [];
    public int Compactions { get; private set; }
    public int Interrupts { get; private set; }
    public Counter Tools { get; } = new();
    public Counter Skills { get; } = new();
    public Counter Writes { get; } = new();
    public Counter WriteChars { get; } = new();
    public Counter Reads { get; } = new();
    public Counter ReadChars { get; } = new();
    public Counter ResultChars { get; } = new();
    public List<string> Errors { get; } = [];
    public int FailedEdits { get; private set; }
    public List<Turn> Turns { get; } = [];
    public List<string> Lines { get; } = [];

    public Session(string path)
    {
        Path = path;
        Id = System.IO.Path.GetFileName(path)[..^".jsonl".Length];
        Size = new FileInfo(path).Length;
        Parse();
    }

    /// <summary>The campaign named most often in the log (the first one named if two tie), or "-".</summary>
    public string Campaign() => Campaigns.IsEmpty ? "-" : Campaigns.MostCommon(1)[0].Key;

    public Usage Tokens() => new(
        Calls.Values.Sum(u => u.Input), Calls.Values.Sum(u => u.Output),
        Calls.Values.Sum(u => u.CacheRead), Calls.Values.Sum(u => u.CacheCreation));

    void Parse()
    {
        // No byte-order mark is skipped: Python reads it as a character, and then the first line is not JSON. A byte
        // that is not UTF-8 stops the digest, where the script stops with a traceback. ReadLine ends a line at \n, \r\n or a
        // lone \r, as Python's universal newlines do.
        string name = System.IO.Path.GetFileName(Path);
        int number = 0;
        try
        {
            using var reader = new StreamReader(Path, new UTF8Encoding(false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: false);
            ParseLines(reader, ref number);
        }
        catch (DecoderFallbackException)
        {
            throw new DmcException($"{name}: not valid UTF-8");
        }
        catch (LogShapeException e)
        {
            throw new DmcException($"{name} line {number}: {e.Message}");
        }
    }

    void ParseLines(StreamReader reader, ref int lineNumber)
    {
        var toolOf = new Dictionary<string, (string Name, string Target)>(); // tool_use id -> (name, file)
        var callOf = new Dictionary<string, Call>();                         // API message id -> its entry in the turn's calls
        var seenIds = new HashSet<string>();
        long ctxNow = 0;
        string? raw;
        while ((raw = reader.ReadLine()) is not null)
        {
            lineNumber++;
            object? parsed;
            try
            {
                parsed = PyJson.Parse(raw);
            }
            catch (JsonDecodeError)
            {
                continue;
            }
            if (parsed is not PyDict rec)
                throw new LogShapeException("the record is not an object");

            foreach (Match match in CampaignPattern().Matches(raw.Replace("\\\\", "/", StringComparison.Ordinal)))
                Campaigns.Add(match.Groups[1].Value);
            string stamp = TextOrNull(rec, "timestamp");
            if (stamp.Length > 0)
            {
                if (Start.Length == 0)
                    Start = stamp;
                End = stamp;
            }
            string clock = Slice(stamp, 11, 19);
            string kind = KeyText(rec, "type");

            if (kind == "system")
            {
                if (KeyText(rec, "subtype") == "compact_boundary")
                {
                    Compactions++;
                    Lines.Add($"\n--- CONTEXT COMPACTED {clock}");
                }
                continue;
            }

            if (kind == "assistant")
            {
                ctxNow = Assistant(DictOrEmpty(rec, "message"), rec, clock, ctxNow, toolOf, callOf, seenIds);
                continue;
            }

            if (kind != "user")
                continue;
            User(DictOrEmpty(rec, "message"), rec, clock, ctxNow, toolOf);
        }
    }

    long Assistant(PyDict msg, PyDict rec, string clock, long ctxNow, Dictionary<string, (string Name, string Target)> toolOf,
        Dictionary<string, Call> callOf, HashSet<string> seenIds)
    {
        PyDict usageRecord = DictOrEmpty(msg, "usage");
        var usage = new Usage(Number(usageRecord, "input_tokens"), Number(usageRecord, "output_tokens"),
            Number(usageRecord, "cache_read_input_tokens"), Number(usageRecord, "cache_creation_input_tokens"));
        ctxNow = usage.Input + usage.CacheRead + usage.CacheCreation;
        string mid = KeyText(msg, "id");
        Calls[mid] = usage;
        if (seenIds.Add(mid))
        {
            Ctx.Add(ctxNow);
            Models.Add(KeyText(msg, "model"));
            if (Turns.Count > 0)
            {
                var call = new Call(ctxNow);
                callOf[mid] = call;
                Turns[^1].Calls.Add(call);
            }
        }

        foreach (object? item in ListOrEmpty(msg, "content"))
        {
            PyDict block = AsDict(item, "a content block");
            string type = KeyText(block, "type");
            if (type == "text" && Py.Strip(TextField(block, "text")).Length > 0)
            {
                string text = TextField(block, "text");
                Lines.Add($"\n### DM {clock} [ctx {ctxNow}]\n{Cut(text, 6000)}");
                if (Turns.Count > 0)
                    Turns[^1].ReplyChars += Py.Len(text);
            }
            else if (type == "tool_use")
            {
                ToolUse(block, clock, mid, toolOf, callOf);
            }
        }
        return ctxNow;
    }

    void ToolUse(PyDict block, string clock, string mid, Dictionary<string, (string Name, string Target)> toolOf,
        Dictionary<string, Call> callOf)
    {
        string name = TextField(block, "name", "?");
        PyDict args = InputOf(block);
        string target = args.ContainsKey("file_path") ? BaseName(TextField(args, "file_path")) : "";
        toolOf[KeyText(block, "id")] = (name, target);
        Tools.Add(name);
        if (name == "Skill")
            Skills.Add(TextField(args, "skill", "?"));
        if (name is "Edit" or "Write")
        {
            Writes.Add(target);
            WriteChars.Add(target, CharsSent(args));
        }
        if (name == "Read")
            Reads.Add(target);
        Lines.Add($"  -> {name} {DescribeCall(name, args)}");
        if (Turns.Count == 0)
            return;

        var turn = Turns[^1];
        turn.Tools.Add(name + (target.Length > 0 ? ":" + target : ""));
        string kind = CallKind(name, args);
        if (callOf.TryGetValue(mid, out var call))
            call.Kinds.Add(kind);
        if (name == "Skill")
            turn.Skills.Add(TextField(args, "skill", "?"));
        if (target.Length > 0 && name is "Read" or "Edit" or "Write")
            turn.Files.Add((kind, target));
        if (target == StateFile && kind == "write")
            turn.StateChars += CharsSent(args);
    }

    /// <summary>The tool call's input, which is <c>{}</c> when it is missing or empty.</summary>
    static PyDict InputOf(PyDict block) => DictOrEmpty(block, "input");

    static long CharsSent(PyDict args) => Len(args, "old_string") + Len(args, "new_string") + Len(args, "content");

    void User(PyDict msg, PyDict rec, string clock, long ctxNow, Dictionary<string, (string Name, string Target)> toolOf)
    {
        var content = Get(msg, "content");
        List<object?> blocks = content switch
        {
            string text => [new PyDict { ["type"] = "text", ["text"] = text }],
            List<object?> list => list,
            var other when !Py.Truthy(other) => [],
            _ => throw new LogShapeException("\"content\" is not text or a list"),
        };

        foreach (object? item in blocks)
        {
            PyDict block = AsDict(item, "a content block");
            string type = KeyText(block, "type");
            if (type == "tool_result")
            {
                var (name, target) = toolOf.TryGetValue(KeyText(block, "tool_use_id"), out var known) ? known : ("?", "");
                var result = Get(block, "content");
                long size = Py.Len(PyJson.Dumps(result));
                ResultChars.Add(name, size);
                if (name == "Read")
                    ReadChars.Add(target, size);
                string text = ResultText(result);
                string flag = "";
                if (Py.Truthy(Get(block, "is_error")))
                {
                    flag = " ERROR";
                    if (name == "Edit")
                        FailedEdits++;
                    Errors.Add($"{clock} {name} {target}: {OneLine(text, 160)}");
                }
                Lines.Add($"  <- {size} chars{flag}: {OneLine(text, 160)}");
            }
            else if (type == "text")
            {
                string text = TextField(block, "text");
                if (Py.Truthy(Get(rec, "isCompactSummary")))
                {
                    Lines.Add($"\n### COMPACTION SUMMARY {clock}\n{Cut(text, 4000)}");
                }
                else if (Py.Truthy(Get(rec, "isMeta"))
                         || text.StartsWith("<local-command", StringComparison.Ordinal)
                         || text.StartsWith("<command-name>", StringComparison.Ordinal)
                         || text.StartsWith("<system-reminder>", StringComparison.Ordinal))
                {
                    Lines.Add($"\n### (injected) {clock} {OneLine(text, 200)}");
                }
                else
                {
                    if (text.Contains("[Request interrupted", StringComparison.Ordinal))
                        Interrupts++;
                    Lines.Add($"\n### PLAYER {clock}\n{Cut(text, 3000)}");
                    var skills = SlashSkill().Matches(text).Select(m => m.Groups[1].Value).ToHashSet();
                    Turns.Add(new Turn(clock, ctxNow, text, skills));
                }
            }
        }
    }
}
