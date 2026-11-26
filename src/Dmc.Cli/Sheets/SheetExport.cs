using System.Globalization;
using Dmc.Size;

namespace Dmc.Sheets;

/// <summary>What an export or a check did.</summary>
/// <param name="Lines">What goes to standard output: the findings and warnings (capped at <see cref="Budgets.CheckLines"/>), then, for an export that was written, its one line.</param>
/// <param name="Refused">The one line of a refused export, for standard error and exit code 1; null when nothing was refused.</param>
/// <param name="Written">The page written, or null for a check or a refusal.</param>
internal sealed record SheetOutcome(IReadOnlyList<string> Lines, string? Refused, string? Written);

/// <summary>
/// The work of <c>dmc sheet</c> (spec 031): which characters, their files read, the structure check, and the page
/// written to <c>exports/sheets/&lt;campaign&gt;/</c> in the DMC folder, outside <c>adventures/</c>, so <c>dmc size</c>
/// and <c>dmc campaign export</c> never read it. Takes the DMC folder and the date as parameters, so a test touches
/// neither the real folder nor the clock. Writes only the one page, and only when the check finds nothing.
/// </summary>
internal static class SheetExport
{
    /// <summary>The folder under the DMC folder that holds the pages, one folder per campaign.</summary>
    public const string ExportFolder = "exports/sheets";

    /// <summary>The file name of the page <c>--party</c> writes.</summary>
    public const string PartyFile = "party.html";

    /// <summary>The file name of a page of several characters that is not the party.</summary>
    public const string SeveralFile = "sheets.html";

    /// <summary>The Party status block that holds people with the party who are not companions (<c>ReadSet.NotCompanions</c>).</summary>
    const string NotCompanions = "With the party, not companions";

    /// <summary>A sheet file as found in <c>character-sheets/</c>, before it is chosen.</summary>
    /// <param name="File">The file name.</param>
    /// <param name="Title">Its <c># </c> title, or the file name without <c>.md</c>.</param>
    /// <param name="Lines">Its lines, comments removed.</param>
    sealed record SheetFileText(string File, string Title, List<string> Lines)
    {
        /// <summary>Whether this is a player character's sheet (<c>pc-*.md</c>).</summary>
        public bool Pc => File.StartsWith("pc-", StringComparison.Ordinal);
    }

    /// <summary>Runs an export, or a check with <see cref="SheetRequest.Check"/>.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="request">The command line, read.</param>
    /// <param name="today">The real date of the export, printed on the page.</param>
    /// <exception cref="DmcException">The campaign, a name or the party cannot be resolved, or a file cannot be read or written (one line, exit 1).</exception>
    public static SheetOutcome Run(string dmcFolder, SheetRequest request, DateOnly today)
    {
        string campaign = SizeReport.Target(dmcFolder, request.Campaign, "dmc sheet --campaign NAME");
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        var files = SheetFiles(root);
        var state = File.Exists(Path.Combine(root, "game-state.md"))
            ? SheetText.Sections(SheetText.LinesWithoutComments(ReadText(Path.Combine(root, "game-state.md"))))
            : [];
        var blocks = PartyBlocks(state);

        var skipped = new List<string>();
        var chosen = Choose(files, blocks, request, campaign, skipped);
        var sheets = chosen.Select(f => Read(root, f, blocks, request.History)).ToList();

        var result = SheetCheck.Run(sheets);
        var lines = Capped([.. result.Findings, .. result.Warnings]);
        if (request.Check)
            return new SheetOutcome(lines, null, null);
        if (result.Findings.Count > 0)
            return new SheetOutcome(lines,
                $"refused: {Count(result.Findings.Count, "problem")} in the files: fix each one and run it again", null);

        string asOf = SheetText.Find(state, "Scene") is { } scene
            ? SheetText.Fields(scene.Lines).FirstOrDefault(f => f.Label.Equals("In-world date and time", StringComparison.OrdinalIgnoreCase))?.Value ?? ""
            : "";
        string name = request.Party ? PartyFile : sheets.Count == 1 ? Path.GetFileNameWithoutExtension(sheets[0].File) + ".html" : SeveralFile;
        string relative = $"{ExportFolder}/{campaign}/{name}";
        string target = Path.Combine(dmcFolder, ExportFolder.Replace('/', Path.DirectorySeparatorChar), campaign, name);
        Write(target, SheetHtml.Page(sheets, asOf.Length > 0 ? asOf : null, today, request.History));

        string line = $"wrote {relative}: {Count(sheets.Count, "sheet")} ({string.Join(", ", sheets.Select(s => s.Title))})";
        if (skipped.Count > 0)
            line += "; skipped " + string.Join(", ", skipped.Select(s => s + " (no sheet)"));
        return new SheetOutcome([.. lines, line], null, target);
    }

