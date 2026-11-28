namespace Dmc.PersonRecords;

/// <summary>
/// The rules every record of a campaign must meet (spec 032 FR-001 to FR-003, data-model.md § Rules): checked after each
/// edit (<c>dmc records edited</c>) and by <c>dmc records check</c>. Each break is one line naming the file, the row
/// and what is wrong; the first is the one a refusal prints. A usage that makes one word mean two persons to the same
/// speaker is a note, not a break.
/// </summary>
internal static class RecordRules
{
    /// <summary>The breaks and the notes for a campaign's records.</summary>
    /// <param name="Errors">One line per break, in file and row order; empty when every record passes.</param>
    /// <param name="Notes">One line per word that means two persons or more to one speaker.</param>
    internal sealed record Outcome(List<string> Errors, List<string> Notes);

    /// <summary>Checks every record of a campaign.</summary>
    /// <param name="scenes">Where a scene source is looked up.</param>
    /// <param name="records">The campaign's records.</param>
    public static Outcome Check(SceneBook scenes, IReadOnlyList<PersonRecord> records)
    {
        var errors = new List<string>();
        var persons = new HashSet<string>(records.Select(r => r.Person), StringComparer.Ordinal);
        foreach (var group in records.GroupBy(r => r.Person, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"two records for \"{group.Key}\": {string.Join(", ", group.Select(r => r.Label))}");
        foreach (var record in records)
        {
            foreach (string problem in record.Problems)
                errors.Add($"{record.Label}: {problem}");
            string slug = RecordFiles.Slug(record.Person);
            if (!record.FileName.Equals(slug, StringComparison.Ordinal))
                errors.Add($"{record.Label}: the record of \"{record.Person}\" must be {RecordFiles.Folder}/{slug}");
            if (record.Of is { } of && !scenes.IsLinked(of))
                errors.Add($"{record.Label}: \"of\" names {of}, which is not a campaign this one continues");
            for (int i = 0; i < record.Rows.Count; i++)
                errors.AddRange(RowErrors(scenes, persons, record, i));
            for (int i = 0; i < record.Limits.Count; i++)
            {
                var limit = record.Limits[i];
                string where = $"{record.Label} limit {i + 1}";
                errors.AddRange(limit.Problems.Select(p => $"{where}: {p}"));
                if (string.IsNullOrWhiteSpace(limit.Text))
                    errors.Add($"{where}: no text");
                if (SourceError(scenes, limit.Source) is { } error)
                    errors.Add($"{where}: {error}");
            }
        }
        return new Outcome(errors, Notes(records));
    }

    /// <summary>The breaks of one row.</summary>
    /// <param name="scenes">Where a scene source is looked up.</param>
    /// <param name="persons">The full names of the campaign's records.</param>
    /// <param name="record">The record.</param>
    /// <param name="index">The row's index.</param>
    static IEnumerable<string> RowErrors(SceneBook scenes, HashSet<string> persons, PersonRecord record, int index)
    {
        var row = record.Rows[index];
        string where = $"{record.Label} row {index + 1}" + (row.Word is { } w ? $" (\"{w}\")" : "");
        foreach (string problem in row.Problems)
            yield return $"{where}: {problem}";
        if (row.Kind is null)
            yield return $"{where}: no kind (usage, name or relationship)";
        else if (!RowKind.All.Contains(row.Kind, StringComparer.Ordinal))
            yield return $"{where}: kind \"{row.Kind}\" is not usage, name or relationship";
        if (string.IsNullOrWhiteSpace(row.Word))
            yield return $"{where}: no word";
        if (SpeakerError(persons, row) is { } speaker)
            yield return $"{where}: {speaker}";
        foreach (string never in row.Never ?? [])
            if (!persons.Contains(never) && !Speaker.IsGroup(never))
                yield return $"{where}: never \"{never}\" is not a person of the records or group: …{Hint(persons, never)}";
        if (SourceError(scenes, row.Source) is { } source)
            yield return $"{where}: {source}";
        for (int j = 0; j < index; j++)
        {
            var other = record.Rows[j];
            if (Same(other.Kind, row.Kind) && Same(other.From, row.From) && Same(other.Word, row.Word))
                yield return $"{where}: the same row as row {j + 1}";
        }
        if (row.From is { } from && row.Word is { } word)
            for (int j = 0; j < record.Rows.Count; j++)
            {
                var other = record.Rows[j];
                if (j != index && Same(other.Word, word) && other.Never is { } list && list.Contains(from, StringComparer.Ordinal))
                    yield return $"{where}: {Speaker.Shown(from)} never says \"{word}\" for {record.Person} (row {j + 1}, {other.Source})";
            }
    }

    /// <summary>What is wrong with a row's speaker for its kind, or null when nothing is.</summary>
    /// <param name="persons">The full names of the campaign's records.</param>
    /// <param name="row">The row.</param>
    static string? SpeakerError(HashSet<string> persons, Row row)
    {
        if (string.IsNullOrWhiteSpace(row.From))
            return "no speaker (from): say who uses the word, or anyone for a name";
        string from = row.From;
        if (from == Speaker.Anyone)
            return row.Kind is RowKind.Usage or RowKind.Relationship ? $"a word anyone says is kind name, not {row.Kind}" : null;
        if (Speaker.IsGroup(from))
            return row.Kind switch
            {
                RowKind.Relationship => "a relationship is from a person, not a group",
                RowKind.Name => "kind name is from anyone; a group's word is kind usage",
                _ => null,
            };
        if (!persons.Contains(from))
            return $"speaker \"{from}\" is not a person of the records, anyone, or group: …{Hint(persons, from)}";
        return row.Kind == RowKind.Name ? "kind name is from anyone; a person's word is kind usage" : null;
    }

    /// <summary>What is wrong with a source, or null when nothing is.</summary>
    /// <param name="scenes">Where a scene source is looked up.</param>
    /// <param name="source">The source.</param>
    internal static string? SourceError(SceneBook scenes, string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "no source (a scene such as s4.2, player: \"…\", setup, or inference)";
        if (!RecordSource.IsWellFormed(source))
            return $"source \"{source}\" is not a scene (s4.2), player: \"…\", setup, or inference";
        if (RecordSource.Scene(source) is { } scene && scenes.Missing(scene) is { } missing)
            return missing;
        return null;
    }

    /// <summary>One note per person speaker whose usage rows give one word for two persons or more.</summary>
    /// <param name="records">The records.</param>
    internal static List<string> Notes(IReadOnlyList<PersonRecord> records) =>
        records.SelectMany(r => r.Rows.Where(row => row.Kind == RowKind.Usage && row.From is { } f && f != Speaker.Anyone && !Speaker.IsGroup(f) && row.Word is not null)
                .Select(row => (Record: r, Row: row)))
            .GroupBy(x => (x.Row.From!, x.Row.Word!.ToLowerInvariant()))
            .Select(g => (g.First().Row, Persons: g.Select(x => x.Record.Person).Distinct(StringComparer.Ordinal).ToList()))
            .Where(x => x.Persons.Count > 1)
            .Select(x => $"to {x.Row.From}, \"{x.Row.Word}\" now means {string.Join(" and ", x.Persons)}; the hook prints both.")
            .ToList();

    /// <summary>Whether two optional strings are equal, ignoring case.</summary>
    /// <param name="a">One.</param>
    /// <param name="b">The other.</param>
    static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>A "did you mean" when a speaker is a word of exactly one person's name.</summary>
    /// <param name="persons">The full names.</param>
    /// <param name="who">The speaker as written.</param>
    static string Hint(HashSet<string> persons, string who)
    {
        var hits = persons.Where(p => p.Split(' ').Any(w => w.Equals(who, StringComparison.OrdinalIgnoreCase))).ToList();
        return hits.Count == 1 ? $" (did you mean \"{hits[0]}\"?)" : "";
    }
}

/// <summary>
/// Where scene sources are looked up (spec 032 research R2): this campaign's session logs, and those of each linked
/// campaign it continues. A scene <c>sN.M</c> exists when <c>story/sessions/session-NNN.md</c> exists and holds at
/// least M <c>### Scene</c> headings; <c>sN</c> when the file exists.
/// </summary>
/// <param name="campaignFolder">The campaign whose records are checked.</param>
/// <param name="linked">The folder name and folder of each campaign it continues.</param>
internal sealed class SceneBook(string campaignFolder, IReadOnlyDictionary<string, string> linked)
{
    /// <summary>The number of scene headings in each session log read so far, by path; -1 for a file that does not exist.</summary>
    readonly Dictionary<string, int> _scenes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True for a book that finds every scene and every linked campaign: for a check of shape only.</summary>
    bool _lenient;

