using Dmc.Install;
using Dmc.Kit;

namespace Dmc;

/// <summary>
/// Everything the commands that install and refresh read from the machine, in one place so that a test can replace each
/// part: the environment, the current folder, the home folder, the folder the program runs from, the program itself and its
/// version, the generated files, the clock, the user's Windows <c>Path</c> and the terminal. <see cref="Real"/> is the
/// real machine. A test builds one over temporary folders and fakes, so nothing it does can reach the real home folder,
/// the real PATH or the real registry.
/// </summary>
/// <param name="Environment">Reads an environment variable; null for one that is not set.</param>
/// <param name="CurrentFolder">The current folder.</param>
/// <param name="Home">The user's home folder.</param>
/// <param name="Windows">True on Windows, which decides the program folder, how the program is reached by name and the file names.</param>
/// <param name="ProgramFolder">The folder the running program is in, which holds <c>dmc.json</c> (<see cref="DmcFolder.ProgramFolder"/>).</param>
/// <param name="RunningExecutable">The full path of the running program, which <c>dmc install</c> copies.</param>
/// <param name="Version">The running program's version (<see cref="CommandLine.Version"/>).</param>
/// <param name="Files">The generated files this program writes (<see cref="GeneratedFiles.All"/>).</param>
/// <param name="Now">The local time, which names a <c>.dmc/replaced/</c> folder.</param>
/// <param name="UserPath">The user's Windows <c>Path</c>; null on Linux, and means the real registry on Windows.</param>
/// <param name="OpenTerminal">Opens the terminal to read an answer from when standard input is not one (Linux, <c>/dev/tty</c>); returns null when there is none.</param>
internal sealed record Machine(
    Func<string, string?> Environment,
    string CurrentFolder,
    string Home,
    bool Windows,
    string ProgramFolder,
    string RunningExecutable,
    string Version,
    IReadOnlyList<GeneratedFile> Files,
    Func<DateTime> Now,
    IUserPath? UserPath,
    Func<TextReader?> OpenTerminal)
{
    /// <summary>The real machine: the process's environment and current folder, the home folder from <c>USERPROFILE</c> or <c>HOME</c>, this program and its embedded files.</summary>
    public static Machine Real() =>
        new(System.Environment.GetEnvironmentVariable, Directory.GetCurrentDirectory(),
            Digest.LogFolder.HomeFolder(System.Environment.GetEnvironmentVariable), OperatingSystem.IsWindows(),
            DmcFolder.ProgramFolder, System.Environment.ProcessPath ?? Path.Combine(DmcFolder.ProgramFolder, ProgramInstall.ExeName(OperatingSystem.IsWindows())),
            CommandLine.Version, GeneratedFiles.All, () => DateTime.Now, null, OpenDevTty);

    /// <summary>Opens <c>/dev/tty</c>, the terminal a Linux program has even when its standard input is a pipe (<c>curl ... | sh</c>); null when there is no terminal or this is not Linux.</summary>
    static TextReader? OpenDevTty()
    {
        if (!OperatingSystem.IsLinux())
            return null;
        try
        {
            return new StreamReader(new FileStream("/dev/tty", FileMode.Open, FileAccess.Read), Io.Utf8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The DMC folders recorded in <c>dmc.json</c> beside this program.</summary>
    public IReadOnlyList<string> Recorded() => DmcFolder.Recorded(ProgramFolder);
}
