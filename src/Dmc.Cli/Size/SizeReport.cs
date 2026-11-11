using System.Text;

namespace Dmc.Size;

/// <summary>A campaign measured: its read set and the flags found in it.</summary>
/// <param name="Campaign">The campaign's folder name.</param>
/// <param name="Parts">The read set, in the resume's order.</param>
/// <param name="Flags">The flags, in the order they are printed.</param>
internal sealed record SizeResult(string Campaign, IReadOnlyList<Part> Parts, IReadOnlyList<Flag> Flags)
{
    /// <summary>The estimated tokens of everything the resume reads.</summary>
    public long Total => Parts.Sum(p => p.Tokens);
}

/// <summary>
/// The work of <c>dmc size</c>: finding the campaign, measuring it (<see cref="Measure"/>, which issue #172's migration
/// can call in-process), and writing the full report. Nothing here writes, moves or deletes a file.
/// </summary>
internal static class SizeReport
{
    /// <summary>The most sections the full report lists for one file.</summary>
    const int SectionsShown = 4;

    /// <summary>The widest a path is shown before it is shortened in the middle.</summary>
    const int PathWidth = 46;

    /// <summary>The widest a section heading is shown before it is cut.</summary>
    const int HeadingWidth = 32;

    /// <summary>The files whose largest sections the full report lists: the ones the full save trims and the resume reads whole.</summary>
    static readonly BudgetKind[] SectionedKinds = [BudgetKind.GameState, BudgetKind.Summary, BudgetKind.DmNotes, BudgetKind.Canon];

    /// <summary>
    /// The campaign to measure: <paramref name="name"/>, or the first line of <c>adventures/.active-campaign</c>. It must be
    /// a folder in <c>adventures/</c> whose name does not start with a dot (a staging folder an interrupted command left).
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="name">The campaign named on the command line, or null.</param>
    /// <param name="usage">How the "no active campaign" failure tells the user to name one; <c>dmc recall</c> passes its own.</param>
    /// <returns>The campaign's folder name.</returns>
    /// <exception cref="DmcException">No campaign is named or active, or the name is not a campaign (contract § Failures).</exception>
    public static string Target(string dmcFolder, string? name, string usage = "dmc size NAME")
    {
        string adventures = Path.Combine(dmcFolder, "adventures");
        name ??= ActiveName(adventures) ?? throw new DmcException($"no active campaign: name one ({usage})");
        if (name.StartsWith('.'))
            throw new DmcException($"{name} is not a campaign (a folder starting with a dot is a staging folder)");
        // One plain folder name: not empty or blank, no separator, no drive (C: is a folder on Windows), not "..".
        if (name.Trim().Length == 0 || name.IndexOfAny(['/', '\\', ':']) >= 0 || name is ".." || !Directory.Exists(Path.Combine(adventures, name)))
            throw new DmcException($"no campaign {name} in adventures/");
        return name;
    }

