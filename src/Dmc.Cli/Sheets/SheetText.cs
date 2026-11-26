using System.Text.RegularExpressions;

namespace Dmc.Sheets;

/// <summary>
/// One labelled field of a sheet: a top-level list item <c>- **Label** (hint): value</c>, with the indented lines under
/// it (sub-items, such as one attack per line).
/// </summary>
/// <param name="Label">The bold text, trimmed: what a layout matches on (spec 031 FR-010).</param>
/// <param name="Value">The text after the first colon that follows the label, comments removed and trimmed; empty when there is none.</param>
/// <param name="HasColon">False for a marker line with no colon, such as <c>- **Player character**</c>.</param>
/// <param name="SubLines">The indented lines under the field, as written, comments removed.</param>
/// <param name="Line">The field's line as written, for a finding that quotes it.</param>
internal sealed record SheetField(string Label, string Value, bool HasColon, IReadOnlyList<string> SubLines, string Line)
{
    /// <summary>Whether the field holds anything: a value, or a sub-item.</summary>
    public bool IsBlank => Value.Length == 0 && SubLines.All(l => l.Trim().Length == 0);
}

/// <summary>One <c>## </c> section of a sheet (or the part before the first, <see cref="SheetText.Top"/>), with its <c>### </c> subsections.</summary>
/// <param name="Heading">The heading text without <c>## </c>, trimmed; <see cref="SheetText.Top"/> for the part before the first heading.</param>
/// <param name="Lines">The lines under the heading, up to the first <c>### </c> heading, comments removed.</param>
/// <param name="Subsections">Each <c>### </c> subsection: its heading text and its lines, comments removed.</param>
internal sealed record SheetSection(string Heading, IReadOnlyList<string> Lines, IReadOnlyList<(string Heading, IReadOnlyList<string> Lines)> Subsections)
{
    /// <summary>Every line of the section, its subsections' headings and lines included, in order.</summary>
    public IEnumerable<string> AllLines =>
        Lines.Concat(Subsections.SelectMany(s => s.Lines.Prepend("### " + s.Heading)));
}

/// <summary>
/// A campaign Markdown file (a sheet, or <c>game-state.md</c>) cut into its sections and fields, the way the templates
/// write them. HTML comments, where the templates keep their guidance, are removed first: nothing inside one is ever
/// read or exported. Only reading; nothing here writes.
/// </summary>
internal static partial class SheetText
{
    /// <summary>The name of the part before the first <c>## </c> heading: the title and the opening fields.</summary>
    public const string Top = "";

    /// <summary>A labelled list item: <c>- **Label**</c>, then anything (a hint in brackets, a colon and the value).</summary>
    [GeneratedRegex(@"^[-*] \*\*(?<label>[^*]+?)\*\*(?<rest>.*)$")]
    private static partial Regex FieldLine();

    /// <summary>The start of a list item: <c>- </c>, <c>* </c>, <c>+ </c> or <c>1. </c>.</summary>
    [GeneratedRegex(@"^(?:[-*+]|\d+[.)])\s")]
    private static partial Regex ListStart();

    /// <summary>An HTML comment on one line.</summary>
    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex InlineComment();

    /// <summary>
    /// The text of a file with every HTML comment removed, a comment that spans lines included; the lines a whole
    /// comment filled are dropped, a line with text beside a comment keeps the text.
    /// </summary>
    /// <param name="text">The file's text.</param>
    public static List<string> LinesWithoutComments(string text)
    {
        var result = new List<string>();
        bool inComment = false;
        foreach (string raw in text.Replace("\r\n", "\n").TrimStart('﻿').Split('\n'))
        {
            string line = raw;
            bool touched = false;
            if (inComment)
            {
                int end = line.IndexOf("-->", StringComparison.Ordinal);
                if (end < 0)
                    continue;
                inComment = false;
                line = line[(end + 3)..];
                touched = true;
            }
            string cleaned = InlineComment().Replace(line, "");
            touched |= cleaned.Length != line.Length;
            int start = cleaned.IndexOf("<!--", StringComparison.Ordinal);
            if (start >= 0)
            {
                inComment = true;
                cleaned = cleaned[..start];
                touched = true;
            }
            if (touched && cleaned.Trim().Length == 0)
                continue;
            result.Add(touched ? cleaned.TrimEnd() : cleaned);
        }
        return result;
    }

