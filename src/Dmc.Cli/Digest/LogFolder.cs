using System.Text;
using Dmc.Maps;

namespace Dmc.Digest;

/// <summary>Where Claude Code keeps the session logs of this toolkit (<c>log_dir</c> of <c>transcript_digest.py</c>).</summary>
internal static class LogFolder
{
    /// <summary>
    /// <c>$CLAUDE_CONFIG_DIR</c> (or <c>~/.claude</c> when it is unset or empty), then <c>projects</c>, then the toolkit's
    /// path with every character outside <c>A-Za-z0-9</c> replaced by "-". A character is one UTF-16 unit, so a letter
    /// outside the BMP is two "-": that is how Claude Code names the folder (JavaScript's <c>replace(/[^a-zA-Z0-9]/g, "-")</c>
    /// works on UTF-16 units), so that dmc finds the folder where the script, which counts code points, does not.
    /// </summary>
    public static string Of(string root, Func<string, string?> environment)
    {
        string? configured = environment("CLAUDE_CONFIG_DIR");
        string baseFolder = string.IsNullOrEmpty(configured) ? Path.Combine(HomeFolder(environment), ".claude") : configured;
        return Path.Combine(baseFolder, "projects", ProjectName(root));
    }

    public static string Default() => Of(Toolkit.Root, Environment.GetEnvironmentVariable);

    public static string ProjectName(string root)
    {
        var name = new StringBuilder(root.Length);
        foreach (char c in root)
            name.Append(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' ? c : '-');
        return name.ToString();
    }

    /// <summary>
    /// Python's <c>os.path.expanduser("~")</c>: <c>USERPROFILE</c> on Windows (else <c>HOMEDRIVE</c> and <c>HOMEPATH</c>) and
    /// <c>HOME</c> elsewhere, so that a test or a launcher can point it somewhere; the account's folder if those are unset.
    /// </summary>
    static string HomeFolder(Func<string, string?> environment)
    {
        if (OperatingSystem.IsWindows())
        {
            string? profile = environment("USERPROFILE");
            if (!string.IsNullOrEmpty(profile))
                return profile;
            string? drive = environment("HOMEDRIVE"), path = environment("HOMEPATH");
            if (path is not null)
                return (drive ?? "") + path;
        }
        else if (environment("HOME") is { Length: > 0 } home)
        {
            return home;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
