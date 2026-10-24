namespace Dmc.Install;

/// <summary>
/// The program's own refusal to change itself or the generated files from inside a Claude Code session (FR-016, research R6).
/// The play settings also deny <c>dmc install</c>, <c>dmc refresh</c> and <c>dmc update</c>, but a deny rule matches the
/// command's text, so <c>"dmc" update</c>, <c>cmd /c dmc update</c> and <c>&amp; dmc update</c> would get past it. Claude Code sets
/// <c>CLAUDECODE=1</c> for every command it runs, so these commands refuse when it is set, whatever they were called as.
/// </summary>
public static class ClaudeSessionGuard
{
    /// <summary>The environment variable Claude Code sets for every Bash and PowerShell command it runs.</summary>
    public const string Variable = "CLAUDECODE";

    /// <summary>The refusal, a line without the command's name (<see cref="CommandLine"/> adds <c>dmc &lt;command&gt;:</c>).</summary>
    public const string Message = "cannot run inside a Claude Code session: run it in a terminal";

    /// <summary>Refuses when <c>CLAUDECODE</c> is set to anything but nothing. Called first, before a command reads anything else or writes anything.</summary>
    /// <param name="environment">Reads an environment variable; null for one that is not set.</param>
    /// <exception cref="DmcException"><see cref="Message"/> (exit code 1).</exception>
    public static void Refuse(Func<string, string?> environment)
    {
        if (!string.IsNullOrEmpty(environment(Variable)))
            throw new DmcException(Message);
    }
}
