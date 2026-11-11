using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>
/// <c>dmc lookup</c>'s work: find one heading in a source and return its section exactly, or the list of headings that
/// match, or the line saying the section is too large to print (research R7 to R9; contracts/dmc-lookup.md). Reads
/// only; returns the lines to print, and throws <see cref="DmcException"/> for a failure.
/// </summary>
internal static partial class Lookup
{
    /// <summary>The most matches a list shows before <c>(+N more)</c>.</summary>
    const int MatchesListed = 8;

    /// <summary>The most nearby headings a "no heading" failure names.</summary>
    const int NearestListed = 5;

    /// <summary>The most pages the PDF rules path names.</summary>
    const int PagesListed = 10;

    /// <summary>Looks a heading up in a source.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="sourceArgument">The <c>SOURCE</c> argument as typed.</param>
    /// <param name="heading">The heading's words.</param>
    /// <returns>The lines to print.</returns>
    /// <exception cref="DmcException">No heading matched, the source is not one, or a file cannot be read.</exception>
    public static List<string> Run(string dmcFolder, string sourceArgument, string heading)
    {
        var source = LookupSource.Resolve(dmcFolder, sourceArgument);
        // A rules reference with no markdown of its own (a PDF, or text) is reached through its index and pages.
        if (source.Kind == SourceKind.Rules && source.Files().Count == 0)
            return PdfRules(dmcFolder, source, heading);

        var candidates = new List<(MarkdownFile File, string Relative, int Index)>();
        foreach (string relative in source.Files())
        {
            var file = Read(dmcFolder, Path.Combine(source.Folder, relative));
            for (int i = 0; i < file.Headings.Count; i++)
                candidates.Add((file, relative, i));
        }

        string key = MarkdownHeadings.Key(heading);
        var matches = candidates.Where(c => key.Length > 0 && MarkdownHeadings.Key(c.File.Headings[c.Index].Text) == key).ToList();
        var wanted = Words.Stems(heading).ToHashSet(StringComparer.Ordinal);
        if (matches.Count == 0 && wanted.Count > 0)
        {
            matches = candidates.Where(c => wanted.IsSubsetOf(Words.Stems(c.File.Headings[c.Index].Text))).ToList();
        }

        if (matches.Count == 1)
            return Section(dmcFolder, matches[0].File, matches[0].Index);
        if (matches.Count > 1)
        {
            var lines = matches.Take(MatchesListed)
                .Select(m => $"{Shown(dmcFolder, m.File.Path)}:{m.File.Headings[m.Index].Line}: {m.File.Lines[m.File.Headings[m.Index].Line - 1].Trim()}")
                .ToList();
            if (matches.Count > MatchesListed)
                lines.Add($"(+{matches.Count - MatchesListed} more)");
            return lines;
        }

        // A markdown file beside a rules PDF (house rules, say) does not hide the PDF: its index and pages come next.
        if (source.Kind == SourceKind.Rules && source.File is null && source.HasPdf)
            return PdfRules(dmcFolder, source, heading);

        var nearest = candidates
            .Select((c, order) => (Text: c.File.Headings[c.Index].Text, Shared: Words.Stems(c.File.Headings[c.Index].Text).Distinct().Count(wanted.Contains), order))
            .Where(n => n.Shared > 0)
            .OrderByDescending(n => n.Shared).ThenBy(n => n.order)
            .Select(n => n.Text).Distinct(StringComparer.Ordinal).Take(NearestListed).ToList();
        string near = nearest.Count > 0 ? $" (nearest: {string.Join("; ", nearest)})" : "";
        throw new DmcException($"no heading \"{heading}\" in {sourceArgument}{near}");
    }

    /// <summary>
    /// The section a heading opens, under a header line <c>&lt;file&gt;:&lt;first&gt;-&lt;last&gt;</c>; or, when it is
    /// at the near-cap mark <c>dmc size</c> uses, the one line saying so (FR-007).
    /// </summary>
    /// <param name="dmcFolder">The DMC folder, which paths are shown relative to.</param>
    /// <param name="file">The file.</param>
    /// <param name="index">The heading's index in the file.</param>
    static List<string> Section(string dmcFolder, MarkdownFile file, int index)
    {
        var heading = file.Headings[index];
        int first = heading.Line;
        int last = MarkdownHeadings.SectionEnd(file.Headings, index, file.Lines.Length);
        var body = file.Lines[(first - 1)..last];
        // Bytes as they are in the file, line ends included, and bytes ÷ 4 rounded down: the estimate dmc size flags with.
        long bytes = file.LineBytes[(first - 1)..last].Sum(b => (long)b);
        long tokens = bytes / 4;
        string where = $"{Shown(dmcFolder, file.Path)}:{first}-{last}";
        if (tokens >= Budgets.NearCapTokens || body.Length >= Budgets.NearCapLines)
        {
            string lines = body.Length.ToString("N0", CultureInfo.InvariantCulture);
            return [$"{where}: section {file.Lines[first - 1].Trim()} is {Measurement.FormatTokens(tokens)} tokens, {lines} lines: read it in parts"];
        }
        return [where, .. body];
    }

