using System.Text.RegularExpressions;

namespace Dmc.Size;

/// <summary>
/// The files <c>dmc-resume-campaign</c> step 2 reads for a campaign, and the part of each, chosen by that step's rules
/// (research R2, R9, R10). The rules live in the skill; this class follows them, and a toolkit test checks that the
/// markers below are still written in the skill word for word, so a change to step 2 fails the build until this class
/// changes too.
/// </summary>
internal static partial class ReadSet
{
    /// <summary>A sheet whose last section is this heading is read down to it.</summary>
    internal const string HistoryHeading = "## History";

    /// <summary>The older name of a sheet's history section.</summary>
    internal const string NotesHeading = "## Notes";

    /// <summary>The other older name of a sheet's history section.</summary>
    internal const string HistoryWithThePcHeading = "## History with the PC";

    /// <summary>The Party status block that is not a companion.</summary>
    internal const string NotCompanions = "With the party, not companions";

    /// <summary>The words of the Last full save line that say which scene the save covers.</summary>
    internal const string ThroughScene = "through scene";

    /// <summary>A real scene heading of a session log; the template's commented <c>### Scene N:</c> does not match.</summary>
    internal const string ScenePattern = "^### Scene [0-9]+:";

    /// <summary>Canon's always-read section of rulings.</summary>
    internal const string Rulings = "Rulings";

    /// <summary>Canon's always-read section of prices, debts and promises.</summary>
    internal const string Prices = "Prices, debts and promises";

    /// <summary>The words in a canon section's comment that make it always read.</summary>
    internal const string AlwaysRead = "Always read";

    /// <summary>The heading of a companion's play card, the first <c>## </c> section of the sheet (spec 013 research R1).</summary>
    internal const string PlayCardHeading = "## Play card";

    /// <summary>The option of <c>dmc recall</c> the resume runs for canon's scene matches (spec 013 research R3).</summary>
    internal const string SceneOption = "--scene";

    /// <summary>Every marker above, which <c>dmc-resume-campaign</c> § 2 must contain (checked by <c>ToolkitTests</c>).</summary>
    internal static readonly string[] Markers =
        [HistoryHeading, NotesHeading, HistoryWithThePcHeading, NotCompanions, ThroughScene, ScenePattern, Rulings, Prices, AlwaysRead,
         PlayCardHeading, SceneOption];

    /// <summary>The history headings, without their <c>## </c>.</summary>
    static readonly string[] HistoryHeadings = [HistoryHeading[3..], NotesHeading[3..], HistoryWithThePcHeading[3..]];

    /// <summary>A scene heading of a session log, with its number.</summary>
    [GeneratedRegex(@"^### Scene ([0-9]+):")]
    private static partial Regex SceneHeading();

    /// <summary>A session log's file name, with its number.</summary>
    [GeneratedRegex(@"^session-([0-9]+)\.md$")]
    private static partial Regex SessionFile();

    /// <summary>The session a Last full save line names.</summary>
    [GeneratedRegex(@"session ([0-9]+)")]
    private static partial Regex StampSession();

    /// <summary>The scene a Last full save line says it went through.</summary>
    [GeneratedRegex(@"through scene ([0-9]+)")]
    private static partial Regex StampScene();

    /// <summary>The module folder named in <c>module-progress.md</c>.</summary>
    [GeneratedRegex(@"modules/([^/`\s]+)")]
    private static partial Regex ModuleFolder();

