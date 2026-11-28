using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.PersonRecords;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc records</c> (spec 032, contracts/dmc-records.md): who calls whom what, and each person's limits, held in
/// <c>records/&lt;slug&gt;.json</c>. <c>show</c> prints records as text (FR-009); <c>check</c> lists the findings of the
/// full save, or with <c>--draft</c> of migration 010's draft (FR-007); <c>draft</c> starts that draft (FR-008); and
/// <c>edited</c> is the <c>PostToolUse</c> hook that checks every record right after an edit and refuses a bad one in one
/// line with exit code 2 (FR-006).
/// </summary>
/// <remarks>
/// <c>edited</c> runs in a hook and must never get in the way of an edit it does not refuse, so it catches every failure
/// itself and exits 0; the other subcommands fail with one line, as every command does (<see cref="NeverFails"/> is
/// false because it covers the whole command).
/// </remarks>
public sealed partial class RecordsCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "records";

    /// <inheritdoc />
    public string Summary => "who calls whom what, and limits: dmc records show|check|draft <campaign>, and the edited hook";

    /// <summary>False: only <c>edited</c> never fails, and it handles that itself.</summary>
    public bool NeverFails => false;

    /// <summary>The text <c>--help</c> prints.</summary>
    static readonly string Help = """
usage: dmc records show <campaign> [PERSON ...] [--card | --sheet | --all] [--folder PATH]
       dmc records check <campaign> [--draft] [--folder PATH]
       dmc records draft <campaign> [--folder PATH]
       dmc records edited            (the PostToolUse hook; reads the hook's JSON on stdin)

Who calls whom what, and each person's limits, from the campaign's records/<person>.json.
  show    the records as text: what each person is called and by whom, relationships, their words for
          others, limits, facts and lists; with PERSON, only those (a full name, or a word of a name no one else has);
          --card, --sheet, --all print a person's sheet from their record with sources: to the end of the
          play card, to History, or all of it
  check   one line per finding of the full save, then the count; --draft checks migration 010's draft
  draft   start migration 010's draft beside the latest backup, from canon's People (also: …) items
  edited  check every record after an edit; a bad one is refused in one line on stderr, exit 2
""".Replace("\r\n", "\n");

    /// <summary>Runs a subcommand.</summary>
    /// <param name="args">The arguments after <c>records</c>.</param>
    /// <param name="io">Standard input is the hook's JSON for <c>edited</c>; standard output gets the lines.</param>
    /// <returns>0; 2 for an edit <c>edited</c> refuses.</returns>
    /// <exception cref="DmcException">A usage mistake (exit 2), or a campaign, person or draft that cannot be used (exit 1).</exception>
    public int Run(string[] args, Io io)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        string sub = args[0];
        if (sub == "edited")
            return Edited(io);
        if (sub is not ("show" or "check" or "draft"))
            throw new DmcException($"unknown subcommand \"{sub}\" (show, check, draft, edited)", 2);
        var given = args[1..].ToList();
        string? folderOption = null;
        int at = given.IndexOf("--folder");
        if (at >= 0)
        {
            if (at + 1 >= given.Count)
                throw new DmcException("--folder needs a path", 2);
            folderOption = given[at + 1];
            given.RemoveRange(at, 2);
        }
        if (given.Count == 0 || given[0].StartsWith("--", StringComparison.Ordinal))
            throw new DmcException($"{sub} needs a campaign: dmc records {sub} <campaign>", 2);
        string folder = DmcFolder.FromOption(folderOption);
        string adventures = Path.Combine(folder, "adventures");
        string campaign = CampaignExporter.FindCampaign(adventures, given[0]);
        string root = Path.Combine(adventures, campaign);
        var rest = given.Skip(1).ToArray();
        switch (sub)
        {
            case "show":
                var part = ShowPart(ref rest);
                foreach (string line in Show(folder, campaign, rest, part))
                    io.Out.WriteLine(line);
                return 0;
            case "check":
                bool draft = rest.Contains("--draft");
                if (rest.Any(a => a != "--draft"))
                    throw new DmcException($"unexpected \"{rest.First(a => a != "--draft")}\": dmc records check <campaign> [--draft]", 2);
                var findings = draft ? RecordCheck.Draft(folder, campaign, RecordDraft.PathFor(root, campaign)) : RecordCheck.Records(folder, campaign);
                foreach (string line in RecordCheck.Report(findings))
                    io.Out.WriteLine(line);
                return 0;
            default:
                if (rest.Length > 0)
                    throw new DmcException($"unexpected \"{rest[0]}\": dmc records draft <campaign>", 2);
                string path = RecordDraft.PathFor(root, campaign);
                var rows = RecordDraft.FromCanon(root);
                RecordDraft.Write(path, rows);
                io.Out.WriteLine($"draft: {Path.GetRelativePath(folder, path).Replace('\\', '/')}, {rows.Count} rows");
                return 0;
        }
    }

    /// <summary>
    /// The text of the records asked for: every record, or those the names give (<see cref="Matches"/>), each once, in
    /// the order named. A name that fits several records gives them all; a name that fits none is named on one line at
    /// the end, after the others' records, so one name the resume gets wrong does not cost the rest (spec 032: SC-003's
    /// base resume needed six calls).
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="names">The persons (<see cref="Matches"/>); empty for all.</param>
    /// <exception cref="DmcException">The campaign has no records, or no name fits any record.</exception>
    /// <param name="part">The part of each record's print (<c>--card</c>, <c>--sheet</c>, <c>--all</c>, spec 036 FR-009), or null for the text view.</param>
    static List<string> Show(string folder, string campaign, string[] names, PrintPart? part = null)
    {
        var index = RecordIndex.Build(folder, campaign);
        if (index.Records.Count == 0)
            throw new DmcException($"{campaign} has no records (records/ comes with migration 010)");
        var chosen = new List<PersonRecord>();
        var missing = new List<string>();
        if (names.Length == 0)
            chosen.AddRange(index.Records.Where(r => r.Campaign == campaign));
        foreach (string name in names)
        {
            var found = Matches(index, name);
            if (found.Count == 0)
                missing.Add($"\"{name.Trim()}\"");
            chosen.AddRange(found.Where(r => !chosen.Contains(r)));
        }
        if (chosen.Count == 0)
            throw new DmcException($"no record for {string.Join(", ", missing)}");
        var lines = new List<string>();
        foreach (var record in chosen)
        {
            if (lines.Count > 0)
                lines.Add("");
            lines.AddRange(part is { } p && record.Sheet is not null && record.HasSheetContent
                ? RecordSheet.Part(RecordSheet.Print(record, sources: true), p)
                : RecordText.Of(index, record));
        }
        if (missing.Count > 0)
            lines.AddRange(["", $"no record for {string.Join(", ", missing)} (dmc records show {campaign} prints everyone)"]);
        return lines;
    }

    /// <summary>
    /// The print part <c>show</c> was asked for, taken out of its arguments: <c>--card</c>, <c>--sheet</c> or <c>--all</c>
    /// (spec 036 FR-009); null for none.
    /// </summary>
    /// <param name="rest">The arguments after the campaign; the flag is removed.</param>
    /// <exception cref="DmcException">Two of the flags, or another option.</exception>
    static PrintPart? ShowPart(ref string[] rest)
    {
        var flags = rest.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToList();
        if (flags.Count > 1 || flags.Any(f => f is not ("--card" or "--sheet" or "--all")))
            throw new DmcException($"unexpected \"{flags.FirstOrDefault(f => f is not ("--card" or "--sheet" or "--all")) ?? flags[1]}\": dmc records show <campaign> [PERSON ...] [--card | --sheet | --all]", 2);
        rest = rest.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
        return flags.Count == 0 ? null : flags[0] switch { "--card" => PrintPart.Card, "--sheet" => PrintPart.Sheet, _ => PrintPart.All };
    }

    /// <summary>A run of characters that are not letters or digits: what separates the words of a name.</summary>
    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotWord();

    /// <summary>The words of a name, lower case, without quotes or other punctuation (<c>Shalefire "Shale" Stoutheart</c>: shalefire, shale, stoutheart).</summary>
    /// <param name="name">A name.</param>
    static string[] Words(string name) =>
        NotWord().Split(name.ToLowerInvariant()).Where(w => w.Length > 0).ToArray();

    /// <summary>
    /// The records a name gives: the one whose full name it is (ignoring case); else the one whose file it names
    /// (<c>kibil-rornson</c>, or a sheet's <c>npc-kibil-rornson.md</c>); else every record whose name holds each of its
    /// words, ignoring case, quotes and punctuation (<c>Shale</c>, <c>Emmajeen Kole</c>). None when nothing fits.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name as typed.</param>
    static List<PersonRecord> Matches(RecordIndex index, string name)
    {
        string wanted = name.Trim();
        if (index.Records.FirstOrDefault(r => r.Person.Equals(wanted, StringComparison.OrdinalIgnoreCase)) is { } exact)
            return [exact];
        string stem = wanted.ToLowerInvariant();
        foreach (string end in new[] { ".json", ".md" })
            if (stem.EndsWith(end, StringComparison.Ordinal))
                stem = stem[..^end.Length];
        foreach (string start in new[] { "npc-", "pc-" })
            if (stem.StartsWith(start, StringComparison.Ordinal))
                stem = stem[start.Length..];
        if (index.Records.FirstOrDefault(r => RecordFiles.Slug(r.Person) == stem + ".json") is { } file)
            return [file];
        // The file name from before T063 only when no record has the name as it is now (T083).
        if (stem.Length > 0 && index.Records.FirstOrDefault(r => RecordFiles.AsciiSlug(r.Person) == stem + ".json") is { } old)
            return [old];
        var words = Words(stem);
        return words.Length == 0 ? [] : [.. index.Records.Where(r => words.All(Words(r.Person).Contains))];
    }

    /// <summary>
    /// The hook: decides the edit on standard input (<see cref="RecordEdit.Decide"/>). A refusal is one line on standard
    /// error and exit 2, which Claude Code hands back to DMC; a note is printed as the hook's <c>additionalContext</c>.
    /// Any failure prints nothing and exits 0.
    /// </summary>
    /// <param name="io">Standard input is the hook's JSON.</param>
    static int Edited(Io io)
    {
        try
        {
            string input = io.InIsTerminal ? "" : io.In.ReadToEnd();
            var outcome = RecordEdit.Decide(input, Environment.GetEnvironmentVariable(DmcFolder.ProjectVariable));
            if (outcome.Refusal is { } refusal)
            {
                io.Err.WriteLine(refusal);
                return 2;
            }
            if (outcome.Note is { } note)
                io.Out.WriteLine(HookContext(note));
            return 0;
        }
        catch (Exception)
        {
            return 0; // a hook never gets in the way of an edit it does not refuse
        }
    }

    /// <summary>The hook's JSON that adds a note to DMC's context after a tool call.</summary>
    /// <param name="note">The note.</param>
    internal static string HookContext(string note)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteStartObject("hookSpecificOutput");
            writer.WriteString("hookEventName", "PostToolUse");
            writer.WriteString("additionalContext", note);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
