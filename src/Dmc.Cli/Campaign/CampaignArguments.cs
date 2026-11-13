namespace Dmc.Campaign;

/// <summary>What <c>dmc campaign</c> was asked to do. Null from <see cref="CampaignArguments.Parse"/> means <c>--help</c>.</summary>
internal abstract record CampaignRequest;

/// <summary><c>dmc campaign export NAME [--out PATH] [--force]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="Out">An existing folder, or a file path ending in <c>.zip</c>; null for <c>exports/</c> under the DMC folder.</param>
/// <param name="Force">Allow replacing an existing file named by <paramref name="Out"/>.</param>
internal sealed record ExportRequest(string Name, string? Out, bool Force) : CampaignRequest;

/// <summary><c>dmc campaign import FILE [--as NAME]</c>.</summary>
/// <param name="File">An export file.</param>
/// <param name="As">The folder name to import under; null for the campaign name recorded in the export.</param>
internal sealed record ImportRequest(string File, string? As) : CampaignRequest;

/// <summary><c>dmc campaign show FILE</c>.</summary>
/// <param name="File">An export file.</param>
internal sealed record ShowRequest(string File) : CampaignRequest;

/// <summary><c>dmc campaign standard [NAME] [--init]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>; null for the active campaign.</param>
/// <param name="Init">Record the kit's standard for a campaign that has no record.</param>
internal sealed record StandardRequest(string? Name, bool Init) : CampaignRequest;

/// <summary><c>dmc campaign backups NAME</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
internal sealed record BackupsRequest(string Name) : CampaignRequest;

/// <summary><c>dmc campaign backup NAME [--prune oldest|old]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="Prune">What older backups the player agreed to remove.</param>
internal sealed record BackupRequest(string Name, PruneMode Prune) : CampaignRequest;

/// <summary><c>dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="Options">The backup, the accounting file and the two flags.</param>
internal sealed record VerifyRequest(string Name, VerifyOptions Options) : CampaignRequest;

/// <summary><c>dmc campaign restore NAME [--from FILE] [--yes]</c>.</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="From">The backup to restore; null for the marked backup, else the latest.</param>
/// <param name="Yes">Restore; without it the plan is printed and nothing changes.</param>
internal sealed record RestoreRequest(string Name, string? From, bool Yes) : CampaignRequest;

/// <summary><c>dmc campaign close NAME [--yes]</c> (spec 010).</summary>
/// <param name="Name">A folder under <c>adventures/</c>.</param>
/// <param name="Yes">Close; without it the plan is printed and nothing changes.</param>
internal sealed record CloseRequest(string Name, bool Yes) : CampaignRequest;

/// <summary><c>dmc campaign links [NAME]</c> (spec 011): the campaigns linked to this one, their modules and the notices.</summary>
/// <param name="Name">A folder under <c>adventures/</c>; null for the active campaign.</param>
internal sealed record LinksRequest(string? Name) : CampaignRequest;

/// <summary>Which form of <c>dmc campaign sequel</c> was asked for.</summary>
internal enum SequelForm
{
    /// <summary><c>--start</c>: write the digest's index, spine and chain copies, and print the parts plan.</summary>
    Start,

    /// <summary><c>--list</c>: print the unaccounted units.</summary>
    List,

    /// <summary><c>--check</c>: the whole proof.</summary>
    Check,

    /// <summary><c>--sample</c>: the sample draw.</summary>
    Sample,
}

/// <summary><c>dmc campaign sequel NAME --start|--list [--all]|--check|--sample [--round N]</c> (spec 011 contracts/campaign-sequel.md).</summary>
/// <param name="Name">The sequel: a folder under <c>adventures/</c> whose Continues line names a closed campaign.</param>
/// <param name="Form">Which form.</param>
/// <param name="All"><c>--list</c>: print every unaccounted unit, not the first 40.</param>
/// <param name="Round"><c>--sample</c>: the round, 1 or more; 1 by default.</param>
internal sealed record SequelRequest(string Name, SequelForm Form, bool All, int Round) : CampaignRequest;

/// <summary>
/// Reads the command line of <c>dmc campaign</c> the way <c>dmc digest</c> and <c>dmc pdf</c> do (see <see cref="Argparse"/>): an
/// option may be shortened to a unique prefix, <c>--option=value</c> is understood, and a mistake is one line and exit code
/// 2, never the usage text.
/// </summary>
internal static class CampaignArguments
{
    /// <summary>The verbs. This is the only place they are spelled out in the program.</summary>
    public const string Export = "export", Import = "import", Show = "show", Standard = "standard", Backups = "backups",
        Backup = "backup", Verify = "verify", Restore = "restore", Close = "close", Links = "links", Sequel = "sequel";

    /// <summary>The verbs, in the order the usage lists them; <c>ToolkitTests</c> checks the prompts against this list.</summary>
    public static readonly string[] Commands = [Export, Import, Show, Standard, Backups, Backup, Verify, Restore, Close, Links, Sequel];

    /// <summary>The message for a missing or unknown verb (exit code 2).</summary>
    const string NeedVerb = "give export, import, show, standard, backups, backup, verify, restore, close, links or sequel (try dmc campaign --help)";

    /// <summary>The message for a <c>sequel</c> without exactly one form (exit code 2).</summary>
    internal const string NeedForm = "give one of --start, --list, --check or --sample";

