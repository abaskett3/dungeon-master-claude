using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.Campaign;

/// <summary>What a check looks at: the campaign as backed up, the campaign as migrated, and the accounting.</summary>
/// <param name="Old">The backup's campaign files as text, by path.</param>
/// <param name="New">The migrated campaign's files as text, by path.</param>
/// <param name="Accounting">The accounting DMC wrote.</param>
internal sealed record CheckInput(IReadOnlyDictionary<string, string> Old, IReadOnlyDictionary<string, string> New, AccountingFile Accounting);

/// <summary>
/// The built-in shape checks a migration names in its <c>checks</c> (data-model.md § Check; research R7). Each is a small
/// named function that returns one failure line per problem, so a file left in the old shape fails verification whatever
/// DMC wrote in the accounting (FR-010). A migration can name only checks registered here; a test checks it.
/// </summary>
internal static partial class MigrationChecks
{
    /// <summary>The folder of the character sheets.</summary>
    const string Sheets = "character-sheets/";

    /// <summary>The DM notes, which hold the Opposition table and the Last played line.</summary>
    const string DmNotes = "story/dm-notes.md";

    /// <summary>Where finished fights and closed threads go (PR #173).</summary>
    const string DmNotesArchive = "story/dm-notes-archive.md";

    /// <summary>The canon, whose Timeline gets a line per finished fight.</summary>
    const string Canon = "story/canon.md";

    /// <summary>The headings a sheet's history goes under: the current one and the two older ones.</summary>
    static readonly string[] HistoryHeadings = ["History", "Notes", "History with the PC"];

    /// <summary>The registry: each check's name and function.</summary>
    static readonly Dictionary<string, Func<CheckInput, IEnumerable<string>>> Registry = new(StringComparer.Ordinal)
    {
        ["sheets-end-with-history"] = SheetsEndWithHistory,
        ["npc-sheets-have-names-line"] = NpcSheetsHaveNamesLine,
        ["no-dead-opposition-rows"] = NoDeadOppositionRows,
        ["fights-have-timeline"] = FightsHaveTimeline,
        ["one-last-played-line"] = OneLastPlayedLine,
        ["continuity-section"] = ContinuitySection,
    };

    /// <summary>The campaign's own player options, which hold the Continuity section (spec 011).</summary>
    const string PlayerOptions = "player-options.md";

    /// <summary>The Continuity section's three lines, by name (spec 011 FR-001).</summary>
    static readonly string[] ContinuityLines = ["Continues", "Side campaign of", "Shares a world with"];

    /// <summary>The names of every check, in the order they are registered.</summary>
    public static IReadOnlyCollection<string> Names => Registry.Keys;

    /// <summary>True when a check of this name exists.</summary>
    /// <param name="name">A name from a migration's <c>checks</c>.</param>
    public static bool Exists(string name) => Registry.ContainsKey(name);

    /// <summary>Runs one check.</summary>
    /// <param name="name">The check's name.</param>
    /// <param name="input">What it looks at.</param>
    /// <returns>Its failure lines, each starting with the check's name; none when it passes.</returns>
    /// <exception cref="DmcException">No check has this name.</exception>
    public static IEnumerable<string> Run(string name, CheckInput input) =>
        Registry.TryGetValue(name, out var check)
            ? check(input).Select(line => $"{name}: {line}")
            : throw new DmcException($"no check named {name}: update dmc");

    /// <summary>An HTML comment, possibly spanning lines.</summary>
    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Comment { get; }

    /// <summary>A session reference such as <c>s12</c> or <c>s12.3</c>.</summary>
    [GeneratedRegex(@"\bs\d+(?:\.\d+)?\b", RegexOptions.CultureInvariant)]
    private static partial Regex SessionReference { get; }

