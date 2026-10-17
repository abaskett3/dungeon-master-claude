using Dmc.Commands;

namespace Dmc;

/// <summary>The command table.</summary>
public static class CommandTable
{
    /// <summary>
    /// Every command <c>dmc</c> knows, in the order <c>dmc --help</c> lists them. <see cref="CommandLine.Run"/> finds a
    /// command here by its <see cref="ICommand.Name"/>; a new command is added to this list and nowhere else.
    /// </summary>
    public static IReadOnlyList<ICommand> All { get; } =
    [
        new RollCommand(),
        new ContextCheckCommand(),
        new MapCommand(),
        new PdfCommand(),
        new DigestCommand(),
        new PlayCommand(),
    ];
}
