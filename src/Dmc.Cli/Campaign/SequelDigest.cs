using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.Campaign;

/// <summary>The closed campaign a sequel continues, checked against its closing backup.</summary>
/// <param name="Sequel">The sequel's folder name.</param>
/// <param name="SequelFolder">The sequel's folder.</param>
/// <param name="Name">The closed campaign's folder name.</param>
/// <param name="Folder">The closed campaign's folder.</param>
/// <param name="BackupFile">The closing backup's file name in the closed campaign's <c>backups/</c>.</param>
/// <param name="Sha256">The closing backup's SHA-256, 64 lower-case hex digits.</param>
/// <param name="Files">Every file the closing backup holds for the campaign, by path with <c>/</c> separators.</param>
internal sealed record ClosedSource(string Sequel, string SequelFolder, string Name, string Folder, string BackupFile, string Sha256,
    IReadOnlyDictionary<string, byte[]> Files)
{
    /// <summary>The digest's folder in the sequel, <c>digests/&lt;closed&gt;/</c>.</summary>
    public string DigestFolder => Path.Combine(SequelFolder, SequelDigest.FolderName, Name);

    /// <summary>The digest's folder as the output names it, <c>digests/&lt;closed&gt;/</c>.</summary>
    public string DigestLabel => $"{SequelDigest.FolderName}/{Name}/";
}

/// <summary>One accounted unit of the closed campaign (data-model.md § Accounted unit): a normalised line of an FR-021 source.</summary>
/// <param name="Id">Its id, as <see cref="ContentUnits.Id"/> gives it.</param>
/// <param name="Text">The normalised line.</param>
/// <param name="Path">The source file, with <c>/</c> separators.</param>
/// <param name="Line">Its 1-based line number in that file.</param>
internal sealed record AccountedUnit(string Id, string Text, string Path, int Line);

/// <summary>How an accounted unit stands in the digest.</summary>
internal enum UnitStatus
{
    /// <summary>Its text is in a digest file, alone or in a longer line.</summary>
    Copied,

    /// <summary>The accounting says it was merged into a line whose anchor is in the digest.</summary>
    Merged,

    /// <summary>The accounting drops it under a declared class.</summary>
    Dropped,

    /// <summary>Not in the digest and not validly accounted.</summary>
    Unaccounted,
}

/// <summary>
/// <c>dmc campaign sequel NAME --start|--list|--check</c> (spec 011 contracts/campaign-sequel.md; research R8 to R10): the exact
/// parts of a sequel's import of the closed campaign it continues. The refusals common to every form; <c>--start</c>, which
/// writes the digest's <c>index.md</c>, its <c>spine.md</c> and the copies of the digests the closed campaign holds, and prints
/// the parts plan; the accounted units, read from the closing backup, never the live folder; <c>--list</c>; and <c>--check</c>,
/// the whole proof. Only <c>--start</c> writes, and only under <c>adventures/NAME/digests/</c>; nothing is ever written to the
/// closed campaign (FR-016). The judgments (what goes under which subject, what is merged or dropped) are DMC's.
/// </summary>
internal static partial class SequelDigest
{
    /// <summary>The folder of a campaign that holds its digests.</summary>
    public const string FolderName = "digests";

    /// <summary>The digest's index file.</summary>
    public const string IndexFile = "index.md";

    /// <summary>The digest's scene spine.</summary>
    public const string SpineFile = "spine.md";

    /// <summary>The digest's accounting file.</summary>
    public const string AccountingName = "accounting.tsv";

    /// <summary>The digest format <c>--start</c> writes.</summary>
    const string Format = "1";

    /// <summary>The size a part of the plan is filled to, in bytes (about 100 KB).</summary>
    const int PartBytes = 100_000;

    /// <summary>How many problem or unaccounted lines are printed without <c>--all</c>.</summary>
    const int Cap = 40;

    /// <summary>The files part 1 of the plan always holds whole: what only the brief needs (FR-007).</summary>
    static readonly string[] BriefFiles = ["player-options.md", "story/dm-notes.md"];

    /// <summary>The drop classes a digest's accounting may use (research R9).</summary>
    public static readonly string[] Classes = ["superseded", "not-canon", "structure", "bookkeeping", "stat-block"];

    /// <summary>The kinds of subject file whose headings must be unique across their parts (research R10).</summary>
    public static readonly string[] Kinds = ["people", "places", "factions", "items", "world"];

    /// <summary>The index lines <c>--check</c> requires, in the order <c>--start</c> writes them.</summary>
    static readonly string[] IndexLines = ["Format", "Campaign", "Closing backup", "Chain", "Carried", "Files"];

