using Dmc.Maps;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc map &lt;map.json&gt; [--png] [--open] [--measure FROM TO]</c>: replaces <c>scripts/map_render.py</c>. It checks a
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

    public int Run(string[] args, Io io)
    {
        var parsed = Parse(args);
        if (parsed.Help)
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        string mapPath = parsed.Map!;
        var model = MapChecker.Check(MapChecker.LoadMap(mapPath));

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

    /// <summary>How argparse sees one argument: a plain word, or an option (known or not) with a value written after "=".</summary>
    /// <param name="Option">The option it is, or null for an option that does not exist.</param>
    /// <param name="Explicit">The text after "=", or null if there was none.</param>
    sealed record Classified(bool IsOption, string? Option = null, string? Explicit = null);

    /// <summary>
    /// <c>ArgumentParser._parse_optional</c>: an exact option, then <c>--option=value</c>, then a unique prefix of one,
    /// then a negative number or a word with a space (a plain argument), and anything else that starts with "-" is an
    /// unknown option.
    /// </summary>
    static Classified Classify(string arg)
    {
        if (arg.Length == 0 || arg[0] != '-')
            return new Classified(false);
        if (arg == "-h" || Options.Contains(arg))
            return new Classified(true, arg == "-h" ? "--help" : arg);
        if (arg.Length == 1)
            return new Classified(false);

        int equals = arg.IndexOf('=');
        if (equals > 0)
        {
            string name = arg[..equals];
            if (name == "-h" || Options.Contains(name))
                return new Classified(true, name == "-h" ? "--help" : name, arg[(equals + 1)..]);
        }

        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            string prefix = equals > 0 ? arg[..equals] : arg;
            var matches = Options.Where(o => o.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            if (matches.Count > 1)
                throw Usage($"ambiguous option: {arg} could match {string.Join(", ", matches)}");
            if (matches.Count == 1)
                return new Classified(true, matches[0], equals > 0 ? arg[(equals + 1)..] : null);
        }

        if (IsNegativeNumber(arg) || arg.Contains(' '))
            return new Classified(false);
        return new Classified(true);
    }

    /// <summary>argparse's <c>^-\d+$|^-\d*\.\d+$</c> (a parser with no option that looks like a number reads these as plain arguments).</summary>
    static bool IsNegativeNumber(string arg)
    {
        if (arg.Length < 2 || arg[0] != '-')
            return false;
        string rest = arg[1..];
        if (rest.All(char.IsDigit))
            return true;
        int dot = rest.IndexOf('.');
        return dot >= 0 && rest[..dot].All(char.IsDigit) && rest.Length > dot + 1 && rest[(dot + 1)..].All(char.IsDigit);
    }

    /// <summary>Read the arguments as <c>argparse</c> does, in order, with its messages.</summary>
    static Arguments Parse(string[] args)
    {
        string? map = null;
        bool png = false, open = false;
        string[]? measure = null;
        var extra = new List<string>();

        // argparse looks at every argument before it acts on any, so an ambiguous option is an error even after a "--help".
        // After a "--" every argument is a plain one (and the "--" itself goes).
        var kinds = new Classified?[args.Length];
        bool onlyPositional = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (!onlyPositional && args[i] == "--")
                onlyPositional = true; // kinds[i] stays null: it is the separator
            else
                kinds[i] = onlyPositional ? new Classified(false) : Classify(args[i]);
        }

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

    static DmcException Usage(string message) => new(message, exitCode: 2);
}
