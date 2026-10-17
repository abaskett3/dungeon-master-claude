using Dmc.Dice;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc roll 1d20+5 2d20kh1+5 ...</c>: replaced <c>scripts/roll.ps1</c> and <c>roll.sh</c>. Every argument is
/// a dice expression. One line is printed per expression, on stdout, errors included, as the scripts do. The
/// exit code is 1 if any expression was bad (the good ones are still rolled), and 1 with a usage line when
/// there are none.
/// </summary>
public sealed class RollCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "roll";

    /// <inheritdoc />
    public string Summary => "roll dice: dmc roll 1d20+5 2d20kh1+5 4d6kh3";

    /// <summary>Rolls every expression in <paramref name="args"/> with the operating system's random bytes.</summary>
    /// <param name="args">Dice expressions such as <c>1d20+5</c>.</param>
    /// <param name="io">Standard output gets one line per expression.</param>
    /// <returns>0 when every expression was rolled, 1 when any was bad or none was given.</returns>
    public int Run(string[] args, Io io)
    {
        using var random = new OsRandomBytes();
        return Run(args, io, random);
    }

    /// <summary>The command with its random source passed in, so a test can roll known bytes.</summary>
    /// <param name="args">Dice expressions.</param>
    /// <param name="io">Standard output gets one line per expression, or the usage line when there are none.</param>
    /// <param name="random">Where the dice come from.</param>
    /// <returns>0 when every expression was rolled, otherwise 1.</returns>
    internal static int Run(string[] args, Io io, IRandomBytes random)
    {
        if (args.Length == 0)
        {
            io.Out.WriteLine(DiceRoller.Usage);
            return 1;
        }

        var roller = new DiceRoller(random);
        bool failed = false;
        foreach (string expr in args)
        {
            var outcome = roller.Roll(expr);
            io.Out.WriteLine(outcome.Line);
            failed |= !outcome.Rolled;
        }
        return failed ? 1 : 0;
    }
}
