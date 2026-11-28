using System.Text;

namespace Dmc.Sheets;

/// <summary>
/// The printable page <c>dmc sheet</c> writes (spec 031 FR-005, FR-008): one self-contained HTML file, a sheet per
/// printed page, each sheet the same panels and boxes in the same order every time, filled only from the places
/// <see cref="SheetLayouts"/> names. Every text from a campaign file is escaped (<see cref="SheetMarkdown"/>). No
/// script, no outside file: it opens from disk on any device and prints from the browser.
/// </summary>
internal static class SheetHtml
{
    /// <summary>What an empty box prints: a box is never left out (FR-008).</summary>
    public const string None = "none";

    /// <summary>What a box prints for a companion whose play card holds that number instead of the field.</summary>
    public const string OnTheCard = "on the play card";

    /// <summary>What a Now box prints when the character's Party status block has no such line, or there is no block.</summary>
    public const string NotRecorded = "not recorded";

    /// <summary>The page.</summary>
    /// <param name="sheets">The characters, in the order they print.</param>
    /// <param name="asOf">The in-world date and time of the Scene in <c>game-state.md</c>, as written, or null when it has none.</param>
    /// <param name="exported">The real date of the export.</param>
    /// <param name="history">Whether <c>--history</c> was given: a PC's sheet then ends with its History panel.</param>
    public static string Page(IReadOnlyList<CharacterSheet> sheets, string? asOf, DateOnly exported, bool history)
    {
        string title = string.Join(", ", sheets.Select(s => s.Title)) + " · DMC sheet" + (sheets.Count == 1 ? "" : "s");
        var html = new StringBuilder();
        html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n")
            .Append("<title>").Append(Maps.Py.HtmlEscape(title)).Append("</title>\n<style>\n").Append(Style.Replace("\r\n", "\n")).Append("</style>\n</head>\n<body>\n");
        string asOfLine = "As of " + (asOf is { Length: > 0 } a ? SheetMarkdown.Inline(a) : NotRecorded) + " in the story · exported "
                          + exported.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        foreach (var sheet in sheets)
        {
            html.Append("<section class=\"sheet\">\n<header><h1>").Append(SheetMarkdown.Inline(sheet.Title)).Append("</h1>")
                .Append("<p class=\"kind\">").Append(sheet.Kind == SheetKind.Pc ? "Player character" : "Companion").Append("</p>")
                .Append("<p class=\"asof\">").Append(asOfLine).Append("</p></header>\n");
            foreach (var panel in SheetLayouts.Of(sheet.Kind!.Value))
            {
                if (panel.Boxes.Any(b => b.Source is HistorySource) && !(history && sheet.Kind == SheetKind.Pc))
                    continue; // History is printed only when asked for, and only for a PC (FR-014)
                html.Append("<div class=\"panel\"><h2>").Append(Maps.Py.HtmlEscape(panel.Title)).Append("</h2><div class=\"boxes\">");
                foreach (var box in panel.Boxes)
                {
                    html.Append("<div class=\"box").Append(box.Wide ? " wide" : "").Append("\"><h3>")
                        .Append(Maps.Py.HtmlEscape(box.Title)).Append("</h3><div class=\"v\">").Append(Content(sheet, box)).Append("</div></div>");
                }
                html.Append("</div></div>\n");
            }
            html.Append("</section>\n");
        }
        return html.Append("</body>\n</html>\n").ToString();
    }

    /// <summary>What one box holds, as HTML.</summary>
    /// <param name="sheet">The character.</param>
    /// <param name="box">The box.</param>
    static string Content(CharacterSheet sheet, Box box)
    {
        bool card = box.CardHeld && sheet.IsCompanion;
        switch (box.Source)
        {
            case FieldSource f:
                return sheet.Field(f.Section, f.Label) is { } field ? Field(field) : Plain(card ? OnTheCard : None);
            case FreeSource s:
                return SheetText.Find(sheet.Sections, s.Section) is { } section ? OrNone(SheetMarkdown.Blocks(section.AllLines)) : Plain(None);
            case AttackTableSource:
                return SheetCheck.AttackRows(sheet) is { } rows
                    ? rows.Count > 1 ? SheetMarkdown.Table(rows) : Plain(None)
                    : Plain(card ? OnTheCard : None);
            case AbilityTableSource:
                return Abilities(sheet);
            case SkillGridSource:
                return SkillGrid(sheet, card);
            case PartySource p:
                return sheet.Party?.FirstOrDefault(x => string.Equals(x.Label, p.Label, StringComparison.OrdinalIgnoreCase)) is { } pf
                    ? Field(pf) : Plain(NotRecorded);
            case HistorySource:
                var lines = (sheet.HistoryLines ?? []).ToList();
                if (sheet.HistoryFile is { } older)
                    lines = [.. older, "", .. lines];
                return OrNone(SheetMarkdown.Blocks(lines));
            case LimitsSource:
                return sheet.Limits.Count > 0
                    ? "<ul>" + string.Concat(sheet.Limits.Select(l => "<li>" + SheetMarkdown.Inline(l) + "</li>")) + "</ul>"
                    : Plain(None);
            default:
                return Plain(None);
        }
    }

    /// <summary>A field's value and its sub-items; <see cref="None"/> when both are empty.</summary>
    /// <param name="field">The field.</param>
    static string Field(SheetField field)
    {
        string value = field.Value.Length > 0 ? "<p>" + SheetMarkdown.Inline(field.Value) + "</p>" : "";
        string sub = SheetMarkdown.Blocks(field.SubLines.Select(l => l.Length > 0 && l[0] == '\t' ? l[1..] : l.Length > 1 && l.StartsWith("  ", StringComparison.Ordinal) ? l[2..] : l));
        return OrNone(value + sub);
    }