    /// <summary>A line that starts, after list marks, quote marks and bold, with "Last played" (the form <c>dmc size</c> looks for).</summary>
    [GeneratedRegex(@"^[\s\-*>_]*Last played\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LastPlayed { get; }

    /// <summary>The character sheets of a campaign that have sections: <c>pc-*.md</c> and <c>npc-*.md</c>, not their <c>-history.md</c> files.</summary>
    /// <param name="files">The campaign's files.</param>
    /// <param name="npcOnly">True for the NPC and companion sheets only.</param>
    static IEnumerable<KeyValuePair<string, string>> SheetsOf(IReadOnlyDictionary<string, string> files, bool npcOnly) =>
        files.Where(f =>
        {
            if (!f.Key.StartsWith(Sheets, StringComparison.Ordinal) || !f.Key.EndsWith(".md", StringComparison.Ordinal))
                return false;
            string name = f.Key[Sheets.Length..];
            if (name.Contains('/') || name.EndsWith("-history.md", StringComparison.Ordinal))
                return false;
            return name.StartsWith("npc-", StringComparison.Ordinal) || (!npcOnly && name.StartsWith("pc-", StringComparison.Ordinal));
        }).OrderBy(f => f.Key, StringComparer.Ordinal);

    /// <summary>A file's lines with its HTML comments taken out (each becomes its line breaks).</summary>
    /// <param name="text">The file's text.</param>
    static string[] Lines(string text) =>
        Comment.Replace(text.Replace("\r\n", "\n"), m => new string('\n', m.Value.Count(c => c == '\n'))).Split('\n');

    /// <summary>The <c>## </c> headings of a file, without the <c>## </c>.</summary>
    /// <param name="text">The file's text.</param>
    static List<string> SectionHeadings(string text) =>
        [.. Lines(text).Where(l => l.StartsWith("## ", StringComparison.Ordinal)).Select(l => l[3..].Trim())];

    /// <summary>The lines of one <c>## </c> section, without its heading; null when the file has no such section.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="heading">The heading, compared ignoring case.</param>
    static List<string>? Section(string text, string heading)
    {
        List<string>? lines = null;
        foreach (string line in Lines(text))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (lines is not null)
                    break;
                if (line[3..].Trim().Equals(heading, StringComparison.OrdinalIgnoreCase))
                    lines = [];
                continue;
            }
            lines?.Add(line);
        }
        return lines;
    }

    /// <summary>The rows of the first table in some lines, after its header and separator; null when there is no table.</summary>
    /// <param name="lines">A section's lines.</param>
    static List<string>? TableRows(List<string> lines)
    {
        var table = lines.Select(l => l.Trim()).SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')).ToList();
        if (table.Count < 2 || ContentUnits.Normalise(table[1]) is not null)
            return null;
        return table[2..];
    }

    /// <summary>The first cell of a table row, without bold or italic marks.</summary>
    /// <param name="row">A table row.</param>
    static string Who(string row)
    {
        string[] cells = row.Trim().Split('|');
        return cells.Length < 2 ? "" : cells[1].Replace("**", "").Replace("__", "").Trim();
    }

    /// <summary>The finished rows of the Opposition table of a <c>dm-notes.md</c>: those whose second cell starts <c>0 /</c> or <c>0/</c>.</summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The rows, or null when there is no Opposition section with a table.</returns>
    static List<string>? DeadRows(string text) =>
        Section(text, "Opposition") is { } section && TableRows(section) is { } rows ? [.. rows.Where(Flags.NoneAlive)] : null;

    /// <summary><c>sheets-end-with-history</c> (migration 002): a PC or NPC sheet with a history section has it as its last <c>## </c> section.</summary>
    /// <param name="input">The campaigns.</param>
    static IEnumerable<string> SheetsEndWithHistory(CheckInput input)
    {
        foreach (var (path, text) in SheetsOf(input.New, npcOnly: false))
        {
            var headings = SectionHeadings(text);
            bool IsHistory(string h) => HistoryHeadings.Contains(h, StringComparer.OrdinalIgnoreCase);
            int history = headings.FindIndex(IsHistory);
            // Everything from the first history section on is history: two history-like sections may end a sheet together.
            if (history >= 0 && headings.Skip(history).FirstOrDefault(h => !IsHistory(h)) is { } after)
                yield return $"{path}: ## {headings[history]} is not the last section (## {after} follows it)";
        }
    }

    /// <summary><c>npc-sheets-have-names-line</c> (migration 003): every NPC or companion sheet has the template's "Names they use" line.</summary>
    /// <param name="input">The campaigns.</param>
    static IEnumerable<string> NpcSheetsHaveNamesLine(CheckInput input)
    {
        foreach (var (path, text) in SheetsOf(input.New, npcOnly: true))
        {
            if (!Lines(text).Any(l => l.Contains("Names they use", StringComparison.OrdinalIgnoreCase)))
                yield return $"{path} has no Names they use line";
        }
    }

