using System.Text.RegularExpressions;

namespace Dmc.Size;

/// <summary>The kinds of flag, in the order their lines are printed: canon over its limit first, because the resume acts on it before it reads.</summary>
internal enum FlagKind
{
    /// <summary>Canon is at or over <see cref="Budgets.CanonLimit"/>, so the resume reads it in sections.</summary>
    CanonOverLimit,

    /// <summary>The part read is near the Read tool's cap for one call.</summary>
    NearCap,

    /// <summary>A file the full save trims is over its budget.</summary>
    OverBudget,

    /// <summary>A section of <c>dm-notes.md</c> with a budget of its own is over it.</summary>
    SectionOverBudget,

    /// <summary><c>dm-notes.md</c> keeps a log of sessions where one Last played line belongs.</summary>
    LastPlayedLog,

    /// <summary><c>dm-notes.md</c>'s Opposition table has rows with none left alive.</summary>
    FinishedOpposition,

    /// <summary><c>dm-notes.md</c> has lines marked DONE or RESOLVED outside the Arc plan.</summary>
    DoneLines,

    /// <summary>Canon's Prices, debts and promises section is large for its kind.</summary>
    PricesSection,

    /// <summary>A file read whole has lines over <see cref="Budgets.LongLineCharacters"/> characters.</summary>
    LongLines,
}

/// <summary>A condition worth acting on, with the file it concerns and the line the report prints for it.</summary>
/// <param name="Kind">What kind of flag it is.</param>
/// <param name="Path">The file it concerns, as the report shows it.</param>
/// <param name="Line">The line printed for it, in the full report and by <c>--check</c>.</param>
/// <param name="HoldsTheSave">True when the full save must fix it or say why it stays before it is complete (FR-015a): every flag on a file the full save trims.</param>
internal sealed record Flag(FlagKind Kind, string Path, string Line, bool HoldsTheSave);

/// <summary>Finds the flags of a read set (research R3, R6) and writes the lines of <c>dmc size --check</c>.</summary>
internal static partial class Flags
{
    /// <summary>The section of <c>dm-notes.md</c> where a finished stage is legitimately one line marked done.</summary>
    const string ArcPlan = "Arc plan";

    /// <summary>The section of <c>dm-notes.md</c> about the player.</summary>
    const string AboutThePlayer = "About this player";

    /// <summary>DONE or RESOLVED as a whole word, in capitals.</summary>
    [GeneratedRegex(@"\b(DONE|RESOLVED)\b")]
    private static partial Regex DoneWord();

    /// <summary>A line that starts, after list marks and bold, with "Last played".</summary>
    [GeneratedRegex(@"^[\s\-*>]*Last played\b", RegexOptions.IgnoreCase)]
    private static partial Regex LastPlayedLine();

    /// <summary>Every flag of a read set, in <see cref="FlagKind"/> order, then the read set's order.</summary>
    /// <param name="parts">The read set, measured.</param>
    public static List<Flag> Find(IReadOnlyList<Part> parts)
    {
        var flags = new List<Flag>();
        foreach (var part in parts.Where(p => p.State == EntryState.Measured))
        {
            bool trimmed = part.Budget != BudgetKind.None;
            if (part.Budget == BudgetKind.Canon && part.FileTokens >= Budgets.CanonLimit)
                flags.Add(new Flag(FlagKind.CanonOverLimit, part.Path,
                    $"{part.Path} {Fmt(part.FileTokens)} is over its {Fmt(Budgets.CanonLimit)} limit: read it in sections; "
                    + $"a full save archives it down to {Fmt(Budgets.CanonArchiveMark)}", true));

            if (part.Tokens >= Budgets.NearCapTokens || part.Lines.Count >= Budgets.NearCapLines)
                flags.Add(new Flag(FlagKind.NearCap, part.Path,
                    $"{part.Path} {Fmt(part.Tokens)} is near the Read cap ({Fmt(Budgets.ReadCapTokens)}): it may be cut short at resume", trimmed));

            if (Budgets.Of(part.Budget) is { } budget && part.Tokens > budget)
                flags.Add(new Flag(FlagKind.OverBudget, part.Path, $"{part.Path} {Fmt(part.Tokens)} is over its {Fmt(budget)} budget", true));

            if (part.Budget == BudgetKind.DmNotes)
                flags.AddRange(DmNotesFlags(part));
            if (part.Budget == BudgetKind.Canon)
                flags.AddRange(CanonFlags(part));

            // Only the files the full save trims (a session log or a digest is prose nobody splits, so a flag there could never
            // be fixed), and only their content: the templates' own guidance, in the preamble and in comments, is not counted.
            if (trimmed && part.Kind == PartKind.Whole && Measurement.ContentLines(part.Lines).Count(l => l.Text.Length > Budgets.LongLineCharacters) is var longLines and > 0)
                flags.Add(new Flag(FlagKind.LongLines, part.Path,
                    $"{part.Path}: {Count(longLines, "line")} over {Budgets.LongLineCharacters} characters", true));
        }
        return flags.OrderBy(f => f.Kind).ToList(); // a stable sort: the read set's order within a kind
    }

