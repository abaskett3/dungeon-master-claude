using System.Text;
using System.Text.RegularExpressions;
using Dmc.Maps;
using Dmc.SessionLog;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>One API call of a reply: the context it read, and what its tool calls did (see <c>Report.ExtraCalls</c>).</summary>
/// <param name="ctx">The context size of the call: input tokens plus those read from and written to the cache.</param>
internal sealed class Call(double ctx)
{
    /// <summary>The context size this call read.</summary>
    public double Ctx { get; } = ctx;

    /// <summary>What its tool calls did, as <see cref="DigestText.CallKind"/> names them (<c>roll</c>, <c>write</c>, <c>lookup</c>, <c>other</c>).</summary>
    public HashSet<string> Kinds { get; } = [];
}

/// <summary>A message the player typed, with the reply that followed it.</summary>
/// <param name="time">The time of day (HH:MM:SS, UTC) the message was written, cut from the record's timestamp.</param>
/// <param name="ctx">The context size of the last API call before this message.</param>
/// <param name="text">The message.</param>
/// <param name="skills">The slash commands the message holds (the names in <c>&lt;command-name&gt;</c> tags).</param>
internal sealed class Turn(string time, double ctx, string text, HashSet<PyKey> skills)
{
    /// <summary>The time of day the message was written (HH:MM:SS, UTC).</summary>
    public string Time { get; } = time;

    /// <summary>The context size of the last API call before this message.</summary>
    public double Ctx { get; } = ctx;

    /// <summary>The message as the player typed it.</summary>
    public string Text { get; } = text;

    /// <summary>The characters of text in the replies to this message, in code points.</summary>
    public long ReplyChars { get; set; }

    /// <summary>The tool calls of the reply, each as the tool's name, with <c>:file</c> after it when the call names a file.</summary>
    public List<string> Tools { get; } = [];

    /// <summary>The API calls of the reply, in order.</summary>
    public List<Call> Calls { get; } = [];

    /// <summary>The skills run in this turn: those the message names, and those the reply called.</summary>
    public HashSet<PyKey> Skills { get; } = skills;

    /// <summary>The files read, edited or written in the reply, in order, with the kind of call.</summary>
    public List<(string Kind, string Target)> Files { get; } = [];

    /// <summary>The characters sent in edits and writes of <c>game-state.md</c> in this turn.</summary>
    public long StateChars { get; set; }
}

/// <summary>
/// The four token counts of an API call. Counts are numbers as Python adds them: whole numbers, unless the log writes a float.
/// </summary>
/// <param name="Input">Input tokens that were not cached.</param>
/// <param name="Output">Output tokens.</param>
/// <param name="CacheRead">Input tokens read from the cache.</param>
/// <param name="CacheCreation">Input tokens written to the cache.</param>
internal readonly record struct Usage(double Input, double Output, double CacheRead, double CacheCreation);

/// <summary>
/// One Claude Code session log (<c>Session</c> of <c>transcript_digest.py</c>): every record is read once, and what the
/// digest reports is gathered as it goes. A line that is not JSON is skipped, as the script skips it. Claude Code
/// writes one record for each content block of a message, repeating the message id, so a call is counted once by its id.
/// </summary>
internal sealed partial class Session
{
    /// <summary>The campaign a record mentions: the folder name after <c>adventures/</c>.</summary>
    [GeneratedRegex("adventures/([a-z0-9][a-z0-9-]*)/")]
    private static partial Regex CampaignPattern();

    /// <summary>A slash command in a player message, as Claude Code writes it in <c>&lt;command-name&gt;</c> or <c>&lt;command-message&gt;</c> tags. Group 1 is the name, without its "/".</summary>
    [GeneratedRegex(@"<command-(?:name|message)>/?([\p{L}\p{Nd}\p{Nl}\p{No}_:-]+)")]
    private static partial Regex SlashSkill();

    /// <summary>The file whose edits are counted separately in the statistics.</summary>
    public const string StateFile = "game-state.md";

    /// <summary>
    /// The skill that marks a message as a full save: <c>dmc-campaign-full-save</c> since the player skills were renamed for
    /// the DMC folder, and <c>campaign-full-save</c> in the logs of the sessions played before that, which still digest.
    /// </summary>
    public static readonly string[] SaveSkills = ["dmc-campaign-full-save", "campaign-full-save"];

