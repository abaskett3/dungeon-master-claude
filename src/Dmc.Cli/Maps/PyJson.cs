using System.Globalization;
using System.Numerics;
using System.Text;

namespace Dmc.Maps;

/// <summary>A JSON syntax error, worded the way Python's <c>json</c> module words it.</summary>
public sealed class JsonDecodeError(string message) : Exception(message);

/// <summary>
/// <c>json.loads</c> and <c>json.dumps</c> as Python does them, because the map files and the page's data file
/// must read and read back the same. It follows CPython's C scanner (<c>_json.c</c>) so that the same bad file
/// gets the same message, down to "line 1 column 14 (char 13)". Differences from <c>System.Text.Json</c> that
/// matter here: it reads <c>NaN</c> and <c>Infinity</c>, keeps whole numbers of any size, lets a later duplicate key
/// win, and writes only ASCII.
/// </summary>
public static class PyJson
{
    /// <summary>Python stops at about this many levels (a RecursionError); this stops with a plain message instead.</summary>
    const int MaxDepth = 900;

    // ---------- reading ----------

    /// <summary><c>json.loads(text)</c>. Throws <see cref="JsonDecodeError"/>.</summary>
    public static object? Parse(string text)
    {
        var reader = new Reader(Py.CodePoints(text));
        return reader.Document();
    }

    sealed class Reader(int[] s)
    {
        sealed class Stop(int index) : Exception
        {
            public int Index { get; } = index;
        }

        int _depth;

        JsonDecodeError Error(string message, int position)
        {
            int line = 1, lineStart = -1;
            for (int i = 0; i < position && i < s.Length; i++)
            {
                if (s[i] == '\n')
                {
                    line++;
                    lineStart = i;
                }
            }
            return new JsonDecodeError($"{message}: line {line} column {position - lineStart} (char {position})");
        }

        public object? Document()
        {
            if (s.Length > 0 && s[0] == 0xFEFF)
                throw Error("Unexpected UTF-8 BOM (decode using utf-8-sig)", 0);
            int at = SkipWhitespace(0);
            object? value;
            try
            {
                value = ScanOnce(at, out at);
            }
            catch (Stop stop)
            {
                throw Error("Expecting value", stop.Index);
            }
            at = SkipWhitespace(at);
            if (at != s.Length)
                throw Error("Extra data", at);
            return value;
        }

        static bool IsWhitespace(int c) => c is ' ' or '\t' or '\n' or '\r';

        int SkipWhitespace(int at)
        {
            while (at < s.Length && IsWhitespace(s[at]))
                at++;
            return at;
        }

        bool Is(int at, char c) => at < s.Length && s[at] == c;

        bool Matches(int at, string word)
        {
            if (at + word.Length > s.Length)
                return false;
            for (int i = 0; i < word.Length; i++)
            {
                if (s[at + i] != word[i])
                    return false;
            }
            return true;
        }

        object? ScanOnce(int at, out int next)
        {
            if (at >= s.Length)
                throw new Stop(at);
            switch (s[at])
            {
                case '"':
                    return ScanString(at + 1, out next);
                case '{':
                    return ScanObject(at + 1, out next);
                case '[':
                    return ScanArray(at + 1, out next);
                case 'n' when Matches(at, "null"):
                    next = at + 4;
                    return null;
                case 't' when Matches(at, "true"):
                    next = at + 4;
                    return true;
                case 'f' when Matches(at, "false"):
                    next = at + 5;
                    return false;
                case 'N' when Matches(at, "NaN"):
                    next = at + 3;
                    return double.NaN;
                case 'I' when Matches(at, "Infinity"):
                    next = at + 8;
                    return double.PositiveInfinity;
                case '-' when Matches(at, "-Infinity"):
                    next = at + 9;
                    return double.NegativeInfinity;
            }
            return ScanNumber(at, out next);
        }

        static bool IsDigit(int c) => c is >= '0' and <= '9';

        object? ScanNumber(int start, out int next)
        {
            int at = start;
            if (Is(at, '-'))
            {
                at++;
                if (at >= s.Length)
                    throw new Stop(start);
            }
            if (s[at] is >= '1' and <= '9')
            {
                at++;
                while (at < s.Length && IsDigit(s[at]))
                    at++;
            }
            else if (s[at] == '0')
            {
                at++;
            }
            else
            {
                throw new Stop(start);
            }

            bool isFloat = false;
            if (at + 1 < s.Length && s[at] == '.' && IsDigit(s[at + 1]))
            {
                isFloat = true;
                at += 2;
                while (at < s.Length && IsDigit(s[at]))
                    at++;
            }
            if (at + 1 < s.Length && (s[at] == 'e' || s[at] == 'E'))
            {
                int exponentStart = at;
                at++;
                if (at + 1 < s.Length && (s[at] == '-' || s[at] == '+'))
                    at++;
                while (at < s.Length && IsDigit(s[at]))
                    at++;
                if (IsDigit(s[at - 1]))
                    isFloat = true;
                else
                    at = exponentStart;
            }

            string number = Py.FromCodePoints(s[start..at]);
            next = at;
            if (isFloat)
                return double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
            var whole = BigInteger.Parse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            return whole >= long.MinValue && whole <= long.MaxValue ? (long)whole : whole;
        }

        static int HexValue(int c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };

