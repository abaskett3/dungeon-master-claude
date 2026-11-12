using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>One item of the sample: where it is and what it says.</summary>
/// <param name="Where">The file, and its line when it is one line of a file: <c>places.md:44</c>, <c>character-sheets/npc-x.md</c>.</param>
/// <param name="Text">The fact's normalised line, or the person's name.</param>
/// <param name="Copy">For a fact, true when it is a copied line and false when it is session-only; for a person, true when they are on the Carried line.</param>
internal sealed record SampleItem(string Where, string Text, bool Copy);

/// <summary>
/// <c>dmc campaign sequel NAME --sample [--round N]</c> (spec 011 research R11; contracts/campaign-sequel.md § --sample): the
/// reproducible draw of the sample test. The seed is the first 8 bytes of the closing backup's SHA-256 plus the round, and
/// the draw uses SplitMix64, written out here, so the same backup and round always give the same 25 items whatever the
/// runtime. 20 facts come from the digest's subject files, split between copied lines and session-only lines in proportion;
/// 5 people come from the closing backup's sheets and minor NPCs, at least one of them carried when the index names any.
/// </summary>
internal static partial class SampleDraw
{
    /// <summary>How many facts a round draws.</summary>
    public const int Facts = 20;

    /// <summary>How many people a round draws.</summary>
    public const int People = 5;

    /// <summary>A line of the table of <c>minor-npcs.md</c>.</summary>
    [GeneratedRegex(@"^\s*\|", RegexOptions.CultureInvariant)]
    private static partial Regex TableRow { get; }

    /// <summary>A table separator row.</summary>
    [GeneratedRegex(@"^[\s|:\-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Separator { get; }

    /// <summary>Draws a round and returns the lines <c>--sample</c> prints.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="sequel">The sequel's name.</param>
    /// <param name="round">The round, 1 or more.</param>
    /// <exception cref="DmcException">A refusal of <see cref="SequelDigest.Open"/>; the digest has no fact lines; the closing backup has no characters.</exception>
    public static List<string> Run(string root, string sequel, int round)
    {
        var source = SequelDigest.Open(root, sequel);
        var (copies, sessionOnly) = FactLines(source);
        if (copies.Count + sessionOnly.Count == 0)
            throw new DmcException("the digest has no fact lines");
        var people = PeopleOf(source.Files, SequelDigest.Carried(source.DigestFolder));
        if (people.Count == 0)
            throw new DmcException("the closing backup has no characters");

        string seedHex = source.Sha256[..16];
        var random = new SplitMix64(ulong.Parse(seedHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) + (ulong)round);
        var (takeCopies, takeOnly) = Split(copies.Count, sessionOnly.Count);
        var facts = random.Draw(copies, takeCopies).Concat(random.Draw(sessionOnly, takeOnly)).ToList();

        var carried = people.Where(p => p.Copy).ToList();
        var drawn = new List<SampleItem>();
        if (people.Count <= People)
        {
            drawn.AddRange(people);
        }
        else
        {
            if (carried.Count > 0)
                drawn.AddRange(random.Draw(carried, 1));
            drawn.AddRange(random.Draw([.. people.Where(p => !drawn.Contains(p))], People - drawn.Count));
        }

        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"sample round {round} of {source.DigestLabel} (seed {seedHex}+{round}): {facts.Count(f => f.Copy)} copied facts, {facts.Count(f => !f.Copy)} session-only facts, {drawn.Count} people"),
        };
        lines.AddRange(facts.Select((f, i) => string.Create(CultureInfo.InvariantCulture, $"fact {i + 1}: {f.Where} | {f.Text}")));
        lines.AddRange(drawn.Select((p, i) => string.Create(CultureInfo.InvariantCulture, $"person {i + 1}: {p.Where} | {p.Text}{(p.Copy ? " (carried)" : "")}")));
        return lines;
    }

    /// <summary>
    /// How many copies and session-only lines a round takes: all of both when there are 20 or fewer; else 20 split in
    /// proportion, rounded, with at least one of each when both exist.
    /// </summary>
    /// <param name="copies">How many copied lines the digest has.</param>
    /// <param name="sessionOnly">How many session-only lines it has.</param>
    public static (int Copies, int SessionOnly) Split(int copies, int sessionOnly)
    {
        int total = copies + sessionOnly;
        if (total <= Facts)
            return (copies, sessionOnly);
        int c = (int)Math.Round(Facts * (double)copies / total, MidpointRounding.AwayFromZero);
        if (copies > 0 && sessionOnly > 0)
            c = Math.Clamp(c, 1, Facts - 1);
        c = Math.Min(c, copies);
        int s = Math.Min(Facts - c, sessionOnly);
        return (Facts - s, s);
    }

