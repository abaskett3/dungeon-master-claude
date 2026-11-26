using System.Text;
using System.Text.RegularExpressions;
using Dmc.Maps;

namespace Dmc.Sheets;

/// <summary>
/// The small part of Markdown the templates use, turned into HTML for a sheet: bold, italics and code within a line;
/// lists (nested by indent), tables, <c>### </c> headings and paragraphs across lines. Everything is escaped first with
/// <see cref="Py.HtmlEscape"/>, so no text in a campaign file can add markup, a script or a link to the page; a link is
/// printed as its text. No package: the release stays four files (spec 031 FR-016).
/// </summary>
internal static partial class SheetMarkdown
{
    /// <summary>Bold text, <c>**...**</c> or <c>__...__</c>, after escaping.</summary>
    [GeneratedRegex(@"\*\*(?=\S)(.+?)(?<=\S)\*\*|__(?=\S)(.+?)(?<=\S)__")]
    private static partial Regex Bold();

    /// <summary>Italic text, <c>*...*</c> or <c>_..._</c> at word edges, after escaping.</summary>
    [GeneratedRegex(@"(?<![\w*])\*(?=\S)(.+?)(?<=\S)\*(?![\w*])|(?<!\w)_(?=\S)(.+?)(?<=\S)_(?!\w)")]
    private static partial Regex Italic();

    /// <summary>Code, <c>`...`</c>, after escaping.</summary>
    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex Code();

    /// <summary>A link, <c>[text](target)</c>: only its text is kept.</summary>
    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex Link();

    /// <summary>A list item: its indent, then <c>- </c>, <c>* </c>, <c>+ </c> or <c>1. </c>, then its text.</summary>
    [GeneratedRegex(@"^(?<indent>[ \t]*)(?:[-*+]|\d+[.)])\s+(?<text>.*)$")]
    private static partial Regex ListItem();

    /// <summary>One line of text as HTML: escaped, then bold, italics and code; a link becomes its text.</summary>
    /// <param name="text">The text, as written in the file.</param>
    public static string Inline(string text)
    {
        string html = Py.HtmlEscape(Link().Replace(text.Trim(), "$1"));
        var codes = new List<string>();
        html = Code().Replace(html, m =>
        {
            codes.Add("<code>" + m.Groups[1].Value + "</code>");
            return "\u0001" + (codes.Count - 1) + "\u0002";
        });
        html = Bold().Replace(html, m => "<strong>" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "</strong>");
        html = Italic().Replace(html, m => "<em>" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "</em>");
        return Regex.Replace(html, "\u0001(\\d+)\u0002", m => codes[int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)]);
    }

    /// <summary>
    /// Lines of a free-form section, or a field's sub-items, as HTML blocks: lists nested by indent, tables, headings of
    /// two to six hashes (a history file's <c>## </c> ones among them), and paragraphs. Blank lines end a paragraph or a list.
    /// </summary>
    /// <param name="lines">The lines, comments removed.</param>
    /// <returns>The HTML, or empty when the lines hold nothing.</returns>
    public static string Blocks(IEnumerable<string> lines)
    {
        var html = new StringBuilder();
        var paragraph = new List<string>();
        var table = new List<string>();
        var stack = new Stack<int>(); // the indents of the open lists
        bool itemOpen = false;

        void EndParagraph()
        {
            if (paragraph.Count > 0)
                html.Append("<p>").Append(string.Join(" ", paragraph.Select(Inline))).Append("</p>");
            paragraph.Clear();
        }

        void EndTable()
        {
            if (table.Count > 0)
                html.Append(Table(SheetText.TableCells(table)));
            table.Clear();
        }

        void EndLists()
        {
            while (stack.Count > 0)
            {
                html.Append("</li></ul>");
                stack.Pop();
            }
            itemOpen = false;
        }

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd();
            if (line.Trim().Length == 0)
            {
                EndParagraph();
                EndTable();
                EndLists();
                continue;
            }
            if (line.TrimStart().StartsWith('|'))
            {
                EndParagraph();
                EndLists();
                table.Add(line);
                continue;
            }
            EndTable();
            var heading = Regex.Match(line, @"^(#{2,6})\s+(.*)$");
            if (heading.Success)
            {
                EndParagraph();
                EndLists();
                html.Append("<h4>").Append(Inline(heading.Groups[2].Value)).Append("</h4>");
                continue;
            }
            var item = ListItem().Match(line);
            if (item.Success)
            {
                EndParagraph();
                int indent = item.Groups["indent"].Value.Replace("\t", "    ").Length;
                if (stack.Count == 0 || indent > stack.Peek())
                {
                    html.Append("<ul><li>");
                    stack.Push(indent);
                }
                else
                {
                    while (stack.Count > 1 && indent < stack.Peek())
                    {
                        html.Append("</li></ul>");
                        stack.Pop();
                    }
                    html.Append("</li><li>");
                }
                itemOpen = true;
                html.Append(Inline(item.Groups["text"].Value));
                continue;
            }
            if (itemOpen && raw.Length > 0 && raw[0] is ' ' or '\t')
            {
                html.Append(' ').Append(Inline(line)); // a list item's text that goes on to the next line
                continue;
            }
            EndLists();
            paragraph.Add(line);
        }
        EndParagraph();
        EndTable();
        EndLists();
        return html.ToString();
    }

    /// <summary>A table as HTML: the first row is the header, an empty table is empty text.</summary>
    /// <param name="rows">The cells, header first.</param>
    public static string Table(IReadOnlyList<string[]> rows)
    {
        if (rows.Count == 0)
            return "";
        var html = new StringBuilder("<table><thead><tr>");
        foreach (string cell in rows[0])
            html.Append("<th>").Append(Inline(cell)).Append("</th>");
        html.Append("</tr></thead><tbody>");
        foreach (string[] row in rows.Skip(1))
        {
            html.Append("<tr>");
            for (int i = 0; i < rows[0].Length; i++)
                html.Append("<td>").Append(Inline(i < row.Length ? row[i] : "")).Append("</td>");
            html.Append("</tr>");
        }
        return html.Append("</tbody></table>").ToString();
    }
}
