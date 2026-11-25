namespace Dmc.Tests.Support;

/// <summary>
/// The size of fixture text for a token count, in the measure of spec 021 (2 tokens for 5 characters, and 160 a Read
/// call), worked out here by hand so a fixture does not depend on the code under test. Fixture text is ASCII with LF
/// line ends, so its characters and bytes are the same.
/// </summary>
public static class TokenSizes
{
    /// <summary>What one Read call adds beyond its text, as <c>Measurement.ReadCallTokens</c> says.</summary>
    public const int ReadCall = 160;

    /// <summary>The fewest characters whose text counts <paramref name="tokens"/> tokens or more: 5 for every 2, rounded up to the 5.</summary>
    /// <param name="tokens">The tokens wanted.</param>
    public static int Chars(long tokens) => checked((int)(5 * ((tokens + 1) / 2)));

    /// <summary>The fewest characters whose one Read costs <paramref name="tokens"/> or more: <see cref="Chars"/> of the tokens less <see cref="ReadCall"/>.</summary>
    /// <param name="tokens">The Read cost wanted.</param>
    public static int ReadChars(long tokens) => Chars(tokens - ReadCall);
}
