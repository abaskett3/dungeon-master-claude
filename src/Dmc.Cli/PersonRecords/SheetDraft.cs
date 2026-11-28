using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Campaign;

namespace Dmc.PersonRecords;

/// <summary>
/// Migration 011's draft (spec 036 FR-012, research R7, data-model.md § Sheet draft): one entry for each line of each
/// person sheet, a companion of the migration's backup (<see cref="BackupFolder.SheetsDraftSuffix"/>), never a campaign
/// file. It holds what has a regular shape: the section, the bold label, the text, and a source only where the line
/// holds exactly one scene mark. The model then splits each entry into one fact an entry, and the player rules every
/// entry with no source. Every entry points to its sheet line, so <c>dmc campaign verify</c>'s accounting can follow it.
/// </summary>
internal static partial class SheetDraft
{
    /// <summary>One line of a sheet as the draft holds it.</summary>
    /// <param name="To">The sheet's person (its title without a bracket at its end).</param>
    /// <param name="Sheet">The sheet's file name.</param>
    /// <param name="Section">The <c>## </c> heading it is under; null for the opening fields.</param>
    /// <param name="Label">The line's bold label without its hint (<c>Voice</c> of <c>**Voice** (and one sample line):</c>); null for a line with none.</param>
    /// <param name="Text">The text after the label, or the line's text.</param>
    /// <param name="Source">The line's one scene mark; null when it has none or several.</param>
    /// <param name="Line">The file and line: <c>character-sheets/npc-x.md:14</c>.</param>
    internal sealed record Entry(string To, string Sheet, string? Section, string? Label, string Text, string? Source, string Line);

    /// <summary>A labelled bullet: the bold label, an optional bracketed hint, a colon, the value.</summary>
    [GeneratedRegex(@"^\s*[-*]\s+\*\*(?<label>[^*]+?)\*\*\s*(?:\([^)]*\))?\s*:\s*(?<value>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex Labelled();

    /// <summary>A bullet with no label: its text.</summary>
    [GeneratedRegex(@"^\s*[-*]\s+(?<value>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Bullet();

    /// <summary>A scene mark: <c>s40</c> or <c>s40.2</c>, as a whole word.</summary>
    [GeneratedRegex(@"(?<![\w.])s\d+(?:\.\d+)?\b", RegexOptions.CultureInvariant)]
    private static partial Regex SceneMark();

    /// <summary>The draft's path for a campaign: beside the backup the migration made (the marked one, else the latest).</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <exception cref="DmcException">The campaign has no backup.</exception>
    public static string PathFor(string campaignFolder, string campaign) =>
        BackupFolder.Companion(BackupFolder.Resolve(campaignFolder, campaign, null), BackupFolder.SheetsDraftSuffix);

    /// <summary>
    /// The entries of every person sheet in <c>character-sheets/</c> (not <c>minor-npcs.md</c> or a <c>-history.md</c>
    /// file), in file and line order: each labelled bullet, each other bullet and each other line of text; not the
    /// title, a heading, a table, a comment or a blank line.
    /// </summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    public static List<Entry> FromSheets(string campaignFolder)
    {
        var entries = new List<Entry>();
        string folder = Path.Combine(campaignFolder, "character-sheets");
        if (!Directory.Exists(folder))
            return entries;
        foreach (string path in Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            if (!RecordFiles.IsPersonSheet(file))
                continue;
            var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            string person = lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)) is { } t
                ? RecordFiles.SheetPerson(t[2..]) : Path.GetFileNameWithoutExtension(file);
            string? section = null;
            bool comment = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i];
                if (comment)
                {
                    comment = !text.Contains("-->", StringComparison.Ordinal);
                    continue;
                }
                string trimmed = text.Trim();
                if (trimmed.StartsWith("<!--", StringComparison.Ordinal))
                {
                    comment = !trimmed.Contains("-->", StringComparison.Ordinal);
                    continue;
                }
                if (trimmed.Length == 0 || trimmed.StartsWith("# ", StringComparison.Ordinal) || trimmed.StartsWith('|') || trimmed.StartsWith("###", StringComparison.Ordinal))
                    continue;
                if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                {
                    section = trimmed[3..].Trim();
                    continue;
                }
                string? label = null;
                string value = trimmed;
                if (Labelled().Match(text) is { Success: true } m)
                {
                    label = m.Groups["label"].Value.Trim();
                    value = m.Groups["value"].Value.Trim();
                }
                else if (Bullet().Match(text) is { Success: true } b)
                    value = b.Groups["value"].Value.Trim();
                var marks = SceneMark().Matches(value).Select(x => x.Value).Distinct(StringComparer.Ordinal).ToList();
                entries.Add(new Entry(person, file, section, label, value, marks.Count == 1 ? marks[0] : null, $"character-sheets/{file}:{i + 1}"));
            }
        }
        return entries;
    }

    /// <summary>Writes a new draft; refuses when one exists, so the model's work on it is never lost.</summary>
    /// <param name="path">The draft's path.</param>
    /// <param name="entries">The entries.</param>
    /// <exception cref="DmcException">The draft exists.</exception>
    public static void Write(string path, IReadOnlyList<Entry> entries)
    {
        if (File.Exists(path))
            throw new DmcException($"{Path.GetFileName(path)} exists: work on it with Edit");
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        writer.WriteStartObject();
        writer.WriteStartArray("entries");
        foreach (var e in entries)
        {
            writer.WriteStartObject();
            writer.WriteString("to", e.To);
            writer.WriteString("sheet", e.Sheet);
            writer.WriteString("line", e.Line);
            Optional(writer, "section", e.Section);
            Optional(writer, "label", e.Label);
            writer.WriteString("text", e.Text);
            Optional(writer, "source", e.Source);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>Writes a string property, or null when there is no value.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="key">The property's name.</param>
    /// <param name="value">Its value, or null.</param>
    static void Optional(Utf8JsonWriter writer, string key, string? value)
    {
        if (value is not null)
            writer.WriteString(key, value);
        else
            writer.WriteNull(key);
    }
}
