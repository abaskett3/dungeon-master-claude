using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.ContextCheck;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>
/// The hook's sheet lines (spec 016 FR-008, research R4, contracts/context-check-sheets.md): for each person the Scene
/// block's Present line or the Combat tracker names who has a sheet, the lines of that sheet that say what is true of
/// them now (looks, where they are, how they move, what they can and cannot do, what they carry), so DMC has them in
/// front of it before it answers. A person is shown again only once <see cref="Refresh"/> tokens of context have passed
/// since their lines were last shown or their sheet was read. <c>dmc context-check</c> prints it after the canon
/// reminder. Reads only, stores nothing: what was shown and read is read back from the session's log.
/// </summary>
internal static partial class SheetReminder
{
    /// <summary>The most bytes the sheet lines may print, their header included: about 2,500 estimated tokens.</summary>
    public const int CapBytes = 10_000;

    /// <summary>The tokens of context after which a person shown or read is shown again.</summary>
    public const long Refresh = 100_000;

    /// <summary>The start of the header; the log is searched for it.</summary>
    public const string HeaderStart = "Sheets of ";

    /// <summary>The end of the header, after the people's names.</summary>
    public const string HeaderEnd = " (what their sheets say now; read the sheet for more):";

    /// <summary>The separator between the people's names in the header.</summary>
    const string NameSeparator = "; ";

    /// <summary>The folder of the sheets in a campaign.</summary>
    const string SheetsFolder = "character-sheets";

    /// <summary>
    /// The fields printed, in order; each is the labels that may hold it, the play card's first and the older shape's
    /// second. The first line of the sheet with any of a field's labels is the one printed.
    /// </summary>
    internal static readonly string[][] Fields =
    [
        ["Looks", "Appearance"],
        ["Usually found"],
        ["AC, max HP, speed, saves, senses", "Speed"],
        ["Standing traits that change play", "Traits"],
        ["Items used in play"],
    ];

    /// <summary>An HTML comment on one line.</summary>
    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex InlineComment();

    /// <summary>What the sheet part found: the lines to print, and the people present whose sheets hold none of the fields.</summary>
    /// <param name="Lines">The header and the lines; empty for none.</param>
    /// <param name="WithoutFields">The titles of the present people whose sheets hold none of the fields, for the canon reminder to cover.</param>
    internal readonly record struct Found(List<string> Lines, List<string> WithoutFields)
    {
        /// <summary>Nothing to print and no one to hand on.</summary>
        public static Found None => new([], []);
    }

    /// <summary>One present person's sheet.</summary>
    /// <param name="File">The sheet's file name.</param>
    /// <param name="Title">Its <c># </c> title.</param>
    /// <param name="At">Where the Present text first names them, for the order.</param>
    /// <param name="Lines">The fields' lines found on it, in <see cref="Fields"/> order.</param>
    internal sealed record Person(string File, string Title, int At, List<string> Lines);

