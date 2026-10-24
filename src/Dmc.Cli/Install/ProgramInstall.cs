namespace Dmc.Install;

/// <summary>
/// Installs <c>dmc</c> itself for the current user: the program folder, a copy of the running program and its native
/// libraries, and a way to reach it by name (Windows: the folder on the user's <c>Path</c>; Linux: the link
/// <c>~/.local/bin/dmc</c>). Used by <c>dmc install</c>. Everything that depends on the machine, the home folder, the
/// environment and the registry, is a parameter, so tests use temporary folders and fakes and never touch the real PATH.
/// </summary>
public static class ProgramInstall
{
    /// <summary>The version every build that is not a release prints; such a build is copied as the program only with <c>--force</c> (research R14).</summary>
    public const string DevVersion = "0.0.0-dev";

    /// <summary>
    /// The program folder: <c>%LOCALAPPDATA%\Programs\dmc</c> on Windows (the <c>LOCALAPPDATA</c> variable, else
    /// <c>AppData\Local</c> in the home folder), <c>~/.local/share/dmc</c> on Linux.
    /// </summary>
    /// <param name="home">The user's home folder.</param>
    /// <param name="environment">Reads an environment variable; null for one that is not set.</param>
    /// <param name="windows">True for the Windows layout; false for Linux's.</param>
    public static string ProgramFolder(string home, Func<string, string?> environment, bool windows) =>
        windows
            ? Path.Combine(environment("LOCALAPPDATA") is { Length: > 0 } local ? local : Path.Combine(home, "AppData", "Local"), "Programs", "dmc")
            : Path.Combine(home, ".local", "share", "dmc");

    /// <summary>The folder that holds the link to <c>dmc</c> on Linux: <c>~/.local/bin</c>.</summary>
    /// <param name="home">The user's home folder.</param>
    public static string LinkFolder(string home) => Path.Combine(home, ".local", "bin");

    /// <summary>The name of the program: <c>dmc.exe</c> on Windows, <c>dmc</c> on Linux.</summary>
    /// <param name="windows">True for Windows.</param>
    public static string ExeName(bool windows) => windows ? "dmc.exe" : "dmc";

    /// <summary>
    /// Removes what an earlier update left in the program folder: files and folders named <c>*.old</c> or <c>.update-*</c>.
    /// Quietly: one that cannot be removed (a program still running from it) stays for next time.
    /// </summary>
    /// <param name="programFolder">The program folder; nothing happens if it does not exist.</param>
    public static void RemoveLeftovers(string programFolder)
    {
        if (!Directory.Exists(programFolder))
            return;
        try
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(programFolder))
            {
                string name = Path.GetFileName(entry);
                if (!name.EndsWith(".old", StringComparison.Ordinal) && !name.StartsWith(".update-", StringComparison.Ordinal))
                    continue;
                try
                {
                    if (Directory.Exists(entry))
                        Directory.Delete(entry, recursive: true);
                    else
                        File.Delete(entry);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // still in use: the next install, update or play tries again
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // an unreadable program folder is reported by the copy, if there is one
        }
    }

    /// <summary>
    /// Whether the running program is to be copied into the program folder: never when it already runs from there; a
    /// development build only with <paramref name="force"/>; any other build always.
    /// </summary>
    /// <param name="version">The running program's version (<see cref="CommandLine.Version"/>).</param>
    /// <param name="force">True with <c>--force</c>.</param>
    /// <param name="runningFolder">The folder the running program is in.</param>
    /// <param name="programFolder">The program folder.</param>
    public static bool ShouldCopy(string version, bool force, string runningFolder, string programFolder) =>
        !SameFolder(runningFolder, programFolder) && (force || version != DevVersion);