    /// <summary>The skill that marks a message as a resume: <c>dmc-resume-campaign</c>, or <c>resume-campaign</c> in an older log.</summary>
    public static readonly string[] ResumeSkills = ["dmc-resume-campaign", "resume-campaign"];

    /// <summary>Whether a turn used one of these skills, by the skill's name as the log spells it.</summary>
    /// <param name="skills">The skills the turn used.</param>
    /// <param name="names">The names that count: <see cref="SaveSkills"/> or <see cref="ResumeSkills"/>.</param>
    public static bool UsedAny(HashSet<PyKey> skills, string[] names) => names.Any(n => skills.Contains(n));

    /// <summary>The log file's path.</summary>
    public string Path { get; }

    /// <summary>The session id: the log's file name without <c>.jsonl</c>.</summary>
    public string Id { get; }

    /// <summary>The log's size in bytes.</summary>
    public long Size { get; }

    /// <summary>The first timestamp in the log (an ISO 8601 text), or "" if no record has one.</summary>
    public string Start { get; private set; } = "";

    /// <summary>The last timestamp in the log, or "".</summary>
    public string End { get; private set; } = "";

    /// <summary>How often each campaign folder is mentioned in the log.</summary>
    public Counter Campaigns { get; } = new();

    /// <summary>The model of each API call, counted once per message id.</summary>
    public Counter Models { get; } = new();

    /// <summary>The usage of each API call by message id, the last record of an id winning.</summary>
    public Dictionary<PyKey, PyDict> Calls { get; } = [];

    /// <summary>The context size of each API call, in order, counted once per message id.</summary>
    public List<double> Ctx { get; } = [];

    /// <summary>How many times the context was compacted.</summary>
    public int Compactions { get; private set; }

    /// <summary>How many player messages say the request was interrupted.</summary>
    public int Interrupts { get; private set; }

    /// <summary>The tool calls, by tool name.</summary>
    public Counter Tools { get; } = new();

    /// <summary>The Skill tool calls, by skill name.</summary>
    public Counter Skills { get; } = new();

    /// <summary>The Edit and Write calls, by file name.</summary>
    public Counter Writes { get; } = new();

    /// <summary>The characters sent to Edit and Write, by file name.</summary>
    public Counter WriteChars { get; } = new();

    /// <summary>The Read calls, by file name.</summary>
    public Counter Reads { get; } = new();

    /// <summary>The characters Read returned, by file name.</summary>
    public Counter ReadChars { get; } = new();

    /// <summary>The characters of every tool's result, by tool name.</summary>
    public Counter ResultChars { get; } = new();

    /// <summary>One line for each tool result flagged as an error, in order.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>How many Edit calls ended in an error.</summary>
    public int FailedEdits { get; private set; }

    /// <summary>The player's messages, in order.</summary>
    public List<Turn> Turns { get; } = [];

    /// <summary>The lines of the readable transcript (<c>&lt;id&gt;-transcript.md</c>), in order. Some start with a line break, to leave a blank line before them.</summary>
    public List<string> Lines { get; } = [];

    /// <summary>Reads the whole log at once, so every property is final when the constructor returns.</summary>
    /// <param name="path">A session log, a <c>.jsonl</c> file.</param>
    /// <exception cref="DmcException">The log is not valid UTF-8, or a record has a shape the digest cannot read (the message names the file and line).</exception>
    public Session(string path)
    {
        Path = path;
        Id = System.IO.Path.GetFileName(path)[..^".jsonl".Length];
        Size = new FileInfo(path).Length;
        Parse();
    }

    /// <summary>The campaign named most often in the log (the first one named if two tie), or "-".</summary>
    public string Campaign() => Campaigns.IsEmpty ? "-" : Campaigns.MostCommon(1)[0].Key.Display;

    /// <summary>
    /// The token counts summed over the calls. (A count that is not a number stops the script here; dmc has already refused
    /// such a log when it read the record, so every count summed here is a finite number.)
    /// </summary>
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

    /// <summary>Opens the log and reads it, turning the two ways it can be unreadable into a <see cref="DmcException"/>.</summary>
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

