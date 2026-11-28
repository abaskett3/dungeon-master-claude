using System.Text.RegularExpressions;

namespace Dmc.PersonRecords;

/// <summary>
/// The full-date rule (spec 036 FR-005, data-model.md § Full dates): a date in a record's text holds a full in-world
/// date, never a day alone. An ordinal day (<c>14th</c>) must have a month after it (<c>the 14th of Eleasis</c>), or end
/// a range that does (<c>the 8th to the 10th of Flamerule</c>); when the campaign's calendar has a year, the date needs
/// one after its month too. <c>dmc</c> has no list of month names, so a month is any capitalised word (research R4).
/// </summary>
internal static partial class RecordDates
{
    /// <summary>An ordinal day: one or two digits and <c>st</c>, <c>nd</c>, <c>rd</c> or <c>th</c>, as a whole word.</summary>
    [GeneratedRegex(@"(?<![\w.])(?<day>\d{1,2}(?:st|nd|rd|th))\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ordinal();

    /// <summary>
    /// A month right after a day: an optional <c>of</c>, then a capitalised word; or a month counted in words, <c>of the
    /// second month</c> (T034).
    /// </summary>
    [GeneratedRegex(@"^,?\s+(?:of\s+the\s+[\p{L}-]+\s+month\b|(?:of\s+)?\p{Lu}[\p{L}'’-]*)", RegexOptions.CultureInvariant)]
    private static partial Regex MonthAfter();

    /// <summary>
    /// The joint of a range of days, after the first: up to two lower-case words (<c>the 14th night to the 18th</c>, T034),
    /// then <c>to</c>, <c>and</c>, <c>or</c>, <c>through</c> or a dash, and an optional <c>the</c>.
    /// </summary>
    [GeneratedRegex(@"^(?:\s+\p{Ll}+){0,2}?\s*(?:to|and|or|through|-|–|—)\s*(?:the\s+)?(?=\d{1,2}(?:st|nd|rd|th)\b)", RegexOptions.CultureInvariant)]
    private static partial Regex RangeJoint();

    /// <summary>A year within three words after a month: a number of three or four digits.</summary>
    [GeneratedRegex(@"^(?:[,\s]+[\p{L}'’]+){0,3}?[,\s]+\d{3,4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex YearAfter();

    /// <summary>A year in a game state's date line: a number of three or four digits.</summary>
    [GeneratedRegex(@"(?<!\d)\d{3,4}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex Year();

    /// <summary>The "In-world date and time" line of <c>game-state.md</c>.</summary>
    [GeneratedRegex(@"^\s*-\s*\*\*In-world date(?: and time)?\*\*\s*:(?<date>.*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex DateLine();

    /// <summary>
    /// What is wrong with the dates in a text, or null when every date is full: the first ordinal day with no month after
    /// it, or, when <paramref name="needsYear"/>, with no year after its month.
    /// </summary>
    /// <param name="text">The text of a value, entry, limit or <c>when</c> note.</param>
    /// <param name="needsYear">True when the campaign's calendar has a year.</param>
    public static string? Error(string? text, bool needsYear)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        // A range shares its month: walk each run of joined days to its last, and check the month after that one.
        var days = Ordinal().Matches(text);
        for (int i = 0; i < days.Count; i++)
        {
            var first = days[i];
            var last = first;
            while (i + 1 < days.Count && RangeJoint().Match(text[(last.Index + last.Length)..]) is { Success: true } joint
                   && last.Index + last.Length + joint.Length == days[i + 1].Index)
                last = days[++i];
            string after = text[(last.Index + last.Length)..];
            var month = MonthAfter().Match(after);
            if (!month.Success)
                return $"\"{Quote(first, last, text)}\" has no month: write the full in-world date (the {last.Groups["day"].Value} of <month>{(needsYear ? ", <year>" : "")})";
            if (needsYear && !YearAfter().IsMatch(after[month.Length..]))
                return $"\"{Quote(first, last, text)}{month.Value}\" has no year: write the full in-world date with its year";
        }
        return null;
    }

    /// <summary>The words of a date as the text has them, from its first day to its last, with <c>the</c> before it when there is one.</summary>
    /// <param name="first">The first day.</param>
    /// <param name="last">The last day.</param>
    /// <param name="text">The text.</param>
    static string Quote(Match first, Match last, string text)
    {
        int start = first.Index >= 4 && text.AsSpan(first.Index - 4, 4).Equals("the ", StringComparison.OrdinalIgnoreCase) ? first.Index - 4 : first.Index;
        return text[start..(last.Index + last.Length)];
    }

    /// <summary>
    /// Whether a campaign's calendar has a year: its <c>game-state.md</c>'s "In-world date and time" line holds a number of
    /// three or four digits (research R4). False when the file or the line is missing.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static bool CalendarHasYear(string campaignFolder)
    {
        string path = Path.Combine(campaignFolder, "game-state.md");
        if (!File.Exists(path))
            return false;
        foreach (string line in File.ReadLines(path))
            if (DateLine().Match(line) is { Success: true } m)
                return Year().IsMatch(Comment().Replace(m.Groups["date"].Value, ""));
        return false;
    }

    /// <summary>An HTML comment, left out of the date line.</summary>
    [GeneratedRegex("<!--.*?-->", RegexOptions.CultureInvariant)]
    private static partial Regex Comment();
}