    /// <summary>The flags of <c>dm-notes.md</c>: its sections over their budgets, a session log of Last played lines, finished Opposition rows, and DONE or RESOLVED lines outside the Arc plan.</summary>
    /// <param name="part">The measured <c>dm-notes.md</c>.</param>
    static IEnumerable<Flag> DmNotesFlags(Part part)
    {
        foreach (var section in part.Sections)
        {
            int? budget = section.Heading.StartsWith(AboutThePlayer, StringComparison.OrdinalIgnoreCase) ? Budgets.DmNotesAboutThePlayer
                : section.Heading.Equals(ArcPlan, StringComparison.OrdinalIgnoreCase) ? Budgets.DmNotesArcPlan
                : null;
            if (budget is { } b && section.Tokens > b)
                yield return new Flag(FlagKind.SectionOverBudget, part.Path,
                    $"{part.Path} {section.Heading}: {Fmt(section.Tokens)}, over its {Fmt(b)} budget", true);
        }

        if (part.Sections.Any(s => s.Heading.Contains("Mix tracker", StringComparison.OrdinalIgnoreCase)))
            yield return new Flag(FlagKind.LastPlayedLog, part.Path,
                $"{part.Path} Mix tracker: a log of sessions, where one Last played line belongs", true);
        else if (part.Lines.Count(l => LastPlayedLine().IsMatch(l.Text)) is var lastPlayed and > 1)
            yield return new Flag(FlagKind.LastPlayedLog, part.Path,
                $"{part.Path}: {lastPlayed} Last played lines, where one belongs", true);

        int finished = Measurement.SectionLines(part.Lines, "Opposition").Count(l => NoneAlive(l.Text));
        if (finished > 0)
            yield return new Flag(FlagKind.FinishedOpposition, part.Path,
                $"{part.Path} Opposition: {Count(finished, "row")} with none left alive", true);

        int done = Measurement.ContentLines(WithoutSection(part.Lines, ArcPlan)).Count(l => DoneWord().IsMatch(l.Text));
        if (done > 0)
            yield return new Flag(FlagKind.DoneLines, part.Path, $"{part.Path}: {Count(done, "line")} marked DONE or RESOLVED", true);
    }

    /// <summary>The lines of a file without one <c>## </c> section (its heading included), compared ignoring case.</summary>
    /// <param name="lines">The lines of a file.</param>
    /// <param name="heading">The section's heading, without <c>## </c>.</param>
    static List<Line> WithoutSection(IReadOnlyList<Line> lines, string heading)
    {
        var kept = new List<Line>();
        bool inside = false;
        foreach (var line in lines)
        {
            if (Measurement.IsSectionHeading(line.Text))
                inside = line.Text[3..].Trim().Equals(heading, StringComparison.OrdinalIgnoreCase);
            if (!inside)
                kept.Add(line);
        }
        return kept;
    }

    /// <summary>Whether a line is an Opposition table row whose "How many (alive / started)" cell, the second, starts with 0 alive.</summary>
    /// <param name="text">A line's text.</param>
    internal static bool NoneAlive(string text)
    {
        string row = text.Trim();
        if (!row.StartsWith('|'))
            return false;
        string[] cells = row.Split('|');
        if (cells.Length < 4)
            return false;
        string count = cells[2].Trim();
        return count.StartsWith("0 /", StringComparison.Ordinal) || count.StartsWith("0/", StringComparison.Ordinal);
    }

    /// <summary>The flags of canon, looked for in the whole file whether it is read whole or in sections: a large Prices, debts and promises section.</summary>
    /// <param name="part">The measured canon.</param>
    static IEnumerable<Flag> CanonFlags(Part part)
    {
        var prices = Measurement.Sections(part.FileLines).FirstOrDefault(s => s.Heading.Equals(ReadSet.Prices, StringComparison.OrdinalIgnoreCase));
        if (prices is not null && prices.Tokens > Budgets.CanonPricesSection)
            yield return new Flag(FlagKind.PricesSection, part.Path,
                $"{part.Path} {ReadSet.Prices}: {Fmt(prices.Tokens)}, which suggests paid debts or kept promises not deleted", true);
    }

    /// <summary>
    /// The lines <c>dmc size --check</c> prints: one per flag, at most <see cref="Budgets.CheckLines"/>; when there are more,
    /// the last line says how many more and how to see them. None for no flags.
    /// </summary>
    /// <param name="flags">The flags, in order.</param>
    /// <param name="campaign">The campaign's name, for the command in the last line.</param>
    public static List<string> CheckLines(IReadOnlyList<Flag> flags, string campaign)
    {
        if (flags.Count <= Budgets.CheckLines)
            return flags.Select(f => f.Line).ToList();
        int shown = Budgets.CheckLines - 1;
        var lines = flags.Take(shown).Select(f => f.Line).ToList();
        lines.Add($"... and {flags.Count - shown} more: run dmc size {campaign}");
        return lines;
    }

    /// <summary>A token count as the report writes it (<see cref="Measurement.FormatTokens"/>).</summary>
    /// <param name="tokens">Estimated tokens.</param>
    static string Fmt(long tokens) => Measurement.FormatTokens(tokens);

    /// <summary>A count and a noun, with an <c>s</c> unless the count is 1.</summary>
    /// <param name="count">How many.</param>
    /// <param name="noun">The noun, singular.</param>
    static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
