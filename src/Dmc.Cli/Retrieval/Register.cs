using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>One person of the names-and-roles register: an entry of canon's <c>## People</c> (spec 013 FR-018, research R6).</summary>
/// <param name="Key">The entry's first bold name, which names the person in the hook's header and in the log.</param>
/// <param name="Phrases">The names matched as written, whole and ignoring case: every bold span before the entry's first colon outside brackets, and the items of its <c>(also: …)</c> bracket.</param>
/// <param name="Words">The words of the bold names matched alone: 3 letters or more, starting with a capital, not a title or small word.</param>
/// <param name="Line">The entry's 1-based line in canon.</param>
internal sealed record Person(string Key, IReadOnlyList<string> Phrases, IReadOnlyList<string> Words, int Line);

/// <summary>
/// The names-and-roles register (spec 013 FR-018, research R6, data-model.md § Register): the people of canon's
/// <c>## People</c> section and the names a player may use for them. It lives inside canon, so it cannot drift from it;
/// the hook's reminder matches it against the player's message, and migration 006's <c>people-entries-named</c> check
/// holds its shape.
/// </summary>
internal static partial class Register
{
    /// <summary>The heading of canon's section the register lives in.</summary>
    public const string PeopleHeading = "People";

    /// <summary>A bold span, <c>**…**</c>; group 1 is its text.</summary>
    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex Bold();

    /// <summary>An entry's start: the first bold span, then an <c>(also: …)</c> bracket; group 1 is the bracket's items.</summary>
    [GeneratedRegex(@"^\s*-\s*\*\*.+?\*\*\s*\(also:\s*([^)]*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex Also();

    /// <summary>Words that never name a person alone: titles, kin words and small words (the read set's list, which a Present line also meets).</summary>
    static bool IsTitleOrSmall(string word) => ReadSet.IsCommonNameWord(word) || Retrieval.Words.IsCommon(word);

    /// <summary>The people of a canon, in file order: each list item under <c>## People</c> that starts with a bold name.</summary>
    /// <param name="canon">Every line of canon.</param>
    public static List<Person> Of(IReadOnlyList<Line> canon)
    {
        var people = new List<Person>();
        bool inside = false;
        for (int i = 0; i < canon.Count; i++)
        {
            string text = canon[i].Text;
            if (Measurement.IsSectionHeading(text))
            {
                inside = text[3..].Trim().Equals(PeopleHeading, StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (inside && Parse(text, i + 1) is { } person)
                people.Add(person);
        }
        return people;
    }

    /// <summary>
    /// Whether a line under <c>## People</c> is in the register's shape: a list item whose text starts with a bold name.
    /// A line that is not a list item (blank, a comment, prose) is not an entry and passes.
    /// </summary>
    /// <param name="text">The line.</param>
    public static bool IsNamedOrNotAnEntry(string text)
    {
        string trimmed = text.TrimStart();
        if (!trimmed.StartsWith("- ", StringComparison.Ordinal) && !trimmed.StartsWith("* ", StringComparison.Ordinal))
            return true;
        return trimmed[2..].TrimStart().StartsWith("**", StringComparison.Ordinal);
    }

    /// <summary>One entry's person, or null for a line that is not a list item starting with a bold name.</summary>
    /// <param name="text">The line.</param>
    /// <param name="line">Its 1-based line number.</param>
    internal static Person? Parse(string text, int line)
    {
        string trimmed = text.TrimStart();
        if (!(trimmed.StartsWith("- **", StringComparison.Ordinal) || trimmed.StartsWith("* **", StringComparison.Ordinal)))
            return null;
        string lead = Lead(trimmed);
        var bold = Bold().Matches(lead).Select(m => Clean(m.Groups[1].Value)).Where(b => b.Length > 0).ToList();
        if (bold.Count == 0)
            return null;
        var phrases = new List<string>(bold);
        if (Also().Match(trimmed) is { Success: true } also)
            phrases.AddRange(also.Groups[1].Value.Split([',', ';']).Select(Clean).Where(a => a.Length > 0));
        var words = bold.SelectMany(b => b.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Select(Clean)
            .Select(w => w.EndsWith("'s", StringComparison.OrdinalIgnoreCase) ? w[..^2] : w)
            .Where(w => w.Length >= 3 && char.IsUpper(w[0]) && !IsTitleOrSmall(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new Person(bold[0], phrases.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), words, line);
    }

    /// <summary>The part of an entry before its first colon outside brackets and bold (the whole entry when it has none).</summary>
    /// <param name="text">The entry, trimmed at the start.</param>
    static string Lead(string text)
    {
        int depth = 0;
        bool bold = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                bold = !bold;
                i++;
            }
            else if (c is '(' or '[')
                depth++;
            else if (c is ')' or ']' && depth > 0)
                depth--;
            else if (c == ':' && depth == 0 && !bold)
                return text[..i];
        }
        return text;
    }

    /// <summary>A name with its surrounding punctuation, quotes and emphasis removed.</summary>
    /// <param name="name">The name as written.</param>
    static string Clean(string name) => name.Replace("**", "").Trim().Trim(',', '.', ';', ':', '(', ')', '"', '\'', '*', '_', '“', '”', '‘', '’').Trim();

    /// <summary>Whether a text holds a name whole (not inside a longer word), ignoring case.</summary>
    /// <param name="text">The text, such as the player's message.</param>
    /// <param name="name">A phrase or word of the register.</param>
    static bool Holds(string text, string name) =>
        name.Length > 0 && Regex.IsMatch(text, $@"(?<!\w){Regex.Escape(name)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// The people a text names, in canon's order. A person is named by one of their phrases (a bold name or an
    /// <c>also:</c> item), or by a word of a bold name that no earlier entry has claimed: People lists a person before the
    /// facts about them (<c>**Kole's rule**</c>), so a lone "Kole" names the first entry it fits, not every one.
    /// </summary>
    /// <param name="text">The text, such as the player's message.</param>
    /// <param name="people">The register, in canon's order.</param>
    public static List<Person> Named(string text, IReadOnlyList<Person> people)
    {
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var named = new List<Person>();
        foreach (var person in people)
        {
            bool byPhrase = person.Phrases.Any(p => Holds(text, p));
            bool byWord = person.Words.Any(w => !claimed.Contains(w) && Holds(text, w));
            if (byPhrase || byWord)
            {
                named.Add(person);
                claimed.UnionWith(person.Words.Where(w => Holds(text, w)));
            }
        }
        return named;
    }

    /// <summary>The words a person's canon lines are ranked by: the words of their phrases, less titles and small words ("Captain" would rank every captain).</summary>
    /// <param name="person">The person.</param>
    public static string QueryText(Person person) =>
        string.Join(' ', person.Phrases.SelectMany(p => p.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Select(Clean).Where(w => w.Length > 0 && !IsTitleOrSmall(w)));
}
