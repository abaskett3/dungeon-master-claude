namespace Dmc.Commands;

/// <summary>
/// <c>dmc folder</c>: prints the DMC folder <see cref="DmcFolder.Find()"/> resolves (<c>CLAUDE_PROJECT_DIR</c> if it is a DMC
/// folder, else the current folder if it is one, else the first recorded folder that still is), one line, exit 0; or fails with
/// <c>no DMC folder: run dmc install</c>. The developer skills <c>review-feedback</c> and <c>review-transcript</c> use it
/// to find the folder to read.
/// </summary>
public sealed class FolderCommand : ICommand
{
    /// <summary>The machine it runs on; the real one unless a test passed another.</summary>
    readonly Machine? _machine;

    /// <summary>Makes the command; <see cref="CommandTable"/> uses the real machine.</summary>
    public FolderCommand() { }

    /// <summary>Makes the command over a machine a test built.</summary>
    /// <param name="machine">The machine to use.</param>
    internal FolderCommand(Machine machine) => _machine = machine;

    /// <inheritdoc />
    public string Name => "folder";

    /// <inheritdoc />
    public string Summary => "print the DMC folder this command would use";

    /// <summary>Prints the folder.</summary>
    /// <param name="args">None are accepted.</param>
    /// <param name="io">Standard output gets the folder's path.</param>
    /// <returns>0.</returns>
    /// <exception cref="DmcException">There is no DMC folder (<c>no DMC folder: run dmc install</c>), or an argument was given (exit code 2).</exception>
    public int Run(string[] args, Io io)
    {
        if (args.Length > 0)
            throw Argparse.Usage($"unrecognized arguments: {string.Join(' ', args)}");
        var machine = _machine;
        string? folder = machine is null
            ? DmcFolder.Find()
            : DmcFolder.Find(machine.Environment, machine.CurrentFolder, machine.ProgramFolder);
        io.Out.WriteLine(folder ?? throw new DmcException(DmcFolder.NoFolder));
        return 0;
    }
}
