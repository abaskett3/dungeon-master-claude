namespace Dmc.Campaign;

/// <summary>What <c>dmc campaign</c> was asked to do. Null from <see cref="CampaignArguments.Parse"/> means <c>--help</c>.</summary>
internal abstract record CampaignRequest;

/// <summary><c>dmc campaign export NAME [--out PATH] [--force]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="Out">An existing folder, or a file path ending in <c>.zip</c>; null for <c>exports/</c> under the toolkit root.</param>
/// <param name="Force">Allow replacing an existing file named by <paramref name="Out"/>.</param>
internal sealed record ExportRequest(string Name, string? Out, bool Force) : CampaignRequest;

/// <summary><c>dmc campaign import FILE [--as NAME]</c>.</summary>
/// <param name="File">An export file.</param>
/// <param name="As">The folder name to import under; null for the campaign name recorded in the export.</param>
internal sealed record ImportRequest(string File, string? As) : CampaignRequest;

/// <summary><c>dmc campaign show FILE</c>.</summary>
/// <param name="File">An export file.</param>
internal sealed record ShowRequest(string File) : CampaignRequest;

/// <summary>
/// Reads the command line of <c>dmc campaign</c> the way <c>dmc digest</c> and <c>dmc pdf</c> do (see <see cref="Argparse"/>): an
/// option may be shortened to a unique prefix, <c>--option=value</c> is understood, and a mistake is one line and exit code
/// 2, never the usage text.
/// </summary>
internal static class CampaignArguments
{
    /// <summary>The verbs. This is the only place they are spelled out in the program.</summary>
    public const string Export = "export", Import = "import", Show = "show";

    /// <summary>The verbs, in the order the usage lists them; <c>ToolkitTests</c> checks the prompts against this list.</summary>
    public static readonly string[] Commands = [Export, Import, Show];

    /// <summary>The message for a missing or unknown verb (exit code 2).</summary>
    const string NeedVerb = "give export, import or show (try dmc campaign --help)";

    /// <summary>The message for an <c>--as</c> that is not a valid campaign name (exit code 2); <see cref="CampaignImporter"/> says the same when it is called with one.</summary>
    internal const string AsMessage = "--as must be lowercase letters, digits and hyphens, starting with a letter or digit";

    /// <summary>The options each verb accepts.</summary>
    static string[] OptionsOf(string verb) => verb switch
    {
        Export => ["--help", "--out", "--force"],
        Import => ["--help", "--as"],
        _ => ["--help"],
    };

    /// <summary>Read the arguments after <c>dmc campaign</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>campaign</c>.</param>
    /// <exception cref="DmcException">
    /// A usage mistake, exit code 2: no verb or an unknown one, a missing <c>NAME</c> or <c>FILE</c>, an option with no
    /// value or an unknown one, a value given to <c>--help</c> or <c>--force</c>, or an <c>--as</c> that is not a valid name.
    /// </exception>
    public static CampaignRequest? Parse(string[] args)
    {
        // Before the verb the parser knows only --help; anything else that starts with "-" is not a verb.
        if (args.Length == 0)
            throw Argparse.Usage(NeedVerb);
        var first = Argparse.Classify(args[0], ["--help"]);
        if (first.IsOption)
            return first.Option == "--help" ? Help(first) : throw Argparse.Usage(NeedVerb);
        string verb = args[0];
        if (!Commands.Contains(verb))
            throw Argparse.Usage(NeedVerb);

        string[] options = OptionsOf(verb);
        string[] rest = args[1..];
        var kinds = Argparse.ClassifyAll(rest, options);
        string? target = null, output = null, rename = null;
        bool force = false;
        var extra = new List<string>();

        for (int i = 0; i < rest.Length; i++)
        {
            string arg = rest[i];
            if (kinds[i] is not { } kind)
                continue; // the "--" separator
            if (!kind.IsOption)
            {
                if (target is null)
                    target = arg;
                else
                    extra.Add(arg);
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(arg); // an option that does not exist: reported last, after the required arguments
                continue;
            }
            if (kind.Option == "--help")
                return Help(kind);
            if (kind.Option == "--force")
            {
                if (kind.Explicit is not null)
                    throw Argparse.Usage($"argument --force: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
                force = true;
                continue;
            }

            // --out and --as take one value: written after "=", or the next argument if that is not an option.
            string value;
            if (kind.Explicit is not null)
                value = kind.Explicit;
            else if (i + 1 < rest.Length && kinds[i + 1] is { IsOption: false })
                value = rest[++i];
            else
                throw Argparse.Usage($"argument {kind.Option}: expected one argument");

            if (kind.Option == "--out")
            {
                output = value;
            }
            else
            {
                if (!IsCampaignName(value))
                    throw Argparse.Usage(AsMessage);
                rename = value;
            }
        }

        if (target is null)
            throw Argparse.Usage("the following arguments are required: " + (verb == Export ? "name" : "file"));
        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));
        return verb switch
        {
            Export => new ExportRequest(target, output, force),
            Import => new ImportRequest(target, rename),
            _ => new ShowRequest(target),
        };
    }

    /// <summary>What <c>--help</c> does: null, which means "print the help text". A value written after "=" is an error, as in argparse.</summary>
    /// <param name="option">The <c>--help</c> argument as classified.</param>
    static CampaignRequest? Help(Argparse.Classified option) =>
        option.Explicit is null
            ? null
            : throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Maps.Py.Repr(option.Explicit)}");

    /// <summary>True for a name of the form <c>/new-campaign</c> makes: lowercase letters, digits and hyphens, starting with a letter or digit.</summary>
    /// <param name="name">The text after <c>--as</c>.</param>
    internal static bool IsCampaignName(string name) =>
        name.Length > 0
        && name[0] != '-'
        && name.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
