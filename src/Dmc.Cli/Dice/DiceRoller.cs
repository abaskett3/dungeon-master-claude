using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Dice;

/// <summary>What became of one expression: the line to print, and whether it was rolled.</summary>
/// <param name="Line">The line to print, one line whatever was typed.</param>
/// <param name="Rolled">False for an error line; no dice were drawn.</param>
/// <param name="Total">The total of the roll (not meaningful for an error line).</param>
/// <param name="Natural">
/// The face of the one d20 that counts, when exactly one does: a single d20, or the kept die of a d20 roll with
/// advantage or disadvantage. Null for any other roll. A natural 20 or 1 on an attack decides it (see
/// <see cref="DiceRoller.RollArgument"/>).
/// </param>
public readonly record struct RollOutcome(string Line, bool Rolled, long Total = 0, int? Natural = null);

/// <summary>
/// Rolls dice expressions exactly as <c>scripts/roll.ps1</c> and <c>roll.sh</c> do: <c>NdS</c>, <c>khK</c>/<c>klK</c>
/// and a flat <c>+M</c>/<c>-M</c>, with the same limits, the same output line and the same error lines. An argument
/// may also carry a label and a target, <c>label:dice@AC14</c> (see <see cref="RollArgument"/>).
/// </summary>
public sealed partial class DiceRoller(IRandomBytes random)
{
    /// <summary>The line printed when <c>dmc roll</c> is given nothing to roll.</summary>
    public const string Usage = "usage: dmc roll [label:]<dice>[@AC<n>|@DC<n>] ...   e.g. 1d20+5 2d20kh1+5 \"Pip, shot:1d20+5@AC14\"";

    /// <summary>What <c>dmc roll --help</c> prints, with "\n" line endings whatever the checkout's.</summary>
    public static readonly string Help = """
usage: dmc roll [label:]<dice>[@AC<n>|@DC<n>] [...]

Rolls each argument and prints one line for it. The dice are NdS, with khK (keep the highest K) or klK (keep the
lowest K) and a flat +M or -M: 1d20+5, 2d20kh1+5 (advantage), 2d20kl1+5 (disadvantage), 4d6kh3.

An argument may start with a label and end with a target. The label is any text without a colon, spaces and commas
included (quote the whole argument). The target is @AC and a number for an attack, or @DC and a number for a check or
a save. The line then ends with the verdict, and a total equal to the target succeeds:

  dmc roll "Pip, shot:2d20kl1+5@AC14"    Pip, shot: 2d20kl1+5: [17, 9] keep [9] +5 = 14 vs AC 14, hit
  dmc roll "Kole, Dex save:1d20+3@DC13"  Kole, Dex save: 1d20+3: [9] +3 = 12 vs DC 13, fail
  dmc roll 1d20+5                        1d20+5: [12] +5 = 17

AC says hit or miss; DC says success or fail. Against an AC, a natural 20 on the d20 that counts (a single d20, or the
kept one under advantage or disadvantage) hits and a natural 1 misses whatever the total, and the line says so:
"hit (natural 20)". A check or a save has no automatic success or failure. Without a label or a target the line is
exactly as it always was.
""".Replace("\r\n", "\n");

