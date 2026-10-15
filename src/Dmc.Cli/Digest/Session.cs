using System.Text;
using System.Text.RegularExpressions;
using Dmc.Maps;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>One API call of a reply: the context it read, and what its tool calls did (see <c>Report.ExtraCalls</c>).</summary>
internal sealed class Call(double ctx)
{
    public double Ctx { get; } = ctx;

    public HashSet<string> Kinds { get; } = [];
}

/// <summary>A message the player typed, with the reply that followed it.</summary>
internal sealed class Turn(string time, double ctx, string text, HashSet<PyKey> skills)
{
    public string Time { get; } = time;

    /// <summary>The context size of the last API call before this message.</summary>
    public double Ctx { get; } = ctx;

    public string Text { get; } = text;

    public long ReplyChars { get; set; }

    public List<string> Tools { get; } = [];

    public List<Call> Calls { get; } = [];

    public HashSet<PyKey> Skills { get; } = skills;

    /// <summary>The files read, edited or written in the reply, in order, with the kind of call.</summary>
    public List<(string Kind, string Target)> Files { get; } = [];

    public long StateChars { get; set; }
}

/// <summary>
/// The four token counts of an API call. Counts are numbers as Python adds them: whole numbers, unless the log writes a float.
/// </summary>
internal readonly record struct Usage(double Input, double Output, double CacheRead, double CacheCreation);

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

    /// <summary>The usage of each API call by message id, the last record of an id winning.</summary>
    public Dictionary<PyKey, PyDict> Calls { get; } = [];

    public List<double> Ctx { get; } = [];
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
    public string Campaign() => Campaigns.IsEmpty ? "-" : Campaigns.MostCommon(1)[0].Key.Display;

    /// <summary>The token counts summed over the calls. (A count that is not a number stops the script here, and so it does dmc.)</summary>
    public Usage Tokens()
    {
        double input = 0, output = 0, cacheRead = 0, cacheCreation = 0;
        foreach (var usage in Calls.Values)
        {
            input += Number(usage, "input_tokens");
            output += Number(usage, "output_tokens");
            cacheRead += Number(usage, "cache_read_input_tokens");
            cacheCreation += Number(usage, "cache_creation_input_tokens");
        }
        return new Usage(input, output, cacheRead, cacheCreation);
    }

    void Parse()
    {
        // No byte-order mark is skipped: Python reads it as a character, and then the first line is not JSON. A byte
        // that is not UTF-8 stops the digest, where the script stops with a traceback. ReadLine ends a line at \n, \r\n or a
        // lone \r, as Python's universal newlines do. The log of a session still going is being appended to by Claude Code,
        // so it is opened the way ContextCheck's reader opens it: sharing everything, so that neither side is locked out.
        string name = System.IO.Path.GetFileName(Path);
        int number = 0;
        try
        {
            using var file = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(file, new UTF8Encoding(false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: false);
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

    /// <summary>A tool call seen: what it was called (any JSON value), and the file it names (the empty string for none).</summary>
    readonly record struct ToolInfo(PyKey Name, string Target);

    void ParseLines(StreamReader reader, ref int lineNumber)
    {
        var toolOf = new Dictionary<PyKey, ToolInfo>(); // tool_use id -> (name, file)
        var callOf = new Dictionary<PyKey, Call>();     // API message id -> its entry in the turn's calls
        var seenIds = new HashSet<PyKey>();
        double ctxNow = 0;
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
            string kind = TypeOf(rec, "type");

            if (kind == "system")
            {
                if (TypeOf(rec, "subtype") == "compact_boundary")
                {
                    Compactions++;
                    Lines.Add($"\n--- CONTEXT COMPACTED {clock}");
                }
                continue;
            }

            if (kind == "assistant")
            {
                ctxNow = Assistant(DictOrEmpty(rec, "message"), clock, toolOf, callOf, seenIds);
                continue;
            }

            if (kind != "user")
                continue;
            User(DictOrEmpty(rec, "message"), rec, clock, ctxNow, toolOf);
        }
    }

    double Assistant(PyDict msg, string clock, Dictionary<PyKey, ToolInfo> toolOf, Dictionary<PyKey, Call> callOf, HashSet<PyKey> seenIds)
    {
        PyDict usage = DictOrEmpty(msg, "usage");
        double ctxNow = Number(usage, "input_tokens") + Number(usage, "cache_read_input_tokens") + Number(usage, "cache_creation_input_tokens");
        var mid = PyKey.Of(Get(msg, "id"));
        Calls[mid] = usage;
        if (seenIds.Add(mid))
        {
            Ctx.Add(ctxNow);
            Models.Add(PyKey.Of(Get(msg, "model")));
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
            string type = TypeOf(block, "type");
            if (type == "text" && Py.Strip(TextField(block, "text")).Length > 0)
            {
                string text = TextField(block, "text");
                Lines.Add($"\n### DM {clock} [ctx {Whole(ctxNow)}]\n{Cut(text, 6000)}");
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

    void ToolUse(PyDict block, string clock, PyKey mid, Dictionary<PyKey, ToolInfo> toolOf, Dictionary<PyKey, Call> callOf)
    {
        // A tool's name, id and skill are whatever the log holds (a null is a key of its own, as in Python); only a name
        // that is not text stops the digest, and only once a player has spoken, where the script adds it to a string.
        object? nameValue = block.TryGetValue("name", out var named) ? named : "?";
        string? name = nameValue as string;
        PyDict args = DictOrEmpty(block, "input");
        string target = args.ContainsKey("file_path") ? BaseName(StrOf(args, "file_path")) : "";
        var tool = PyKey.Of(nameValue);
        toolOf[PyKey.Of(Get(block, "id"))] = new ToolInfo(tool, target);
        Tools.Add(tool);
        var skill = default(PyKey);
        if (name == "Skill")
        {
            skill = PyKey.Of(args.TryGetValue("skill", out var skillValue) ? skillValue : "?");
            Skills.Add(skill);
        }
        if (name is "Edit" or "Write")
        {
            Writes.Add(target);
            WriteChars.Add(target, CharsSent(args));
        }
        if (name == "Read")
            Reads.Add(target);
        Lines.Add($"  -> {tool.Display} {DescribeCall(name, args)}");
        if (Turns.Count == 0)
            return;

        var turn = Turns[^1];
        turn.Tools.Add((name ?? throw new LogShapeException("a tool call whose name is not text")) + (target.Length > 0 ? ":" + target : ""));
        string kind = CallKind(name, args);
        if (callOf.TryGetValue(mid, out var call))
            call.Kinds.Add(kind);
        if (name == "Skill")
            turn.Skills.Add(skill);
        if (target.Length > 0 && name is "Read" or "Edit" or "Write")
            turn.Files.Add((kind, target));
        if (target == StateFile && kind == "write")
            turn.StateChars += CharsSent(args);
    }

    static long CharsSent(PyDict args) => Len(args, "old_string") + Len(args, "new_string") + Len(args, "content");

    void User(PyDict msg, PyDict rec, string clock, double ctxNow, Dictionary<PyKey, ToolInfo> toolOf)
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
            string type = TypeOf(block, "type");
            if (type == "tool_result")
            {
                var (tool, target) = toolOf.TryGetValue(PyKey.Of(Get(block, "tool_use_id")), out var known) ? known : new ToolInfo("?", "");
                string name = tool.Display;
                var result = Get(block, "content");
                long size = Py.Len(PyJson.Dumps(result));
                ResultChars.Add(tool, size);
                if (tool.Value as string == "Read")
                    ReadChars.Add(target, size);
                string text = ResultText(result);
                string flag = "";
                if (Py.Truthy(Get(block, "is_error")))
                {
                    flag = " ERROR";
                    if (tool.Value as string == "Edit")
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
                    var skills = SlashSkill().Matches(text).Select(m => PyKey.Of(m.Groups[1].Value)).ToHashSet();
                    Turns.Add(new Turn(clock, ctxNow, text, skills));
                }
            }
        }
    }
}