    /// <summary>
    /// The findings and warnings as printed: at most <see cref="Budgets.CheckLines"/> lines, the last saying how many more
    /// there are when they do not fit (as <c>dmc size --check</c> does).
    /// </summary>
    /// <param name="lines">Every finding, then every warning.</param>
    static List<string> Capped(List<string> lines)
    {
        if (lines.Count <= Budgets.CheckLines)
            return lines;
        int shown = Budgets.CheckLines - 1;
        return [.. lines.Take(shown), $"... and {lines.Count - shown} more: fix these and run it again"];
    }

    /// <summary>A count and a noun, with an <c>s</c> unless the count is 1.</summary>
    /// <param name="count">How many.</param>
    /// <param name="noun">The noun, singular.</param>
    static string Count(int count, string noun) => string.Create(CultureInfo.InvariantCulture, $"{count} {noun}{(count == 1 ? "" : "s")}");

    /// <summary>A file's text, read as UTF-8.</summary>
    /// <param name="path">The file.</param>
    /// <exception cref="DmcException">It cannot be read.</exception>
    static string ReadText(string path)
    {
        try
        {
            return File.ReadAllText(path, System.Text.Encoding.UTF8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot read {path}: {e.Message.TrimEnd('.', '\r', '\n')}");
        }
    }

    /// <summary>The sheets in <c>character-sheets/</c>, in file name order: every <c>*.md</c> but <c>minor-npcs.md</c> and the <c>-history.md</c> files.</summary>
    /// <param name="root">The campaign folder.</param>
    static List<SheetFileText> SheetFiles(string root)
    {
        string folder = Path.Combine(root, "character-sheets");
        if (!Directory.Exists(folder))
            return [];
        var files = new List<SheetFileText>();
        foreach (string file in Directory.EnumerateFiles(folder, "*.md").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal))
        {
            if (file == "minor-npcs.md" || file.EndsWith("-history.md", StringComparison.Ordinal))
                continue;
            var lines = SheetText.LinesWithoutComments(ReadText(Path.Combine(folder, file)));
            files.Add(new SheetFileText(file, SheetText.Title(lines) ?? Path.GetFileNameWithoutExtension(file), lines));
        }
        return files;
    }

    /// <summary>
    /// The <c>### </c> blocks under <c>## Party status</c>, in order, with their lines: every one but
    /// "With the party, not companions" and the template's bracketed placeholder.
    /// </summary>
    /// <param name="state">The sections of <c>game-state.md</c>.</param>
    static List<(string Name, IReadOnlyList<string> Lines)> PartyBlocks(IReadOnlyList<SheetSection> state) =>
        SheetText.Find(state, "Party status") is { } party
            ? [.. party.Subsections.Where(s => s.Heading.Length > 0
                                              && !s.Heading.Contains(NotCompanions, StringComparison.OrdinalIgnoreCase)
                                              && !(s.Heading.StartsWith('(') && s.Heading.EndsWith(')')))]
            : [];

