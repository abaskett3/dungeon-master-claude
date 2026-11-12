using System.Globalization;
using Dmc.Campaign;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc campaign export|import|show|standard|backups|backup|verify|restore|close NAME-or-FILE</c>: moves a whole campaign in and out
/// of one file. <c>export</c> writes a campaign folder (and the general player options) to a ZIP with a manifest of sizes
/// and checksums; <c>import</c> checks such a file completely, then creates the campaign all or nothing under a name that
/// is free; <c>show</c> says what a file holds and writes nothing. <c>standard</c>, <c>backups</c>, <c>backup</c>, <c>verify</c> and <c>restore</c> are the exact steps of bringing a campaign's files up to the kit's standard (spec 005): where it stands, a checked backup, the no-loss check, and the way back. <c>close</c> closes a finished campaign for good (spec 010): the mark, a permanent closing backup and its copy in <c>exports/</c>; every writing verb then refuses the campaign.
/// </summary>
/// <remarks>
/// Arguments are read the way Python's <c>argparse</c> reads them (see <see cref="CampaignArguments"/>), with its messages,
/// but a mistake is one line and exit code 2, never the usage text. The work is in <c>Dmc.Campaign</c>, which takes the
/// DMC folder as a parameter so a test can aim it at a temporary folder; only this class finds the folder, with <see cref="DmcFolder.Require"/> (a command run where there is no DMC folder fails with <c>no DMC folder: run dmc install</c>).
/// </remarks>
public sealed class CampaignCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "campaign";

    /// <inheritdoc />
    public string Summary => "export or import a whole campaign as one file, bring its files up to date, or close it for good: dmc campaign export|import|show|standard|backups|backup|verify|restore|close NAME-or-FILE";

    /// <summary>The text <c>--help</c> prints: usage, what each verb does and what the options mean.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc campaign export NAME [--out PATH] [--force]
       dmc campaign import FILE [--as NAME]
       dmc campaign show FILE
       dmc campaign standard [NAME] [--init]
       dmc campaign backups NAME
       dmc campaign backup NAME [--prune oldest|old]
       dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]
       dmc campaign restore NAME [--from FILE] [--yes]
       dmc campaign close NAME [--yes]

Moves a whole campaign in and out of one ZIP file: a backup, a copy under another name, or a move to another computer.
Brings a campaign's files up to the kit's standard with DMC, safely: a backup first, a check that nothing was lost, and
a way back. Closes a finished campaign for good.

  export    writes adventures/NAME, and the general adventures/player-options.md if there is one, to a file. Nothing in
            adventures/ is changed. The file name is NAME-YYYYMMDDHHmm.zip (local time) in exports/ in the DMC folder.
            The campaign's backups/ folder is left out.
  import    checks the whole file, then creates adventures/<name> from it, all or nothing. It never changes an existing
            campaign, never replaces an existing adventures/player-options.md and never changes the active campaign.
  show      prints what a file holds (name, date, files, module) and writes nothing.
  standard  says whether the campaign is closed, or whether its files are current, behind the kit's standard (and
            which migrations are pending), ahead of it, or in the middle of an unfinished migration. Changes nothing,
            except --init.
  backups   lists the campaign's backups, oldest first, and ends with the question to ask the player when it is time
            to remove old ones.
  backup    writes a checked backup to adventures/NAME/backups/ and, when a migration is pending, marks it as under way.
  verify    checks that every line of the backup is in the migrated campaign or accounted for, and that each pending
            migration's checks pass; then records the new standard. Changes nothing when it fails.
  restore   puts the campaign back as a backup holds it, keeping backups/. Without --yes, prints the plan only.
  close     closes a current campaign for good: marks it closed, writes a checked closing backup that is never pruned
            to adventures/NAME/backups/ and a copy of it to exports/. Nothing changes a closed campaign again; a
            sequel continues its story. Without --yes, prints the plan only.

  NAME          a folder under adventures/ (standard: default the active campaign)
  FILE          an export file; its name does not matter, the campaign name is read from inside it
  --out         export: an existing folder (the default file name is used inside it), or a file path ending in .zip
                (default: exports/ in the DMC folder, made when needed; never inside adventures/)
  --force       export: replace an existing file named by --out; the default name is never replaced, the next free -2, -3 ... is used
  --as          import: the folder name to import under (lowercase letters, digits and hyphens; default: the name inside the file)
  --init        standard: record the kit's standard for a new campaign that has no record
  --prune       backup: after the backup, remove the oldest backup (oldest) or all but the latest two older ones (old);
                only on the player's yes to the question backups asks
  --backup      verify: the backup to compare with (default: the one of the unfinished migration, else the latest)
  --accounting  verify: the accounting file (default: backups/<backup file>.accounting.tsv)
  --list        verify: print what is still to account for, and write nothing
  --all         verify: print every unaccounted line, not the first 40
  --from        restore: the backup to restore (default: the one of the unfinished migration, else the latest)
  --yes         restore, close: do it; without it nothing changes

