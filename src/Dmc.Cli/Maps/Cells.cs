using System.Globalization;
using System.Numerics;
using System.Text;

namespace Dmc.Maps;

/// <summary>Cell names: columns lettered A, B, C... from the left, rows numbered 1, 2, 3... from the top.</summary>
public static class Cells
{
    /// <summary>0 -> A, 25 -> Z, 26 -> AA.</summary>
    /// <param name="index">The column, counting from 0 at the left.</param>
    public static string ColName(int index)
    {
        var name = new StringBuilder();
        index++;
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            index = (index - 1) / 26;
            name.Insert(0, (char)(65 + rem));
        }
        return name.ToString();
    }

    /// <summary>The name of a cell: (0, 0) is "A1", (2, 4) is "C5".</summary>
    /// <param name="col">The column, from 0 at the left.</param>
    /// <param name="row">The row, from 0 at the top.</param>
    public static string CellName(int col, int row) => $"{ColName(col)}{row + 1}";

    /// <summary>
    /// Turn "E3" into (col, row), both 0-based. Reads the text as Python does: case ignored, ends trimmed, every
    /// letter in it counted as a column letter, and what follows them all must be digits.
    /// </summary>
    /// <param name="value">The cell name as it came from the file or the command line; a value that is not text is read as its Python <c>str</c>.</param>
    /// <param name="cols">The map's width in cells.</param>
    /// <param name="rows">The map's height in cells.</param>
    /// <param name="what">Names the thing the cell belongs to, for the error ("token 1 (Thorin)").</param>
    /// <exception cref="DmcException">The text is not a cell name, or names a cell outside the map.</exception>
    public static (int Col, int Row) Parse(object? value, int cols, int rows, string what)
    {
        string text = Py.Upper(Py.Strip(Py.Str(value)));
        int[] cps = Py.CodePoints(text);
        int[] letters = cps.Where(Py.IsAlpha).ToArray();
        int[] digits = cps[letters.Length..];
        if (letters.Length == 0 || digits.Length == 0 || !digits.All(Py.IsDigit))
            throw new DmcException($"{what}: \"{text}\" is not a cell like \"E3\".");

        BigInteger col = 0;
        foreach (int letter in letters)
            col = col * 26 + (letter - 64);
        col -= 1;
        BigInteger row = 0;
        foreach (int digit in digits)
            row = row * 10 + CharUnicodeInfo.GetDecimalDigitValue(Py.FromCodePoint(digit), 0);
        row -= 1;
        if (!(col >= 0 && col < cols && row >= 0 && row < rows))
            throw new DmcException($"{what}: {text} is off the map (the map is A1 to {CellName(cols - 1, rows - 1)}).");
        return ((int)col, (int)row);
    }
}
