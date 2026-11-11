using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Retrieval;

/// <summary>One ATX heading of a markdown file.</summary>
/// <param name="Level">1 to 6, the number of <c>#</c>.</param>
/// <param name="Text">The heading's text after the <c>#</c>s, as written (closing <c>#</c>s removed).</param>
/// <param name="Line">Its 1-based line number.</param>
internal sealed record Heading(int Level, string Text, int Line);

/// <summary>
/// A markdown file read once: its lines (line ends removed, a byte-order mark dropped), which of them are inside a
/// fenced code block, and its headings. Both commands read files only through this, so a heading and a fence mean the
/// same to lookup and to recall (research R3, R7).
/// </summary>
internal sealed class MarkdownFile
{
    /// <summary>The path the file was read from.</summary>
    public string Path { get; }

    /// <summary>The file's lines, without their line ends.</summary>
    public string[] Lines { get; }

    /// <summary>For each line (0-based), true when it is part of a fenced code block, its fence lines included.</summary>
    public bool[] Fenced { get; }

    /// <summary>The headings outside fenced code blocks, in order.</summary>
    public IReadOnlyList<Heading> Headings { get; }

    /// <summary>Builds a file from text already read.</summary>
    /// <param name="path">The path, kept for messages and labels.</param>
    /// <param name="text">The file's whole text.</param>
    MarkdownFile(string path, string text)
    {
        Path = path;
        if (text.Length > 0 && text[0] == '﻿')
            text = text[1..];
        string[] lines = text.Split('\n');
        if (lines.Length > 0 && lines[^1].Length == 0 && text.EndsWith('\n'))
            lines = lines[..^1];
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].EndsWith('\r'))
                lines[i] = lines[i][..^1];
        }
        Lines = lines;
        Fenced = MarkdownHeadings.FencedLines(lines);
        var headings = new List<Heading>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!Fenced[i] && MarkdownHeadings.Parse(lines[i]) is { } h)
                headings.Add(h with { Line = i + 1 });
        }
        Headings = headings;
    }

    /// <summary>Reads a file as UTF-8.</summary>
    /// <param name="path">The file.</param>
    /// <returns>The parsed file.</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be read.</exception>
    public static MarkdownFile Read(string path) => new(path, File.ReadAllText(path, Encoding.UTF8));

    /// <summary>Builds a file from text, for tests.</summary>
    /// <param name="path">A path to label it with.</param>
    /// <param name="text">Its text.</param>
    /// <returns>The parsed file.</returns>
    public static MarkdownFile FromText(string path, string text) => new(path, text);
}

/// <summary>
/// ATX headings, fenced code blocks, sections and the normalised heading key that lookup matches on (research R7).
/// Setext headings are not recognised: the kit's templates never use them.
/// </summary>
internal static partial class MarkdownHeadings
{
    /// <summary>The most spaces a heading or fence line may be indented by.</summary>
    const int MaxIndent = 3;

    /// <summary>The heading on a line, or null. Its <see cref="Heading.Line"/> is 0; the caller sets it.</summary>
    /// <param name="line">One line, without its line end.</param>
    public static Heading? Parse(string line)
    {
        int i = 0;
        while (i < line.Length && i < MaxIndent && line[i] == ' ')
            i++;
        int start = i;
        while (i < line.Length && line[i] == '#')
            i++;
        int level = i - start;
        if (level is < 1 or > 6)
            return null;
        if (i < line.Length && line[i] != ' ' && line[i] != '\t')
            return null;
        string text = line[i..].Trim();
        // An optional closing sequence of #s, preceded by a space, is not part of the text.
        string closed = text.TrimEnd('#');
        if (closed.Length < text.Length && (closed.Length == 0 || closed.EndsWith(' ') || closed.EndsWith('\t')))
            text = closed.TrimEnd();
        return new Heading(level, text, 0);
    }

    /// <summary>
    /// Which lines are part of a fenced code block: a fence opens with at least three <c>`</c> or <c>~</c> after at most
    /// three spaces, and closes at a line of the same character, at least as many, and nothing but spaces after them.
    /// An unclosed fence runs to the end of the file.
    /// </summary>
    /// <param name="lines">The file's lines.</param>
    /// <returns>One flag per line.</returns>
    public static bool[] FencedLines(string[] lines)
    {
        var fenced = new bool[lines.Length];
        char fence = '\0';
        int length = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var (c, n, rest) = FenceRun(lines[i]);
            if (fence == '\0')
            {
                if (n >= 3 && !(c == '`' && rest.Contains('`')))
                {
                    fence = c;
                    length = n;
                    fenced[i] = true;
                }
            }
            else
            {
                fenced[i] = true;
                if (c == fence && n >= length && rest.Trim().Length == 0)
                    fence = '\0';
            }
        }
        return fenced;
    }

    /// <summary>The fence character a line starts with (after at most three spaces), how many, and the rest of the line.</summary>
    /// <param name="line">One line.</param>
    static (char Char, int Count, string After) FenceRun(string line)
    {
        int i = 0;
        while (i < line.Length && i < MaxIndent && line[i] == ' ')
            i++;
        if (i >= line.Length || (line[i] != '`' && line[i] != '~'))
            return ('\0', 0, "");
        char c = line[i];
        int start = i;
        while (i < line.Length && line[i] == c)
            i++;
        return (c, i - start, line[i..]);
    }

    /// <summary>
    /// The last line (1-based) of the section a heading opens: the line before the next heading of the same or a
    /// higher level (a smaller or equal number of <c>#</c>), or the file's last line.
    /// </summary>
    /// <param name="headings">The file's headings, in order.</param>
    /// <param name="index">The index in <paramref name="headings"/> of the section's heading.</param>
    /// <param name="lineCount">The number of lines in the file.</param>
    public static int SectionEnd(IReadOnlyList<Heading> headings, int index, int lineCount)
    {
        int level = headings[index].Level;
        for (int j = index + 1; j < headings.Count; j++)
        {
            if (headings[j].Level <= level)
                return headings[j].Line - 1;
        }
        return lineCount;
    }

    /// <summary>
    /// The normalised form lookup matches headings and requests on: lower case; <c>*</c>, <c>_</c> and <c>`</c>
    /// removed; a link reduced to its text; a trailing parenthesis that starts with <c>PDF</c>, <c>p.</c>,
    /// <c>pp.</c> or <c>page</c> removed; every other character that is not a letter or digit turned into a space;
    /// spaces collapsed.
    /// </summary>
    /// <param name="text">A heading's text or a request.</param>
    /// <returns>The key.</returns>
    public static string Key(string text)
    {
        text = LinkPattern().Replace(text, "$1");
        text = PageReferencePattern().Replace(text, "");
        var key = new StringBuilder(text.Length);
        bool space = true;
        foreach (char raw in text)
        {
            if (raw is '*' or '_' or '`')
                continue;
            char c = char.ToLowerInvariant(raw);
            if (char.IsLetterOrDigit(c))
            {
                key.Append(c);
                space = false;
            }
            else if (!space)
            {
                key.Append(' ');
                space = true;
            }
        }
        return key.ToString().TrimEnd();
    }

    /// <summary>A markdown link, <c>[text](target)</c>; group 1 is its text.</summary>
    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkPattern();

    /// <summary>A trailing page reference in parentheses: <c>(PDF 212)</c>, <c>(p. 34)</c>, <c>(pp. 3-4)</c>, <c>(page 9)</c>.</summary>
    [GeneratedRegex(@"\s*\((?:pdf|pp?\.|page)[^)]*\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PageReferencePattern();
}
