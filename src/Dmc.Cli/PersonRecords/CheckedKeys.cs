using System.Globalization;

namespace Dmc.PersonRecords;

/// <summary>
/// The fact keys whose values <c>dmc</c> computes with (spec 036 FR-002, research R5): the six ability scores, their
/// saves, the proficiency bonus and passive Perception, which <c>dmc sheet</c>'s arithmetic reads and the print builds the
/// ability table from. Each value must start with its number. Any other key is stored, sourced and printed as it is.
/// </summary>
internal static class CheckedKeys
{
    /// <summary>The six abilities, as the ability table names them (<see cref="Sheets.SheetLayouts.Abilities"/>).</summary>
    public static IReadOnlyList<string> Abilities => Sheets.SheetLayouts.Abilities;

    /// <summary>The suffix of a save's key: <c>STR save</c>.</summary>
    public const string SaveSuffix = " save";

    /// <summary>The proficiency bonus's key.</summary>
    public const string Proficiency = "Proficiency bonus";

    /// <summary>Passive Perception's key.</summary>
    public const string PassivePerception = "Passive Perception";

    /// <summary>What is wrong with a checked key's value, or null when it is right or the key is not checked.</summary>
    /// <param name="key">The fact's key, trimmed.</param>
    /// <param name="value">Its value.</param>
    public static string? ValueError(string key, string value)
    {
        int? n = Sheets.SheetNumbers.ParseSigned(value);
        bool signed = value.TrimStart().Length > 0 && value.TrimStart()[0] is '+' or '-' or '−' or '–';
        if (Abilities.FirstOrDefault(a => a.Equals(key, StringComparison.OrdinalIgnoreCase)) is { } ability)
            return n is >= 1 and <= 30 && !signed ? null : $"\"{value}\" is not a score: {ability} is a whole number from 1 to 30";
        if (key.EndsWith(SaveSuffix, StringComparison.OrdinalIgnoreCase)
            && Abilities.Any(a => a.Equals(key[..^SaveSuffix.Length].Trim(), StringComparison.OrdinalIgnoreCase)))
            return n is not null ? null : $"\"{value}\" is not a save: write the signed number (+3, -1)";
        if (key.Equals(Proficiency, StringComparison.OrdinalIgnoreCase))
            return n is >= 2 and <= 9 ? null : $"\"{value}\" is not a proficiency bonus: write +2 to +9";
        if (key.Equals(PassivePerception, StringComparison.OrdinalIgnoreCase))
            return n is >= 0 && !signed ? null : $"\"{value}\" is not a passive Perception: write a whole number";
        return null;
    }

    /// <summary>The score a record's facts give for an ability, or null when it has none that reads as one.</summary>
    /// <param name="facts">The record's facts.</param>
    /// <param name="ability">The ability (<c>STR</c>).</param>
    public static int? Score(IReadOnlyList<Fact> facts, string ability) =>
        facts.FirstOrDefault(f => f.Key?.Trim().Equals(ability, StringComparison.OrdinalIgnoreCase) == true) is { Value: { } v }
            ? Sheets.SheetNumbers.ParseSigned(v) : null;

    /// <summary>A modifier with its sign, as the ability table prints it.</summary>
    /// <param name="score">The score.</param>
    public static string Modifier(int score) => Sheets.SheetNumbers.WithSign(Sheets.SheetNumbers.Modifier(score));

    /// <summary>Whether a key is one of the six scores or their saves, which print in the ability table rather than as lines.</summary>
    /// <param name="key">The key.</param>
    public static bool InAbilityTable(string key)
    {
        string k = key.Trim();
        if (k.EndsWith(SaveSuffix, StringComparison.OrdinalIgnoreCase))
            k = k[..^SaveSuffix.Length].Trim();
        return Abilities.Any(a => a.Equals(k, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A number as invariant text.</summary>
    /// <param name="n">The number.</param>
    internal static string Text(int n) => n.ToString(CultureInfo.InvariantCulture);
}