    /// <summary>The message for an <c>--as</c> that is not a valid campaign name (exit code 2); <see cref="CampaignImporter"/> says the same when it is called with one.</summary>
    internal const string AsMessage = "--as must be lowercase letters, digits and hyphens, starting with a letter or digit";

    /// <summary>The options that take no value.</summary>
    static readonly string[] Flags = ["--force", "--init", "--list", "--all", "--yes", "--start", "--check", "--sample"];

    /// <summary>The options each verb accepts.</summary>
    static string[] OptionsOf(string verb) => verb switch
    {
        Export => ["--help", "--out", "--force"],
        Import => ["--help", "--as"],
        Standard => ["--help", "--init"],
        Backup => ["--help", "--prune"],
        Verify => ["--help", "--backup", "--accounting", "--list", "--all"],
        Restore => ["--help", "--from", "--yes"],
        Close => ["--help", "--yes"],
        Sequel => ["--help", "--start", "--list", "--all", "--check", "--sample", "--round"],
        _ => ["--help"],
    };

    /// <summary>What the one positional argument of a verb is called in the "required" message.</summary>
    static string TargetOf(string verb) => verb is Import or Show ? "file" : "name";

    /// <summary>Read the arguments after <c>dmc campaign</c>. Null means <c>--help</c>.</summary>
    /// <param name="args">The arguments after <c>campaign</c>.</param>
    /// <exception cref="DmcException">
    /// A usage mistake, exit code 2: no verb or an unknown one, a missing <c>NAME</c> or <c>FILE</c>, an option with no
    /// value or an unknown one, a value given to <c>--help</c> or to a flag, an <c>--as</c> that is not a valid name, or a
    /// <c>--prune</c> that is not <c>oldest</c> or <c>old</c>.
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
        string? target = null;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);
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
            if (Flags.Contains(kind.Option))
            {
                if (kind.Explicit is not null)
                    throw Argparse.Usage($"argument {kind.Option}: ignored explicit argument {Maps.Py.Repr(kind.Explicit)}");
                flags.Add(kind.Option);
                continue;
            }

            // Every other option takes one value: written after "=", or the next argument if that is not an option.
            string value;
            if (kind.Explicit is not null)
                value = kind.Explicit;
            else if (i + 1 < rest.Length && kinds[i + 1] is { IsOption: false })
                value = rest[++i];
            else
                throw Argparse.Usage($"argument {kind.Option}: expected one argument");

            if (kind.Option == "--as" && !IsCampaignName(value))
                throw Argparse.Usage(AsMessage);
            values[kind.Option] = value;
        }

        if (target is null && verb is not (Standard or Links))
            throw Argparse.Usage("the following arguments are required: " + TargetOf(verb));
        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));

        PruneMode prune = PruneMode.None;
        if (values.TryGetValue("--prune", out string? pruneValue))
            prune = BackupFolder.ParsePrune(pruneValue) ?? throw Argparse.Usage(
                $"argument --prune: invalid choice: {Maps.Py.Repr(pruneValue)} (choose from 'oldest', 'old')");

        string Value(string option) => values.GetValueOrDefault(option)!;
        if (verb == Sequel)
            return SequelOf(target!, flags, Value("--round"));
        return verb switch
        {
            Export => new ExportRequest(target!, Value("--out"), flags.Contains("--force")),
            Import => new ImportRequest(target!, Value("--as")),
            Show => new ShowRequest(target!),
            Standard => new StandardRequest(target, flags.Contains("--init")),
            Backups => new BackupsRequest(target!),
            Backup => new BackupRequest(target!, prune),
            Verify => new VerifyRequest(target!, new VerifyOptions(Value("--backup"), Value("--accounting"), flags.Contains("--list"), flags.Contains("--all"))),
            Restore => new RestoreRequest(target!, Value("--from"), flags.Contains("--yes")),
            Links => new LinksRequest(target),
            _ => new CloseRequest(target!, flags.Contains("--yes")),
        };
    }

    /// <summary>
    /// The <c>sequel</c> request: exactly one of <c>--start</c>, <c>--list</c>, <c>--check</c> and <c>--sample</c>; <c>--all</c> only
    /// with <c>--list</c>; <c>--round N</c>, N of 1 or more, only with <c>--sample</c>.
    /// </summary>
    /// <param name="name">The sequel's name.</param>
    /// <param name="flags">The flags given.</param>
    /// <param name="round">The value of <c>--round</c>, or null.</param>
    /// <exception cref="DmcException">A usage mistake, exit code 2.</exception>
    static SequelRequest SequelOf(string name, HashSet<string> flags, string? round)
    {
        var forms = new (string Flag, SequelForm Form)[]
        {
            ("--start", SequelForm.Start), ("--list", SequelForm.List), ("--check", SequelForm.Check), ("--sample", SequelForm.Sample),
        }.Where(f => flags.Contains(f.Flag)).ToList();
        if (forms.Count != 1)
            throw Argparse.Usage(NeedForm);
        var form = forms[0].Form;
        if (flags.Contains("--all") && form != SequelForm.List)
            throw Argparse.Usage("argument --all: only with --list");
        int number = 1;
        if (round is not null)
        {
            if (form != SequelForm.Sample)
                throw Argparse.Usage("argument --round: only with --sample");
            if (!int.TryParse(round, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out number) || number < 1)
                throw Argparse.Usage($"argument --round: must be a whole number of 1 or more, not {Maps.Py.Repr(round)}");
        }
        return new SequelRequest(name, form, flags.Contains("--all"), number);
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
