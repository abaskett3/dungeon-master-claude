using Dmc.Maps;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc map &lt;map.json&gt; [--png] [--open] [--measure FROM TO]</c>: replaces <c>scripts/map_render.py</c>. It checks a
/// map file, draws it as <c>live.html</c> and <c>live-data.js</c> next to it, and prints the distances between the
/// creatures. The format of a map file is in <c>.claude/skills/draw-map/map-format.md</c>.
/// </summary>
/// <remarks>
/// Arguments follow Python's <c>argparse</c> (an option may be shortened to a unique prefix), but a mistake is one
/// line and exit code 2, never the usage text.
/// </remarks>
public sealed class MapCommand(Action<string>? open = null) : ICommand
{
    readonly Action<string> _open = open ?? Browser.Open;

    public string Name => "map";

    public string Summary => "draw a map for the player's browser: dmc map <map.json> [--png] [--open] [--measure A B]";

    const string Help = """
usage: dmc map <map.json> [--png] [--open]
       dmc map <map.json> --measure FROM TO

Draws a place or scene file as live.html and live-data.js next to it, and prints the distance between every pair of
creatures. The format of the file is in .claude/skills/draw-map/map-format.md.

  --png      also write live.png, the same picture, for DMC to look at with Read
  --open     also open live.html in the default browser
  --measure  only print the distance between two cells, draw nothing
""";

    static readonly string[] Options = ["--help", "--png", "--open", "--measure"];

    sealed record Arguments(string Map, bool Png, bool Open, string[]? Measure);

    public int Run(string[] args, Io io)
    {
        if (WantsHelp(args))
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        var parsed = Parse(args);
        var model = MapChecker.Check(MapChecker.LoadMap(parsed.Map));

        if (parsed.Measure is not null)
        {
            var a = Cells.Parse(parsed.Measure[0], model.Cols, model.Rows, "--measure");
            var b = Cells.Parse(parsed.Measure[1], model.Cols, model.Rows, "--measure");
            io.Out.WriteLine(Distances.MeasureText(a, b, model.CellFt));
            return 0;
        }

        string svg = SvgDrawing.Render(model);
        string folder = Path.GetDirectoryName(Path.GetFullPath(parsed.Map)) ?? "";
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

    /// <summary>-h or any unambiguous start of --help, anywhere before a "--".</summary>
    static bool WantsHelp(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg == "--")
                return false;
            if (arg == "-h" || (arg.Length > 2 && arg.StartsWith("--", StringComparison.Ordinal) && "--help".StartsWith(arg, StringComparison.Ordinal)
                                && Options.Count(o => o.StartsWith(arg, StringComparison.Ordinal)) == 1))
                return true;
        }
        return false;
    }

    /// <summary>Read the arguments as <c>argparse</c> does, with its messages.</summary>
    static Arguments Parse(string[] args)
    {
        string? map = null;
        bool png = false, open = false;
        string[]? measure = null;
        var extra = new List<string>();
        bool onlyPositional = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (onlyPositional || !LooksLikeOption(arg))
            {
                if (map is null)
                    map = arg;
                else
                    extra.Add(arg);
                continue;
            }
            if (arg == "--")
            {
                onlyPositional = true;
                continue;
            }

            string name = arg, explicitValue = "";
            bool hasValue = false;
            int equals = arg.IndexOf('=');
            if (arg.StartsWith("--", StringComparison.Ordinal) && equals > 0)
            {
                name = arg[..equals];
                explicitValue = arg[(equals + 1)..];
                hasValue = true;
            }
            string? option = Resolve(name);
            if (option is null)
            {
                extra.Add(arg);
                continue;
            }
            switch (option)
            {
                case "--png" or "--open":
                    if (hasValue)
                        throw Usage($"argument {option}: ignored explicit argument '{explicitValue}'");
                    if (option == "--png")
                        png = true;
                    else
                        open = true;
                    break;
                case "--measure":
                    var values = new List<string>();
                    if (hasValue)
                        values.Add(explicitValue);
                    while (values.Count < 2 && i + 1 < args.Length && !LooksLikeOption(args[i + 1]))
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
        return new Arguments(map, png, open, measure);
    }

    /// <summary>Whether an argument is an option, as argparse decides it: it starts with "-", is not just "-" or a negative number, and has no space.</summary>
    static bool LooksLikeOption(string arg)
    {
        if (arg == "--")
            return true;
        if (arg.Length < 2 || arg[0] != '-' || arg.Contains(' '))
            return false;
        string rest = arg[1..];
        bool negativeNumber = rest.Any(char.IsAsciiDigit) && rest.All(c => char.IsAsciiDigit(c) || c == '.');
        return !negativeNumber;
    }

    /// <summary>An option may be written in full or as any prefix of it that no other option shares (like <c>--pn</c>).</summary>
    static string? Resolve(string name)
    {
        if (!name.StartsWith("--", StringComparison.Ordinal))
            return null;
        if (Options.Contains(name))
            return name;
        var matches = Options.Where(o => o.StartsWith(name, StringComparison.Ordinal)).ToList();
        if (matches.Count > 1)
            throw Usage($"ambiguous option: {name} could match {string.Join(", ", matches)}");
        return matches.Count == 1 ? matches[0] : null;
    }

    static DmcException Usage(string message) => new(message, exitCode: 2);
}
