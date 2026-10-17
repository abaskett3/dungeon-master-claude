using System.Text.Json;
using System.Text.Unicode;

namespace Dmc.ContextCheck;

/// <summary>
/// Finds the context size of the last API call in a Claude Code session log (<c>.jsonl</c>), as
/// <c>scripts/context_check.py</c> did, and the baseline the break reminder is measured from. The size is the last
/// assistant record's input tokens plus the tokens read from and written to the cache. This runs before every
/// player message and reads the log from the end in chunks: <see cref="LastContext"/> stops at the first answer, one
/// read; <see cref="Read"/> goes on back to the session's resume, which is usually near the start of the log.
/// <para>
/// <b>The baseline</b> is where the context settled after the session loaded its files. It is found from the log
/// alone, in the part of it after the latest compaction boundary (a <c>/clear</c> starts a new log, so it needs no
/// rule of its own):
/// </para>
/// <list type="number">
/// <item>Find the most recent resume: a <c>/resume-campaign</c> command echoed in a user record, or a call of the
/// <c>Skill</c> tool for <c>resume-campaign</c> by DMC (how it re-runs after a compaction).</item>
/// <item>The baseline is the context of the last assistant call before the first real player message after that
/// resume, the end of the recap. A real player message is a user record that is not a tool result, a slash-command
/// echo, a hook or system injection or a compaction summary. If no player message has followed yet, it is the
/// latest context.</item>
/// <item>With no resume since the start of the log (or since the compaction), it is the context of the first
/// assistant reply in that part of the log.</item>
/// </list>
/// </summary>
public static class TranscriptReader
{
    /// <summary>Bytes of the log read at a time, from the end.</summary>
    public const int ChunkSize = 1 << 20;

    /// <summary>The UTF-8 byte-order mark, dropped from the start of a line.</summary>
    static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>How each log line is parsed: deeper nesting than <see cref="JsonDocument"/> allows by default.</summary>
    // Python's json.loads allows nesting up to its recursion limit, about 1000; JsonDocument stops at 64 by default.
    static readonly JsonDocumentOptions Options = new() { MaxDepth = 1000 };

    /// <summary>
    /// Context of the last call in the log; 0 if a compaction came after it or there is none. Reads the log from
    /// the end in <see cref="ChunkSize"/> pieces and stops at the first answer, so a long log costs one read.
    /// Throws on anything it cannot read, and on a record whose shape the script would also trip over: the caller
    /// treats every failure as "print nothing".
    /// </summary>
    /// <param name="path">The session log, a file of one JSON record per line. It may be growing while it is read.</param>
    /// <returns>The context size in tokens, or 0 when a compaction boundary is the latest thing in the log or no call is in it.</returns>
    public static long LastContext(string path)
    {
        long context = 0;
        ScanBackwards(path, line =>
        {
            var found = Classify(line);
            if (found.Kind == RecordKind.Compaction)
                return true; // context stays 0
            if (found.Kind != RecordKind.Assistant)
                return false;
            context = found.Context;
            return true;
        });
        return context;
    }

    /// <summary>
    /// The latest context size and the baseline the break reminder is measured from (see the class summary for how
    /// the baseline is found). Reads back from the end of the log as far as the latest resume, the latest compaction
    /// boundary or the start of the log, whichever comes first. A record of an unexpected shape throws if it is the
    /// one that gives the latest size, as for <see cref="LastContext"/>; further back it is skipped, since it
    /// cannot change the reminder by more than the baseline, which the reminder can do without.
    /// </summary>
    /// <param name="path">The session log. It may be growing while it is read.</param>
    /// <returns>
    /// The reading: size 0 when a compaction boundary is the latest thing in the log or no call is in it. The
    /// baseline is 0 when no call is in the log.
    /// </returns>
    public static ContextReading Read(string path)
    {
        long size = -1;          // the latest context; -1 until the first call is seen from the end
        long first = 0;          // the earliest call seen so far: the baseline when there is no resume
        long afterRecap = 0;     // the call before the earliest player message seen so far
        bool playerSeen = false; // a player message was seen and the call before it has not yet been
        bool resumed = false;

        ScanBackwards(path, line =>
        {
            Classified found;
            try
            {
                found = Classify(line);
            }
            catch (InvalidDataException) when (size >= 0)
            {
                return false;
            }

            switch (found.Kind)
            {
                case RecordKind.Compaction:
                    if (size < 0)
                        size = 0;
                    return true;
                case RecordKind.Assistant:
                    if (size < 0)
                        size = found.Context;
                    first = found.Context;
                    if (playerSeen)
                    {
                        afterRecap = found.Context;
                        playerSeen = false;
                    }
                    if (found.CallsResume)
                        resumed = true;
                    return resumed;
                case RecordKind.Player:
                    playerSeen = true;
                    return false;
                case RecordKind.ResumeCommand:
                    resumed = true;
                    return true;
                default:
                    return false;
            }
        });

        size = Math.Max(size, 0);
        return new ContextReading(size, resumed && afterRecap > 0 ? afterRecap : resumed ? size : first);
    }