    /// <summary>
    /// A book that finds every scene and every linked campaign, for a check of the records' shape alone: migration 010's
    /// <c>records-in-place</c>, which sees only the campaign's own files. The check after each edit has already looked up
    /// each scene when the record was written.
    /// </summary>
    public static SceneBook Lenient() => new("", new Dictionary<string, string>()) { _lenient = true };

    /// <summary>A book for a campaign with no linked campaigns.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static SceneBook Alone(string campaignFolder) => new(campaignFolder, new Dictionary<string, string>());

    /// <summary>A book for a campaign in a DMC folder, with the campaigns it continues (<see cref="RecordIndex.Earlier"/>).</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    public static SceneBook For(string dmcFolder, string campaign) =>
        new(Path.Combine(dmcFolder, "adventures", campaign), RecordIndex.Earlier(dmcFolder, campaign));

    /// <summary>Whether a campaign is one this one continues.</summary>
    /// <param name="name">Its folder name.</param>
    public bool IsLinked(string name) => _lenient || linked.ContainsKey(name);

    /// <summary>Why a scene cannot be found, or null when it exists.</summary>
    /// <param name="scene">The scene.</param>
    public string? Missing(RecordSource.SceneRef scene)
    {
        if (_lenient)
            return null;
        string folder;
        if (scene.Campaign is null)
            folder = campaignFolder;
        else if (!linked.TryGetValue(scene.Campaign, out folder!))
            return $"{scene.Campaign} is not a campaign this one continues";
        string log = Path.Combine(folder, "story", "sessions", $"session-{scene.Session:000}.md");
        if (!_scenes.TryGetValue(log, out int count))
            _scenes[log] = count = File.Exists(log) ? File.ReadLines(log).Count(l => l.StartsWith("### Scene", StringComparison.Ordinal)) : -1;
        string name = (scene.Campaign is null ? "" : scene.Campaign + " ") + $"s{scene.Session}" + (scene.Scene is { } n ? $".{n}" : "");
        if (count < 0 || (scene.Scene is { } m && (m < 1 || m > count)))
            return $"scene {name} is not in the session logs";
        return null;
    }
}
