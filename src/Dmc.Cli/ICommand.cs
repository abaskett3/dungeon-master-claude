namespace Dmc;

/// <summary>
/// One <c>dmc</c> subcommand. A command is one class (in <c>Commands/</c>) that parses its own arguments and
/// delegates the real work to library code that tests can call in-process. Add it to <see cref="CommandTable.All"/>.
/// </summary>
public interface ICommand
{
    /// <summary>The word typed after <c>dmc</c>.</summary>
    string Name { get; }

    /// <summary>One line for <c>dmc --help</c>.</summary>
    string Summary { get; }

    /// <summary>
    /// True for a command that runs in a hook and must never get in the way: on any failure it prints nothing
    /// and exits 0, and it skips the stale-binary check.
    /// </summary>
    bool NeverFails => false;

    /// <summary>Run the command. Throw <see cref="DmcException"/> for a failure the user can fix.</summary>
    /// <returns>The process exit code.</returns>
    int Run(string[] args, Io io);
}
