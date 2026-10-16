using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
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
internal static partial class DigestText
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
    public static string DescribeCall(string? name, PyDict args)
    {
        if (name is "Read" or "Write" or "Edit" or "NotebookEdit")
        {
            string description = BaseName(StrOf(args, "file_path"));
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

    /// <summary>
    /// <c>dmc roll</c> as a shell runs it: <c>./bin/dmc roll</c>, <c>bin/dmc.exe roll</c>, <c>&amp; "D:/x/bin/dmc.exe" roll</c>
    /// or a bare <c>dmc roll</c>, once backslashes are made forward slashes. The program is the whole word <c>dmc</c>, at the
    /// start of the command or after a space, a quote, <c>&amp;</c>, <c>;</c>, <c>|</c> or <c>(</c>.
    /// </summary>
    [GeneratedRegex(@"(?:^|[\s;&|(""'])(?:[^\s""']*/)?dmc(?:\.exe)?[""']?\s+roll(?![\w-])")]
    private static partial Regex DmcRoll();

    /// <summary>
    /// What a tool call did, for classing the calls of a message: roll, write, lookup or other. A roll is <c>dmc roll</c>,
    /// or one of the dice scripts that came before it (<c>scripts/roll.ps1</c>, <c>scripts/roll.sh</c>), so that the logs
    /// of sessions played before the switch to <c>dmc</c> digest as they always did.
    /// </summary>
    public static string CallKind(string? name, PyDict args)
    {
        if (name is "Bash" or "PowerShell" && StrOf(args, "command").Replace('\\', '/') is var command
            && (command.Contains("scripts/roll", StringComparison.Ordinal) || DmcRoll().IsMatch(command)))
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
            $"{(p.Key.Truthy ? p.Key.Display : "?")} x{p.Value}{(extra is { IsEmpty: false } ? $" ({extra[p.Key]} chars)" : "")}");
        string text = string.Join(", ", rows);
        return text.Length == 0 ? "none" : text;
    }

    public static string Percent(double part, double whole) =>
        whole != 0 ? Py.F1(100.0 * part / whole) + "%" : "0%";

    /// <summary>
    /// Python's <c>"%d" % number</c>: a whole number as it is, a float cut towards zero. Python refuses <c>nan</c> and
    /// <c>inf</c> (ValueError, OverflowError), and so does this, in one line.
    /// </summary>
    public static string Whole(double number)
    {
        if (!double.IsFinite(number))
            throw new LogShapeException("a token count that is not a finite number");
        return new BigInteger(Math.Truncate(number)).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Python's <c>a // b</c>: the quotient rounded towards minus infinity. Counts of tokens are whole numbers (a float only
    /// if the log says so), and whole numbers are divided exactly.
    /// </summary>
    public static double FloorDiv(double a, double b)
    {
        if (Math.Abs(a) < 1L << 53 && Math.Abs(b) < 1L << 53 && a == Math.Floor(a) && b == Math.Floor(b))
            return FloorDiv((long)a, (long)b);
        return Math.Floor(a / b);
    }

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

    /// <summary>A text field; <paramref name="fallback"/> when it is missing. Any other kind of value is an error (the script stops on one).</summary>
    public static string TextField(PyDict record, string key, string fallback = "") => record.TryGetValue(key, out var value)
        ? value as string ?? throw new LogShapeException($"\"{key}\" is not text")
        : fallback;

    /// <summary>Python's <c>str(record.get(key, fallback))</c>: what a path or a command is made of, whatever the log holds there ("None" for a null).</summary>
    public static string StrOf(PyDict record, string key, string fallback = "") =>
        record.TryGetValue(key, out var value) ? Py.Str(value) : fallback;

    /// <summary>
    /// Python's <c>record.get(key) or ""</c> for a time: missing, null, 0, false and empty are all "", text is itself. (A
    /// time that is a number, a list or an object would stop the script when it is cut, and so it does here.)
    /// </summary>
    public static string TextOrNull(PyDict record, string key) => Get(record, key) switch
    {
        string s => s,
        var other when !Py.Truthy(other) => "",
        _ => throw new LogShapeException($"\"{key}\" is not text"),
    };

    /// <summary>A record's type or subtype, to compare with a name: the text, or "" for anything else (which equals no name).</summary>
    public static string TypeOf(PyDict record, string key) => Get(record, key) as string ?? "";

    /// <summary>
    /// A number field, 0 when missing: what Python's <c>+</c> and <c>sum</c> take, a whole number of any size, a float, or a
    /// bool (<c>True</c> is 1). A string, a null, a list or an object is an error, as it is in Python.
    /// </summary>
    public static double Number(PyDict record, string key) => !record.TryGetValue(key, out var value) ? 0 : value switch
    {
        bool b => b ? 1 : 0,
        long n => n,
        BigInteger big => (double)big,
        double d => d,
        _ => throw new LogShapeException($"\"{key}\" is not a number"),
    };

    /// <summary>Python's <c>len(record.get(key, ""))</c>.</summary>
    public static long Len(PyDict record, string key) => !record.TryGetValue(key, out var value) ? 0 : value switch
    {
        string s => Py.Len(s),
        List<object?> list => list.Count,
        PyDict dict => dict.Count,
        _ => throw new LogShapeException($"\"{key}\" has no length"),
    };
}
