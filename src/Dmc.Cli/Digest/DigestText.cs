using System.Globalization;
using System.Text;
using Dmc.Maps;

namespace Dmc.Digest;

/// <summary>
/// A record in a session log that is not shaped the way Claude Code writes it (a message that is a number, a tool
/// call's input that is a list). The script would stop on it with a traceback; <see cref="Session"/> turns this into
/// one line that names the file and the line.
/// </summary>
internal sealed class LogShapeException(string message) : Exception(message);

/// <summary>
/// The small text functions of <c>transcript_digest.py</c> (<c>cut</c>, <c>one_line</c>, <c>base_name</c>,
/// <c>describe_call</c>, <c>result_text</c>, <c>table</c>, <c>percent</c>, <c>median</c>), with Python's meaning:
/// text is counted and cut in code points, not in UTF-16 units, and an integer division rounds down.
/// </summary>
internal static class DigestText
{
    // ---------- text in code points ----------

    /// <summary>The UTF-16 index where code point number <paramref name="codePoints"/> starts (the end of the text if it is shorter).</summary>
    static int Offset(string text, int codePoints)
    {
        int i = 0;
        for (int count = 0; i < text.Length && count < codePoints; count++)
            i += char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
        return i;
    }

    /// <summary>Python's <c>text[start:end]</c> for non-negative bounds.</summary>
    public static string Slice(string text, int start, int end)
    {
        int from = Offset(text, start);
        return text[from..(from + Offset(text[from..], end - start))];
    }

    /// <summary>Python's <c>"%-Ns"</c>: padded with spaces on the right to N code points, never cut.</summary>
    public static string PadRight(string text, int width) => text + new string(' ', Math.Max(0, width - Py.Len(text)));

    /// <summary>Python's <c>"%Ns"</c> and <c>"%Nd"</c>: padded with spaces on the left.</summary>
    public static string PadLeft(string text, int width) => new string(' ', Math.Max(0, width - Py.Len(text))) + text;

    /// <summary>Python's <c>a // b</c>: the quotient rounded towards minus infinity.</summary>
    public static long FloorDiv(long a, long b)
    {
        long q = a / b;
        return a % b != 0 && (a < 0) != (b < 0) ? q - 1 : q;
    }

    public static string Num(long n) => n.ToString(CultureInfo.InvariantCulture);

    // ---------- the script's functions ----------

    /// <summary>The text with its carriage returns removed, cut to <paramref name="limit"/> characters with a note of how many were dropped.</summary>
    public static string Cut(string text, int limit)
    {
        text = text.Replace("\r", "", StringComparison.Ordinal);
        int length = Py.Len(text);
        return length <= limit ? text : $"{Slice(text, 0, limit)} ...[+{length - limit} chars]";
    }

    public static string OneLine(string text, int limit) => Cut(string.Join(" / ", text.Split('\n')), limit);

    public static string BaseName(string path) => path.Replace('\\', '/').Split('/')[^1];

    /// <summary>What a tool call is shown as in the transcript.</summary>
    public static string DescribeCall(string name, PyDict args)
    {
        if (name is "Read" or "Write" or "Edit" or "NotebookEdit")
        {
            string description = BaseName(TextField(args, "file_path"));
            if (name == "Write")
                description += $" [{Len(args, "content")} chars]";
            else if (name == "Edit")
                description += $" [-{Len(args, "old_string")} +{Len(args, "new_string")}]";
            else if (Py.Truthy(Get(args, "limit")) || Py.Truthy(Get(args, "pages")))
                description += " (part)";
            return description;
        }
        return OneLine(PyJson.Dumps(args), 220);
    }

    /// <summary>What a tool call did, for classing the calls of a message: roll, write, lookup or other.</summary>
    public static string CallKind(string name, PyDict args)
    {
        if (name is "Bash" or "PowerShell" && TextField(args, "command").Replace('\\', '/').Contains("scripts/roll", StringComparison.Ordinal))
            return "roll";
        if (name is "Edit" or "Write")
            return "write";
        if (name is "Read" or "Grep" or "Glob")
            return "lookup";
        return "other";
    }

