using Dmc.Dice;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc roll [label:]1d20+5[@AC14] 2d20kh1+5 ...</c>: replaced <c>scripts/roll.ps1</c> and <c>roll.sh</c>. Every
/// argument is a dice expression, which may start with a label and end with a target (see
/// <see cref="DiceRoller.RollArgument"/>). One line is printed per expression, on stdout, errors included, as the
/// scripts do. The exit code is 1 if any expression was bad (the good ones are still rolled), and 1 with a usage
/// line when there are none. <c>--help</c> (or <c>-h</c>) prints <see cref="DiceRoller.Help"/> and rolls nothing.
/// </summary>
public sealed class RollCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "roll";

    /// <inheritdoc />
    public string Summary => "roll dice: dmc roll 1d20+5 4d6kh3, or with a label and target \"Pip, shot:2d20kl1+5@AC14\"";

    /// <summary>Rolls every argument in <paramref name="args"/> with the operating system's random bytes.</summary>
    /// <param name="args">Dice expressions such as <c>1d20+5</c>, each with an optional label and target, or <c>--help</c>.</param>
    /// <param name="io">Standard output gets one line per argument, or the help.</param>
    /// <returns>0 when every argument was rolled or help was asked for, 1 when any was bad or none was given.</returns>
    public int Run(string[] args, Io io)
    {
        using var random = new OsRandomBytes();
        return Run(args, io, random);
    }

    /// <summary>
    /// The command with its random source passed in, so a test can roll known bytes: the usage line when there are no
    /// arguments, the help when <c>--help</c> or <c>-h</c> is among them, otherwise one line per argument.
    /// </summary>
    /// <param name="args">Dice expressions, each with an optional label and target.</param>
    /// <param name="io">Standard output gets one line per argument, the help, or the usage line when there are none.</param>
    /// <param name="random">Where the dice come from.</param>
    /// <returns>0 when every argument was rolled or help was printed, otherwise 1.</returns>
    internal static int Run(string[] args, Io io, IRandomBytes random)
    {
        if (args.Length == 0)
        {
            io.Out.WriteLine(DiceRoller.Usage);
            return 1;
        }
        if (args.Any(a => a is "--help" or "-h"))
        {
            io.Out.WriteLine(DiceRoller.Help);
            return 0;
        }

        var roller = new DiceRoller(random);
        bool failed = false;
        foreach (string argument in args)
        {
            var outcome = roller.RollArgument(argument);
            io.Out.WriteLine(outcome.Line);
            failed |= !outcome.Rolled;
        }
        return failed ? 1 : 0;
    }
}
