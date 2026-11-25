namespace Dmc.SlipTest;

/// <summary>
/// A failure the user can fix: a malformed input file, a missing log, a changed locked file. <see cref="Program"/>
/// prints its message as one line, prefixed with <c>slip-test:</c>, and exits 1.
/// </summary>
/// <param name="message">The one line to print, with no trailing full stop.</param>
internal sealed class SlipTestException(string message) : Exception(message);
