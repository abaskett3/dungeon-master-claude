namespace Dmc.Maps;

/// <summary>
/// Distances between creatures and cells. At 5 ft and 30 ft they count squares the 5e way (a diagonal step costs
/// the same as a straight one). On a mile-scale map they are the true straight line between cell centres. Above
/// 5 ft a square is bigger than a creature, so they are approximate.
/// </summary>
public static class Distances
{
    /// <summary>
    /// Squares between the nearest pair of cells of two creatures, counted the 5e way: the larger of the column and
    /// row differences. A creature that covers several cells is as near as its nearest cell.
    /// </summary>
    /// <param name="a">The cells of one creature. Must not be empty.</param>
    /// <param name="b">The cells of the other. Must not be empty.</param>
    public static int SquaresBetween(IEnumerable<(int Col, int Row)> a, IEnumerable<(int Col, int Row)> b)
    {
        var bList = b.ToList();
        return a.Min(p => bList.Min(q => Math.Max(Math.Abs(p.Col - q.Col), Math.Abs(p.Row - q.Row))));
    }

    /// <summary>True straight-line distance in squares between the nearest cell centres.</summary>
    public static double StraightSquares(IEnumerable<(int Col, int Row)> a, IEnumerable<(int Col, int Row)> b)
    {
        var bList = b.ToList();
        return a.Min(p => bList.Min(q =>
        {
            double dx = p.Col - q.Col, dy = p.Row - q.Row;
            return Math.Sqrt(dx * dx + dy * dy);
        }));
    }

    /// <summary>A distance in squares as text: feet on a foot-scale map, whole miles on an overland one.</summary>
    /// <param name="squares">The distance in squares.</param>
    /// <param name="cellFt">Feet in one square, from <see cref="MapRules.Scales"/>.</param>
    /// <returns>For example "30 ft", or "1 mile" / "6 miles" (miles round half to even).</returns>
    public static string FormatDistance(double squares, int cellFt)
    {
        if (MapRules.IsOverland(cellFt))
        {
            long miles = (long)Math.Round(squares * cellFt / MapRules.Mile, MidpointRounding.ToEven);
            return $"{miles} mile" + (miles == 1 ? "" : "s");
        }
        return $"{Py.G((long)(squares * cellFt))} ft";
    }

    /// <summary>Squares apart: Chebyshev on a foot-scale map (the 5e rule), a straight line on an overland one.</summary>
    public static double SquaresApart(IReadOnlyList<(int Col, int Row)> a, IReadOnlyList<(int Col, int Row)> b, int cellFt) =>
        MapRules.IsOverland(cellFt) ? StraightSquares(a, b) : SquaresBetween(a, b);

    /// <summary>A distance as text. Above 5 ft a square is bigger than a creature, so it is "about", and one cell is "same square".</summary>
    public static string DistanceText(double squares, int cellFt)
    {
        if (!MapRules.IsCoarse(cellFt))
            return FormatDistance(squares, cellFt);
        if (squares == 0)
            return $"same square (within {FormatDistance(1, cellFt)})";
        return "about " + FormatDistance(squares, cellFt);
    }

    /// <summary>The distance between two creatures as text: <see cref="SquaresApart"/> run through <see cref="DistanceText"/>.</summary>
    /// <param name="a">The cells of one creature.</param>
    /// <param name="b">The cells of the other.</param>
    /// <param name="cellFt">Feet in one square.</param>
    public static string Distance(IReadOnlyList<(int Col, int Row)> a, IReadOnlyList<(int Col, int Row)> b, int cellFt) =>
        DistanceText(SquaresApart(a, b, cellFt), cellFt);

    /// <summary>The line <c>--measure</c> prints for two cells.</summary>
    public static string MeasureText((int Col, int Row) a, (int Col, int Row) b, int cellFt)
    {
        double squares = SquaresApart([a], [b], cellFt);
        string span = $"{Cells.CellName(a.Col, a.Row)} to {Cells.CellName(b.Col, b.Row)}: ";
        string far = DistanceText(squares, cellFt);
        if (MapRules.IsCoarse(cellFt) && squares == 0)
            return span + far;
        if (!MapRules.IsOverland(cellFt)) // a count of squares only means something on a grid
            far = $"{squares} square{(squares == 1 ? "" : "s")}, {far}";
        return $"{span}{far} (straight line, walls ignored)";
    }

    /// <summary>The heading and one line for each pair of creatures; nothing for fewer than two.</summary>
    public static List<string> DistanceLines(MapModel m)
    {
        var tokens = m.Tokens;
        var lines = new List<string>();
        if (tokens.Count < 2)
            return lines;
        string scale = FormatDistance(1, m.CellFt);
        if (MapRules.IsOverland(m.CellFt))
            lines.Add($"Distances (1 cell = {scale}, straight line, walls ignored):");
        else if (MapRules.IsCoarse(m.CellFt)) // a foot scale above 5 ft: 30 ft
            lines.Add($"Distances (1 cell = {scale}, so about ±{scale}; straight line, walls ignored):");
        else
            lines.Add($"Distances (1 cell = {scale}, straight line, walls ignored; {scale} is adjacent):");
        for (int i = 0; i < tokens.Count; i++)
        {
            for (int j = i + 1; j < tokens.Count; j++)
            {
                var a = tokens[i];
                var b = tokens[j];
                lines.Add($"  {a.Id} {a.Name} ({Cells.CellName(a.Col, a.Row)})  <->  {b.Id} {b.Name} ({Cells.CellName(b.Col, b.Row)})"
                          + $":  {Distance(a.Cells, b.Cells, m.CellFt)}");
            }
        }
        return lines;
    }
}
