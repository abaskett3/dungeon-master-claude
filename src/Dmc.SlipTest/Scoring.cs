using System.Globalization;
using System.Text;

namespace Dmc.SlipTest;

/// <summary>
/// <c>score</c> (FR-009): joins the scores with the hidden map, and reports each arm's slip rate with a 95% interval
/// from a bootstrap over fork points, the paired difference between arms, the pass line's verdict and the cost
/// (research R5).
/// </summary>
internal static class Scoring
{
    /// <summary>The bootstrap's samples.</summary>
    public const int Samples = 10_000;

    /// <summary>The bootstrap's seed, so a result can be computed again (the issue's number).</summary>
    public const int Seed = 249;

    /// <summary>One scored run.</summary>
    /// <param name="Point">The point id.</param>
    /// <param name="Arm">The arm.</param>
    /// <param name="Slip">Whether the reply slipped.</param>
    internal sealed record Scored(string Point, string Arm, bool Slip);

    /// <summary>A rate and its interval.</summary>
    /// <param name="Rate">The rate on the whole sample.</param>
    /// <param name="Low">The 2.5th percentile over the bootstrap samples.</param>
    /// <param name="High">The 97.5th percentile.</param>
    internal readonly record struct Estimate(double Rate, double Low, double High);

    /// <summary>Reads <c>export/scores.tsv</c> and <c>export/.map.tsv</c> and joins them.</summary>
    /// <param name="work">The work folder.</param>
    /// <exception cref="SlipTestException">A file is missing, an id is unknown, repeated or missing, or a row has no 0 or 1.</exception>
    public static List<Scored> Read(string work)
    {
        string dir = Path.Combine(work, "export");
        string mapPath = Path.Combine(dir, ".map.tsv"), scoresPath = Path.Combine(dir, "scores.tsv");
        if (!File.Exists(mapPath) || !File.Exists(scoresPath))
            throw new SlipTestException("no export: run slip-test export, then fill in export/scores.tsv");
        var map = File.ReadLines(mapPath).Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToDictionary(c => c[0], c => (Point: c[1], Arm: c[2]));
        var scored = new List<Scored>();
        var seen = new HashSet<string>();
        int n = 1;
        foreach (string line in File.ReadLines(scoresPath).Skip(1))
        {
            n++;
            if (line.Trim().Length == 0)
                continue;
            string[] c = line.Split('\t');
            if (!map.TryGetValue(c[0], out var at))
                throw new SlipTestException($"scores.tsv line {n}: id {c[0]} is not in the export");
            if (!seen.Add(c[0]))
                throw new SlipTestException($"scores.tsv line {n}: id {c[0]} is scored twice");
            if (c.Length < 2 || c[1].Trim() is not ("0" or "1"))
                throw new SlipTestException($"scores.tsv line {n}: slip must be 0 or 1");
            scored.Add(new Scored(at.Point, at.Arm, c[1].Trim() == "1"));
        }
        if (map.Keys.FirstOrDefault(id => !seen.Contains(id)) is { } missing)
            throw new SlipTestException($"scores.tsv has no row for id {missing}");
        return scored;
    }

    /// <summary>
    /// The bootstrap: each sample draws as many fork points as there are, with replacement, and computes a value from
    /// the drawn points. Returns the value on the whole sample and the 2.5th and 97.5th percentiles.
    /// </summary>
    /// <param name="points">The fork point ids.</param>
    /// <param name="value">The value of a sample, given the drawn ids; NaN when it is undefined (no runs).</param>
    public static Estimate Bootstrap(IReadOnlyList<string> points, Func<IReadOnlyList<string>, double> value)
    {
        var random = new Random(Seed);
        var values = new List<double>(Samples);
        var draw = new string[points.Count];
        for (int s = 0; s < Samples; s++)
        {
            for (int i = 0; i < draw.Length; i++)
                draw[i] = points[random.Next(points.Count)];
            double v = value(draw);
            if (!double.IsNaN(v))
                values.Add(v);
        }
        values.Sort();
        double whole = value(points);
        return values.Count == 0 ? new Estimate(whole, whole, whole) : new Estimate(whole, Percentile(values, 0.025), Percentile(values, 0.975));
    }

