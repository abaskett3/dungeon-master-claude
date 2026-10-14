using Dmc.Commands;

namespace Dmc;

/// <summary>The command table. Later phases add <c>map</c>, <c>pdf</c>, <c>digest</c> and <c>play</c> here.</summary>
public static class CommandTable
{
    public static IReadOnlyList<ICommand> All { get; } =
    [
        new RollCommand(),
        new ContextCheckCommand(),
    ];
}
