using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Retrieval;

/// <summary>A file's authority for recall, most authoritative first (research R1). The order breaks ties between equal scores.</summary>
internal enum Tier
{
    /// <summary><c>story/canon.md</c>.</summary>
    Canon,

    /// <summary><c>story/canon-archive.md</c>, <c>story/dm-notes-archive.md</c> and <c>character-sheets/*-history.md</c>.</summary>
    Archive,

    /// <summary>The other files in <c>character-sheets/</c>, <c>minor-npcs.md</c> included.</summary>
    Sheet,

    /// <summary>The campaign's other markdown: summary, DM notes, game state, options, module progress, place notes.</summary>
    Story,

    /// <summary><c>story/sessions/*.md</c>.</summary>
    Session,

    /// <summary>A module's digest and reference files.</summary>
    Module,
}

/// <summary>A campaign or module recall searches.</summary>
/// <param name="Label">How a hit names it: the campaign's folder name, or <c>modules/&lt;name&gt;</c>.</param>
/// <param name="Folder">Its folder, absolute.</param>
/// <param name="IsModule">True for a module.</param>
/// <param name="Distance">0 for the campaign recalled in, 1 for its predecessor, and so on; a module has its campaign's.</param>
internal sealed record Place(string Label, string Folder, bool IsModule, int Distance);

/// <summary>One file in recall's scope.</summary>
/// <param name="Place">The campaign or module it belongs to.</param>
/// <param name="Relative">Its path in the place's folder, with <c>/</c>.</param>
/// <param name="Tier">Its authority.</param>
/// <param name="Weight">The factor its lines' relevance is multiplied by.</param>
internal sealed record ScopeFile(Place Place, string Relative, Tier Tier, double Weight);

/// <summary>
/// What recall searches (research R1, R6, R11): the campaign; each campaign its Predecessor line names, to the end of
/// the chain; and each of their modules, once. Builds the list of files with their weights, and the notices for what
/// could not be searched.
/// </summary>
internal static partial class RecallScope
{
    /// <summary>The weight of a canon line: the highest, so canon wins over narrative that matches about as well.</summary>
    const double CanonWeight = 1.30;

    /// <summary>The weight of an archive or history line: curated facts that can no longer come up often.</summary>
    const double ArchiveWeight = 1.20;

    /// <summary>The weight of a sheet line.</summary>
    const double SheetWeight = 1.15;

    /// <summary>The weight of the campaign's other markdown.</summary>
    const double StoryWeight = 1.05;

    /// <summary>The weight of a session-log line: narrative, which the recall test found less reliable than curated lines.</summary>
    const double SessionWeight = 1.00;

    /// <summary>The weight of a module line: the module as printed, below every campaign file, since canon wins over it.</summary>
    const double ModuleWeight = 0.80;

    /// <summary>The factor applied once per step back along the Predecessor chain.</summary>
    const double StepFactor = 0.95;

    /// <summary>The most notices printed before <c>(+N more skipped)</c>.</summary>
    public const int NoticesShown = 3;

    /// <summary>Builds the scope of a recall in a campaign.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>, already checked to exist.</param>
    /// <param name="notices">Gets one line for each predecessor or module that could not be found.</param>
    /// <returns>The files to search, the campaign's first.</returns>
    public static List<ScopeFile> Build(string dmcFolder, string campaign, List<string> notices)
    {
        string adventures = Path.Combine(dmcFolder, "adventures");
        var campaigns = new List<Place>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? next = campaign;
        for (int distance = 0; next is not null; distance++)
        {
            var place = new Place(next, Path.Combine(adventures, next), false, distance);
            campaigns.Add(place);
            visited.Add(next);
            string? value = LineValue(Path.Combine(place.Folder, "player-options.md"), PredecessorLine());
            next = null;
            if (value is null)
                continue;
            string name = Clean(value, "adventures/");
            if (name.Length == 0 || name.Equals("none", StringComparison.OrdinalIgnoreCase) || visited.Contains(name))
                continue;
            if (!IsPlainName(name) || !Directory.Exists(Path.Combine(adventures, name)))
            {
                notices.Add($"{place.Label}: Predecessor {name} is not in adventures/, not searched");
                continue;
            }
            next = name;
        }

        var places = new List<Place>(campaigns);
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var place in campaigns)
        {
            string? value = LineValue(Path.Combine(place.Folder, "module-progress.md"), ModuleFolderLine());
            if (value is null)
                continue;
            string name = Clean(value, "modules/");
            if (name.Length == 0 || name.Contains('<') || !modules.Add(name))
                continue;
            string folder = Path.Combine(dmcFolder, "modules", name);
            if (!IsPlainName(name) || !Directory.Exists(folder))
            {
                notices.Add($"{place.Label}: module folder modules/{name}/ not found, not searched");
                continue;
            }
            places.Add(new Place($"modules/{name}", folder, true, place.Distance));
        }

