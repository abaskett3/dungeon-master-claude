namespace Dmc.Sheets;

/// <summary><c>dmc sheet [NAME ...] [--campaign NAME] [--party] [--history] [--open] [--check] [--folder PATH]</c>, read.</summary>
/// <param name="Names">The characters named, by sheet title or file name; empty for every player character (or the party with <paramref name="Party"/>).</param>
/// <param name="Campaign">The campaign folder in <c>adventures/</c>; null for the active campaign.</param>
/// <param name="Party">True for every character with a block under Party status.</param>
/// <param name="History">True to add a PC's History.</param>
/// <param name="Open">True to open the page in the browser after writing it.</param>
/// <param name="Check">True to only check the characters' files and print the findings, writing nothing.</param>
/// <param name="Folder">The DMC folder; null for the one <see cref="DmcFolder.Require"/> finds.</param>
internal sealed record SheetRequest(IReadOnlyList<string> Names, string? Campaign, bool Party, bool History, bool Open, bool Check, string? Folder);

/// <summary>
/// Reads the command line of <c>dmc sheet</c> the way <c>dmc size</c> reads its own (<see cref="Argparse"/>): an option
/// may be shortened to a unique prefix, <c>--campaign=NAME</c> is understood, and a mistake is one line and exit code 2.
/// </summary>
internal static class SheetArguments
{
    /// <summary>The options <c>dmc sheet</c> accepts.</summary>
    static readonly string[] Options = ["--help", "--campaign", "--party", "--history", "--open", "--check", "--folder"];

    /// <summary>The options that take no value.</summary>
    static readonly string[] Flags = ["--party", "--history", "--open", "--check"];

    /// <summary>Reads the arguments after <c>dmc sheet</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    /// <exception cref="DmcException">
    /// A usage mistake, exit code 2: an unknown option, a value given to a flag, <c>--campaign</c> or <c>--folder</c> with
    /// no value, names with <c>--party</c>, or <c>--open</c> with <c>--check</c>.
    /// </exception>
    public static SheetRequest? Parse(string[] args)
    {
        var kinds = Argparse.ClassifyAll(args, Options);
        var names = new List<string>();
        var set = new HashSet<string>(StringComparer.Ordinal);
        string? campaign = null, folder = null;
        var extra = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (kinds[i] is not { } kind)
                continue; // the "--" separator: every argument after it is a name
            if (!kind.IsOption)
            {
                names.Add(arg);
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(arg);
                continue;
            }
            if (kind.Option == "--help")
            {
                return kind.Explicit is null
                    ? null
                    : throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
            }
            if (Flags.Contains(kind.Option))
            {
                if (kind.Explicit is not null)
                    throw Argparse.Usage($"argument {kind.Option}: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
                set.Add(kind.Option);
                continue;
            }

            // --campaign and --folder take one value: written after "=", or the next argument if that is not an option.
            string value;
            if (kind.Explicit is not null)
                value = kind.Explicit;
            else if (i + 1 < args.Length && kinds[i + 1] is { IsOption: false })
                value = args[++i];
            else
                throw Argparse.Usage($"argument {kind.Option}: expected one argument");
            if (kind.Option == "--campaign")
                campaign = value;
            else
                folder = value;
        }

        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));
        if (set.Contains("--party") && names.Count > 0)
            throw Argparse.Usage("argument --party: not allowed with a character's name");
        if (set.Contains("--open") && set.Contains("--check"))
            throw Argparse.Usage("argument --open: not allowed with argument --check");
        if (names.Any(n => n.Trim().Length == 0))
            throw Argparse.Usage("a character's name is empty");
        return new SheetRequest(names, campaign, set.Contains("--party"), set.Contains("--history"), set.Contains("--open"),
            set.Contains("--check"), folder);
    }
}