    /// <summary><c>no-dead-opposition-rows</c> (migration 004): the Opposition table of <c>story/dm-notes.md</c> has no row with none left alive.</summary>
    /// <param name="input">The campaigns.</param>
    static IEnumerable<string> NoDeadOppositionRows(CheckInput input)
    {
        if (!input.New.TryGetValue(DmNotes, out string? text) || DeadRows(text) is not { } dead)
        {
            yield return $"no Opposition table found in {DmNotes}";
            yield break;
        }
        foreach (string row in dead)
            yield return $"{DmNotes} Opposition still has {Who(row)}, with none left alive: move the row to {DmNotesArchive}";
    }

    /// <summary>
    /// <c>fights-have-timeline</c> (migration 004): every Opposition row that was finished in the backup is a row of
    /// <c>story/dm-notes-archive.md</c>, and a line under <c>## Timeline</c> in <c>story/canon.md</c> carries a session
    /// reference and the row's "Who" text (ignoring case).
    /// </summary>
    /// <param name="input">The campaigns.</param>
    static IEnumerable<string> FightsHaveTimeline(CheckInput input)
    {
        if (!input.Old.TryGetValue(DmNotes, out string? old) || DeadRows(old) is not { Count: > 0 } dead)
            yield break;
        var archived = input.New.TryGetValue(DmNotesArchive, out string? archive)
            ? Lines(archive).Where(l => l.TrimStart().StartsWith('|')).Select(Who).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var timeline = input.New.TryGetValue(Canon, out string? canon) && Section(canon, "Timeline") is { } section
            ? section.Where(l => SessionReference.IsMatch(l)).ToList()
            : [];
        foreach (string row in dead)
        {
            string who = Who(row);
            if (!archived.Contains(who))
                yield return $"{who}: the finished row is not in {DmNotesArchive}";
            if (!timeline.Any(l => l.Contains(who, StringComparison.OrdinalIgnoreCase)))
                yield return $"{who}: no line under ## Timeline in {Canon} names it with a session reference (sN.M)";
        }
    }

    /// <summary><c>one-last-played-line</c> (migration 004): <c>story/dm-notes.md</c> has at most one Last played line and no Mix tracker section.</summary>
    /// <param name="input">The campaigns.</param>
    static IEnumerable<string> OneLastPlayedLine(CheckInput input)
    {
        if (!input.New.TryGetValue(DmNotes, out string? text))
            yield break;
        string[] lines = Lines(text);
        if (lines.Any(l => l.TrimStart().StartsWith('#') && l.Contains("Mix tracker", StringComparison.OrdinalIgnoreCase)))
            yield return $"{DmNotes} still has a Mix tracker section: one Last played line replaces it";
        int count = lines.Count(l => LastPlayed.IsMatch(l));
        if (count > 1)
            yield return $"{DmNotes} has {count} Last played lines, where one belongs";
    }

    /// <summary>
    /// <c>continuity-section</c> (migration 005, spec 011 R14): the campaign's <c>player-options.md</c> has no Predecessor
    /// line left, and has a <c>## Continuity</c> section holding its three lines (each <c>none</c> or names). A campaign
    /// with no player options is not checked.
    /// </summary>
    /// <param name="input">The migrated campaign.</param>
    static IEnumerable<string> ContinuitySection(CheckInput input)
    {
        if (!input.New.TryGetValue(PlayerOptions, out string? text))
            yield break;
        string[] lines = Lines(text);
        if (lines.Any(l => PredecessorLine.IsMatch(l)))
            yield return $"{PlayerOptions} still has a Predecessor line: the Continuity section replaces it";
        int start = Array.FindIndex(lines, l => l.TrimEnd() == "## Continuity");
        if (start < 0)
        {
            yield return $"{PlayerOptions} has no ## Continuity section";
            yield break;
        }
        var section = lines.Skip(start + 1).TakeWhile(l => !l.StartsWith('#')).ToList();
        foreach (string name in ContinuityLines)
        {
            if (!section.Any(l => l.TrimStart().StartsWith($"- **{name}**:", StringComparison.Ordinal)))
                yield return $"{PlayerOptions}'s Continuity section has no {name} line";
        }
    }

    /// <summary>The old Predecessor line of a campaign's player options, as recall read it before spec 011.</summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*\*\*Predecessor\*\*:", RegexOptions.CultureInvariant)]
    private static partial Regex PredecessorLine { get; }
}
