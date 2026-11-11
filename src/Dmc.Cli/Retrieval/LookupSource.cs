namespace Dmc.Retrieval;

/// <summary>What kind of folder a lookup searches.</summary>
internal enum SourceKind
{
    /// <summary>A campaign folder in <c>adventures/</c>.</summary>
    Campaign,

    /// <summary>An ingested module in <c>modules/</c>.</summary>
    Module,

    /// <summary>The rules reference in <c>rules/</c>.</summary>
    Rules,
}

/// <summary>
/// The source a lookup searches, resolved from its command-line form (research R8): a campaign folder name, a module
/// folder name or <c>rules</c>, optionally prefixed with <c>adventures/</c> or <c>modules/</c> to say which, and
/// optionally followed by <c>/&lt;path&gt;</c> to search one file inside it.
/// </summary>
/// <param name="Kind">Campaign, module or rules.</param>
/// <param name="Name">The folder's name (<c>rules</c> for the rules reference), as it is printed in messages.</param>
/// <param name="Folder">The source's folder, absolute.</param>
/// <param name="File">The one file to search, relative to <paramref name="Folder"/> with <c>/</c>; null for all of them.</param>
internal sealed record LookupSource(SourceKind Kind, string Name, string Folder, string? File)
{
    /// <summary>How many names of each kind the "no such source" message lists before it stops.</summary>
    const int NamesListed = 5;

    /// <summary>Resolves a lookup's <c>SOURCE</c> argument.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="source">The argument as typed.</param>
    /// <returns>The source.</returns>
    /// <exception cref="DmcException">A staging folder, a name that is both a campaign and a module, an unknown name, or a path that is not one of the source's files (contracts/dmc-lookup.md § Failures).</exception>
    public static LookupSource Resolve(string dmcFolder, string source)
    {
        string text = source.Replace('\\', '/').Trim('/');
        SourceKind? forced = null;
        if (text.StartsWith("adventures/", StringComparison.Ordinal))
        {
            forced = SourceKind.Campaign;
            text = text["adventures/".Length..];
        }
        else if (text.StartsWith("modules/", StringComparison.Ordinal))
        {
            forced = SourceKind.Module;
            text = text["modules/".Length..];
        }

        int slash = text.IndexOf('/');
        string name = slash < 0 ? text : text[..slash];
        string? file = slash < 0 ? null : text[(slash + 1)..];
        if (name.StartsWith('.'))
            throw new DmcException($"{name} is not a source (a folder starting with a dot is a staging folder)");

        string adventures = Path.Combine(dmcFolder, "adventures");
        string modules = Path.Combine(dmcFolder, "modules");
        bool plain = name.Length > 0 && name != ".." && name.IndexOf(':') < 0;
        bool isCampaign = plain && forced != SourceKind.Module && Directory.Exists(Path.Combine(adventures, name));
        bool isModule = plain && forced != SourceKind.Campaign && Directory.Exists(Path.Combine(modules, name));

        LookupSource resolved;
        if (forced is null && name == "rules")
            resolved = new(SourceKind.Rules, "rules", Path.Combine(dmcFolder, "rules"), null);
        else if (isCampaign && isModule)
            throw new DmcException($"{name} is both a campaign and a module: say adventures/{name} or modules/{name}");
        else if (isCampaign)
            resolved = new(SourceKind.Campaign, name, Path.Combine(adventures, name), null);
        else if (isModule)
            resolved = new(SourceKind.Module, name, Path.Combine(modules, name), null);
        else
            throw new DmcException($"no source {name}: not a campaign, a module or rules (campaigns: {Listed(adventures)}; modules: {Listed(modules)})");

        if (resolved.Kind == SourceKind.Rules && !Directory.Exists(resolved.Folder))
            throw new DmcException("rules: no rules reference in rules/ (set one up with /dmc-setup-rules)");
        if (file is null)
            return resolved;
        if (!resolved.Files().Contains(file, StringComparer.Ordinal))
            throw new DmcException($"no {file} in {name}");
        return resolved with { File = file };
    }

    /// <summary>True when this is the rules reference kept as a PDF: <c>rules/</c> holds no markdown other than <c>index.md</c> (research R9).</summary>
    public bool IsPdfRules => Kind == SourceKind.Rules && Files().Count == 0;

    /// <summary>The markdown files the source searches, relative to its folder with <c>/</c>, in ordinal order. For a narrowed source, the one file.</summary>
    public IReadOnlyList<string> Files()
    {
        if (File is not null)
            return [File];
        return Kind switch
        {
            SourceKind.Campaign => SourceFiles.Campaign(Folder),
            SourceKind.Module => SourceFiles.Module(Folder),
            _ => SourceFiles.Rules(Folder),
        };
    }

    /// <summary>The folder names in a folder, at most <see cref="NamesListed"/>, for a message; dot folders left out.</summary>
    /// <param name="folder">The folder.</param>
    static string Listed(string folder)
    {
        if (!Directory.Exists(folder))
            return "none";
        var names = Directory.EnumerateDirectories(folder).Select(Path.GetFileName).OfType<string>()
            .Where(n => !n.StartsWith('.')).Order(StringComparer.Ordinal).ToList();
        if (names.Count == 0)
            return "none";
        string shown = string.Join(", ", names.Take(NamesListed));
        return names.Count > NamesListed ? $"{shown}, …" : shown;
    }
}

/// <summary>
/// Which markdown files of a campaign, a module or the rules reference the two commands read (research R6, R8). Every
/// path is relative to the folder, with <c>/</c>, in ordinal order, so output is the same on every system.
/// </summary>
internal static class SourceFiles
{
    /// <summary>The top-level folders of a campaign that are never searched: an import's record, exports and backups.</summary>
    static readonly string[] CampaignExcluded = ["import", "exports", "backups"];

    /// <summary>A campaign's <c>*.md</c>, outside <c>import/</c>, <c>exports/</c>, <c>backups/</c> and any folder starting with a dot.</summary>
    /// <param name="folder">The campaign folder.</param>
    public static List<string> Campaign(string folder) =>
        Markdown(folder, rel => rel.Split('/') is var parts && !CampaignExcluded.Contains(parts[0], StringComparer.OrdinalIgnoreCase));

    /// <summary>A module's <c>index.md</c>, its other top-level <c>*.md</c>, and <c>chapters/</c>; never <c>source/</c>.</summary>
    /// <param name="folder">The module folder.</param>
    public static List<string> Module(string folder) =>
        Markdown(folder, rel => !rel.Contains('/') || rel.StartsWith("chapters/", StringComparison.OrdinalIgnoreCase));

    /// <summary>The rules reference's markdown other than its top-level <c>index.md</c>, outside <c>extracted/</c>.</summary>
    /// <param name="folder">The <c>rules/</c> folder.</param>
    public static List<string> Rules(string folder) =>
        Markdown(folder, rel => !rel.Equals("index.md", StringComparison.OrdinalIgnoreCase)
                                && !rel.StartsWith("extracted/", StringComparison.OrdinalIgnoreCase));

    /// <summary>The <c>*.md</c> files under a folder that <paramref name="keep"/> accepts, skipping every folder whose name starts with a dot.</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="keep">Takes a relative path with <c>/</c>.</param>
    static List<string> Markdown(string folder, Func<string, bool> keep)
    {
        if (!Directory.Exists(folder))
            return [];
        return Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(folder, p).Replace('\\', '/'))
            .Where(rel => !rel.Split('/').SkipLast(1).Any(part => part.StartsWith('.')))
            .Where(keep)
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
