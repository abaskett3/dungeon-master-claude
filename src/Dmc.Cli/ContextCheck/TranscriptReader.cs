using System.Text.Json;

namespace Dmc.ContextCheck;

/// <summary>
/// Finds the context size of the last API call in a Claude Code session log (<c>.jsonl</c>), as
/// <c>scripts/context_check.py</c> does. The size is the last assistant record's input tokens plus the tokens
/// read from and written to the cache.
/// </summary>
public static class TranscriptReader
{
    /// <summary>Bytes of the log read at a time, from the end.</summary>
    public const int ChunkSize = 1 << 20;

    /// <summary>
    /// Context of the last call in the log; 0 if a compaction came after it or there is none. Reads the log from
    /// the end in <see cref="ChunkSize"/> pieces and stops at the first answer, so a long log costs one read.
    /// Throws on anything it cannot read, and on a record whose shape the script would also trip over: the caller
    /// treats every failure as "print nothing".
    /// </summary>
    public static long LastContext(string path)
    {
        // The log is being written by Claude Code while it is read: share it fully.
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        long end = file.Length;
        byte[] tail = [];
        while (end > 0)
        {
            long start = Math.Max(0, end - ChunkSize);
            int fresh = (int)(end - start);
            var block = new byte[fresh + tail.Length];
            file.Seek(start, SeekOrigin.Begin);
            file.ReadExactly(block, 0, fresh);
            tail.CopyTo(block, fresh);
            end = start;

            // Lines are taken from the end of the block. Unless this block starts the file, its first line may
            // be cut: keep it, and read it whole together with the next chunk.
            int limit = block.Length;
            while (true)
            {
                int newline = block.AsSpan(0, limit).LastIndexOf((byte)'\n');
                if (newline < 0 && end > 0)
                {
                    tail = block.AsSpan(0, limit).ToArray();
                    break;
                }
                if (newline < 0)
                    tail = [];

                var line = new ReadOnlyMemory<byte>(block, newline + 1, limit - (newline + 1));
                if (TryRecordContext(line, out long size))
                    return size;
                if (newline < 0)
                    break;
                limit = newline;
            }
        }
        return 0;
    }

    /// <summary>
    /// True when this line settles the answer: <paramref name="size"/> is 0 at a compaction boundary, or the
    /// context size of an assistant record. A line that is not a JSON object, or does not count, is skipped.
    /// </summary>
    static bool TryRecordContext(ReadOnlyMemory<byte> line, out long size)
    {
        size = 0;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return false; // a cut last line, a blank line or junk
        }

        using (document)
        {
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
