using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
internal static partial class RowsReminder
{
    /// <summary>
    /// The part's own cap: word lines first, then the rest of each block while it fits. Every person's "says" line comes
    /// before the rest and may take the part past it, up to the room the hook's whole output has left (<see cref="Lines"/>).
    /// </summary>
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
    /// <param name="room">The most bytes the part may print with the "says" lines (<see cref="Lines"/>): what the hook's whole output has left.</param>
    /// <returns>The lines to print; empty for none.</returns>
    /// <exception cref="JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static List<string> For(string input, string? projectFolder, int room = CapBytes)
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
        return Lines(index, prompt, presentText, shown, room);
    }

    /// <summary>
    /// The persons who are present: those the Present text names as a message names them (<see cref="RecordIndex.Named"/>:
    /// in full, or by a row word whose rows all mean that one person), with any text in brackets left out, as play writes
    /// notes on who is away there ("(Benno at the mill)"). Not by a surname alone: a family shares one. Found by
    /// SC-003's run (2026-11-27): by name rows only, a person whose short name is only others' word for them was
    /// never present, and the persons in the note always were.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="presentText">The Present line and Combat tracker (<see cref="ReadSet.PresentText"/>).</param>
    internal static List<string> Present(RecordIndex index, string presentText)
    {
        string text = presentText;
        for (string before = ""; before != text;)
        {
            before = text;
            text = Bracketed().Replace(text, " ");
        }
        return text.Trim().Length == 0 ? [] : index.Named(text);
    }

    /// <summary>Text in round brackets with no bracket inside; applied until nothing changes, it takes nested ones from the inside out.</summary>
    [GeneratedRegex(@"\([^()]*\)")]
    private static partial Regex Bracketed();

    /// <summary>
    /// The key of a scene: the first 8 hex digits of the SHA-256 of who is present, in name order, so a change in who is
    /// present makes a new one and nothing else does: not an HP or initiative change in the Combat tracker, nor a note
    /// (T056; the whole Present text made every round of a fight a new scene).
    /// </summary>
    /// <param name="present">The persons present (<see cref="Present"/>).</param>
    internal static string SceneKey(IEnumerable<string> present) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", present.Order(StringComparer.Ordinal)))))[..8];

    /// <summary>
    /// The part's lines: a word line for each row word in the message; then, when anyone present or named has not been
    /// shown in this scene (<paramref name="shown"/> holds <c>key|person</c>), the header and their blocks, in three
    /// passes: first every person's "says" line, what they call the others here (short, and what keeps a speaker's
    /// words right), then every person's limits line (US3, T057), each line on its own within <paramref name="room"/>,
    /// so one person's long limits never keep out another's words; then the rest of each block (what they are called,
    /// their relationships), whole, while the part stays within <see cref="CapBytes"/>. Each person's
    /// lines print in their block's order. In one pass, within the part's cap, a long block left no room for those
    /// after it: in SC-003's run (2026-11-27) one person's block, most of it what others call them, crowded out the main
    /// risk speaker's in every treatment run. A person is named in the header when any line of theirs is printed, so a
    /// person whose rest did not fit is not shown it again in this scene; one with no line printed is not named.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="prompt">The player's message.</param>
    /// <param name="presentText">The Present text.</param>
    /// <param name="shown">The <c>key|person</c> pairs the log's current part already shows.</param>
    /// <param name="room">The most bytes the part may print with the "says" and limits lines; the word lines and the rest of the blocks stay within the smaller of it and <see cref="CapBytes"/>.</param>
    internal static List<string> Lines(RecordIndex index, string prompt, string presentText, IReadOnlySet<string> shown, int room = CapBytes)
    {
        var output = new List<string>();
        int cap = Math.Min(CapBytes, room);
        int used = 0;
        foreach (var found in index.WordsIn(prompt))
        {
            string line = WordLine(found);
            if (used + Bytes(line) > cap)
                break;
            output.Add(line);
            used += Bytes(line);
        }

        var present = Present(index, presentText);
        var scene = present.Concat(index.Named(prompt)).Distinct(StringComparer.Ordinal).ToList();
        string key = SceneKey(present);
        var due = scene.Where(p => !shown.Contains(key + "|" + p)).ToList();
        var blocks = due.Select(p => (Person: p, Lines: Block(index, p, scene))).Where(b => b.Lines.Count > 0).ToList();
        var printed = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        used += Bytes(HeaderStart + key + HeaderMiddle);

        // Adds a person's lines when they fit within the limit with the person's name in the header, if it is not there yet.
        void Add(string person, List<string> lines, int limit)
        {
            if (lines.Count == 0)
                return;
            int name = printed.ContainsKey(person) ? 0 : Encoding.UTF8.GetByteCount(person) + (printed.Count > 0 ? NameSeparator.Length : 0);
            int cost = lines.Sum(Bytes) + name;
            if (used + cost > limit)
                return;
            if (!printed.TryGetValue(person, out var set))
                printed[person] = set = new HashSet<string>(StringComparer.Ordinal);
            set.UnionWith(lines);
            used += cost;
        }

        // Three passes, each line on its own: every "says" line, then every limits line, within the room; then the rest.
        foreach (var (person, lines) in blocks)
            Add(person, lines.Where(l => l.StartsWith(SaysStart(person), StringComparison.Ordinal)).ToList(), room);
        foreach (var (person, lines) in blocks)
            Add(person, lines.Where(l => l.StartsWith(LimitsStart(person), StringComparison.Ordinal)).ToList(), room);
        foreach (var (person, lines) in blocks)
            Add(person, lines.Where(l => !IsFirst(person, l)).ToList(), cap);
        var included = due.Where(printed.ContainsKey).ToList();
        if (included.Count > 0)
        {
            output.Add(HeaderStart + key + HeaderMiddle + string.Join(NameSeparator, included));
            foreach (var (person, lines) in blocks.Where(b => printed.ContainsKey(b.Person)))
                output.AddRange(lines.Where(printed[person].Contains));
        }
        return output;
    }

    /// <summary>Whether a line of a person's block is their "says" line or their limits line, which the first two passes place and the third leaves out.</summary>
    /// <param name="person">The person.</param>
    /// <param name="line">The line.</param>
    static bool IsFirst(string person, string line) =>
        line.StartsWith(SaysStart(person), StringComparison.Ordinal) || line.StartsWith(LimitsStart(person), StringComparison.Ordinal);

    /// <summary>The start of a person's "says" line in their block.</summary>
    /// <param name="person">The person.</param>
    static string SaysStart(string person) => $"- {person} says: ";

    /// <summary>The start of a person's limits line in their block.</summary>
    /// <param name="person">The person.</param>
    static string LimitsStart(string person) => $"- {person}, limits: ";

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
            lines.Add(SaysStart(person) + string.Join("; ", says.Concat(nevers)));
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
            lines.Add(LimitsStart(person) + string.Join(" ", limits));
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
