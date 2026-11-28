using System.Globalization;
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
        findings.AddRange(SheetAndRecord(root, loaded.Records));
        return findings;
    }

    /// <summary>
    /// The <c>sheet-and-record</c> findings (spec 036 FR-011): a Markdown sheet that still exists beside a record that
    /// prints it, two homes for one person's facts, as a migration left part way through leaves them.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="records">The campaign's records.</param>
    static IEnumerable<string> SheetAndRecord(string root, IReadOnlyList<PersonRecord> records) =>
        records.Where(r => r.Sheet is { } s && r.HasSheetContent && File.Exists(Path.Combine(root, "character-sheets", s)))
            .Select(r => $"sheet-and-record: character-sheets/{r.Sheet}: {r.Person}'s record prints this sheet; remove the Markdown sheet once its lines are in the record");

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

        // Two Names lines of one sheet that give one speaker different words for one person, a speaker and person one
        // line has and the other lacks among them (#237's five sheets).
        foreach (var file in draft.Where(d => !string.IsNullOrWhiteSpace(d.Row.From)).GroupBy(d => FileOf(d.Line)))
        {
            var namesLines = file.Select(d => d.Line).Distinct(StringComparer.Ordinal).Where(l => IsNamesLine(root, l)).OrderBy(l => l.Length).ThenBy(l => l, StringComparer.Ordinal).ToList();
            if (namesLines.Count < 2)
                continue;
            foreach (var pair in file.Where(d => namesLines.Contains(d.Line)).GroupBy(d => (Speaker: d.Row.From!, d.To)))
            {
                var byLine = namesLines.Select(l => (Line: l, Words: pair.Where(d => d.Line == l).Select(d => d.Row.Word?.Trim().ToLowerInvariant()).OfType<string>().ToHashSet(StringComparer.Ordinal))).ToList();
                for (int i = 0; i < byLine.Count; i++)
                    for (int j = i + 1; j < byLine.Count; j++)
                        if (!byLine[i].Words.SetEquals(byLine[j].Words))
                            findings.Add($"names-lines-differ: {byLine[i].Line} and {byLine[j].Line} give {Speaker.Shown(pair.Key.Speaker)} different words for {pair.Key.To} ({WordList(byLine[i].Words)} against {WordList(byLine[j].Words)})");
            }
        }

        // The sheets' samples against the draft's rows and the records' limits.
        var drafted = draft.GroupBy(d => d.To).Select(g => new PersonRecord(campaign, RecordFiles.Slug(g.Key), g.Key, null,
            g.Select(d => d.Row).ToList(), existing.FirstOrDefault(r => r.Person == g.Key)?.Limits ?? [], [])).ToList();
        drafted.AddRange(existing.Where(r => drafted.All(d => d.Person != r.Person)));
        findings.AddRange(Samples(root, new RecordIndex(drafted)));
        return findings.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The sample lines of each sheet against the rows: a word on the speaker's <c>never</c> list that they use for no one
    /// else (<c>sample-breaks-rows</c>); for a sample that names whom it addresses, <c>(to X)</c>, a word the speaker uses
    /// only for someone else; and a sample with a word a limit of the speaker is about, lisped or not (<c>sample-stale</c>).
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="index">The rows to check against.</param>
    internal static List<string> Samples(string root, RecordIndex index)
    {
        var findings = new List<string>();
        foreach (var (where, title, text) in VoiceLines(root))
        {
            string? speaker = index.Records.FirstOrDefault(r => r.Person.Equals(title, StringComparison.OrdinalIgnoreCase))?.Person;
            if (speaker is null)
                continue;
            foreach (Match m in Sample().Matches(text))
            {
                string words = m.Groups[1].Value;
                string? addressee = m.Groups[2].Success ? ResolveAddressee(index, m.Groups[2].Value) : null;
                // The words as the index reads them, longest first, so "Auntie Em" is not taken for an "Auntie" inside it.
                var said = index.WordsIn(words);
                // A never word breaks the rows only when the speaker has no usage of it for someone else: Kibil's "Auntie"
                // is Kole, though he never says it for Hush; a gloss to Hush is caught below.
                foreach (var entry in said.Where(f => !f.Rows.Any(r => r.Row.From == speaker && r.Row.Kind == RowKind.Usage))
                             .SelectMany(f => f.Rows).Where(e => (e.Row.Never ?? []).Contains(speaker)))
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
                            if (Holds(words, key) || Lisped(key) is { } lisped && Holds(words, lisped))
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

    /// <summary>
    /// A limit's key word as a child's lisp says it, r and l spoken as w ("fly" as "fwy", #237's case of Hala's sample),
    /// or null when the word has neither letter.
    /// </summary>
    /// <param name="key">The key word, in lower case.</param>
    internal static string? Lisped(string key) =>
        key.IndexOfAny(['r', 'l']) < 0 ? null : key.Replace('r', 'w').Replace('l', 'w');

    /// <summary>
    /// Each sheet's Voice lines (play card and Playing them): where it is, the sheet's person and its text. A Markdown
    /// sheet's line is <c>character-sheets/&lt;file&gt;:&lt;n&gt;</c>; for a sheet a record prints (spec 036), each entry of
    /// its Voice list, <c>records/&lt;file&gt; list &lt;n&gt;</c>.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    static IEnumerable<(string Where, string Title, string Text)> VoiceLines(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (Directory.Exists(folder))
            foreach (string path in Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.Ordinal))
            {
                string file = Path.GetFileName(path);
                if (!RecordFiles.IsPersonSheet(file))
                    continue;
                var lines = File.ReadAllLines(path);
                string title = lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)) is { } t ? RecordFiles.SheetPerson(t[2..]) : Path.GetFileNameWithoutExtension(file);
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i].TrimStart().StartsWith("- **Voice**", StringComparison.Ordinal))
                        yield return ($"character-sheets/{file}:{i + 1}", title, lines[i]);
            }
        foreach (var printed in RecordSheet.Printed(root))
        {
            var record = printed.Record!;
            for (int i = 0; i < record.Lists.Count; i++)
                if (record.Lists[i] is { List: { } list, Text: { } text } && list.Trim().Equals("Voice", StringComparison.OrdinalIgnoreCase))
                    yield return ($"{record.Label} list {i + 1}", record.Person, text);
        }
    }

    /// <summary>
    /// The titles of a campaign's sheets, so a draft may name a person whose record it will create, and the edit hook
    /// a person whose record is still to be written; not <c>minor-npcs.md</c> or a <c>-history.md</c> file, whose titles
    /// name no one.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    internal static IEnumerable<string> SheetTitles(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (!Directory.Exists(folder))
            return [];
        return Directory.EnumerateFiles(folder, "*.md")
            .Where(p => RecordFiles.IsPersonSheet(Path.GetFileName(p)))
            .Select(p => File.ReadLines(p).FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)))
            .OfType<string>().Select(t => RecordFiles.SheetPerson(t[2..])).ToList();
    }

    /// <summary>A line's words for a finding, or <c>nothing</c> when the line gives none.</summary>
    /// <param name="words">The words.</param>
    static string WordList(HashSet<string> words) => words.Count == 0 ? "nothing" : string.Join(", ", words.Order(StringComparer.Ordinal));

    /// <summary>Whether a draft row's line (<c>character-sheets/npc-x.md:14</c>) is a sheet's "Names they use" line.</summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="line">The draft row's line.</param>
    static bool IsNamesLine(string root, string line)
    {
        int colon = line.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(line[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 1)
            return false;
        string path = Path.Combine(root, line[..colon]);
        if (!File.Exists(path))
            return false;
        string? text = File.ReadLines(path).Skip(number - 1).FirstOrDefault();
        return text is not null && text.Contains("**Names they use**", StringComparison.Ordinal);
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
