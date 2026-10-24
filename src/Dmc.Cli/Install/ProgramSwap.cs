using System.Diagnostics;

namespace Dmc.Install;

/// <summary>
/// The two steps of <c>dmc update</c> that run programs and replace files in the program folder (contract
/// <c>dmc-update.md</c>, steps 5 to 7; research R9): <see cref="Swap"/> puts the new program in place of the old with
/// <c>*.old</c> renames (a running <c>dmc.exe</c> can be renamed, not deleted) and moves back what it moved if anything fails;
/// <see cref="Start"/> runs a program and captures what it prints.
/// </summary>
internal static class ProgramSwap
{
    /// <summary>Runs a program and returns its exit code and what it printed; the seam of <c>dmc update</c> for the tests, which pass a fake.</summary>
    /// <param name="file">The program to run.</param>
    /// <param name="arguments">Its arguments.</param>
    /// <returns>The exit code and the lines the program printed on standard output and standard error, in that order, trimmed.</returns>
    internal delegate (int ExitCode, string Output) Runner(string file, string[] arguments);

    /// <summary>
    /// Moves the files of an unpacked release into the program folder: each file already there is renamed to <c>*.old</c>
    /// first (to <c>*.&lt;random&gt;.old</c> when an old <c>*.old</c> cannot be deleted because something still runs it), the
    /// program itself last. If any step fails, everything moved so far is moved back (the new files out, the old ones in) and the
    /// failure is reported: nothing is half-installed. Only files at the top of the folder are moved.
    /// </summary>
    /// <param name="unpacked">The folder the archive was unpacked into.</param>
    /// <param name="programFolder">The program folder; it must exist.</param>
    /// <param name="exeName">The name of the program (<see cref="ProgramInstall.ExeName"/>), moved last.</param>
    /// <param name="afterMove">Called with the number of files moved so far after each one (null in the real program); a test throws from it to fail the swap in the middle.</param>
    /// <exception cref="DmcException"><c>cannot write &lt;folder&gt;: &lt;reason&gt;</c>, after moving back what was moved.</exception>
    public static void Swap(string unpacked, string programFolder, string exeName, Action<int>? afterMove = null)
    {
        var done = new List<(string Target, string? Aside)>();
        try
        {
            int moved = 0;
            foreach (string file in Directory.EnumerateFiles(unpacked).OrderBy(f => Path.GetFileName(f) == exeName ? 1 : 0).ThenBy(f => Path.GetFileName(f), StringComparer.Ordinal))
            {
                string target = Path.Combine(programFolder, Path.GetFileName(file));
                string? aside = null;
                if (File.Exists(target))
                {
                    aside = AsideName(target);
                    File.Move(target, aside);
                }
                done.Add((target, aside));
                File.Move(file, target);
                afterMove?.Invoke(++moved);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MoveBack(done);
            throw new DmcException($"cannot write {programFolder}: {e.Message}");
        }
    }

    /// <summary>Where an old file goes while it is replaced: <c>&lt;file&gt;.old</c>, or with a random part when an earlier <c>.old</c> is still in use and cannot be deleted.</summary>
    /// <param name="target">The file that is being replaced.</param>
    static string AsideName(string target)
    {
        string aside = target + ".old";
        try
        {
            if (File.Exists(aside))
                File.Delete(aside);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            aside = $"{target}.{Guid.NewGuid():N}.old";
        }
        return aside;
    }

    /// <summary>Undoes a swap as far as it got, last move first: the new file is removed and the old one renamed back. Best effort: a file that cannot be moved back is left for the player to see.</summary>
    /// <param name="done">What was moved, in order.</param>
    static void MoveBack(List<(string Target, string? Aside)> done)
    {
        for (int i = done.Count - 1; i >= 0; i--)
        {
            var (target, aside) = done[i];
            try
            {
                if (File.Exists(target))
                    File.Delete(target);
                if (aside is not null && File.Exists(aside))
                    File.Move(aside, target);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // nothing more can be done for this file; the others are still moved back
            }
        }
    }

    /// <summary>
    /// The real <see cref="Runner"/>: starts the program with no window and no input, waits at most two minutes, and returns its
    /// exit code and its output (standard output, then standard error). A program that cannot be started or does not finish
    /// is exit code 1 with the reason.
    /// </summary>
    /// <param name="file">The program to run.</param>
    /// <param name="arguments">Its arguments.</param>
    public static (int ExitCode, string Output) Start(string file, string[] arguments)
    {
        var info = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
            info.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(info) ?? throw new IOException($"could not start {file}");
            process.StandardInput.Close();
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
            {
                process.Kill(entireProcessTree: true);
                return (1, "it did not finish within two minutes");
            }
            Task.WaitAll(output, error);
            return (process.ExitCode, (output.Result + error.Result).Trim());
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (1, e.Message);
        }
    }
}
