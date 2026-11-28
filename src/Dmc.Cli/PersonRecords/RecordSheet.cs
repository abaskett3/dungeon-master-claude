using Dmc.Sheets;
using MarkdownSheet = Dmc.Sheets.SheetText;

namespace Dmc.PersonRecords;

/// <summary>
/// A record printed as a sheet (spec 036 FR-008, data-model.md § Print, research R2): the Markdown a sheet in its
/// template's shape would hold, built from the record's facts and lists on each run and never stored. The readers of a
/// sheet (the resume's read set, the hook's sheet lines, <c>dmc sheet</c>, the sample check) read this print where the
/// Markdown sheet is gone, through <see cref="Sheets(string)"/>, so their parsing, budgets and layouts stay as they are.
/// </summary>
internal static class RecordSheet
{
    /// <summary>The heading of the history section, which always prints last.</summary>
    public const string History = "History";

    /// <summary>The list whose entries print as the attack table of <c>## Combat</c>.</summary>
    public const string Attacks = "Attacks";

    /// <summary>The header of the attack table, as <c>templates/pc-sheet.md</c> has it.</summary>
    const string AttackHeader = "| Attack | To hit | Damage | Range and notes |";

    /// <summary>One sheet of a campaign, as a reader sees it.</summary>
    /// <param name="File">The sheet's file name in <c>character-sheets/</c> (a record's <c>sheet</c>).</param>
    /// <param name="Lines">The sheet's lines: the file's, or the record's print without sources.</param>
    /// <param name="Record">The record printed, or null for a Markdown sheet.</param>
    internal sealed record SheetLines(string File, IReadOnlyList<string> Lines, PersonRecord? Record);

    /// <summary>Whether a sheet file name is a PC's (<c>pc-*.md</c>).</summary>
    /// <param name="file">The file name.</param>
    public static bool IsPc(string file) => file.StartsWith("pc-", StringComparison.Ordinal);