    /// <summary>
    /// The sheet lines for a hook's input: nothing when the input has no prompt or folder, the prompt is a command, the
    /// named log has no reply since its latest compaction boundary (the first message after a <c>/clear</c>, whose resume
    /// reads the sheets next; spec 019 FR-005), there is no active open campaign with a <c>game-state.md</c>, or no one
    /// present with a sheet is due.
    /// </summary>
    /// <param name="input">The hook's JSON (<c>prompt</c>, <c>cwd</c>, <c>transcript_path</c>).</param>
    /// <param name="projectFolder">The <c>CLAUDE_PROJECT_DIR</c> the hook runs with, used when <c>cwd</c> is not a DMC folder; null for none.</param>
    /// <param name="contextNow">The session's latest context size, in tokens. 0 with a log named means the log has no reply since its latest compaction boundary, and nothing is printed; 0 with no log named means it is not known.</param>
    /// <returns>The lines to print and the people to hand on to the canon reminder.</returns>
    /// <exception cref="JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static Found For(string input, string? projectFolder, long contextNow)
    {
        var (_, prompt) = HookInput.Field(input, "prompt");
        if (string.IsNullOrWhiteSpace(prompt) || prompt.TrimStart().StartsWith('/'))
            return Found.None;
        var (_, log) = HookInput.Field(input, "transcript_path");
        if (!string.IsNullOrEmpty(log) && contextNow == 0)
            return Found.None;
        var (_, cwd) = HookInput.Field(input, "cwd");
        string? folder = new[] { cwd, projectFolder }.FirstOrDefault(f => !string.IsNullOrEmpty(f) && DmcFolder.IsDmcFolder(f));
        if (folder is null || CanonReminder.ActiveCampaign(folder) is not { } campaign)
            return Found.None;
        string root = Path.Combine(folder, "adventures", campaign);
        string gameState = Path.Combine(root, "game-state.md");
        if (Campaign.ClosedCampaign.IsClosed(root) || !File.Exists(gameState))
            return Found.None;

        var present = Present(root, ReadSet.PresentText(Measurement.ReadLines(gameState)));
        if (present.Count == 0)
            return Found.None;
        var without = present.Where(p => p.Lines.Count == 0).Select(p => p.Title).ToList();
        var due = present.Where(p => p.Lines.Count > 0).ToList();
        if (due.Count > 0 && !string.IsNullOrEmpty(log))
        {
            var seen = Seen(Commands.ContextCheckCommand.ExpandUser(log, OperatingSystem.IsWindows()), contextNow);
            due = due.Where(p => !Recent(seen, p, contextNow)).ToList();
        }
        return new Found(Lines(due), without);
    }

