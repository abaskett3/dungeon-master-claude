namespace Dmc.SessionLog;

/// <summary>
/// What the text of a user record in a Claude Code session log is: something the player typed, or something Claude
/// Code or a hook put there. <c>dmc digest</c> and <c>dmc context-check</c> read the same logs and share this one
/// classifier, so they cannot disagree about it.
/// </summary>
internal static class UserText
{
    /// <summary>The prefixes of text that Claude Code or a hook injects, which the digest does not count as a player message.</summary>
    static readonly string[] InjectionPrefixes = ["<local-command", "<command-name>", "<system-reminder>"];

    /// <summary>
    /// The further prefixes of text that is no typed message either: a slash-command echo, the output of a shell
    /// command the player ran, a background-task notice, an interruption marker. The digest keeps these as turns (a
    /// command echo is how it sees a resume, an interruption is counted); a context baseline must not.
    /// </summary>
    static readonly string[] OtherNonTypedPrefixes = ["<command-", "<bash-", "<task-notification", "[Request interrupted"];

    /// <summary>True when <paramref name="text"/> is injected by Claude Code or a hook: local-command output, a command name, a system reminder.</summary>
    /// <param name="text">The text of one content block of a user record.</param>
    /// <returns>True when it starts with one of the injection prefixes.</returns>
    public static bool IsInjected(string text) => StartsWithAny(text, InjectionPrefixes);

    /// <summary>
    /// True when <paramref name="text"/> is a message the player typed: not an injection, a slash-command echo, a
    /// shell-command record, a task notice or an interruption marker.
    /// </summary>
    /// <param name="text">The text of one content block of a user record.</param>
    /// <returns>True for a typed message.</returns>
    public static bool IsTyped(string text) => !IsInjected(text) && !StartsWithAny(text, OtherNonTypedPrefixes);

    /// <summary>True when <paramref name="text"/> starts with any of <paramref name="prefixes"/> (ordinal).</summary>
    /// <param name="text">The text to test.</param>
    /// <param name="prefixes">The prefixes.</param>
    /// <returns>True on a match.</returns>
    static bool StartsWithAny(string text, string[] prefixes)
    {
        foreach (string prefix in prefixes)
            if (text.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        return false;
    }
}
