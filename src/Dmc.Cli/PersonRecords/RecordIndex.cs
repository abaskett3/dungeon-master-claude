using System.Text.RegularExpressions;
using Dmc.Campaign;

namespace Dmc.PersonRecords;

/// <summary>
/// The index of every row (spec 032 FR-004, research R4), built on each run, never stored: the rows of the active
/// campaign and of each campaign it continues that has <c>records/</c>. It has no list of title or kin words: a word is
/// ambiguous only when the rows say so. Views by word, by speaker, and the persons a text names.
/// </summary>
internal sealed class RecordIndex
{
    /// <summary>One row with the person it names and the campaign whose records hold it.</summary>
    /// <param name="Person">The person the row names.</param>
    /// <param name="Row">The row.</param>
    /// <param name="Campaign">The campaign whose <c>records/</c> holds it.</param>
    internal sealed record Entry(string Person, Row Row, string Campaign);

    /// <summary>A word found in a text, with every row of it.</summary>
    /// <param name="Word">The word as the rows spell it.</param>
    /// <param name="At">Where the text first holds it.</param>
    /// <param name="Rows">Every row with this word.</param>
    internal sealed record Found(string Word, int At, IReadOnlyList<Entry> Rows);

    /// <summary>The rows by word, lower case.</summary>
    readonly Dictionary<string, List<Entry>> _byWord = new(StringComparer.Ordinal);

    /// <summary>The records, the active campaign's first.</summary>
    public IReadOnlyList<PersonRecord> Records { get; }

    /// <summary>Every row of every record, in record and row order.</summary>
    public IReadOnlyList<Entry> Rows { get; }

    /// <summary>An index of some records.</summary>
    /// <param name="records">The records, the active campaign's first.</param>
    public RecordIndex(IReadOnlyList<PersonRecord> records)
    {
        Records = records;
        Rows = records.SelectMany(r => r.Rows.Where(row => !string.IsNullOrWhiteSpace(row.Word)).Select(row => new Entry(r.Person, row, r.Campaign))).ToList();
        // A relationship's word ("mother") is what the person is to the speaker, not a word anyone calls them (FR-002),
        // so only usages and names are words a text can hold.
        foreach (var entry in Rows.Where(e => e.Row.Kind != RowKind.Relationship))
        {
            string key = entry.Row.Word!.Trim().ToLowerInvariant();
            if (!_byWord.TryGetValue(key, out var list))
                _byWord[key] = list = [];
            list.Add(entry);
        }
    }

    /// <summary>The index of a campaign in a DMC folder: its records and those of each campaign it continues; empty when it has no <c>records/</c>.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    public static RecordIndex Build(string dmcFolder, string campaign)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        if (!RecordFiles.Has(root))
            return new RecordIndex([]);
        var records = new List<PersonRecord>(RecordFiles.Read(root).Records);
        foreach (string folder in Earlier(dmcFolder, campaign).Values)
            records.AddRange(RecordFiles.Read(folder).Records);
        return new RecordIndex(records);
    }

    /// <summary>
    /// The campaigns a campaign continues that are in <c>adventures/</c>, by folder name: the members of its linked set
    /// (<see cref="CampaignLinks.Build"/>) whose story it comes after. A side or world campaign is not one.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    public static Dictionary<string, string> Earlier(string dmcFolder, string campaign)
    {
        var earlier = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in CampaignLinks.Build(dmcFolder, campaign).Members)
            if (member.Present && member.Before && member.Label == "continues" && !member.Name.Equals(campaign, StringComparison.OrdinalIgnoreCase))
                earlier[member.Name] = member.Folder;
        return earlier;
    }

    /// <summary>Every usage and name with a word, ignoring case; empty for a word none holds (a relationship's word is not one).</summary>
    /// <param name="word">The word.</param>
    public IReadOnlyList<Entry> ByWord(string word) => _byWord.TryGetValue(word.Trim().ToLowerInvariant(), out var list) ? list : [];

    /// <summary>The rows a person says: every row whose speaker is them.</summary>
    /// <param name="speaker">The speaker's full name.</param>
    public IEnumerable<Entry> BySpeaker(string speaker) => Rows.Where(e => string.Equals(e.Row.From, speaker, StringComparison.Ordinal));

    /// <summary>
    /// The row words a text holds, each whole and ignoring case, longest first so a longer word wins over one it holds
    /// ("Auntie Em" over "Auntie"), a part of the text matched once; in the order the text holds them.
    /// </summary>
    /// <param name="text">The text, such as the player's message.</param>
    public List<Found> WordsIn(string text)
    {
        var taken = new bool[text.Length];
        var found = new List<Found>();
        foreach (string key in _byWord.Keys.OrderByDescending(k => k.Length).ThenBy(k => k, StringComparer.Ordinal))
        {
            foreach (Match m in Regex.Matches(text, $@"(?<!\w){Regex.Escape(key)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                if (Enumerable.Range(m.Index, m.Length).Any(i => taken[i]))
                    continue;
                for (int i = m.Index; i < m.Index + m.Length; i++)
                    taken[i] = true;
                var rows = _byWord[key];
                if (!found.Any(f => f.Rows == rows))
                    found.Add(new Found(rows[0].Row.Word!.Trim(), m.Index, rows));
            }
        }
        return found.OrderBy(f => f.At).ToList();
    }

    /// <summary>
    /// The persons a text names, in the order it names them: by their full name, or by a row word whose rows all mean
    /// that one person. A word that means several persons names none of them (the hook prints every candidate instead).
    /// </summary>
    /// <param name="text">The text.</param>
    public List<string> Named(string text)
    {
        var named = new List<(string Person, int At)>();
        foreach (var record in Records)
        {
            int at = Size.ReadSet.WholeWordAt(text, record.Person);
            if (at >= 0)
                named.Add((record.Person, at));
        }
        foreach (var found in WordsIn(text))
        {
            var persons = found.Rows.Select(r => r.Person).Distinct(StringComparer.Ordinal).ToList();
            if (persons.Count == 1)
                named.Add((persons[0], found.At));
        }
        return named.OrderBy(n => n.At).Select(n => n.Person).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The record of a person, or null.</summary>
    /// <param name="person">The full name.</param>
    public PersonRecord? Record(string person) => Records.FirstOrDefault(r => r.Person.Equals(person, StringComparison.Ordinal));
}