    /// <summary>A pointer that names no digest: the word <c>none</c>, in any case, alone or followed by punctuation and prose.</summary>
    [GeneratedRegex(@"^none\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoDigest();

    /// <summary>The chapter digest file named in <c>module-progress.md</c>.</summary>
    [GeneratedRegex(@"[\w./-]+\.md")]
    private static partial Regex DigestFile();

    /// <summary>An HTML comment on one line.</summary>
    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex InlineComment();

    /// <summary>One sheet in <c>character-sheets/</c>.</summary>
    /// <param name="File">Its file name.</param>
    /// <param name="Title">Its <c># </c> title, or the file name without <c>.md</c> when it has none.</param>
    /// <param name="Pc">True for a <c>pc-*.md</c> sheet.</param>
    /// <param name="Lines">The sheet's lines, read once for both its title and its measurement.</param>
    /// <param name="Shown">The path the report names: <c>character-sheets/&lt;file&gt;</c>, or for a sheet a record prints (spec 036) the record's.</param>
    sealed record Sheet(string File, string Title, bool Pc, List<Line> Lines, string Shown);

    /// <summary>
    /// Words never matched on their own when a sheet's first or last word is tried: articles, prepositions and titles,
    /// which a Present line or another sheet's title holds as often as the name (a "Sister Ivy" is not "Sister Maren").
    /// </summary>
    static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "of", "and", "at", "in", "on", "to", "from", "old", "young", "little", "big",
        "captain", "sergeant", "lieutenant", "commander", "general", "corporal", "private", "watchman", "guard",
        "sister", "brother", "mother", "father", "aunt", "auntie", "uncle", "granny", "grandma", "grandpa",
        "lady", "lord", "sir", "dame", "master", "mistress", "madam", "king", "queen", "prince", "princess",
        "duke", "duchess", "baron", "baroness", "count", "countess", "elder", "high", "priest", "priestess", "doctor",
    };

    /// <summary>Whether a word is one of <see cref="CommonWords"/>, never matched alone as a name; the register (spec 013) uses the same list.</summary>
    /// <param name="word">A word, compared ignoring case.</param>
    internal static bool IsCommonNameWord(string word) => CommonWords.Contains(word);

    /// <summary>
    /// The read set of a campaign, in the resume's order: item 1 (both player-options files), 2 (<c>game-state.md</c>),
    /// 3 (<c>summary.md</c>), 4 (<c>dm-notes.md</c>), 5 (the sheets), 6 (the latest session log), 7 (canon) and 9 (the
    /// module files). Item 8, the maps, is never read into context, so it is not counted.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder, which holds <c>adventures/</c> and <c>modules/</c>.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>.</param>
    /// <returns>One measured part per file, the files that are missing included.</returns>
    /// <exception cref="DmcException">A file cannot be read.</exception>
    public static List<Part> Build(string dmcFolder, string campaign)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        var parts = new List<Part>
        {
            Whole(Path.Combine(dmcFolder, "adventures", "player-options.md"), "adventures/player-options.md", BudgetKind.None),
            Whole(Path.Combine(root, "player-options.md"), "player-options.md", BudgetKind.None),
        };

        string gameStatePath = Path.Combine(root, "game-state.md");
        var gameState = File.Exists(gameStatePath) ? Measurement.ReadLines(gameStatePath) : [];
        parts.Add(File.Exists(gameStatePath)
            ? new Part("game-state.md", PartKind.Whole, EntryState.Measured, BudgetKind.GameState, gameState, null, gameState)
            : Part.Absent("game-state.md", EntryState.Missing, BudgetKind.GameState, "missing"));
        parts.Add(Whole(Path.Combine(root, "story", "summary.md"), "story/summary.md", BudgetKind.Summary));
        parts.Add(Whole(Path.Combine(root, "story", "dm-notes.md"), "story/dm-notes.md", BudgetKind.DmNotes));
        parts.AddRange(Sheets(root, gameState));
        parts.Add(SessionLog(root, gameState));
        parts.Add(Canon(dmcFolder, campaign, root));
        parts.AddRange(ModuleFiles(dmcFolder, root));
        return parts;
    }

    /// <summary>
    /// The files <c>dmc size</c> measures and flags that the resume does not read, so they are outside its total: the
    /// campaign's own <c>legacy.md</c>, when it exists, which linked campaigns read and the full save keeps within its
    /// budget (spec 011 FR-025). None for a campaign with no legacy.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>.</param>
    /// <exception cref="DmcException">A file cannot be read.</exception>
    public static List<Part> OutsideTheTotal(string dmcFolder, string campaign)
    {
        string path = Path.Combine(dmcFolder, "adventures", campaign, Campaign.CampaignLinks.LegacyFile);
        if (!File.Exists(path))
            return [];
        var lines = Measurement.ReadLines(path);
        return [new Part(Campaign.CampaignLinks.LegacyFile, PartKind.Whole, EntryState.Measured, BudgetKind.Legacy, lines,
            "outside the total: linked campaigns read it", lines, InTotal: false)];
    }

    /// <summary>A file read whole, or <see cref="EntryState.Missing"/> when it does not exist.</summary>
    /// <param name="path">The file's full path.</param>
    /// <param name="shown">The path the report shows.</param>
    /// <param name="budget">Its budget kind.</param>
    static Part Whole(string path, string shown, BudgetKind budget)
    {
        if (!File.Exists(path))
            return Part.Absent(shown, EntryState.Missing, budget, "missing");
        var lines = Measurement.ReadLines(path);
        return new Part(shown, PartKind.Whole, EntryState.Measured, budget, lines, null, lines);
    }

    // ---------- item 5: the sheets ----------

