using System.Text.Json;
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
public sealed class RecordsCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "records";

    /// <inheritdoc />
    public string Summary => "who calls whom what, and limits: dmc records show|check|draft <campaign>, and the edited hook";

    /// <summary>False: only <c>edited</c> never fails, and it handles that itself.</summary>
    public bool NeverFails => false;

    /// <summary>The text <c>--help</c> prints.</summary>
    static readonly string Help = """
usage: dmc records show <campaign> [PERSON ...] [--folder PATH]
       dmc records check <campaign> [--draft] [--folder PATH]
       dmc records draft <campaign> [--folder PATH]
       dmc records edited            (the PostToolUse hook; reads the hook's JSON on stdin)

Who calls whom what, and each person's limits, from the campaign's records/<person>.json.
  show    the records as text: what each person is called and by whom, relationships, their words for
          others, limits; with PERSON, only those (a full name, or a word of a name no one else has)
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
                foreach (string line in Show(folder, campaign, rest))
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

    /// <summary>The text of the records asked for: every record, or the persons named.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name.</param>
    /// <param name="names">The persons, by full name or a word of a name no other record has; empty for all.</param>
    /// <exception cref="DmcException">The campaign has no records, or a name fits no record or more than one.</exception>
    static List<string> Show(string folder, string campaign, string[] names)
    {
        var index = RecordIndex.Build(folder, campaign);
        if (index.Records.Count == 0)
            throw new DmcException($"{campaign} has no records (records/ comes with migration 010)");
        var chosen = names.Length == 0 ? index.Records.Where(r => r.Campaign == campaign).ToList() : names.Select(n => Find(index, n)).ToList();
        var lines = new List<string>();
        foreach (var record in chosen)
        {
            if (lines.Count > 0)
                lines.Add("");
            lines.AddRange(RecordText.Of(index, record));
        }
        return lines;
    }

    /// <summary>The record a name means: a full name, or a word of one record's name.</summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name as typed.</param>
    /// <exception cref="DmcException">No record fits, or more than one does.</exception>
    static PersonRecord Find(RecordIndex index, string name)
    {
        string wanted = name.Trim();
        if (index.Records.FirstOrDefault(r => r.Person.Equals(wanted, StringComparison.OrdinalIgnoreCase)) is { } exact)
            return exact;
        var partial = index.Records.Where(r => r.Person.Split(' ').Any(w => w.Equals(wanted, StringComparison.OrdinalIgnoreCase))).ToList();
        return partial.Count switch
        {
            1 => partial[0],
            0 => throw new DmcException($"no record for \"{wanted}\""),
            _ => throw new DmcException($"\"{wanted}\" could be {string.Join(" or ", partial.Select(p => p.Person))}: use the full name"),
        };
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
