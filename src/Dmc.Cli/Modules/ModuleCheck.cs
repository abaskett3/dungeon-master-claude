using System.Globalization;
using System.Text.RegularExpressions;
using Dmc.Retrieval;
using Dmc.Size;

namespace Dmc.Modules;

/// <summary>
/// <c>dmc module check</c>'s work (spec 012, contracts/dmc-module-check.md): reads every markdown file of an ingested
/// module that lookup searches, once, in memory, and finds what keeps a lookup from printing one area or one entry: a
/// file at the Read cap mark, a heading whose own text is over the section limit, a heading over several stat blocks,
/// and a heading repeated under one parent. Reads only; stores nothing.
/// </summary>
internal static partial class ModuleCheck
{
    /// <summary>How many lines after an armor-class line its hit-points line may come, for the pair to count as a stat block (research R4).</summary>
    const int StatBlockSpan = 8;

    /// <summary>
    /// Resolves a module's name to its folder: a folder in <c>modules/</c>, written plain or as <c>modules/NAME</c>.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="name">The name as typed.</param>
    /// <returns>The module's name, as messages print it.</returns>
    /// <exception cref="DmcException">A staging folder, or no such module (or one with no markdown lookup searches).</exception>
    public static string Resolve(string dmcFolder, string name)
    {
        string text = name.Replace('\\', '/').Trim('/');
        if (text.StartsWith("modules/", StringComparison.Ordinal))
            text = text["modules/".Length..];
        if (text.StartsWith('.'))
            throw new DmcException($"{text} is not a module (a folder starting with a dot is a staging folder)");
        string modules = Path.Combine(dmcFolder, "modules");
        bool plain = text.Length > 0 && text != ".." && text.IndexOfAny([':', '/']) < 0;
        if (!plain || !Directory.Exists(Path.Combine(modules, text)) || SourceFiles.Module(Path.Combine(modules, text)).Count == 0)
            throw new DmcException($"no module {text} (modules: {LookupSource.Listed(modules)})");
        return text;
    }

    /// <summary>
    /// The problems of a module, one line each, in order: by file in lookup's ordinal order, and within a file its size
    /// line first, then its sections' lines by line number (data-model.md § Module problem). Empty for a sound module.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="module">The module's name, as <see cref="Resolve"/> returned it.</param>
    /// <exception cref="DmcException">A file cannot be read.</exception>
    public static List<string> Problems(string dmcFolder, string module)
    {
        string folder = Path.Combine(dmcFolder, "modules", module);
        var problems = new List<string>();
        foreach (string relative in SourceFiles.Module(folder))
        {
            string shown = $"modules/{module}/{relative}";
            MarkdownFile file;
            try
            {
                file = MarkdownFile.Read(Path.Combine(folder, relative));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new DmcException($"cannot read {shown}: {e.Message}");
            }
            problems.AddRange(FileProblems(file, shown));
        }
        return problems;
    }

    /// <summary>The problems of one file: its size against the mark, then each heading's own size, stat blocks and repeat.</summary>
    /// <param name="file">The file, read.</param>
    /// <param name="shown">Its path as the lines print it.</param>
    internal static List<string> FileProblems(MarkdownFile file, string shown)
    {
        var problems = new List<string>();
        // What one Read of the file costs, compared with the near-cap mark dmc size uses (spec 021 FR-003).
        long fileTokens = Measurement.ReadCost(file.LineCharacters.Sum(c => (long)c), 1);
        if (fileTokens >= Budgets.NearCapTokens || file.Lines.Length >= Budgets.NearCapLines)
            problems.Add($"{shown} {Measurement.FormatTokens(fileTokens)} tokens, {file.Lines.Length.ToString("N0", CultureInfo.InvariantCulture)} lines: "
                + $"at the Read cap mark ({Measurement.FormatTokens(Budgets.NearCapTokens)}): split it by heading into smaller files beside it");

        var firstUnderParent = new Dictionary<(int Parent, int Level, string Key), int>();
        var open = new List<int>(); // the indexes of the headings enclosing the current one, outermost first
        for (int i = 0; i < file.Headings.Count; i++)
        {
            var heading = file.Headings[i];
            while (open.Count > 0 && file.Headings[open[^1]].Level >= heading.Level)
                open.RemoveAt(open.Count - 1);
            int parent = open.Count > 0 ? open[^1] : -1;
            open.Add(i);

            int first = heading.Line - 1;
            int end = i + 1 < file.Headings.Count ? file.Headings[i + 1].Line - 1 : file.Lines.Length;
            long tokens = Measurement.Tokens(file.LineCharacters[first..end].Sum(c => (long)c));
            int statBlocks = StatBlocks(file, first, end);
            string where = $"{shown}:{heading.Line.ToString(CultureInfo.InvariantCulture)} \"{heading.Text}\"";
            if (statBlocks >= 2)
                problems.Add($"{where} holds {statBlocks.ToString(CultureInfo.InvariantCulture)} stat blocks: give each its own heading");
            else if (tokens > Budgets.ModuleSection)
                problems.Add($"{where} {Measurement.FormatTokens(tokens)} tokens, over the {Measurement.FormatTokens(Budgets.ModuleSection)} section limit: "
                    + "give each area or entry its own heading");

            string key = MarkdownHeadings.Key(heading.Text);
            if (key.Length == 0)
                continue;
            if (firstUnderParent.TryGetValue((parent, heading.Level, key), out int earlier))
                problems.Add($"{where} repeats line {file.Headings[earlier].Line.ToString(CultureInfo.InvariantCulture)}: make each heading unique");
            else
                firstUnderParent[(parent, heading.Level, key)] = i;
        }
        return problems;
    }

    /// <summary>
    /// The stat blocks in lines <paramref name="first"/> to <paramref name="end"/> (0-based, end exclusive): each
    /// armor-class line with a hit-points line in the <see cref="StatBlockSpan"/> lines after it, outside fenced code
    /// (research R4).
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="first">The heading's line, 0-based.</param>
    /// <param name="end">The line after the heading's own text, 0-based.</param>
    static int StatBlocks(MarkdownFile file, int first, int end)
    {
        int count = 0;
        for (int i = first; i < end; i++)
        {
            if (file.Fenced[i] || !ArmorClass().IsMatch(file.Visible[i]))
                continue;
            for (int j = i + 1; j < end && j <= i + StatBlockSpan; j++)
            {
                if (!file.Fenced[j] && HitPoints().IsMatch(file.Visible[j]))
                {
                    count++;
                    i = j;
                    break;
                }
            }
        }
        return count;
    }

    /// <summary>A line that starts, after list marks, <c>&gt;</c> and emphasis, with <c>AC</c> or <c>Armor Class</c> and then a number.</summary>
    [GeneratedRegex(@"^[\s\-*+>_|]*(?:AC|Armor Class)[\s*_:]*\d", RegexOptions.IgnoreCase)]
    private static partial Regex ArmorClass();

    /// <summary>A line that starts, after list marks, <c>&gt;</c> and emphasis, with <c>HP</c> or <c>Hit Points</c> and then a number.</summary>
    [GeneratedRegex(@"^[\s\-*+>_|]*(?:HP|Hit Points)[\s*_:]*\d", RegexOptions.IgnoreCase)]
    private static partial Regex HitPoints();
}