    /// <summary>The text of a tool result, which Claude Code writes as a string or as a list of blocks.</summary>
    public static string ResultText(object? content)
    {
        switch (content)
        {
            case string s:
                return s;
            case List<object?> list:
                return string.Join(" ", list.OfType<PyDict>().Select(c =>
                    c.TryGetValue("text", out var text)
                        ? text as string ?? throw new LogShapeException("a tool result block whose text is not text")
                        : "<" + (c.TryGetValue("type", out var type) ? Py.Str(type) : "?") + ">"));
            default:
                return Py.Str(content);
        }
    }

    /// <summary>The rows of a counter, biggest first, as <c>Read x3 (90 chars), Edit x1</c>.</summary>
    public static string Table(Counter counter, Counter? extra = null, int top = 8)
    {
        var rows = counter.MostCommon(top).Select(p =>
            $"{(p.Key.Length == 0 ? "?" : p.Key)} x{p.Value}{(extra is { IsEmpty: false } ? $" ({extra[p.Key]} chars)" : "")}");
        string text = string.Join(", ", rows);
        return text.Length == 0 ? "none" : text;
    }

    public static string Percent(long part, long whole) =>
        whole != 0 ? Py.F1(100.0 * part / whole) + "%" : "0%";

    public static double Median(IEnumerable<long> values)
    {
        long[] sorted = values.Order().ToArray();
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    // ---------- reading a JSON record ----------

    public static object? Get(PyDict record, string key) => record.TryGetValue(key, out var value) ? value : null;

    /// <summary>Python's <c>record.get(key) or {}</c>.</summary>
    public static PyDict DictOrEmpty(PyDict record, string key) => Get(record, key) switch
    {
        PyDict dict => dict,
        var other when !Py.Truthy(other) => new PyDict(),
        _ => throw new LogShapeException($"\"{key}\" is not an object"),
    };

    /// <summary>Python's <c>record.get(key) or []</c>.</summary>
    public static List<object?> ListOrEmpty(PyDict record, string key) => Get(record, key) switch
    {
        List<object?> list => list,
        var other when !Py.Truthy(other) => [],
        _ => throw new LogShapeException($"\"{key}\" is not a list"),
    };

    public static PyDict AsDict(object? value, string what) =>
        value as PyDict ?? throw new LogShapeException($"{what} is not an object");

    /// <summary>A text field; <paramref name="fallback"/> when it is missing. A JSON <c>null</c> is not text (the script stops on one).</summary>
    public static string TextField(PyDict record, string key, string fallback = "") => record.TryGetValue(key, out var value)
        ? value as string ?? throw new LogShapeException($"\"{key}\" is not text")
        : fallback;

    /// <summary>
    /// A field read as a key or a label (a record type, a message id, a model name): missing or <c>null</c> is the empty
    /// string, text is itself, and anything else is its Python <c>str()</c>, as a Python <c>Counter</c> key would show it.
    /// </summary>
    public static string KeyText(PyDict record, string key) => Get(record, key) switch
    {
        null => "",
        string s => s,
        var other => Py.Str(other),
    };

    /// <summary>A text field that is <c>null</c> or missing as the empty string; any other kind of value is an error.</summary>
    public static string TextOrNull(PyDict record, string key) => Get(record, key) switch
    {
        null => "",
        string s => s,
        _ => throw new LogShapeException($"\"{key}\" is not text"),
    };

    /// <summary>A whole number field, 0 when missing.</summary>
    public static long Number(PyDict record, string key) => Get(record, key) switch
    {
        long n => n,
        null when !record.ContainsKey(key) => 0,
        _ => throw new LogShapeException($"\"{key}\" is not a whole number"),
    };

    /// <summary>Python's <c>len(record.get(key, ""))</c>.</summary>
    public static long Len(PyDict record, string key) => !record.TryGetValue(key, out var value) ? 0 : value switch
    {
        string s => Py.Len(s),
        List<object?> list => list.Count,
        PyDict dict => dict.Count,
        _ => throw new LogShapeException($"\"{key}\" has no length"),
    };

    /// <summary>The text written as UTF-8 with no byte-order mark, as the script writes its files (a lone surrogate becomes "?").</summary>
    public static readonly Encoding Utf8 = MakeUtf8();

    static Encoding MakeUtf8()
    {
        var encoding = (Encoding)new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).Clone();
        encoding.EncoderFallback = new EncoderReplacementFallback("?");
        return encoding;
    }
}
