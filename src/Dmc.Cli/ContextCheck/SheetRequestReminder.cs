using System.Text.RegularExpressions;

namespace Dmc.ContextCheck;

/// <summary>
/// The hook's sheet-request line (spec 031 FR-019): when the player's message names a sheet ("sheet" or "sheets" as a
/// whole word, in any case), one line puts the way to show one in front of DMC at that moment, from
/// <c>dmc-character-sheet-update</c> § Showing a sheet to the player. The slip test of #191 found that a plain request
/// ("I need character sheets please") never reached that section, while a request for one companion's sheet did.
/// Silent for every other message and for a command.
/// </summary>
internal static partial class SheetRequestReminder
{
    /// <summary>The line printed: the skill's rule for showing a sheet, in one line.</summary>
    public const string Line =
        "Sheets: if the player wants to see one, run `dmc sheet` (the player characters), `dmc sheet --party` (the whole "
        + "party) or `dmc sheet NAME` and give the path it prints; never type a sheet out.";

    /// <summary>The word <c>sheet</c> or <c>sheets</c>, whole, in any case.</summary>
    [GeneratedRegex(@"\bsheets?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Sheet();

    /// <summary>The line for a hook's input, or nothing when its prompt does not name a sheet or is a command.</summary>
    /// <param name="input">The hook's JSON.</param>
    /// <returns><see cref="Line"/>, or empty.</returns>
    /// <exception cref="System.Text.Json.JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static string For(string input)
    {
        var (_, prompt) = HookInput.Field(input, "prompt");
        return prompt is not null && !prompt.TrimStart().StartsWith('/') && Sheet().IsMatch(prompt) ? Line : "";
    }
}