    // The target, after the last "@": AC or DC in any case, and a number.
    [GeneratedRegex(@"^(ac|dc)([0-9]+)\z", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Target();

    // [0-9] and \z rather than \d and $: ASCII digits only, and no match before a trailing newline (as in roll.sh).
    [GeneratedRegex(@"^([0-9]*)d([0-9]+)(?:(kh|kl)([0-9]+))?([+-][0-9]+)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex Expression();

    /// <summary>
    /// Roll one plain dice expression, with no label and no target: the line <c>1d20+5: [12] +5 = 17</c>, or an error
    /// line. The outcome also carries the total and the natural d20, which <see cref="RollArgument"/> needs.
    /// </summary>
    public RollOutcome Roll(string expr)
    {
        long mod = 0;
        var m = Expression().Match(expr.ToLowerInvariant());
        if (!m.Success)
            return Failed($"{Printable(expr)}: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)");

        // A count or die size too long for an int is out of range, as it is in the scripts.
        if (!TryInt(m.Groups[1].Value, empty: 1, out int count) || !TryInt(m.Groups[2].Value, empty: 0, out int sides)
            || count < 1 || count > 100 || sides < 2 || sides > 1000)
            return Failed($"{expr}: out of range (1-100 dice, 2-1000 sides)");

        // A modifier is a 64-bit number, as in roll.sh (roll.ps1 silently drops one that does not fit an int, which
        // is a bug). One too large even for that gets its own message, and so does a total that overflows.
        if (m.Groups[5].Length > 0 && !long.TryParse(m.Groups[5].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out mod))
            return Failed($"{expr}: modifier {m.Groups[5].Value} is too large (at most {long.MaxValue})");

        // Checked before any die is drawn: a rejected expression uses no randomness.
        bool keeping = m.Groups[3].Success;
        int keepN = 0;
        if (keeping && (!TryInt(m.Groups[4].Value, empty: 0, out keepN) || keepN < 1 || keepN > count))
        {
            // Leading zeros are not shown ("kh007" is "7"); a number too long for an int is shown as typed.
            string shown = keepN > 0 ? keepN.ToString(CultureInfo.InvariantCulture)
                : m.Groups[4].Value.TrimStart('0') is { Length: > 0 } digits ? digits : "0";
            return Failed($"{expr}: cannot keep {shown} of {count} dice");
        }

        var dice = new int[count];
        for (int i = 0; i < count; i++)
            dice[i] = Die(sides);

        int[] kept = dice;
        string keepText = "";
        if (keeping)
        {
            var sorted = dice.Order().ToArray();
            if (m.Groups[3].Value == "kh")
                Array.Reverse(sorted);
            kept = sorted[..keepN];
            keepText = $" keep [{string.Join(", ", kept)}]";
        }

        long total;
        try
        {
            total = checked(kept.Sum(d => (long)d) + mod);
        }
        catch (OverflowException)
        {
            return Failed($"{expr}: modifier {m.Groups[5].Value} is too large (at most {long.MaxValue})");
        }
        string modText = mod > 0 ? $" +{mod}" : mod < 0 ? $" {mod}" : "";
        // One d20 counts when a single one is rolled or one is kept: that die is the "natural" roll.
        int? natural = sides == 20 && kept.Length == 1 ? kept[0] : null;
        return new RollOutcome($"{expr}: [{string.Join(", ", dice)}]{keepText}{modText} = {total}", Rolled: true, total, natural);
    }

    /// <summary>
    /// Roll one command-line argument: <c>[label:]dice[@AC&lt;n&gt;|@DC&lt;n&gt;]</c>. The label is everything before the
    /// first colon (so it may have spaces and commas but no colon), the target everything after the last <c>@</c>.
    /// An argument with neither is rolled by <see cref="Roll"/> exactly as it always was.
    /// </summary>
    /// <remarks>
    /// The line is the label and ": ", then the plain line, then " vs AC 14, hit". Meeting the target succeeds, as in 5e.
    /// Against an AC only, a natural 20 hits and a natural 1 misses whatever the total (the 5e rule for attack rolls,
    /// shown as "(natural 20)"); checks and saves, which are DCs, have no such rule. An error line starts with the
    /// label too, is one line, and draws no dice.
    /// </remarks>
    public RollOutcome RollArgument(string argument)
    {
        if (!argument.Contains(':') && !argument.Contains('@'))
            return Roll(argument);

        string? label = null;
        string rest = argument;
        int colon = argument.IndexOf(':');
        if (colon >= 0)
        {
            label = argument[..colon].Trim(' ', '\t');
            rest = argument[(colon + 1)..];
            if (label.Length == 0)
                return Failed($"{Printable(argument)}: the label before ':' is empty (write label:dice, or just the dice)");
            if (label.AsSpan().IndexOfAny(LineBreaks) >= 0)
                return Failed($"{Printable(argument)}: a label must be on one line");
        }
        string prefix = label is null ? "" : label + ": ";

        string? targetText = null;
        int at = rest.LastIndexOf('@');
        if (at >= 0)
        {
            targetText = rest[(at + 1)..].Trim(' ', '\t');
            rest = rest[..at];
        }
        string expr = rest.Trim(' ', '\t');

        Match target = Match.Empty;
        if (targetText is not null)
        {
            target = Target().Match(targetText);
            if (!target.Success || !int.TryParse(target.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                return Failed($"{prefix}{Printable(expr)}: target \"@{Printable(targetText)}\" is not @AC<n> or @DC<n> (try @AC14, @DC13)");
        }

        var outcome = Roll(expr);
        if (!outcome.Rolled)
            return Failed(prefix + outcome.Line);
        if (targetText is null)
            return outcome with { Line = prefix + outcome.Line };

        bool attack = string.Equals(target.Groups[1].Value, "ac", StringComparison.OrdinalIgnoreCase);
        int number = int.Parse(target.Groups[2].Value, CultureInfo.InvariantCulture);
        bool met = outcome.Total >= number;
        string note = "";
        if (attack && outcome.Natural == 20)
            (met, note) = (true, " (natural 20)");
        else if (attack && outcome.Natural == 1)
            (met, note) = (false, " (natural 1)");
        string verdict = attack ? (met ? "hit" : "miss") : (met ? "success" : "fail");
        return outcome with { Line = $"{prefix}{outcome.Line} vs {(attack ? "AC" : "DC")} {number}, {verdict}{note}" };
    }

    /// <summary>
    /// A fair die from 1 to <paramref name="sides"/>. Each draw is 32 random bits; draws at or above the largest
    /// multiple of the die size are thrown away and redrawn, so every face is exactly equally likely (no modulo
    /// bias). The same rule as both scripts.
    /// </summary>
    internal int Die(int sides)
    {
        ulong limit = 4294967296UL - 4294967296UL % (ulong)sides;
        Span<byte> buffer = stackalloc byte[4];
        while (true)
        {
            random.Fill(buffer);
            uint n = BitConverter.ToUInt32(buffer);
            if (n < limit)
                return (int)(n % (uint)sides) + 1;
        }
    }

    /// <summary>
    /// The expression as it is echoed in an error line: line breaks are written out (as a backslash and a letter),
    /// so that an error is always one line, whatever was typed. A rolled line cannot contain one: the pattern
    /// rejects it.
    /// </summary>
    // The characters string.ReplaceLineEndings treats as line breaks: LF, CR, FF, NEL, LS and PS.
    static readonly char[] LineBreaks = ['\n', '\r', (char)0x0C, (char)0x85, (char)0x2028, (char)0x2029];

    static string Printable(string expr)
    {
        if (expr.AsSpan().IndexOfAny(LineBreaks) < 0)
            return expr;
        var text = new System.Text.StringBuilder();
        foreach (char c in expr)
        {
            text.Append(c switch
            {
                '\n' => "\\n",
                '\r' => "\\r",
                _ when LineBreaks.Contains(c) => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        }
        return text.ToString();
    }

    static RollOutcome Failed(string line) => new(line, Rolled: false);

    static bool TryInt(string digits, int empty, out int value)
    {
        if (digits.Length == 0)
        {
            value = empty;
            return true;
        }
        return int.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
}
