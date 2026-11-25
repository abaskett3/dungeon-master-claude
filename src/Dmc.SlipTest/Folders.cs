using System.Text.RegularExpressions;

namespace Dmc.SlipTest;

/// <summary>The folders a trial uses, and the guards that keep the runner out of the DMC folder (FR-010).</summary>
internal static partial class Folders
{
    /// <summary>Any character that Claude Code replaces with <c>-</c> in a project folder's name (research R1).</summary>
    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex NotLetterOrDigit();

    /// <summary>Whether paths compare without case, as they do on Windows.</summary>
    static bool IgnoreCase => OperatingSystem.IsWindows();

    /// <summary>How paths compare on this system.</summary>
    public static StringComparison PathComparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// The name of the folder under <c>~/.claude/projects/</c> that holds the logs of sessions started in a folder:
    /// its full path with every character that is not a letter or a digit replaced by <c>-</c>
    /// (<c>D:\Claude\DMC</c> is <c>D--Claude-DMC</c>).
    /// </summary>
    /// <param name="folder">The working directory of the sessions.</param>
    public static string Slug(string folder) => NotLetterOrDigit().Replace(Path.GetFullPath(folder).TrimEnd('/', '\\'), "-");

    /// <summary>A folder's full path with no trailing separator.</summary>
    /// <param name="path">A path.</param>
    public static string Full(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    /// <summary>Whether <paramref name="inner"/> is <paramref name="outer"/> or inside it.</summary>
    /// <param name="inner">The path that may be inside.</param>
    /// <param name="outer">The folder.</param>
    public static bool IsInside(string inner, string outer)
    {
        string i = Full(inner), o = Full(outer);
        return i.Equals(o, PathComparison) || i.StartsWith(o + Path.DirectorySeparatorChar, PathComparison);
    }

    /// <summary>Refuses a copy folder that is the DMC folder or inside it, or that holds it.</summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="copy">The copy folder.</param>
    /// <exception cref="SlipTestException">The two are the same or nested.</exception>
    public static void Guard(string folder, string copy)
    {
        if (IsInside(copy, folder) || IsInside(folder, copy))
            throw new SlipTestException($"the copy folder {copy} and the DMC folder {folder} must be separate, and neither inside the other");
    }

    /// <summary>Refuses a path the runner would write that is inside the DMC folder or its Claude Code project folder (FR-010).</summary>
    /// <param name="path">The path about to be written or deleted.</param>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="claudeHome">The Claude Code folder (<c>~/.claude</c>).</param>
    /// <exception cref="SlipTestException">The path is inside either.</exception>
    public static void GuardWrite(string path, string folder, string claudeHome)
    {
        string project = Path.Combine(claudeHome, "projects", Slug(folder));
        if (IsInside(path, folder) || IsInside(path, project))
            throw new SlipTestException($"refused to write {path}: it is inside the DMC folder or its logs");
    }

    /// <summary>Copies a folder tree, leaving out one folder inside it (the work folder, when it is in the DMC folder).</summary>
    /// <param name="from">The folder to copy.</param>
    /// <param name="to">Where to copy it; made if missing. Files there are overwritten.</param>
    /// <param name="skip">A folder to leave out, or null.</param>
    public static void CopyTree(string from, string to, string? skip = null)
    {
        Directory.CreateDirectory(to);
        foreach (string dir in Directory.EnumerateDirectories(from))
        {
            if (skip is not null && Full(skip).Equals(Full(dir), PathComparison))
                continue;
            CopyTree(dir, Path.Combine(to, Path.GetFileName(dir)), skip);
        }
        foreach (string file in Directory.EnumerateFiles(from))
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
    }

    /// <summary>Replaces a folder with a copy of another: the target is deleted, then the source is copied to it.</summary>
    /// <param name="from">The source folder; when it does not exist, the target is only deleted.</param>
    /// <param name="to">The folder to replace.</param>
    public static void Replace(string from, string to)
    {
        if (Directory.Exists(to))
            Directory.Delete(to, recursive: true);
        if (Directory.Exists(from))
            CopyTree(from, to);
    }

    /// <summary>
    /// The Claude Code folder: <c>CLAUDE_CONFIG_DIR</c> when it is set, else <c>.claude</c> in the home folder.
    /// </summary>
    public static string ClaudeHome() =>
        Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") is { Length: > 0 } dir
            ? dir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

    /// <summary>The folder that holds the logs of the sessions started in a folder.</summary>
    /// <param name="claudeHome">The Claude Code folder.</param>
    /// <param name="folder">The sessions' working directory.</param>
    public static string Project(string claudeHome, string folder) => Path.Combine(claudeHome, "projects", Slug(folder));
}