    /// <summary>
    /// Calls <paramref name="visit"/> on each line of the log, the last line first, until it returns true. Reads the
    /// log in <see cref="ChunkSize"/> pieces from the end, so stopping early costs only the pieces read.
    /// </summary>
    /// <param name="path">The session log. It may be growing while it is read.</param>
    /// <param name="visit">Takes one line (without its newline) and returns true to stop.</param>
    static void ScanBackwards(string path, Func<ReadOnlyMemory<byte>, bool> visit)
    {
        // The log is being written by Claude Code while it is read: share it fully.
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        long end = file.Length;

        // The pieces of a line that crosses chunk boundaries, the latest in the file first. A line is put together
        // once, when it is complete: a line longer than a chunk costs one copy, not one per chunk.
        var carry = new List<ReadOnlyMemory<byte>>();

        while (end > 0)
        {
            long start = Math.Max(0, end - ChunkSize);
            var block = new byte[(int)(end - start)];
            file.Seek(start, SeekOrigin.Begin);
            file.ReadExactly(block);
            end = start;

            // Lines are taken from the end of the block. What follows its last newline continues into the carry;
            // what precedes its first newline continues into an earlier chunk (unless this one starts the file).
            int limit = block.Length;
            while (true)
            {
                int newline = block.AsSpan(0, limit).LastIndexOf((byte)'\n');
                var piece = new ReadOnlyMemory<byte>(block, newline + 1, limit - (newline + 1));

                // The piece joins the carry if it is the end of the chunk (it continues into the later chunks'
                // pieces) or the start of it with no newline before it (it continues into an earlier chunk).
                bool startsInEarlierChunk = newline < 0 && end > 0;
                bool joinsCarry = limit == block.Length || startsInEarlierChunk;
                if (joinsCarry)
                    carry.Add(piece);
                if (startsInEarlierChunk)
                    break;

                ReadOnlyMemory<byte> line = piece;
                if (joinsCarry)
                {
                    line = Assemble(carry);
                    carry.Clear();
                }

                if (visit(line))
                    return;
                if (newline < 0)
                    break;
                limit = newline;
            }
        }
    }

