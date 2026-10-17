using System.Text.Json;
using System.Text.Unicode;

namespace Dmc.ContextCheck;

/// <summary>
/// Finds the context size of the last API call in a Claude Code session log (<c>.jsonl</c>), as
/// <c>scripts/context_check.py</c> does. The size is the last assistant record's input tokens plus the tokens
/// read from and written to the cache. This runs before every player message, so it reads as little as it can.
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

                if (TryRecordContext(line, out long size))
                    return size;
                if (newline < 0)
                    break;
                limit = newline;
            }
        }
        return 0;
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
    /// True when this line settles the answer: <paramref name="size"/> is 0 at a compaction boundary, or the
    /// context size of an assistant record. A line that is not a JSON object, or does not count, is skipped.
    /// </summary>
    static bool TryRecordContext(ReadOnlyMemory<byte> line, out long size)
    {
        size = 0;
        // Python decodes a line as UTF-8-with-BOM before parsing it, so a BOM is dropped...
        if (line.Span.StartsWith(Utf8Bom))
            line = line[3..];
        if (!CouldBeRelevant(line.Span))
            return false;
        // ...and bytes that are not UTF-8 anywhere, inside a string or not, make the whole line junk.
        if (!Utf8.IsValid(line.Span))
            return false;

        using var document = Parse(line);
        if (document is null)
            return false; // a cut last line, a blank line or junk

        var record = document.RootElement;
        if (record.ValueKind != JsonValueKind.Object)
            return false;
        if (Text(record, "type") == "system" && Text(record, "subtype") == "compact_boundary")
            return true; // size stays 0

        double context = ContextOf(record);
        if (context == 0)
            return false; // synthetic messages (errors, interruptions) carry zero usage
        size = (long)Math.Floor(context);
        return true;
    }

    /// <summary>
    /// A cheap test that skips most lines (user messages, tool results) without parsing them. It may only say no
    /// for a line that cannot be an assistant record or a compaction boundary, so a "no" never changes the answer.
    /// Those records need the words "assistant" or "compact_boundary" in the log, unless a letter of them is
    /// written as a <c>\uXXXX</c> escape, which JSON allows: any line with <c>\u</c> in it is parsed.
    /// </summary>
    static bool CouldBeRelevant(ReadOnlySpan<byte> line) =>
        line.IndexOf("assistant"u8) >= 0 || line.IndexOf("compact_boundary"u8) >= 0 || line.IndexOf("\\u"u8) >= 0;

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
