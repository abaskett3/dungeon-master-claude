using Dmc.ContextCheck;
using Dmc.Retrieval;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc context-check</c>: replaced <c>scripts/context_check.py</c>. The play launcher's UserPromptSubmit hook
/// runs it before each player message. Claude Code passes a JSON object on stdin that includes
/// <c>transcript_path</c>, the session's log. Nothing is printed until the context passes the first band (the larger of
/// 150k and the context after the session's resume plus 40k, see <see cref="BreakReminder"/>); from there on one line
/// is, and Claude Code adds it to DMC's context, until a reply in that band has offered the break (one offer per band,
/// <see cref="TranscriptReader.OfferedSince"/>). After it, the canon reminder (spec 013): the best canon lines for the
/// people of the register the player's message names, once per person per session part (<see cref="CanonReminder"/>).
/// Then the sheet lines (spec 016 FR-008): what the sheets of the people present say of them now, again once 100,000
/// tokens have passed, and none before DMC's first reply after a <c>/clear</c> or a compaction (<see cref="SheetReminder"/>,
/// spec 019). Last, for a message that starts out of character, the rule for a
/// correction in one line (spec 016 FR-011, <see cref="OocReminder"/>).
/// <para>
/// It never gets in the way of a prompt: on any error (bad input, a missing or unreadable log) it prints nothing
/// and exits 0.
/// </para>
/// </summary>
public sealed class ContextCheckCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "context-check";

    /// <inheritdoc />
    public string Summary => "read the hook's JSON on stdin; print a line when the context is large, canon lines for people the message names, sheet lines for people present, and a line for an out-of-character message (never fails)";

    /// <summary>True: this runs in a hook, so it prints nothing and exits 0 on any failure.</summary>
    public bool NeverFails => true;

    /// <summary>
    /// Reads the hook's JSON from standard input and prints the <see cref="BreakReminder"/> line for the session's
    /// current context size and baseline, or nothing when the size is below the first band or cannot be found; then the
    /// canon reminder for the people the player's message names (<see cref="CanonReminder"/>, spec 013 FR-015); then the
    /// sheet lines of the people present (<see cref="SheetReminder"/>, spec 016 FR-008); then the out-of-character line
    /// (<see cref="OocReminder"/>, spec 016 FR-011). Each part prints nothing when it has nothing.
    /// </summary>
    /// <param name="args">Ignored: the hook passes everything on standard input.</param>
    /// <param name="io">Standard input is the hook's JSON; standard output gets the lines.</param>
    /// <returns>Always 0.</returns>
    // A failure of the break line is swallowed by CommandLine, which is the one place that enforces "never fails" (see
    // NeverFails). Each later part is built before anything is written and fails on its own into nothing, so a bad canon
    // or sheet never costs another part; every line is written last, so a failure leaves no partial output.
    public int Run(string[] args, Io io)
    {
        string input = io.In.ReadToEnd();
        var reading = ContextOfHook(input);
        string line = BreakReminder.For(reading.Size, reading.Baseline);
        // One offer per band (kit/dm-rules.md § Context): once a reply in this band has offered the break, the line
        // is spent, whether the player declined or played on. On any failure the line prints as before.
        if (line.Length > 0 && Quietly(() => TranscriptReader.OfferedSince(TranscriptPath(input)!,
                BreakReminder.BandOf(reading.Size, reading.Baseline)), false))
            line = "";
        string? project = Environment.GetEnvironmentVariable(DmcFolder.ProjectVariable);
        var sheets = Quietly(() => SheetReminder.For(input, project, reading.Size), SheetReminder.Found.None);
        var reminder = Quietly(() => CanonReminder.For(input, project, sheets.WithoutFields), new List<string>());
        string ooc = Quietly(() => OocReminder.For(input), "");
        if (line.Length > 0)
            io.Out.WriteLine(line);
        foreach (string text in reminder.Concat(sheets.Lines))
            io.Out.WriteLine(text);
        if (ooc.Length > 0)
            io.Out.WriteLine(ooc);
        return 0;
    }

    /// <summary>A part of the output, or <paramref name="none"/> on any failure: a hook must never fail or block a message (spec 013 FR-017).</summary>
    /// <typeparam name="T">The part's type.</typeparam>
    /// <param name="part">Builds the part.</param>
    /// <param name="none">What to print instead when it fails.</param>
    static T Quietly<T>(Func<T> part, T none)
    {
        try
        {
            return part();
        }
        catch (Exception)
        {
            return none;
        }
    }

    /// <summary>The context size and baseline the hook's input points at; both 0 when it points at nothing usable.</summary>
    /// <param name="input">The hook's JSON; empty text counts as <c>{}</c>.</param>
    /// <returns>
    /// The reading of the log that <c>transcript_path</c> names (a leading <c>~</c> is expanded): the last context
    /// size and the baseline it is measured from. Both are 0 when the JSON is not an object or has no non-empty
    /// string <c>transcript_path</c>.
    /// </returns>
    /// <exception cref="System.Text.Json.JsonException">The input is not valid JSON. The caller lets it through; <see cref="CommandLine"/> turns it into silence.</exception>
    internal static ContextReading ContextOfHook(string input) =>
        TranscriptPath(input) is { } path ? TranscriptReader.Read(path) : default;

    /// <summary>The session log the hook's input names, with a leading <c>~</c> expanded.</summary>
    /// <param name="input">The hook's JSON; empty text counts as <c>{}</c>.</param>
    /// <returns>The path, or null when the JSON is not an object or has no non-empty string <c>transcript_path</c>.</returns>
    /// <exception cref="System.Text.Json.JsonException">The input is not valid JSON.</exception>
    static string? TranscriptPath(string input)
    {
        var (kind, path) = HookInput.Field(input, "transcript_path");
        return kind != System.Text.Json.JsonValueKind.String || string.IsNullOrEmpty(path)
            ? null
            : ExpandUser(path, OperatingSystem.IsWindows());
    }

    /// <summary>
    /// Python's <c>os.path.expanduser</c> for the forms that matter. <c>~</c> and <c>~/x</c> everywhere; <c>~\x</c>
    /// only on Windows, where <c>ntpath</c> takes both separators. On Linux (<c>posixpath</c>) a
    /// backslash is an ordinary character, so <c>~\x</c> is a file name in the current folder.
    /// </summary>
    /// <param name="path">A file path, possibly starting with <c>~</c>. Other forms (<c>~user</c>) are left as they are.</param>
    /// <param name="windows">True to treat <c>~\</c> as the home folder too (the real program passes <see cref="OperatingSystem.IsWindows"/>).</param>
    /// <returns>The path with a leading <c>~</c> replaced by the user's profile folder.</returns>
    internal static string ExpandUser(string path, bool windows)
    {
        bool home = path == "~" || path.StartsWith("~/", StringComparison.Ordinal)
            || (windows && path.StartsWith("~\\", StringComparison.Ordinal));
        return home ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..] : path;
    }
}