    /// <summary>
    /// Reads every record in order and hands it to <see cref="Assistant"/> or <see cref="User"/>; compaction
    /// boundaries are counted here. Other record types are ignored.
    /// </summary>
    /// <param name="reader">The log, read line by line. A line that is not JSON is skipped.</param>
    /// <param name="lineNumber">The number of the line being read, so a caller can say where a <see cref="LogShapeException"/> happened.</param>
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

    /// <summary>The message of the shape error for a token count that is NaN or infinite (a log can spell both), or a sum of counts that overflows.</summary>
    const string NotFinite = "a token count that is not a finite number";

    /// <summary>
    /// One token count of a <c>usage</c> object, 0 when missing: <see cref="DigestText.Number"/>, and finite. A count that
    /// is NaN or infinite would make Python's <c>"%d"</c> fail in the statistics, so it is refused where it is read.
    /// </summary>
    /// <param name="usage">The <c>usage</c> object of an assistant message.</param>
    /// <param name="key">The count's name, such as <c>output_tokens</c>.</param>
    /// <exception cref="LogShapeException">The count is not a number (<c>"output_tokens" is not a number</c>), or is NaN or infinite.</exception>
    static double Count(PyDict usage, string key)
    {
        double count = Number(usage, key);
        return double.IsFinite(count) ? count : throw new LogShapeException(NotFinite);
    }

    /// <summary>
    /// An assistant record: notes the API call (once per message id), writes the DM's text to the transcript and hands
    /// each tool call to <see cref="ToolUse"/>. Checks that its token counts are finite numbers (see <see cref="Count"/>).
    /// </summary>
    /// <exception cref="LogShapeException">A token count is not a finite number, or a content block has the wrong shape.</exception>
    /// <param name="msg">The record's <c>message</c>.</param>
    /// <param name="clock">The time of day of the record.</param>
    /// <param name="toolOf">Tool calls seen so far, by id, for matching their results.</param>
    /// <param name="callOf">The calls of the current turn, by message id.</param>
    /// <param name="seenIds">The message ids already counted.</param>
    /// <returns>The context size of this call, which the next player message is recorded with.</returns>
    double Assistant(PyDict msg, string clock, Dictionary<PyKey, ToolInfo> toolOf, Dictionary<PyKey, Call> callOf, HashSet<PyKey> seenIds)
    {
        PyDict usage = DictOrEmpty(msg, "usage");
        // Every count the statistics add up or print is checked here, in the record that holds it, so a bad one is reported
        // with its line, by the same catch as every other shape error, whichever command reads the log.
        double ctxNow = Count(usage, "input_tokens") + Count(usage, "cache_read_input_tokens") + Count(usage, "cache_creation_input_tokens");
        if (!double.IsFinite(ctxNow))
            throw new LogShapeException(NotFinite);
        Count(usage, "output_tokens");
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

    /// <summary>
    /// A tool call: counts it by tool, skill and file, remembers it so its result can be matched, and, once a player
    /// has spoken, adds it to the current turn.
    /// </summary>
    /// <param name="block">The <c>tool_use</c> content block.</param>
    /// <param name="clock">The time of day of the record.</param>
    /// <param name="mid">The id of the API message the call belongs to.</param>
    /// <param name="toolOf">Tool calls by id; this adds the new one.</param>
    /// <param name="callOf">The calls of the current turn, by message id.</param>
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

    /// <summary>The characters an Edit or Write call sent: the old and new text of an edit, or the content of a write.</summary>
    static long CharsSent(PyDict args) => Len(args, "old_string") + Len(args, "new_string") + Len(args, "content");

    /// <summary>
    /// A user record, which holds the player's messages and the results of tool calls. A result is counted against its
    /// tool; a text block is a compaction summary, an injected message (hook, system reminder, slash-command echo) or
    /// a real player message, which starts a new <see cref="Turn"/>.
    /// </summary>
    /// <param name="msg">The record's <c>message</c>.</param>
    /// <param name="rec">The whole record.</param>
    /// <param name="clock">The time of day of the record.</param>
    /// <param name="ctxNow">The context size of the last API call so far.</param>
    /// <param name="toolOf">Tool calls by id, to find what a result belongs to.</param>
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
                else if (Py.Truthy(Get(rec, "isMeta")) || UserText.IsInjected(text))
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