    /// <summary>The ability table as the sheet writes it: a column per ability, the rows Score, Modifier and Save.</summary>
    /// <param name="sheet">The character.</param>
    static string Abilities(CharacterSheet sheet)
    {
        var rows = SheetNumbers.AbilityRows(SheetCheck.AbilityCells(sheet));
        var table = new List<string[]>();
        table.Add(["", .. SheetLayouts.Abilities]);
        foreach (string row in new[] { "Score", "Modifier", "Save" })
        {
            string[] cells = rows.TryGetValue(row, out string[]? found) ? found
                : [.. SheetLayouts.Abilities.Select(_ => row == "Save" && sheet.IsCompanion ? OnTheCard : None)];
            table.Add([row, .. cells.Select(c => c.Length == 0 ? None : c)]);
        }
        return SheetMarkdown.Table(table);
    }

    /// <summary>
    /// The eighteen skills, each with its ability, the bonus as written, whether it is proficient (and expertise) and the
    /// note beside it. A skill the sheet does not write prints <see cref="None"/>; the check has refused such a sheet first.
    /// </summary>
    /// <param name="sheet">The character.</param>
    /// <param name="card">True for a companion whose play card may hold the skills instead.</param>
    static string SkillGrid(CharacterSheet sheet, bool card)
    {
        var skills = sheet.Field("Abilities", "Skills");
        var other = sheet.Field("Abilities", "Other skills");
        if (skills is null && other is null && card)
            return Plain(OnTheCard);
        var proficient = SheetNumbers.SkillList(skills?.Value ?? "");
        var rest = SheetNumbers.SkillList(other?.Value ?? "");
        var expertise = SheetNumbers.SkillNames(sheet.Field("Abilities", "Expertise")?.Value ?? "");
        var html = new StringBuilder("<table class=\"skills\"><thead><tr><th>Skill</th><th>Bonus</th><th>Proficient</th><th>Note</th></tr></thead><tbody>");
        foreach (var (skill, ability) in SheetLayouts.Skills)
        {
            var entry = proficient.FirstOrDefault(e => e.Skill == skill);
            bool prof = entry is not null;
            entry ??= rest.FirstOrDefault(e => e.Skill == skill);
            string mark = !prof ? "" : expertise.Contains(skill) ? "expertise" : "yes";
            html.Append("<tr><td>").Append(Maps.Py.HtmlEscape(skill)).Append(" <span class=\"ab\">(").Append(ability).Append(")</span></td><td>")
                .Append(entry is { Written.Length: > 0 } ? SheetMarkdown.Inline(entry.Written) : None).Append("</td><td>").Append(mark)
                .Append("</td><td>").Append(entry is { Note.Length: > 0 } ? SheetMarkdown.Inline(entry.Note) : "").Append("</td></tr>");
        }
        return html.Append("</tbody></table>").ToString();
    }

    /// <summary>Some HTML, or <see cref="None"/> when it is empty.</summary>
    /// <param name="html">The HTML.</param>
    static string OrNone(string html) => html.Length > 0 ? html : Plain(None);

    /// <summary>A word the page prints in a box of its own, set apart from the sheet's text.</summary>
    /// <param name="word">The word, such as <see cref="None"/>.</param>
    static string Plain(string word) => "<p class=\"empty\">" + word + "</p>";

    /// <summary>The page's styles: black on white for print, a grid of boxes that folds to one column on a phone, a sheet per printed page. A checkout with CRLF line endings makes the literal CRLF too; <see cref="Page"/> writes it with "\n".</summary>
    const string Style = """
  :root { --ink: #111; --muted: #555; --line: #888; --paper: #fff; }
  * { box-sizing: border-box; }
  html, body { margin: 0; background: var(--paper); color: var(--ink);
               font: 10.5pt/1.35 Georgia, "Times New Roman", serif; }
  .sheet { max-width: 200mm; margin: 0 auto; padding: 16px; }
  header { border-bottom: 2px solid var(--ink); margin-bottom: 8px; }
  h1 { margin: 0; font-size: 20pt; }
  .kind, .asof { margin: 2px 0; color: var(--muted); font-size: 9pt; }
  .panel { margin-top: 10px; }
  h2 { font-size: 11pt; margin: 0 0 4px; text-transform: uppercase; letter-spacing: 0.06em; }
  .boxes { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 6px; }
  .box { border: 1px solid var(--line); border-radius: 4px; padding: 4px 6px; min-width: 0;
         overflow-wrap: anywhere; break-inside: avoid; }
  .box.wide { grid-column: 1 / -1; }
  h3 { margin: 0 0 2px; font: 7.5pt/1.2 Arial, Helvetica, sans-serif; text-transform: uppercase;
       letter-spacing: 0.05em; color: var(--muted); }
  h4 { margin: 4px 0 2px; font-size: 10pt; }
  .v p { margin: 0 0 2px; }
  .v ul { margin: 0; padding-left: 16px; }
  .empty { color: var(--muted); font-style: italic; }
  table { width: 100%; border-collapse: collapse; font-size: 9.5pt; }
  th, td { border-bottom: 1px solid #ccc; padding: 1px 4px; text-align: left; vertical-align: top; }
  th { font: 7.5pt Arial, Helvetica, sans-serif; text-transform: uppercase; color: var(--muted); }
  .ab { color: var(--muted); font-size: 8pt; }
  @page { margin: 12mm; }
  @media print {
    .sheet { max-width: none; padding: 0; break-after: page; }
    .sheet:last-of-type { break-after: auto; }
  }

""";
}
