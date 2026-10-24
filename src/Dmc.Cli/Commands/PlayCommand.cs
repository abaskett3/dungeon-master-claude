using System.Diagnostics;
using System.Runtime.InteropServices;
using Dmc.Install;
using Dmc.Kit;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc play [args...]</c> (contract <c>dmc-play.md</c>, FR-011, FR-015): starts a play session in the DMC folder. It
/// removes what an update left behind, finds the folder (the current folder if it is a DMC folder, else the first recorded
/// one), refreshes it first when its manifest's version is not this program's or a generated file is missing or changed,
/// finds Claude Code, and starts it in the folder as
/// <c>claude --model sonnet --effort high</c> followed by every argument given to it unchanged (so <c>dmc play --continue</c>
/// works, and <c>dmc play --help</c> is Claude Code's help), with this program's folder first on the child's <c>PATH</c>
/// so that <c>dmc</c> is found by name even before a new terminal has picked up the install. The rules, skills and
/// settings load because the folder is a Claude Code project, so no <c>--settings</c> is passed. It waits for Claude Code
/// and exits with its exit code.
/// </summary>
public sealed class PlayCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "play";

    /// <inheritdoc />
    public string Summary => "start a play session in your DMC folder: claude --model sonnet --effort high [args]";

    /// <summary>What comes before the player's arguments.</summary>
    internal static IReadOnlyList<string> FixedArguments { get; } = ["--model", "sonnet", "--effort", "high"];

    /// <summary>Plays in the DMC folder of the real machine, with the real <see cref="StartAndWait"/>; see <see cref="Run(string[], Io, Machine, Func{ProcessStartInfo, int})"/>.</summary>
    /// <param name="args">Everything after <c>play</c>, passed to Claude Code unchanged.</param>
    /// <param name="io">Standard output gets the lines of a refresh; Claude Code takes over the terminal itself.</param>
    /// <returns>Claude Code's exit code.</returns>
    /// <exception cref="DmcException">There is no DMC folder (<c>no DMC folder: run dmc install</c>), the refresh fails, Claude Code is not on the PATH (exit code 127), or only its <c>claude.cmd</c> shim is (see <see cref="FindClaude"/>).</exception>
    public int Run(string[] args, Io io) => Run(args, io, Machine.Real(), StartAndWait);

    /// <summary>
    /// The whole command over a machine: leftovers, the folder, the refresh, Claude Code, then the start. A test passes a
    /// machine over temporary folders and a <paramref name="start"/> that only records what it was given.
    /// </summary>
    /// <param name="args">The player's arguments, passed on unchanged and last.</param>
    /// <param name="io">Standard output gets the lines of a refresh.</param>
    /// <param name="machine">The machine: environment, current folder, program folder and the generated files.</param>
    /// <param name="start">Starts the process and waits for it, returning its exit code.</param>
    /// <returns>Whatever <paramref name="start"/> returns.</returns>
    /// <exception cref="DmcException">As <see cref="Run(string[], Io)"/>.</exception>
    internal static int Run(string[] args, Io io, Machine machine, Func<ProcessStartInfo, int> start)
    {
        ProgramInstall.RemoveLeftovers(machine.ProgramFolder);
        // The current folder, or a recorded one: CLAUDE_PROJECT_DIR is for hooks, not for starting a session.
        string folder = DmcFolder.Find(_ => null, machine.CurrentFolder, machine.ProgramFolder) ?? throw new DmcException(DmcFolder.NoFolder);
        if (!Refresher.IsCurrent(folder, machine.Files, machine.Version))
        {
            var result = Refresher.Refresh(folder, machine.Files, machine.Version, machine.Now());
            foreach (string line in Refresher.Report("dmc refresh", folder, result, machine.Version))
                io.Out.WriteLine(line);
        }
        var claude = FindClaude(machine.Environment("PATH"), machine.Windows);
        return Run(args, claude, start, folder, machine.ProgramFolder, machine.Environment("PATH"));
    }

    /// <summary>How to start Claude Code: a program, and the arguments that go before Claude Code's own.</summary>
    /// <param name="File">The program to start: <c>claude</c> itself, or PowerShell when Claude Code is only a <c>claude.ps1</c> shim.</param>
    /// <param name="Prefix">The arguments that go before <see cref="FixedArguments"/>: empty for <c>claude</c>, the <c>-File</c> arguments for PowerShell.</param>
    internal sealed record Launch(string File, IReadOnlyList<string> Prefix);

    /// <summary>The start itself, with its two seams: how <c>claude</c> is started, and what starts it.</summary>
    /// <param name="args">The player's arguments, passed on unchanged and last.</param>
    /// <param name="claude">The program and its leading arguments.</param>
    /// <param name="start">Starts the process and waits for it, returning its exit code. A test passes a function that only records what it was given.</param>
    /// <param name="folder">The DMC folder, where Claude Code is started.</param>
    /// <param name="programFolder">This program's folder, put first on the child's <c>PATH</c>.</param>
    /// <param name="inheritedPath">The <c>PATH</c> this process has, which follows the program folder; null when unset.</param>
    /// <returns>Whatever <paramref name="start"/> returns.</returns>
    internal static int Run(string[] args, Launch claude, Func<ProcessStartInfo, int> start, string folder, string programFolder, string? inheritedPath)
    {
        var info = new ProcessStartInfo(claude.File) { WorkingDirectory = folder, UseShellExecute = false };
        info.Environment["PATH"] = string.IsNullOrEmpty(inheritedPath) ? programFolder : programFolder + Path.PathSeparator + inheritedPath;
        foreach (string arg in claude.Prefix.Concat(FixedArguments).Concat(args))
            info.ArgumentList.Add(arg);
        return start(info);
    }

    /// <summary>
    /// How to start Claude Code from the PATH. Elsewhere than Windows, the executable <c>claude</c>. On Windows,
    /// <c>claude.exe</c> (the native installer's) wherever it is on the PATH. Failing that, npm's shims: never its
    /// <c>claude.cmd</c>, which would run through <c>cmd.exe</c> (which re-parses the arguments, and asks "Terminate
    /// batch job (Y/N)?" after Ctrl+C), but the <c>claude.ps1</c> next to it, through PowerShell, which is what a
    /// PowerShell prompt runs for <c>claude</c>.
    /// </summary>
    /// <param name="path">The PATH variable's value, or null if unset.</param>
    /// <param name="windows">True for the Windows rules (see the summary); false for Linux.</param>
    /// <exception cref="DmcException">
    /// Nothing named <c>claude</c> is on the PATH (exit code 127); or only a <c>claude.cmd</c> or <c>.bat</c> is, with no
    /// <c>claude.ps1</c> beside it (exit code 1).
    /// </exception>
    internal static Launch FindClaude(string? path, bool windows)
    {
        const string notFound = "Claude Code (claude) is not on the PATH: install it, then run this again";
        if (!windows)
            return FindOnPath("claude", path, null, windows: false) is { } claude ? new Launch(claude, []) : throw new DmcException(notFound, 127);

        if (FindOnPath("claude", path, ".exe", windows: true) is { } exe)
            return new Launch(exe, []);
        string? shim = FindOnPath("claude", path, ".cmd;.bat", windows: true);
        if (shim is null)
            throw new DmcException(notFound, 127);
        string script = Path.Combine(Path.GetDirectoryName(shim)!, "claude.ps1");
        if (!File.Exists(script))
            throw new DmcException($"only {shim} was found, and dmc does not start Claude Code through cmd.exe: install Claude Code's claude.exe");
        string powerShell = FindOnPath("pwsh", path, ".exe", windows: true)
            ?? FindOnPath("powershell", path, ".exe", windows: true)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        return new Launch(powerShell, ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script]);
    }

    /// <summary>
    /// The full path of <paramref name="name"/> on the PATH, found the way a shell finds it; null if it is not there.
    /// On Windows each extension of <paramref name="pathExt"/> (PATHEXT's form, <c>.EXE;.CMD</c>) is tried in each
    /// folder. Elsewhere the file must be executable.
    /// </summary>
    /// <param name="name">The program name without an extension.</param>
    /// <param name="path">The PATH variable's value, or null if unset; entries are split on the platform's separator and may be quoted.</param>
    /// <param name="pathExt">The extensions to try on Windows, separated by ";". Null or empty means <c>.COM;.EXE;.BAT;.CMD</c>. Ignored elsewhere.</param>
    /// <param name="windows">True to try extensions and skip the executable-bit check.</param>
    internal static string? FindOnPath(string name, string? path, string? pathExt, bool windows)
    {
        string[] extensions = windows
            ? (string.IsNullOrEmpty(pathExt) ? ".COM;.EXE;.BAT;.CMD" : pathExt).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [""];
        foreach (string folder in (path ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string extension in extensions)
            {
                string candidate;
                try
                {
                    candidate = Path.Combine(folder.Trim('"'), name + extension);
                }
                catch (ArgumentException)
                {
                    continue; // a PATH entry with characters no path may hold
                }
                if (File.Exists(candidate) && (windows || IsExecutable(candidate)))
                    return candidate;
            }
        }
        return null;
    }

    /// <summary>True when any execute permission bit is set on the file. Always true on Windows, which has none.</summary>
    static bool IsExecutable(string file)
    {
        if (OperatingSystem.IsWindows())
            return true;
        const UnixFileMode anyExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        return (File.GetUnixFileMode(file) & anyExecute) != 0;
    }

    /// <summary>
    /// Start Claude Code on this terminal and wait for it. Claude Code reads Ctrl+C itself (to interrupt, and twice
    /// to quit), and the terminal sends it to dmc as well; dmc ignores it while Claude Code runs, so that it neither
    /// dies and leaves Claude Code behind without a terminal, nor ends the session early.
    /// </summary>
    /// <param name="info">The process to start.</param>
    /// <returns>Its exit code.</returns>
    /// <exception cref="DmcException"><see cref="Process.Start(ProcessStartInfo)"/> returned no process (exit code 1). When it throws instead (the program cannot be run), <see cref="CommandLine"/> reports that as an unexpected failure, also exit code 1.</exception>
    static int StartAndWait(ProcessStartInfo info)
    {
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, context => context.Cancel = true);
        using var quit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context => context.Cancel = true);
        using var process = Process.Start(info) ?? throw new DmcException($"could not start {info.FileName}");
        process.WaitForExit();
        return process.ExitCode;
    }
}