        var files = new List<ScopeFile>();
        foreach (var place in places)
        {
            var relatives = place.IsModule ? SourceFiles.Module(place.Folder) : SourceFiles.Campaign(place.Folder);
            foreach (string relative in relatives)
            {
                var tier = place.IsModule ? Tier.Module : TierOf(relative);
                files.Add(new ScopeFile(place, relative, tier, WeightOf(tier) * Math.Pow(StepFactor, place.Distance)));
            }
        }
        return files;
    }

    /// <summary>The tier of a campaign file by its path.</summary>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    public static Tier TierOf(string relative)
    {
        string path = relative.ToLowerInvariant();
        if (path == "story/canon.md")
            return Tier.Canon;
        if (path is "story/canon-archive.md" or "story/dm-notes-archive.md"
            || (path.StartsWith("character-sheets/", StringComparison.Ordinal) && path.EndsWith("-history.md", StringComparison.Ordinal)))
            return Tier.Archive;
        if (path.StartsWith("character-sheets/", StringComparison.Ordinal))
            return Tier.Sheet;
        if (path.StartsWith("story/sessions/", StringComparison.Ordinal))
            return Tier.Session;
        return Tier.Story;
    }

    /// <summary>The weight of a tier at distance 0.</summary>
    /// <param name="tier">The tier.</param>
    public static double WeightOf(Tier tier) => tier switch
    {
        Tier.Canon => CanonWeight,
        Tier.Archive => ArchiveWeight,
        Tier.Sheet => SheetWeight,
        Tier.Story => StoryWeight,
        Tier.Session => SessionWeight,
        _ => ModuleWeight,
    };

    /// <summary>The value after the first line of a file that <paramref name="pattern"/> matches, or null when the file or the line is missing or unreadable.</summary>
    /// <param name="path">The file.</param>
    /// <param name="pattern">A pattern whose group 1 is the value.</param>
    static string? LineValue(string path, Regex pattern)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var match = pattern.Match(File.ReadAllText(path, Encoding.UTF8));
            return match.Success ? match.Groups[1].Value : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>A link's value as a folder name: an HTML comment, backticks, a leading <paramref name="prefix"/> and slashes removed.</summary>
    /// <param name="value">The text after the line's colon.</param>
    /// <param name="prefix"><c>adventures/</c> or <c>modules/</c>.</param>
    static string Clean(string value, string prefix)
    {
        string name = CommentPattern().Replace(value, "").Trim().Trim('`').Trim().Replace('\\', '/');
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            name = name[prefix.Length..];
        return name.Trim('/');
    }

    /// <summary>True for one plain folder name: no separator, no drive, not <c>.</c> or <c>..</c>, not a dot folder.</summary>
    /// <param name="name">The name.</param>
    static bool IsPlainName(string name) => name.IndexOfAny(['/', '\\', ':']) < 0 && !name.StartsWith('.');

    /// <summary>The Predecessor line of a campaign's <c>player-options.md</c>; group 1 is its value.</summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*\*\*Predecessor\*\*:[ \t]*(.*?)[ \t]*\r?$", RegexOptions.Multiline)]
    private static partial Regex PredecessorLine();

    /// <summary>The Module folder line of <c>module-progress.md</c>; group 1 is its value.</summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*\*\*Module folder\*\*:[ \t]*(.*?)[ \t]*\r?$", RegexOptions.Multiline)]
    private static partial Regex ModuleFolderLine();

    /// <summary>An HTML comment.</summary>
    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex CommentPattern();
}
