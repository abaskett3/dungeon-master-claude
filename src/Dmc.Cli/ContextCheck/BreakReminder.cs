namespace Dmc.ContextCheck;

/// <summary>
/// The line that tells DMC a break is due. DMC cannot see its own context size; this is how it learns it. A break
/// is due when the context is large: first at the larger of <see cref="Threshold"/> and the session's baseline
/// (where the context settled after loading, see <see cref="TranscriptReader"/>) plus <see cref="Margin"/>, then at
/// every <see cref="Band"/> after that. A resume that loads 100k therefore does not trigger a break one round later.
/// </summary>
public static class BreakReminder
{
    /// <summary>The floor of the first band: no break is due below this context size (tokens), whatever the baseline.</summary>
    public const long Threshold = 150000;

    /// <summary>How far above the baseline the context must grow before the first break is due.</summary>
    public const long Margin = 40000;

    /// <summary>The reminder names a band that rises every this many tokens, so a declined offer is repeated at the next band.</summary>
    public const long Band = 40000;

    /// <summary>Where the first band starts for a session whose context settled at <paramref name="baseline"/>.</summary>
    /// <param name="baseline">The context after loading, in tokens; 0 or less when unknown.</param>
    /// <returns>The larger of <see cref="Threshold"/> and the baseline plus <see cref="Margin"/>.</returns>
    public static long FirstBand(long baseline) => Math.Max(Threshold, baseline + Margin);

    /// <summary>
    /// The reminder for a context of <paramref name="size"/> tokens; empty below the first band. The band named
    /// is the latest one reached, counted from the first band, so a declined offer is made again only when the
    /// context has grown by another <see cref="Band"/>.
    /// </summary>
    /// <param name="size">The latest context size, in tokens.</param>
    /// <param name="baseline">The context after loading, in tokens; the default 0 gives the plain <see cref="Threshold"/>.</param>
    /// <returns>One line for DMC, or an empty string when no break is due.</returns>
    public static string For(long size, long baseline = 0)
    {
        long first = FirstBand(baseline);
        if (size < first)
            return "";
        long band = first + (size - first) / Band * Band;
        return $"Context {size / 1000}k is large, break due since {band / 1000}k: at the next break point, offer a full save " +
               $"and a fresh session, unless the player has declined since {band / 1000}k.";
    }
}
