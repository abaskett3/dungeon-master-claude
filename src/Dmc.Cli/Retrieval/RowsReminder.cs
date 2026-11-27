using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dmc.PersonRecords;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>
/// The hook's rows part (spec 032 FR-005, research R5, contracts/context-check-rows.md): who calls whom what, from the
/// records. For each row word in the player's message, every row of that word, each time, so an ambiguous word is never
/// resolved by guessing; and once per scene, for each person present or named, the words between them and the others
/// present, what they are called, and their limits. A change in who is present (the Present line) starts a new scene.
/// Prints nothing in a campaign without <c>records/</c>. Reads only, stores nothing: what was printed in this scene is
/// read back from the session's log.
/// </summary>
internal static class RowsReminder
{
    /// <summary>The most bytes the part may print: word lines first, then person blocks while they fit.</summary>
    public const int CapBytes = 2_400;

    /// <summary>The start of the header; the log is searched for it.</summary>
    public const string HeaderStart = "Called, scene ";

    /// <summary>The middle of the header, between the scene key and the persons' names.</summary>
    public const string HeaderMiddle = " (who says what for whom here; dmc records show for more): ";

    /// <summary>The separator between the persons' names in the header.</summary>
    const string NameSeparator = "; ";

    /// <summary>
    /// The lines for a hook's input: nothing when the input has no prompt or folder, the prompt is a command, there is no
    /// active open campaign with <c>records/</c>, or nothing is due.
    /// </summary>
    /// <param name="input">The hook's JSON (<c>prompt</c>, <c>cwd</c>, <c>transcript_path</c>).</param>
    /// <param name="projectFolder">The <c>CLAUDE_PROJECT_DIR</c> the hook runs with, used when <c>cwd</c> is not a DMC folder; null for none.</param>
    /// <returns>The lines to print; empty for none.</returns>
    /// <exception cref="JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static List<string> For(string input, string? projectFolder)
    {
        var (_, prompt) = HookInput.Field(input, "prompt");
        if (string.IsNullOrWhiteSpace(prompt) || prompt.TrimStart().StartsWith('/'))
            return [];
        var (_, cwd) = HookInput.Field(input, "cwd");
        string? folder = new[] { cwd, projectFolder }.FirstOrDefault(f => !string.IsNullOrEmpty(f) && DmcFolder.IsDmcFolder(f));
        if (folder is null || CanonReminder.ActiveCampaign(folder) is not { } campaign)
            return [];
        string root = Path.Combine(folder, "adventures", campaign);
        if (Campaign.ClosedCampaign.IsClosed(root) || !RecordFiles.Has(root))
            return [];
        var index = RecordIndex.Build(folder, campaign);
        if (index.Records.Count == 0)
            return [];
        string gameState = Path.Combine(root, "game-state.md");
        string presentText = File.Exists(gameState) ? ReadSet.PresentText(Measurement.ReadLines(gameState)) : "";
        var (_, log) = HookInput.Field(input, "transcript_path");
        var shown = string.IsNullOrEmpty(log) ? new HashSet<string>(StringComparer.Ordinal) : Shown(Commands.ContextCheckCommand.ExpandUser(log, OperatingSystem.IsWindows()));
        return Lines(index, prompt, presentText, shown);
    }

