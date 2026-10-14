namespace Dmc;

/// <summary>The three standard streams a command reads and writes, so a command can run in-process in a test.</summary>
public sealed record Io(TextReader In, TextWriter Out, TextWriter Err);