    /// <summary>The active campaign's name: the first line of <c>adventures/.active-campaign</c>, trimmed and without a byte-order mark. <c>dmc size</c> and <c>dmc campaign standard</c> both read it here.</summary>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <returns>The name, or null when the file is missing or its first line is blank.</returns>
    /// <exception cref="DmcException">The file cannot be read.</exception>
    internal static string? ActiveName(string adventures)
    {
        string active = Path.Combine(adventures, ".active-campaign");
        string? name = File.Exists(active) ? Measurement.ReadLines(active).Select(l => l.Text.Trim().TrimStart('﻿')).FirstOrDefault() : null;
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>Measures a campaign: its read set (<see cref="ReadSet.Build"/>) and its flags (<see cref="Flags.Find"/>). Writes nothing.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name, as <see cref="Target"/> returns it.</param>
    /// <exception cref="DmcException">A file cannot be read.</exception>
    public static SizeResult Measure(string dmcFolder, string campaign)
    {
        var parts = ReadSet.Build(dmcFolder, campaign);
        return new SizeResult(campaign, parts, Flags.Find(parts));
    }

    /// <summary>
    /// The full report (contract § Full report): a header, one line per file with its tokens, budget or limit and what is
    /// read, the total, the largest sections of the files read whole, and the flags or <c>nothing to flag</c>.
    /// </summary>
    /// <param name="result">The measured campaign.</param>
    public static List<string> FullReport(SizeResult result)
    {
        var lines = new List<string> { $"{result.Campaign}: what a resume reads, in estimated tokens (bytes / 4)" };
        int width = Math.Min(PathWidth, result.Parts.Select(p => p.Path.Length).Append("total".Length).Max());
        foreach (var part in result.Parts)
        {
            string limit = part.Budget == BudgetKind.Canon ? $"limit {Fmt(Budgets.CanonLimit)}"
                : Budgets.Of(part.Budget) is { } b ? $"budget {Fmt(b)}" : "";
            var notes = new List<string>();
            if (part.Note is { } note)
                notes.Add(note);
            if (part.State == EntryState.Measured && Budgets.Of(part.Budget) is { } over && part.Tokens > over)
                notes.Add("over");
            if (part.State == EntryState.Measured && (part.Tokens >= Budgets.NearCapTokens || part.Lines.Count >= Budgets.NearCapLines))
                notes.Add("near the Read cap");
            string tokens = part.State == EntryState.Measured ? Fmt(part.Tokens) : "";
            lines.Add($"  {Shorten(part.Path, width).PadRight(width)} {tokens,6}   {limit,-11} {string.Join(", ", notes)}".TrimEnd());
            if (part.Kind == PartKind.AlwaysReadSections)
                lines.Add("    (the search for the scene's names is not counted)");
        }
        lines.Add($"  {"total".PadRight(width)} {Fmt(result.Total),6}");

        // Canon's sections are those of the whole file even when it is read in sections: that is where the archive step looks.
        var sectioned = result.Parts
            .Where(p => p.State == EntryState.Measured && SectionedKinds.Contains(p.Budget)
                        && (p.Kind == PartKind.Whole || p.Budget == BudgetKind.Canon))
            .Select(p => (Part: p, Sections: p.Budget == BudgetKind.Canon ? Measurement.Sections(p.FileLines) : p.Sections.ToList()))
            .Where(x => x.Sections.Count > 1).ToList();
        if (sectioned.Count > 0)
        {
            lines.Add("largest sections:");
            foreach (var (part, sections) in sectioned)
            {
                var largest = sections.OrderByDescending(s => s.Bytes).ToList();
                string shown = string.Join(", ", largest.Take(SectionsShown).Select(s => $"{Cut(s.Heading, HeadingWidth)} {Fmt(s.Tokens)}"));
                string more = largest.Count > SectionsShown ? $" (+{largest.Count - SectionsShown} more)" : "";
                lines.Add($"  {part.Path}: {shown}{more}");
            }
        }

        lines.Add("flags:");
        if (result.Flags.Count == 0)
            lines.Add("  nothing to flag");
        lines.AddRange(result.Flags.Select(f => "  " + f.Line));
        return lines;
    }

    /// <summary>A heading cut to a width, ending in <c>...</c> when it was cut.</summary>
    /// <param name="text">The heading.</param>
    /// <param name="width">The most characters it may take.</param>
    static string Cut(string text, int width) => text.Length <= width ? text : text[..(width - 3)].TrimEnd() + "...";

    /// <summary>A path cut to a width, keeping its start and its end with <c>...</c> between them.</summary>
    /// <param name="path">The path.</param>
    /// <param name="width">The most characters it may take.</param>
    static string Shorten(string path, int width)
    {
        if (path.Length <= width)
            return path;
        int keep = width - 3;
        return new StringBuilder().Append(path, 0, keep - keep / 2).Append("...").Append(path, path.Length - keep / 2, keep / 2).ToString();
    }

    /// <summary>A token count as the report writes it (<see cref="Measurement.FormatTokens"/>).</summary>
    /// <param name="tokens">Estimated tokens.</param>
    static string Fmt(long tokens) => Measurement.FormatTokens(tokens);
}
