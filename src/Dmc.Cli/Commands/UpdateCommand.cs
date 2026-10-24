using Dmc.Install;
using Dmc.Kit;

namespace Dmc.Commands;

/// <summary>
/// What <c>dmc update</c> takes from outside, in one place so that a test can replace each part (the real program uses
/// <see cref="Real"/>): the keys the checksum list's signature is checked against, how a program is run, how the HTTP requests are
/// carried, and a hook inside the swap. Nothing here is reachable from the environment or a setting (FR-028).
/// </summary>
/// <param name="Keys">The keys that may have signed a release (<see cref="ReleaseKeys.Built"/> in the real program).</param>
/// <param name="Run">Runs a program and captures its output (<see cref="ProgramSwap.Start"/> in the real program).</param>
/// <param name="Http">What carries the requests to GitHub; null for the real one.</param>
/// <param name="AfterMove">Called after each file of the swap is moved; a test throws from it to fail the swap in the middle. Null in the real program.</param>
internal sealed record UpdateSeams(ReleaseKeys Keys, ProgramSwap.Runner Run, HttpMessageHandler? Http = null, Action<int>? AfterMove = null)
{
    /// <summary>The seams of the real program: the built-in keys, real processes, real HTTP.</summary>
    public static UpdateSeams Real { get; } = new(ReleaseKeys.Built, ProgramSwap.Start);
}

/// <summary>
/// <c>dmc update</c> (contract <c>dmc-update.md</c>, FR-014, FR-026 to FR-028, research R9 and R16): replaces the installed
/// program in place with the latest GitHub release, then has the new program rewrite the generated files. It refuses first
/// inside a Claude Code session, and a development build. It reads the latest release; stops when that is not newer than this
/// program (still refreshing any stale DMC folder); downloads the checksum list and its signature and checks the signature
/// against the keys built into <c>dmc</c>; downloads the archive for this machine and checks it against the signed list and
/// the digest the API reports; unpacks it beside the program, runs the new <c>dmc --version</c>; swaps the files with
/// <c>*.old</c> renames (moving back on a failure); and runs the new <c>dmc refresh --all</c>. Before the swap nothing changes.
/// Player files are never touched. The signature and hash checks cannot be turned off.
/// </summary>
public sealed class UpdateCommand : ICommand
{
    /// <summary>The machine it runs on; the real one unless a test passed another.</summary>
    readonly Machine? _machine;

    /// <summary>What it takes from outside; the real ones unless a test passed others.</summary>
    readonly UpdateSeams _seams;

    /// <summary>Makes the command; <see cref="CommandTable"/> uses the real machine, keys and network.</summary>
    public UpdateCommand() => _seams = UpdateSeams.Real;

    /// <summary>Makes the command over a machine and seams a test built.</summary>
    /// <param name="machine">The machine to use.</param>
    /// <param name="seams">The keys, the program runner, the HTTP handler and the swap hook to use.</param>
    internal UpdateCommand(Machine machine, UpdateSeams seams)
    {
        _machine = machine;
        _seams = seams;
    }

    /// <inheritdoc />
    public string Name => "update";

