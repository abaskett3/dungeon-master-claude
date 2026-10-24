using Dmc.Commands;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// What the tests that start other programs share: the file name of <c>dmc</c>, the shells a hook is run with (found the way
/// <c>dmc play</c> finds Claude Code), and a fake <c>claude</c> that prints where it was started and with what. The release
/// archives, the pin and the scripts of the launcher days (<c>play.ps1</c>, <c>tools/get-dmc.*</c>) that this class once
/// built are gone with them.
/// </summary>
public static class Bootstrap
{
    /// <summary>The file name of the program: <c>dmc.exe</c> on Windows, <c>dmc</c> elsewhere.</summary>
    public static string ExeName { get; } = OperatingSystem.IsWindows() ? "dmc.exe" : "dmc";

    /// <summary>
    /// A folder with a fake <c>claude</c> in it, to put first on the PATH. It prints the folder it runs in and each
    /// argument on its own line, and exits with 7. On Windows it is the three shims npm installs: <c>claude.ps1</c>,
    /// which dmc and PowerShell run; a <c>claude</c> shell script, which Git Bash runs; and <c>claude.cmd</c>, which
    /// nothing may run (it says so and exits with 9).
    /// </summary>
    public static string FakeClaude()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-fake-claude-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string script = Path.Combine(folder, "claude");
        File.WriteAllText(script, "#!/bin/sh\necho \"fake claude in $(pwd)\"\necho \"path: $PATH\"\nfor a in \"$@\"; do echo \"arg: $a\"; done\nexit 7\n");
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(Path.Combine(folder, "claude.ps1"),
                "\"fake claude in $((Get-Location).Path)\"\r\n\"path: $env:PATH\"\r\nforeach ($a in $args) { \"arg: $a\" }\r\nexit 7\r\n");
            File.WriteAllText(Path.Combine(folder, "claude.cmd"), "@echo off\r\necho the cmd shim ran\r\nexit /b 9\r\n");
        }
        else
        {
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        return folder;
    }

    /// <summary>
    /// The PATH with <paramref name="folder"/> first, and without the folders of a real Claude Code: dmc takes a
    /// <c>claude.exe</c> anywhere on the PATH before npm's shims, which is what the fake is on Windows.
    /// </summary>
    public static string PathWith(string folder) =>
        string.Join(Path.PathSeparator, (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(dir => !HasClaude(dir))
            .Prepend(folder));

    /// <summary>True when a PATH folder holds something named <c>claude</c> with any extension.</summary>
    static bool HasClaude(string dir)
    {
        try
        {
            return Directory.Exists(dir.Trim('"')) && Directory.EnumerateFiles(dir.Trim('"'), "claude*").Any(
                f => Path.GetFileNameWithoutExtension(f).Equals("claude", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>What the fake claude printed: the folder it ran in, the <c>PATH</c> it had, and its arguments.</summary>
    public static (string Folder, string Path, string[] Args) ClaudeSaw(ProcessResult result)
    {
        string[] lines = result.StdoutLines;
        string? folder = lines.FirstOrDefault(l => l.StartsWith("fake claude in ", StringComparison.Ordinal));
        Assert.IsNotNull(folder, $"claude did not start:\n{result}");
        string path = lines.First(l => l.StartsWith("path: ", StringComparison.Ordinal))["path: ".Length..];
        return (folder["fake claude in ".Length..], path, lines.Where(l => l.StartsWith("arg: ", StringComparison.Ordinal)).Select(l => l["arg: ".Length..]).ToArray());
    }

    /// <summary>What claude must be started with: the fixed arguments, then the player's.</summary>
    public static string[] ClaudeArgs(params string[] players) => [.. PlayCommand.FixedArguments, .. players];

    /// <summary>The full path of a shell, after checking it is there (a skip locally, a failure on CI).</summary>
    public static string Shell(string name)
    {
        string? found = name == "bash" ? Bash.Value : Find(name);
        TestEnv.Need(found is not null, name);
        return found!;
    }

    /// <summary>The full path of a program on the PATH, found the way <c>dmc play</c> finds Claude Code; null if absent.</summary>
    static string? Find(string name) =>
        PlayCommand.FindOnPath(name, Environment.GetEnvironmentVariable("PATH"), ".EXE", OperatingSystem.IsWindows());

    /// <summary>
    /// On Windows, Git Bash: a bare "bash" can be the WSL launcher in the Windows folder, so Git's own bash is taken,
    /// found from <c>git</c> or Program Files.
    /// </summary>
    static readonly Lazy<string?> Bash = new(() =>
    {
        if (!OperatingSystem.IsWindows())
            return Find("bash");
        var candidates = new List<string>();
        if (Find("git") is { } git)
            candidates.Add(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(git))!, "bin", "bash.exe"));
        if (Environment.GetEnvironmentVariable("ProgramFiles") is { } programFiles)
            candidates.Add(Path.Combine(programFiles, "Git", "bin", "bash.exe"));
        return candidates.FirstOrDefault(File.Exists);
    });
}
