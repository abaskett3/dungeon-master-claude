using Dmc.Maps;

namespace Dmc.Digest;

/// <summary>What <c>dmc digest</c> was asked to do.</summary>
/// <param name="Command">list or show.</param>
/// <param name="Campaign">Only sessions that touched this campaign; null or empty for all.</param>
/// <param name="After">Only sessions that ended on or after this date (text, compared with the first 10 characters of the end time).</param>
/// <param name="Logs">The folder of session logs; null or empty for the Claude Code logs of the DMC folder.</param>
/// <param name="Out">The output folder of <c>show</c>; null or empty for <c>adventures/.transcript-review</c> in the DMC folder.</param>
/// <param name="Sessions">The starts of session ids <c>show</c> was given; none means every session.</param>
/// <param name="Folder">The DMC folder whose Claude Code logs are read and whose <c>adventures/</c> holds the default output folder; null or empty for the folder <see cref="DmcFolder.Find()"/> finds.</param>
internal sealed record DigestRequest(string Command, string? Campaign, string? After, string? Logs, string? Out,
    IReadOnlyList<string> Sessions, string? Folder = null);

/// <summary>
/// Reads the command line the way <c>transcript_digest.py</c>'s <c>argparse</c> did, with its messages (apart from the
/// usage text, which dmc leaves out: an error is one line and exit code 2). The subcommand names, and so the
/// invalid-choice message that lists them, are dmc's own now (the script's second subcommand was <c>digest</c>, and is
/// <c>show</c> here); everything else still follows the script. argparse has two levels here, and so has
/// this: the command is picked first, and everything after it belongs to the command's own parser. The order argparse
/// follows is kept: every argument is sorted as an option, a plain argument or the "--" marker before any is read; the
/// session ids are the plain arguments up to the first option after them (a later one is "unrecognized"); an option
/// takes the one plain argument after it, or the text after "="; a repeated option is overridden by its last use; and the
/// unrecognized arguments are reported last, after every other mistake.
/// </summary>
internal static class DigestArguments
{
    /// <summary>The subcommands' names. This is the only place they are spelled out in the program.</summary>
    public const string List = "list", Show = "show";

    /// <summary>The subcommands, in the order the invalid-choice message lists them.</summary>
    internal static readonly string[] Commands = [List, Show];

    /// <summary>The options <c>list</c> accepts.</summary>
    static readonly string[] ListOptions = ["--help", "--campaign", "--after", "--logs", "--folder"];

    /// <summary>The options <c>show</c> accepts: those of <c>list</c> and <c>--out</c>.</summary>
    static readonly string[] ShowOptions = [.. ListOptions, "--out"];

