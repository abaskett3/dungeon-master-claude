namespace Dmc;

/// <summary>
/// How Python's <c>argparse</c> reads a command line, for the commands that replace a script that used it: an option
/// may be shortened to a unique prefix, <c>--option=value</c> is understood, and a negative number is a plain
/// argument. A mistake is one line and exit code 2, never the usage text.
/// </summary>
public static class Argparse
{
    /// <summary>An argparse error: the message of <c>parser.error</c> and its exit code, 2.</summary>
    public static DmcException Usage(string message) => new(message, exitCode: 2);

    /// <summary>How argparse sees one argument: a plain word, or an option (known or not) with a value written after "=".</summary>
    /// <param name="Option">The option it is, or null for an option that does not exist.</param>
    /// <param name="Explicit">The text after "=", or null if there was none.</param>
    public sealed record Classified(bool IsOption, string? Option = null, string? Explicit = null);

    /// <summary>
    /// <c>ArgumentParser._parse_optional</c>: an exact option, then <c>--option=value</c>, then a unique prefix of one,
    /// then a negative number or a word with a space (a plain argument), and anything else that starts with "-" is an
    /// unknown option.
    /// </summary>
    public static Classified Classify(string arg, string[] options)
    {
        if (arg.Length == 0 || arg[0] != '-')
            return new Classified(false);
        if (arg == "-h" || options.Contains(arg))
            return new Classified(true, arg == "-h" ? "--help" : arg);
        if (arg.Length == 1)
            return new Classified(false);

        int equals = arg.IndexOf('=');
        if (equals > 0)
        {
            string name = arg[..equals];
            if (name == "-h" || options.Contains(name))
                return new Classified(true, name == "-h" ? "--help" : name, arg[(equals + 1)..]);
        }

        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            string prefix = equals > 0 ? arg[..equals] : arg;
            var matches = options.Where(o => o.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            if (matches.Count > 1)
                throw Usage($"ambiguous option: {arg} could match {string.Join(", ", matches)}");
            if (matches.Count == 1)
                return new Classified(true, matches[0], equals > 0 ? arg[(equals + 1)..] : null);
        }

        if (IsNegativeNumber(arg) || arg.Contains(' '))
            return new Classified(false);
        return new Classified(true);
    }

    /// <summary>
    /// Classify every argument before any is acted on, as argparse does: an ambiguous option is an error even after a
    /// "--help". After a "--" every argument is a plain one, and the "--" itself goes (its entry stays null).
    /// </summary>
    public static Classified?[] ClassifyAll(string[] args, string[] options)
    {
        var kinds = new Classified?[args.Length];
        bool onlyPositional = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (!onlyPositional && args[i] == "--")
                onlyPositional = true; // kinds[i] stays null: it is the separator
            else
                kinds[i] = onlyPositional ? new Classified(false) : Classify(args[i], options);
        }
        return kinds;
    }

    /// <summary>argparse's <c>^-\d+$|^-\d*\.\d+$</c> (a parser with no option that looks like a number reads these as plain arguments).</summary>
    public static bool IsNegativeNumber(string arg)
    {
        if (arg.Length < 2 || arg[0] != '-')
            return false;
        string rest = arg[1..];
        if (rest.All(char.IsDigit))
            return true;
        int dot = rest.IndexOf('.');
        return dot >= 0 && rest[..dot].All(char.IsDigit) && rest.Length > dot + 1 && rest[(dot + 1)..].All(char.IsDigit);
    }
}