    /// <summary>
    /// The sheet a name gives: one whose title is the name, or whose file name is the name with or without <c>.md</c>,
    /// ignoring case. Nothing looser (spec 031 FR-003): a party block or a name must name its sheet exactly, so an NPC's
    /// sheet is never picked by a shared word.
    /// </summary>
    /// <param name="files">The sheets.</param>
    /// <param name="name">A name, or a block's heading.</param>
    /// <exception cref="DmcException">The name gives two sheets.</exception>
    static SheetFileText? Match(List<SheetFileText> files, string name)
    {
        string n = name.Trim();
        var found = files.Where(f => f.Title.Equals(n, StringComparison.OrdinalIgnoreCase) || f.File.Equals(n, StringComparison.OrdinalIgnoreCase)
                                     || Path.GetFileNameWithoutExtension(f.File).Equals(n, StringComparison.OrdinalIgnoreCase)).ToList();
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new DmcException($"{n} names {found.Count} sheets ({string.Join(", ", found.Select(f => f.File))}): give the file name"),
        };
    }

    /// <summary>Which sheets an export prints, in order, each once.</summary>
    /// <param name="files">The sheets.</param>
    /// <param name="blocks">The Party status blocks.</param>
    /// <param name="request">The command line.</param>
    /// <param name="campaign">The campaign's name, for the messages.</param>
    /// <param name="skipped">Receives the Party status blocks that name no sheet (with <c>--party</c>).</param>
    /// <exception cref="DmcException">There is nobody to print, or a name is not a PC or a party member, or names no sheet.</exception>
    static List<SheetFileText> Choose(List<SheetFileText> files, List<(string Name, IReadOnlyList<string> Lines)> blocks, SheetRequest request,
        string campaign, List<string> skipped)
    {
        var chosen = new List<SheetFileText>();
        void Add(SheetFileText f)
        {
            if (!chosen.Contains(f))
                chosen.Add(f);
        }

        if (request.Party)
        {
            foreach (var (block, _) in blocks)
            {
                if (Match(files, block) is { } f)
                    Add(f);
                else
                    skipped.Add(block);
            }
            return chosen.Count > 0 ? chosen
                : throw new DmcException($"no block under Party status in {campaign}/game-state.md names a sheet in character-sheets/");
        }
        if (request.Names.Count == 0)
        {
            foreach (var pc in files.Where(f => f.Pc))
                Add(pc);
            return chosen.Count > 0 ? chosen
                : throw new DmcException($"no player character's sheet (character-sheets/pc-*.md) in {campaign}");
        }

        var party = blocks.Select(b => Match(files, b.Name)).OfType<SheetFileText>().ToHashSet();
        foreach (string name in request.Names)
        {
            var f = Match(files, name) ?? throw new DmcException($"no sheet named {name} in {campaign}/character-sheets/ (give its title or its file name)");
            if (!f.Pc && !party.Contains(f))
                throw new DmcException($"{name} is not a player character or in the party: an NPC's sheet holds spoilers, so it is not exported");
            Add(f);
        }
        return chosen;
    }

    /// <summary>A chosen sheet read: its sections and kind, its Party status block, and with <c>--history</c> a PC's history file.</summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="file">The sheet.</param>
    /// <param name="blocks">The Party status blocks.</param>
    /// <param name="history">Whether <c>--history</c> was given.</param>
    static CharacterSheet Read(string root, SheetFileText file, List<(string Name, IReadOnlyList<string> Lines)> blocks, bool history)
    {
        var sections = SheetText.Sections(file.Lines);
        SheetKind? kind = file.Pc ? SheetKind.Pc : CharacterSheet.CompanionKind(sections);
        IReadOnlyList<SheetField>? party = null;
        var others = new List<string>();
        foreach (var (name, lines) in blocks)
        {
            string n = name.Trim();
            if (file.Title.Equals(n, StringComparison.OrdinalIgnoreCase) || file.File.Equals(n, StringComparison.OrdinalIgnoreCase)
                || Path.GetFileNameWithoutExtension(file.File).Equals(n, StringComparison.OrdinalIgnoreCase))
            {
                party = SheetText.Fields(lines, others);
                break;
            }
        }
        IReadOnlyList<string>? older = null;
        string historyPath = Path.Combine(root, "character-sheets", Path.GetFileNameWithoutExtension(file.File) + "-history.md");
        if (history && file.Pc && File.Exists(historyPath))
            older = [.. SheetText.LinesWithoutComments(ReadText(historyPath)).Where(l => !l.StartsWith("# ", StringComparison.Ordinal))];
        return new CharacterSheet(file.File, file.Title, kind, sections, party, others, older);
    }

    /// <summary>Writes the page: to a file beside it first, then over the old page, so a failed write leaves the old one whole.</summary>
    /// <param name="target">The page's path.</param>
    /// <param name="html">The page.</param>
    /// <exception cref="DmcException">It cannot be written.</exception>
    static void Write(string target, string html)
    {
        string temp = target + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(temp, Io.Utf8Replacing.GetBytes(html));
            File.Move(temp, target, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception d) when (d is IOException or UnauthorizedAccessException)
            {
            }
            throw new DmcException($"cannot write {target}: {e.Message.TrimEnd('.', '\r', '\n')}");
        }
    }
}