    /// <summary>The <c># </c> title of a file's lines, or null when it has none.</summary>
    /// <param name="lines">The lines, comments removed.</param>
    public static string? Title(IEnumerable<string> lines) =>
        lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)) is { } t ? t[2..].Trim() : null;

    /// <summary>
    /// Cuts lines into sections: the part before the first <c>## </c> heading (<see cref="Top"/>, the title line left
    /// out), then each <c>## </c> section with its <c>### </c> subsections. A heading is compared later ignoring case and
    /// spaces at its ends.
    /// </summary>
    /// <param name="lines">The lines, comments removed.</param>
    public static List<SheetSection> Sections(IReadOnlyList<string> lines)
    {
        var sections = new List<SheetSection>();
        string heading = Top;
        var own = new List<string>();
        var subs = new List<(string, IReadOnlyList<string>)>();
        string? subHeading = null;
        var subLines = new List<string>();

        void CloseSub()
        {
            if (subHeading is not null)
                subs.Add((subHeading, subLines));
            subHeading = null;
            subLines = [];
        }

        void Close()
        {
            CloseSub();
            sections.Add(new SheetSection(heading, own, subs));
            own = [];
            subs = [];
        }

        foreach (string line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Close();
                heading = line[3..].Trim();
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseSub();
                subHeading = line[4..].Trim();
            }
            else if (heading == Top && line.StartsWith("# ", StringComparison.Ordinal))
            {
                // The title is not a line of the top section.
            }
            else if (subHeading is not null)
                subLines.Add(line);
            else
                own.Add(line);
        }
        Close();
        return sections;
    }

    /// <summary>The section with this heading, compared ignoring case, or null.</summary>
    /// <param name="sections">A file's sections.</param>
    /// <param name="heading">The heading without <c>## </c>; <see cref="Top"/> for the opening part.</param>
    public static SheetSection? Find(IEnumerable<SheetSection> sections, string heading) =>
        sections.FirstOrDefault(s => string.Equals(s.Heading, heading, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// What a run of lines holds: its labelled fields, each with the indented lines under it (and an unindented line that
    /// continues its value with no blank line between, as Markdown reads it), and every other line that is not blank, not
    /// part of a field and not a table row (<paramref name="tables"/> collects those), in order.
    /// </summary>
    /// <param name="lines">The lines of a section or subsection.</param>
    /// <param name="others">Receives the lines that are neither a field, a field's sub-line, a table row nor blank.</param>
    /// <param name="tables">Receives the table rows (lines starting with <c>|</c>).</param>
    public static List<SheetField> Fields(IEnumerable<string> lines, List<string>? others = null, List<string>? tables = null)
    {
        var fields = new List<SheetField>();
        string? label = null, value = null, line = null;
        bool colon = false, afterBlank = false;
        var sub = new List<string>();

        void Flush()
        {
            if (label is not null)
                fields.Add(new SheetField(label, value!, colon, sub, line!));
            label = null;
            sub = [];
        }

        foreach (string text in lines)
        {
            if (text.Trim().Length == 0)
            {
                afterBlank = true;
                continue;
            }
            bool lazy = !afterBlank;
            afterBlank = false;
            bool indented = text[0] is ' ' or '\t';
            if (indented && label is not null)
            {
                sub.Add(text);
                continue;
            }
            var match = FieldLine().Match(text.TrimEnd());
            if (!indented && match.Success)
            {
                Flush();
                label = match.Groups["label"].Value.Trim().TrimEnd(':').Trim();
                string rest = match.Groups["rest"].Value;
                int at = rest.IndexOf(':');
                // "**Label:**" puts the colon inside the bold text; the value is then all of the rest.
                bool colonInLabel = match.Groups["label"].Value.TrimEnd().EndsWith(':');
                colon = at >= 0 || colonInLabel;
                value = (colonInLabel && at < 0 ? rest : at >= 0 ? rest[(at + 1)..] : "").Trim();
                line = text.TrimEnd();
                continue;
            }
            // A Markdown lazy continuation: a field's value wrapped onto the next line with no indent and no blank between.
            if (label is not null && lazy && sub.Count == 0 && !text.StartsWith('|') && !ListStart().IsMatch(text) && !text.StartsWith('#'))
            {
                value = (value + " " + text.Trim()).Trim();
                continue;
            }
            Flush();
            if (text.TrimStart().StartsWith('|'))
                tables?.Add(text.Trim());
            else
                others?.Add(text.TrimEnd());
        }
        Flush();
        return fields;
    }

    /// <summary>
    /// A Markdown table's rows as cells, the separator row (<c>|---|</c>) left out; each cell trimmed. The first row is the
    /// header.
    /// </summary>
    /// <param name="rows">The table's lines, each starting with <c>|</c>.</param>
    public static List<string[]> TableCells(IEnumerable<string> rows) =>
        [.. rows.Select(r => r.Trim())
            .Where(r => !Regex.IsMatch(r, @"^\|[\s:|-]*$"))
            .Select(r => r.Trim('|').Split('|').Select(c => c.Trim()).ToArray())];
}
