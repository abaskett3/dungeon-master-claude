using System.Text;

namespace Dmc.SlipTest;

/// <summary>
/// <c>export</c> (FR-008, #240 rule 2): every reply under a random id, in a random order, with no tools and no hook
/// output, for a score against the key; and the hidden map from id to arm and run. Each reply names its point and
/// the key's row for it, since the scorer needs both; only the arm and the run are hidden.
/// </summary>
internal static class BlindExport
{
    /// <summary>One reply to export.</summary>
    /// <param name="Point">The point id.</param>
    /// <param name="Arm">The arm.</param>
    /// <param name="Run">The run number.</param>
    /// <param name="Reply">The reply's text (<see cref="LogFile.Reply"/>).</param>
    /// <param name="Key">What counts as a slip at the point.</param>
    internal sealed record Item(string Point, string Arm, int Run, string Reply, string Key);

    /// <summary>Reads the reply of every finished run (base sessions left out).</summary>
    /// <exception cref="SlipTestException">A fork log does not hold its replayed message.</exception>
    /// <param name="trial">The trial.</param>
    /// <param name="points">The resolved points.</param>
    /// <param name="runs">The finished runs.</param>
    public static List<Item> Collect(Trial trial, IReadOnlyList<ResolvedPoint> points, IReadOnlyList<RunRow> runs)
    {
        var items = new List<Item>();
        foreach (var run in runs.Where(r => r.Point != "base"))
        {
            var point = points.First(p => p.Point.Id == run.Point);
            var fork = LogFile.Read(Path.Combine(trial.Work, "runs", run.Arm, $"{run.Point}-{run.Run}.jsonl"));
            int at = fork.LastTyped(point.Text);
            if (at < 0)
                throw new SlipTestException($"{run.Point} {run.Arm} {run.Run}: the replayed message is not in the fork log, so its reply cannot be found");
            items.Add(new Item(run.Point, run.Arm, run.Run, LogFile.Reply(fork.TurnAfter(at)), trial.Key[run.Point]));
        }
        return items;
    }

    /// <summary>Writes <c>export/replies.md</c>, <c>export/scores.tsv</c> and <c>export/.map.tsv</c>.</summary>
    /// <param name="work">The work folder.</param>
    /// <param name="items">The replies.</param>
    /// <param name="random">The source of the ids and the order.</param>
    /// <exception cref="SlipTestException">There is nothing to export, or <c>scores.tsv</c> already holds a score.</exception>
    public static void Write(string work, IReadOnlyList<Item> items, Random random)
    {
        if (items.Count == 0)
            throw new SlipTestException("no finished run to export: run slip-test run first");
        string dir = Path.Combine(work, "export");
        string scores = Path.Combine(dir, "scores.tsv");
        if (File.Exists(scores) && File.ReadLines(scores).Skip(1).Any(l => l.Split('\t') is [_, "0" or "1", ..]))
            throw new SlipTestException("export/scores.tsv already holds scores: move it away before exporting again");
        Directory.CreateDirectory(dir);
        var ids = new HashSet<string>();
        var rows = items.Select(item =>
        {
            string id;
            do id = Convert.ToHexStringLower(BitConverter.GetBytes(random.Next()))[..6]; while (!ids.Add(id));
            return (Id: id, Item: item);
        }).OrderBy(_ => random.Next()).ToList();

        var replies = new StringBuilder("# Replies to score\n\nScore each reply against key.tsv in export/scores.tsv: 1 for any slip, 0 for none.\n");
        foreach (var (id, item) in rows)
            replies.Append($"\n## {id}\n\nPoint {item.Point}. A slip here: {item.Key}\n\n{(item.Reply.Length > 0 ? item.Reply : "(no text)")}\n");
        File.WriteAllText(Path.Combine(dir, "replies.md"), replies.ToString());
        File.WriteAllText(scores, "id\tslip\tclass\tnote\n" + string.Concat(rows.Select(r => $"{r.Id}\t\t\t\n")));
        File.WriteAllText(Path.Combine(dir, ".map.tsv"), "id\tpoint\tarm\trun\n" + string.Concat(rows.Select(r => $"{r.Id}\t{r.Item.Point}\t{r.Item.Arm}\t{r.Item.Run}\n")));
    }
}
