namespace Dmc.ContextCheck;

/// <summary>The line that tells DMC a break is due. DMC cannot see its own context size; this is how it learns it.</summary>
public static class BreakReminder
{
    /// <summary>From this context size (tokens) on, a reminder is printed on every message.</summary>
    public const long Threshold = 150000;

    /// <summary>The reminder names a band that rises every this many tokens, so a declined offer is repeated at the next band.</summary>
    public const long Band = 40000;

    /// <summary>The reminder for a context of <paramref name="size"/> tokens; empty below <see cref="Threshold"/>.</summary>
    public static string For(long size)
    {
        if (size < Threshold)
            return "";
        long band = Threshold + (size - Threshold) / Band * Band;
        return $"Context {size / 1000}k, break due since {band / 1000}k: at the next break point, offer a full save " +
               $"and a fresh session, unless the player has declined since {band / 1000}k.";
    }
}
