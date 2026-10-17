using System.Text;

namespace Dmc;

/// <summary>The three standard streams a command reads and writes, so a command can run in-process in a test.</summary>
/// <param name="In">Standard input.</param>
/// <param name="Out">Standard output: what the command reports.</param>
/// <param name="Err">Standard error: messages about the run, never its result.</param>
public sealed record Io(TextReader In, TextWriter Out, TextWriter Err)
{
    /// <summary>UTF-8 with no byte-order mark: what dmc prints lands in DMC's context, so it stays plain.</summary>
    public static Encoding Utf8 { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// <see cref="Utf8"/>, but a character that cannot be encoded (a lone surrogate, which JSON can spell as an escape)
    /// is written as "?", as Python does with <c>errors="replace"</c>. This is what standard output uses, and what a
    /// command uses for a file it writes, so that the two agree.
    /// </summary>
    public static Encoding Utf8Replacing { get; } = MakeReplacing();

    /// <summary>Builds <see cref="Utf8Replacing"/>.</summary>
    static Encoding MakeReplacing()
    {
        // A clone, because the encoding above is read-only.
        var encoding = (Encoding)new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).Clone();
        encoding.EncoderFallback = new EncoderReplacementFallback("?");
        return encoding;
    }
}