        string ScanString(int start, out int next)
        {
            int begin = start - 1;
            var builder = new StringBuilder();
            int end = start;
            while (true)
            {
                int at = end;
                int c = 0;
                for (; at < s.Length; at++)
                {
                    c = s[at];
                    if (c == '"' || c == '\\')
                        break;
                    if (c <= 0x1F)
                        throw Error("Invalid control character at", at);
                    builder.Append(Py.FromCodePoint(c));
                }
                if (at >= s.Length)
                    throw Error("Unterminated string starting at", begin);

                at++; // past the quote or the backslash
                if (c == '"')
                {
                    next = at;
                    return builder.ToString();
                }
                if (at == s.Length)
                    throw Error("Unterminated string starting at", begin);

                c = s[at];
                if (c != 'u')
                {
                    end = at + 1;
                    switch (c)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        default: throw Error("Invalid \\escape", end - 2);
                    }
                    continue;
                }

                at++;
                end = at + 4;
                if (end >= s.Length)
                    throw Error("Invalid \\uXXXX escape", at - 1);
                int unit = 0;
                for (; at < end; at++)
                {
                    int digit = HexValue(s[at]);
                    if (digit < 0)
                        throw Error("Invalid \\uXXXX escape", end - 5);
                    unit = (unit << 4) | digit;
                }
                if (unit is >= 0xD800 and <= 0xDBFF && end + 6 < s.Length && s[at] == '\\' && s[at + 1] == 'u')
                {
                    at += 2;
                    end += 6;
                    int second = 0;
                    for (; at < end; at++)
                    {
                        int digit = HexValue(s[at]);
                        if (digit < 0)
                            throw Error("Invalid \\uXXXX escape", end - 5);
                        second = (second << 4) | digit;
                    }
                    if (second is >= 0xDC00 and <= 0xDFFF)
                        unit = 0x10000 + (((unit - 0xD800) << 10) | (second - 0xDC00));
                    else
                        end -= 6;
                }
                builder.Append(Py.FromCodePoint(unit));
            }
        }

        PyDict ScanObject(int start, out int next)
        {
            Enter();
            var result = new PyDict();
            int at = SkipWhitespace(start);
            if (!Is(at, '}'))
            {
                while (true)
                {
                    if (!Is(at, '"'))
                        throw Error("Expecting property name enclosed in double quotes", at);
                    string key = ScanString(at + 1, out at);
                    at = SkipWhitespace(at);
                    if (!Is(at, ':'))
                        throw Error("Expecting ':' delimiter", at);
                    at = SkipWhitespace(at + 1);
                    result[key] = ScanOnce(at, out at);
                    at = SkipWhitespace(at);
                    if (Is(at, '}'))
                        break;
                    if (!Is(at, ','))
                        throw Error("Expecting ',' delimiter", at);
                    at = SkipWhitespace(at + 1);
                }
            }
            _depth--;
            next = at + 1;
            return result;
        }

        List<object?> ScanArray(int start, out int next)
        {
            Enter();
            var result = new List<object?>();
            int at = SkipWhitespace(start);
            if (!Is(at, ']'))
            {
                while (true)
                {
                    result.Add(ScanOnce(at, out at));
                    at = SkipWhitespace(at);
                    if (Is(at, ']'))
                        break;
                    if (!Is(at, ','))
                        throw Error("Expecting ',' delimiter", at);
                    at = SkipWhitespace(at + 1);
                }
            }
            _depth--;
            next = at + 1;
            return result;
        }

        void Enter()
        {
            if (++_depth > MaxDepth)
                throw new JsonDecodeError("the JSON is nested too deeply");
        }
    }

    // ---------- writing ----------

    /// <summary><c>json.dumps(value)</c>: ", " and ": " separators, and only ASCII (anything else is a \uXXXX escape).</summary>
    public static string Dumps(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value);
        return builder.ToString();
    }

    static void Write(StringBuilder builder, object? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case bool b:
                builder.Append(b ? "true" : "false");
                break;
            case long l:
                builder.Append(l.ToString(CultureInfo.InvariantCulture));
                break;
            case int i:
                builder.Append(i.ToString(CultureInfo.InvariantCulture));
                break;
            case BigInteger big:
                builder.Append(big.ToString(CultureInfo.InvariantCulture));
                break;
            case double d:
                builder.Append(double.IsNaN(d) ? "NaN" : double.IsPositiveInfinity(d) ? "Infinity"
                    : double.IsNegativeInfinity(d) ? "-Infinity" : Py.FloatRepr(d));
                break;
            case string s:
                WriteString(builder, s);
                break;
            case List<object?> list:
                builder.Append('[');
                for (int n = 0; n < list.Count; n++)
                {
                    if (n > 0)
                        builder.Append(", ");
                    Write(builder, list[n]);
                }
                builder.Append(']');
                break;
            case PyDict dict:
                builder.Append('{');
                bool first = true;
                foreach (var pair in dict)
                {
                    if (!first)
                        builder.Append(", ");
                    first = false;
                    WriteString(builder, pair.Key);
                    builder.Append(": ");
                    Write(builder, pair.Value);
                }
                builder.Append('}');
                break;
            default:
                throw new InvalidOperationException("not a JSON value: " + value.GetType().Name);
        }
    }

    static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case >= ' ' and <= '~': builder.Append(c); break;
                default: builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); break;
            }
        }
        builder.Append('"');
    }
}