    /// <inheritdoc />
    public string Summary => "replace dmc with the latest release and refresh your DMC folders: dmc update";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc update

Replaces dmc with the latest release from GitHub, then rewrites the rules, skills, settings and templates of your DMC
folders. The release's checksums must carry a valid signature from the DMC release key, and the download must match
them, or nothing is installed. Your adventures, feedback, modules, rules and exports are never touched. Run it in a
terminal, not inside a Claude Code session.
""".Replace("\r\n", "\n");

    /// <summary>Does the update, as the class summary describes.</summary>
    /// <param name="args">None are accepted (<c>-h</c> and <c>--help</c> print the help).</param>
    /// <param name="io">Standard output gets the result lines, and the refresh's.</param>
    /// <returns>0 on success, when this is already the latest release, and for <c>--help</c>.</returns>
    /// <exception cref="DmcException">Every error of the contract's table, and a usage mistake (exit code 2).</exception>
    public int Run(string[] args, Io io)
    {
        var machine = _machine ?? Machine.Real();
        ClaudeSessionGuard.Refuse(machine.Environment);
        foreach (string arg in args)
        {
            if (arg is "-h" or "--help")
            {
                io.Out.WriteLine(Help);
                return 0;
            }
            throw Argparse.Usage($"unrecognized arguments: {arg}");
        }

        if (machine.Version == ProgramInstall.DevVersion || ReleaseSource.ParseVersion(machine.Version) is null)
            throw new DmcException($"this is a development build ({machine.Version}): install a release to update");
        ProgramInstall.RemoveLeftovers(machine.ProgramFolder);

        using var source = new ReleaseSource(machine.Environment, machine.Version, _seams.Http);
        var release = source.Latest();
        if (release.Numbers.CompareTo(ReleaseSource.ParseVersion(machine.Version)!.Value) <= 0)
        {
            io.Out.WriteLine($"dmc update: dmc {machine.Version} is already the latest");
            RefreshStale(machine, io);
            return 0;
        }

        string staging = Path.Combine(machine.ProgramFolder, ".update-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            try
            {
                Directory.CreateDirectory(staging);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new DmcException($"cannot write {machine.ProgramFolder}: {e.Message}");
            }
            string rid = ReleaseSource.Rid(machine.Windows);
            string download = Path.Combine(staging, "download"), unpacked = Path.Combine(staging, "new");
            Directory.CreateDirectory(download);
            string archive = source.Download(release, rid, download, _seams.Keys);
            ReleaseSource.Unpack(archive, unpacked);

            string exeName = ProgramInstall.ExeName(machine.Windows);
            string staged = Path.Combine(unpacked, exeName);
            if (!File.Exists(staged))
                throw new DmcException("the new dmc does not run: nothing was installed");
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(staged, File.GetUnixFileMode(staged) | UnixFileMode.UserExecute);
            var (code, said) = _seams.Run(staged, ["--version"]);
            if (code != 0 || said != $"dmc {release.Version}")
                throw new DmcException("the new dmc does not run: nothing was installed");

            ProgramSwap.Swap(unpacked, machine.ProgramFolder, exeName, _seams.AfterMove);
            ProgramInstall.RemoveLeftovers(machine.ProgramFolder);

            var (refreshCode, refreshed) = _seams.Run(Path.Combine(machine.ProgramFolder, exeName), ["refresh", "--all"]);
            if (refreshCode != 0)
                throw new DmcException($"installed dmc {release.Version}, but the DMC folder was not refreshed: {Reason(refreshed)}; dmc play will refresh it");
            io.Out.WriteLine($"dmc update: installed dmc {release.Version} (was {machine.Version})");
            foreach (string line in refreshed.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
                io.Out.WriteLine(line);
            return 0;
        }
        finally
        {
            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // left for the next update, install or play, which remove .update-* folders
            }
        }
    }

    /// <summary>Rewrites the generated files of every recorded DMC folder that is not what this program would write, and prints the refresh's lines. Used when this program is already the latest release.</summary>
    /// <param name="machine">The machine: the recorded folders, the program's files and version.</param>
    /// <param name="io">Standard output gets the lines of each refresh.</param>
    /// <exception cref="DmcException">A path cannot be written.</exception>
    static void RefreshStale(Machine machine, Io io)
    {
        foreach (string folder in machine.Recorded().Where(DmcFolder.IsDmcFolder))
        {
            if (Refresher.IsCurrent(folder, machine.Files, machine.Version))
                continue;
            var result = Refresher.Refresh(folder, machine.Files, machine.Version, machine.Now());
            foreach (string line in Refresher.Report("dmc refresh", folder, result, machine.Version))
                io.Out.WriteLine(line);
        }
    }

    /// <summary>The reason a refresh failed, from what the new program printed: its last line, without the leading <c>dmc refresh: </c>.</summary>
    /// <param name="output">What the new <c>dmc refresh --all</c> printed.</param>
    static string Reason(string output)
    {
        string last = output.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? "it failed";
        return last.StartsWith("dmc refresh: ", StringComparison.Ordinal) ? last["dmc refresh: ".Length..] : last;
    }
}
