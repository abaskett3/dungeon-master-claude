using System.Text;
using System.Text.Json;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>
/// The hook's canon reminder (spec 013 FR-015 to FR-017, research R5, contracts/context-check-canon.md): for the people
/// of the register a player's message names, and not yet reminded in this session part, the best few canon lines that the
/// resume did not read whole, so DMC has them before it answers. <c>dmc context-check</c> prints it after its break line.
/// Reads only, stores nothing: what was already reminded is read back from the session's log.
/// </summary>
internal static class CanonReminder
{
    /// <summary>The most lines printed for one person.</summary>
    public const int LinesPerPerson = 3;

    /// <summary>The most bytes the reminder may print, its header included: about 1,000 estimated tokens.</summary>
    public const int CapBytes = 4000;

    /// <summary>The start of the reminder's header; the log is searched for it.</summary>
    public const string HeaderStart = "Canon on ";

    /// <summary>The end of the header, after the people's names.</summary>
    public const string HeaderEnd = " (a reminder of some lines, not all canon says; recall for more):";

    /// <summary>The separator between the people's names in the header.</summary>
    const string NameSeparator = "; ";

    /// <summary>
    /// The reminder for a hook's input: nothing when the input has no prompt or folder, the prompt is a command, there is
    /// no active open campaign with a canon, or no person of the register is named who was not reminded since the log's
    /// latest compaction boundary.
    /// </summary>
    /// <param name="input">The hook's JSON (<c>prompt</c>, <c>cwd</c>, <c>transcript_path</c>).</param>
    /// <param name="projectFolder">The <c>CLAUDE_PROJECT_DIR</c> the hook runs with, used when <c>cwd</c> is not a DMC folder; null for none.</param>
    /// <param name="alsoNamed">
    /// People counted as named although the message does not name them: those present whose sheets hold none of the
    /// lines the sheet part prints (<see cref="SheetReminder"/>, spec 016 FR-008), so canon covers them instead. Null for none.
    /// </param>
    /// <returns>The lines to print, the header first; empty for none.</returns>
    /// <exception cref="JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static List<string> For(string input, string? projectFolder, IReadOnlyList<string>? alsoNamed = null)
    {
        var (_, prompt) = HookInput.Field(input, "prompt");
        if (string.IsNullOrWhiteSpace(prompt) || prompt.TrimStart().StartsWith('/'))
            return [];
        var (_, cwd) = HookInput.Field(input, "cwd");
        string? folder = new[] { cwd, projectFolder }.FirstOrDefault(f => !string.IsNullOrEmpty(f) && DmcFolder.IsDmcFolder(f));
        if (folder is null || ActiveCampaign(folder) is not { } campaign)
            return [];
        string root = Path.Combine(folder, "adventures", campaign);
        string canonPath = Path.Combine(root, "story", "canon.md");
        if (Campaign.ClosedCampaign.IsClosed(root) || !File.Exists(canonPath))
            return [];

        var canon = Measurement.ReadLines(canonPath);
        string namedText = alsoNamed is { Count: > 0 } ? prompt + "\n" + string.Join("; ", alsoNamed) : prompt;
        var named = Register.Named(namedText, Register.Of(canon));
        if (named.Count == 0)
            return [];
        var (_, log) = HookInput.Field(input, "transcript_path");
        var reminded = string.IsNullOrEmpty(log) ? [] : Reminded(Commands.ContextCheckCommand.ExpandUser(log, OperatingSystem.IsWindows()));
        named = named.Where(p => !reminded.Contains(p.Key)).ToList();
        if (named.Count == 0)
            return [];

        // A canon under CanonWholeBelow is read whole at resume (FR-015: only lines the resume did not read whole), so only its archive is left.
        bool small = canon.Sum(l => (long)l.Bytes) / 4 < Budgets.CanonWholeBelow;
        List<LineUnit> units = small ? [] : SceneRead.Units(root, campaign, canonPath, canon);
        units.AddRange(ArchiveUnits(root, campaign));
        units = Recall.Merge(units);
        return Lines(named, units);
    }

