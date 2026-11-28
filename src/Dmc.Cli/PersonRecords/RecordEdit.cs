using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dmc.PersonRecords;

/// <summary>
/// The check after each edit (spec 032 FR-006 and FR-007 class 4, research R3, contracts/dmc-records.md § edited): what
/// <c>dmc records edited</c> decides for one <c>PostToolUse</c> hook input. A record edit is refused when any record of
/// the campaign breaks a rule, or when it adds a quotation of the player that is not in the player's typed messages; an
/// Edit of a campaign Markdown file is refused when it changes the text and credits the player in the same edit. A new
/// usage that gives one speaker one word for two persons stands, with a note. Anything else is let through.
/// </summary>
internal static partial class RecordEdit
{
    /// <summary>The outcome of one edit.</summary>
    /// <param name="Refusal">The one line for standard error, or null when the edit stands.</param>
    /// <param name="Note">The note for DMC's context, or null for none.</param>
    internal sealed record Outcome(string? Refusal, string? Note)
    {
        /// <summary>The edit stands, with nothing to say.</summary>
        public static Outcome Pass => new(null, null);
    }

    /// <summary>A credit to the player: the phrases DMC writes when it marks a line as the player's (#237, #242).</summary>
    [GeneratedRegex(@"\(the player\b|\bby the player\b|\bthe player's (?:words|wording|ruling|own)\b|\bplayer, (?:sheet audit|ruling)\b|\bthe player (?:ruled|said|chose|picked|wrote)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Credit();

    /// <summary>A bracket that holds a credit, removed whole when the text without credits is compared.</summary>
    [GeneratedRegex(@"\([^()]*\bplayer\b[^()]*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CreditBracket();

    /// <summary>Quotes and runs of white space, folded when a quotation is looked for.</summary>
    [GeneratedRegex(@"[\s""'“”‘’]+", RegexOptions.CultureInvariant)]
    private static partial Regex Fold();

    /// <summary>Decides one hook input.</summary>
    /// <param name="input">The hook's JSON.</param>
    /// <param name="projectFolder">The <c>CLAUDE_PROJECT_DIR</c> the hook runs with; null for none.</param>
    public static Outcome Decide(string input, string? projectFolder)
    {
        var (_, cwd) = HookInput.Field(input, "cwd");
        string? folder = new[] { cwd, projectFolder }.FirstOrDefault(f => !string.IsNullOrEmpty(f) && DmcFolder.IsDmcFolder(f));
        if (folder is null || Retrieval.CanonReminder.ActiveCampaign(folder) is not { } campaign)
            return Outcome.Pass;
        using var doc = JsonDocument.Parse(input);
        var root = doc.RootElement;
        if (!root.TryGetProperty("tool_input", out var tool) || tool.ValueKind != JsonValueKind.Object || RecordFiles.String(tool, "file_path") is not { } filePath)
            return Outcome.Pass;
        string campaignFolder = Path.GetFullPath(Path.Combine(folder, "adventures", campaign));
        string path = Path.GetFullPath(Path.IsPathRooted(filePath) ? filePath : Path.Combine(folder, filePath));
        if (!path.StartsWith(campaignFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return Outcome.Pass;
        string toolName = RecordFiles.String(root, "tool_name") ?? "";
        string? oldText = RecordFiles.String(tool, "old_string");
        string? newText = RecordFiles.String(tool, "new_string");
        string records = Path.Combine(campaignFolder, RecordFiles.Folder) + Path.DirectorySeparatorChar;

        if (path.StartsWith(records, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return Record(folder, campaign, campaignFolder, path, tool, toolName, root.TryGetProperty("tool_response", out var response) ? response : null, RecordFiles.String(root, "transcript_path"));
        if (toolName == "Edit" && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && oldText is not null && newText is not null
            && CreditsInEdit(oldText, newText))
            return new Outcome($"dmc records: {Relative(campaignFolder, path)}: this edit changes the text and credits the player in the same edit; credit only words the player wrote, quoted.", null);
        return Outcome.Pass;
    }

    /// <summary>A record edit: the campaign's records checked, the new quotations of the player looked for, and any new note.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="path">The record's full path.</param>
    /// <param name="tool">The hook's <c>tool_input</c>.</param>
    /// <param name="toolName">The tool's name.</param>
    /// <param name="response">The hook's <c>tool_response</c>, or null.</param>
    /// <param name="transcript">The session's log, or null.</param>
    static Outcome Record(string folder, string campaign, string campaignFolder, string path, JsonElement tool, string toolName, JsonElement? response, string? transcript)
    {
        var loaded = RecordFiles.Read(campaignFolder);
        // A person with a sheet and no record yet is a note here, not a refusal (T059), so two records that name each
        // other can be written one at a time; the full save's check still refuses the row until the record exists.
        var outcome = RecordRules.Check(SceneBook.For(folder, campaign, inPlay: true), loaded.Records, RecordCheck.SheetTitles(campaignFolder).ToList());
        var errors = loaded.Errors.Concat(outcome.Errors).ToList();
        if (errors.Count > 0)
            return new Outcome($"dmc records: {errors[0]}. Fix the record now, or put it back as it was.", null);

        string fileName = Path.GetFileName(path);
        string now = File.Exists(path) ? File.ReadAllText(path) : "";
        string? before = Before(response, tool, toolName, now);
        RecordFiles.Parse(campaign, fileName, before ?? "", out var previous);
        var current = loaded.Records.FirstOrDefault(r => r.FileName.Equals(fileName, StringComparison.Ordinal));

        var added = before is null ? CountedQuotations(current, tool, toolName) : NewQuotations(current, previous);
        if (added.Count > 0 && !string.IsNullOrEmpty(transcript))
        {
            string log = Commands.ContextCheckCommand.ExpandUser(transcript, OperatingSystem.IsWindows());
            if (File.Exists(log))
            {
                var said = PlayerMessages(log).Select(FoldText).ToList();
                foreach (var (row, words) in added)
                    if (!said.Any(m => m.Contains(FoldText(words), StringComparison.Ordinal)))
                        return new Outcome($"dmc records: {RecordFiles.Folder}/{fileName} {row}: the player's words \"{words}\" are not in this session's messages; quote them exactly, or use another source.", null);
            }
        }

        var others = loaded.Records.Where(r => !r.FileName.Equals(fileName, StringComparison.Ordinal)).ToList();
        var notesBefore = RecordRules.Notes(previous is null ? others : [.. others, previous]);
        // A note on a person with no record yet is given for the record just edited only, not again on every edit of another.
        string label = $"{RecordFiles.Folder}/{fileName} ";
        var fresh = outcome.Notes.Except(notesBefore, StringComparer.Ordinal)
            .Where(n => !RecordRules.IsWaitingNote(n) || n.StartsWith(label, StringComparison.Ordinal)).ToList();
        return fresh.Count > 0 ? new Outcome(null, "dmc records: " + string.Join(" ", fresh)) : Outcome.Pass;
    }

    /// <summary>
    /// The record's text before this edit, or null when it cannot be known: Claude Code's <c>tool_response</c>
    /// <c>originalFile</c> when the hook is given it (the play logs' tool results hold it for Edit and Write; that the
    /// hook's input does too is unverified), empty when that is null (a new file); else the edit undone, for an Edit or
    /// a MultiEdit whose new texts are not empty; empty for a Write (a new record); null for an edit that deletes text.
    /// </summary>
    /// <param name="response">The hook's <c>tool_response</c>, if any.</param>
    /// <param name="tool">The hook's <c>tool_input</c>.</param>
    /// <param name="toolName">The tool's name.</param>
    /// <param name="now">The file's text now.</param>
    static string? Before(JsonElement? response, JsonElement tool, string toolName, string now)
    {
        if (response is { ValueKind: JsonValueKind.Object } r && r.TryGetProperty("originalFile", out var original))
        {
            if (original.ValueKind == JsonValueKind.String)
                return original.GetString();
            if (original.ValueKind == JsonValueKind.Null)
                return "";
        }
        var edits = Edits(tool, toolName);
        if (edits is null)
            return toolName == "Write" ? "" : null;
        if (edits.Count == 0 || edits.Any(e => e.New.Length == 0))
            return null;
        string text = now;
        foreach (var (old, @new, all) in Enumerable.Reverse(edits))
            text = all ? text.Replace(@new, old, StringComparison.Ordinal) : ReplaceFirst(text, @new, old);
        return text;
    }

    /// <summary>An Edit's or a MultiEdit's edits, in order; null for another tool.</summary>
    /// <param name="tool">The hook's <c>tool_input</c>.</param>
    /// <param name="toolName">The tool's name.</param>
    static List<(string Old, string New, bool All)>? Edits(JsonElement tool, string toolName)
    {
        static (string, string, bool) One(JsonElement e) => (RecordFiles.String(e, "old_string") ?? "", RecordFiles.String(e, "new_string") ?? "",
            e.TryGetProperty("replace_all", out var all) && all.ValueKind == JsonValueKind.True);
        return toolName switch
        {
            "Edit" => [One(tool)],
            "MultiEdit" => tool.TryGetProperty("edits", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).Select(One).ToList()
                : [],
            _ => null,
        };
    }

    /// <summary>
    /// The quotations of the player this edit adds (T058), by their words, not by where they are, so a row that moves
    /// keeps its quotation and a quotation whose words the edit changed is new: each of the record's quotations whose
    /// words were not a quotation of the record before, counted as often as they occur.
    /// </summary>
    /// <param name="current">The record as it is now, or null.</param>
    /// <param name="previous">The record before the edit, or null for none.</param>
    static List<(string Where, string Words)> NewQuotations(PersonRecord? current, PersonRecord? previous)
    {
        var old = Quotations(previous).Select(q => FoldText(q.Words)).ToList();
        var added = new List<(string, string)>();
        foreach (var q in Quotations(current))
            if (!old.Remove(FoldText(q.Words)))
                added.Add(q);
        return added;
    }

    /// <summary>
    /// The quotations an edit adds when the record before it cannot be known (a deleting edit, with no
    /// <c>originalFile</c>): those whose words the edit's new texts hold more often than its old texts. A deletion
    /// inside a quotation's words is not caught.
    /// </summary>
    /// <param name="current">The record as it is now, or null.</param>
    /// <param name="tool">The hook's <c>tool_input</c>.</param>
    /// <param name="toolName">The tool's name.</param>
    static List<(string Where, string Words)> CountedQuotations(PersonRecord? current, JsonElement tool, string toolName)
    {
        var edits = Edits(tool, toolName) ?? [];
        string oldText = FoldText(string.Join(" ", edits.Select(e => JsonUnescape(e.Old))));
        string newText = FoldText(string.Join(" ", edits.Select(e => JsonUnescape(e.New))));
        return Quotations(current).Where(q => Count(newText, FoldText(q.Words)) > Count(oldText, FoldText(q.Words))).ToList();
    }

    /// <summary>How many times a text holds some words, not overlapping; none for empty words.</summary>
    /// <param name="text">The text.</param>
    /// <param name="words">The words.</param>
    static int Count(string text, string words)
    {
        if (words.Length == 0)
            return 0;
        int n = 0;
        for (int at = text.IndexOf(words, StringComparison.Ordinal); at >= 0; at = text.IndexOf(words, at + words.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    /// <summary>A piece of a JSON file's text with its string escapes undone (<c>\"</c>, <c>\\</c>, <c>\n</c>, <c>\u2019</c>), so it reads as the values do.</summary>
    /// <param name="text">The raw text, such as an Edit's <c>new_string</c>.</param>
    static string JsonUnescape(string text) => JsonEscape().Replace(text, m => m.Groups[1].Value switch
    {
        { Length: 5 } hex => ((char)Convert.ToInt32(hex[1..], 16)).ToString(),
        "n" or "r" or "t" => " ",
        var c => c,
    });

    /// <summary>A JSON string escape: a backslash and the character, or <c>u</c> and four hex digits.</summary>
    [GeneratedRegex(@"\\(u[0-9a-fA-F]{4}|.)", RegexOptions.CultureInvariant)]
    private static partial Regex JsonEscape();

    /// <summary>Each quotation of the player a record's rows and limits give as their source, with where it is.</summary>
    /// <param name="record">The record, or null for none.</param>
    static IEnumerable<(string Where, string Words)> Quotations(PersonRecord? record)
    {
        if (record is null)
            yield break;
        for (int i = 0; i < record.Rows.Count; i++)
            if (record.Rows[i].Source is { } s && RecordSource.PlayerWords(s) is { } w)
                yield return ($"row {i + 1}", w);
        for (int i = 0; i < record.Limits.Count; i++)
            if (record.Limits[i].Source is { } s && RecordSource.PlayerWords(s) is { } w)
                yield return ($"limit {i + 1}", w);
    }

    /// <summary>Whether an Edit both changes the text and adds a credit to the player.</summary>
    /// <param name="oldText">The text replaced.</param>
    /// <param name="newText">The text it became.</param>
    internal static bool CreditsInEdit(string oldText, string newText)
    {
        if (Credit().Matches(newText).Count <= Credit().Matches(oldText).Count)
            return false;
        static string Bare(string text) => Fold().Replace(Credit().Replace(CreditBracket().Replace(text, ""), ""), " ").Trim();
        return !string.Equals(Bare(oldText), Bare(newText), StringComparison.Ordinal);
    }

    /// <summary>The player's typed messages in a session's log: every user record whose text the player wrote.</summary>
    /// <param name="log">The session's log, a JSON-lines file.</param>
    internal static IEnumerable<string> PlayerMessages(string log)
    {
        foreach (string line in File.ReadLines(log))
        {
            if (!line.Contains("\"type\":\"user\"", StringComparison.Ordinal))
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
                if (!record.RootElement.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
                    || !message.TryGetProperty("content", out var content))
                    continue;
                var texts = content.ValueKind == JsonValueKind.String
                    ? [content.GetString()!]
                    : content.ValueKind == JsonValueKind.Array
                        ? content.EnumerateArray().Where(b => b.ValueKind == JsonValueKind.Object && RecordFiles.String(b, "type") == "text").Select(b => RecordFiles.String(b, "text") ?? "").ToList()
                        : [];
                foreach (string text in texts)
                    if (SessionLog.UserText.IsTyped(text))
                        yield return text;
            }
        }
    }

    /// <summary>A text with quotes and white space folded and in lower case, for looking a quotation up.</summary>
    /// <param name="text">The text.</param>
    static string FoldText(string text) => Fold().Replace(text, " ").Trim().ToLowerInvariant();

    /// <summary>A text with its first occurrence of one string replaced by another.</summary>
    /// <param name="text">The text.</param>
    /// <param name="find">What to find.</param>
    /// <param name="replace">What to put in its place.</param>
    static string ReplaceFirst(string text, string find, string replace)
    {
        int at = text.IndexOf(find, StringComparison.Ordinal);
        return at < 0 ? text : text[..at] + replace + text[(at + find.Length)..];
    }

    /// <summary>A path relative to the campaign folder, with forward slashes.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="path">The full path.</param>
    static string Relative(string campaignFolder, string path) => Path.GetRelativePath(campaignFolder, path).Replace('\\', '/');
}
