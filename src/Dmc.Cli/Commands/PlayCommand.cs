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

    public int Run(string[] args, Io io)
    {
        string? claude = FindOnPath("claude", Environment.GetEnvironmentVariable("PATH"),
            Environment.GetEnvironmentVariable("PATHEXT"), OperatingSystem.IsWindows());
        return Run(args, claude, StartAndWait);
    }

    /// <summary>The command with its two seams: where <c>claude</c> is, and what starts it.</summary>
    internal static int Run(string[] args, string? claude, Func<ProcessStartInfo, int> start)
    {
        if (claude is null)
            throw new DmcException("Claude Code (claude) is not on the PATH: install it, then run this again", 127);

        var info = new ProcessStartInfo(claude) { WorkingDirectory = Toolkit.Root, UseShellExecute = false };
        foreach (string arg in FixedArguments.Concat(args))
            info.ArgumentList.Add(arg);
        return start(info);
    }

    /// <summary>
    /// The full path of <paramref name="name"/> on the PATH, found the way a shell finds it; null if it is not there.
    /// On Windows each extension of PATHEXT is tried, so the <c>claude.exe</c> of the native installer and the
    /// <c>claude.cmd</c> of npm are both found. Elsewhere the file must be executable.
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
