namespace Dmc.Maps;

/// <summary>One kind of symbol in the grid, resolved: what it is drawn as, what the key calls it, and any colour.</summary>
/// <param name="Terrain">One of <see cref="MapRules.Terrains"/>.</param>
/// <param name="Name">The name shown in the key.</param>
/// <param name="Color">A colour from the legend entry, or null for the terrain's own.</param>
public sealed record LegendEntry(string Terrain, string Name, string? Color);

/// <summary>A creature on the map, checked.</summary>
/// <param name="Size">The declared width in cells (1 to 4); the panel shows it.</param>
/// <param name="SizeText">The size as Python would print it (<c>2</c>, or <c>2.0</c> if the file wrote it that way).</param>
/// <param name="Span">Cells across on this map: <paramref name="Size"/> at 5 ft, 1 on any bigger scale.</param>
/// <param name="Cells">Every cell it covers, row by row from the top-left one.</param>
public sealed record Token(string Id, string Name, string Kind, int Size, string SizeText, int Span, int Col, int Row,
    IReadOnlyList<(int Col, int Row)> Cells, string Note, bool Down);

/// <summary>A caption on a cell.</summary>
public sealed record Label(int Col, int Row, string Text);

/// <summary>A map that has passed <see cref="MapChecker.Check"/>, in the form the drawing code uses.</summary>
/// <param name="Grid">Each row as code points (a grid cell is one Python character).</param>
/// <param name="Legend">Every symbol the grid uses, in code point order.</param>
/// <param name="CellFt">Feet in one square: one of <see cref="MapRules.Scales"/>.</param>
/// <param name="North">"up", "right", "down", "left", or null when no direction is established.</param>
/// <param name="Edges">Captions for the top, bottom, left and right, only the ones that say something.</param>
/// <param name="Initiative">
/// The turn order of a fight: the tokens, in the order they act, each one found by the name the file gave it.
/// Empty when the file has no <c>"initiative"</c>, and then nothing is drawn for it.
/// </param>
/// <param name="Current">The name of the creature whose turn it is, one of <paramref name="Initiative"/>, or null.</param>
public sealed record MapModel(string Title, string Notes, int[][] Grid, int Cols, int Rows,
    SortedDictionary<int, LegendEntry> Legend, int CellFt, string ScaleNote, string? North,
    IReadOnlyList<Token> Tokens, IReadOnlyList<Label> Labels, IReadOnlyDictionary<string, string> Edges,
    IReadOnlyList<Token>? Initiative = null, string? Current = null);

/// <summary>The fixed rules of the map format: scales, symbols, terrains and kinds of creature.</summary>
public static class MapRules
{
    /// <summary>The standard map scales in feet per square: 5 ft, 30 ft, and 1, 6 and 60 miles.</summary>
    public static IReadOnlyList<int> Scales { get; } = [5, 30, 5280, 31680, 316800];

    public const int Mile = 5280;

    /// <summary>The built-in symbols: terrain and default name.</summary>
    public static IReadOnlyDictionary<int, (string Terrain, string Name)> Builtin { get; } =
        new Dictionary<int, (string, string)>
        {
            ['#'] = ("wall", "wall"),
            ['.'] = ("floor", "floor"),
            ['+'] = ("door", "door"),
            ['='] = ("window", "window"),
            ['~'] = ("water", "water"),
            ['^'] = ("hazard", "hazard"),
            ['>'] = ("stairs_down", "stairs down"),
            ['<'] = ("stairs_up", "stairs up"),
            ['T'] = ("tree", "tree"),
            ['o'] = ("pillar", "pillar"),
            ['_'] = ("cover", "low cover"),
            [','] = ("difficult", "difficult terrain"),
            ['?'] = ("unknown", "not yet seen"),
        };

    public static IReadOnlySet<string> Terrains { get; } = new HashSet<string>
    {
        "wall", "floor", "door", "window", "water", "hazard", "stairs_down", "stairs_up", "tree", "pillar", "cover",
        "difficult", "unknown", "feature",
    };

    /// <summary>Kinds of creature, in the order the error message lists them, with their colour.</summary>
    public static IReadOnlyList<(string Kind, string Color)> Kinds { get; } =
        [("pc", "#2f6fed"), ("ally", "#2e9e5b"), ("enemy", "#d0342c"), ("neutral", "#7d8590")];

    /// <summary>How far to turn the compass arrow for each direction that "up" can be.</summary>
    public static IReadOnlyDictionary<string, int> NorthTurn { get; } =
        new Dictionary<string, int> { ["up"] = 0, ["right"] = 90, ["down"] = 180, ["left"] = 270 };

    public static bool IsOverland(int cellFt) => cellFt >= Mile;

    /// <summary>Any square bigger than a creature's 5 ft: tokens share cells and size counts as 1.</summary>
    public static bool IsCoarse(int cellFt) => cellFt > 5;
}