    /// <summary>Joins the pieces of one line into a single buffer.</summary>
    /// <param name="pieces">The pieces, the latest in the file first (the order <see cref="LastContext"/> collects them in).</param>
    /// <returns>The line in file order; the one piece itself, uncopied, when there is only one.</returns>
    static ReadOnlyMemory<byte> Assemble(List<ReadOnlyMemory<byte>> pieces)
    {
        if (pieces.Count == 1)
            return pieces[0];
        var line = new byte[pieces.Sum(p => p.Length)];
        int at = 0;
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            pieces[i].CopyTo(line.AsMemory(at));
            at += pieces[i].Length;
        }
        return line;
    }

    /// <summary>
    /// What one log line is, for the scans: a compaction boundary, an assistant call (with its context size), a
    /// real player message, the echo of a <c>/resume-campaign</c> command, or nothing that matters. A line that is
    /// not a JSON object, or does not count, is <see cref="RecordKind.Skipped"/>.
    /// </summary>
    /// <param name="line">One log line, without its newline.</param>
    /// <returns>The classification; a default value (<see cref="RecordKind.Skipped"/>) for a line that does not matter.</returns>
    /// <exception cref="InvalidDataException">An assistant record has a shape the old script would also trip over.</exception>
    static Classified Classify(ReadOnlyMemory<byte> line)
    {
        // Python decodes a line as UTF-8-with-BOM before parsing it, so a BOM is dropped...
        if (line.Span.StartsWith(Utf8Bom))
            line = line[3..];
        if (!CouldBeRelevant(line.Span))
            return default;
        // ...and bytes that are not UTF-8 anywhere, inside a string or not, make the whole line junk.
        if (!Utf8.IsValid(line.Span))
            return default;

        using var document = Parse(line);
        if (document is null)
            return default; // a cut last line, a blank line or junk

        var record = document.RootElement;
        if (record.ValueKind != JsonValueKind.Object)
            return default;
        if (Text(record, "type") == "system" && Text(record, "subtype") == "compact_boundary")
            return new Classified(RecordKind.Compaction);
        if (Text(record, "type") == "user")
            return new Classified(UserKind(record));

        double context = ContextOf(record);
        if (context == 0)
            return default; // synthetic messages (errors, interruptions) carry zero usage
        return new Classified(RecordKind.Assistant, (long)Math.Floor(context), CallsResumeSkill(record));
    }

    /// <summary>
    /// What a user record is. Never throws: a shape it does not know is skipped. A real player message has text
    /// (a string, or a text block) and is none of: a sub-agent's message, a meta record (the skill text a command
    /// loads), a compaction summary, a slash-command echo, a local-command output or a system reminder.
    /// </summary>
    /// <param name="record">A record whose <c>type</c> is <c>user</c>.</param>
    /// <returns><see cref="RecordKind.Player"/>, <see cref="RecordKind.ResumeCommand"/> or <see cref="RecordKind.Skipped"/>.</returns>
    static RecordKind UserKind(JsonElement record)
    {
        if ((record.TryGetProperty("isSidechain", out var side) && Truthy(side))
            || (record.TryGetProperty("isMeta", out var meta) && Truthy(meta))
            || (record.TryGetProperty("isCompactSummary", out var summary) && Truthy(summary)))
            return RecordKind.Skipped;
        if (!record.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("content", out var content))
            return RecordKind.Skipped;

        string? text = content.ValueKind switch
        {
            JsonValueKind.String => content.GetString(),
            JsonValueKind.Array => content.EnumerateArray()
                .Where(block => block.ValueKind == JsonValueKind.Object && Text(block, "type") == "text")
                .Select(block => Text(block, "text")).FirstOrDefault(),
            _ => null, // an array of tool results only has no text
        };
        if (string.IsNullOrEmpty(text))
            return RecordKind.Skipped;
        if (text.StartsWith("<command-", StringComparison.Ordinal))
            return text.Contains("<command-name>/resume-campaign</command-name>", StringComparison.Ordinal)
                ? RecordKind.ResumeCommand
                : RecordKind.Skipped;
        return text.StartsWith("<local-command", StringComparison.Ordinal)
               || text.StartsWith("<system-reminder>", StringComparison.Ordinal)
            ? RecordKind.Skipped
            : RecordKind.Player;
    }

    /// <summary>True when an assistant record calls the <c>Skill</c> tool for <c>resume-campaign</c>: how DMC re-runs the resume after a compaction.</summary>
    /// <param name="record">An assistant record.</param>
    /// <returns>True when one of its content blocks is that tool call.</returns>
    static bool CallsResumeSkill(JsonElement record) =>
        record.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
        && message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array
        && content.EnumerateArray().Any(block =>
            block.ValueKind == JsonValueKind.Object && Text(block, "type") == "tool_use" && Text(block, "name") == "Skill"
            && block.TryGetProperty("input", out var input) && input.ValueKind == JsonValueKind.Object
            && Text(input, "skill") == "resume-campaign");

    /// <summary>
    /// A cheap test that skips most lines (attachments, snapshots) without parsing them. It may only say no for a
    /// line that cannot be an assistant record, a user record or a compaction boundary, so a "no" never changes
    /// the answer. Those records need the words "assistant", "user" or "compact_boundary" in the log, unless a
    /// letter of them is written as a <c>\uXXXX</c> escape, which JSON allows: any line with <c>\u</c> in it is parsed.
    /// </summary>
    /// <param name="line">One log line.</param>
    /// <returns>False only when the line cannot matter.</returns>
    static bool CouldBeRelevant(ReadOnlySpan<byte> line) =>
        line.IndexOf("assistant"u8) >= 0 || line.IndexOf("user"u8) >= 0 || line.IndexOf("compact_boundary"u8) >= 0
        || line.IndexOf("\\u"u8) >= 0;

    /// <summary>Parses one log line as JSON the way Python's <c>json.loads</c> would, including its <c>NaN</c> and <c>Infinity</c>.</summary>
    /// <returns>The document, or null when the line is not JSON (a cut last line, a blank one, junk). The caller disposes it.</returns>
    static JsonDocument? Parse(ReadOnlyMemory<byte> line)
    {
        try
        {
            return JsonDocument.Parse(line, Options);
        }
        catch (JsonException)
        {
        }

        // Python also reads NaN, Infinity and -Infinity. Written as strings they are just as unusable as a token
        // count, and they make a record that holds them parse, as it does in Python.
        if (line.Span.IndexOf("NaN"u8) < 0 && line.Span.IndexOf("Infinity"u8) < 0)
            return null;
        try
        {
            return JsonDocument.Parse(QuoteNonFiniteNumbers(line.Span), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Replace the bare words NaN, Infinity and -Infinity (outside strings) with the string "NaN".</summary>
    static byte[] QuoteNonFiniteNumbers(ReadOnlySpan<byte> json)
    {
        var result = new List<byte>(json.Length + 16);
        bool inString = false;
        for (int i = 0; i < json.Length; i++)
        {
            byte b = json[i];
            if (inString)
            {
                result.Add(b);
                if (b == '\\' && i + 1 < json.Length)
                    result.Add(json[++i]);
                else if (b == '"')
                    inString = false;
                continue;
            }

            var rest = json[i..];
            if (b == '"')
            {
                inString = true;
                result.Add(b);
            }
            else if (rest.StartsWith("NaN"u8) || rest.StartsWith("Infinity"u8) || rest.StartsWith("-Infinity"u8))
            {
                result.AddRange("\"NaN\""u8.ToArray());
                i += (b == '-' ? "-Infinity"u8 : rest.StartsWith("NaN"u8) ? "NaN"u8 : "Infinity"u8).Length - 1;
            }
            else
            {
                result.Add(b);
            }
        }
        return result.ToArray();
    }

    /// <summary>The context size of an assistant record from the main conversation; 0 if it is anything else.</summary>
    static double ContextOf(JsonElement record)
    {
        if (Text(record, "type") != "assistant" || (record.TryGetProperty("isSidechain", out var side) && Truthy(side)))
            return 0;
        if (!TryObject(record, "message", out var message) || !TryObject(message, "usage", out var usage))
            return 0;
        return Tokens(usage, "input_tokens") + Tokens(usage, "cache_read_input_tokens")
            + Tokens(usage, "cache_creation_input_tokens");
    }

    /// <summary>The string under <paramref name="name"/>; null when it is missing or is not a string.</summary>
    static string? Text(JsonElement record, string name) =>
        record.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// The object under <paramref name="name"/>; false when it is missing or empty (null, 0, "", []). A value of
    /// any other kind throws, as the script does when it calls <c>.get</c> on a string or a number.
    /// </summary>
    static bool TryObject(JsonElement parent, string name, out JsonElement value)
    {
        if (parent.TryGetProperty(name, out value))
        {
            if (value.ValueKind == JsonValueKind.Object)
                return true;
            if (Truthy(value))
                throw new InvalidDataException($"\"{name}\" is not an object");
        }
        return false;
    }

    /// <summary>A token count: 0 when absent. A value that is not a number (or true/false) throws, as in the script.</summary>
    static double Tokens(JsonElement usage, string name)
    {
        if (!usage.TryGetProperty(name, out var value))
            return 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => throw new InvalidDataException($"\"{name}\" is not a number"),
        };
    }

    /// <summary>Python's truthiness, for the JSON values the log can hold.</summary>
    static bool Truthy(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.Number => value.GetDouble() != 0,
        JsonValueKind.String => value.GetString()!.Length > 0,
        JsonValueKind.Array => value.GetArrayLength() > 0,
        JsonValueKind.Object => value.EnumerateObject().Any(),
        _ => false,
    };
}

/// <summary>What <see cref="TranscriptReader"/> finds in a session log.</summary>
/// <param name="Size">The context, in tokens, of the latest call; 0 when a compaction boundary is the latest thing in the log or no call is in it.</param>
/// <param name="Baseline">The context, in tokens, where the session settled after loading its files (see <see cref="TranscriptReader"/>); 0 when no call is in the log.</param>
public readonly record struct ContextReading(long Size, long Baseline);

/// <summary>The kinds of log line the scans tell apart.</summary>
enum RecordKind
{
    /// <summary>Anything that does not matter: junk, attachments, tool results, system records, injections.</summary>
    Skipped,

    /// <summary>A <c>compact_boundary</c> system record.</summary>
    Compaction,

    /// <summary>An assistant call of the main conversation with a non-zero context.</summary>
    Assistant,

    /// <summary>A message the player typed.</summary>
    Player,

    /// <summary>The echo of a <c>/resume-campaign</c> command.</summary>
    ResumeCommand,
}

/// <summary>One classified log line.</summary>
/// <param name="Kind">What the line is.</param>
/// <param name="Context">The context size, for an assistant call; otherwise 0.</param>
/// <param name="CallsResume">True for an assistant call that invokes the <c>resume-campaign</c> skill.</param>
readonly record struct Classified(RecordKind Kind, long Context = 0, bool CallsResume = false);