    /// <summary>A percentile of sorted values, by linear interpolation between the two nearest.</summary>
    /// <param name="sorted">The values, sorted.</param>
    /// <param name="p">The fraction, 0 to 1.</param>
    static double Percentile(List<double> sorted, double p)
    {
        double at = p * (sorted.Count - 1);
        int lo = (int)Math.Floor(at), hi = (int)Math.Ceiling(at);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (at - lo);
    }

    /// <summary>An arm's slip rate over drawn points: slips over runs, NaN when the points have no run of the arm.</summary>
    /// <param name="byPoint">Each point's runs and slips in the arm.</param>
    /// <param name="drawn">The drawn point ids.</param>
    static double Rate(IReadOnlyDictionary<string, (int Runs, int Slips)> byPoint, IReadOnlyList<string> drawn)
    {
        int runs = 0, slips = 0;
        foreach (string p in drawn)
            if (byPoint.TryGetValue(p, out var c))
            {
                runs += c.Runs;
                slips += c.Slips;
            }
        return runs == 0 ? double.NaN : (double)slips / runs;
    }

    /// <summary>Writes the result (contract): one line per arm, the difference and the pass line when the trial has one, and the cost.</summary>
    /// <param name="trial">The trial.</param>
    /// <param name="scored">The scored runs.</param>
    /// <param name="runs">Every finished run, base sessions included, for the cost.</param>
    public static string Report(Trial trial, IReadOnlyList<Scored> scored, IReadOnlyList<RunRow> runs)
    {
        var byArm = trial.Arms.ToDictionary(a => a.Name, a => (IReadOnlyDictionary<string, (int Runs, int Slips)>)scored
            .Where(s => s.Arm == a.Name).GroupBy(s => s.Point).ToDictionary(g => g.Key, g => (g.Count(), g.Count(s => s.Slip))));
        var points = scored.Select(s => s.Point).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();
        var o = new StringBuilder();
        o.Append("arm        runs  points  slips  rate   95% interval\n");
        foreach (var arm in trial.Arms)
        {
            var c = byArm[arm.Name];
            var e = Bootstrap(points, d => Rate(c, d));
            o.Append(CultureInfo.InvariantCulture, $"{arm.Name,-10} {c.Values.Sum(v => v.Runs),4}  {c.Count,6}  {c.Values.Sum(v => v.Slips),5}  {F(e.Rate)}  {F(e.Low)} to {F(e.High)}\n");
        }
        if (trial.Pass is { } pass)
        {
            var a = byArm[pass.Arm];
            var b = byArm[pass.Against];
            var shared = points.Where(p => a.ContainsKey(p) && b.ContainsKey(p)).ToList();
            var d = Bootstrap(shared, drawn => Rate(a, drawn) - Rate(b, drawn));
            bool passed = d.High < pass.Below;
            o.Append(CultureInfo.InvariantCulture, $"{pass.Arm} - {pass.Against}: {F(d.Rate)}, 95% interval {F(d.Low)} to {F(d.High)}\n");
            o.Append(CultureInfo.InvariantCulture, $"pass line: {pass.Arm} - {pass.Against} below {pass.Below}: {(passed ? "pass" : "fail")}\n");
        }
        long input = runs.Sum(r => r.Result.Input + r.Result.CacheRead + r.Result.CacheWrite);
        long cached = runs.Sum(r => r.Result.CacheRead);
        o.Append(CultureInfo.InvariantCulture, $"cost: {runs.Count} runs, ${runs.Sum(r => r.Result.CostUsd):0.00}, {input:N0} input tokens ({(input == 0 ? 0 : 100.0 * cached / input):0}% from the cache), {runs.Sum(r => r.Result.Output):N0} output tokens\n");
        return o.ToString();
    }

    /// <summary>A rate with three decimals.</summary>
    /// <param name="v">The rate.</param>
    static string F(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);
}