    /// <summary>
    /// The persons who are present: each record whose person the Present text names in full, or by a name row's word.
    /// Not by a surname alone, as the sheet lines match a title: a family shares one, and a person the text does not name
    /// would be shown as present.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="presentText">The Present line and Combat tracker (<see cref="ReadSet.PresentText"/>).</param>
    internal static List<string> Present(RecordIndex index, string presentText)
    {
        if (presentText.Trim().Length == 0)
            return [];
        var present = new List<(string Person, int At)>();
        foreach (var record in index.Records)
        {
            int at = ReadSet.WholeWordAt(presentText, record.Person);
            foreach (var row in record.Rows.Where(r => r.Kind == RowKind.Name && !string.IsNullOrWhiteSpace(r.Word)))
            {
                int w = ReadSet.WholeWordAt(presentText, row.Word!.Trim());
                if (w >= 0 && (at < 0 || w < at))
                    at = w;
            }
            if (at >= 0)
                present.Add((record.Person, at));
        }
        return present.OrderBy(p => p.At).Select(p => p.Person).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The key of a scene: the first 8 hex digits of the SHA-256 of its Present text, so a change in who is present makes a new one.</summary>
    /// <param name="presentText">The Present line and Combat tracker.</param>
    internal static string SceneKey(string presentText) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(presentText.Trim())))[..8];

    /// <summary>
    /// The part's lines: a word line for each row word in the message; then, when anyone present or named has not been
    /// shown in this scene (<paramref name="shown"/> holds <c>key|person</c>), the header and their blocks while the
    /// whole fits in <see cref="CapBytes"/>. A person whose block does not fit is left out, header and block.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="prompt">The player's message.</param>
    /// <param name="presentText">The Present text.</param>
    /// <param name="shown">The <c>key|person</c> pairs the log's current part already shows.</param>
    internal static List<string> Lines(RecordIndex index, string prompt, string presentText, IReadOnlySet<string> shown)
    {
        var output = new List<string>();
        int used = 0;
        foreach (var found in index.WordsIn(prompt))
        {
            string line = WordLine(found);
            if (used + Bytes(line) > CapBytes)
                break;
            output.Add(line);
            used += Bytes(line);
        }

        var present = Present(index, presentText);
        var scene = present.Concat(index.Named(prompt)).Distinct(StringComparer.Ordinal).ToList();
        string key = SceneKey(presentText);
        var due = scene.Where(p => !shown.Contains(key + "|" + p)).ToList();
        var included = new List<string>();
        var blocks = new List<string>();
        used += Bytes(HeaderStart + key + HeaderMiddle);
        foreach (string person in due)
        {
            var block = Block(index, person, scene);
            if (block.Count == 0)
                continue;
            int cost = block.Sum(Bytes) + Encoding.UTF8.GetByteCount(person) + (included.Count > 0 ? NameSeparator.Length : 0);
            if (used + cost > CapBytes)
                continue;
            included.Add(person);
            blocks.AddRange(block);
            used += cost;
        }
        if (included.Count > 0)
        {
            output.Add(HeaderStart + key + HeaderMiddle + string.Join(NameSeparator, included));
            output.AddRange(blocks);
        }
        return output;
    }

    /// <summary>A word's line: every row of it, the persons it means and who uses it for each.</summary>
    /// <param name="found">The word and its rows.</param>
    internal static string WordLine(RecordIndex.Found found)
    {
        var parts = new List<string>();
        foreach (var group in found.Rows.GroupBy(r => (r.Person, r.Row.When, Never: string.Join(", ", r.Row.Never ?? []))))
        {
            var speakers = group.Select(r => r.Row.From).OfType<string>().Select(Speaker.Shown).Distinct(StringComparer.Ordinal).ToList();
            string who = group.Key.Person;
            string part = speakers.Count == 1 && speakers[0] == who ? $"{who} (their own word)" : $"{who} to {string.Join(" and ", speakers)}";
            if (group.Key.When is { Length: > 0 } when)
                part += $" ({when})";
            if (group.Key.Never.Length > 0)
                part += $", never {group.Key.Never}";
            parts.Add(part);
        }
        return $"\"{found.Word}\": {string.Join("; ", parts)}";
    }

    /// <summary>
    /// A person's block: what they say for the others in the scene, what they are called and by whom, their
    /// relationships, and their limits; empty when the record says none of these.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="person">The person.</param>
    /// <param name="scene">Everyone present or named.</param>
    internal static List<string> Block(RecordIndex index, string person, IReadOnlyList<string> scene)
    {
        var lines = new List<string>();
        var says = index.BySpeaker(person).Where(e => e.Person != person && scene.Contains(e.Person) && e.Row.Kind == RowKind.Usage)
            .Select(e => $"\"{e.Row.Word}\" for {e.Person}{When(e.Row)}").ToList();
        var nevers = index.Rows.Where(e => scene.Contains(e.Person) && (e.Row.Never ?? []).Contains(person))
            .Select(e => $"never \"{e.Row.Word}\" for {e.Person}").ToList();
        if (says.Count + nevers.Count > 0)
            lines.Add($"- {person} says: {string.Join("; ", says.Concat(nevers))}");
        if (index.Record(person) is not { } record)
            return lines;
        var called = record.Rows.Where(r => r.Kind != RowKind.Relationship && !string.IsNullOrWhiteSpace(r.Word))
            .Select(r => $"\"{r.Word}\" ({Speaker.Shown(r.From ?? "?")}{When(r)}{Never(r)})").ToList();
        if (called.Count > 0)
            lines.Add($"- {person}, called: {string.Join("; ", called)}");
        var relationships = record.Rows.Where(r => r.Kind == RowKind.Relationship && r.From is not null && r.Word is not null)
            .Select(r => $"{r.From}'s {r.Word}")
            .Concat(index.BySpeaker(person).Where(e => e.Row.Kind == RowKind.Relationship).Select(e => $"{e.Person} is their {e.Row.Word}"))
            .ToList();
        if (relationships.Count > 0)
            lines.Add($"- {person}: {string.Join("; ", relationships)}");
        var limits = record.Limits.Where(l => !string.IsNullOrWhiteSpace(l.Text)).Select(l => $"{l.Text!.Trim()} ({l.Source})").ToList();
        if (limits.Count > 0)
            lines.Add($"- {person}, limits: {string.Join(" ", limits)}");
        return lines;
    }

    /// <summary>A row's note on when, or nothing.</summary>
    /// <param name="row">The row.</param>
    static string When(Row row) => row.When is { Length: > 0 } w ? $", {w}" : "";

    /// <summary>A row's never list, or nothing.</summary>
    /// <param name="row">The row.</param>
    static string Never(Row row) => row.Never is { Count: > 0 } n ? $"; never {string.Join(", ", n.Select(Speaker.Shown))}" : "";

    /// <summary>A line's bytes with its line break.</summary>
    /// <param name="line">The line.</param>
    static int Bytes(string line) => Encoding.UTF8.GetByteCount(line) + 1;

    /// <summary>
    /// The <c>key|person</c> pairs every header of this part in the log's current part names: its headers after the
    /// latest compaction boundary. A missing or unreadable log has none. Only the lines that could be a boundary or
    /// hold a header are parsed, as <see cref="CanonReminder.Reminded"/> reads its own.
    /// </summary>
    /// <param name="log">The session's log, a JSON-lines file.</param>
    internal static HashSet<string> Shown(string log)
    {
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(log))
            return pairs;
        foreach (string line in File.ReadLines(log))
        {
            bool boundary = line.Contains("compact_boundary", StringComparison.Ordinal);
            if (!boundary && !line.Contains(HeaderStart, StringComparison.Ordinal))
                continue;
            JsonDocument record;
            try
            {
                record = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }
            using (record)
            {
                var root = record.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    continue;
                if (boundary && RecordFiles.String(root, "type") == "system" && RecordFiles.String(root, "subtype") == "compact_boundary")
                {
                    pairs.Clear();
                    continue;
                }
                if (RecordFiles.String(root, "type") != "attachment" || !root.TryGetProperty("attachment", out var attachment)
                    || attachment.ValueKind != JsonValueKind.Object || RecordFiles.String(attachment, "hookEvent") != "UserPromptSubmit")
                    continue;
                foreach (string text in (RecordFiles.String(attachment, "content") ?? "").Split('\n'))
                {
                    if (!text.StartsWith(HeaderStart, StringComparison.Ordinal))
                        continue;
                    int middle = text.IndexOf(HeaderMiddle, StringComparison.Ordinal);
                    if (middle < 0)
                        continue;
                    string key = text[HeaderStart.Length..middle];
                    foreach (string name in text[(middle + HeaderMiddle.Length)..].TrimEnd('\r').Split(NameSeparator))
                        pairs.Add(key + "|" + name);
                }
            }
        }
        return pairs;
    }
}
