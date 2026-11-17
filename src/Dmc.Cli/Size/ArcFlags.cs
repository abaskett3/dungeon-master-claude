using System.Text.RegularExpressions;

namespace Dmc.Size;

/// <summary>
/// The Arc plan flags <c>dmc size NAME --arc</c> prints (spec 016 FR-026, research R2, contracts/size-arc-flags.md): signs
/// that play has overtaken the plan in <c>story/dm-notes.md</c>, for the full save alone to fix. They are not part of
/// <c>--check</c>, which the resume runs and whose leftover lines reach the player, so the player never sees them. Each
/// line starts <see cref="Prefix"/>. Nothing is printed unless the Arc plan holds something besides its Character arcs
/// and Last played line: no file records that a module is finished, and a module campaign keeps only Character arcs
/// there until it is (the template's comment).
/// </summary>
internal static partial class ArcFlags
{
    /// <summary>The start of every line.</summary>
    internal const string Prefix = "arc: ";

    /// <summary>The section of <c>dm-notes.md</c> with each antagonist's next move.</summary>
    const string Antagonists = "Antagonists";

    /// <summary>The current stage's start: <c>current since s12</c>.</summary>
    [GeneratedRegex(@"current since s(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex CurrentSince();

    /// <summary>A stage's budget: <c>about 4 sessions</c>.</summary>
    [GeneratedRegex(@"about (\d+) sessions?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Budget();

    /// <summary>A next move's session: <c>by s14</c>.</summary>
    [GeneratedRegex(@"\bby s\d+")]
    private static partial Regex BySession();

    /// <summary>DONE or RESOLVED as a whole word, in capitals, as <see cref="Flags"/> reads a finished line.</summary>
    [GeneratedRegex(@"\b(DONE|RESOLVED)\b")]
    private static partial Regex DoneWord();

    /// <summary>The word dead, in any case.</summary>
    [GeneratedRegex(@"\bdead\b", RegexOptions.IgnoreCase)]
    private static partial Regex DeadWord();

    /// <summary>A sheet's Status line whose value starts with dead, in any case, bold or not.</summary>
    [GeneratedRegex(@"^\s*-\s*\*\*Status\*\*\s*:\s*\**\s*dead\b", RegexOptions.IgnoreCase)]
    private static partial Regex DeadStatus();

    /// <summary><c>game-state.md</c>'s current session.</summary>
    [GeneratedRegex(@"^\s*-\s*\*\*Current session\*\*\s*:\s*(\d+)")]
    private static partial Regex CurrentSession();

    /// <summary>The Character arcs item of the Arc plan, a top-level list item.</summary>
    [GeneratedRegex(@"^[-*]\s+(\*\*)?Character arcs\b", RegexOptions.IgnoreCase)]
    private static partial Regex CharacterArcs();

    /// <summary>A Last played line.</summary>
    [GeneratedRegex(@"^[\s\-*>]*Last played\b", RegexOptions.IgnoreCase)]
    private static partial Regex LastPlayed();

    /// <summary>
    /// The Arc plan flags of a campaign, in the contract's order: a dead person named ahead (one line each), no current
    /// stage, no budget on it, over its budget, and a living antagonist's next move with no session (one line each). None when
    /// the Arc plan holds no plan (see the class summary) or <c>dm-notes.md</c> is missing.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>.</param>
    /// <exception cref="DmcException">A file cannot be read.</exception>
    public static List<string> Find(string dmcFolder, string campaign)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        string notesPath = Path.Combine(root, "story", "dm-notes.md");
        if (!File.Exists(notesPath))
            return [];
        var notes = Measurement.ReadLines(notesPath);
        var plan = Content(Measurement.SectionLines(notes, Flags.ArcPlan));
        if (!HoldsAPlan(plan))
            return [];
        var antagonists = Content(Measurement.SectionLines(notes, Antagonists));

        var lines = new List<string>();
        var dead = DeadTitles(root);
        foreach (string title in dead)
        {
            bool named = plan.Concat(antagonists).Any(l => !DoneWord().IsMatch(l) && !DeadWord().IsMatch(l) && ReadSet.Mentions(l, title));
            if (named)
                lines.Add($"{Prefix}the plan still names {title}, whose sheet says dead: revise the stages ahead (step 5, Revise)");
        }

        string? current = plan.FirstOrDefault(l => CurrentSince().IsMatch(l));
        if (current is null)
            lines.Add($"{Prefix}no current stage (current since sN): add it (step 5, Arc plan)");
        else if (Budget().Match(current) is not { Success: true } budget)
            lines.Add($"{Prefix}the current stage has no budget in sessions: add one (step 5, Arc plan)");
        else
        {
            int since = int.Parse(CurrentSince().Match(current).Groups[1].ValueSpan);
            int sessions = int.Parse(budget.Groups[1].ValueSpan);
            if (SessionNow(root) is { } now && now > since + sessions)
                lines.Add($"{Prefix}the current stage began at s{since} with a budget of {sessions} and this is s{now}: tighten or revise (step 5, Pace)");
        }

        // A dead antagonist has no next move: the first flag already asks for the plan to drop them.
        foreach (var (name, block) in Entries(antagonists))
            if (!dead.Any(t => ReadSet.Mentions(name, t)) && !block.Any(l => BySession().IsMatch(l)))
                lines.Add($"{Prefix}{name}'s next move has no session (by sN): add one (step 5, Clocks)");
        return lines;
    }

    /// <summary>The texts of a section's lines outside HTML comments, its heading and blank lines left out.</summary>
    /// <param name="section">The section's lines, heading first.</param>
    static List<string> Content(IReadOnlyList<Line> section) =>
        ReadSet.OutsideComments(section.Skip(1)).Where(t => t.Trim().Length > 0).ToList();

    /// <summary>
    /// Whether an Arc plan holds a plan: a line besides its Character arcs item (with the lines indented under it) and
    /// its Last played line.
    /// </summary>
    /// <param name="plan">The Arc plan's content lines.</param>
    static bool HoldsAPlan(IReadOnlyList<string> plan)
    {
        bool inArcs = false;
        foreach (string line in plan)
        {
            bool indented = line.Length > 0 && char.IsWhiteSpace(line[0]);
            if (CharacterArcs().IsMatch(line))
                inArcs = true;
            else if (inArcs && indented)
                continue;
            else
            {
                inArcs = false;
                if (!LastPlayed().IsMatch(line))
                    return true;
            }
        }
        return false;
    }

    /// <summary>The titles of the campaign's sheets whose Status says dead; <c>minor-npcs.md</c> and the history files are not sheets.</summary>
    /// <param name="root">The campaign folder.</param>
    static List<string> DeadTitles(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (!Directory.Exists(folder))
            return [];
        var titles = new List<string>();
        foreach (string path in Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            if (file == "minor-npcs.md" || file.EndsWith("-history.md", StringComparison.Ordinal))
                continue;
            var lines = Measurement.ReadLines(path);
            if (!lines.Any(l => DeadStatus().IsMatch(l.Text)))
                continue;
            var first = lines.FirstOrDefault(l => l.Text.StartsWith("# ", StringComparison.Ordinal));
            titles.Add(first.Text is { } t ? t[2..].Trim() : Path.GetFileNameWithoutExtension(file));
        }
        return titles;
    }

    /// <summary><c>game-state.md</c>'s current session, or null when it is missing or has none.</summary>
    /// <param name="root">The campaign folder.</param>
    static int? SessionNow(string root)
    {
        string path = Path.Combine(root, "game-state.md");
        if (!File.Exists(path))
            return null;
        foreach (var line in Measurement.ReadLines(path))
            if (CurrentSession().Match(line.Text) is { Success: true } match)
                return int.Parse(match.Groups[1].ValueSpan);
        return null;
    }

    /// <summary>
    /// The Antagonists entries, each its name and its lines: a <c>### </c> block each, or, where the section has none, a
    /// top-level list item each (the template's "For each"), named by its bold name or its first words.
    /// </summary>
    /// <param name="antagonists">The section's content lines.</param>
    static List<(string Name, List<string> Lines)> Entries(IReadOnlyList<string> antagonists)
    {
        bool blocks = antagonists.Any(l => l.StartsWith("### ", StringComparison.Ordinal));
        var entries = new List<(string Name, List<string> Lines)>();
        foreach (string line in antagonists)
        {
            if (blocks ? line.StartsWith("### ", StringComparison.Ordinal) : line.StartsWith("- ", StringComparison.Ordinal))
                entries.Add((EntryName(blocks ? line[4..] : line[2..]), [line]));
            else if (entries.Count > 0)
                entries[^1].Lines.Add(line);
        }
        return entries;
    }

    /// <summary>An entry's name: its bold part, else its text up to a bracket or colon.</summary>
    /// <param name="text">The heading or item text, without its mark.</param>
    static string EntryName(string text)
    {
        var bold = Regex.Match(text, @"\*\*(.+?)\*\*");
        string name = bold.Success ? bold.Groups[1].Value : text;
        int cut = name.IndexOfAny(['(', ':']);
        return (cut > 0 ? name[..cut] : name).Trim();
    }
}
