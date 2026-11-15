namespace Dmc.Retrieval;

/// <summary><c>dmc lookup SOURCE HEADING... [--folder PATH]</c>.</summary>
/// <param name="Source">A campaign or module folder name, or <c>rules</c>, optionally with <c>/&lt;path&gt;</c>.</param>
/// <param name="Heading">The heading's words, joined by single spaces.</param>
/// <param name="Folder">The DMC folder; null for the one <see cref="DmcFolder.Require"/> finds.</param>
internal sealed record LookupRequest(string Source, string Heading, string? Folder);

/// <summary><c>dmc recall [NAME] WORDS... [--folder PATH]</c>, or <c>dmc recall [NAME] --scene</c>. Whether the first word names the campaign is decided later (research R10).</summary>
/// <param name="Words">Every positional argument, in order: with <paramref name="Scene"/>, at most one, the campaign.</param>
/// <param name="Folder">The DMC folder; null for the one <see cref="DmcFolder.Require"/> finds.</param>
/// <param name="Scene">True for <c>--scene</c>: canon's lines for the scene in <c>game-state.md</c> (spec 013 research R3).</param>
internal sealed record RecallRequest(IReadOnlyList<string> Words, string? Folder, bool Scene = false);

/// <summary>
/// Reads the command lines of <c>dmc lookup</c> and <c>dmc recall</c> the way <c>dmc size</c> reads its own (see
/// <see cref="Argparse"/>): an option may be shortened to a unique prefix, <c>--folder=PATH</c> is understood, and a
/// mistake is one line and exit code 2, never the usage text.
/// </summary>
internal static class RetrievalArguments
{
    /// <summary>The options both commands accept.</summary>
    static readonly string[] Options = ["--help", "--folder"];

    /// <summary>The options of <c>dmc recall</c>: both commands' and <c>--scene</c>.</summary>
    static readonly string[] RecallOptions = ["--help", "--folder", "--scene"];

    /// <summary>Reads the arguments after <c>dmc lookup</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>lookup</c>.</param>
    /// <exception cref="DmcException">A usage mistake, exit code 2: no source, no heading, an unknown option, or <c>--folder</c> with no value.</exception>
    public static LookupRequest? ParseLookup(string[] args)
    {
        if (Parse(args, Options) is not { } parsed)
            return null;
        var (positionals, folder, _) = parsed;
        if (positionals.Count == 0)
            throw Argparse.Usage("the following arguments are required: SOURCE, HEADING");
        if (positionals.Count == 1)
            throw Argparse.Usage("the following arguments are required: HEADING");
        return new LookupRequest(positionals[0], string.Join(' ', positionals.Skip(1)), folder);
    }

    /// <summary>Reads the arguments after <c>dmc recall</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>recall</c>.</param>
    /// <exception cref="DmcException">A usage mistake, exit code 2: no words, words with <c>--scene</c>, an unknown option, or <c>--folder</c> with no value.</exception>
    public static RecallRequest? ParseRecall(string[] args)
    {
        if (Parse(args, RecallOptions) is not { } parsed)
            return null;
        var (positionals, folder, scene) = parsed;
        if (scene)
        {
            return positionals.Count <= 1
                ? new RecallRequest(positionals, folder, Scene: true)
                : throw Argparse.Usage("--scene takes no words");
        }
        if (positionals.Count == 0)
            throw Argparse.Usage("the following arguments are required: WORDS");
        return new RecallRequest(positionals, folder);
    }

    /// <summary>The positional arguments, the <c>--folder</c> value and whether <c>--scene</c> was given; null for <c>--help</c>.</summary>
    /// <param name="args">The arguments after the command's name.</param>
    /// <param name="options">The options the command accepts.</param>
    /// <exception cref="DmcException">A usage mistake, exit code 2.</exception>
    static (List<string> Positionals, string? Folder, bool Scene)? Parse(string[] args, string[] options)
    {
        var kinds = Argparse.ClassifyAll(args, options);
        var positionals = new List<string>();
        var extra = new List<string>();
        string? folder = null;
        bool scene = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (kinds[i] is not { } kind)
                continue; // the "--" separator
            if (!kind.IsOption)
            {
                positionals.Add(args[i]);
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(args[i]); // an option that does not exist: reported last, as argparse does
                continue;
            }
            if (kind.Option == "--help")
            {
                return kind.Explicit is null
                    ? null
                    : throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
            }
            if (kind.Option == "--scene")
            {
                scene = kind.Explicit is null
                    ? true
                    : throw Argparse.Usage($"argument --scene: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
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
        return (positionals, folder, scene);
    }
}