    /// <summary>The files the index's Files line names.</summary>
    const string FilesValue = "people.md, places.md, factions.md, items.md, world.md, spine.md, secrets.md, accounting.tsv";

    /// <summary>A session log's file name; group 1 is its number.</summary>
    [GeneratedRegex(@"^session-(\d+)\.md$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionLog { get; }

    /// <summary>A scene heading of a session log; group 1 is its number, group 2 the text after <c>Scene M: </c>.</summary>
    [GeneratedRegex(@"^### Scene (\d+): (.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex SceneHeading { get; }

    /// <summary>A line of the digest's index; group 1 is its name, group 2 its value.</summary>
    [GeneratedRegex(@"^- \*\*([^*]+)\*\*:[ \t]*(.*?)[ \t]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IndexLine { get; }

    /// <summary>An HTML comment on one line.</summary>
    [GeneratedRegex(@"<!--.*?-->", RegexOptions.CultureInvariant)]
    private static partial Regex Comment { get; }

    /// <summary>A subject file of a kind: <c>people.md</c>, <c>people-2.md</c>; group 1 is the kind.</summary>
    [GeneratedRegex(@"^(people|places|factions|items|world)(?:-\d+)?\.md$", RegexOptions.CultureInvariant)]
    private static partial Regex KindFile { get; }

    /// <summary>
    /// Finds the closed campaign a sequel continues and checks it, with the contract's refusals in its order: the sequel is
    /// closed; it continues nothing; it continues an open campaign; the closing backup is missing; the backup fails the export
    /// check; the closed campaign differs from its backup.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="sequel">The sequel's name, as given.</param>
    /// <exception cref="DmcException">A refusal, or a campaign that is not in <c>adventures/</c>.</exception>
    public static ClosedSource Open(string root, string sequel)
    {
        string adventures = Path.Combine(root, "adventures");
        string name = CampaignExporter.FindCampaign(adventures, sequel);
        string folder = Path.Combine(adventures, name);
        if (ClosedCampaign.IsClosed(folder))
            throw new DmcException(ClosedCampaign.Refusal(name));

        string options = Path.Combine(folder, "player-options.md");
        string text = File.Exists(options) ? File.ReadAllText(options, Encoding.UTF8) : "";
        string continues = CampaignLinks.ContinuesOf(text, name)
            ?? throw new DmcException($"{name} continues nothing: its Continues line is none");
        string other = CampaignExporter.FindCampaign(adventures, continues);
        string otherFolder = Path.Combine(adventures, other);
        if (!ClosedCampaign.IsClosed(otherFolder))
            throw new DmcException($"{name} continues {other}, which is not closed: close it first with dmc-close-campaign");

        string? file = ClosedCampaign.Read(otherFolder)?.Backup ?? BackupFolder.Closing(otherFolder, other).FirstOrDefault();
        string? path = file is null ? null : Path.Combine(BackupFolder.Of(otherFolder), file);
        if (path is null || !File.Exists(path))
            throw new DmcException($"{other}'s closing backup is missing: {BackupFolder.FolderName}/{file} (it cannot be digested without it)");

        SortedDictionary<string, byte[]> held;
        try
        {
            held = BackupFolder.Open(path, other).ReadCampaign();
        }
        catch (DmcException e)
        {
            throw new DmcException($"{other}'s closing backup fails its check: {e.Message}");
        }
        if (CampaignVerifier.ClosedDifferences(otherFolder, held, out _).Count > 0)
            throw new DmcException($"{other} has changed since it closed: run dmc campaign verify {other}");

        return new ClosedSource(name, folder, other, otherFolder, file!, CampaignFiles.Hash(File.ReadAllBytes(path)), held);
    }

    /// <summary>
    /// <c>--start</c>: writes the digest's <c>index.md</c> (keeping a Carried line already there), its <c>spine.md</c> from the
    /// closing backup's session logs, and a byte-for-byte copy of each digest the closed campaign holds, then returns the
    /// started line and the parts plan.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="sequel">The sequel's name.</param>
    /// <exception cref="DmcException">A refusal of <see cref="Open"/>, or the digest's folder holds another campaign's digest.</exception>
    public static List<string> Start(string root, string sequel)
    {
        var source = Open(root, sequel);
        ClosedCampaign.Refuse(root, source.SequelFolder);
        string folder = source.DigestFolder;
        string index = Path.Combine(folder, IndexFile);
        string? carried = null;
        if (File.Exists(index))
        {
            var lines = ReadIndex(File.ReadAllText(index, Encoding.UTF8));
            if (lines.TryGetValue("Campaign", out var campaign) && !campaign.Value.Equals(source.Name, StringComparison.OrdinalIgnoreCase))
                throw new DmcException($"{source.DigestLabel} holds a digest of {campaign.Value}: it cannot be started over for {source.Name}");
            carried = lines.TryGetValue("Carried", out var line) ? line.Raw : null;
        }

        var chain = Chain(source);
        var (spine, sessions, scenes) = Spine(source);
        Directory.CreateDirectory(folder);
        WriteText(index, IndexText(source, chain, carried));
        WriteText(Path.Combine(folder, SpineFile), spine);
        foreach (string held in chain)
        {
            string copy = Path.Combine(source.SequelFolder, FolderName, held);
            if (Directory.Exists(copy))
                Directory.Delete(copy, recursive: true);
            foreach (var (path, bytes) in HeldDigest(source, held))
            {
                string target = Path.Combine(copy, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, bytes);
            }
        }

        var output = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"started {source.DigestLabel} (spine: {sessions} sessions, {scenes} scenes; chain: {(chain.Count == 0 ? "none" : string.Join(", ", chain))})"),
        };
        output.AddRange(Plan(source.Files));
        return output;
    }

    /// <summary><c>--list</c>: each unaccounted unit, at most 40 without <paramref name="all"/>, or <c>nothing unaccounted</c>.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="sequel">The sequel's name.</param>
    /// <param name="all">Print every unaccounted unit.</param>
    /// <exception cref="DmcException">A refusal of <see cref="Open"/>, an accounting file with a bad line, or two units sharing an id.</exception>
    public static List<string> List(string root, string sequel, bool all)
    {
        var source = Open(root, sequel);
        var (_, statuses, _) = Account(source);
        var open = statuses.Where(s => s.Status == UnitStatus.Unaccounted).Select(s => s.Unit).ToList();
        if (open.Count == 0)
            return ["nothing unaccounted"];
        var lines = (all ? open : open.Take(Cap)).Select(u => $"unaccounted: {u.Id} {u.Path}:{u.Line.ToString(CultureInfo.InvariantCulture)} | {u.Text}").ToList();
        if (!all && open.Count > Cap)
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"(+{open.Count - Cap} more: --all)"));
        return lines;
    }

    /// <summary>
    /// <c>--check</c>: the accounting is complete and valid, each chain copy is byte-identical to the closing backup's, the
    /// index's lines are there and its checksum is the backup's, no file is over the near-cap margin, and no subject heading
    /// is twice in its kind.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="sequel">The sequel's name.</param>
    /// <returns>The lines to print, and null on a pass or the failure's summary.</returns>
    /// <exception cref="DmcException">A refusal of <see cref="Open"/>, an accounting file with a bad line, or two units sharing an id.</exception>
    public static (List<string> Lines, string? Failure) Check(string root, string sequel)
    {
        var source = Open(root, sequel);
        var (units, statuses, accountingProblems) = Account(source);
        string label = source.DigestLabel;
        var problems = new List<string>();
        int unaccounted = statuses.Count(s => s.Status == UnitStatus.Unaccounted && !s.HasEntry);
        if (unaccounted > 0)
            problems.Add(string.Create(CultureInfo.InvariantCulture, $"unaccounted: {unaccounted} units (dmc campaign sequel {source.Sequel} --list)"));
        problems.AddRange(accountingProblems);
        problems.AddRange(ChainProblems(source));
        problems.AddRange(IndexProblems(source));

        var files = Directory.Exists(source.DigestFolder)
            ? new DirectoryInfo(source.DigestFolder).EnumerateFiles().OrderBy(f => f.Name, StringComparer.Ordinal).ToList()
            : [];
        long largest = 0;
        foreach (var file in files.Where(f => f.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            long tokens = file.Length / 4;
            largest = Math.Max(largest, tokens);
            if (tokens > Budgets.NearCapTokens)
                problems.Add($"too large: {label}{file.Name} {Measurement.FormatTokens(tokens)} tokens (split it by subject)");
        }
        problems.AddRange(HeadingProblems(source, files));

        if (problems.Count > 0)
        {
            var lines = problems.Take(Cap).ToList();
            if (problems.Count > Cap)
                lines.Add(string.Create(CultureInfo.InvariantCulture, $"(+{problems.Count - Cap} more)"));
            return (lines, problems.Count == 1 ? "1 problem" : string.Create(CultureInfo.InvariantCulture, $"{problems.Count} problems"));
        }

        int copied = statuses.Count(s => s.Status == UnitStatus.Copied);
        int merged = statuses.Count(s => s.Status == UnitStatus.Merged);
        int dropped = statuses.Count(s => s.Status == UnitStatus.Dropped);
        return ([string.Create(CultureInfo.InvariantCulture,
            $"checked {label}: {units.Count:N0} units ({copied:N0} copied, {merged:N0} merged, {dropped:N0} dropped), {files.Count} files, largest {Measurement.FormatTokens(largest)} tokens")], null);
    }

    /// <summary>
    /// The accounted units of a closed campaign's files (FR-021, research R9), in source order, each text once: every line of
    /// <c>story/canon.md</c>, <c>story/canon-archive.md</c>, <c>module-progress.md</c> and every file in <c>character-sheets/</c>;
    /// in <c>story/dm-notes.md</c> only § Secrets and in <c>story/summary.md</c> only § How it ended. A source the campaign
    /// lacks counts no units.
    /// </summary>
    /// <param name="files">The closed campaign's files, from its closing backup.</param>
    /// <exception cref="DmcException">Two different units share an id.</exception>
    public static List<AccountedUnit> Units(IReadOnlyDictionary<string, byte[]> files)
    {
        var units = new List<AccountedUnit>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Sources(files))
        {
            string text = Text(files[path]);
            var lines = path switch
            {
                "story/dm-notes.md" => SectionUnits(text, "Secrets"),
                "story/summary.md" => SectionUnits(text, "How it ended"),
                _ => ContentUnits.Lines(text),
            };
            foreach (var (line, unit) in lines)
            {
                if (!seen.Add(unit))
                    continue;
                string id = ContentUnits.Id(unit);
                if (ids.TryGetValue(id, out string? other))
                    throw new DmcException($"cannot account: two different units share the id {id} (\"{other}\" and \"{unit}\")");
                ids[id] = unit;
                units.Add(new AccountedUnit(id, unit, path, line));
            }
        }
        return units;
    }

    /// <summary>
    /// The digest's subject files and its secrets, by name: every <c>.md</c> file in <c>digests/&lt;closed&gt;/</c> but the index
    /// and the spine, which <c>--start</c> writes and which hold no copied line.
    /// </summary>
    /// <param name="digestFolder">The digest's folder.</param>
    /// <returns>Each file's name and text, in name order; empty when the folder is missing.</returns>
    public static List<(string Name, string Text)> DigestFiles(string digestFolder) =>
        Directory.Exists(digestFolder)
            ? [.. new DirectoryInfo(digestFolder).EnumerateFiles("*.md")
                .Where(f => f.Name is not (IndexFile or SpineFile))
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => (f.Name, Text(File.ReadAllBytes(f.FullName))))]
            : [];

    /// <summary>True for a digest file of one of the subject kinds (<see cref="Kinds"/>), parts included.</summary>
    /// <param name="name">The file's name.</param>
    public static bool IsSubjectFile(string name) => KindFile.IsMatch(name);

    /// <summary>True when a line is copied: the unit is a line of the digest, or is in a longer one at 20 characters or more, as <c>verify</c> allows.</summary>
    /// <param name="unit">The normalised unit.</param>
    /// <param name="digest">The digest's units.</param>
    /// <param name="longLines">The digest's units long enough to contain another.</param>
    public static bool IsCopied(string unit, HashSet<string> digest, IReadOnlyList<string> longLines) =>
        digest.Contains(unit)
        || (unit.Length >= CampaignVerifier.ContainedFloor && longLines.Any(l => l.Length > unit.Length && l.Contains(unit, StringComparison.Ordinal)));

    /// <summary>The names on the index's Carried line, or none when it says <c>none</c> or is missing.</summary>
    /// <param name="digestFolder">The digest's folder.</param>
    public static List<string> Carried(string digestFolder)
    {
        string index = Path.Combine(digestFolder, IndexFile);
        if (!File.Exists(index) || !ReadIndex(File.ReadAllText(index, Encoding.UTF8)).TryGetValue("Carried", out var line))
            return [];
        return [.. line.Value.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0 && !n.Equals("none", StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>The status of each unit, with whether it has an accounting line, and the problems with the accounting.</summary>
    /// <param name="source">The closed campaign.</param>
    /// <exception cref="DmcException">The accounting file has a bad line, or two units share an id.</exception>
    static (List<AccountedUnit> Units, List<(AccountedUnit Unit, UnitStatus Status, bool HasEntry)> Statuses, List<string> Problems) Account(ClosedSource source)
    {
        var units = Units(source.Files);
        var digestFiles = DigestFiles(source.DigestFolder);
        var digest = new HashSet<string>(digestFiles.SelectMany(f => ContentUnits.Split(f.Text)), StringComparer.Ordinal);
        var longLines = digest.Where(u => u.Length > CampaignVerifier.ContainedFloor).ToList();
        var accounting = AccountingFile.Read(Path.Combine(source.DigestFolder, AccountingName),
            source.DigestLabel + AccountingName, AccountingFile.Merged);

        var statuses = new List<(AccountedUnit, UnitStatus, bool)>();
        var problems = new List<string>();
        foreach (var unit in units)
        {
            bool has = accounting.Entries.TryGetValue(unit.Id, out var entry);
            if (IsCopied(unit.Text, digest, longLines))
            {
                statuses.Add((unit, UnitStatus.Copied, has));
                continue;
            }
            UnitStatus status = UnitStatus.Unaccounted;
            if (entry is { Disposition: Disposition.Reworded })
            {
                if (CampaignVerifier.AnchorFound(entry.Detail, digestFiles.Select(f => f.Text), digest))
                    status = UnitStatus.Merged;
                else
                    problems.Add($"accounting: {unit.Id}: anchor not found in the digest: {entry.Detail}");
            }
            else if (entry is not null)
            {
                if (Classes.Contains(entry.Class))
                    status = UnitStatus.Dropped;
                else
                    problems.Add($"accounting: {unit.Id}: dropped:{entry.Class} is not a declared class ({string.Join(", ", Classes)})");
            }
            statuses.Add((unit, status, has));
        }
        var known = units.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in accounting.Entries.Values.Where(e => !known.Contains(e.Id)).OrderBy(e => e.Line))
            problems.Add($"accounting: {entry.Id}: not a unit of {source.Name}'s accounted files");
        return (units, statuses, problems);
    }

    /// <summary>The accounted sources a closed campaign has, in the order the plan and the units take them.</summary>
    /// <param name="files">The closed campaign's files.</param>
    static IEnumerable<string> Sources(IReadOnlyDictionary<string, byte[]> files)
    {
        string[] fixedFirst = ["story/canon.md", "story/canon-archive.md"];
        foreach (string path in fixedFirst.Where(files.ContainsKey))
            yield return path;
        foreach (string path in files.Keys.Where(IsSheet).Order(StringComparer.Ordinal))
            yield return path;
        foreach (string path in new[] { "story/dm-notes.md", "story/summary.md", "module-progress.md" }.Where(files.ContainsKey))
            yield return path;
    }

    /// <summary>True for a markdown file in <c>character-sheets/</c>, at any depth.</summary>
    /// <param name="path">A campaign path.</param>
    static bool IsSheet(string path) =>
        path.StartsWith("character-sheets/", StringComparison.Ordinal) && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>The units of one <c>## </c> section of a text, with their line numbers in the whole text; none when there is no such section.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="heading">The section's heading, without <c>## </c>, compared ignoring case.</param>
    static List<(int Line, string Unit)> SectionUnits(string text, string heading)
    {
        string[] lines = text.Split('\n');
        int start = Array.FindIndex(lines, l => Measurement.IsSectionHeading(l) && l[3..].Trim().Equals(heading, StringComparison.OrdinalIgnoreCase));
        if (start < 0)
            return [];
        int end = Array.FindIndex(lines, start + 1, l => Measurement.IsSectionHeading(l));
        if (end < 0)
            end = lines.Length;
        return [.. ContentUnits.Lines(string.Join('\n', lines[(start + 1)..end])).Select(u => (u.Line + start + 1, u.Unit))];
    }

    /// <summary>
    /// The digests the closed campaign holds, nearest first: the one of the campaign it continues, then that digest's own Chain,
    /// then any other by name.
    /// </summary>
    /// <param name="source">The closed campaign.</param>
    static List<string> Chain(ClosedSource source)
    {
        var held = source.Files.Keys
            .Where(p => p.StartsWith(FolderName + "/", StringComparison.Ordinal) && p.Count(c => c == '/') >= 2)
            .Select(p => p.Split('/')[1])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var order = new List<string>();
        string? options = source.Files.TryGetValue("player-options.md", out var bytes) ? Text(bytes) : null;
        if (options is not null && CampaignLinks.ContinuesOf(options, source.Name) is { } nearest
            && held.FirstOrDefault(h => h.Equals(nearest, StringComparison.OrdinalIgnoreCase)) is { } first)
        {
            order.Add(first);
            if (source.Files.TryGetValue($"{FolderName}/{first}/{IndexFile}", out var index)
                && ReadIndex(Text(index)).TryGetValue("Chain", out var chain))
            {
                foreach (string name in chain.Value.Split(',').Select(n => n.Trim()))
                {
                    if (held.FirstOrDefault(h => h.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } found && !order.Contains(found))
                        order.Add(found);
                }
            }
        }
        order.AddRange(held.Where(h => !order.Contains(h)));
        return order;
    }

    /// <summary>The files of one digest the closed campaign holds, by path inside that digest's folder.</summary>
    /// <param name="source">The closed campaign.</param>
    /// <param name="held">The digest's folder name.</param>
    static IEnumerable<(string Path, byte[] Bytes)> HeldDigest(ClosedSource source, string held)
    {
        string prefix = $"{FolderName}/{held}/";
        return source.Files.Where(f => f.Key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(f => f.Key, StringComparer.Ordinal)
            .Select(f => (f.Key[prefix.Length..], f.Value));
    }

    /// <summary>
    /// The scene spine (data-model.md § Digest): per session log of the closing backup, in order, <c>## &lt;its # title&gt;
    /// (session-NNN.md)</c>, then <c>- sN.M &lt;the heading's text after "Scene M: "&gt;</c> per scene heading, word for word.
    /// </summary>
    /// <param name="source">The closed campaign.</param>
    /// <returns>The file's text and how many sessions and scenes it holds.</returns>
    static (string Text, int Sessions, int Scenes) Spine(ClosedSource source)
    {
        var text = new StringBuilder($"# Spine of {source.Name}\n");
        int sessions = 0, scenes = 0;
        foreach (var (path, bytes) in source.Files.Where(f => IsLog(f.Key)).OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            string file = path["story/sessions/".Length..];
            int number = int.Parse(SessionLog.Match(file).Groups[1].Value, CultureInfo.InvariantCulture);
            string[] lines = Text(bytes).Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            string title = lines.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
                ?? string.Create(CultureInfo.InvariantCulture, $"Session {number}");
            text.Append('\n').Append("## ").Append(title).Append(" (").Append(file).Append(")\n");
            sessions++;
            var heads = lines.Select(l => SceneHeading.Match(l)).Where(m => m.Success).ToList();
            if (heads.Count > 0)
                text.Append('\n');
            foreach (var head in heads)
            {
                text.Append(CultureInfo.InvariantCulture, $"- s{number}.{head.Groups[1].Value} {head.Groups[2].Value}\n");
                scenes++;
            }
        }
        return (text.ToString(), sessions, scenes);
    }

    /// <summary>True for a session log of the closed campaign: <c>story/sessions/session-NNN.md</c>.</summary>
    /// <param name="path">A campaign path.</param>
    static bool IsLog(string path) =>
        path.StartsWith("story/sessions/", StringComparison.Ordinal) && SessionLog.IsMatch(path["story/sessions/".Length..]);

    /// <summary>The text of the digest's index.</summary>
    /// <param name="source">The closed campaign.</param>
    /// <param name="chain">The digests copied with it, nearest first.</param>
    /// <param name="carried">The Carried line already there, kept as it is; null for <c>none</c>.</param>
    static string IndexText(ClosedSource source, IReadOnlyList<string> chain, string? carried) =>
        $"# Digest of {source.Name}\n\n" +
        $"- **Format**: {Format}\n" +
        $"- **Campaign**: {source.Name}\n" +
        $"- **Closing backup**: {source.BackupFile} sha256:{source.Sha256}\n" +
        $"- **Chain**: {(chain.Count == 0 ? "none" : string.Join(", ", chain))} <!-- the digests copied with it, nearest first -->\n" +
        (carried ?? "- **Carried**: none <!-- the characters this sequel carried over, as their headings in people.md give them -->") + "\n" +
        $"- **Files**: {FilesValue}\n";

    /// <summary>The <c>- **Name**: value</c> lines of an index, the first of each name: the value without comments, and the raw line.</summary>
    /// <param name="text">The index's text.</param>
    static Dictionary<string, (string Value, string Raw)> ReadIndex(string text)
    {
        var lines = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in text.Split('\n').Select(l => l.TrimEnd('\r')))
        {
            if (IndexLine.Match(raw) is { Success: true } match)
                lines.TryAdd(match.Groups[1].Value.Trim(), (Comment.Replace(match.Groups[2].Value, "").Trim(), raw));
        }
        return lines;
    }

    /// <summary>The <c>copy differs:</c> lines: each file of a held digest that the sequel's copy lacks, changes or adds to.</summary>
    /// <param name="source">The closed campaign.</param>
    static IEnumerable<string> ChainProblems(ClosedSource source)
    {
        foreach (string held in Chain(source))
        {
            var want = HeldDigest(source, held).ToDictionary(f => f.Path, f => f.Bytes, StringComparer.Ordinal);
            string copy = Path.Combine(source.SequelFolder, FolderName, held);
            var have = Directory.Exists(copy)
                ? Directory.EnumerateFiles(copy, "*", SearchOption.AllDirectories)
                    .ToDictionary(f => Path.GetRelativePath(copy, f).Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllBytes, StringComparer.Ordinal)
                : [];
            foreach (string path in want.Keys.Union(have.Keys).Order(StringComparer.Ordinal))
            {
                if (!want.TryGetValue(path, out var a) || !have.TryGetValue(path, out var b) || !a.AsSpan().SequenceEqual(b))
                    yield return $"copy differs: {FolderName}/{held}/{path}";
            }
        }
    }

    /// <summary>The <c>index:</c> lines: the index missing, a required line missing, another campaign named, or a checksum that is not the closing backup's.</summary>
    /// <param name="source">The closed campaign.</param>
    static IEnumerable<string> IndexProblems(ClosedSource source)
    {
        string path = Path.Combine(source.DigestFolder, IndexFile);
        if (!File.Exists(path))
        {
            yield return $"index: {source.DigestLabel}{IndexFile} is missing (dmc campaign sequel {source.Sequel} --start writes it)";
            yield break;
        }
        var lines = ReadIndex(File.ReadAllText(path, Encoding.UTF8));
        foreach (string name in IndexLines.Where(n => !lines.ContainsKey(n)))
            yield return $"index: no {name} line";
        if (lines.TryGetValue("Campaign", out var campaign) && !campaign.Value.Equals(source.Name, StringComparison.OrdinalIgnoreCase))
            yield return $"index: Campaign names {campaign.Value}, not {source.Name}";
        if (lines.TryGetValue("Closing backup", out var backup))
        {
            string[] parts = backup.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? sha = parts.FirstOrDefault(p => p.StartsWith("sha256:", StringComparison.Ordinal))?["sha256:".Length..];
            if (parts.Length == 0 || parts[0] != source.BackupFile)
                yield return $"index: Closing backup names {(parts.Length == 0 ? "nothing" : parts[0])}, not {source.BackupFile}";
            else if (!string.Equals(sha, source.Sha256, StringComparison.OrdinalIgnoreCase))
                yield return $"index: Closing backup's checksum differs from {BackupFolder.FolderName}/{source.BackupFile}";
        }
    }

    /// <summary>The <c>heading twice:</c> lines: a <c>## </c> subject heading found twice across the parts of one kind.</summary>
    /// <param name="source">The closed campaign.</param>
    /// <param name="files">The files of the digest's folder.</param>
    static IEnumerable<string> HeadingProblems(ClosedSource source, IReadOnlyList<FileInfo> files)
    {
        foreach (string kind in Kinds)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var twice = new List<string>();
            foreach (var file in files.Where(f => KindFile.Match(f.Name) is { Success: true } m && m.Groups[1].Value == kind))
            {
                string stripped = Comment.Replace(Text(File.ReadAllBytes(file.FullName)), "");
                foreach (string line in stripped.Split('\n').Select(l => l.TrimEnd('\r')).Where(Measurement.IsSectionHeading))
                {
                    string heading = line[3..].Trim();
                    if (!seen.Add(heading) && !twice.Contains(heading, StringComparer.OrdinalIgnoreCase))
                        twice.Add(heading);
                }
            }
            foreach (string heading in twice)
                yield return $"heading twice: {source.DigestLabel}{kind}: {heading}";
        }
    }

    /// <summary>One piece of the parts plan: a whole file, or a range of its lines split at <c>## </c> headings.</summary>
    /// <param name="Path">The campaign path.</param>
    /// <param name="From">The first line, 1-based; 0 for the whole file.</param>
    /// <param name="To">The last line.</param>
    /// <param name="Bytes">Its size.</param>
    sealed record Piece(string Path, int From, int To, long Bytes)
    {
        /// <summary>True when the piece is its whole file.</summary>
        public bool Whole => From == 0;
    }

    /// <summary>
    /// The parts plan: part 1, the files only the brief needs, whole; then the accounted sources but the DM notes, and the session
    /// logs, in parts of about 100 KB, a source split only at its <c>## </c> headings and a session log never split.
    /// </summary>
    /// <param name="files">The closed campaign's files.</param>
    static List<string> Plan(IReadOnlyDictionary<string, byte[]> files)
    {
        var lines = new List<string>();
        var brief = BriefFiles.Where(files.ContainsKey).ToList();
        lines.Add($"part 1: {(brief.Count == 0 ? "nothing" : string.Join(", ", brief))} (the brief)");

        var pieces = new List<Piece>();
        foreach (string path in Sources(files).Where(p => !BriefFiles.Contains(p)))
            pieces.AddRange(Pieces(path, files[path]));
        foreach (var (path, bytes) in files.Where(f => IsLog(f.Key)).OrderBy(f => f.Key, StringComparer.Ordinal))
            pieces.Add(new Piece(path, 0, 0, bytes.LongLength));

        var parts = new List<List<Piece>>();
        long filled = 0;
        foreach (var piece in pieces)
        {
            if (parts.Count == 0 || filled + piece.Bytes > PartBytes)
            {
                parts.Add([]);
                filled = 0;
            }
            parts[^1].Add(piece);
            filled += piece.Bytes;
        }
        for (int i = 0; i < parts.Count; i++)
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"part {i + 2}: {Describe(parts[i], pieces)}"));
        return lines;
    }