    /// <summary>
    /// The header and the lines for some people, in the order given: each person's best <see cref="LinesPerPerson"/> lines
    /// not already printed for another, while the whole fits in <see cref="CapBytes"/>. A person no line is found for is
    /// left out of the header, so a later message can try again.
    /// </summary>
    /// <param name="people">The people to remind, in the prompt's order.</param>
    /// <param name="units">The lines that may be printed: canon outside its always-read sections, then the archive.</param>
    internal static List<string> Lines(IReadOnlyList<Person> people, IReadOnlyList<LineUnit> units)
    {
        var printed = new HashSet<LineUnit>();
        var shown = new List<(Person Person, List<string> Lines)>();
        int used = Recall.Bytes(HeaderStart + HeaderEnd);
        foreach (var person in people)
        {
            var wanted = Words.Stems(Register.QueryText(person)).ToHashSet(StringComparer.Ordinal);
            if (wanted.Count == 0)
                continue;
            var lines = new List<string>();
            int nameBytes = Encoding.UTF8.GetByteCount(person.Key) + (shown.Count > 0 ? NameSeparator.Length : 0);
            foreach (var unit in Recall.Rank(units, wanted).Where(u => !printed.Contains(u)).Take(LinesPerPerson))
            {
                string line = Recall.Hit(unit, wanted);
                int cost = Recall.Bytes(line) + (lines.Count == 0 ? nameBytes : 0);
                if (used + cost > CapBytes)
                    break;
                lines.Add(line);
                printed.Add(unit);
                used += cost;
            }
            if (lines.Count > 0)
                shown.Add((person, lines));
        }
        if (shown.Count == 0)
            return [];
        var output = new List<string> { HeaderStart + string.Join(NameSeparator, shown.Select(s => s.Person.Key)) + HeaderEnd };
        output.AddRange(shown.SelectMany(s => s.Lines));
        return output;
    }

    /// <summary>The line units of <c>story/canon-archive.md</c>, labelled as recall labels them; none when there is no archive.</summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    static List<LineUnit> ArchiveUnits(string root, string campaign)
    {
        string path = Path.Combine(root, "story", "canon-archive.md");
        if (!File.Exists(path))
            return [];
        var file = new ScopeFile(new Place(campaign, root, false, 0), "story/canon-archive.md", Tier.Archive, RecallScope.WeightOf(Tier.Archive));
        return LineUnits.Of(file, MarkdownFile.Read(path));
    }

    /// <summary>
    /// The campaign <c>adventures/.active-campaign</c> names, read as <c>dmc size</c> reads it (<see cref="SizeReport.ActiveName"/>,
    /// its first line), when it is a plain folder name that exists; null otherwise.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    internal static string? ActiveCampaign(string folder)
    {
        if (SizeReport.ActiveName(Path.Combine(folder, "adventures")) is not { } name)
            return null;
        bool plain = !name.StartsWith('.') && name.IndexOfAny(['/', '\\', ':']) < 0;
        return plain && Directory.Exists(Path.Combine(folder, "adventures", name)) ? name : null;
    }

    /// <summary>
    /// The people already reminded in the log's current part: the names in every reminder header the hook printed after
    /// the latest compaction boundary. A missing or unreadable log has none. Only the lines that could be a boundary or
    /// hold a header are parsed.
    /// </summary>
    /// <param name="log">The session's log, a JSON-lines file.</param>
    internal static HashSet<string> Reminded(string log)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(log))
            return names;
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
                if (boundary && String(root, "type") == "system" && String(root, "subtype") == "compact_boundary")
                {
                    names.Clear();
                    continue;
                }
                if (String(root, "type") != "attachment" || !root.TryGetProperty("attachment", out var attachment)
                    || attachment.ValueKind != JsonValueKind.Object || String(attachment, "hookEvent") != "UserPromptSubmit")
                    continue;
                foreach (string text in (String(attachment, "content") ?? "").Split('\n'))
                {
                    if (text.StartsWith(HeaderStart, StringComparison.Ordinal) && text.EndsWith(HeaderEnd, StringComparison.Ordinal))
                        names.UnionWith(text[HeaderStart.Length..^HeaderEnd.Length].Split(NameSeparator));
                }
            }
        }
        return names;
    }

    /// <summary>A string property of a JSON object, or null.</summary>
    /// <param name="element">The object.</param>
    /// <param name="name">The property's name.</param>
    static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