    /// <summary>Read the arguments after <c>dmc digest</c>. Null means "--help".</summary>
    /// <param name="args">The arguments after <c>digest</c>.</param>
    /// <exception cref="DmcException">A usage mistake, exit code 2: no or an invalid command, an option with no value, a value given to <c>--help</c>, or arguments no parser recognised.</exception>
    public static DigestRequest? Parse(string[] args)
    {
        // The parser above the commands knows only --help, so an option before the command is unknown, and a lone
        // plain argument is the command; all that follows it is the command's.
        var (kinds, classified) = Sort(args, ["--help"]);
        var extras = new List<string>();
        int at = 0;
        for (; at < args.Length; at++)
        {
            if (kinds[at] == 'O')
            {
                if (classified[at]!.Option == "--help")
                    return Help(classified[at]!);
                extras.Add(args[at]);
            }
            else
            {
                break;
            }
        }

        // "--" before the command is taken for the command itself, which is not one.
        if (at == args.Length || (kinds[at] == '-' && at + 1 == args.Length))
            throw Argparse.Usage("the following arguments are required: command");
        string command = args[at];
        if (!Commands.Contains(command))
            throw Argparse.Usage($"argument command: invalid choice: {Py.Repr(command)} (choose from {string.Join(", ", Commands)})");

        var request = ParseCommand(command, args[(at + 1)..], extras);
        if (request is null)
            return null;
        if (extras.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extras));
        return request;
    }

    /// <summary>What <c>--help</c> does: null, which means "print the help text". A value written after "=" is an error, as in argparse.</summary>
    /// <param name="option">The <c>--help</c> argument as classified.</param>
    static DigestRequest? Help(Argparse.Classified option) =>
        option.Explicit is null
            ? null
            : throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Py.Repr(option.Explicit)}");

    /// <summary>
    /// Sort every argument as <c>O</c> (an option, known or not), <c>A</c> (a plain argument) or <c>-</c> (the first
    /// "--", after which every argument is a plain one), as argparse's <c>arg_strings_pattern</c> does.
    /// </summary>
    /// <param name="args">The arguments to sort.</param>
    /// <param name="options">The options the parser knows.</param>
    /// <returns>The letter of each argument, and, for each one that was classified (not after a "--"), its classification.</returns>
    static (char[] Kinds, Argparse.Classified?[] Classified) Sort(string[] args, string[] options)
    {
        var kinds = new char[args.Length];
        var classified = new Argparse.Classified?[args.Length];
        bool onlyPlain = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (onlyPlain)
            {
                kinds[i] = 'A';
            }
            else if (args[i] == "--")
            {
                kinds[i] = '-';
                onlyPlain = true;
            }
            else
            {
                classified[i] = Argparse.Classify(args[i], options);
                kinds[i] = classified[i]!.IsOption ? 'O' : 'A';
            }
        }
        return (kinds, classified);
    }

    /// <summary>The command's own parser. Arguments it does not recognize are added to <paramref name="extras"/>.</summary>
    /// <param name="command"><c>list</c> or <c>show</c>. Only <c>show</c> takes session ids and <c>--out</c>.</param>
    /// <param name="args">The arguments after the command word.</param>
    /// <param name="extras">The unrecognized arguments so far; this adds its own, for the caller to report.</param>
    /// <returns>The request, or null for <c>--help</c>.</returns>
    static DigestRequest? ParseCommand(string command, string[] args, List<string> extras)
    {
        bool show = command == Show;
        var (kinds, classified) = Sort(args, show ? ShowOptions : ListOptions);
        string? campaign = null, after = null, logs = null, output = null, folder = null;
        var sessions = new List<string>();
        bool sessionsOpen = show; // the session ids are taken once

        // The plain arguments from here, up to the next option: the session ids, if still open.
        int TakePlain(int from)
        {
            if (!sessionsOpen)
                return from;
            sessionsOpen = false;
            int end = from;
            while (end < args.Length && kinds[end] is 'A' or '-')
                end++;
            var taken = args[from..end].ToList();
            taken.Remove("--"); // only the first "--" goes
            sessions.AddRange(taken);
            return end;
        }

        int lastOption = Array.FindLastIndex(kinds, k => k == 'O');
        int start = 0;
        while (start <= lastOption)
        {
            int next = Array.IndexOf(kinds, 'O', start);
            if (start != next)
            {
                int end = TakePlain(start);
                if (end > start)
                {
                    start = end;
                    continue;
                }
            }
            if (kinds[start] != 'O')
            {
                extras.AddRange(args[start..next]);
                start = next;
            }

            var option = classified[start]!;
            if (option.Option is null)
            {
                extras.Add(args[start]); // an option that does not exist
                start++;
                continue;
            }
            if (option.Option == "--help")
                return Help(option);

            string value;
            if (option.Explicit is not null)
            {
                value = option.Explicit;
                start++;
            }
            else
            {
                // The one plain argument after it. (A "--" in between does not do: argparse leaves it out of an option's pattern.)
                int j = start + 1;
                if (j >= args.Length || kinds[j] != 'A')
                    throw Argparse.Usage($"argument {option.Option}: expected one argument");
                value = args[j];
                start = j + 1;
            }
            switch (option.Option)
            {
                case "--campaign": campaign = value; break;
                case "--after": after = value; break;
                case "--logs": logs = value; break;
                case "--folder": folder = value; break;
                default: output = value; break;
            }
        }

        int rest = TakePlain(start);
        extras.AddRange(args[rest..]);
        return new DigestRequest(command, campaign, after, logs, output, sessions, folder);
    }
}
