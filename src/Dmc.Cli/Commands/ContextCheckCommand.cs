using System.Text.Json;
using Dmc.ContextCheck;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc context-check</c>: replaces <c>scripts/context_check.py</c>. The play launcher's UserPromptSubmit hook
/// runs it before each player message. Claude Code passes a JSON object on stdin that includes
/// <c>transcript_path</c>, the session's log. Below the threshold nothing is printed; from it on, one line is, and
/// Claude Code adds it to DMC's context.
/// <para>
/// It never gets in the way of a prompt: on any error (bad input, a missing or unreadable log) it prints nothing
/// and exits 0.
/// </para>
/// </summary>
public sealed class ContextCheckCommand : ICommand
{
    public string Name => "context-check";

    public string Summary => "read the hook's JSON on stdin; print a line when the context is large (never fails)";

    public bool NeverFails => true;

    public int Run(string[] args, Io io)
    {
        try
        {
            string line = BreakReminder.For(ContextOfHook(io.In.ReadToEnd()));
            if (line.Length > 0)
                io.Out.WriteLine(line);
        }
        catch (Exception)
        {
            // A broken check must never block the player's message.
        }
        return 0;
    }

    /// <summary>The context size the hook's input points at; 0 when it points at nothing usable.</summary>
    internal static long ContextOfHook(string input)
    {
        using var hook = JsonDocument.Parse(string.IsNullOrEmpty(input) ? "{}" : input);
        if (hook.RootElement.ValueKind != JsonValueKind.Object
            || !hook.RootElement.TryGetProperty("transcript_path", out var value)
            || value.ValueKind != JsonValueKind.String)
            return 0;
        string? path = value.GetString();
        return string.IsNullOrEmpty(path) ? 0 : TranscriptReader.LastContext(ExpandUser(path));
    }

    /// <summary>Python's <c>os.path.expanduser</c> for the forms that matter: <c>~</c>, <c>~/x</c> and <c>~\x</c>.</summary>
    static string ExpandUser(string path)
    {
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        return path;
    }
}
