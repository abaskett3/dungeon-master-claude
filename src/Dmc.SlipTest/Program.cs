namespace Dmc.SlipTest;

/// <summary>
/// The entry point of <c>slip-test</c>, the replay runner of spec 024 (issue #249). Usage:
/// <c>slip-test &lt;prepare|run|pilot|export|score&gt; --work &lt;folder&gt; [--pilot] [--claude &lt;program&gt;]</c>.
/// The contract is <c>specs/024-slip-test-runner/contracts/slip-test.md</c>.
/// </summary>
internal static class Program
{
    /// <summary>The usage line printed for a malformed command line.</summary>
    public const string Usage = "usage: slip-test <prepare|run|pilot|export|score> --work <folder> [--pilot] [--claude <program>]";

    /// <summary>Runs the program on the real console, Claude Code folder and clock.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>0 when the command did its work, 1 otherwise.</returns>
    static int Main(string[] args) => Run(args, Console.Out, Console.Error, Folders.ClaudeHome(), null, DateTimeOffset.UtcNow, new Random());

    /// <summary>Runs a command with every outside dependency passed in, so the tests can call it in-process.</summary>
    /// <param name="args">The command line.</param>
    /// <param name="output">Where reports go.</param>
    /// <param name="error">Where the one-line error goes.</param>
    /// <param name="claudeHome">Claude Code's folder.</param>
    /// <param name="claude">The <c>claude</c> program, or null to take <c>--claude</c> or find it on the PATH.</param>
    /// <param name="now">The time a lock records.</param>
    /// <param name="random">The source of the arm order, the ids and the export's order.</param>
    /// <param name="fake">A stand-in for Claude Code (the tests), or null for the real program.</param>
    /// <returns>0 when the command did its work (for <c>pilot</c>, every check passed), 1 otherwise.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error, string claudeHome, string? claude, DateTimeOffset now, Random random, IClaude? fake = null)
    {
        try
        {
            if (args.Length == 0)
                throw new SlipTestException(Usage);
            string command = args[0];
            string? work = null;
            bool pilot = false;
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--work" when i + 1 < args.Length:
                        work = args[++i];
                        break;
                    case "--claude" when i + 1 < args.Length:
                        claude = args[++i];
                        break;
                    case "--pilot":
                        pilot = true;
                        break;
                    default:
                        throw new SlipTestException(Usage);
                }
            }
            if (work is null)
                throw new SlipTestException(Usage);
            var trial = Trial.Load(work);
            switch (command)
            {
                case "prepare":
                    new Runner(trial, claudeHome, null, output).Prepare(now);
                    return 0;
                case "run":
                    new Runner(trial, claudeHome, fake ?? new ClaudeProcess(claude ?? ClaudeRunner.Find()), output).Run(pilot, random);
                    return 0;
                case "pilot":
                {
                    trial.CheckLock();
                    var points = new Runner(trial, claudeHome, null, output).Resolve();
                    var (report, passed) = PilotCheck.Run(trial, points, RunRow.ReadAll(trial.Work));
                    File.WriteAllText(Path.Combine(trial.Work, "pilot.md"), report);
                    output.Write(report);
                    return passed ? 0 : 1;
                }
                case "export":
                {
                    trial.CheckLock();
                    var points = new Runner(trial, claudeHome, null, output).Resolve();
                    var items = BlindExport.Collect(trial, points, RunRow.ReadAll(trial.Work));
                    BlindExport.Write(trial.Work, items, random);
                    output.WriteLine($"exported {items.Count} replies to {Path.Combine(trial.Work, "export", "replies.md")}");
                    return 0;
                }
                case "score":
                {
                    trial.CheckLock();
                    string report = Scoring.Report(trial, Scoring.Read(trial.Work), RunRow.ReadAll(trial.Work));
                    File.WriteAllText(Path.Combine(trial.Work, "result.md"), report);
                    output.Write(report);
                    return 0;
                }
                default:
                    throw new SlipTestException(Usage);
            }
        }
        catch (SlipTestException e)
        {
            error.WriteLine($"slip-test: {e.Message}");
            return 1;
        }
        catch (IOException e)
        {
            error.WriteLine($"slip-test: {e.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException e)
        {
            error.WriteLine($"slip-test: {e.Message}");
            return 1;
        }
    }
}
