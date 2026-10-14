using Dmc.Tests.Support;

namespace Dmc.Tests.Parity;

/// <summary>
/// Finds and runs the old scripts (<c>scripts/*.py</c>, <c>roll.sh</c>, <c>roll.ps1</c>) for the parity check, the
/// way <c>tests/support.py</c> does: <see cref="TestEnv.Need"/> skips a test locally when a tool is missing, and
/// fails it on CI, where all of them are installed on purpose.
/// </summary>
public static class Legacy
{
    public static string Scripts { get; } = Path.Combine(TestEnv.Root, "scripts");

    /// <summary>A script's full path, with forward slashes so that Git Bash on Windows takes it too.</summary>
    public static string Script(string name) => Path.Combine(Scripts, name).Replace('\\', '/');

    /// <summary>
    /// Full path of a bash that works, or null. On Windows a bare "bash" can start the WSL launcher in System32
    /// instead of Git Bash, so candidates are tried by full path until one answers.
    /// </summary>
    public static string? Bash() => _bash.Value;

    public static string? PowerShell() => Which("pwsh") ?? Which("powershell");

    /// <summary>A Python that answers <c>--version</c>. On Windows <c>python3</c> can be the Store stub, which does not.</summary>
    public static string? Python() => _python.Value;

    static readonly Lazy<string?> _bash = new(() =>
    {
        var candidates = new List<string?> { Which("bash") };
        if (OperatingSystem.IsWindows())
        {
            string? git = Which("git");
            if (git is not null)
                candidates.Add(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(git))!, "bin", "bash.exe"));
            foreach (string? baseDir in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
            {
                if (baseDir is not null)
                    candidates.Add(Path.Combine(baseDir, "Git", "bin", "bash.exe"));
            }
        }
        return candidates.FirstOrDefault(c => c is not null && File.Exists(c) && Answers(c, "-c", "echo ok") == "ok");
    });

    static readonly Lazy<string?> _python = new(() =>
        new[] { "python3", "python" }.Select(Which).FirstOrDefault(c => c is not null && Answers(c, "--version") is { } v && v.StartsWith("Python 3", StringComparison.Ordinal)));

    static string? Answers(string file, params string[] args)
    {
        try
        {
            var result = TestEnv.Run(file, args, timeoutSeconds: 30);
            return result.ExitCode == 0 ? (result.Stdout + result.Stderr).Trim() : null;
        }
        catch (Exception e) when (e is TimeoutException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>The full path of a program on PATH, or null.</summary>
    static string? Which(string name)
    {
        string[] extensions = OperatingSystem.IsWindows() ? [".exe", ".cmd", ".bat"] : [""];
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (string extension in extensions)
            {
                string candidate = Path.Combine(dir.Trim('"'), name + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        return null;
    }
}
