using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Retrieval;

/// <summary>One ATX heading of a markdown file.</summary>
/// <param name="Level">1 to 6, the number of <c>#</c>.</param>
/// <param name="Text">The heading's text after the <c>#</c>s, as written (closing <c>#</c>s removed).</param>
/// <param name="Line">Its 1-based line number.</param>
internal sealed record Heading(int Level, string Text, int Line);

/// <summary>
/// A markdown file read once: its lines (line ends removed, a byte-order mark dropped, as <c>dmc size</c> reads them,
/// so their byte counts are the same), which of them are inside a fenced code block, each line's visible text (with
/// HTML comments removed, since the templates' guidance and examples live in comments), and its headings. Both
/// commands read files only through this, so a heading, a fence and a comment mean the same to lookup and to recall
/// (research R3, R7).
/// </summary>
internal sealed class MarkdownFile
{
    /// <summary>The path the file was read from.</summary>
    public string Path { get; }

    /// <summary>The file's lines, without their line ends, exactly as written.</summary>
    public string[] Lines { get; }

    /// <summary>For each line (0-based), its bytes in the file, line end included, as <c>dmc size</c> counts them.</summary>
    public int[] LineBytes { get; }

    /// <summary>For each line (0-based), true when it is part of a fenced code block, its fence lines included.</summary>
    public bool[] Fenced { get; }

    /// <summary>
    /// For each line (0-based), its text with every HTML comment removed, including the parts of a comment that runs
    /// over several lines; a fenced line is kept as written.
    /// </summary>
    public string[] Visible { get; }

    /// <summary>The headings outside fenced code blocks and HTML comments, in order.</summary>
    public IReadOnlyList<Heading> Headings { get; }

    /// <summary>Builds a file from its bytes.</summary>
    /// <param name="path">The path, kept for messages and labels.</param>
    /// <param name="bytes">The file's bytes.</param>
    MarkdownFile(string path, byte[] bytes)
    {
        Path = path;
        var lines = Size.Measurement.Split(bytes);
        Lines = lines.Select(l => l.Text).ToArray();
        LineBytes = lines.Select(l => l.Bytes).ToArray();
        (Fenced, Visible) = MarkdownHeadings.Scan(Lines);
        var headings = new List<Heading>();
        for (int i = 0; i < Lines.Length; i++)
        {
            if (!Fenced[i] && MarkdownHeadings.Parse(Visible[i]) is { } h)
                headings.Add(h with { Line = i + 1 });
        }
        Headings = headings;
    }

    /// <summary>Reads a file.</summary>
    /// <param name="path">The file.</param>
    /// <returns>The parsed file.</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be read.</exception>
    public static MarkdownFile Read(string path) => new(path, AsUtf8(File.ReadAllBytes(path)));

