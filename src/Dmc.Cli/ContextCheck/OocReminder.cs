using System.Text.RegularExpressions;

namespace Dmc.ContextCheck;

/// <summary>
/// The hook's out-of-character line (spec 016 FR-011, research R5, contracts/context-check-sheets.md): when the player's
/// message starts with <c>ooc</c> or <c>((</c>, the marks <c>kit/templates/player-options.md</c> gives for speaking out of
/// character, one line puts the rule for a correction in front of DMC at that moment. Silent for every other message.
/// </summary>
internal static partial class OocReminder
{
    /// <summary>The line printed: the rule of <c>kit/dm-rules.md</c> for a correction (FR-010), in one line.</summary>
    public const string Line =
        "Out of character: if this says a fact is wrong or asks how someone knew, read the files, open your reply by quoting "
        + "what they say, for and against, say what you think, propose one fix, and change no file until the player agrees.";

    /// <summary>A message that starts out of character: <c>ooc</c> in any case followed by a space, a colon or the end, or <c>((</c>.</summary>
    [GeneratedRegex(@"^\s*(?:ooc(?:[\s:]|$)|\(\()", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OutOfCharacter();

    /// <summary>The line for a hook's input, or nothing when its prompt does not start out of character.</summary>
    /// <param name="input">The hook's JSON.</param>
    /// <returns><see cref="Line"/>, or empty.</returns>
    /// <exception cref="System.Text.Json.JsonException">The input is not JSON; the caller turns any failure into silence.</exception>
    public static string For(string input)
    {
        var (_, prompt) = HookInput.Field(input, "prompt");
        return prompt is not null && OutOfCharacter().IsMatch(prompt) ? Line : "";
    }
}
