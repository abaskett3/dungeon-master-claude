using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Sheets;

/// <summary>One skill as a sheet writes it: <c>Stealth +4 (disadvantage in ring mail)</c>.</summary>
/// <param name="Skill">The skill's name as <see cref="SheetLayouts.Skills"/> spells it.</param>
/// <param name="Written">The bonus as written (<c>+4</c>), or empty when the entry gives none.</param>
/// <param name="Note">Anything written beside it (a bracket, or words after the number), or empty.</param>
internal sealed record SkillEntry(string Skill, string Written, string Note);

/// <summary>
/// Reading the numbers a sheet writes: the ability table, signed bonuses and skill lists. <c>dmc sheet</c> prints the
/// written values; these readers serve only the arithmetic check (spec 031 FR-012), which compares, warns and never
/// prints a number of its own.
/// </summary>
internal static partial class SheetNumbers
{
    /// <summary>A signed whole number, with a plain or typographic minus: <c>+3</c>, <c>-1</c>, <c>−1</c>, <c>3</c>.</summary>
    [GeneratedRegex(@"^\s*(?<sign>[+\-−–])?\s*(?<n>\d{1,3})\b")]
    private static partial Regex Signed();

    /// <summary>What follows a skill's name in a skill list: an optional colon, an optional bonus (bare or in brackets: <c>+5</c>, <c>(+5)</c>), then anything else (a note).</summary>
    [GeneratedRegex(@"^\s*(?::\s*)?(?:\((?<bonus>[+\-−–]\s*\d{1,3})\)|(?<bonus>[+\-−–]\s*\d{1,3}))?(?<rest>.*)$")]
    private static partial Regex Bonus();

    /// <summary>A signed number at the start of a text, or null when it does not start with one.</summary>
    /// <param name="text">A written value such as <c>+5</c> or <c>+2 (from level 1)</c>.</param>
    public static int? ParseSigned(string text)
    {
        var match = Signed().Match(text.Replace("**", ""));
        if (!match.Success)
            return null;
        int n = int.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture);
        return match.Groups["sign"].Value is "-" or "−" or "–" ? -n : n;
    }

    /// <summary>The modifier a score gives: (score − 10) ÷ 2, rounded down.</summary>
    /// <param name="score">An ability score.</param>
    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    /// <summary>A number written with its sign, the way a sheet writes a bonus: <c>+3</c>, <c>-1</c>, <c>+0</c>.</summary>
    /// <param name="n">The number.</param>
    public static string WithSign(int n) => n >= 0 ? "+" + n.ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The entries of a skill list, split at commas and semicolons outside brackets. An entry that names no skill is
    /// returned in <paramref name="unknown"/>; the word <c>none</c> is neither.
    /// </summary>
    /// <param name="value">The field's value, such as <c>Athletics +5, Stealth +4 (disadvantage in ring mail)</c>.</param>
    /// <param name="unknown">Receives the entries that name no skill.</param>
    public static List<SkillEntry> SkillList(string value, List<string>? unknown = null)
    {
        var entries = new List<SkillEntry>();
        foreach (string raw in SplitOutsideBrackets(value.Replace("**", "")))
        {
            string piece = raw.Trim().TrimEnd('.');
            if (piece.Length == 0 || piece.Equals("none", StringComparison.OrdinalIgnoreCase))
                continue;
            var skill = SheetLayouts.Skills.Select(k => k.Skill)
                .Where(k => piece.StartsWith(k, StringComparison.OrdinalIgnoreCase)
                            && (piece.Length == k.Length || !char.IsLetter(piece[k.Length])))
                .OrderByDescending(k => k.Length).FirstOrDefault();
            if (skill is null)
            {
                unknown?.Add(piece);
                continue;
            }
            var match = Bonus().Match(piece[skill.Length..]);
            string bonus = match.Success ? Regex.Replace(match.Groups["bonus"].Value, @"\s", "") : "";
            string note = (match.Success ? match.Groups["rest"].Value : piece[skill.Length..]).Trim().TrimStart(',', ';', ':').Trim();
            if (note.StartsWith('(') && note.EndsWith(')') && note.IndexOf(')') == note.Length - 1)
                note = note[1..^1].Trim();
            entries.Add(new SkillEntry(skill, bonus, note));
        }
        return entries;
    }

    /// <summary>The names of skills a field lists (Expertise: <c>Stealth, Thieves' tools</c>), ignoring bonuses and anything that is not a skill.</summary>
    /// <param name="value">The field's value.</param>
    public static HashSet<string> SkillNames(string value) =>
        new(SkillList(value).Select(e => e.Skill), StringComparer.OrdinalIgnoreCase);

    /// <summary>The abilities a saving-throw field names, by their three letters or full names (<c>STR, Constitution</c>).</summary>
    /// <param name="value">The field's value.</param>
    public static HashSet<string> SaveAbilities(string value)
    {
        string[] full = ["strength", "dexterity", "constitution", "intelligence", "wisdom", "charisma"];
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match word in Regex.Matches(value, @"[A-Za-z]+"))
        {
            string w = word.Value;
            int index = SheetLayouts.Abilities.ToList().FindIndex(a => a.Equals(w, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                index = Array.FindIndex(full, f => f.Equals(w, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                found.Add(SheetLayouts.Abilities[index]);
        }
        return found;
    }

    /// <summary>The ability table of <c>## Abilities</c>: for each of the six abilities its score, modifier and save cells as written.</summary>
    /// <param name="rows">The table's cells, header first (<see cref="SheetText.TableCells"/>).</param>
    /// <returns>Row name (<c>Score</c>, <c>Modifier</c>, <c>Save</c>, as written in the first column, lower case) to the six cells in STR to CHA order; a cell the row lacks is empty.</returns>
    public static Dictionary<string, string[]> AbilityRows(IReadOnlyList<string[]> rows)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (rows.Count == 0)
            return result;
        string[] header = rows[0];
        int[] columns = [.. SheetLayouts.Abilities.Select(a => Array.FindIndex(header, h => h.Equals(a, StringComparison.OrdinalIgnoreCase)))];
        foreach (string[] row in rows.Skip(1))
        {
            if (row.Length == 0 || row[0].Length == 0)
                continue;
            result[row[0].Replace("**", "").Trim()] = [.. columns.Select(c => c >= 0 && c < row.Length ? row[c] : "")];
        }
        return result;
    }

    /// <summary>The pieces of a text between commas and semicolons that are not inside brackets.</summary>
    /// <param name="text">The text.</param>
    static IEnumerable<string> SplitOutsideBrackets(string text)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is '(' or '[')
                depth++;
            else if (c is ')' or ']')
                depth = Math.Max(0, depth - 1);
            else if (depth == 0 && c is ',' or ';')
            {
                yield return text[start..i];
                start = i + 1;
            }
        }
        yield return text[start..];
    }
}
