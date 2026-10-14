namespace Dmc;

/// <summary>
/// The seam for the stale-binary warning. Phase 5 fills it in; until then it does nothing, because
/// <c>tools/dmc-release.json</c> (the pinned release) does not exist yet.
/// </summary>
/// <remarks>
/// What Phase 5 puts here: compare <see cref="CommandLine.Version"/> with the tag in
/// <see cref="ReleaseFile"/>. If they differ, print one line to <paramref name="err"/> naming the command to run
/// (<c>tools/get-dmc</c>) and carry on. Stay silent when the file is missing or unreadable, as it is in a
/// developer's checkout. Commands with <see cref="ICommand.NeverFails"/> (the hook) never reach this call.
/// </remarks>
public static class StaleBinaryCheck
{
    public static string ReleaseFile { get; } = Path.Combine(Toolkit.Root, "tools", "dmc-release.json");

    public static void Run(TextWriter err)
    {
    }
}
