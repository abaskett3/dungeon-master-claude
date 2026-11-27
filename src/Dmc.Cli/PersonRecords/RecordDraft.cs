using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Size;

namespace Dmc.PersonRecords;

/// <summary>
/// Migration 010's draft (spec 032 FR-008, research R7, data-model.md § Draft): the rows the migration proposes before
/// the player rules, a companion of the migration's backup (<see cref="BackupFolder.RecordsDraftSuffix"/>), never a
/// campaign file. <c>dmc records draft</c> starts it with what has a regular shape, each People <c>(also: …)</c> item of
/// canon as a row with no speaker; DMC adds the rows of the Names lines, each pointing to its sheet line. Its rows may
/// break the rules the records keep; <see cref="RecordCheck.Draft"/> turns each break into a finding for the player.
/// </summary>
internal static partial class RecordDraft
{
    /// <summary>The keys a draft row may have: a row's, plus the person it names and the line it came from.</summary>
    static readonly string[] DraftKeys = [.. RecordFiles.RowKeys, "to", "line"];

    /// <summary>A People entry with an <c>(also: …)</c> bracket: group 1 the first bold name, group 2 the bracket's items.</summary>
    [GeneratedRegex(@"^\s*[-*]\s*\*\*(.+?)\*\*.*?\(also:\s*([^)]*)\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlsoEntry();

    /// <summary>The draft's path for a campaign: beside the backup the migration made (the marked one, else the latest).</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <exception cref="DmcException">The campaign has no backup.</exception>
    public static string PathFor(string campaignFolder, string campaign) =>
        BackupFolder.Companion(BackupFolder.Resolve(campaignFolder, campaign, null), BackupFolder.RecordsDraftSuffix);

    /// <summary>The rows of canon's People register that have a regular shape: each <c>(also: …)</c> item, as a row with no speaker.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static List<DraftRow> FromCanon(string campaignFolder)
    {
        var rows = new List<DraftRow>();
        string canon = Path.Combine(campaignFolder, "story", "canon.md");
        if (!File.Exists(canon))
            return rows;
        var lines = Measurement.ReadLines(canon);
        bool inside = false;
        for (int i = 0; i < lines.Count; i++)
        {
            string text = lines[i].Text;
            if (Measurement.IsSectionHeading(text))
            {
                inside = text[3..].Trim().Equals(Retrieval.Register.PeopleHeading, StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inside || AlsoEntry().Match(text) is not { Success: true } m)
                continue;
            string person = m.Groups[1].Value.Trim();
            foreach (string item in m.Groups[2].Value.Split([',', ';']).Select(s => s.Trim().Trim('"', '*', '“', '”').Trim()).Where(s => s.Length > 0))
                rows.Add(new DraftRow(person, new Row(null, null, item, null, null, null, []), $"story/canon.md:{i + 1}"));
        }
        return rows;
    }

    /// <summary>Writes a new draft; refuses when one exists, so DMC's added rows are never lost.</summary>
    /// <param name="path">The draft's path.</param>
    /// <param name="rows">The rows.</param>
    /// <exception cref="DmcException">The draft exists.</exception>
    public static void Write(string path, IReadOnlyList<DraftRow> rows)
    {
        if (File.Exists(path))
            throw new DmcException($"{Path.GetFileName(path)} exists: add to it with Edit, or rule on its findings with dmc records check --draft");
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        writer.WriteStartObject();
        writer.WriteStartArray("rows");
        foreach (var d in rows)
        {
            writer.WriteStartObject();
            writer.WriteString("to", d.To);
            writer.WriteString("line", d.Line);
            void Optional(string key, string? value)
            {
                if (value is not null)
                    writer.WriteString(key, value);
                else
                    writer.WriteNull(key);
            }
            Optional("kind", d.Row.Kind);
            Optional("from", d.Row.From);
            writer.WriteString("word", d.Row.Word ?? "");
            Optional("source", d.Row.Source);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>The rows of a draft file.</summary>
    /// <param name="path">The draft's path.</param>
    /// <exception cref="DmcException">There is no draft, or it is not a JSON object with a <c>rows</c> list.</exception>
    public static List<DraftRow> Read(string path)
    {
        if (!File.Exists(path))
            throw new DmcException($"no draft at {Path.GetFileName(path)}: run dmc records draft first");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
                throw new DmcException($"{Path.GetFileName(path)}: must be a JSON object with a \"rows\" list");
            return rows.EnumerateArray().Select(e => new DraftRow(
                RecordFiles.String(e, "to") ?? "?",
                RecordFiles.ReadRow(e, DraftKeys),
                RecordFiles.String(e, "line") ?? "?")).ToList();
        }
        catch (JsonException e)
        {
            throw new DmcException($"{Path.GetFileName(path)}: not valid JSON (line {(e.LineNumber ?? 0) + 1})");
        }
    }
}
