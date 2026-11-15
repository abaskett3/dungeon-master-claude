namespace Dmc.Modules;

/// <summary><c>dmc module check NAME [--folder PATH]</c>.</summary>
/// <param name="Name">A module folder in <c>modules/</c>, as typed.</param>
/// <param name="Folder">The DMC folder; null for the one <see cref="DmcFolder.Require"/> finds.</param>
internal sealed record ModuleRequest(string Name, string? Folder);

/// <summary>
/// Reads the command line of <c>dmc module</c> the way <c>dmc size</c> reads its own (see <see cref="Argparse"/>): an
/// option may be shortened to a unique prefix, <c>--folder=PATH</c> is understood, and a mistake is one line and exit
/// code 2, never the usage text.
/// </summary>
internal static class ModuleArguments
{
    /// <summary>The options <c>dmc module</c> accepts.</summary>
    static readonly string[] Options = ["--help", "--folder"];

    /// <summary>The verbs <c>dmc module</c> accepts.</summary>
    static readonly string[] Verbs = ["check"];

    /// <summary>Reads the arguments after <c>dmc module</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>module</c>.</param>
    /// <exception cref="DmcException">
    /// A usage mistake, exit code 2: no verb or no name, an unknown verb or option, a second name, <c>--folder</c> with
    /// no value, or a value given to <c>--help</c>.
    /// </exception>
    public static ModuleRequest? Parse(string[] args)
    {
        var kinds = Argparse.ClassifyAll(args, Options);
        var plain = new List<string>();
        string? folder = null;
        var extra = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (kinds[i] is not { } kind)
                continue; // the "--" separator
            if (!kind.IsOption)
            {
                plain.Add(arg);
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

            // --folder takes one value: written after "=", or the next argument if that is not an option.
            if (kind.Explicit is not null)
                folder = kind.Explicit;
            else if (i + 1 < args.Length && kinds[i + 1] is { IsOption: false })
                folder = args[++i];
            else
                throw Argparse.Usage("argument --folder: expected one argument");
        }

        if (plain.Count == 0)
            throw Argparse.Usage("the following arguments are required: VERB, NAME");
        if (!Verbs.Contains(plain[0], StringComparer.Ordinal))
            throw Argparse.Usage($"argument VERB: invalid choice: {Maps.Py.Repr(plain[0])} (choose from 'check')");
        if (plain.Count == 1)
            throw Argparse.Usage("the following arguments are required: NAME");
        extra.InsertRange(0, plain.Skip(2));
        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));
        return new ModuleRequest(plain[1], folder);
    }
}