    /// <summary>
    /// The person sheets of a campaign, in file-name order: each Markdown file of <c>character-sheets/</c> that is one
    /// person's (not <c>minor-npcs.md</c> or a <c>-history.md</c> file), then each record that names a <c>sheet</c> whose
    /// file is not there, printed without sources. A record whose sheet file still exists is left to the file: the full
    /// save's check names the pair (<c>sheet-and-record</c>).
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static List<SheetLines> Sheets(string campaignFolder)
    {
        var found = new List<SheetLines>();
        string folder = Path.Combine(campaignFolder, "character-sheets");
        if (Directory.Exists(folder))
            foreach (string path in Directory.EnumerateFiles(folder, "*.md"))
                if (RecordFiles.IsPersonSheet(Path.GetFileName(path)))
                    found.Add(new SheetLines(Path.GetFileName(path), ReadLines(path), null));
        found.AddRange(Printed(campaignFolder));
        return [.. found.OrderBy(s => s.File, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The sheets a campaign's records print, without sources: each record with facts or lists that names a
    /// <c>sheet</c> whose file is not in <c>character-sheets/</c>, in record order. For a reader that reads the Markdown
    /// files its own way (<c>dmc sheet</c>, the resume's read set) and adds these.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static List<SheetLines> Printed(string campaignFolder)
    {
        string folder = Path.Combine(campaignFolder, "character-sheets");
        return [.. RecordFiles.Read(campaignFolder).Records
            .Where(r => r.Sheet is { } sheet && r.HasSheetContent && !File.Exists(Path.Combine(folder, sheet)))
            .Select(r => new SheetLines(r.Sheet!, Print(r, sources: false), r))];
    }

    /// <summary>A print's lines as <c>dmc size</c> measures a file's: each with a line end, UTF-8 bytes and characters.</summary>
    /// <param name="lines">The print's lines.</param>
    public static List<Size.Line> Measured(IEnumerable<string> lines) =>
        Size.Measurement.Split(System.Text.Encoding.UTF8.GetBytes(string.Concat(lines.Select(l => l + "\n"))));

    /// <summary>A file's lines, read as UTF-8 with a byte-order mark removed.</summary>
    /// <param name="path">The file.</param>
    static List<string> ReadLines(string path) => [.. File.ReadAllLines(path, System.Text.Encoding.UTF8)];

    /// <summary>
    /// The record as a sheet's Markdown: the title; the opening fields; <c>## Play card</c> with the card's entries when
    /// it has any; then each section, the template's in its order, then any other in record order, History last. A fact
    /// prints <c>- **Key**: value</c>; the entries of a list named as its section print a bullet each; another list's
    /// print on one line, <c>- **List**: …</c>. With <paramref name="sources"/>, each entry ends with its source in
    /// brackets; a table row never does.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="sources">True to print each entry's source.</param>
    public static List<string> Print(PersonRecord record, bool sources)
    {
        var lines = new List<string> { $"# {record.Person}" };
        var items = Items(record);
        var opening = items.Where(i => !i.Card && i.Section is null).ToList();
        if (opening.Count > 0)
        {
            lines.Add("");
            lines.AddRange(Body(opening, null, sources));
        }
        var card = items.Where(i => i.Card).ToList();
        if (card.Count > 0)
        {
            lines.Add("");
            lines.Add("## " + SheetLayouts.PlayCard);
            lines.Add("");
            lines.AddRange(Body(card, SheetLayouts.PlayCard, sources));
        }
        foreach (string section in Sections(record, items))
        {
            var these = items.Where(i => !i.Card && i.Section is { } s && s.Equals(section, StringComparison.OrdinalIgnoreCase)).ToList();
            lines.Add("");
            lines.Add("## " + section);
            lines.Add("");
            lines.AddRange(Body(these, section, sources));
        }
        return lines;
    }

    /// <summary>
    /// The lines a reader of <paramref name="part"/> gets: to the end of the play card (all of the print when it has none),
    /// to the History heading, or all of it.
    /// </summary>
    /// <param name="print">A record's print.</param>
    /// <param name="part">Which part.</param>
    public static List<string> Part(List<string> print, PrintPart part)
    {
        int Heading(Func<string, bool> which) => print.FindIndex(l => l.StartsWith("## ", StringComparison.Ordinal) && which(l[3..].Trim()));
        int end = part switch
        {
            PrintPart.Card when Heading(h => h.Equals(SheetLayouts.PlayCard, StringComparison.OrdinalIgnoreCase)) is >= 0 and var card
                => print.FindIndex(card + 1, l => l.StartsWith("## ", StringComparison.Ordinal)),
            PrintPart.Card => -1,
            PrintPart.Sheet => Heading(h => h.Equals(History, StringComparison.OrdinalIgnoreCase)),
            _ => -1,
        };
        var lines = end < 0 ? print : print[..end];
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines = lines[..^1];
        return lines;
    }

    /// <summary>One fact or list entry, as the print places it.</summary>
    /// <param name="Name">The fact's key or the entry's list.</param>
    /// <param name="Text">The fact's value or the entry's text.</param>
    /// <param name="Section">Its section; null for the opening fields.</param>
    /// <param name="Card">True when it prints on the play card.</param>
    /// <param name="Source">Its source.</param>
    /// <param name="IsFact">True for a fact.</param>
    sealed record Item(string Name, string Text, string? Section, bool Card, string? Source, bool IsFact);

    /// <summary>The record's facts, then its list entries, leaving out any with no name or no text (the check refuses them).</summary>
    /// <param name="record">The record.</param>
    static List<Item> Items(PersonRecord record) =>
    [
        .. record.Facts.Where(f => !string.IsNullOrWhiteSpace(f.Key) && !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => new Item(f.Key!.Trim(), f.Value!.Trim(), Blank(f.Section), f.Card, f.Source, true)),
        .. record.Lists.Where(e => !string.IsNullOrWhiteSpace(e.List) && !string.IsNullOrWhiteSpace(e.Text))
            .Select(e => new Item(e.List!.Trim(), e.Text!.Trim(), Blank(e.Section), e.Card, e.Source, false)),
    ];

    /// <summary>A section's name, or null when it is blank or the opening fields'.</summary>
    /// <param name="section">The section as written.</param>
    static string? Blank(string? section) => string.IsNullOrWhiteSpace(section) || section.Trim() == MarkdownSheet.Top ? null : section.Trim();

    /// <summary>The sections the print has besides the opening fields and the card: the template's in its order, then the others in record order, History last.</summary>
    /// <param name="record">The record.</param>
    /// <param name="items">Its items.</param>
    static List<string> Sections(PersonRecord record, List<Item> items)
    {
        var used = items.Where(i => !i.Card && i.Section is not null).Select(i => i.Section!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var order = record.Sheet is { } s && IsPc(s) ? SheetLayouts.PcSections : SheetLayouts.NpcSections;
        var known = order.Where(o => o != MarkdownSheet.Top && o != SheetLayouts.PlayCard && !o.Equals(History, StringComparison.OrdinalIgnoreCase))
            .Select(o => used.FirstOrDefault(u => u.Equals(o, StringComparison.OrdinalIgnoreCase))).OfType<string>();
        var others = used.Where(u => !order.Contains(u, StringComparer.OrdinalIgnoreCase));
        var history = used.Where(u => u.Equals(History, StringComparison.OrdinalIgnoreCase));
        return [.. known, .. others, .. history];
    }

    /// <summary>
    /// The lines of one section's items: the ability table first in <c>Abilities</c>, then the facts, then each list in
    /// the order it first appears; the attack table last in <c>Combat</c>.
    /// </summary>
    /// <param name="items">The section's items, in record order.</param>
    /// <param name="section">The section's name; null for the opening fields.</param>
    /// <param name="sources">True to print sources.</param>
    static List<string> Body(List<Item> items, string? section, bool sources)
    {
        var lines = new List<string>();
        bool abilities = section is not null && section.Equals("Abilities", StringComparison.OrdinalIgnoreCase);
        if (abilities)
            lines.AddRange(AbilityTable(items));
        foreach (var fact in items.Where(i => i.IsFact && !(abilities && CheckedKeys.InAbilityTable(i.Name))))
            lines.Add($"- **{fact.Name}**: {fact.Text}{Mark(fact, sources)}");
        bool combat = section is not null && section.Equals("Combat", StringComparison.OrdinalIgnoreCase);
        var lists = items.Where(i => !i.IsFact).GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var list in lists)
        {
            if (combat && list.Key.Equals(Attacks, StringComparison.OrdinalIgnoreCase))
                continue;
            if (section is not null && list.Key.Equals(section, StringComparison.OrdinalIgnoreCase))
                lines.AddRange(list.Select(e => $"- {e.Text}{Mark(e, sources)}"));
            else
                lines.Add($"- **{list.First().Name}**: {string.Join(" ", list.Select(e => e.Text + Mark(e, sources)))}");
        }
        if (combat && lists.FirstOrDefault(l => l.Key.Equals(Attacks, StringComparison.OrdinalIgnoreCase)) is { } attacks)
        {
            lines.Add("");
            lines.Add("### " + Attacks);
            lines.Add("");
            lines.Add(AttackHeader);
            lines.Add("|---|---|---|---|");
            lines.AddRange(attacks.Select(a => Row(a.Text)));
        }
        return lines;
    }

    /// <summary>
    /// The ability table from the six score facts and their saves: Score, Modifier worked out from the score, and Save
    /// when any save fact is there. None when no score fact is there.
    /// </summary>
    /// <param name="items">The section's items.</param>
    static List<string> AbilityTable(List<Item> items)
    {
        string Cell(string key) => items.FirstOrDefault(i => i.IsFact && i.Name.Equals(key, StringComparison.OrdinalIgnoreCase))?.Text ?? "";
        var scores = CheckedKeys.Abilities.Select(Cell).ToList();
        if (scores.All(s => s.Length == 0))
            return [];
        var lines = new List<string>
        {
            "| | " + string.Join(" | ", CheckedKeys.Abilities) + " |",
            "|---|---|---|---|---|---|---|",
            "| Score | " + string.Join(" | ", scores) + " |",
            "| Modifier | " + string.Join(" | ", scores.Select(s => SheetNumbers.ParseSigned(s) is { } n ? CheckedKeys.Modifier(n) : "")) + " |",
        };
        var saves = CheckedKeys.Abilities.Select(a => Cell(a + CheckedKeys.SaveSuffix)).ToList();
        if (saves.Any(s => s.Length > 0))
            lines.Add("| Save | " + string.Join(" | ", saves) + " |");
        lines.Add("");
        return lines;
    }

    /// <summary>An attack entry as a table row: its cells as written between <c>|</c>, or the whole text in the first cell.</summary>
    /// <param name="text">The entry's text, such as <c>Bite | +3 | 1d6+1 piercing | reach 5 ft</c>.</param>
    static string Row(string text)
    {
        var cells = text.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();
        while (cells.Count < 4)
            cells.Add("");
        return "| " + string.Join(" | ", cells) + " |";
    }

    /// <summary>An item's source mark, <c> (s4.2)</c>, or nothing.</summary>
    /// <param name="item">The item.</param>
    /// <param name="sources">True to print sources.</param>
    static string Mark(Item item, bool sources) => sources && !string.IsNullOrWhiteSpace(item.Source) ? $" ({item.Source})" : "";
}

/// <summary>Which part of a record's print a reader gets (spec 036 FR-009).</summary>
internal enum PrintPart
{
    /// <summary>To the end of the play card: what the resume reads of a companion.</summary>
    Card,

    /// <summary>To the History heading: what the resume reads of any other sheet.</summary>
    Sheet,

    /// <summary>All of it.</summary>
    All,
}
