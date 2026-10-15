using System.Reflection;

namespace Dmc;

/// <summary>Finds the subcommand, runs it, and turns every failure into one line. Never prints a stack trace.</summary>
public static class CommandLine
{
    public static string Version { get; } =
        typeof(CommandLine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

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
