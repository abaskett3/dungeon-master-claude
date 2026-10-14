namespace Dmc;

/// <summary>Where the toolkit is on disk.</summary>
public static class Toolkit
{
    /// <summary>
    /// The toolkit root: the folder that holds <c>bin/</c>, <c>tools/</c> and <c>adventures/</c>. <c>dmc</c> lives
    /// in <c>bin/</c> there, so the root is the parent of the executable's folder. It comes from
    /// <see cref="AppContext.BaseDirectory"/>, never from the current directory or <c>Assembly.Location</c>
    /// (which is empty in a single-file build).
    /// </summary>
    public static string Root { get; } =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
}
