using System.Text;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>How one step between two linked campaigns goes, seen from the campaign the step starts at (research R2).</summary>
internal enum LinkStep
{
    /// <summary>To the campaign this one's Continues line names: an earlier campaign of the chain.</summary>
    Earlier,

    /// <summary>To a campaign whose Continues line names this one: a later campaign of the chain.</summary>
    Later,

    /// <summary>Over a Side campaign of line, either way.</summary>
    Side,

    /// <summary>Over a Shares a world with line, either way.</summary>
    World,
}

/// <summary>A campaign in a linked set (spec 011 data-model.md § Linked set).</summary>
/// <param name="Name">Its folder name, as the folder in <c>adventures/</c> spells it, or as the link spells it when the folder is missing.</param>
/// <param name="Folder">Its folder, absolute (which may not exist).</param>
/// <param name="Distance">Steps on the first path found from the start.</param>
/// <param name="Label">How it is linked to the start: <c>continues</c>, <c>sequel</c>, <c>side</c> or <c>world</c>.</param>
/// <param name="Present">False when its folder is not in <c>adventures/</c>.</param>
/// <param name="Closed">True when its folder holds the closed mark; false when it is open or missing.</param>
/// <param name="DigestHolder">The campaign nearer the start (the start included) that holds <c>digests/&lt;Name&gt;/</c>, nearest first; null when none does.</param>
internal sealed record LinkedCampaign(string Name, string Folder, int Distance, string Label, bool Present, bool Closed, string? DigestHolder);

/// <summary>A module a linked set reaches through a campaign's <c>module-progress.md</c>.</summary>
/// <param name="Name">Its folder name in <c>modules/</c>.</param>
/// <param name="Folder">Its folder, absolute.</param>
/// <param name="Distance">The distance of the nearest campaign that names it.</param>
/// <param name="Present">False when its folder is not in <c>modules/</c>.</param>
/// <param name="Via">The campaign that names it.</param>
internal sealed record LinkedModule(string Name, string Folder, int Distance, bool Present, string Via);

/// <summary>A problem with a link line, in one line (spec 011 FR-004).</summary>
/// <param name="Text">The notice, <c>&lt;campaign&gt;: &lt;line&gt; ...</c>.</param>
/// <param name="NotSearched">True when the notice means a campaign was not searched (a missing folder, a value that is not a folder name), so recall adds <c>, not searched</c>.</param>
internal sealed record LinkNotice(string Text, bool NotSearched);

/// <summary>
/// Every campaign reachable from one through links, both ways, each once (spec 011 FR-003): with the modules they reach
/// and the notices about their link lines.
/// </summary>
/// <param name="Start">The campaign the set is built from, as named.</param>
/// <param name="StartFolder">Its folder, absolute.</param>
/// <param name="Members">The other campaigns, nearest first, then by name.</param>
/// <param name="Modules">The modules, each once at its smallest distance, nearest first.</param>
/// <param name="Notices">The notices, in the order of the campaigns and their lines.</param>
internal sealed record LinkedSet(string Start, string StartFolder, IReadOnlyList<LinkedCampaign> Members, IReadOnlyList<LinkedModule> Modules, IReadOnlyList<LinkNotice> Notices)
{
    /// <summary>The member of this name, case-insensitively; null when the campaign is not in the set.</summary>
    /// <param name="name">A folder name.</param>
    public LinkedCampaign? Member(string name) => Members.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when <paramref name="name"/> is the start.</summary>
    /// <param name="name">A folder name.</param>
    public bool IsStart(string name) => name.Equals(Start, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The distance a digest's lines count at: that of the campaign it digests, or one more than its holder's when that
    /// campaign is not in the set (research R4).
    /// </summary>
    /// <param name="digested">The campaign the digest is of.</param>
    /// <param name="holderDistance">The distance of the campaign that holds it.</param>
    public int DigestDistance(string digested, int holderDistance) =>
        IsStart(digested) ? 0 : Member(digested)?.Distance ?? holderDistance + 1;

    /// <summary>
    /// The label a digest's hits carry: that of the campaign it digests; for one not in the set, the holder's label
    /// followed by a step back along the chain; none for a digest of the start itself (research R6).
    /// </summary>
    /// <param name="digested">The campaign the digest is of.</param>
    /// <param name="holderLabel">The holder's label, null for the start.</param>
    public string? DigestLabel(string digested, string? holderLabel) =>
        IsStart(digested) ? null : Member(digested)?.Label ?? CampaignLinks.Combine(holderLabel, LinkStep.Earlier);
}

/// <summary>
/// Reads campaigns' Continuity sections (or, failing that, their old Predecessor line) and builds linked sets (spec 011
/// research R1 to R3). The one home of the linked set: recall, <c>dmc campaign links</c> and <c>dmc size --check</c> all
/// ask here. Reads only.
/// </summary>
internal static partial class CampaignLinks
{
    /// <summary>The label of an earlier campaign of the chain.</summary>
    public const string Continues = "continues";