    /// <summary>
    /// A file's bytes as UTF-8: unchanged unless they start with a UTF-16 or UTF-32 byte-order mark (Windows PowerShell
    /// 5.1 writes UTF-16 by default), which are decoded and re-encoded, so their lines still match.
    /// </summary>
    /// <param name="bytes">The file's bytes.</param>
    static byte[] AsUtf8(byte[] bytes)
    {
        bool wide = bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF))
                    || (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF);
        if (!wide)
            return bytes;
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Encoding.UTF8.GetBytes(reader.ReadToEnd());
    }

    /// <summary>Builds a file from text, for tests.</summary>
    /// <param name="path">A path to label it with.</param>
    /// <param name="text">Its text.</param>
    /// <returns>The parsed file.</returns>
    public static MarkdownFile FromText(string path, string text) => new(path, Encoding.UTF8.GetBytes(text));
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
    /// Fenced code blocks and HTML comments, worked out together in one pass so that neither hides the other: a fence
    /// opens with at least three <c>`</c> or <c>~</c> after at most three spaces and closes at a line of the same
    /// character, at least as many, and nothing else; a comment runs from <c>&lt;!--</c> to the next <c>--&gt;</c>, on
    /// the same line or a later one. A fence line inside a comment does not open a fence, and a fenced line keeps any
    /// <c>&lt;!--</c> as text. <c>&lt;!--</c> inside an inline code span is text. An opener that is never closed is
    /// text too, so a stray <c>&lt;!--</c> in prose cannot hide the rest of a file; the empty comments <c>&lt;!--&gt;</c>
    /// and <c>&lt;!---&gt;</c> close at once. An unclosed fence runs to the end of the file.
    /// </summary>
    /// <param name="lines">The file's lines.</param>
    /// <returns>For each line, whether it is fenced, and its text with comments removed (a fenced line as written).</returns>
    public static (bool[] Fenced, string[] Visible) Scan(string[] lines)
    {
        var ignored = new HashSet<(int Line, int Column)>();
        while (true)
        {
            var (fenced, visible, unclosed) = ScanOnce(lines, ignored);
            if (unclosed is not { } opener)
                return (fenced, visible);
            // That opener never closes: take it as text and scan again. Each pass ignores one more, so this ends.
            ignored.Add(opener);
        }
    }

    /// <summary>One pass of <see cref="Scan"/>, with some comment openers taken as text.</summary>
    /// <param name="lines">The file's lines.</param>
    /// <param name="ignored">The openers (line and column, 0-based) to take as text.</param>
    /// <returns>The flags and texts, and the opener of a comment still open at the end, if any.</returns>
    static (bool[] Fenced, string[] Visible, (int, int)? Unclosed) ScanOnce(string[] lines, HashSet<(int Line, int Column)> ignored)
    {
        var fenced = new bool[lines.Length];
        var visible = new string[lines.Length];
        char fence = '\0';
        int fenceLength = 0;
        bool inComment = false;
        (int, int) opener = (0, 0);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (fence != '\0')
            {
                fenced[i] = true;
                visible[i] = line;
                var (c, n, after) = FenceRun(line);
                if (c == fence && n >= fenceLength && after.Trim().Length == 0)
                    fence = '\0';
                continue;
            }
            if (!inComment)
            {
                var (c, n, after) = FenceRun(line);
                if (n >= 3 && !(c == '`' && after.Contains('`')))
                {
                    fence = c;
                    fenceLength = n;
                    fenced[i] = true;
                    visible[i] = line;
                    continue;
                }
                if (!line.Contains("<!--", StringComparison.Ordinal))
                {
                    visible[i] = line; // most lines: no comment, nothing to copy
                    continue;
                }
            }

            var kept = new StringBuilder();
            int at = 0;
            while (at < line.Length)
            {
                if (inComment)
                {
                    int close = line.IndexOf("-->", at, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        at = line.Length;
                        break;
                    }
                    inComment = false;
                    at = close + 3;
                    continue;
                }
                int start = NextOpener(line, at, i, ignored);
                if (start < 0)
                {
                    kept.Append(line, at, line.Length - at);
                    break;
                }
                kept.Append(line, at, start - at);
                string rest = line[(start + 4)..];
                if (rest.StartsWith('>'))
                    at = start + 5;
                else if (rest.StartsWith("->", StringComparison.Ordinal))
                    at = start + 6;
                else
                {
                    inComment = true;
                    opener = (i, start);
                    at = start + 4;
                }
            }
            visible[i] = kept.ToString();
        }
        return (fenced, visible, inComment ? opener : null);
    }

    /// <summary>The next <c>&lt;!--</c> at or after <paramref name="from"/> that opens a comment: not one taken as text, and not inside an inline code span (after an odd number of backticks on the line).</summary>
    /// <param name="line">The line.</param>
    /// <param name="from">Where to start looking.</param>
    /// <param name="lineIndex">The line's 0-based index, to check <paramref name="ignored"/>.</param>
    /// <param name="ignored">The openers taken as text.</param>
    /// <returns>Its column, or -1.</returns>
    static int NextOpener(string line, int from, int lineIndex, HashSet<(int Line, int Column)> ignored)
    {
        for (int start = line.IndexOf("<!--", from, StringComparison.Ordinal); start >= 0; start = line.IndexOf("<!--", start + 1, StringComparison.Ordinal))
        {
            bool inCode = line.AsSpan(0, start).Count('`') % 2 == 1;
            if (!inCode && !ignored.Contains((lineIndex, start)))
                return start;
        }
        return -1;
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
