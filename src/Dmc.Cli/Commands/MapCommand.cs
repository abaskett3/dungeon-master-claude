using Dmc.Maps;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc map &lt;map.json&gt; [--png] [--open] [--measure FROM TO]</c>: replaced <c>scripts/map_render.py</c>. It checks a
/// map file, draws it as <c>live.html</c> and <c>live-data.js</c> next to it, and prints the distances between the
/// creatures. The format of a map file is in <c>.claude/skills/draw-map/map-format.md</c>.
/// </summary>
/// <remarks>
/// Arguments are read the way Python's <c>argparse</c> reads them (an option may be shortened to a unique prefix, and
/// <c>--option=value</c> is understood), with its messages, but a mistake is one line and exit code 2, never the usage text.
/// </remarks>
public sealed class MapCommand(Action<string>? open = null) : ICommand
{
    readonly Action<string> _open = open ?? Browser.Open;

    public string Name => "map";

    public string Summary => "draw a map for the player's browser: dmc map <map.json> [--png] [--open] [--measure A B]";

    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc map <map.json> [--png] [--open]
       dmc map <map.json> --measure FROM TO

Draws a place or scene file as live.html and live-data.js next to it, and prints the distance between every pair of
creatures. The format of the file is in .claude/skills/draw-map/map-format.md.

  --png      also write live.png, the same picture, for DMC to look at with Read
  --open     also open live.html in the default browser
  --measure  only print the distance between two cells, draw nothing
""".Replace("\r\n", "\n");

    static readonly string[] Options = ["--help", "--png", "--open", "--measure"];

    sealed record Arguments(string? Map, bool Png, bool Open, string[]? Measure, bool Help);

    /// <summary>
    /// Check the map and draw it (or, with <c>--measure</c>, print one distance and draw nothing). The turn order
    /// (<c>"initiative"</c> and <c>"current"</c>) is checked and drawn only when drawing.
    /// </summary>
    public int Run(string[] args, Io io)
    {
        var parsed = Parse(args);
        if (parsed.Help)
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        string mapPath = parsed.Map!;
        // --measure draws nothing, so a stale turn order must not stop it.
        var model = MapChecker.Check(MapChecker.LoadMap(mapPath), turnOrder: parsed.Measure is null);

        if (parsed.Measure is not null)
        {
            var a = Cells.Parse(parsed.Measure[0], model.Cols, model.Rows, "--measure");
            var b = Cells.Parse(parsed.Measure[1], model.Cols, model.Rows, "--measure");
            io.Out.WriteLine(Distances.MeasureText(a, b, model.CellFt));
            return 0;
        }

        string svg = SvgDrawing.Render(model);
        string folder = Path.GetDirectoryName(Path.GetFullPath(mapPath)) ?? "";
        MapPage.WriteText(Path.Combine(folder, "live-data.js"), MapPage.Data(DateTime.Now, model.Title, svg));
        string page = Path.Combine(folder, "live.html");
        MapPage.WriteText(page, MapPage.Html);

        io.Out.WriteLine($"Drew \"{model.Title}\": {model.Cols} x {model.Rows} cells, {model.Tokens.Count} creatures.");
        io.Out.WriteLine($"Player's page: {page}");
        if (parsed.Png)
        {
            string png = Path.Combine(folder, "live.png");
            MapPng.Write(svg, png);
            io.Out.WriteLine($"Picture to check: {png}");
        }
        foreach (string line in Distances.DistanceLines(model))
            io.Out.WriteLine(line);
        if (parsed.Open)
            _open("file:///" + page.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/'));
        return 0;
    }

    /// <summary>Read the arguments as <c>argparse</c> does, in order, with its messages.</summary>
    static Arguments Parse(string[] args)
    {
        string? map = null;
        bool png = false, open = false;
        string[]? measure = null;
        var extra = new List<string>();

        var kinds = Argparse.ClassifyAll(args, Options);

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (kinds[i] is not { } kind)
                continue;
            if (!kind.IsOption)
            {
                if (map is null)
                    map = arg;
                else
                    extra.Add(arg);
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(arg); // an option that does not exist: reported at the end, after the required argument
                continue;
            }

            switch (kind.Option)
            {
                case "--help" or "--png" or "--open":
                    if (kind.Explicit is not null)
                    {
                        string names = kind.Option == "--help" ? "-h/--help" : kind.Option;
                        throw Usage($"argument {names}: ignored explicit argument {Py.Repr(kind.Explicit)}");
                    }
                    if (kind.Option == "--help")
                        return new Arguments(null, false, false, null, Help: true);
                    if (kind.Option == "--png")
                        png = true;
                    else
                        open = true;
                    break;
                case "--measure":
                    // A value written with "=" is one argument, and the option takes two: argparse refuses it.
                    var values = new List<string>();
                    while (kind.Explicit is null && values.Count < 2 && i + 1 < args.Length && kinds[i + 1] is { IsOption: false })
                        values.Add(args[++i]);
                    if (values.Count < 2)
                        throw Usage("argument --measure: expected 2 arguments");
                    measure = [.. values];
                    break;
            }
        }

        if (map is null)
            throw Usage("the following arguments are required: map");
        if (extra.Count > 0)
            throw Usage("unrecognized arguments: " + string.Join(' ', extra));
        return new Arguments(map, png, open, measure, Help: false);
    }

    static DmcException Usage(string message) => Argparse.Usage(message);
}
