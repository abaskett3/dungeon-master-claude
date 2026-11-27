using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.PersonRecords;

/// <summary>
/// The check at the full save and the migration's (spec 032 FR-007, research R7, data-model.md § Finding): one line per
/// finding, <c>&lt;class&gt;: &lt;where&gt;: &lt;what&gt;</c>. On the records: every rule broken (<c>invalid</c>), and
/// the sheets' sample lines against the rows and limits. On the migration's draft: also a row with no speaker, a word for
/// two persons where a row lacks its speaker, two Names lines that give one speaker different words for one person, and
/// a row with no source. A word that means different persons to different speakers, all known, is never a finding.
/// </summary>
internal static partial class RecordCheck
{
    /// <summary>A quoted sample line, straight or curly quotes, with an optional <c>(to X)</c> after it; group 1 the words, group 2 the addressee.</summary>
    [GeneratedRegex(@"[""“]([^""“”]{2,}?)[""”]\**_*\s*(?:\(to ([^)]+)\))?", RegexOptions.CultureInvariant)]
    private static partial Regex Sample();

    /// <summary>The words a limit is about: what follows "cannot", "can't", "never" or "unable to", up to the first stop.</summary>
    [GeneratedRegex(@"\b(?:cannot|can't|can not|never|unable to|not able to)\s+([^.;,:]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LimitWords();

    /// <summary>A word of at least three letters.</summary>
    [GeneratedRegex(@"[A-Za-z']{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex Word();

    /// <summary>The findings for a campaign's records, and its sheets' sample lines.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    public static List<string> Records(string dmcFolder, string campaign)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        var loaded = RecordFiles.Read(root);
        var findings = loaded.Errors.Select(e => $"invalid: {e}").ToList();
        var outcome = RecordRules.Check(SceneBook.For(dmcFolder, campaign), loaded.Records);
        findings.AddRange(outcome.Errors.Select(e => (e.Contains(" never says ", StringComparison.Ordinal) ? "rows-disagree: " : "invalid: ") + e));
        findings.AddRange(Samples(root, RecordIndex.Build(dmcFolder, campaign)));
        return findings;
    }

    /// <summary>The findings for the migration's draft: its rows checked with the campaign's records, and the sheets' samples against them.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="draftPath">The draft's file.</param>
    /// <exception cref="DmcException">The draft cannot be read.</exception>
    public static List<string> Draft(string dmcFolder, string campaign, string draftPath)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        var draft = RecordDraft.Read(draftPath);
        var existing = RecordFiles.Read(root).Records;
        var scenes = SceneBook.For(dmcFolder, campaign);
        var findings = new List<string>();
        var known = new HashSet<string>(existing.Select(r => r.Person).Concat(draft.Select(d => d.To)).Concat(SheetTitles(root)), StringComparer.Ordinal);

        foreach (var d in draft)
        {
            var row = d.Row;
            string where = $"{d.Line} (\"{row.Word}\" for {d.To})";
            if (string.IsNullOrWhiteSpace(row.From))
                findings.Add($"no-speaker: {where}: no one is named as saying it");
            else if (row.From != Speaker.Anyone && !Speaker.IsGroup(row.From) && !known.Contains(row.From))
                findings.Add($"invalid: {where}: speaker \"{row.From}\" has no record or sheet");
            if (string.IsNullOrWhiteSpace(row.Source))
                findings.Add($"no-source: {where}: the line gives no scene, no words of the player, and is not the setup");
            else if (RecordRules.SourceError(scenes, row.Source) is { } source)
                findings.Add($"invalid: {where}: {source}");
            if (row.Kind is { } kind && !RowKind.All.Contains(kind, StringComparer.Ordinal))
                findings.Add($"invalid: {where}: kind \"{kind}\" is not usage, name or relationship");
        }

        // A word for two persons where a row lacks its speaker: it cannot be resolved without one.
        foreach (var word in draft.Where(d => d.Row.Word is not null).GroupBy(d => d.Row.Word!.Trim().ToLowerInvariant()))
        {
            var persons = word.Select(d => d.To).Distinct(StringComparer.Ordinal).ToList();
            if (persons.Count > 1 && word.Any(d => string.IsNullOrWhiteSpace(d.Row.From)))
                findings.Add($"ambiguous-no-speaker: \"{word.First().Row.Word}\" names {string.Join(" and ", persons)}, and {string.Join(", ", word.Where(d => string.IsNullOrWhiteSpace(d.Row.From)).Select(d => d.Line))} says no speaker");
        }

        // A never and a usage of the same word, speaker and person.
        foreach (var d in draft.Where(d => d.Row.Never is { Count: > 0 }))
            foreach (string speaker in d.Row.Never!)
                foreach (var other in draft.Where(o => o.To == d.To && o.Row.From == speaker && Same(o.Row.Word, d.Row.Word) && o.Row.Kind == RowKind.Usage))
                    findings.Add($"rows-disagree: {other.Line} has {Speaker.Shown(speaker)} say \"{other.Row.Word}\" for {d.To}, and {d.Line} says {Speaker.Shown(speaker)} never does");

        // Two Names lines of one sheet that give one speaker different words for one person.
        foreach (var pair in draft.Where(d => !string.IsNullOrWhiteSpace(d.Row.From)).GroupBy(d => (Speaker: d.Row.From!, d.To, File: FileOf(d.Line))))
        {
            var byLine = pair.GroupBy(d => d.Line).Select(g => (Line: g.Key, Words: g.Select(d => d.Row.Word?.Trim().ToLowerInvariant()).OfType<string>().ToHashSet(StringComparer.Ordinal))).ToList();
            for (int i = 0; i < byLine.Count; i++)
                for (int j = i + 1; j < byLine.Count; j++)
                    if (!byLine[i].Words.SetEquals(byLine[j].Words))
                        findings.Add($"names-lines-differ: {byLine[i].Line} and {byLine[j].Line} give {Speaker.Shown(pair.Key.Speaker)} different words for {pair.Key.To} ({string.Join(", ", byLine[i].Words.Order())} against {string.Join(", ", byLine[j].Words.Order())})");
        }

        // The sheets' samples against the draft's rows and the records' limits.
        var drafted = draft.GroupBy(d => d.To).Select(g => new PersonRecord(campaign, RecordFiles.Slug(g.Key), g.Key, null,
            g.Select(d => d.Row).ToList(), existing.FirstOrDefault(r => r.Person == g.Key)?.Limits ?? [], [])).ToList();
        drafted.AddRange(existing.Where(r => drafted.All(d => d.Person != r.Person)));
        findings.AddRange(Samples(root, new RecordIndex(drafted)));
        return findings.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The sample lines of each sheet against the rows: a word on the speaker's <c>never</c> list
    /// (<c>sample-breaks-rows</c>); for a sample that names whom it addresses, <c>(to X)</c>, a word the speaker uses
    /// only for someone else; and a sample with a word a limit of the speaker is about (<c>sample-stale</c>).
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="index">The rows to check against.</param>
    internal static List<string> Samples(string root, RecordIndex index)
    {
        var findings = new List<string>();
        foreach (var (file, title, lineNumber, text) in VoiceLines(root))
        {
            string where = $"character-sheets/{file}:{lineNumber}";
            string? speaker = index.Records.FirstOrDefault(r => r.Person.Equals(title, StringComparison.OrdinalIgnoreCase))?.Person;
            if (speaker is null)
                continue;
            foreach (Match m in Sample().Matches(text))
            {
                string words = m.Groups[1].Value;
                string? addressee = m.Groups[2].Success ? ResolveAddressee(index, m.Groups[2].Value) : null;
                // The words as the index reads them, longest first, so "Auntie Em" is not taken for an "Auntie" inside it.
                var said = index.WordsIn(words);
                foreach (var entry in said.SelectMany(f => f.Rows).Where(e => (e.Row.Never ?? []).Contains(speaker)))
                    findings.Add($"sample-breaks-rows: {where}: {speaker} never says \"{entry.Row.Word}\" for {entry.Person} ({entry.Row.Source})");
                if (addressee is not null)
                {
                    foreach (var found in said)
                    {
                        // A speaker's word for themselves ("Kibiw fwy high!" to Shale) is not a word for whom they address.
                        var mine = found.Rows.Where(r => r.Row.From == speaker && r.Row.Kind == RowKind.Usage && r.Person != speaker).ToList();
                        if (mine.Count > 0 && mine.All(r => r.Person != addressee))
                            findings.Add($"sample-breaks-rows: {where}: to {speaker}, \"{found.Word}\" means {string.Join(" and ", mine.Select(r => r.Person).Distinct())}, not {addressee}");
                    }
                }
                if (index.Record(speaker) is { } record)
                    foreach (var limit in record.Limits.Where(l => l.Text is not null))
                        foreach (string key in KeyWords(limit.Text!))
                            if (Holds(words, key))
                                findings.Add($"sample-stale: {where}: \"{key}\" against the limit \"{limit.Text!.Trim()}\" ({limit.Source})");
            }
        }
        return findings;
    }

    /// <summary>The key words of a limit: the words of three letters or more after "cannot" and the like, not common words.</summary>
    /// <param name="limit">The limit's text.</param>
    internal static List<string> KeyWords(string limit) =>
        LimitWords().Matches(limit).SelectMany(m => Word().Matches(m.Groups[1].Value).Select(w => w.Value.ToLowerInvariant()))
            .Where(w => !Retrieval.Words.IsCommon(w) && !ReadSet.IsCommonNameWord(w) && w is not ("yet" or "any" or "one" or "the" or "and"))
            .Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Each sheet's Voice lines (play card and Playing them): the file, the sheet's title, the 1-based line and its text.</summary>
    /// <param name="root">The campaign folder.</param>
    static IEnumerable<(string File, string Title, int Line, string Text)> VoiceLines(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (!Directory.Exists(folder))
            yield break;
        foreach (string path in Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            if (file == "minor-npcs.md" || file.EndsWith("-history.md", StringComparison.Ordinal))
                continue;
            var lines = File.ReadAllLines(path);
            string title = lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)) is { } t ? RecordFiles.SheetPerson(t[2..]) : Path.GetFileNameWithoutExtension(file);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].TrimStart().StartsWith("- **Voice**", StringComparison.Ordinal))
                    yield return (file, title, i + 1, lines[i]);
        }
    }

    /// <summary>The titles of a campaign's sheets, so a draft may name a person whose record it will create.</summary>
    /// <param name="root">The campaign folder.</param>
    static IEnumerable<string> SheetTitles(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (!Directory.Exists(folder))
            return [];
        return Directory.EnumerateFiles(folder, "*.md")
            .Select(p => File.ReadLines(p).FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)))
            .OfType<string>().Select(t => RecordFiles.SheetPerson(t[2..])).ToList();
    }

    /// <summary>The person an addressee names: a full name, or a word whose rows all mean one person; else the text as written.</summary>
    /// <param name="index">The index.</param>
    /// <param name="addressee">The text in <c>(to X)</c>.</param>
    static string ResolveAddressee(RecordIndex index, string addressee) =>
        index.Named(addressee).FirstOrDefault() ?? addressee.Trim();

    /// <summary>The file part of a draft row's line, <c>character-sheets/npc-x.md</c> of <c>character-sheets/npc-x.md:14</c>.</summary>
    /// <param name="line">The line.</param>
    static string FileOf(string line) => line.LastIndexOf(':') is var i and > 0 ? line[..i] : line;

    /// <summary>Whether a text holds a word whole, ignoring case.</summary>
    /// <param name="text">The text.</param>
    /// <param name="word">The word.</param>
    static bool Holds(string text, string word) => ReadSet.WholeWordAt(text, word.Trim()) >= 0;

    /// <summary>Whether two optional strings are equal, ignoring case.</summary>
    /// <param name="a">One.</param>
    /// <param name="b">The other.</param>
    static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>The findings as output lines: each finding, then how many.</summary>
    /// <param name="findings">The findings.</param>
    public static List<string> Report(List<string> findings) =>
        [.. findings, findings.Count == 0 ? "no findings" : findings.Count == 1 ? "1 finding" : $"{findings.Count} findings"];
}
