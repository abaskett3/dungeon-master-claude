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
    public string Name => "play";

    public string Summary => "start a play session: claude --model sonnet --effort high --settings .claude/play-settings.json [args]";

    /// <summary>What comes before the player's arguments, the same as in the launchers.</summary>
    internal static IReadOnlyList<string> FixedArguments { get; } =
        ["--model", "sonnet", "--effort", "high", "--settings", ".claude/play-settings.json"];

    public int Run(string[] args, Io io) =>
        Run(args, FindClaude(Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsWindows()), StartAndWait);

    /// <summary>How to start Claude Code: a program, and the arguments that go before Claude Code's own.</summary>
    internal sealed record Launch(string File, IReadOnlyList<string> Prefix);

    /// <summary>The command with its two seams: how <c>claude</c> is started, and what starts it.</summary>
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
    static int StartAndWait(ProcessStartInfo info)
    {
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, context => context.Cancel = true);
        using var quit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context => context.Cancel = true);
        using var process = Process.Start(info) ?? throw new DmcException($"could not start {info.FileName}");
        process.WaitForExit();
        return process.ExitCode;
    }
}
