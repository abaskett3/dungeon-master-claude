namespace Dmc.Size;

/// <summary><c>dmc size [NAME] [--check] [--folder PATH]</c>.</summary>
/// <param name="Name">A campaign folder in <c>adventures/</c>; null for the active campaign.</param>
/// <param name="Check">True for the play form, which prints only what to act on.</param>
/// <param name="Folder">The DMC folder; null for the one <see cref="DmcFolder.Require"/> finds.</param>
internal sealed record SizeRequest(string? Name, bool Check, string? Folder);

/// <summary>
/// Reads the command line of <c>dmc size</c> the way <c>dmc campaign</c> reads its own (see <see cref="Argparse"/>): an
/// option may be shortened to a unique prefix, <c>--folder=PATH</c> is understood, and a mistake is one line and exit
/// code 2, never the usage text.
/// </summary>
internal static class SizeArguments
{
    /// <summary>The options <c>dmc size</c> accepts.</summary>
    static readonly string[] Options = ["--help", "--check", "--folder"];

    /// <summary>Reads the arguments after <c>dmc size</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>size</c>.</param>
    /// <exception cref="DmcException">
    /// A usage mistake, exit code 2: an unknown option, a second name, <c>--folder</c> with no value, or a value given to
    /// <c>--help</c> or <c>--check</c>.
    /// </exception>
    public static SizeRequest? Parse(string[] args)
    {
        var kinds = Argparse.ClassifyAll(args, Options);
        string? name = null, folder = null;
        bool check = false;
        var extra = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (kinds[i] is not { } kind)
                continue; // the "--" separator
            if (!kind.IsOption)
            {
                if (name is null)
                    name = arg;
                else
                    extra.Add(arg);
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(arg); // an option that does not exist: reported last, as argparse does
                continue;
            }
            if (kind.Option == "--help")
            {
                return kind.Explicit is null
                    ? null
                    : throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
            }
            if (kind.Option == "--check")
            {
                if (kind.Explicit is not null)
                    throw Argparse.Usage($"argument --check: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
                check = true;
                continue;
            }

            // --folder takes one value: written after "=", or the next argument if that is not an option.
            if (kind.Explicit is not null)
                folder = kind.Explicit;
            else if (i + 1 < args.Length && kinds[i + 1] is { IsOption: false })
                folder = args[++i];
            else
                throw Argparse.Usage("argument --folder: expected one argument");
        }

        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));
        return new SizeRequest(name, check, folder);
    }
}