    /// <summary>
    /// The sheets the resume reads (step 2.5): every PC's; each companion's, a <c>### </c> block under Party status other
    /// than <see cref="NotCompanions"/>; and each NPC's whose name is in the Scene block's Present line or the Combat
    /// tracker. Each is read down to its history, and listed once. A companion block with no sheet is
    /// <see cref="EntryState.NotFound"/>. <c>minor-npcs.md</c> and the <c>&lt;sheet&gt;-history.md</c> files the full save
    /// moves old history to are never read.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="gameState">The lines of <c>game-state.md</c> (empty when it is missing).</param>
    static List<Part> Sheets(string root, IReadOnlyList<Line> gameState)
    {
        string folder = Path.Combine(root, "character-sheets");
        var sheets = new List<Sheet>();
        if (Directory.Exists(folder))
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*.md").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal))
            {
                // Not a sheet: the bit parts, and the history a full save moves out of a sheet (resume reads neither).
                if (file == "minor-npcs.md" || file.EndsWith("-history.md", StringComparison.Ordinal))
                    continue;
                var lines = Measurement.ReadLines(Path.Combine(folder, file));
                var first = lines.FirstOrDefault(l => l.Text.StartsWith("# ", StringComparison.Ordinal));
                string title = first.Text is { } t ? t[2..].Trim() : Path.GetFileNameWithoutExtension(file);
                sheets.Add(new Sheet(file, title, file.StartsWith("pc-", StringComparison.Ordinal), lines, "character-sheets/" + file));
            }
        }
        // A sheet a record prints where its Markdown file is gone (spec 036 FR-010), measured without its source marks,
        // as the play-card budget counts it (#262); the resume reads the marks too (dmc records show --card).
        foreach (var printed in PersonRecords.RecordSheet.Printed(root))
            sheets.Add(new Sheet(printed.File, printed.Record!.Person, PersonRecords.RecordSheet.IsPc(printed.File),
                PersonRecords.RecordSheet.Measured(printed.Lines), printed.Record.Label));
        sheets.Sort((a, b) => string.CompareOrdinal(a.File, b.File));

        var chosen = new List<Sheet>();
        var companions = new HashSet<Sheet>();
        var parts = new List<Part>();
        foreach (var pc in sheets.Where(s => s.Pc))
            chosen.Add(pc);

        bool hasParty = Measurement.SectionLines(gameState, "Party status").Count > 0;
        var titles = sheets.Select(s => s.Title).ToList();
        foreach (string block in CompanionBlocks(gameState))
        {
            int index = MatchCompanion(block, titles);
            var match = index >= 0 ? sheets[index] : null;
            if (match is null)
                parts.Add(Part.Absent($"character-sheets: {block}", EntryState.NotFound, BudgetKind.OtherSheet, "not found"));
            else if (!chosen.Contains(match))
            {
                chosen.Add(match);
                companions.Add(match);
            }
        }

        string scene = PresentText(gameState);
        foreach (var npc in sheets.Where(s => !s.Pc && !chosen.Contains(s) && Mentions(scene, s.Title)))
            chosen.Add(npc);

