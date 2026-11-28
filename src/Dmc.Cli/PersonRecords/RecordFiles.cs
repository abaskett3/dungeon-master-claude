using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dmc.PersonRecords;

/// <summary>
/// Reading a campaign's records (spec 032 research R1): every <c>records/*.json</c>, each parsed with
/// <see cref="JsonDocument"/> into a <see cref="PersonRecord"/> whose fields are kept as the file gives them, so the
/// rules can say what is wrong. A file that is not a JSON object is an error line instead of a record. Reads only.
/// </summary>
internal static partial class RecordFiles
{
    /// <summary>The folder of the records in a campaign.</summary>
    public const string Folder = "records";

    /// <summary>The keys a record may have.</summary>
    static readonly string[] RecordKeys = ["person", "of", "sheet", "rows", "limits", "facts", "lists"];

    /// <summary>The keys a fact may have (spec 036).</summary>
    static readonly string[] FactKeys = ["key", "value", "section", "card", "source"];

    /// <summary>The keys a list entry may have (spec 036).</summary>
    static readonly string[] EntryKeys = ["list", "text", "section", "card", "source"];

    /// <summary>The keys a row may have.</summary>
    internal static readonly string[] RowKeys = ["kind", "from", "word", "when", "never", "source"];

    /// <summary>The keys a limit may have.</summary>
    static readonly string[] LimitKeys = ["text", "source"];

