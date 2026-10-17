namespace Dmc.Tests.Support;

/// <summary>
/// One input for a <c>dmc</c> subcommand: its arguments, stdin and starting files. The lists of these in
/// <c>MapCases</c>, <c>PdfCases</c> and <c>DigestCases</c> are run by the one-line error guard and by a check that every
/// input that should work still does.
/// </summary>
/// <param name="Name">Shown in the test name and in failure messages.</param>
/// <param name="Args">The arguments after the subcommand; they get the working folder, to name files in it.</param>
/// <param name="Stdin">The standard input, given the working folder; null for none.</param>
/// <param name="Seed">Writes the starting files into a fresh working folder.</param>
public sealed record CommandCase(string Name, Func<string, string[]> Args, Func<string, string>? Stdin = null,
    Action<string>? Seed = null)
{
    public CommandCase(string name, params string[] args) : this(name, _ => args)
    {
    }

    public override string ToString() => Name;

    /// <summary>
    /// Run the published <c>dmc &lt;command&gt;</c> on this case in a fresh working folder, then hand the result and the
    /// folder to <paramref name="check"/> before the folder is removed.
    /// </summary>
    public void Run(string command, Action<ProcessResult, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Seed?.Invoke(folder);
            var result = DmcRunner.BlackBox([command, .. Args(folder)], Stdin?.Invoke(folder), folder);
            check(result, folder);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// <see cref="Run"/> every case, and fail once at the end with every case whose check failed, so that one run shows
    /// them all.
    /// </summary>
    public static void RunAll(IEnumerable<CommandCase> cases, string command, Action<CommandCase, ProcessResult, string> check)
    {
        var problems = new List<string>();
        int count = 0;
        foreach (var test in cases)
        {
            count++;
            try
            {
                test.Run(command, (result, folder) => check(test, result, folder));
            }
            catch (AssertFailedException e)
            {
                problems.Add($"{test.Describe(command)}: {e.Message}");
            }
        }
        Assert.IsGreaterThan(0, count, "no cases");
        if (problems.Count > 0)
            Assert.Fail($"{problems.Count} of {count} cases failed:\n" + string.Join("\n\n", problems));
    }

    /// <summary>The command line, for messages.</summary>
    public string Describe(string command) => $"dmc {command} {string.Join(' ', Args("<work>"))} ({Name})";
}