    /// <summary>
    /// The sheets of the people a Present text names, in the order it names them: every <c>character-sheets/*.md</c>
    /// except <c>minor-npcs.md</c>, the <c>*-history.md</c> files and a PC's, matched by <see cref="ReadSet.MentionAt"/>
    /// as the resume matches them. A crowd or a name with no sheet matches nothing.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="presentText">The Present line and Combat tracker (<see cref="ReadSet.PresentText"/>).</param>
    internal static List<Person> Present(string root, string presentText)
    {
        string folder = Path.Combine(root, SheetsFolder);
        if (presentText.Trim().Length == 0 || !Directory.Exists(folder))
            return [];
        var people = new List<Person>();
        foreach (string file in Directory.EnumerateFiles(folder, "*.md").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal))
        {
            if (file == "minor-npcs.md" || file.EndsWith("-history.md", StringComparison.Ordinal) || file.StartsWith("pc-", StringComparison.Ordinal))
                continue;
            var lines = Measurement.ReadLines(Path.Combine(folder, file));
            string title = lines.FirstOrDefault(l => l.Text.StartsWith("# ", StringComparison.Ordinal)).Text is { } t ? t[2..].Trim() : Path.GetFileNameWithoutExtension(file);
            int at = ReadSet.MentionAt(presentText, title);
            if (at >= 0)
                people.Add(new Person(file, title, at, FieldLines(lines)));
        }
        return people.OrderBy(p => p.At).ThenBy(p => p.File, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The lines of a sheet that hold the <see cref="Fields"/>, one per field, each the first line with any of its labels
    /// (outside HTML comments) that has text after the label. A field the sheet lacks is left out.
    /// </summary>
    /// <param name="lines">The sheet's lines.</param>
    internal static List<string> FieldLines(IReadOnlyList<Line> lines)
    {
        var found = new List<string>();
        var outside = ReadSet.OutsideComments(lines).ToList();
        foreach (var labels in Fields)
        {
            string? line = outside.FirstOrDefault(l => labels.Any(label => HasValue(l, label)));
            if (line is not null)
                found.Add(line.TrimEnd());
        }
        return found;
    }

    /// <summary>Whether a line is a field line with this label and some text after the label's colon.</summary>
    /// <param name="line">A sheet line, comments removed.</param>
    /// <param name="label">The field's label, as written between <c>**</c>.</param>
    static bool HasValue(string line, string label)
    {
        string start = $"- **{label}**";
        if (!line.StartsWith(start, StringComparison.Ordinal))
            return false;
        int colon = line.IndexOf(':', start.Length);
        return colon >= 0 && InlineComment().Replace(line[(colon + 1)..], "").Trim().Length > 0;
    }

    /// <summary>
    /// The header and the lines for some people, in the order given, while the whole fits in <see cref="CapBytes"/>. A
    /// person whose lines do not all fit is left out, header and lines, so a later message tries again.
    /// </summary>
    /// <param name="people">The people due, in the Present text's order.</param>
    internal static List<string> Lines(IReadOnlyList<Person> people)
    {
        var shown = new List<Person>();
        var body = new List<string>();
        int used = Recall.Bytes(HeaderStart + HeaderEnd);
        foreach (var person in people)
        {
            var lines = person.Lines.Select(l => $"{SheetsFolder}/{person.File} | {l}").ToList();
            int cost = lines.Sum(Recall.Bytes) + Encoding.UTF8.GetByteCount(person.Title) + (shown.Count > 0 ? NameSeparator.Length : 0);
            if (used + cost > CapBytes)
                continue;
            shown.Add(person);
            body.AddRange(lines);
            used += cost;
        }
        if (shown.Count == 0)
            return [];
        var output = new List<string> { HeaderStart + string.Join(NameSeparator, shown.Select(p => p.Title)) + HeaderEnd };
        output.AddRange(body);
        return output;
    }

    /// <summary>When each person was last shown and each sheet last read, in the log's current part.</summary>
    /// <param name="Shown">The context, in tokens, at which each title was last named in a header this hook printed.</param>
    /// <param name="Read">The context at which each sheet file name (lower case) was last opened by a Read tool call.</param>
    internal sealed record SeenInLog(Dictionary<string, long> Shown, Dictionary<string, long> Read);

    /// <summary>Whether a person was shown, or their sheet read, within <see cref="Refresh"/> tokens of now.</summary>
    /// <param name="seen">What the log holds.</param>
    /// <param name="person">The person.</param>
    /// <param name="contextNow">The latest context; 0 when not known, which counts anything seen as recent. <see cref="For"/> never passes 0, since it prints nothing then (spec 019).</param>
    static bool Recent(SeenInLog seen, Person person, long contextNow)
    {
        bool Near(long at) => contextNow == 0 || contextNow - at < Refresh;
        return (seen.Shown.TryGetValue(person.Title, out long shown) && Near(shown))
               || (seen.Read.TryGetValue(person.File.ToLowerInvariant(), out long read) && Near(read));
    }

    /// <summary>
    /// What the log holds of the last <see cref="Refresh"/> tokens after its latest compaction boundary: each sheet-lines
    /// header this hook printed, at the context of the last call before it, and each Read tool call of a
    /// <c>character-sheets/</c> file, at the context of the call that made it. The log is read from the end, with the
    /// break line's reader (<see cref="TranscriptReader"/>), and the read stops at the first call more than
    /// <see cref="Refresh"/> below <paramref name="contextNow"/>, since nothing older can be recent, or at a compaction
    /// boundary: about 100,000 tokens of log, not the whole of it (the code review of PR #230 measured the whole-log scan
    /// at 214 ms on a 6.8 MB log). Only lines that could be a call, a boundary or a header are classified, and only a call
    /// that reads a sheet, or a header, is parsed further. A missing log holds nothing. Should the context fall and rise
    /// again within one part, the read stops early and a person is shown sooner, never later.
    /// </summary>
    /// <param name="log">The session's log, a JSON-lines file.</param>
    /// <param name="contextNow">The latest context, in tokens; 0 when not known, and then the whole part is read. <see cref="For"/> never passes 0, since it prints nothing then (spec 019).</param>
    internal static SeenInLog Seen(string log, long contextNow)
    {
        var seen = new SeenInLog(new Dictionary<string, long>(StringComparer.Ordinal), new Dictionary<string, long>(StringComparer.Ordinal));
        if (!File.Exists(log))
            return seen;
        long limit = contextNow - Refresh;
        var pending = new List<string>(); // header names waiting for the call before them, which comes later in this read

        // The latest of each is kept: read from the end, that is the first one met.
        void Settle(long at)
        {
            foreach (string title in pending)
                seen.Shown.TryAdd(title, at);
            pending.Clear();
        }

        TranscriptReader.ScanBackwards(log, line =>
        {
            var span = line.Span;
            bool header = span.IndexOf("Sheets of "u8) >= 0;
            if (!header && span.IndexOf("\"type\":\"assistant\""u8) < 0 && span.IndexOf("compact_boundary"u8) < 0)
                return false;
            Classified found;
            try
            {
                found = TranscriptReader.Classify(line);
            }
            catch (Exception e) when (e is InvalidDataException or FormatException or InvalidOperationException)
            {
                return false; // a record of a shape the reader does not know: skipped, as before
            }
            if (found.Kind == RecordKind.Compaction)
            {
                Settle(0);
                return true;
            }
            if (found.Kind == RecordKind.Assistant)
            {
                Settle(found.Context);
                if (span.IndexOf("character-sheets"u8) >= 0 && span.IndexOf("\"name\":\"Read\""u8) >= 0)
                    foreach (string file in SheetsRead(Encoding.UTF8.GetString(span)))
                        seen.Read.TryAdd(file, found.Context);
                return contextNow > 0 && found.Context < limit;
            }
            if (header)
                pending.AddRange(HeaderNames(Encoding.UTF8.GetString(span)));
            return false;
        });
        Settle(0); // headers with no call before them in the log
        return seen;
    }

    /// <summary>The file names (lower case) of the <c>character-sheets/</c> files an assistant record's Read tool calls open.</summary>
    /// <param name="line">One log line holding an assistant record.</param>
    static List<string> SheetsRead(string line)
    {
        var files = new List<string>();
        try
        {
            using var record = JsonDocument.Parse(line);
            if (!record.RootElement.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                return files;
            foreach (var block in content.EnumerateArray())
            {
                if (block.ValueKind != JsonValueKind.Object || String(block, "type") != "tool_use" || String(block, "name") != "Read"
                    || !block.TryGetProperty("input", out var toolInput) || toolInput.ValueKind != JsonValueKind.Object
                    || String(toolInput, "file_path") is not { } path)
                    continue;
                string normal = path.Replace('\\', '/');
                string? parent = Path.GetFileName(Path.GetDirectoryName(normal) ?? "");
                if (string.Equals(parent, SheetsFolder, StringComparison.OrdinalIgnoreCase))
                    files.Add(Path.GetFileName(normal).ToLowerInvariant());
            }
        }
        catch (JsonException)
        {
        }
        return files;
    }

    /// <summary>The names in every sheet-lines header of a log line that is the hook's own output.</summary>
    /// <param name="line">One log line.</param>
    static List<string> HeaderNames(string line)
    {
        var names = new List<string>();
        try
        {
            using var record = JsonDocument.Parse(line);
            var root = record.RootElement;
            if (root.ValueKind != JsonValueKind.Object || String(root, "type") != "attachment" || !root.TryGetProperty("attachment", out var attachment)
                || attachment.ValueKind != JsonValueKind.Object || String(attachment, "hookEvent") != "UserPromptSubmit")
                return names;
            foreach (string text in (String(attachment, "content") ?? "").Split('\n'))
                if (text.StartsWith(HeaderStart, StringComparison.Ordinal) && text.EndsWith(HeaderEnd, StringComparison.Ordinal))
                    names.AddRange(text[HeaderStart.Length..^HeaderEnd.Length].Split(NameSeparator));
        }
        catch (JsonException)
        {
        }
        return names;
    }

    /// <summary>A string property of a JSON object, or null.</summary>
    /// <param name="element">The object.</param>
    /// <param name="name">The property's name.</param>
    static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
