using Dmc.Campaign;

namespace Dmc.Retrieval;

/// <summary>A file's authority for recall, most authoritative first (research R1; spec 011 R4). The order breaks ties between equal scores.</summary>
internal enum Tier
{
    /// <summary><c>story/canon.md</c>.</summary>
    Canon,

    /// <summary>A digest of a closed campaign, <c>digests/&lt;campaign&gt;/*.md</c> (spec 011): its curated facts, reorganised by subject.</summary>
    Digest,

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
/// <param name="Distance">0 for the campaign recalled in, else its distance in the linked set; a module has its nearest campaign's.</param>
/// <param name="Link">How a campaign other than the one recalled in is linked to it (<c>continues</c>, <c>sequel</c>, <c>side</c> or <c>world</c>); null for that campaign and for a module.</param>
internal sealed record Place(string Label, string Folder, bool IsModule, int Distance, string? Link = null);

/// <summary>One file in recall's scope.</summary>
/// <param name="Place">The campaign or module it belongs to.</param>
/// <param name="Relative">Its path in the place's folder, with <c>/</c>.</param>
/// <param name="Tier">Its authority.</param>
/// <param name="Weight">The factor its lines' relevance is multiplied by.</param>
/// <param name="DigestLink">For a digest's file, how the campaign it digests is linked to the one recalled in; null otherwise, and for a digest of that campaign itself.</param>
internal sealed record ScopeFile(Place Place, string Relative, Tier Tier, double Weight, string? DigestLink = null);

/// <summary>
/// What recall searches (research R1, R6, R11; spec 011 R4): the campaign; every campaign of its linked set
/// (<see cref="CampaignLinks"/>) whose folder is there; the digests any of them hold, except each digest's
/// <c>index.md</c>; and each module any of them names, once. Builds the list of files with their weights, and the
/// notices for what could not be searched.
/// </summary>
internal static class RecallScope
{
    /// <summary>The weight of a canon line: the highest, so canon wins over narrative that matches about as well.</summary>
    const double CanonWeight = 1.30;

    /// <summary>The weight of an archive or history line: curated facts that can no longer come up often. A digest's lines take it too.</summary>
    const double ArchiveWeight = 1.20;

    /// <summary>The weight of a sheet line.</summary>
    const double SheetWeight = 1.15;

    /// <summary>The weight of the campaign's other markdown.</summary>
    const double StoryWeight = 1.05;

    /// <summary>The weight of a session-log line: narrative, which the recall test found less reliable than curated lines.</summary>
    const double SessionWeight = 1.00;

    /// <summary>The weight of a module line: the module as printed, below every campaign file, since canon wins over it.</summary>
    const double ModuleWeight = 0.80;

    /// <summary>The factor applied once per step away in the linked set.</summary>
    const double StepFactor = 0.95;

    /// <summary>
    /// The factor on every file of a campaign whose digest a campaign nearer the start holds: its own folder is the last
    /// resort, so a digest's line (1.20 × 0.95 a step) ranks above the same line in the folder it digests (spec 011 R4).
    /// </summary>
    const double LastResortFactor = 0.85;

    /// <summary>The most notices printed before <c>(+N more skipped)</c>.</summary>
    public const int NoticesShown = CampaignLinks.NoticesShown;

    /// <summary>Builds the scope of a recall in a campaign.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>, already checked to exist.</param>
    /// <param name="notices">Gets one line for each link that could not be followed and each module that could not be found.</param>
    /// <returns>The files to search, the campaign's first.</returns>
    public static List<ScopeFile> Build(string dmcFolder, string campaign, List<string> notices)
    {
        var set = CampaignLinks.Build(dmcFolder, campaign);
        foreach (var notice in set.Notices)
            notices.Add(notice.NotSearched ? notice.Text + ", not searched" : notice.Text);

        var places = new List<(Place Place, bool LastResort)> { (new Place(campaign, set.StartFolder, false, 0), false) };
        foreach (var member in set.Members.Where(m => m.Present))
            places.Add((new Place(member.Name, member.Folder, false, member.Distance, member.Label), member.DigestHolder is not null));
        foreach (var module in set.Modules)
        {
            if (!module.Present)
            {
                notices.Add($"{module.Via}: module folder modules/{module.Name}/ not found, not searched");
                continue;
            }
            places.Add((new Place($"modules/{module.Name}", module.Folder, true, module.Distance), false));
        }

        var files = new List<ScopeFile>();
        foreach (var (place, lastResort) in places)
        {
            double factor = lastResort ? LastResortFactor : 1.0;
            if (place.IsModule)
            {
                foreach (string relative in SourceFiles.Module(place.Folder))
                    files.Add(new ScopeFile(place, relative, Tier.Module, ModuleWeight * Math.Pow(StepFactor, place.Distance)));
                continue;
            }
            foreach (string relative in SourceFiles.Campaign(place.Folder))
            {
                var tier = TierOf(relative);
                int distance = place.Distance;
                string? digestLink = null;
                if (tier == Tier.Digest)
                {
                    string[] parts = relative.Split('/');
                    // A digest's index.md holds its format, backup, chain and file list: no fact to recall.
                    if (parts.Length == 3 && parts[2].Equals("index.md", StringComparison.OrdinalIgnoreCase))
                        continue;
                    distance = set.DigestDistance(parts[1], place.Distance);
                    digestLink = set.DigestLabel(parts[1], place.Link);
                }
                files.Add(new ScopeFile(place, relative, tier, WeightOf(tier) * Math.Pow(StepFactor, distance) * factor, digestLink));
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
        if (path.StartsWith("digests/", StringComparison.Ordinal) && path.Split('/').Length >= 3)
            return Tier.Digest;
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
        Tier.Digest or Tier.Archive => ArchiveWeight,
        Tier.Sheet => SheetWeight,
        Tier.Story => StoryWeight,
        Tier.Session => SessionWeight,
        _ => ModuleWeight,
    };
}
