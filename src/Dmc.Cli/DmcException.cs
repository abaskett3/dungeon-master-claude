namespace Dmc;

/// <summary>
/// A failure the user can understand and fix. <see cref="CommandLine"/> prints it as the single line
/// <c>dmc &lt;command&gt;: &lt;message&gt;</c> and exits with <see cref="ExitCode"/>. Never a stack trace.
/// </summary>
public sealed class DmcException(string message, int exitCode = 1) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
