using System.Text.Json;
using Dmc.Maps;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc redraw</c>: the play settings' Stop hook runs it when DMC has finished a reply. If the map the player's
/// page shows has changed since it was drawn, it draws the page again, so the picture changes only after the reply
/// that describes the change has been written. Claude Code passes a JSON object on standard input; the field that
/// matters is <c>stop_hook_active</c>.
/// </summary>
/// <remarks>
/// <para>
/// It prints nothing and exits 0 when there is nothing to do (no active campaign, no page drawn yet, no change), and
/// on any failure that is not a refused map file. A map file that <see cref="MapChecker"/> refuses is the one case
/// that is reported: one line on standard error and exit code 2, which for a Stop hook makes Claude Code hand the line
/// to DMC and let it carry on, so DMC corrects the file and the reply that follows draws it.
/// </para>
/// <para>
/// <c>stop_hook_active</c> is true when Claude Code is already continuing because a Stop hook asked it to. The map is
/// still drawn then (it may be the corrected file), but a refusal is not reported a second time in the same turn, so a
/// file DMC cannot fix never makes it loop. Nothing is lost by that: a file that is still refused stays newer than the
/// picture, so the next reply, which starts a new turn, is reported again. The message says so. When the command is
/// run by hand at a terminal it does not wait for input, and acts as if <c>stop_hook_active</c> were false. Which file
/// is drawn, and when, is decided by <see cref="MapRedraw"/>. If a <c>live.png</c> sits beside the page it is
/// refreshed too, which costs nothing when something is drawn anyway.
/// </para>
/// </remarks>
/// <param name="root">The toolkit root, the folder that holds <c>adventures/</c>; <see cref="Toolkit.Root"/> when null. A test passes a temporary folder.</param>
public sealed class RedrawCommand(string? root = null) : ICommand
{
    /// <summary>The folder that holds <c>adventures/</c>.</summary>
    readonly string _root = root ?? Toolkit.Root;

    /// <inheritdoc />
    public string Name => "redraw";

    /// <inheritdoc />
    public string Summary => "Stop hook: redraw the player's map if its file changed (exit 2 with one line if the file is refused)";

    /// <summary>True: this runs in a hook, so it skips the stale-binary check and any failure but a refused map file prints nothing and exits 0.</summary>
    public bool NeverFails => true;

    /// <summary>
    /// Reads the hook's JSON from standard input (not when that is a terminal), finds the map file to draw again
    /// (<see cref="MapRedraw.Pending"/>) and draws it, with <c>live.png</c> too when one already sits beside the page.
    /// </summary>
    /// <param name="args">Ignored: the hook passes everything on standard input.</param>
    /// <param name="io">Standard input is the hook's JSON; standard error gets the line for a refused file.</param>
    /// <returns>0, or 2 when the map file was refused and no Stop hook was already active this turn.</returns>
    // The refusal is the only failure handled here; anything else is swallowed by CommandLine (see NeverFails).
    public int Run(string[] args, Io io)
    {
        bool active = !io.InIsTerminal && StopHookActive(io.In.ReadToEnd());
        string? path = MapRedraw.Pending(_root);
        if (path is null)
            return 0;
        try
        {
            bool png = File.Exists(Path.Combine(Path.GetDirectoryName(path) ?? "", "live.png"));
            MapRedraw.Draw(path, png);
            return 0;
        }
        catch (DmcException e)
        {
            if (active)
                return 0;
            io.Err.WriteLine($"dmc redraw: {Path.GetFileName(path)} was not drawn, so the player's map is out of date: "
                             + $"{e.Message.ReplaceLineEndings(" ").Trim()} Fix it now; if it is still refused it will be reported again when your next reply ends.");
            return 2;
        }
    }

    /// <summary>Whether the hook's input says <c>"stop_hook_active": true</c>. Empty input, JSON that is not an object, or anything else gives false.</summary>
    /// <param name="input">The hook's JSON.</param>
    internal static bool StopHookActive(string input)
    {
        try
        {
            return HookInput.Field(input, "stop_hook_active").Kind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
