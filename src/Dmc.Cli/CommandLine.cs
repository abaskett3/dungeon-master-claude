using System.Reflection;

namespace Dmc;

/// <summary>Finds the subcommand, runs it, and turns every failure into one line. Never prints a stack trace.</summary>
public static class CommandLine
{
    /// <summary>
    /// The version <c>dmc --version</c> prints: the assembly's informational version, which is <c>0.0.0-dev</c> for a
    /// build made here and the tag's number for a release (see <c>release.yml</c>). "unknown" if the assembly has none.
    /// </summary>
    public static string Version { get; } =
        typeof(CommandLine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    /// <summary>
    /// Runs one <c>dmc</c> invocation. <c>--version</c> and <c>-h</c>/<c>--help</c>/<c>help</c> are answered here; any
    /// other first argument must name a command. Unless the command is <see cref="ICommand.NeverFails"/>, the
    /// stale-binary warning is checked first. A <see cref="DmcException"/> prints as <c>dmc &lt;command&gt;: message</c>
    /// and exits with its own code; any other exception prints the same way with the exception's type name added, and
    /// exits 1. A never-fails command swallows every exception and exits 0 with nothing printed.
    /// </summary>
    /// <param name="args">The command line, without the program name.</param>
    /// <param name="io">The streams to read and write.</param>
    /// <param name="commands">The commands to choose from (<see cref="CommandTable.All"/> in the real program).</param>
    /// <returns>The process exit code: 0 on success, 1 for no or an unknown command, or whatever the command or failure chose.</returns>
    public static int Run(string[] args, Io io, IReadOnlyList<ICommand> commands)
    {
        if (args.Length == 0)
        {
            io.Err.WriteLine($"dmc: no command given (try {Names(commands)}, --version)");
            return 1;
        }

        string name = args[0];
        switch (name)
        {
            case "--version":
                io.Out.WriteLine($"dmc {Version}");
                return 0;
            case "-h" or "--help" or "help":
                io.Out.WriteLine("usage: dmc <command> [args]");
                foreach (var c in commands)
                    io.Out.WriteLine($"  {c.Name,-14}{c.Summary}");
                io.Out.WriteLine($"  {"--version",-14}print the version");
                return 0;
        }

        var command = commands.FirstOrDefault(c => c.Name == name);
        if (command is null)
        {
            io.Err.WriteLine($"dmc: unknown command \"{OneLine(name)}\" (try {Names(commands)}, --version)");
            return 1;
        }

        try
        {
            if (!command.NeverFails)
                StaleBinaryCheck.Run(io.Err);
            return command.Run(args[1..], io);
        }
        catch (Exception) when (command.NeverFails)
        {
            return 0;
        }
        catch (DmcException e)
        {
            io.Err.WriteLine($"dmc {name}: {OneLine(e.Message)}");
            return e.ExitCode;
        }
        catch (Exception e)
        {
            // An unexpected failure: still one line, naming the exception type, never the stack.
            io.Err.WriteLine($"dmc {name}: {OneLine(e.Message)} ({e.GetType().Name})");
            return 1;
        }
    }

    /// <summary>The command names as a comma-separated list, for the "no command" and "unknown command" messages.</summary>
    static string Names(IReadOnlyList<ICommand> commands) => string.Join(", ", commands.Select(c => c.Name));

    /// <summary>Collapse line breaks so a message is always one line.</summary>
    /// <remarks>
    /// A lone surrogate (which JSON can spell as an escape, and no encoding can write) is written as a backslash escape,
    /// as Python writes it to stderr.
    /// </remarks>
    static string OneLine(string text)
    {
        text = text.ReplaceLineEndings(" ").Trim();
        if (!text.Any(char.IsSurrogate))
            return text;
        var builder = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                builder.Append(text, i++, 2);
            else if (char.IsSurrogate(text[i]))
                builder.Append('\\').Append('u').Append(((int)text[i]).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
            else
                builder.Append(text[i]);
        }
        return builder.ToString();
    }
}
