using Dmc.Campaign;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc campaign export|import|show NAME-or-FILE [--out PATH] [--force] [--as NAME]</c>: moves a whole campaign in and out
/// of one file. <c>export</c> writes a campaign folder (and the general player options) to a ZIP with a manifest of sizes
/// and checksums; <c>import</c> checks such a file completely, then creates the campaign all or nothing under a name that
/// is free; <c>show</c> says what a file holds and writes nothing.
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
    public string Summary => "export or import a whole campaign as one file: dmc campaign export|import|show NAME-or-FILE [--out PATH] [--force] [--as NAME]";

    /// <summary>The text <c>--help</c> prints: usage, what each verb does and what the options mean.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc campaign export NAME [--out PATH] [--force]
       dmc campaign import FILE [--as NAME]
       dmc campaign show FILE

Moves a whole campaign in and out of one ZIP file: a backup, a copy under another name, or a move to another computer.

  export  writes adventures/NAME, and the general adventures/player-options.md if there is one, to a file. Nothing in
          adventures/ is changed. The file name is NAME-YYYYMMDDHHmm.zip (local time) in exports/ in the DMC folder.
  import  checks the whole file, then creates adventures/<name> from it, all or nothing. It never changes an existing
          campaign, never replaces an existing adventures/player-options.md and never changes the active campaign.
  show    prints what a file holds (name, date, files, module) and writes nothing.

  NAME    a folder under adventures/
  FILE    an export file; its name does not matter, the campaign name is read from inside it
  --out   export: an existing folder (the default file name is used inside it), or a file path ending in .zip
          (default: exports/ in the DMC folder, made when needed; never inside adventures/)
  --force export: replace an existing file named by --out; the default name is never replaced, the next free -2, -3 ... is used
  --as    import: the folder name to import under (lowercase letters, digits and hyphens; default: the name inside the file)

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
