using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>
/// The content units of a campaign file (data-model.md § Content unit; research R5): its non-blank lines, normalised so that
/// a line moved between files or out of a list still matches. A line is trimmed and its runs of whitespace collapsed to one
/// space; a heading loses its leading <c>#</c>s and the space after them; a leading <c>- </c>, <c>* </c>, <c>+ </c>,
/// <c>N. </c> or <c>&gt; </c> is removed (repeatedly, so <c>&gt; - x</c> is <c>x</c>). Table separator rows, horizontal rules
/// and the inside of <c>&lt;!-- ... --&gt;</c> comments are structure, not content, and are skipped. A table row is one unit.
/// Comparison is case-sensitive. A unit's id is the first 12 hex digits of the SHA-256 of its UTF-8 text.
/// </summary>
internal static partial class ContentUnits
{
    /// <summary>How many hex digits an id has.</summary>
    public const int IdLength = 12;

    /// <summary>A test's replacement for <see cref="Id"/>, to force two units onto one id; null in the program.</summary>
    internal static Func<string, string>? IdOverride { get; set; }

    /// <summary>An HTML comment, possibly spanning lines.</summary>
    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Comment { get; }

    /// <summary>A run of whitespace.</summary>
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace { get; }

    /// <summary>A horizontal rule: three or more of one of <c>-</c>, <c>*</c>, <c>_</c>, spaces allowed between.</summary>
    [GeneratedRegex(@"^([-*_])(?: ?\1){2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex Rule { get; }

    /// <summary>A table separator row: pipes, dashes, colons and spaces only, with at least one pipe and one dash.</summary>
    [GeneratedRegex(@"^(?=.*\|)(?=.*-)[|:\- ]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Separator { get; }

    /// <summary>A heading: one to six <c>#</c> and a space.</summary>
    [GeneratedRegex(@"^#{1,6} ", RegexOptions.CultureInvariant)]
    private static partial Regex Heading { get; }

    /// <summary>A leading list or quote marker and the space after it.</summary>
    [GeneratedRegex(@"^(?:[-*+]|\d+\.|>) ", RegexOptions.CultureInvariant)]
    private static partial Regex Marker { get; }

    /// <summary>A list or quote marker with nothing after it: an empty item, not content.</summary>
    [GeneratedRegex(@"^(?:[-*+]|\d+\.|>)$", RegexOptions.CultureInvariant)]
    private static partial Regex BareMarker { get; }

    /// <summary>The units of a file's text, in order, duplicates kept.</summary>
    /// <param name="text">The file's text, with any line endings.</param>
    public static List<string> Split(string text) => [.. Lines(text).Select(u => u.Unit)];

    /// <summary>The units of a file's text with the line each comes from, in order, duplicates kept.</summary>
    /// <param name="text">The file's text, with any line endings.</param>
    /// <returns>Each unit and its 1-based line number in <paramref name="text"/>.</returns>
    public static List<(int Line, string Unit)> Lines(string text)
    {
        // A comment becomes its line breaks, so the lines around it stay lines; an unclosed "<!--" is left as content.
        string stripped = Comment.Replace(text, m => new string('\n', m.Value.Count(c => c == '\n')));
        var units = new List<(int, string)>();
        string[] lines = stripped.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (Normalise(lines[i]) is { } unit)
                units.Add((i + 1, unit));
        }
        return units;
    }

    /// <summary>The units of a file's bytes, read as UTF-8.</summary>
    /// <param name="bytes">The file's bytes.</param>
    public static List<string> Split(byte[] bytes) => Split(Encoding.UTF8.GetString(bytes));

    /// <summary>One line as a unit.</summary>
    /// <param name="line">A line, without its line break.</param>
    /// <returns>The normalised text, or null for a blank line or a line of structure.</returns>
    public static string? Normalise(string line)
    {
        string text = Whitespace.Replace(line, " ").Trim();
        if (text.Length == 0 || Rule.IsMatch(text) || Separator.IsMatch(text))
            return null;
        if (Heading.Match(text) is { Success: true } heading)
            text = text[heading.Length..].Trim();
        for (var marker = Marker.Match(text); marker.Success; marker = Marker.Match(text))
            text = text[marker.Length..].TrimStart();
        return text.Length == 0 || BareMarker.IsMatch(text) ? null : text;
    }

    /// <summary>A unit's id: the first 12 hex digits of the SHA-256 of its UTF-8 text.</summary>
    /// <param name="unit">A normalised unit.</param>
    public static string Id(string unit) =>
        IdOverride?.Invoke(unit) ?? Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(unit)))[..IdLength];
}
