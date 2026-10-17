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
    /// <summary>The word typed after <c>dmc</c>.</summary>
    public string Name => "roll";

    /// <summary>One line for <c>dmc --help</c>.</summary>
    public string Summary => "roll dice: dmc roll 1d20+5 4d6kh3, or with a label and target \"Pip, shot:2d20kl1+5@AC14\"";

    /// <summary>Roll with the operating system's random bytes.</summary>
    public int Run(string[] args, Io io)
    {
        using var random = new OsRandomBytes();
        return Run(args, io, random);
    }

    /// <summary>
    /// Print the usage line (no arguments, exit 1), the help (<c>--help</c> or <c>-h</c> anywhere, exit 0), or roll each
    /// argument with <paramref name="random"/>, one line each, exit 1 if any was bad. Tests pass a scripted source.
    /// </summary>
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
