namespace Dmc;

/// <summary>Where the toolkit is on disk.</summary>
public static class Toolkit
{
    /// <summary>
    /// The toolkit root: the folder that holds <c>tools/</c> and <c>adventures/</c>. A player's <c>dmc</c> lives in
    /// <c>bin/</c> there, so the root is the parent of the executable's folder; a developer's build in
    /// <c>artifacts/bin/</c> is one level further down, so the root is the nearest of the first three folders above the
    /// executable's that holds <c>tools/dmc-release.json</c> (and the parent when none does). It comes from
    /// <see cref="AppContext.BaseDirectory"/>, never from the current directory or <c>Assembly.Location</c>
    /// (which is empty in a single-file build).
    /// </summary>
    public static string Root { get; } = FindRoot(AppContext.BaseDirectory);

    internal static string FindRoot(string baseDirectory)
    {
        string parent = Path.GetFullPath(Path.Combine(baseDirectory, ".."));
        string? folder = parent;
        for (int i = 0; i < 3 && folder is not null; i++, folder = Path.GetDirectoryName(folder))
        {
            if (File.Exists(Path.Combine(folder, "tools", "dmc-release.json")))
                return folder;
        }
        return parent;
    }
}