    /// <summary>The label of a later campaign of the chain.</summary>
    public const string Sequel = "sequel";

    /// <summary>The label of a campaign one Side campaign of step away.</summary>
    public const string Side = "side";

    /// <summary>The label of any other campaign of the set.</summary>
    public const string World = "world";

    /// <summary>The most notices printed before <c>(+N more)</c>.</summary>
    public const int NoticesShown = 3;

    /// <summary>The line name the old single link has in notices.</summary>
    const string PredecessorLineName = "Predecessor";

    /// <summary>One link a campaign's file names, or a problem with one of its lines, in file order.</summary>
    /// <param name="Line">The line's name: Continues, Side campaign of, Shares a world with or Predecessor.</param>
    /// <param name="Step">The step to the named campaign.</param>
    /// <param name="Name">The named campaign, cleaned; null for a problem.</param>
    /// <param name="Problem">The notice for a problem; null for a link.</param>
    sealed record Entry(string Line, LinkStep Step, string? Name, LinkNotice? Problem);

    /// <summary>Builds the linked set of a campaign.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name in <c>adventures/</c>, already checked to exist.</param>
    public static LinkedSet Build(string dmcFolder, string campaign)
    {
        string adventures = Path.Combine(dmcFolder, "adventures");
        string[] folders = Directory.Exists(adventures)
            ? [.. new DirectoryInfo(adventures).EnumerateDirectories().Select(d => d.Name).Where(n => !n.StartsWith('.')).Order(StringComparer.Ordinal)]
            : [];
        var actual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string folder in folders)
            actual.TryAdd(folder, folder);
        var entries = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
        foreach (string folder in folders)
            entries.TryAdd(folder, Read(Path.Combine(adventures, folder), folder));

        // The steps out of each campaign: the links it names first, in its file's order, then those that name it, by name.
        var steps = new Dictionary<string, List<(string To, LinkStep Step)>>(StringComparer.OrdinalIgnoreCase);
        List<(string To, LinkStep Step)> StepsOf(string name) => steps.TryGetValue(name, out var list) ? list : steps[name] = [];
        foreach (string folder in folders)
        {
            foreach (var entry in entries[folder].Where(e => e.Name is not null))
                StepsOf(folder).Add((actual.GetValueOrDefault(entry.Name!, entry.Name!), entry.Step));
        }
        foreach (string folder in folders)
        {
            foreach (var entry in entries[folder].Where(e => e.Name is not null && actual.ContainsKey(e.Name)))
                StepsOf(actual[entry.Name!]).Add((folder, Reverse(entry.Step)));
        }

        // Breadth first from the start; each campaign is reached once, by the first path found.
        var reached = new Dictionary<string, (int Distance, string? Label)>(StringComparer.OrdinalIgnoreCase) { [campaign] = (0, null) };
        var order = new List<string>();
        var queue = new Queue<string>([campaign]);
        while (queue.Count > 0)
        {
            string here = queue.Dequeue();
            if (!actual.ContainsKey(here))
                continue; // a missing folder names nothing
            var (distance, label) = reached[here];
            foreach (var (to, step) in StepsOf(here))
            {
                if (reached.ContainsKey(to))
                    continue;
                reached[to] = (distance + 1, Combine(label, step));
                order.Add(to);
                queue.Enqueue(to);
            }
        }

        string startFolder = Path.Combine(adventures, campaign);
        var sorted = order.OrderBy(n => reached[n].Distance).ThenBy(n => n, StringComparer.Ordinal).ToList();
        // Who holds whose digest: the start, then the present members, nearest first.
        var holders = new List<(string Name, string Folder, int Distance)> { (campaign, startFolder, 0) };
        holders.AddRange(sorted.Where(actual.ContainsKey).Select(n => (n, Path.Combine(adventures, n), reached[n].Distance)));
        bool Holds(string folder, string digested) => Directory.Exists(Path.Combine(folder, "digests", digested));