    /// <summary>
    /// The rules reference kept as a PDF (research R9): the lines of <c>rules/index.md</c> that hold every word of the
    /// request, or, when none does, the pages of the extracted text that hold it as a phrase. Never any page text.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="source">The rules source.</param>
    /// <param name="request">The heading's words.</param>
    /// <exception cref="DmcException">Neither the index nor the pages hold the request.</exception>
    static List<string> PdfRules(string dmcFolder, LookupSource source, string request)
    {
        var wanted = Words.Stems(request).ToHashSet(StringComparer.Ordinal);
        string indexPath = Path.Combine(source.Folder, "index.md");
        if (wanted.Count > 0 && File.Exists(indexPath))
        {
            var index = Read(dmcFolder, indexPath);
            var hits = new List<string>();
            int total = 0;
            for (int i = 0; i < index.Lines.Length; i++)
            {
                string line = index.Visible[i].Trim();
                if (line.Length == 0 || index.Fenced[i] || MarkdownHeadings.Parse(line) is not null || TableSeparator().IsMatch(line))
                    continue;
                if (!wanted.IsSubsetOf(Words.Stems(line)))
                    continue;
                total++;
                if (hits.Count < MatchesListed)
                    hits.Add($"rules/index.md:{i + 1}: {Trim.Around(line, wanted)}");
            }
            if (total > MatchesListed)
                hits.Add($"(+{total - MatchesListed} more)");
            if (hits.Count > 0)
                return hits;
        }

        string phrase = MarkdownHeadings.Key(request);
        string pagesFolder = Path.Combine(source.Folder, "extracted", "text");
        if (phrase.Length > 0 && Directory.Exists(pagesFolder))
        {
            var pages = new List<int>();
            foreach (string page in Directory.EnumerateFiles(pagesFolder, "page-*.txt"))
            {
                var number = PageNumber().Match(Path.GetFileName(page));
                if (!number.Success)
                    continue;
                string text;
                try
                {
                    text = File.ReadAllText(page, Encoding.UTF8);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    throw new DmcException($"cannot read {Shown(dmcFolder, page)}: {e.Message}");
                }
                if ($" {MarkdownHeadings.Key(text)} ".Contains($" {phrase} ", StringComparison.Ordinal))
                    pages.Add(int.Parse(number.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            if (pages.Count > 0)
            {
                pages.Sort();
                string listed = string.Join(", ", pages.Take(PagesListed));
                if (pages.Count > PagesListed)
                    listed += $" (+{pages.Count - PagesListed} more)";
                string pdf = Directory.EnumerateFiles(source.Folder, "*.pdf").Select(Path.GetFileName).Order(StringComparer.Ordinal).FirstOrDefault() is { } name
                    ? $"rules/{name}"
                    : "the rules PDF";
                return [$"rules: \"{request}\" is on PDF pages {listed} (read them in {pdf})"];
            }
        }
        throw new DmcException($"rules: no \"{request}\" in rules/index.md or its pages");
    }

    /// <summary>Reads a markdown file, turning a read failure into the one-line failure.</summary>
    /// <param name="dmcFolder">The DMC folder, for the message.</param>
    /// <param name="path">The file.</param>
    /// <exception cref="DmcException">The file cannot be read.</exception>
    static MarkdownFile Read(string dmcFolder, string path)
    {
        try
        {
            return MarkdownFile.Read(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot read {Shown(dmcFolder, path)}: {e.Message}");
        }
    }

    /// <summary>A path as output shows it: relative to the DMC folder, with <c>/</c>.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="path">An absolute path inside it.</param>
    internal static string Shown(string dmcFolder, string path) => Path.GetRelativePath(dmcFolder, path).Replace('\\', '/');

    /// <summary>A markdown table's separator row, such as <c>|---|:---:|</c>.</summary>
    [GeneratedRegex(@"^\|[\s:|\-]+$")]
    private static partial Regex TableSeparator();

    /// <summary>An extracted page's file name, <c>page-NNN.txt</c>; group 1 is the number.</summary>
    [GeneratedRegex(@"^page-(\d+)\.txt$", RegexOptions.IgnoreCase)]
    private static partial Regex PageNumber();
}
