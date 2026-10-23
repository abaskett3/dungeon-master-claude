using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc play [args...]</c>: replaces what <c>play.ps1</c> and <c>play.sh</c> did themselves. It starts Claude Code
/// for a play session, from the toolkit root, as
/// <c>claude --model sonnet --effort high --settings .claude/play-settings.json</c> followed by every argument given
/// to it unchanged (so <c>dmc play --continue</c> works, and <c>dmc play --help</c> is Claude Code's help). It waits
/// for Claude Code and exits with its exit code.
/// </summary>
/// <remarks>
/// The launchers hand over to this once a release is pinned (see <c>tools/dmc-release.json</c>); until then they
/// start Claude Code themselves, with the same command.
/// </remarks>
public sealed class PlayCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "play";

    /// <inheritdoc />
    public string Summary => "start a play session: claude --model sonnet --effort high --settings .claude/play-settings.json [args]";

    /// <summary>What comes before the player's arguments, the same as in the launchers.</summary>
    internal static IReadOnlyList<string> FixedArguments { get; } =
        ["--model", "sonnet", "--effort", "high", "--settings", ".claude/play-settings.json"];

    /// <summary>Finds <c>claude</c> on the PATH and runs it, in the toolkit root, with <see cref="FixedArguments"/> and then <paramref name="args"/>.</summary>
    /// <param name="args">Everything after <c>play</c>, passed to Claude Code unchanged.</param>
    /// <param name="io">Not used: Claude Code takes over the terminal itself.</param>
    /// <returns>Claude Code's exit code.</returns>
    /// <exception cref="DmcException">Claude Code is not on the PATH (exit code 127), or only its <c>claude.cmd</c> shim is (see <see cref="FindClaude"/>).</exception>
    public int Run(string[] args, Io io) =>
        Run(args, FindClaude(Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsWindows()), StartAndWait);

    /// <summary>How to start Claude Code: a program, and the arguments that go before Claude Code's own.</summary>
    /// <param name="File">The program to start: <c>claude</c> itself, or PowerShell when Claude Code is only a <c>claude.ps1</c> shim.</param>
    /// <param name="Prefix">The arguments that go before <see cref="FixedArguments"/>: empty for <c>claude</c>, the <c>-File</c> arguments for PowerShell.</param>
    internal sealed record Launch(string File, IReadOnlyList<string> Prefix);

    /// <summary>The command with its two seams: how <c>claude</c> is started, and what starts it.</summary>
    /// <param name="args">The player's arguments, passed on unchanged and last.</param>
    /// <param name="claude">The program and its leading arguments.</param>
    /// <param name="start">Starts the process and waits for it, returning its exit code. A test passes a function that only records what it was given.</param>
    /// <returns>Whatever <paramref name="start"/> returns.</returns>
    internal static int Run(string[] args, Launch claude, Func<ProcessStartInfo, int> start)
    {
        var info = new ProcessStartInfo(claude.File) { WorkingDirectory = Toolkit.Root, UseShellExecute = false };
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
            throw new DmcException($"only {shim} was found, and dmc does not start Claude Code through cmd.exe: install Claude Code's claude.exe, or start play with ./play.ps1");
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