    /// <summary>Whether two paths name the same folder (compared by full path, ignoring a trailing separator; case-insensitively on Windows).</summary>
    /// <param name="a">One folder.</param>
    /// <param name="b">The other.</param>
    public static bool SameFolder(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>
    /// Copies the running program and its native libraries (every <c>*.dll</c> and <c>*.so</c> beside it) into the program
    /// folder. A file in use there (a running <c>dmc</c> can be renamed but not overwritten on Windows) is renamed to
    /// <c>*.old</c> first, which the next install, update or play removes.
    /// </summary>
    /// <param name="runningExecutable">The full path of the running program.</param>
    /// <param name="programFolder">The program folder; made if it does not exist.</param>
    /// <param name="windows">True for Windows, which decides the name the program is installed under.</param>
    /// <exception cref="DmcException"><c>cannot write &lt;folder&gt;: &lt;reason&gt;</c>.</exception>
    public static void Copy(string runningExecutable, string programFolder, bool windows)
    {
        try
        {
            Directory.CreateDirectory(programFolder);
            string source = Path.GetDirectoryName(Path.GetFullPath(runningExecutable))!;
            var files = new List<(string From, string To)> { (runningExecutable, Path.Combine(programFolder, ExeName(windows))) };
            files.AddRange(Directory.EnumerateFiles(source).Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".so", StringComparison.Ordinal))
                .Select(f => (f, Path.Combine(programFolder, Path.GetFileName(f)))));
            foreach (var (from, to) in files)
                CopyOne(from, to);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {programFolder}: {e.Message}");
        }
    }

    /// <summary>Copies one file over its place; if the place is in use, moves it aside as <c>*.old</c> and copies again.</summary>
    /// <param name="from">The file to copy.</param>
    /// <param name="to">Where it goes.</param>
    static void CopyOne(string from, string to)
    {
        try
        {
            File.Copy(from, to, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!File.Exists(to))
                throw;
            string aside = to + ".old";
            if (File.Exists(aside))
                File.Delete(aside);
            File.Move(to, aside);
            File.Copy(from, to, overwrite: true);
        }
    }

    /// <summary>
    /// Linux: makes <c>~/.local/bin/dmc</c> a link to the installed program, replacing an older link.
    /// </summary>
    /// <param name="programFolder">The program folder, which holds <c>dmc</c>.</param>
    /// <param name="home">The user's home folder.</param>
    /// <exception cref="DmcException">The link cannot be made (<c>cannot write &lt;link&gt;: ...</c>), or a file that is not a link is in its place.</exception>
    public static void Link(string programFolder, string home)
    {
        string folder = LinkFolder(home), link = Path.Combine(folder, "dmc");
        try
        {
            Directory.CreateDirectory(folder);
            var existing = new FileInfo(link);
            if (existing.LinkTarget is not null)
                existing.Delete();
            else if (existing.Exists)
                throw new DmcException($"cannot write {link}: a file that is not a link is already there");
            File.CreateSymbolicLink(link, Path.Combine(programFolder, ExeName(false)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {link}: {e.Message}");
        }
    }

    /// <summary>Whether <c>~/.local/bin</c> is one of the folders of a <c>PATH</c> value, so that the link <c>dmc</c> is found by name.</summary>
    /// <param name="home">The user's home folder.</param>
    /// <param name="path">The <c>PATH</c> value; null when unset.</param>
    public static bool LinkFolderOnPath(string home, string? path) =>
        (path ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(p => SameFolder(p, LinkFolder(home)));

    /// <summary>
    /// Windows: puts the program folder on the user's <c>Path</c> unless it is there already, and tells running programs. The
    /// value is read and written without expanding its <c>%VARIABLES%</c>, so what the user keeps there is kept.
    /// </summary>
    /// <param name="programFolder">The program folder.</param>
    /// <param name="userPath">The user's <c>Path</c> (<see cref="RegistryUserPath"/> in the real program).</param>
    /// <returns>True when the folder was added; false when it was already on the <c>Path</c>.</returns>
    /// <exception cref="DmcException"><c>could not add &lt;folder&gt; to your PATH: &lt;reason&gt;</c>.</exception>
    public static bool AddToUserPath(string programFolder, IUserPath userPath)
    {
        try
        {
            string current = userPath.Read() ?? "";
            var entries = current.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string wanted = Path.TrimEndingDirectorySeparator(programFolder);
            if (entries.Any(e => string.Equals(Path.TrimEndingDirectorySeparator(Environment.ExpandEnvironmentVariables(e.Trim())), wanted, StringComparison.OrdinalIgnoreCase)))
                return false;
            userPath.Write(current.Length == 0 ? wanted : current.TrimEnd(';') + ";" + wanted);
            userPath.Broadcast();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            throw new DmcException($"could not add {programFolder} to your PATH: {e.Message}");
        }
    }
}
