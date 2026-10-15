using Dmc.Commands;

namespace Dmc;

/// <summary>The command table. A later phase adds <c>play</c> here.</summary>
public static class CommandTable
{
    public static IReadOnlyList<ICommand> All { get; } =
    [
        new RollCommand(),
        new ContextCheckCommand(),
        new MapCommand(),
        new PdfCommand(),
        new DigestCommand(),
    ];
}