An export holds the whole campaign, spoilers included (story/dm-notes.md): do not open it before you have played the story.
A published module is not exported; ingest it again from your own copy.
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments with <see cref="CampaignArguments.Parse"/> and hands the request to its verb; <c>--help</c> prints <see cref="Help"/> instead.</summary>
    /// <param name="args">The arguments after <c>campaign</c>.</param>
    /// <param name="io">Standard output gets the result lines, or the help text.</param>
    /// <returns>0 on success or <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2; every other failure exits 1.</returns>
    public int Run(string[] args, Io io)
    {
        switch (CampaignArguments.Parse(args))
        {
            case null:
                io.Out.WriteLine(Help);
                return 0;
            case ExportRequest export:
                return RunExport(export, io);
            case ImportRequest import:
                return RunImport(import, io);
            case ShowRequest show:
                return RunShow(show, io);
            case StandardRequest standard:
                return RunStandard(standard, io);
            case BackupsRequest backups:
                return RunBackups(backups, io);
            case BackupRequest backup:
                return RunBackup(backup, io);
            case VerifyRequest verify:
                return RunVerify(verify, io);
            case RestoreRequest restore:
                return RunRestore(restore, io);
            case CloseRequest close:
                return RunClose(close, io);
            default:
                throw new InvalidOperationException("unknown campaign request");
        }
    }

    /// <summary>Writes the campaign to a file and prints the contract's one line.</summary>
    /// <param name="request">The parsed <c>export</c> request.</param>
    /// <param name="io">Standard output gets the result line.</param>
    static int RunExport(ExportRequest request, Io io)
    {
        var result = CampaignExporter.Export(DmcFolder.Require(), request);
        string options = result.GeneralOptions ? " and the general player options" : "";
        io.Out.WriteLine($"exported {request.Name}: {result.Files} files{options} to {result.Archive}");
        return 0;
    }

    /// <summary>Creates a campaign from a file and prints the contract's lines.</summary>
    /// <param name="request">The parsed <c>import</c> request.</param>
    /// <param name="io">Standard output gets the result lines.</param>
    static int RunImport(ImportRequest request, Io io)
    {
        var result = CampaignImporter.Import(DmcFolder.Require(), request, io.Err);
        io.Out.WriteLine($"imported {result.Name}: {result.Files} files to {result.Folder}");
        if (result.LeftOut > 0)
            io.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"left out backups/ ({result.LeftOut} files): an export made by an older dmc held it, and backups stay with their campaign"));
        switch (result.Options)
        {
            case OptionsOutcome.Written:
                io.Out.WriteLine("general player options: written to adventures/player-options.md");
                break;
            case OptionsOutcome.Kept:
                io.Out.WriteLine("general player options: kept your existing adventures/player-options.md");
                break;
        }
        // The same module state that "show" prints, so the two commands cannot disagree about it.
        if (result.Module is { Kind: ModuleKind.Named, Installed: false } missing)
            io.Out.WriteLine($"module {missing.Folder} is not in modules/: ingest it from your own copy before play");
        else if (result.Module.Kind == ModuleKind.Unnamed)
            io.Out.WriteLine("this is a module campaign, but its module-progress.md names no module folder");
        io.Out.WriteLine($"to play it, start a session and ask DMC to resume {result.Name}");
        if (ImportStandardLine(DmcFolder.Require(), result.Name) is { } standard)
            io.Out.WriteLine(standard);
        return 0;
    }

    /// <summary>The extra line an import prints when the new campaign is not on the kit's standard (research R12), or is closed (spec 010 FR-018); null when it is current.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The imported campaign.</param>
    internal static string? ImportStandardLine(string root, string name)
    {
        StandardStatus status;
        try
        {
            status = StandardRecord.Status(root, name);
        }
        catch (DmcException)
        {
            return null; // the import itself succeeded; resume reports an unreadable record or migration
        }
        return status.State switch
        {
            StandardState.Closed => ClosedCampaign.Refusal(name),
            StandardState.Behind => string.Create(CultureInfo.InvariantCulture,
                $"{name} is behind (standard {status.Campaign}, kit {status.Kit}): ask DMC to bring it up to date"),
            StandardState.Ahead => string.Create(CultureInfo.InvariantCulture,
                $"{name} is ahead (standard {status.Campaign}, kit {status.Kit}): update dmc"),
            _ => null,
        };
    }

    /// <summary>Prints where a campaign stands, or records the kit's standard with <c>--init</c>.</summary>
    /// <param name="request">The parsed <c>standard</c> request.</param>
    /// <param name="io">Standard output gets the one line.</param>
    static int RunStandard(StandardRequest request, Io io)
    {
        string root = DmcFolder.Require();
        string adventures = Path.Combine(root, "adventures");
        string named = request.Name ?? Dmc.Size.SizeReport.ActiveName(adventures)
            ?? throw new DmcException("no active campaign: name one (dmc campaign standard NAME)");
        Recover(root, named, io);
        string name = CampaignExporter.FindCampaign(adventures, named);
        string campaign = Path.Combine(adventures, name);
        if (request.Init)
        {
            ClosedCampaign.Refuse(root, campaign);
            if (File.Exists(Path.Combine(campaign, StandardRecord.FileName)))
                throw new DmcException($"{name} already has a standard: only dmc campaign verify changes it");
            int kit = MigrationSet.Read(root).Kit;
            StandardRecord.Write(campaign, kit);
            io.Out.WriteLine(string.Create(CultureInfo.InvariantCulture, $"initialised {name} at standard {kit}"));
            return 0;
        }
        io.Out.WriteLine(StandardRecord.Status(root, name).Line);
        return 0;
    }

    /// <summary>
    /// Puts back what an interrupted restore of the campaign left (<see cref="CampaignRestorer.Recover"/>), and prints what it
    /// did. Every migration verb runs it first, so a campaign never reads as current while its backups or its mark sit in a
    /// restore's staging folder.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's name, as given.</param>
    /// <param name="io">Standard output gets the line, if there is one.</param>
    static void Recover(string root, string name, Io io)
    {
        if (CampaignRestorer.Recover(root, name) is { } line)
            io.Out.WriteLine(line);
    }

    /// <summary>
    /// Lists a campaign's backups and, when it is due, the removal question. For a closed campaign the list ends with its
    /// closing backup instead (spec 010), and no question is asked: nothing may be removed from a closed campaign.
    /// </summary>
    /// <param name="request">The parsed <c>backups</c> request.</param>
    /// <param name="io">Standard output gets the count, the file names and the question or the closing backup.</param>
    static int RunBackups(BackupsRequest request, Io io)
    {
        string root = DmcFolder.Require();
        Recover(root, request.Name, io);
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, request.Name));
        var backups = BackupFolder.List(campaign, request.Name);
        io.Out.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{backups.Count} backups"));
        foreach (var backup in backups)
            io.Out.WriteLine(backup.FileName);
        if (ClosedCampaign.IsClosed(campaign))
        {
            // The mark's name only when the file is there: an imported closed campaign's mark names a backup its backups/ never got.
            string? named = ClosedCampaign.Read(campaign)?.Backup;
            string? closing = named is not null && File.Exists(Path.Combine(BackupFolder.Of(campaign), named))
                ? named
                : BackupFolder.Closing(campaign, request.Name).FirstOrDefault();
            io.Out.WriteLine(closing is null ? "closed: no closing backup in backups/" : $"closed: {BackupFolder.FolderName}/{closing}");
            return 0;
        }
        if (BackupFolder.AskLine(backups, BackupFolder.Marked(campaign, request.Name) is not null) is { } ask)
            io.Out.WriteLine(ask);
        return 0;
    }

    /// <summary>Takes a backup, pruning on the flag, and prints the contract's one line.</summary>
    /// <param name="request">The parsed <c>backup</c> request.</param>
    /// <param name="io">Standard output gets the result line.</param>
    static int RunBackup(BackupRequest request, Io io)
    {
        string root = DmcFolder.Require();
        Recover(root, request.Name, io);
        var result = CampaignBackup.Take(root, request.Name, request.Prune);
        string line = string.Create(CultureInfo.InvariantCulture,
            $"backed up {request.Name}: {result.Files} files to {BackupFolder.FolderName}/{result.FileName}");
        if (request.Prune != PruneMode.None)
            line += string.Create(CultureInfo.InvariantCulture, $"; removed {result.Removed} older");
        if (result.NotRemoved.Count > 0)
            line += "; could not remove " + string.Join(", ", result.NotRemoved);
        io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>Verifies a migration and prints the list, pass or fail lines.</summary>
    /// <param name="request">The parsed <c>verify</c> request.</param>
    /// <param name="io">Standard output gets the lines.</param>
    /// <exception cref="DmcException">The verification failed (after its lines are printed), or cannot run.</exception>
    static int RunVerify(VerifyRequest request, Io io)
    {
        string root = DmcFolder.Require();
        Recover(root, request.Name, io);
        var outcome = CampaignVerifier.Run(root, request.Name, request.Options);
        foreach (string line in outcome.Lines)
            io.Out.WriteLine(line);
        return outcome.Failure is null ? 0 : throw new DmcException("verification failed: " + outcome.Failure);
    }

    /// <summary>Plans or makes a restore and prints its lines.</summary>
    /// <param name="request">The parsed <c>restore</c> request.</param>
    /// <param name="io">Standard output gets the lines.</param>
    static int RunRestore(RestoreRequest request, Io io)
    {
        foreach (string line in CampaignRestorer.Run(DmcFolder.Require(), request.Name, request.From, request.Yes))
            io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>Plans or makes a close, after putting back an interrupted restore, and prints the contract's one line.</summary>
    /// <param name="request">The parsed <c>close</c> request.</param>
    /// <param name="io">Standard output gets the lines.</param>
    static int RunClose(CloseRequest request, Io io)
    {
        string root = DmcFolder.Require();
        Recover(root, request.Name, io);
        var plan = request.Yes ? CampaignCloser.Close(root, request.Name) : CampaignCloser.Plan(root, request.Name);
        string verb = request.Yes ? "closed" : "would close";
        io.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{verb} {plan.Name}: {plan.Files} files to {BackupFolder.FolderName}/{plan.Backup} and exports/{plan.Backup}"));
        return 0;
    }

    /// <summary>Prints what a file holds.</summary>
    /// <param name="request">The parsed <c>show</c> request.</param>
    /// <param name="io">Standard output gets the summary lines.</param>
    static int RunShow(ShowRequest request, Io io)
    {
        // Every line is built before any is printed, so a file that is refused prints only the refusal.
        foreach (string line in ExportSummary.Open(request.File).ShowLines(DmcFolder.Require()).ToList())
            io.Out.WriteLine(line);
        return 0;
    }
}
