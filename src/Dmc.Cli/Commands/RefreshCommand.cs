using Dmc.Install;
using Dmc.Kit;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc refresh [FOLDER | --all]</c>: rewrites the generated files of a DMC folder from the copy inside this <c>dmc</c>
/// (contract <c>dmc-refresh.md</c>, FR-013, FR-015) and nothing else: only the paths of the program's table are written,
/// only paths of the folder's old manifest are removed, a file edited by hand is copied to <c>.dmc/replaced/&lt;stamp&gt;/</c>
/// first, and the manifest is written last (<see cref="Refresher"/>). The folder is the one given, or with <c>--all</c> every
/// recorded folder that is still a DMC folder, or else the one <c>dmc folder</c> prints. It refuses first, before
/// anything else, inside a Claude Code session.
/// </summary>
public sealed class RefreshCommand : ICommand
{
    /// <summary>The machine it runs on; the real one unless a test passed another.</summary>
    readonly Machine? _machine;

    /// <summary>Makes the command; <see cref="CommandTable"/> uses the real machine.</summary>
    public RefreshCommand() { }

    /// <summary>Makes the command over a machine a test built.</summary>
    /// <param name="machine">The machine to use.</param>
    internal RefreshCommand(Machine machine) => _machine = machine;

    /// <inheritdoc />
    public string Name => "refresh";

    /// <inheritdoc />
    public string Summary => "bring a DMC folder's rules, skills, settings and templates up to date: dmc refresh [FOLDER | --all]";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc refresh [FOLDER | --all]

Rewrites the files dmc wrote into a DMC folder (CLAUDE.md, .claude/, templates/) from the copy inside this dmc. Your
adventures, feedback, modules, rules and exports are never touched. A generated file you edited by hand is kept under
.dmc/replaced/ before it is replaced.

  FOLDER  the DMC folder (default: the one "dmc folder" prints)
  --all   every DMC folder installed on this computer
""".Replace("\r\n", "\n");

    /// <summary>Refreshes the folder or folders and prints the contract's lines (nothing when nothing changed).</summary>
    /// <param name="args">The arguments after <c>refresh</c>.</param>
    /// <param name="io">Standard output gets at most two lines per folder.</param>
    /// <returns>0 on success, and for <c>--help</c>.</returns>
    /// <exception cref="DmcException">Inside a Claude Code session; the folder is not a DMC folder; there is no DMC folder; a path cannot be written; or a usage mistake (exit code 2).</exception>
    public int Run(string[] args, Io io)
    {
        var machine = _machine ?? Machine.Real();
        ClaudeSessionGuard.Refuse(machine.Environment);
        string? folder = null;
        bool all = false;
        foreach (string arg in args)
        {
            if (arg is "-h" or "--help")
            {
                io.Out.WriteLine(Help);
                return 0;
            }
            if (arg == "--all")
                all = true;
            else if (arg.StartsWith('-'))
                throw Argparse.Usage($"unrecognized arguments: {arg}");
            else if (folder is null)
                folder = arg;
            else
                throw Argparse.Usage($"unrecognized arguments: {arg}");
        }
        if (all && folder is not null)
            throw Argparse.Usage("a folder and --all cannot be used together");

        IEnumerable<string> targets;
        if (all)
        {
            targets = machine.Recorded().Where(DmcFolder.IsDmcFolder).ToList();
        }
        else if (folder is not null)
        {
            string full = Path.GetFullPath(folder, machine.CurrentFolder);
            targets = DmcFolder.IsDmcFolder(full) ? [full] : throw new DmcException($"{full} is not a DMC folder: run dmc install");
        }
        else
        {
            targets = [DmcFolder.Find(machine.Environment, machine.CurrentFolder, machine.ProgramFolder) ?? throw new DmcException(DmcFolder.NoFolder)];
        }

        foreach (string target in targets)
        {
            var result = Refresher.Refresh(target, machine.Files, machine.Version, machine.Now());
            foreach (string line in Refresher.Report("dmc refresh", target, result, machine.Version))
                io.Out.WriteLine(line);
        }
        return 0;
    }
}
