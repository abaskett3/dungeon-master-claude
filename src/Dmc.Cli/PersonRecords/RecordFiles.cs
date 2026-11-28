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
    static readonly string[] RecordKeys = ["person", "of", "rows", "limits"];

    /// <summary>The keys a row may have.</summary>
    internal static readonly string[] RowKeys = ["kind", "from", "word", "when", "never", "source"];

    /// <summary>The keys a limit may have.</summary>
    static readonly string[] LimitKeys = ["text", "source"];

    /// <summary>A run of characters that are not lower-case letters or digits, for the slug.</summary>
    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotSlug();

    /// <summary>The records of a campaign and the files that could not be read.</summary>
    /// <param name="Records">The records, in file-name order.</param>
    /// <param name="Errors">One line per file that is not a JSON object with a person, first file first.</param>
    internal sealed record Loaded(List<PersonRecord> Records, List<string> Errors);

    /// <summary>The file name of a person's record: their name in lower case, each run of other characters a hyphen.</summary>
    /// <param name="person">The person's full name.</param>
    public static string Slug(string person) => NotSlug().Replace(person.ToLowerInvariant(), "-").Trim('-') + ".json";

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
            var problems = UnknownKeys(root, RecordKeys, "a record has person, of, rows, limits");
            if (root.TryGetProperty("of", out var of) && of.ValueKind != JsonValueKind.String)
                problems.Add("\"of\" must be a campaign's folder name");
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
            record = new PersonRecord(campaign, fileName, person.Trim(), String(root, "of"), rows, limits, problems);
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