    /// <summary>
    /// The fact lines of the digest's subject files (people, places, factions, items and world, parts included): every line
    /// but blank lines, headings and comments, as a unit. A line is a copy when it matches an accounted unit of the closing
    /// backup, as <c>--check</c> matches them, and session-only when it does not.
    /// </summary>
    /// <param name="source">The closed campaign.</param>
    static (List<SampleItem> Copies, List<SampleItem> SessionOnly) FactLines(ClosedSource source)
    {
        var units = SequelDigest.Units(source.Files).Select(u => u.Text).ToList();
        var unitSet = units.ToHashSet(StringComparer.Ordinal);
        var copies = new List<SampleItem>();
        var only = new List<SampleItem>();
        foreach (var (name, text) in SequelDigest.DigestFiles(source.DigestFolder).Where(f => SequelDigest.IsSubjectFile(f.Name)))
        {
            string[] raw = text.Split('\n');
            foreach (var (line, unit) in ContentUnits.Lines(text))
            {
                if (raw[line - 1].TrimStart().StartsWith('#'))
                    continue;
                bool copy = unitSet.Contains(unit)
                    || units.Any(u => u.Length >= CampaignVerifier.ContainedFloor && unit.Length > u.Length && unit.Contains(u, StringComparison.Ordinal));
                (copy ? copies : only).Add(new SampleItem(string.Create(CultureInfo.InvariantCulture, $"{name}:{line}"), unit, copy));
            }
        }
        return (copies, only);
    }

    /// <summary>
    /// The people of the closing backup (research R11): the <c># </c> title of each sheet in <c>character-sheets/</c> but
    /// <c>*-history.md</c> and <c>minor-npcs.md</c>, and the Name cell of each row of <c>minor-npcs.md</c>'s table. A person
    /// whose name is on the Carried line is marked.
    /// </summary>
    /// <param name="files">The closed campaign's files.</param>
    /// <param name="carried">The names on the index's Carried line.</param>
    internal static List<SampleItem> PeopleOf(IReadOnlyDictionary<string, byte[]> files, IReadOnlyList<string> carried)
    {
        bool Carried(string name) => carried.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
        var people = new List<SampleItem>();
        foreach (var (path, bytes) in files.Where(f => IsTopSheet(f.Key)).OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            string file = path["character-sheets/".Length..];
            string[] lines = SequelDigest.Text(bytes).Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            if (file == "minor-npcs.md")
            {
                bool header = true;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!TableRow.IsMatch(lines[i]))
                    {
                        header = true;
                        continue;
                    }
                    if (Separator.IsMatch(lines[i]))
                        continue;
                    if (header)
                    {
                        header = false;
                        continue;
                    }
                    string name = lines[i].Trim().Trim('|').Split('|')[0].Trim();
                    if (name.Length > 0)
                        people.Add(new SampleItem(string.Create(CultureInfo.InvariantCulture, $"{path}:{i + 1}"), name, Carried(name)));
                }
            }
            else if (!file.EndsWith("-history.md", StringComparison.OrdinalIgnoreCase)
                && lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal)) is { } title)
            {
                string name = title[2..].Trim();
                people.Add(new SampleItem(path, name, Carried(name)));
            }
        }
        return people;
    }

    /// <summary>True for a markdown file directly in <c>character-sheets/</c>.</summary>
    /// <param name="path">A campaign path.</param>
    static bool IsTopSheet(string path) =>
        path.StartsWith("character-sheets/", StringComparison.Ordinal)
        && path.IndexOf('/', "character-sheets/".Length) < 0
        && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// SplitMix64 (Steele, Lea and Flood, 2014), the generator the draw uses: written out here so the draw never changes with
    /// the runtime, as <see cref="Random"/> may.
    /// </summary>
    /// <param name="state">The starting state: the seed plus the round.</param>
    sealed class SplitMix64(ulong state)
    {
        /// <summary>The next 64 bits.</summary>
        ulong Next()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>A number from 0 to <paramref name="n"/> minus 1.</summary>
        /// <param name="n">How many there are to choose from, 1 or more.</param>
        int Below(int n) => (int)(Next() % (ulong)n);

        /// <summary>Draws <paramref name="count"/> items without repeats (a partial Fisher-Yates shuffle), in the order drawn; all of them when there are fewer.</summary>
        /// <param name="items">What to draw from.</param>
        /// <param name="count">How many to draw.</param>
        public List<T> Draw<T>(IReadOnlyList<T> items, int count)
        {
            var pool = items.ToList();
            int take = Math.Min(count, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int j = i + Below(pool.Count - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            return pool.GetRange(0, take);
        }
    }
}
