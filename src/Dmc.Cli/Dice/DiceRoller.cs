using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Dice;

/// <summary>What became of one expression: the line to print, and whether it was rolled.</summary>
public readonly record struct RollOutcome(string Line, bool Rolled);

/// <summary>
/// Rolls dice expressions exactly as <c>scripts/roll.ps1</c> and <c>roll.sh</c> do: <c>NdS</c>, <c>khK</c>/<c>klK</c>
/// and a flat <c>+M</c>/<c>-M</c>, with the same limits, the same output line and the same error lines.
/// </summary>
public sealed partial class DiceRoller(IRandomBytes random)
{
    public const string Usage = "usage: dmc roll <dice> [<dice> ...]   e.g. 1d20+5 2d6+3 2d20kh1+5 4d6kh3";

    // [0-9] and \z rather than \d and $: ASCII digits only, and no match before a trailing newline (as in roll.sh).
    [GeneratedRegex(@"^([0-9]*)d([0-9]+)(?:(kh|kl)([0-9]+))?([+-][0-9]+)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex Expression();

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
        return new RollOutcome($"{expr}: [{string.Join(", ", dice)}]{keepText}{modText} = {total}", Rolled: true);
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