        var members = new List<LinkedCampaign>();
        foreach (string name in sorted)
        {
            var (distance, label) = reached[name];
            string folder = Path.Combine(adventures, name);
            bool present = actual.ContainsKey(name);
            string? holder = holders.Where(h => h.Distance < distance && Holds(h.Folder, name)).Select(h => h.Name).FirstOrDefault();
            members.Add(new LinkedCampaign(name, folder, distance, label!, present, present && ClosedCampaign.IsClosed(folder), holder));
        }

        var notices = new List<LinkNotice>();
        foreach (var (name, _, _) in holders)
        {
            foreach (var entry in entries.GetValueOrDefault(name) ?? [])
            {
                if (entry.Problem is not null)
                    notices.Add(entry.Problem);
                else if (!actual.ContainsKey(entry.Name!) && !holders.Any(h => Holds(h.Folder, entry.Name!)))
                    notices.Add(new LinkNotice($"{name}: {entry.Line} {entry.Name} is not in adventures/", NotSearched: true));
            }
        }

        var modules = new List<LinkedModule>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, folder, distance) in holders)
        {
            // The same reading of the Module folder line as dmc campaign export's, so the two never disagree.
            string? module = ReadText(Path.Combine(folder, "module-progress.md")) is { } text ? CampaignExporter.ModuleFolder(text) : null;
            if (module is null || !seen.Add(module))
                continue;
            string moduleFolder = Path.Combine(dmcFolder, "modules", module);
            modules.Add(new LinkedModule(module, moduleFolder, distance, Directory.Exists(moduleFolder), name));
        }

        return new LinkedSet(campaign, startFolder, members, modules, notices);
    }

    /// <summary>
    /// The label of a campaign one step beyond another (research R2): a run of steps back along the chain is
    /// <c>continues</c>, a run forward is <c>sequel</c>, a single Side campaign of step is <c>side</c>, anything else is
    /// <c>world</c>.
    /// </summary>
    /// <param name="label">The label of the campaign the step starts at; null for the start.</param>
    /// <param name="step">The step.</param>
    public static string Combine(string? label, LinkStep step) => (label, step) switch
    {
        (null, LinkStep.Earlier) or (Continues, LinkStep.Earlier) => Continues,
        (null, LinkStep.Later) or (Sequel, LinkStep.Later) => Sequel,
        (null, LinkStep.Side) => Side,
        _ => World,
    };

    /// <summary>The same link seen from the other campaign.</summary>
    /// <param name="step">The step as the naming campaign takes it.</param>
    static LinkStep Reverse(LinkStep step) => step switch
    {
        LinkStep.Earlier => LinkStep.Later,
        LinkStep.Later => LinkStep.Earlier,
        _ => step,
    };

    /// <summary>
    /// What <c>dmc campaign links</c> prints (contracts/campaign-links.md): one line per member, then one per module whose
    /// folder exists, then the notices; <c>no links</c> first when the set has no member.
    /// </summary>
    /// <param name="set">The set.</param>
    public static List<string> ListLines(LinkedSet set)
    {
        var lines = new List<string>();
        if (set.Members.Count == 0)
            lines.Add("no links");
        foreach (var m in set.Members)
        {
            string state = !m.Present ? "missing" : m.Closed ? "closed" : "open";
            lines.Add($"{m.Name} {m.Label} {m.Distance} {state}{(m.DigestHolder is null ? "" : " digest")}");
        }
        foreach (var module in set.Modules.Where(m => m.Present))
            lines.Add($"modules/{module.Name} module {module.Distance}");
        lines.AddRange(NoticeLines(set.Notices, "notice: ", ""));
        return lines;
    }

    /// <summary>The notices as lines, at most <see cref="NoticesShown"/>, then <c>(+N more)</c>.</summary>
    /// <param name="notices">The notices.</param>
    /// <param name="prefix">Written before each notice.</param>
    /// <param name="suffix">Written after each notice.</param>
    public static List<string> NoticeLines(IReadOnlyList<LinkNotice> notices, string prefix, string suffix)
    {
        var lines = notices.Take(NoticesShown).Select(n => prefix + n.Text + suffix).ToList();
        if (notices.Count > NoticesShown)
            lines.Add($"(+{notices.Count - NoticesShown} more)");
        return lines;
    }

    /// <summary>
    /// A campaign's link lines, in file order: its Continuity section's three lines, or its Predecessor line as Continues
    /// when it has no such section (FR-005). Each value is cleaned as a folder name; a value that is not one, a link to
    /// the campaign itself and a second value on a single-valued line become problems.
    /// </summary>
    /// <param name="folder">The campaign's folder.</param>
    /// <param name="campaign">Its folder name.</param>
    static List<Entry> Read(string folder, string campaign)
    {
        var entries = new List<Entry>();
        if (ReadText(Path.Combine(folder, "player-options.md")) is not { } text)
            return entries;
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        int section = Array.FindIndex(lines, l => ContinuityHeading().IsMatch(l));
        if (section >= 0)
        {
            for (int i = section + 1; i < lines.Length && !lines[i].StartsWith('#'); i++)
            {
                var match = ContinuityLine().Match(lines[i]);
                if (!match.Success)
                    continue;
                string line = match.Groups[1].Value;
                (string name, LinkStep step, bool many) = line.ToLowerInvariant() switch
                {
                    "continues" => ("Continues", LinkStep.Earlier, false),
                    "side campaign of" => ("Side campaign of", LinkStep.Side, false),
                    _ => ("Shares a world with", LinkStep.World, true),
                };
                Values(entries, campaign, name, step, many, match.Groups[2].Value);
            }
            return entries;
        }
        foreach (string line in lines)
        {
            if (PredecessorLine().Match(line) is { Success: true } match)
            {
                Values(entries, campaign, PredecessorLineName, LinkStep.Earlier, many: false, match.Groups[1].Value);
                break;
            }
        }
        return entries;
    }

    /// <summary>Adds a line's values to <paramref name="entries"/>: each a link, or a problem.</summary>
    /// <param name="entries">The campaign's entries so far.</param>
    /// <param name="campaign">The campaign whose line it is.</param>
    /// <param name="line">The line's name.</param>
    /// <param name="step">The step its links take.</param>
    /// <param name="many">True when the line takes any number of names.</param>
    /// <param name="value">The text after the line's colon.</param>
    static void Values(List<Entry> entries, string campaign, string line, LinkStep step, bool many, string value)
    {
        var names = CommentPattern().Replace(value, "").Split(',')
            .Select(Clean)
            .Where(n => n.Length > 0 && !n.Equals("none", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!many && names.Count > 1)
        {
            entries.Add(new Entry(line, step, null, new LinkNotice($"{campaign}: {line} has more than one name; only {names[0]} is followed", NotSearched: false)));
            names = names.GetRange(0, 1);
        }
        foreach (string name in names)
        {
            if (!IsPlainName(name))
                entries.Add(new Entry(line, step, null, new LinkNotice($"{campaign}: {line} {name} is not a campaign folder name", NotSearched: true)));
            else if (name.Equals(campaign, StringComparison.OrdinalIgnoreCase))
                entries.Add(new Entry(line, step, null, new LinkNotice($"{campaign}: {line} names the campaign itself", NotSearched: false)));
            else
                entries.Add(new Entry(line, step, name, null));
        }
    }

    /// <summary>A link's value as a folder name: backticks, a leading <c>adventures/</c> and slashes removed, backslashes read as slashes.</summary>
    /// <param name="value">One value, its comments already removed.</param>
    static string Clean(string value)
    {
        string name = value.Trim().Trim('`').Trim().Replace('\\', '/');
        if (name.StartsWith("adventures/", StringComparison.OrdinalIgnoreCase))
            name = name["adventures/".Length..];
        return name.Trim('/');
    }

    /// <summary>True for one plain folder name: no separator, no drive, not <c>.</c> or <c>..</c>, not a dot folder.</summary>
    /// <param name="name">The name.</param>
    static bool IsPlainName(string name) => name.IndexOfAny(['/', '\\', ':']) < 0 && !name.StartsWith('.');

    /// <summary>A file's text, or null when it is missing or cannot be read (recall names an unreadable file when it reads it).</summary>
    /// <param name="path">The file.</param>
    static string? ReadText(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The Continuity section's heading.</summary>
    [GeneratedRegex(@"^##[ \t]+Continuity[ \t]*$")]
    private static partial Regex ContinuityHeading();

    /// <summary>One of the Continuity section's three lines; group 1 is its name, group 2 its value.</summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*\*\*(Continues|Side campaign of|Shares a world with)\*\*:[ \t]*(.*?)[ \t]*$", RegexOptions.IgnoreCase)]
    private static partial Regex ContinuityLine();

    /// <summary>The old Predecessor line of a campaign's <c>player-options.md</c>; group 1 is its value.</summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*\*\*Predecessor\*\*:[ \t]*(.*?)[ \t]*$")]
    private static partial Regex PredecessorLine();

    /// <summary>An HTML comment.</summary>
    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex CommentPattern();
}
