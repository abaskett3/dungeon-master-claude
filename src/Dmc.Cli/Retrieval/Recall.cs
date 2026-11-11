using System.Text;

namespace Dmc.Retrieval;

/// <summary>
/// <c>dmc recall</c>'s work (research R1, R4, R5, R11; contracts/dmc-recall.md): index every line unit of the scope in
/// memory, rank the units by BM25 times their file's weight, and return the best as hit lines within the output cap.
/// Reads only; stores nothing.
/// </summary>
internal static class Recall
{
    /// <summary>BM25's term-frequency saturation, the prototype's value.</summary>
    const double K1 = 1.4;

    /// <summary>BM25's length normalisation, the prototype's value.</summary>
    const double B = 0.6;

    /// <summary>The most bytes the whole output may hold: about 1,500 estimated tokens by <c>dmc size</c>'s bytes ÷ 4 (FR-016).</summary>
    public const int CapBytes = 6000;

    /// <summary>Room kept for the closing <c>(N more lines matched)</c> line while hits are added.</summary>
    const int ClosingLineRoom = 40;

    /// <summary>Recalls words in a campaign.</summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name, already checked to exist.</param>
    /// <param name="query">The words, as typed.</param>
    /// <returns>The lines to print: notices, hits, and the closing line or <c>nothing matched</c>.</returns>
    /// <exception cref="DmcException">Every word of the query is a common one.</exception>
    public static List<string> Run(string dmcFolder, string campaign, string query)
    {
        var wanted = Words.Stems(query).ToHashSet(StringComparer.Ordinal);
        if (wanted.Count == 0)
            throw new DmcException($"nothing to search for in \"{query}\": every word is a common one");

        var notices = new List<string>();
        var units = new List<LineUnit>();
        foreach (var file in RecallScope.Build(dmcFolder, campaign, notices))
        {
            string path = Path.Combine(file.Place.Folder, file.Relative);
            MarkdownFile markdown;
            try
            {
                markdown = MarkdownFile.Read(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                notices.Add($"{Lookup.Shown(dmcFolder, path)}: cannot be read ({e.Message}), skipped");
                continue;
            }
            units.AddRange(LineUnits.Of(file, markdown));
        }

        var output = new List<string>();
        foreach (string notice in notices.Take(RecallScope.NoticesShown))
            output.Add(notice);
        if (notices.Count > RecallScope.NoticesShown)
            output.Add($"(+{notices.Count - RecallScope.NoticesShown} more skipped)");

        var ranked = Rank(units, wanted);
        if (ranked.Count == 0)
        {
            output.Add($"nothing matched: {query}");
            return output;
        }

        int used = output.Sum(Bytes);
        int shown = 0;
        foreach (var unit in ranked)
        {
            string line = Hit(unit, wanted);
            bool more = shown + 1 < ranked.Count;
            if (used + Bytes(line) + (more ? ClosingLineRoom : 0) > CapBytes)
                break;
            output.Add(line);
            used += Bytes(line);
            shown++;
        }
        if (shown < ranked.Count)
            output.Add($"({ranked.Count - shown} more lines matched)");
        return output;
    }

    /// <summary>
    /// The units holding at least one wanted stem, best first: BM25 (k1 = 1.4, b = 0.6, idf = ln(1 + (N − df + 0.5) /
    /// (df + 0.5)), length normalised by the average unit's stem count) times the unit's weight, compared at two
    /// decimals; ties by tier, distance, file and line, so the order is the same on every run.
    /// </summary>
    /// <param name="units">Every unit of the scope.</param>
    /// <param name="wanted">The query's stems.</param>
    public static List<LineUnit> Rank(IReadOnlyList<LineUnit> units, IReadOnlySet<string> wanted)
    {
        if (units.Count == 0)
            return [];
        double average = units.Average(u => (double)u.Stems.Count);
        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            foreach (string stem in unit.Stems.Distinct())
            {
                if (wanted.Contains(stem))
                    frequency[stem] = frequency.GetValueOrDefault(stem) + 1;
            }
        }

        var scored = new List<(LineUnit Unit, double Score)>();
        foreach (var unit in units)
        {
            double score = 0;
            foreach (string stem in wanted)
            {
                if (!frequency.TryGetValue(stem, out int df))
                    continue;
                int tf = unit.Stems.Count(s => s == stem);
                if (tf == 0)
                    continue;
                double idf = Math.Log(1 + (units.Count - df + 0.5) / (df + 0.5));
                score += idf * tf * (K1 + 1) / (tf + K1 * (1 - B + B * unit.Stems.Count / average));
            }
            if (score > 0)
                scored.Add((unit, score * unit.File.Weight));
        }

        return scored
            .OrderByDescending(s => Math.Round(s.Score, 2))
            .ThenBy(s => s.Unit.File.Tier)
            .ThenBy(s => s.Unit.File.Place.Distance)
            .ThenBy(s => s.Unit.File.Place.Label, StringComparer.Ordinal)
            .ThenBy(s => s.Unit.File.Relative, StringComparer.Ordinal)
            .ThenBy(s => s.Unit.Line)
            .Select(s => s.Unit)
            .ToList();
    }

    /// <summary>One hit as printed: <c>&lt;place&gt; &lt;file&gt;:&lt;line&gt; [&lt;session&gt;] [DM] | &lt;text&gt;</c>, the text trimmed around the match.</summary>
    /// <param name="unit">The unit.</param>
    /// <param name="wanted">The query's stems.</param>
    static string Hit(LineUnit unit, IReadOnlySet<string> wanted)
    {
        var line = new StringBuilder();
        line.Append(unit.File.Place.Label).Append(' ').Append(unit.File.Relative).Append(':').Append(unit.Line);
        if (unit.Session is not null)
            line.Append(' ').Append(unit.Session);
        if (unit.DmOnly)
            line.Append(" DM");
        line.Append(" | ").Append(Trim.Around(unit.Text, wanted));
        return line.ToString();
    }

    /// <summary>The bytes a printed line costs: its UTF-8 and its line end.</summary>
    /// <param name="line">The line.</param>
    static int Bytes(string line) => Encoding.UTF8.GetByteCount(line) + 1;
}