        var result = chosen.Select(s => SheetPart(s, companions.Contains(s))).ToList();
        result.AddRange(parts);
        if (gameState.Count > 0 && (!hasParty || !gameState.Any(l => l.Text.Contains("**Present**", StringComparison.Ordinal))))
            result.Add(Part.Absent("character-sheets: companions and NPCs present", EntryState.NotFound, BudgetKind.None,
                "could not be told: game-state.md has no Party status or Present line"));
        return result;
    }

    /// <summary>
    /// The sheet a Party status block names, by its title: the same name, then the whole title within the block, then a
    /// distinctive word of it as the block's first or last word (a trailing bracket left out); -1 for none. A word inside
    /// the name does not count, so "The Kelderak treasure (s48.3)" names no Kelderak's sheet. Shared with migration 006's
    /// <c>companions-have-play-card</c> check (spec 013), so the check and the resume agree on who is a companion.
    /// </summary>
    /// <param name="block">The block's heading text.</param>
    /// <param name="titles">The sheets' titles, in file name order.</param>
    internal static int MatchCompanion(string block, IReadOnlyList<string> titles)
    {
        for (int i = 0; i < titles.Count; i++)
            if (string.Equals(titles[i], block, StringComparison.OrdinalIgnoreCase))
                return i;
        for (int i = 0; i < titles.Count; i++)
            if (IsWholeWord(block, titles[i]))
                return i;
        string edges = EdgeWords(block);
        for (int i = 0; i < titles.Count; i++)
            if (Mentions(edges, titles[i]))
                return i;
        return -1;
    }

    /// <summary>A block name's first and last words, with a trailing bracket (<c>(s48.3)</c>) left out; the whole name when it is one word.</summary>
    /// <param name="block">The block's heading text.</param>
    static string EdgeWords(string block)
    {
        string name = Regex.Replace(block, @"\s*\([^)]*\)\s*$", "");
        string[] words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 2 ? name : words[0] + " " + words[^1];
    }

    /// <summary>The names of the <c>### </c> blocks under Party status, except <see cref="NotCompanions"/> and a placeholder in brackets.</summary>
    /// <param name="gameState">The lines of <c>game-state.md</c>.</param>
    internal static IEnumerable<string> CompanionBlocks(IReadOnlyList<Line> gameState) =>
        OutsideComments(Measurement.SectionLines(gameState, "Party status"))
            .Where(t => t.StartsWith("### ", StringComparison.Ordinal))
            .Select(t => t[4..].Trim())
            .Where(name => name.Length > 0 && !name.Contains(NotCompanions, StringComparison.OrdinalIgnoreCase)
                           && !(name.StartsWith('(') && name.EndsWith(')'))); // the template's "(character name)" placeholder

    /// <summary>
    /// The text that names who is in the scene: the Scene block's Present line and the Combat tracker section, comments
    /// left out. Shared by the resume's read set (<see cref="Sheets"/>) and the hook's sheet lines
    /// (<see cref="Retrieval.SheetReminder"/>, spec 016 FR-008), so the two agree on who is present.
    /// </summary>
    /// <param name="gameState">The lines of <c>game-state.md</c>.</param>
    internal static string PresentText(IReadOnlyList<Line> gameState)
    {
        var present = OutsideComments(Measurement.SectionLines(gameState, "Scene"))
            .Where(t => t.Contains("**Present**", StringComparison.Ordinal));
        var tracker = OutsideComments(Measurement.SectionLines(gameState, "Combat tracker"));
        return string.Join("\n", present.Concat(tracker));
    }

    /// <summary>The texts of the lines outside HTML comments (a comment may span lines); a comment within a line is removed.</summary>
    /// <param name="lines">Some lines.</param>
    internal static IEnumerable<string> OutsideComments(IEnumerable<Line> lines)
    {
        bool inComment = false;
        foreach (var line in lines)
        {
            string text = InlineComment().Replace(line.Text, "");
            if (inComment)
            {
                int end = text.IndexOf("-->", StringComparison.Ordinal);
                if (end < 0)
                    continue;
                inComment = false;
                text = text[(end + 3)..];
            }
            int start = text.IndexOf("<!--", StringComparison.Ordinal);
            if (start >= 0)
            {
                inComment = true;
                text = text[..start];
            }
            yield return text;
        }
    }

    /// <summary>
    /// Whether a text names a sheet's character: its whole title, its first word or its last word (a surname), each as a
    /// whole word and ignoring case. Words shorter than three letters and <see cref="CommonWords"/> are not tried alone. A
    /// text that names someone off scene also matches, which counts a sheet the resume may not read: the measurement errs
    /// toward a flag, not away from one.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="title">The sheet's title.</param>
    internal static bool Mentions(string text, string title) => MentionAt(text, title) >= 0;

    /// <summary>
    /// Where a text first names a sheet's character, by the same tries as <see cref="Mentions"/>: the index of the
    /// earliest match, or -1 for none. The hook's sheet lines use it to keep the Present line's order (spec 016 FR-008).
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="title">The sheet's title.</param>
    internal static int MentionAt(string text, string title)
    {
        if (text.Length == 0 || title.Length == 0)
            return -1;
        string[] words = title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var tries = new List<string> { title };
        if (words.Length > 1)
        {
            tries.AddRange(new[] { words[0], words[^1] }.Select(w => w.Trim(',', '.', ';', ':', '(', ')', '"', '\''))
                .Where(w => w.Length >= 3 && !CommonWords.Contains(w)));
        }
        int first = -1;
        foreach (string word in tries)
        {
            int at = WholeWordAt(text, word);
            if (at >= 0 && (first < 0 || at < first))
                first = at;
        }
        return first;
    }

    /// <summary>Whether a text holds a word or phrase as a whole word (not inside a longer one), ignoring case.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="word">The word or phrase.</param>
    static bool IsWholeWord(string text, string word) => WholeWordAt(text, word) >= 0;

    /// <summary>The index of the first whole-word match of a word or phrase in a text, ignoring case; -1 for none.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="word">The word or phrase.</param>
    internal static int WholeWordAt(string text, string word)
    {
        if (word.Length == 0)
            return -1;
        var match = Regex.Match(text, $@"(?<!\w){Regex.Escape(word)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Index : -1;
    }

    /// <summary>
    /// A sheet as the resume reads it. A companion's with a <see cref="PlayCardHeading"/> section: from the top through
    /// the card, against <see cref="Budgets.PlayCard"/> (spec 013 FR-004). Any other sheet, and a companion's with no
    /// card (marked so the full save writes one): down to its history, the lines before its last <c>## </c> heading when
    /// that is a history heading; the whole sheet otherwise (history in the middle is read whole).
    /// </summary>
    /// <param name="sheet">The sheet, with its lines.</param>
    /// <param name="companion">True when the sheet is a companion's (a block under Party status).</param>
    static Part SheetPart(Sheet sheet, bool companion)
    {
        var lines = sheet.Lines;
        if (companion && PlayCardEnd(lines) is { } cardEnd)
            return new Part(sheet.Shown, PartKind.ToPlayCard, EntryState.Measured, BudgetKind.PlayCard,
                lines.GetRange(0, cardEnd), "to its play card", lines);
        int last = -1;
        for (int i = 0; i < lines.Count; i++)
            if (Measurement.IsSectionHeading(lines[i].Text))
                last = i;
        string shown = sheet.Shown;
        var budget = sheet.Pc ? BudgetKind.PcSheet : BudgetKind.OtherSheet;
        if (last >= 0 && HistoryHeadings.Contains(lines[last].Text[3..].Trim(), StringComparer.OrdinalIgnoreCase))
            return new Part(shown, PartKind.ToHistory, EntryState.Measured, budget, lines.GetRange(0, last), "to its history", lines,
                NoPlayCard: companion);
        return new Part(shown, PartKind.Whole, EntryState.Measured, budget, lines, "whole (no history last)", lines,
            NoPlayCard: companion);
    }

    /// <summary>
    /// Where the resume stops reading a sheet with a play card: the line of the next <c>## </c> heading after
    /// <see cref="PlayCardHeading"/>, or the end of the sheet; null when the sheet has no card. The heading is matched
    /// ignoring case and trailing spaces.
    /// </summary>
    /// <param name="lines">The sheet's lines.</param>
    internal static int? PlayCardEnd(IReadOnlyList<Line> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].Text.TrimEnd().Equals(PlayCardHeading, StringComparison.OrdinalIgnoreCase))
                continue;
            for (int end = i + 1; end < lines.Count; end++)
                if (Measurement.IsSectionHeading(lines[end].Text))
                    return end;
            return lines.Count;
        }
        return null;
    }

    // ---------- item 6: the session log ----------

    /// <summary>
    /// The latest session log (the <c>session-NNN.md</c> with the highest number): from its third-last scene heading to the
    /// end; whole when it has fewer than three scenes, or when the Last full save line does not cover it (no "through scene
    /// N", an earlier session than the log's, or N before the log's last scene).
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="gameState">The lines of <c>game-state.md</c>.</param>
    static Part SessionLog(string root, IReadOnlyList<Line> gameState)
    {
        string folder = Path.Combine(root, "story", "sessions");
        var latest = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "session-*.md")
                .Select(f => (File: f, Match: SessionFile().Match(Path.GetFileName(f))))
                .Where(x => x.Match.Success)
                .OrderByDescending(x => long.Parse(x.Match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
                .FirstOrDefault()
            : default;
        if (latest.File is null)
            return Part.Absent("story/sessions/", EntryState.Missing, BudgetKind.None, "missing (no session log)");

        string shown = "story/sessions/" + Path.GetFileName(latest.File);
        long session = long.Parse(latest.Match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var lines = Measurement.ReadLines(latest.File);
        var scenes = new List<(int Index, long Number)>();
        for (int i = 0; i < lines.Count; i++)
            if (SceneHeading().Match(lines[i].Text) is { Success: true } m)
                scenes.Add((i, long.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)));

        if (scenes.Count < 3)
            return new Part(shown, PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, "whole (fewer than 3 scenes)", lines);
        if (!Covered(gameState, session, scenes[^1].Number))
            return new Part(shown, PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, "whole (the last full save does not cover it)", lines);
        var tail = scenes[^3];
        return new Part(shown, PartKind.Tail, EntryState.Measured, BudgetKind.None, lines.GetRange(tail.Index, lines.Count - tail.Index), $"from scene {tail.Number}", lines);
    }

    /// <summary>Whether the Last full save line in <c>game-state.md</c> covers a log: it names this session or a later one and a scene at or after the log's last.</summary>
    /// <param name="gameState">The lines of <c>game-state.md</c>.</param>
    /// <param name="session">The log's session number.</param>
    /// <param name="lastScene">The number of the log's last scene.</param>
    static bool Covered(IReadOnlyList<Line> gameState, long session, long lastScene)
    {
        string? stamp = gameState.Select(l => l.Text).FirstOrDefault(t => t.Contains("**Last full save**", StringComparison.Ordinal));
        if (stamp is null || StampScene().Match(stamp) is not { Success: true } scene || StampSession().Match(stamp) is not { Success: true } saved)
            return false;
        long savedSession = long.Parse(saved.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        long savedScene = long.Parse(scene.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return savedSession > session || (savedSession == session && savedScene >= lastScene);
    }

    // ---------- item 7: canon ----------

    /// <summary>
    /// Canon (spec 013 FR-007 to FR-009): whole while it is under <see cref="Budgets.CanonWholeBelow"/>; at or over it,
    /// its always-read sections (<see cref="Rulings"/>, <see cref="Prices"/>, and any section whose first comment says
    /// <see cref="AlwaysRead"/>) and the characters <c>dmc recall --scene</c> prints, which <see cref="Retrieval.SceneRead"/>
    /// builds, so the measure is what the resume reads.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="root">The campaign folder.</param>
    static Part Canon(string dmcFolder, string campaign, string root)
    {
        string path = Path.Combine(root, "story", "canon.md");
        const string shown = "story/canon.md";
        if (!File.Exists(path))
            return Part.Absent(shown, EntryState.Missing, BudgetKind.Canon, "missing");
        var lines = Measurement.ReadLines(path);
        if (Measurement.TokensOf(lines) < Budgets.CanonWholeBelow)
            return new Part(shown, PartKind.Whole, EntryState.Measured, BudgetKind.Canon, lines, "read whole", lines);
        long scene = Retrieval.SceneRead.Run(dmcFolder, campaign).Sum(l => (long)Retrieval.Recall.Characters(l));
        return new Part(shown, PartKind.ByScene, EntryState.Measured, BudgetKind.Canon, AlwaysReadSections(lines),
            "read in sections and by scene", lines, ExtraCharacters: scene);
    }

    /// <summary>The lines of canon's always-read sections, in file order.</summary>
    /// <param name="lines">Every line of canon.</param>
    internal static List<Line> AlwaysReadSections(IReadOnlyList<Line> lines)
    {
        var always = AlwaysReadMask(lines);
        return lines.Where((_, i) => always[i]).ToList();
    }

    /// <summary>
    /// For each line of canon (0-based), whether it is in an always-read section, its heading included. The scene read
    /// and the hook's reminder search the other lines, which the resume does not read whole.
    /// </summary>
    /// <param name="lines">Every line of canon.</param>
    internal static bool[] AlwaysReadMask(IReadOnlyList<Line> lines)
    {
        var kept = new bool[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            if (!Measurement.IsSectionHeading(lines[i].Text))
                continue;
            int end = i + 1;
            while (end < lines.Count && !Measurement.IsSectionHeading(lines[end].Text))
                end++;
            string heading = lines[i].Text[3..].Trim();
            bool always = heading.Equals(Rulings, StringComparison.OrdinalIgnoreCase)
                          || heading.Equals(Prices, StringComparison.OrdinalIgnoreCase)
                          || FirstComment(lines, i + 1, end).Contains(AlwaysRead, StringComparison.OrdinalIgnoreCase);
            if (always)
                for (int k = i; k < end; k++)
                    kept[k] = true;
            i = end - 1;
        }
        return kept;
    }

    /// <summary>The first HTML comment directly under a heading (blank lines skipped), or empty when the first text is not a comment.</summary>
    /// <param name="lines">The file's lines.</param>
    /// <param name="from">The line after the heading.</param>
    /// <param name="to">The next heading's line.</param>
    static string FirstComment(IReadOnlyList<Line> lines, int from, int to)
    {
        int i = from;
        while (i < to && lines[i].Text.Trim().Length == 0)
            i++;
        if (i >= to || !lines[i].Text.TrimStart().StartsWith("<!--", StringComparison.Ordinal))
            return "";
        var text = new List<string>();
        for (; i < to; i++)
        {
            text.Add(lines[i].Text);
            if (lines[i].Text.Contains("-->", StringComparison.Ordinal))
                break;
        }
        return string.Join("\n", text);
    }

    // ---------- item 9: the module files ----------

    /// <summary>
    /// For a module campaign (<c>module-progress.md</c> exists): that file, the module's <c>index.md</c> and the digest
    /// section named under "Digest to read before continuing"; only <c>module-progress.md</c> once every chapter is
    /// complete. Empty for a homebrew campaign.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder, which holds <c>modules/</c>.</param>
    /// <param name="root">The campaign folder.</param>
    static List<Part> ModuleFiles(string dmcFolder, string root)
    {
        string progressPath = Path.Combine(root, "module-progress.md");
        if (!File.Exists(progressPath))
            return [];
        var progress = Measurement.ReadLines(progressPath);
        var parts = new List<Part> { new("module-progress.md", PartKind.Whole, EntryState.Measured, BudgetKind.None, progress, null, progress) };
        if (ModuleFinished(progress))
            return parts;

        string? folderLine = ValueOf(progress, "**Module folder**");
        if (folderLine is null || ModuleFolder().Match(folderLine) is not { Success: true } named)
        {
            parts.Add(Part.Absent("modules/", EntryState.NotIngested, BudgetKind.None, "not ingested (module-progress.md names no module folder)"));
            return parts;
        }
        string module = named.Groups[1].Value;
        string moduleRoot = Path.Combine(dmcFolder, "modules", module);
        string indexShown = $"modules/{module}/index.md";
        string indexPath = Path.Combine(moduleRoot, "index.md");
        parts.Add(File.Exists(indexPath)
            ? Whole(indexPath, indexShown, BudgetKind.None)
            : Part.Absent(indexShown, EntryState.NotIngested, BudgetKind.None, "not ingested"));

        string? digest = ValueOf(progress, "**Digest to read before continuing**");
        // "none", and "none." with prose after it (the-silver-egg's "none. Consult … (`items.md`) …"), name no digest,
        // even when the prose names a file (spec 012 FR-003).
        if (digest is null || NoDigest().IsMatch(digest)
            || DigestFile().Match(digest) is not { Success: true } file)
            return parts;
        string relative = file.Value.StartsWith($"modules/{module}/", StringComparison.Ordinal) ? file.Value[$"modules/{module}/".Length..] : file.Value;
        string chapterShown = $"modules/{module}/{relative}";
        string chapterPath = Path.Combine(moduleRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(chapterPath))
        {
            parts.Add(Part.Absent(chapterShown, EntryState.NotIngested, BudgetKind.None, "not ingested"));
            return parts;
        }
        var sections = Headings(digest[(file.Index + file.Length)..]);
        if (sections.Count <= 1)
        {
            parts.Add(DigestSection(chapterPath, chapterShown, sections.Count == 0 ? "" : sections[0], relative));
            return parts;
        }
        // Several headings (an area and the event it depends on): one part each, each section counted once. When any
        // misses, the chapter is measured whole once, as for one heading, and its flag names every miss.
        var found = sections.Select(s => DigestSectionAt(chapterPath, $"{chapterShown} § {s}", s, relative)).ToList();
        var misses = found.Where(f => f.Part.PointerMiss is not null).Select(f => f.Part).ToList();
        if (misses.Count > 0)
        {
            string line = misses[0].PointerMiss!
                + string.Concat(misses.Skip(1).Select(m => "; " + m.PointerMiss!["module-progress.md: ".Length..]));
            parts.Add(misses[0] with { Path = chapterShown, PointerMiss = line });
            return parts;
        }
        var seen = new HashSet<int>();
        parts.AddRange(found.Where(f => seen.Add(f.Start)).Select(f => f.Part));
        return parts;
    }

    /// <summary>
    /// The headings a pointer names after its file (spec 012 research R7): each after a <c>§</c>, separated by <c>;</c>, in
    /// the new form; otherwise the one section the old forms name (<c>, section Name</c>, or the text alone), or none.
    /// </summary>
    /// <param name="rest">The pointer's text after the file's path.</param>
    internal static List<string> Headings(string rest)
    {
        char[] trim = [' ', ',', ';', ':', '-', '–', '`', '.'];
        if (rest.Contains('§'))
            return rest.Split('§').Select(p => p.Trim(trim)).Where(p => p.Length > 0).ToList();
        string section = rest.Trim(trim);
        if (section.StartsWith("section ", StringComparison.OrdinalIgnoreCase))
            section = section["section ".Length..].Trim();
        return section.Length == 0 ? [] : [section];
    }

    /// <summary>
    /// The module of a module campaign that is not finished and whose module folder exists, as named by
    /// <c>module-progress.md</c>'s Module folder line; null for any other campaign. Used for <c>dmc size</c>'s
    /// <c>module:</c> line (spec 012 research R6), by the same reading as <see cref="ModuleFiles"/>.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    internal static string? UnfinishedModule(string dmcFolder, string campaign)
    {
        string progressPath = Path.Combine(dmcFolder, "adventures", campaign, "module-progress.md");
        if (!File.Exists(progressPath))
            return null;
        var progress = Measurement.ReadLines(progressPath);
        if (ModuleFinished(progress) || ValueOf(progress, "**Module folder**") is not { } folderLine
            || ModuleFolder().Match(folderLine) is not { Success: true } named)
            return null;
        string module = named.Groups[1].Value;
        return Directory.Exists(Path.Combine(dmcFolder, "modules", module)) ? module : null;
    }

    /// <summary>The text after <c>key:</c> on the first line that holds <paramref name="key"/>, with HTML comments and backticks removed; null when there is no such line or it holds nothing.</summary>
    /// <param name="lines">The lines of a file.</param>
    /// <param name="key">The bold label, for example <c>**Module folder**</c>.</param>
    static string? ValueOf(IReadOnlyList<Line> lines, string key)
    {
        string? line = lines.Select(l => l.Text).FirstOrDefault(t => t.Contains(key, StringComparison.Ordinal));
        if (line is null)
            return null;
        string value = InlineComment().Replace(line[(line.IndexOf(key, StringComparison.Ordinal) + key.Length)..], "")
            .TrimStart(':', ' ').Replace("`", "").Trim();
        return value.Length == 0 ? null : value;
    }

    /// <summary>Whether <c>module-progress.md</c>'s Chapter status marks every chapter complete (and has at least one).</summary>
    /// <param name="progress">The lines of <c>module-progress.md</c>.</param>
    static bool ModuleFinished(IReadOnlyList<Line> progress)
    {
        var chapters = OutsideComments(Measurement.SectionLines(progress, "Chapter status"))
            .Select(t => t.Trim()).Where(t => t.StartsWith("- ", StringComparison.Ordinal)).ToList();
        return chapters.Count > 0 && chapters.All(t =>
            t.Contains("complete", StringComparison.OrdinalIgnoreCase) && !t.Contains("incomplete", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The digest section a module campaign reads next (see <see cref="DigestSectionAt"/>).</summary>
    /// <param name="path">The chapter digest file.</param>
    /// <param name="shown">The path the report shows.</param>
    /// <param name="section">The heading named in <c>module-progress.md</c>, or empty.</param>
    /// <param name="relative">The chapter's path in the module, as the pointer writes it, for the miss line.</param>
    static Part DigestSection(string path, string shown, string section, string relative) =>
        DigestSectionAt(path, shown, section, relative).Part;

    /// <summary>
    /// The digest section a module campaign reads next, found as <c>dmc lookup</c> finds it, by the same matcher
    /// (<see cref="Retrieval.Lookup.FindSection"/>, spec 012 research R8), so that what is measured is what the resume's
    /// lookup prints: from the heading to the next heading of the same or a higher level. The whole chapter when no section
    /// is named; also when the heading matches none or several, with the note saying which and the
    /// <see cref="Part.PointerMiss"/> line for the save.
    /// </summary>
    /// <param name="path">The chapter digest file.</param>
    /// <param name="shown">The path the report shows.</param>
    /// <param name="section">The heading named in <c>module-progress.md</c>, or empty.</param>
    /// <param name="relative">The chapter's path in the module, as the pointer writes it, for the miss line.</param>
    /// <returns>The part, and the 1-based line its section starts on (0 when the chapter is measured whole).</returns>
    static (Part Part, int Start) DigestSectionAt(string path, string shown, string section, string relative)
    {
        var lines = Measurement.ReadLines(path);
        if (section.Length == 0)
            return (new Part(shown, PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, "whole (no section named)", lines), 0);
        Retrieval.MarkdownFile file;
        try
        {
            file = Retrieval.MarkdownFile.Read(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot read {path}: {e.Message.TrimEnd('.', '\r', '\n')}");
        }
        var (matches, nearest) = Retrieval.Lookup.FindSection(file, section);
        if (matches.Count == 1)
        {
            // The section's lines come from the parsed file, whose line numbers they share, so a UTF-16 digest
            // (re-encoded as UTF-8 by MarkdownFile) is measured as lookup prints it.
            var heading = file.Headings[matches[0]];
            int last = Retrieval.MarkdownHeadings.SectionEnd(file.Headings, matches[0], file.Lines.Length);
            var sectionLines = Enumerable.Range(heading.Line - 1, last - heading.Line + 1)
                .Select(i => new Line(file.Lines[i], file.LineBytes[i], file.LineCharacters[i])).ToList();
            return (new Part(shown, PartKind.Section, EntryState.Measured, BudgetKind.None,
                sectionLines, "the section named", lines), heading.Line);
        }
        string miss = matches.Count == 0
            ? $"module-progress.md: no heading \"{section}\" in {relative}"
              + (nearest.Count > 0 ? $" (nearest: {string.Join("; ", nearest)})" : "") + ": name one"
            : $"module-progress.md: \"{section}\" matches {matches.Count} headings in {relative}: name one";
        string note = matches.Count == 0 ? $"whole (no heading holds \"{section}\")" : $"whole (\"{section}\" matches {matches.Count} headings)";
        return (new Part(shown, PartKind.Whole, EntryState.Measured, BudgetKind.None, lines, note, lines, PointerMiss: miss), 0);
    }
}
