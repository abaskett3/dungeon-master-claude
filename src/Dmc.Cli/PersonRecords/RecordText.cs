namespace Dmc.PersonRecords;

/// <summary>
/// A record as text for the player and for the resume (spec 032 FR-009, contracts/dmc-records.md § show): what the
/// person is called and by whom, their relationships from both sides, the words they use for others, and their limits,
/// each with its source. Nobody reads the JSON to check a sheet.
/// </summary>
internal static class RecordText
{
    /// <summary>The lines of one record.</summary>
    /// <param name="index">The index the record is in, for the rows that name others.</param>
    /// <param name="record">The record.</param>
    public static List<string> Of(RecordIndex index, PersonRecord record)
    {
        string title = record.Of is { } of ? $"{record.Person} (of {of})" : record.Person;
        var lines = new List<string> { $"{title}, called:" };
        foreach (var row in record.Rows.Where(r => r.Kind != RowKind.Relationship))
        {
            string never = row.Never is { Count: > 0 } n ? $"; never {string.Join(", ", n.Select(Speaker.Shown))}" : "";
            string when = row.When is { Length: > 0 } w ? $", {w}" : "";
            lines.Add($"- \"{row.Word}\": {Speaker.Shown(row.From ?? "?")}{when}{never} ({row.Source})");
        }
        var relationships = record.Rows.Where(r => r.Kind == RowKind.Relationship).Select(r => $"{r.From}'s {r.Word} ({r.Source})")
            .Concat(index.BySpeaker(record.Person).Where(e => e.Row.Kind == RowKind.Relationship).Select(e => $"{e.Person} is their {e.Row.Word} ({e.Row.Source})"))
            .ToList();
        if (relationships.Count > 0)
            lines.Add($"Relationships: {string.Join("; ", relationships)}");
        var says = index.BySpeaker(record.Person).Where(e => e.Row.Kind == RowKind.Usage && e.Person != record.Person)
            .Select(e => $"\"{e.Row.Word}\" for {e.Person}" + (e.Row.When is { Length: > 0 } w ? $", {w}" : "") + $" ({e.Row.Source})").ToList();
        if (says.Count > 0)
            lines.Add($"{record.Person} says: {string.Join("; ", says)}");
        if (record.Limits.Count > 0)
            lines.Add($"Limits: {string.Join(" ", record.Limits.Select(l => $"{l.Text?.Trim()} ({l.Source})"))}");
        return lines;
    }
}
