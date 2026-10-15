using System.Globalization;
using System.Numerics;
using System.Text;

namespace Dmc.Maps;

/// <summary>
/// A JSON object as <c>json.load</c> gives it in Python: keys in the order they were first written, and a later
/// duplicate key replaces the value but keeps the place. (<c>OrderedDictionary</c> does the same.)
/// </summary>
public sealed class PyDict : OrderedDictionary<string, object?>
{
}

/// <summary>
/// The bits of Python that <c>map_render.py</c> relies on, so that a map file means the same thing to <c>dmc map</c>.
/// A JSON value is <c>null</c>, <see cref="bool"/>, <see cref="long"/> or <see cref="BigInteger"/>,
/// <see cref="double"/>, <see cref="string"/>, <c>List&lt;object?&gt;</c> or <see cref="PyDict"/>. Text is counted in
/// code points, as Python counts it, not in UTF-16 units.
/// </summary>
public static class Py
{
    // ---------- text ----------

    /// <summary>The code points of a string. A lone surrogate (which JSON can spell with \ud800) counts as one.</summary>
    public static int[] CodePoints(string text)
    {
        var result = new List<int>(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                result.Add(char.ConvertToUtf32(text[i], text[i + 1]));
                i++;
            }
            else
            {
                result.Add(text[i]);
            }
        }
        return result.ToArray();
    }

    /// <summary>The text of one code point (a lone surrogate stays a lone surrogate).</summary>
    public static string FromCodePoint(int codePoint) =>
        codePoint is >= 0xD800 and <= 0xDFFF ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint);

    public static string FromCodePoints(IEnumerable<int> codePoints)
    {
        var builder = new StringBuilder();
        foreach (int cp in codePoints)
            builder.Append(FromCodePoint(cp));
        return builder.ToString();
    }

    /// <summary><c>len(text)</c>.</summary>
    public static int Len(string text)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                i++;
            count++;
        }
        return count;
    }

    /// <summary>Python's <c>str.isspace</c> for one code point.</summary>
    public static bool IsSpace(int cp) =>
        cp is >= 9 and <= 13 or >= 0x1C and <= 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A
            or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000;

    /// <summary><c>text.split()</c>: the words between runs of whitespace.</summary>
    public static List<string> Split(string text)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        foreach (int cp in CodePoints(text))
        {
            if (IsSpace(cp))
            {
                if (word.Length > 0)
                {
                    words.Add(word.ToString());
                    word.Clear();
                }
            }
            else
            {
                word.Append(FromCodePoint(cp));
            }
        }
        if (word.Length > 0)
            words.Add(word.ToString());
        return words;
    }

    /// <summary><c>text.strip()</c>.</summary>
    public static string Strip(string text)
    {
        int[] cps = CodePoints(text);
        int start = 0, end = cps.Length;
        while (start < end && IsSpace(cps[start]))
            start++;
        while (end > start && IsSpace(cps[end - 1]))
            end--;
        return FromCodePoints(cps[start..end]);
    }

    /// <summary><c>html.escape(text, quote=True)</c>.</summary>
    public static string HtmlEscape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#x27;");

    public static bool IsAlpha(int cp) =>
        CharUnicodeInfo.GetUnicodeCategory(cp) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter;

    public static bool IsDigit(int cp) => CharUnicodeInfo.GetUnicodeCategory(cp) == UnicodeCategory.DecimalDigitNumber;

    // ---------- values ----------

    /// <summary>Python's truth value of a JSON value.</summary>
    public static bool Truthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        long l => l != 0,
        BigInteger big => !big.IsZero,
        double d => d != 0,
        string s => s.Length > 0,
        List<object?> list => list.Count > 0,
        PyDict dict => dict.Count > 0,
        _ => true,
    };

    /// <summary>Python's <c>str(value)</c> of a JSON value.</summary>
    public static string Str(object? value) => value is string s ? s : Repr(value);

    /// <summary>Python's <c>repr(value)</c> of a JSON value.</summary>
    public static string Repr(object? value) => value switch
    {
        null => "None",
        bool b => b ? "True" : "False",
        long l => l.ToString(CultureInfo.InvariantCulture),
        BigInteger big => big.ToString(CultureInfo.InvariantCulture),
        double d => FloatRepr(d),
        string s => StringRepr(s),
        List<object?> list => "[" + string.Join(", ", list.Select(Repr)) + "]",
        PyDict dict => "{" + string.Join(", ", dict.Select(p => StringRepr(p.Key) + ": " + Repr(p.Value))) + "}",
        _ => value.ToString() ?? "",
    };

    static string StringRepr(string text)
    {
        // Python picks single quotes unless the text has a single quote and no double quote.
        char quote = text.Contains('\'') && !text.Contains('"') ? '"' : '\'';
        var builder = new StringBuilder().Append(quote);
        foreach (int cp in CodePoints(text))
        {
            if (cp == quote || cp == '\\')
                builder.Append('\\').Append((char)cp);
            else if (cp == '\t')
                builder.Append("\\t");
            else if (cp == '\n')
                builder.Append("\\n");
            else if (cp == '\r')
                builder.Append("\\r");
            else if (cp < 0x20 || cp == 0x7F)
                builder.Append("\\x").Append(cp.ToString("x2", CultureInfo.InvariantCulture));
            else if (cp < 0x7F || IsPrintable(cp))
                builder.Append(FromCodePoint(cp));
            else if (cp <= 0xFF)
                builder.Append("\\x").Append(cp.ToString("x2", CultureInfo.InvariantCulture));
            else if (cp <= 0xFFFF)
                builder.Append("\\u").Append(cp.ToString("x4", CultureInfo.InvariantCulture));
            else
                builder.Append("\\U").Append(cp.ToString("x8", CultureInfo.InvariantCulture));
        }
        return builder.Append(quote).ToString();
    }

    static bool IsPrintable(int cp) =>
        cp is not (>= 0xD800 and <= 0xDFFF)
        && CharUnicodeInfo.GetUnicodeCategory(cp) is not (UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.SpaceSeparator);

    /// <summary>
    /// Whether a value equals a whole number the way Python's <c>==</c> does: <c>5</c>, <c>5.0</c> and <c>True</c> (1)
    /// all do. Used for "is it one of these numbers", which is how <c>in (1, 2, 3, 4)</c> works.
    /// </summary>
    public static bool TryExactInt(object? value, out BigInteger number)
    {
        switch (value)
        {
            case bool b:
                number = b ? 1 : 0;
                return true;
            case long l:
                number = l;
                return true;
            case BigInteger big:
                number = big;
                return true;
            case double d when double.IsFinite(d) && d == Math.Floor(d):
                number = new BigInteger(d);
                return true;
            default:
                number = default;
                return false;
        }
    }

    // ---------- numbers ----------

    /// <summary>Python's <c>repr(float)</c>: the shortest text that reads back as the same number.</summary>
    public static string FloatRepr(double value)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsInfinity(value))
            return value > 0 ? "inf" : "-inf";
        if (value == 0)
            return double.IsNegative(value) ? "-0.0" : "0.0";

        // Take the digits and the exponent from .NET's shortest round-trip text ("1.2345E-05", "123.45", "1E+16").
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        bool negative = text[0] == '-';
        if (negative)
            text = text[1..];
        int exponent = 0;
        int e = text.IndexOfAny(['E', 'e']);
        if (e >= 0)
        {
            exponent = int.Parse(text[(e + 1)..], CultureInfo.InvariantCulture);
            text = text[..e];
        }
        int point = text.IndexOf('.');
        string digits = point < 0 ? text : text.Remove(point, 1);
        int pointAt = (point < 0 ? text.Length : point) + exponent; // digits before the decimal point
        string stripped = digits.TrimStart('0');
        pointAt -= digits.Length - stripped.Length;
        digits = stripped.TrimEnd('0');
        if (digits.Length == 0)
            digits = "0";

        string body;
        if (pointAt > 16 || pointAt < -3)
        {
            // Scientific: d.ddde+XX
            int exp10 = pointAt - 1;
            string mantissa = digits.Length > 1 ? digits[0] + "." + digits[1..] : digits;
            body = mantissa + "e" + (exp10 < 0 ? "-" : "+") + Math.Abs(exp10).ToString("00", CultureInfo.InvariantCulture);
        }
        else if (pointAt <= 0)
        {
            body = "0." + new string('0', -pointAt) + digits;
        }
        else if (pointAt >= digits.Length)
        {
            body = digits + new string('0', pointAt - digits.Length) + ".0";
        }
        else
        {
            body = digits[..pointAt] + "." + digits[pointAt..];
        }
        return negative ? "-" + body : body;
    }

    /// <summary>Python's <c>f"{value:.1f}"</c>, which rounds the exact binary value, half to even.</summary>
    public static string F1(double value) => Fixed(value, 1);

    /// <summary>Python's <c>f"{value:.0f}"</c>.</summary>
    public static string F0(double value) => Fixed(value, 0);

    static string Fixed(double value, int digits)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsInfinity(value))
            return value > 0 ? "inf" : "-inf";

        // .NET rounds an exact tie (0.25 to one place, 2.5 to none) away from zero; Python rounds it to even.
        // A tie can only be a multiple of 1/4 (one place) or 1/2 (none), and those are exact in binary.
        double scaled = value * (digits == 1 ? 4 : 2);
        if (scaled == Math.Floor(scaled) && Math.Abs(scaled % 2) == 1)
        {
            decimal rounded = Math.Round((decimal)value, digits, MidpointRounding.ToEven);
            return rounded.ToString(digits == 1 ? "0.0" : "0", CultureInfo.InvariantCulture);
        }
        return value.ToString(digits == 1 ? "F1" : "F0", CultureInfo.InvariantCulture);
    }

    /// <summary>Python's <c>f"{value:g}"</c> for a whole number.</summary>
    public static string G(long value) => value.ToString("G6", CultureInfo.InvariantCulture).Replace('E', 'e');
}
