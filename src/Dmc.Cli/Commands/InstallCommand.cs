using Dmc.Install;
using Dmc.Kit;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc install [--path FOLDER] [--force]</c>: installs <c>dmc</c> for the current user and writes a DMC folder (contract
/// <c>dmc-install.md</c>, FR-007 to FR-010). It copies the running program and its native libraries into the program folder
/// and makes it reachable by name (<see cref="ProgramInstall"/>); asks for the folder (default <c>~/dmc</c>); writes the
/// generated files and <c>.dmc/manifest.json</c> there (<see cref="Refresher"/>); makes the player's folders empty if they are
/// missing, writing nothing in them; and records the folder in <c>dmc.json</c> beside the program. A development build
/// (<c>0.0.0-dev</c>) copies itself as the program only with <c>--force</c>. Over a folder that already has a manifest it
/// behaves as <c>dmc refresh</c>. It refuses first, before anything else, inside a Claude Code session.
/// </summary>
public sealed class InstallCommand : ICommand
{
    /// <summary>The machine's home folder, environment and the rest; the real machine unless a test passed another.</summary>
    readonly Machine? _machine;

    /// <summary>Makes the command; <see cref="CommandTable"/> uses the real machine.</summary>
    public InstallCommand() { }

    /// <summary>Makes the command over a machine a test built.</summary>
    /// <param name="machine">The machine to use.</param>
    internal InstallCommand(Machine machine) => _machine = machine;

    /// <inheritdoc />
    public string Name => "install";

    /// <inheritdoc />
    public string Summary => "install dmc and write a DMC folder to play in: dmc install [--path FOLDER] [--force]";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc install [--path FOLDER] [--force]

Installs dmc for you and writes a DMC folder, the folder you play in, with the rules, skills and settings DMC needs.
It asks where (Enter takes the default, ~/dmc); --path answers for you. Your adventures, feedback, modules and
rules are never written by it. Run it again, or dmc refresh, to bring a folder up to date.

  --path   the DMC folder, without asking
  --force  also install this program when it is a development build (a release is always installed)
""".Replace("\r\n", "\n");

    /// <summary>Does the install, as the class summary describes.</summary>
    /// <param name="args">The arguments after <c>install</c>.</param>
    /// <param name="io">Standard output gets the result lines (and the question); standard input answers it when it is a terminal.</param>
    /// <returns>0 on success, and for <c>--help</c>.</returns>
    /// <exception cref="DmcException">Every failure of the contract's table: inside a Claude Code session, the folder is inside the repository or is a file, a file in the way, a path that cannot be written, a PATH that cannot be changed; or a usage mistake (exit code 2).</exception>
    public int Run(string[] args, Io io)
    {
        var machine = _machine ?? Machine.Real();
        ClaudeSessionGuard.Refuse(machine.Environment);
        var (path, force, help) = Parse(args);
        if (help)
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        string programFolder = ProgramInstall.ProgramFolder(machine.Home, machine.Environment, machine.Windows);
        ProgramInstall.RemoveLeftovers(programFolder);

        string folder = ChooseFolder(path, io, machine);
        CheckFolder(folder);
        Refresher.Preflight(folder, machine.Files);

        bool onProgram = ProgramInstall.SameFolder(machine.ProgramFolder, programFolder);
        bool copy = ProgramInstall.ShouldCopy(machine.Version, force, machine.ProgramFolder, programFolder);
        if (copy)
        {
            ProgramInstall.Copy(machine.RunningExecutable, programFolder, machine.Windows);
            onProgram = true;
        }
        if (onProgram && !machine.Windows)
            ProgramInstall.Link(programFolder, machine.Home);

        bool refreshing = DmcFolder.IsDmcFolder(folder);
        var result = Refresher.Refresh(folder, machine.Files, machine.Version, machine.Now());
        foreach (string name in new[] { "adventures", "feedback", "modules", "rules" })
            MakeFolder(Path.Combine(folder, name));
        DmcFolder.Record(onProgram ? programFolder : machine.ProgramFolder, folder);

        if (copy)
            io.Out.WriteLine($"dmc install: installed dmc {machine.Version} in {programFolder}");
        if (refreshing)
        {
            foreach (string line in Refresher.Report("dmc install", folder, result, machine.Version))
                io.Out.WriteLine(line);
        }
        io.Out.WriteLine($"dmc install: DMC folder ready at {folder}");

        // Windows: the PATH is changed last, so a failure leaves the folder written, as the contract says.
        if (copy && machine.Windows)
            AddToPath(programFolder, machine);
        if (copy)
        {
            bool reachable = machine.Windows || ProgramInstall.LinkFolderOnPath(machine.Home, machine.Environment("PATH"));
            io.Out.WriteLine(!reachable
                ? "dmc install: add ~/.local/bin to your PATH (for example in ~/.profile), then run dmc play"
                : "dmc install: open a new terminal, then run dmc play");
        }
        return 0;
    }

    /// <summary>Reads the arguments: <c>--path FOLDER</c> or <c>--path=FOLDER</c>, <c>--force</c>, <c>-h</c> and <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>install</c>.</param>
    /// <exception cref="DmcException">An unknown argument, or <c>--path</c> without a folder (exit code 2).</exception>
    static (string? Path, bool Force, bool Help) Parse(string[] args)
    {
        string? path = null;
        bool force = false, help = false;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--force")
                force = true;
            else if (arg is "-h" or "--help")
                help = true;
            else if (arg == "--path")
                path = i + 1 < args.Length ? args[++i] : throw Argparse.Usage("argument --path: expected one argument");
            else if (arg.StartsWith("--path=", StringComparison.Ordinal))
                path = arg["--path=".Length..];
            else
                throw Argparse.Usage($"unrecognized arguments: {arg}");
        }
        if (path is { Length: 0 })
            throw Argparse.Usage("argument --path: expected one argument");
        return (path, force, help);
    }

    /// <summary>
    /// The DMC folder: <c>--path</c> if given. Otherwise the answer to <c>DMC folder [&lt;home&gt;/dmc]: </c>, read from standard
    /// input when that is a terminal, else (Linux, under <c>curl | sh</c>) from the terminal itself, else the default
    /// without asking. An empty answer is the default; a leading <c>~</c> is the home folder.
    /// </summary>
    /// <param name="path">The <c>--path</c> argument, or null.</param>
    /// <param name="io">Where the question is written and, for a terminal, read.</param>
    /// <param name="machine">The machine: the home folder, the current folder and the terminal.</param>
    static string ChooseFolder(string? path, Io io, Machine machine)
    {
        string fallback = Path.Combine(machine.Home, "dmc");
        string? answer = path;
        if (answer is null)
        {
            using var terminal = io.InIsTerminal || machine.Windows ? null : machine.OpenTerminal();
            TextReader? reader = io.InIsTerminal ? io.In : terminal;
            if (reader is not null)
            {
                io.Out.Write($"DMC folder [{fallback}]: ");
                io.Out.Flush();
                answer = reader.ReadLine();
            }
        }
        answer = answer?.Trim();
        if (string.IsNullOrEmpty(answer))
            return fallback;
        if (answer == "~" || answer.StartsWith("~/", StringComparison.Ordinal) || answer.StartsWith("~\\", StringComparison.Ordinal))
            answer = machine.Home + answer[1..];
        return Path.GetFullPath(answer, machine.CurrentFolder);
    }

    /// <summary>Refuses a folder that is a file, or that is inside a checkout of the source repository (a <c>kit/dm-rules.md</c> in it or above it).</summary>
    /// <param name="folder">The chosen folder, as a full path.</param>
    /// <exception cref="DmcException"><c>&lt;folder&gt; is a file, not a folder</c> or <c>&lt;folder&gt; is inside the DMC source repository: choose another folder</c>.</exception>
    static void CheckFolder(string folder)
    {
        if (File.Exists(folder))
            throw new DmcException($"{folder} is a file, not a folder");
        for (string? up = folder; up is not null; up = Path.GetDirectoryName(up))
        {
            if (File.Exists(Path.Combine(up, "kit", "dm-rules.md")))
                throw new DmcException($"{folder} is inside the DMC source repository: choose another folder");
        }
    }

    /// <summary>Makes a folder if it is missing; writes nothing in it.</summary>
    /// <param name="folder">The folder.</param>
    /// <exception cref="DmcException"><c>cannot write &lt;folder&gt;: &lt;reason&gt;</c>.</exception>
    static void MakeFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {folder}: {e.Message}");
        }
    }

    /// <summary>Puts the program folder on the user's Windows <c>Path</c> (<see cref="ProgramInstall.AddToUserPath"/>).</summary>
    /// <param name="programFolder">The program folder.</param>
    /// <param name="machine">The machine: its <see cref="Machine.UserPath"/>, or the real registry when it has none.</param>
    /// <returns>True when the folder was added; false when it was already there.</returns>
    static bool AddToPath(string programFolder, Machine machine) =>
        ProgramInstall.AddToUserPath(programFolder, machine.UserPath ?? RealUserPath());

    /// <summary>The user's real <c>Path</c>; only called on Windows.</summary>
    static IUserPath RealUserPath() =>
        OperatingSystem.IsWindows() ? new RegistryUserPath() : throw new InvalidOperationException("the user Path is a Windows setting");
}
