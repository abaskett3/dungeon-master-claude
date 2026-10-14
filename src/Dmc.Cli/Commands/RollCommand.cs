using Dmc.Dice;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc roll 1d20+5 2d20kh1+5 ...</c>: replaces <c>scripts/roll.ps1</c> and <c>roll.sh</c>. Every argument is
/// a dice expression. One line is printed per expression, on stdout, errors included, as the scripts do. The
/// exit code is 1 if any expression was bad (the good ones are still rolled), and 1 with a usage line when
/// there are none.
/// </summary>
public sealed class RollCommand : ICommand
{
    public string Name => "roll";

    public string Summary => "roll dice: dmc roll 1d20+5 2d20kh1+5 4d6kh3";

    public int Run(string[] args, Io io)
    {
        using var random = new OsRandomBytes();
        return Run(args, io, random);
    }

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
