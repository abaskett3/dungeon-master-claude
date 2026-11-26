using Dmc.Maps;
using Dmc.Sheets;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc sheet [NAME ...] [--campaign NAME] [--party] [--history] [--open] [--check] [--folder PATH]</c> (spec 031,
/// issue #191): a printable HTML character sheet for the player characters, or the party, written from the campaign
/// files, so DMC never retypes a sheet from memory. Each box of the fixed layout is filled from one labelled field;
/// what a box may not print (Secrets, What they know, a companion's motives and history) is never read into the page.
/// A character whose files are out of shape is refused, with what to fix, instead of exported wrongly.
/// </summary>
/// <remarks>
/// Output follows <c>dmc campaign verify</c>: a written export prints its warnings and one line; <c>--check</c> prints
/// its findings and exits 0; a refused export prints the same findings on standard output, then one line on standard
/// error, and exits 1; a usage mistake is one line and exit 2. The work is in <c>Dmc.Sheets</c>.
/// </remarks>
/// <param name="today">The date the page is stamped with; the local date in the real program, a fixed one in a test.</param>
/// <param name="open">Opens the page for <c>--open</c>: <see cref="Browser.Open"/> in the real program, a recorder in a test.</param>
public sealed class SheetCommand(Func<DateOnly>? today = null, Action<string>? open = null) : ICommand
{
    /// <summary>The date of the export.</summary>
    readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));

    /// <summary>Opens a URL in the browser.</summary>
    readonly Action<string> _open = open ?? Browser.Open;

    /// <inheritdoc />
    public string Name => "sheet";

    /// <inheritdoc />
    public string Summary => "a printable character sheet from the campaign files: dmc sheet [NAME ...] [--party] [--history] [--open] [--check]";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc sheet [NAME ...] [--campaign NAME] [--party] [--history] [--open] [--folder PATH]
       dmc sheet --check [NAME ...] [--campaign NAME] [--party] [--folder PATH]

Writes a printable character sheet, one per page, from the campaign's sheets and game-state.md, to
exports/sheets/<campaign>/ in the DMC folder: <sheet>.html for one character, party.html for --party,
sheets.html for several. A new export replaces the old file. Only what the player character could know is
printed: never Secrets, What they know, or a companion's motives or history. A character whose files are out
of shape is refused, with one line for each thing to fix.

  NAME        a sheet's title or file name: a player character or someone in the party
              (default: every player character, character-sheets/pc-*.md)
  --campaign  a campaign folder in adventures/ (default: the active campaign)
  --party     every character with a block under Party status in game-state.md
  --history   add a player character's History (and <sheet>-history.md)
  --open      open the page in the default browser
  --check     only check the files and print what to fix, writing nothing
  --folder    the DMC folder (default: the one dmc folder prints)
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments, runs the export or the check and prints its lines.</summary>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    /// <param name="io">Standard output gets the lines; standard error the refusal.</param>
    /// <returns>0 for a written export, a check or <c>--help</c>; 1 for a refused export.</returns>
    /// <exception cref="DmcException">A usage mistake (exit 2), or a campaign, name or file that cannot be used (exit 1).</exception>
    public int Run(string[] args, Io io)
    {
        var request = SheetArguments.Parse(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        string folder = DmcFolder.FromOption(request.Folder);
        var outcome = SheetExport.Run(folder, request, _today());
        foreach (string line in outcome.Lines)
            io.Out.WriteLine(line);
        if (outcome.Refused is { } refused)
        {
            io.Err.WriteLine($"dmc {Name}: {refused}");
            return 1;
        }
        if (request.Open && outcome.Written is { } written)
            _open(new Uri(written).AbsoluteUri);
        return 0;
    }
}
