using System.Text;

namespace Dmc.Retrieval;

/// <summary>
/// The words of a text as <c>dmc recall</c> and <c>dmc lookup</c> compare them: lower-cased, common words dropped, each
/// reduced to a stem by a small suffix stripper. These are the settings of the issue-179 recall prototype that the
/// spec's figures came from (research R2), kept so the figures can be reproduced before anything is tuned.
/// </summary>
/// <remarks>
/// Known gaps, kept with the prototype: a word and its plural can stem apart ("horse" stays, "horses" becomes "hors"),
/// and a stem under four letters is left whole ("ruled"). The one deliberate change is that a letter of any script
/// counts as a letter, where the prototype took ASCII only, so an accented name stays one word.
/// </remarks>
internal static class Words
{
    /// <summary>The prototype's common-word list (<c>recall.py</c>'s <c>STOP</c>), copied verbatim.</summary>
    static readonly HashSet<string> Common = new(
        ("a an the and or of to in on at for with by from is are was were be been do does did what who whom whose when " +
         "where why how that this it its his her he she i me my we us our you your they them their as not no if so " +
         "than then there about into out up down ever still same after before all any which have has had say said " +
         "tell told get got one thing things").Split(' '),
        StringComparer.Ordinal);

    /// <summary>The suffixes stripped, longest first, as in the prototype's "better stemming" variant (<c>recall_alias.py</c>).</summary>
    static readonly string[] Suffixes = ["ation", "ion", "ing", "ed", "es", "s"];

    /// <summary>The fewest letters a stem may keep; a suffix that would leave fewer is not stripped.</summary>
    const int MinimumStem = 4;

    /// <summary>The stems of <paramref name="text"/>, in order, repeats kept.</summary>
    /// <param name="text">Any text: a line, a heading or a query.</param>
    /// <returns>One stem per word that is longer than one character and not a common word.</returns>
    public static List<string> Stems(string text)
    {
        var stems = new List<string>();
        foreach (string word in Tokens(text))
        {
            if (word.Length > 1 && !Common.Contains(word))
                stems.Add(Stem(word));
        }
        return stems;
    }

    /// <summary>True when <paramref name="word"/>, lower-cased, is on the common-word list.</summary>
    /// <param name="word">One word.</param>
    public static bool IsCommon(string word) => Common.Contains(word.ToLowerInvariant());

    /// <summary>
    /// The words of a text: runs of letters, digits and apostrophes in the lower-cased text, with the typographic
    /// apostrophe folded to <c>'</c>. Every other character separates words.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The words, in order.</returns>
    public static IEnumerable<string> Tokens(string text)
    {
        var word = new StringBuilder();
        foreach (char raw in text)
        {
            char c = raw == '’' ? '\'' : char.ToLowerInvariant(raw);
            if (char.IsLetterOrDigit(c) || c == '\'')
            {
                word.Append(c);
            }
            else if (word.Length > 0)
            {
                yield return word.ToString();
                word.Clear();
            }
        }
        if (word.Length > 0)
            yield return word.ToString();
    }

    /// <summary>
    /// The stem of one lower-case word: a trailing <c>'s</c> dropped, then the first suffix of <see cref="Suffixes"/>
    /// that leaves at least <see cref="MinimumStem"/> letters.
    /// </summary>
    /// <param name="word">A lower-case word.</param>
    /// <returns>Its stem.</returns>
    public static string Stem(string word)
    {
        if (word.EndsWith("'s", StringComparison.Ordinal))
            word = word[..^2];
        foreach (string suffix in Suffixes)
        {
            if (word.EndsWith(suffix, StringComparison.Ordinal) && word.Length - suffix.Length >= MinimumStem)
                return word[..^suffix.Length];
        }
        return word;
    }
}
