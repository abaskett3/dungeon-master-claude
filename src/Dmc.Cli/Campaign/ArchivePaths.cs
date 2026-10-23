namespace Dmc.Campaign;

/// <summary>
/// The portable-path rules of research R6, applied to every path in an export on every operating system, so an
/// archive made on Linux that Windows could not store is refused everywhere with the same words. A path is relative,
/// uses <c>/</c>, has no empty, <c>.</c> or <c>..</c> segment, and no segment holds a character or name Windows cannot
/// store.
/// </summary>
internal static class ArchivePaths
{
    /// <summary>The characters Windows cannot store in a file name (the path separators are handled apart).</summary>
    const string Forbidden = "<>:\"|?*";

    /// <summary>The names Windows reserves, whatever their extension (the name is compared without its extension, ignoring case).</summary>
    static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        // The console devices, and the superscript digits Windows also reads as port numbers.
        "CONIN$", "CONOUT$", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³",
    };

    /// <summary>Why a path cannot be imported.</summary>
    /// <param name="path">An entry path as written in the archive or the manifest, without a trailing <c>/</c>.</param>
    /// <returns>A short lower-case reason (it goes inside parentheses in the message), or null when the path is fine.</returns>
    public static string? Refusal(string path)
    {
        if (path.Length == 0)
            return "empty path";
        if (path[0] == '/')
            return "absolute path";
        if (path.Contains('\\'))
            return "contains a backslash";
        if (path.Length >= 2 && path[1] == ':' && char.IsAsciiLetter(path[0]))
            return "has a drive";

        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0)
                return "has an empty segment";
            if (segment == ".")
                return "contains .";
            if (segment == "..")
                return "contains ..";
            foreach (char c in segment)
            {
                if (c < ' ')
                    return "contains a control character";
                if (Forbidden.Contains(c))
                    return $"contains {c}";
            }
            if (segment[^1] is '.' or ' ')
                return "ends with a dot or a space";

            // The reserved name is what comes before the first dot, and Windows ignores spaces left before the dot.
            int dot = segment.IndexOf('.');
            string stem = (dot < 0 ? segment : segment[..dot]).TrimEnd(' ');
            if (Reserved.Contains(stem))
                return $"reserved name {stem}";
        }
        return null;
    }

    /// <summary>The first two paths of the list that are the same when case is ignored, because a case-insensitive file system would make them one file.</summary>
    /// <param name="paths">Every path of the campaign, files and folders together.</param>
    /// <returns>The earlier path and the later one, or null when there is no clash.</returns>
    public static (string First, string Second)? CaseClash(IEnumerable<string> paths)
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            if (!seen.TryAdd(path, path))
                return (seen[path], path);
        }
        return null;
    }

    /// <summary>The path of an entry under a folder, after checking that the result is still inside it (defence in depth against "zip slip": the path was already checked by <see cref="Refusal"/>).</summary>
    /// <param name="folder">The folder to resolve under.</param>
    /// <param name="path">An entry path that <see cref="Refusal"/> accepted.</param>
    /// <returns>The full path on this system.</returns>
    /// <exception cref="DmcException">The path leads outside <paramref name="folder"/>.</exception>
    public static string Combine(string folder, string path)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(root, StringComparison.Ordinal))
            throw new DmcException($"{path} would be written outside {folder}");
        return full;
    }
}