    /// <summary>A run of characters that are not lower-case or other letters, their marks or digits, for the slug.</summary>
    [GeneratedRegex(@"[^\p{Ll}\p{Lo}\p{Lm}\p{Mn}\p{Nd}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotSlug();

    /// <summary>The records of a campaign and the files that could not be read.</summary>
    /// <param name="Records">The records, in file-name order.</param>
    /// <param name="Errors">One line per file that is not a JSON object with a person, first file first.</param>
    internal sealed record Loaded(List<PersonRecord> Records, List<string> Errors);

    /// <summary>
    /// The file name of a person's record: their name in lower case, its letters, their marks and digits kept, those
    /// outside ASCII too, so two names never share one (T063: "Éla" and "Íla" were both <c>la.json</c>); each run of
    /// other characters a hyphen. Not normalized: <c>dmc</c> runs in invariant globalization mode, which does not compose.
    /// </summary>
    /// <param name="person">The person's full name.</param>
    public static string Slug(string person) => NotSlug().Replace(person.ToLowerInvariant(), "-").Trim('-') + ".json";

    /// <summary>A run of characters that are not lower-case ASCII letters or digits, for the file name before T063.</summary>
    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotAsciiSlug();

    /// <summary>
    /// The file name a record had before T063 kept letters outside ASCII: only <c>a</c> to <c>z</c> and digits kept.
    /// The check and <c>dmc records show</c> take it too, when it has a stem, so a record written under the old rule
    /// still passes and is found (T076).
    /// </summary>
    /// <param name="person">The person's full name.</param>
    public static string AsciiSlug(string person) => NotAsciiSlug().Replace(person.ToLowerInvariant(), "-").Trim('-') + ".json";

    /// <summary>Whether a sheet file is one person's: not <c>minor-npcs.md</c> or a <c>-history.md</c> file (T079).</summary>
    /// <param name="fileName">The file's name.</param>
    public static bool IsPersonSheet(string fileName) =>
        fileName != "minor-npcs.md" && !fileName.EndsWith("-history.md", StringComparison.Ordinal);

    /// <summary>A bracket at the end of a sheet's title, such as the <c> (Hysvearorn)</c> of <c># Rorn (Hysvearorn)</c>.</summary>
    [GeneratedRegex(@"\s*\([^()]*\)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TitleBracket();

    /// <summary>
    /// The person a sheet's title names: the title without a bracket at its end, so <c>Rorn (Hysvearorn)</c> is the
    /// record of <c>Rorn</c>; the whole title when the bracket is all there is.
    /// </summary>
    /// <param name="title">The text after the sheet's <c># </c>.</param>
    public static string SheetPerson(string title)
    {
        string person = TitleBracket().Replace(title, "").Trim();
        return person.Length > 0 ? person : title.Trim();
    }

    /// <summary>Whether a campaign folder has a <c>records/</c> folder: a campaign at standard 10.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static bool Has(string campaignFolder) => Directory.Exists(Path.Combine(campaignFolder, Folder));

    /// <summary>Every record of a campaign; none when it has no <c>records/</c>.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static Loaded Read(string campaignFolder)
    {
        var loaded = new Loaded([], []);
        string folder = Path.Combine(campaignFolder, Folder);
        if (!Directory.Exists(folder))
            return loaded;
        string campaign = Path.GetFileName(Path.TrimEndingDirectorySeparator(campaignFolder));
        foreach (string path in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(path);
            if (Parse(campaign, name, File.ReadAllText(path), out var record) is { } error)
                loaded.Errors.Add(error);
            else
                loaded.Records.Add(record!);
        }
        return loaded;
    }

    /// <summary>One record from its text; the error line when it is not a JSON object with a person.</summary>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="fileName">The file's name in <c>records/</c>.</param>
    /// <param name="text">The file's text.</param>
    /// <param name="record">The record, or null on an error.</param>
    /// <returns>Null when the record was read, else the error line.</returns>
    public static string? Parse(string campaign, string fileName, string text, out PersonRecord? record)
    {
        record = null;
        string label = $"{Folder}/{fileName}";
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return $"{label}: must be a JSON object";
            if (String(root, "person") is not { } person || person.Trim().Length == 0)
                return $"{label}: \"person\" must name the person";
            var problems = UnknownKeys(root, RecordKeys, "a record has person, of, sheet, rows, limits, facts, lists");
            if (root.TryGetProperty("of", out var of) && of.ValueKind != JsonValueKind.String)
                problems.Add("\"of\" must be a campaign's folder name");
            if (root.TryGetProperty("sheet", out var sheet) && sheet.ValueKind != JsonValueKind.String)
                problems.Add("\"sheet\" must be the sheet's file name");
            var facts = ReadList(root, "facts", ReadFact, problems);
            var entries = ReadList(root, "lists", ReadEntry, problems);
            var rows = new List<Row>();
            if (root.TryGetProperty("rows", out var rowsElement))
            {
                if (rowsElement.ValueKind != JsonValueKind.Array)
                    problems.Add("\"rows\" must be a list");
                else
                    rows.AddRange(rowsElement.EnumerateArray().Select(ReadRow));
            }
            var limits = new List<Limit>();
            if (root.TryGetProperty("limits", out var limitsElement))
            {
                if (limitsElement.ValueKind != JsonValueKind.Array)
                    problems.Add("\"limits\" must be a list");
                else
                    limits.AddRange(limitsElement.EnumerateArray().Select(ReadLimit));
            }
            record = new PersonRecord(campaign, fileName, person.Trim(), String(root, "of"), rows, limits, problems)
            {
                Sheet = String(root, "sheet"),
                Facts = facts,
                Lists = entries,
            };
            return null;
        }
        catch (JsonException e)
        {
            return $"{label}: not valid JSON (line {(e.LineNumber ?? 0) + 1})";
        }
    }

    /// <summary>A row as the file gives it.</summary>
    /// <param name="element">The row's JSON.</param>
    internal static Row ReadRow(JsonElement element) => ReadRow(element, RowKeys);

    /// <summary>A row as the file gives it, with the keys it may have (the draft's rows also have <c>to</c> and <c>line</c>).</summary>
    /// <param name="element">The row's JSON.</param>
    /// <param name="keys">The keys allowed.</param>
    internal static Row ReadRow(JsonElement element, string[] keys)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new Row(null, null, null, null, null, null, ["must be an object"]);
        var problems = UnknownKeys(element, keys, "a row has " + string.Join(", ", RowKeys));
        foreach (string key in new[] { "kind", "from", "word", "when", "source" })
            if (element.TryGetProperty(key, out var v) && v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                problems.Add($"\"{key}\" must be text"); // a null is a field left out, which the rules name
        List<string>? never = null;
        if (element.TryGetProperty("never", out var n))
        {
            if (n.ValueKind == JsonValueKind.Array && n.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String))
                never = n.EnumerateArray().Select(x => x.GetString()!).ToList();
            else
                problems.Add("\"never\" must be a list of persons or groups");
        }
        return new Row(String(element, "kind"), String(element, "from"), String(element, "word"), String(element, "when"), never, String(element, "source"), problems);
    }

    /// <summary>A limit as the file gives it.</summary>
    /// <param name="element">The limit's JSON.</param>
    static Limit ReadLimit(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new Limit(null, null, ["must be an object"]);
        var problems = UnknownKeys(element, LimitKeys, "a limit has text, source");
        foreach (string key in LimitKeys)
            if (element.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.String)
                problems.Add($"\"{key}\" must be text");
        return new Limit(String(element, "text"), String(element, "source"), problems);
    }

    /// <summary>The items of one of a record's lists, read by <paramref name="read"/>; none when it is missing, a problem when it is not a list.</summary>
    /// <typeparam name="T">The item's type.</typeparam>
    /// <param name="root">The record's JSON.</param>
    /// <param name="name">The list's key, <c>facts</c> or <c>lists</c>.</param>
    /// <param name="read">Reads one item.</param>
    /// <param name="problems">Receives the problem when it is not a list.</param>
    static List<T> ReadList<T>(JsonElement root, string name, Func<JsonElement, T> read, List<string> problems)
    {
        if (!root.TryGetProperty(name, out var element))
            return [];
        if (element.ValueKind == JsonValueKind.Array)
            return element.EnumerateArray().Select(read).ToList();
        problems.Add($"\"{name}\" must be a list");
        return [];
    }

    /// <summary>A fact as the file gives it; a number value is taken as its text.</summary>
    /// <param name="element">The fact's JSON.</param>
    static Fact ReadFact(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new Fact(null, null, null, false, null, ["must be an object"]);
        var problems = UnknownKeys(element, FactKeys, "a fact has key, value, section, card, source");
        string? value = String(element, "value");
        if (element.TryGetProperty("value", out var v))
        {
            if (v.ValueKind == JsonValueKind.Number)
                value = v.GetRawText();
            else if (v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                problems.Add("\"value\" must be text or a number");
        }
        TextFields(element, ["key", "section", "source"], problems);
        return new Fact(String(element, "key"), value, String(element, "section"), Card(element, problems), String(element, "source"), problems);
    }

    /// <summary>A list entry as the file gives it.</summary>
    /// <param name="element">The entry's JSON.</param>
    static ListEntry ReadEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new ListEntry(null, null, null, false, null, ["must be an object"]);
        var problems = UnknownKeys(element, EntryKeys, "a list entry has list, text, section, card, source");
        TextFields(element, ["list", "text", "section", "source"], problems);
        return new ListEntry(String(element, "list"), String(element, "text"), String(element, "section"), Card(element, problems), String(element, "source"), problems);
    }

    /// <summary>One problem for each of some fields that is there and is neither text nor null.</summary>
    /// <param name="element">The object.</param>
    /// <param name="keys">The fields that must be text.</param>
    /// <param name="problems">Receives the problems.</param>
    static void TextFields(JsonElement element, string[] keys, List<string> problems)
    {
        foreach (string key in keys)
            if (element.TryGetProperty(key, out var v) && v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                problems.Add($"\"{key}\" must be text");
    }

    /// <summary>An item's <c>card</c> mark: true or false; a problem when it is something else.</summary>
    /// <param name="element">The item's JSON.</param>
    /// <param name="problems">Receives the problem.</param>
    static bool Card(JsonElement element, List<string> problems)
    {
        if (!element.TryGetProperty("card", out var card))
            return false;
        if (card.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return card.ValueKind == JsonValueKind.True;
        problems.Add("\"card\" must be true or false");
        return false;
    }

    /// <summary>One problem per key of an object that is not one of the allowed keys.</summary>
    /// <param name="element">The object.</param>
    /// <param name="allowed">The keys it may have.</param>
    /// <param name="hint">What it may have, for the message.</param>
    static List<string> UnknownKeys(JsonElement element, string[] allowed, string hint) =>
        element.EnumerateObject().Where(p => !allowed.Contains(p.Name, StringComparer.Ordinal))
            .Select(p => $"unknown key \"{p.Name}\" ({hint})").ToList();

    /// <summary>A string property of a JSON object, or null.</summary>
    /// <param name="element">The object.</param>
    /// <param name="name">The property's name.</param>
    internal static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
