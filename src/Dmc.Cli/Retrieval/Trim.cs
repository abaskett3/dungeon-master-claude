using System.Text;

namespace Dmc.Retrieval;

/// <summary>
/// Cuts a long line down to the part around the words a request matched, so a 3,000-character line costs no more than
/// a short one (research R4). Used by recall for its hits and by lookup for a rules index line.
/// </summary>
internal static class Trim
{
    /// <summary>
    /// The most characters a shown line keeps: the recall prototype's 700, which the spec's recall figures rest on.
    /// </summary>
    public const int Width = 700;

    /// <summary>The mark put where text was cut.</summary>
    public const string Cut = "…";

    /// <summary>
    /// <paramref name="text"/> whole when it is <see cref="Width"/> characters or fewer; otherwise the window of that
    /// width that holds the most distinct matched stems, starting at a matched word (the first such window on a tie),
    /// ending at a word boundary, with <see cref="Cut"/> on each side where text was left out. With no matched word in
    /// the line, the window starts at the beginning.
    /// </summary>
    /// <param name="text">The line.</param>
    /// <param name="matched">The request's stems.</param>
    /// <returns>The text to show.</returns>
    public static string Around(string text, IReadOnlySet<string> matched)
    {
        if (text.Length <= Width)
            return text;

        var words = Positions(text);
        int bestStart = 0, bestCount = -1;
        foreach (var (start, stem) in words)
        {
            if (!matched.Contains(stem))
                continue;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (other, otherStem) in words)
            {
                if (other >= start && other < start + Width && matched.Contains(otherStem))
                    seen.Add(otherStem);
            }
            if (seen.Count > bestCount)
            {
                bestCount = seen.Count;
                bestStart = start;
            }
        }

        // A window that would run past the end slides back so it is full, starting at a word.
        if (bestStart + Width > text.Length)
        {
            int back = text.Length - Width;
            bestStart = words.Select(w => w.Start).Where(s => s >= back).DefaultIfEmpty(back).First();
        }

        int end = Math.Min(text.Length, bestStart + Width);
        if (end < text.Length)
        {
            int space = text.LastIndexOf(' ', end - 1, end - bestStart);
            if (space > bestStart)
                end = space;
        }

        var shown = new StringBuilder();
        if (bestStart > 0)
            shown.Append(Cut);
        shown.Append(text.AsSpan(bestStart, end - bestStart).TrimEnd());
        if (end < text.Length)
            shown.Append(Cut);
        return shown.ToString();
    }

    /// <summary>Each word of a text with the index it starts at and its stem; common and one-character words are left out.</summary>
    /// <param name="text">The text.</param>
    static List<(int Start, string Stem)> Positions(string text)
    {
        var found = new List<(int, string)>();
        int i = 0;
        while (i < text.Length)
        {
            if (!IsWordChar(text[i]))
            {
                i++;
                continue;
            }
            int start = i;
            while (i < text.Length && IsWordChar(text[i]))
                i++;
            string? word = Words.Clean(text[start..i].Replace('’', '\'').ToLowerInvariant());
            if (word is { Length: > 1 } && !Words.IsCommon(word))
                found.Add((start, Words.Stem(word)));
        }
        return found;
    }

    /// <summary>A character that is part of a word, as <see cref="Words.Tokens"/> counts it.</summary>
    /// <param name="c">The character.</param>
    static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '\'' or '’';
}