    /// <summary>A source as pieces: whole when it fits a part, else its <c>## </c> sections grouped up to a part's size.</summary>
    /// <param name="path">The campaign path.</param>
    /// <param name="bytes">Its bytes.</param>
    static IEnumerable<Piece> Pieces(string path, byte[] bytes)
    {
        if (bytes.LongLength <= PartBytes)
            return [new Piece(path, 0, 0, bytes.LongLength)];
        var lines = Measurement.Split(bytes);
        var pieces = new List<Piece>();
        int from = 1;
        long size = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (Measurement.IsSectionHeading(lines[i].Text) && size > 0 && size + SectionBytes(lines, i) > PartBytes)
            {
                pieces.Add(new Piece(path, from, i, size));
                from = i + 1;
                size = 0;
            }
            size += lines[i].Bytes;
        }
        pieces.Add(new Piece(path, from, lines.Count, size));
        return pieces;
    }

    /// <summary>The bytes of the <c>## </c> section starting at a line, up to the next one.</summary>
    /// <param name="lines">The file's lines.</param>
    /// <param name="start">The section's heading line, 0-based.</param>
    static long SectionBytes(IReadOnlyList<Line> lines, int start)
    {
        long size = lines[start].Bytes;
        for (int i = start + 1; i < lines.Count && !Measurement.IsSectionHeading(lines[i].Text); i++)
            size += lines[i].Bytes;
        return size;
    }

    /// <summary>The folders whose files a part names as a run: the sheets and the session logs.</summary>
    static readonly string[] GroupedFolders = ["character-sheets", "story/sessions"];

    /// <summary>
    /// How a part prints: a whole file by its path, a range as <c>path a-b</c>, and a run of two or more whole files of the
    /// sheets' or the logs' folder as <c>folder/ (N files)</c> when it is the whole folder, else <c>folder/first to last</c>.
    /// </summary>
    /// <param name="part">The part's pieces.</param>
    /// <param name="all">Every piece of the plan, to tell a whole folder.</param>
    static string Describe(IReadOnlyList<Piece> part, IReadOnlyList<Piece> all)
    {
        var said = new List<string>();
        for (int i = 0; i < part.Count;)
        {
            var piece = part[i];
            string folder = FolderOf(piece.Path);
            int end = i + 1;
            while (piece.Whole && end < part.Count && part[end].Whole && FolderOf(part[end].Path) == folder && GroupedFolders.Contains(folder))
                end++;
            if (end - i >= 2)
            {
                int inFolder = all.Count(p => FolderOf(p.Path) == folder);
                said.Add(end - i == inFolder
                    ? string.Create(CultureInfo.InvariantCulture, $"{folder}/ ({end - i} files)")
                    : $"{piece.Path} to {part[end - 1].Path[(folder.Length + 1)..]}");
            }
            else
            {
                said.Add(piece.Whole ? piece.Path : string.Create(CultureInfo.InvariantCulture, $"{piece.Path} {piece.From}-{piece.To}"));
            }
            i = end;
        }
        return string.Join(", ", said);
    }

    /// <summary>The folder part of a campaign path, or empty for a file at the top.</summary>
    /// <param name="path">A campaign path.</param>
    static string FolderOf(string path) => path.LastIndexOf('/') is var at and >= 0 ? path[..at] : "";

    /// <summary>A file's bytes as text, without a byte-order mark.</summary>
    /// <param name="bytes">The bytes.</param>
    internal static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimStart('﻿');

    /// <summary>Writes UTF-8 text with no byte-order mark.</summary>
    /// <param name="path">The file.</param>
    /// <param name="text">What it holds.</param>
    static void WriteText(string path, string text) => File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text));
}
