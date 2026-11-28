using System.Text.RegularExpressions;

namespace Dmc.PersonRecords;

/// <summary>
/// The full-date rule (spec 036 FR-005, data-model.md § Full dates): a date in a record's text holds a full in-world
/// date, never a day alone and never without its year. An ordinal day (<c>14th</c>) must have a month of the Harptos
/// calendar after it (<c>the 14th of Eleasis</c>), or end a range that does (<c>the 8th to the 10th of Flamerule</c>),
/// and a year after its month (<c>the 14th of Eleasis, 1493 DR</c>). An ordinal followed straight by another capitalised
/// word is a name (<c>the 58th Dwarven Company</c>), not a date.
/// </summary>
internal static partial class RecordDates
{
    /// <summary>The twelve months of the Harptos calendar, the calendar of every campaign (the player's ruling, 2026-11-28).</summary>
    public static readonly IReadOnlyList<string> Months =
        ["Hammer", "Alturiak", "Ches", "Tarsakh", "Mirtul", "Kythorn", "Flamerule", "Eleasis", "Eleint", "Marpenoth", "Uktar", "Nightal"];

    /// <summary>An ordinal day: one or two digits and <c>st</c>, <c>nd</c>, <c>rd</c> or <c>th</c>, as a whole word.</summary>
    [GeneratedRegex(@"(?<![\w.])(?<day>\d{1,2}(?:st|nd|rd|th))\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ordinal();

    /// <summary>
    /// A month right after a day: an optional <c>of</c>, then a Harptos month; or a month counted in words, <c>of the
    /// second month</c> (T034).
    /// </summary>
    [GeneratedRegex(@"^,?\s+(?:of\s+the\s+[\p{L}-]+\s+month\b|(?:of\s+)?(?:Hammer|Alturiak|Ches|Tarsakh|Mirtul|Kythorn|Flamerule|Eleasis|Eleint|Marpenoth|Uktar|Nightal)\b)", RegexOptions.CultureInvariant)]
    private static partial Regex MonthAfter();

    /// <summary><c>of</c> and a capitalised word that is not a Harptos month (<c>the 3rd of Harvestmoon</c>): a month the calendar does not have.</summary>
    [GeneratedRegex(@"^,?\s+of\s+(?<word>\p{Lu}[\p{L}'’-]*)", RegexOptions.CultureInvariant)]
    private static partial Regex OtherMonth();

    /// <summary>A capitalised word straight after a day, with no comma or <c>of</c> (<c>the 58th Dwarven</c>): a name, not a date.</summary>
    [GeneratedRegex(@"^\s+\p{Lu}", RegexOptions.CultureInvariant)]
    private static partial Regex NameAfter();

    /// <summary>
    /// The joint of a range of days, after the first: up to two lower-case words (<c>the 14th night to the 18th</c>, T034),
    /// then <c>to</c>, <c>and</c>, <c>or</c>, <c>through</c> or a dash, and an optional <c>the</c>.
    /// </summary>
    [GeneratedRegex(@"^(?:\s+\p{Ll}+){0,2}?\s*(?:to|and|or|through|-|–|—)\s*(?:the\s+)?(?=\d{1,2}(?:st|nd|rd|th)\b)", RegexOptions.CultureInvariant)]
    private static partial Regex RangeJoint();

    /// <summary>A year within three words after a month: a number of three or four digits.</summary>
    [GeneratedRegex(@"^(?:[,\s]+[\p{L}'’]+){0,3}?[,\s]+\d{3,4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex YearAfter();

    /// <summary>
    /// What is wrong with the dates in a text, or null when every date is full: the first ordinal day with no month after
    /// it, with a month the Harptos calendar does not have, or with no year after its month.
    /// </summary>
    /// <param name="text">The text of a value, entry, limit or <c>when</c> note.</param>
    public static string? Error(string? text)
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
            {
                if (OtherMonth().Match(after) is { Success: true } other)
                    return $"\"{Quote(first, last, text)} of {other.Groups["word"].Value}\": {other.Groups["word"].Value} is not a month of the Harptos calendar ({string.Join(", ", Months)})";
                if (first == last && NameAfter().IsMatch(after))
                    continue;
                return $"\"{Quote(first, last, text)}\" has no month: write the full in-world date (the {last.Groups["day"].Value} of <month>, <year> DR)";
            }
            if (!YearAfter().IsMatch(after[month.Length..]))
                return $"\"{Quote(first, last, text)}{month.Value}\" has no year: write the full in-world date with its year (<year> DR)";
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
}
