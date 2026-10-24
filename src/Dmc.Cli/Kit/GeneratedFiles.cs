using System.Reflection;

namespace Dmc.Kit;

/// <summary>
/// The fixed table that says which file of <c>kit/</c> becomes which file of a DMC folder, and the guard that keeps every
/// target out of the player's own folders (FR-012). <c>kit/</c> is embedded in the program (see <c>Dmc.Cli.csproj</c>) under
/// the logical name <c>kit/&lt;path&gt;</c>:
/// <c>kit/dm-rules.md</c> becomes <c>CLAUDE.md</c>, <c>kit/settings.json</c> becomes <c>.claude/settings.json</c>,
/// <c>kit/skills/**</c> becomes <c>.claude/skills/**</c> and <c>kit/templates/**</c> becomes <c>templates/**</c>.
/// </summary>
public static class GeneratedFiles
{
    /// <summary>The version the generated files carry: the program's own (<see cref="CommandLine.Version"/>; <c>0.0.0-dev</c> in a development build).</summary>
    public static string Version => CommandLine.Version;

    /// <summary>
    /// The folders of a DMC folder that belong to the player. <c>dmc</c> never writes, replaces or removes anything under
    /// them: <see cref="Guard"/> refuses such a path whatever asks for it.
    /// </summary>
    public static IReadOnlyList<string> PlayerFolders { get; } = ["adventures", "feedback", "modules", "rules", "exports"];

    /// <summary>
    /// The table: a source in <c>kit/</c> and the target in the DMC folder. A source that ends in <c>/</c> is a folder
    /// (every file under it, keeping its path below it); any other is one file.
    /// </summary>
    internal static IReadOnlyList<(string Source, string Target)> Table { get; } =
    [
        ("kit/dm-rules.md", "CLAUDE.md"),
        ("kit/settings.json", ".claude/settings.json"),
        ("kit/skills/", ".claude/skills/"),
        ("kit/templates/", "templates/"),
    ];

    /// <summary>Every generated file of this program, in ordinal order of path, read from the embedded <c>kit/</c>.</summary>
    public static IReadOnlyList<GeneratedFile> All => _all.Value;

    /// <summary>The lazily read <see cref="All"/>.</summary>
    static readonly Lazy<IReadOnlyList<GeneratedFile>> _all = new(() => Read(typeof(GeneratedFiles).Assembly));

    /// <summary>The name an embedded resource of <c>kit/</c> starts with.</summary>
    internal const string Prefix = "kit/";

    /// <summary>
    /// The generated files of an assembly: each embedded resource whose name starts with <c>kit/</c>, mapped by
    /// <see cref="TargetOf"/>. Other resources (the map fonts) are not part of it.
    /// </summary>
    /// <param name="assembly">The assembly that holds the resources: this program's.</param>
    /// <exception cref="InvalidOperationException">A resource under <c>kit/</c> is not in the table, or would land under a player folder: a program error.</exception>
    internal static IReadOnlyList<GeneratedFile> Read(Assembly assembly)
    {
        var files = new List<GeneratedFile>();
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!Normalize(name).StartsWith(Prefix, StringComparison.Ordinal))
                continue;
            using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"resource {name} cannot be read");
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            files.Add(new GeneratedFile(TargetOf(name), copy.ToArray()));
        }
        return files.OrderBy(f => f.Path, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The embedded resource names of <c>kit/</c> in an assembly, with <c>/</c> as the separator, in ordinal order. The
    /// toolkit tests compare this set with the files under <c>kit/</c> in the repository.
    /// </summary>
    /// <param name="assembly">The assembly that holds the resources.</param>
    internal static IReadOnlyList<string> ResourceNames(Assembly assembly) =>
        assembly.GetManifestResourceNames().Select(Normalize).Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToList();

    /// <summary>A resource name with <c>\</c> turned into <c>/</c>: MSBuild's <c>%(RecursiveDir)</c> uses the platform's separator.</summary>
    /// <param name="name">The resource name.</param>
    internal static string Normalize(string name) => name.Replace('\\', '/');

    /// <summary>The path in a DMC folder of an embedded <c>kit/</c> resource, by the <see cref="Table"/>, after <see cref="Guard"/>.</summary>
    /// <param name="resourceName">The resource's name, <c>kit/...</c>, with either separator.</param>
    /// <exception cref="InvalidOperationException">The name is not in the table, or its target is under a player folder.</exception>
    internal static string TargetOf(string resourceName)
    {
        string name = Normalize(resourceName);
        foreach (var (source, target) in Table)
        {
            string? mapped = source.EndsWith('/')
                ? name.StartsWith(source, StringComparison.Ordinal) && name.Length > source.Length ? target + name[source.Length..] : null
                : name == source ? target : null;
            if (mapped is not null)
            {
                Guard(mapped);
                return mapped;
            }
        }
        throw new InvalidOperationException($"{resourceName} is in kit/ but not in the table of generated files");
    }

    /// <summary>
    /// The one rule of FR-012: <c>dmc</c> writes only inside the DMC folder and never under <c>adventures/</c>,
    /// <c>feedback/</c>, <c>modules/</c>, <c>rules/</c> or <c>exports/</c>. Applied to every target of the table and every
    /// path of an old manifest before anything is written or removed. It also refuses a path that is rooted or climbs out
    /// of the folder with <c>..</c>, which a damaged manifest could name.
    /// </summary>
    /// <param name="path">A path relative to the DMC folder, with <c>/</c> or <c>\</c>.</param>
    /// <exception cref="InvalidOperationException">The path is under a player folder, rooted, or leaves the folder.</exception>
    public static void Guard(string path)
    {
        string[] parts = Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || Path.IsPathRooted(path) || path.StartsWith('/') || parts.Contains("..") || parts[0].Contains(':'))
            throw new InvalidOperationException($"refusing to touch {path}: it is not a path inside the DMC folder");
        string first = parts[0] == "." && parts.Length > 1 ? parts[1] : parts[0];
        if (PlayerFolders.Any(f => string.Equals(f, first, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"refusing to touch {path}: {first}/ belongs to the player");
    }
}
