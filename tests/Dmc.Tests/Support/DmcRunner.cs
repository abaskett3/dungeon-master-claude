namespace Dmc.Tests.Support;

/// <summary>
/// The two ways the tests run <c>dmc</c>: in-process, against the library code, and black-box, as the published
/// binary in a subprocess, the way DMC calls it (the counterpart of <c>tests/support.py: run_script</c>).
/// </summary>
public static class DmcRunner
{
    /// <summary>
    /// Run <c>dmc</c> in this process, with the real command table and an in-memory stdin, stdout and stderr.
    /// Output is always LF, as the real program writes it.
    /// </summary>
    public static ProcessResult InProcess(string[] args, string? stdin = null, IReadOnlyList<ICommand>? commands = null)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var errors = new StringWriter { NewLine = "\n" };
        int exitCode = CommandLine.Run(args, new Io(new StringReader(stdin ?? ""), output, errors),
            commands ?? CommandTable.All);
        return new ProcessResult(exitCode, output.ToString(), errors.ToString());
    }

    /// <summary>
    /// Run the published binary. It must exist: publish it first with
    /// <c>dotnet publish src/Dmc.Cli -r &lt;rid&gt; -o bin</c>. Set <c>DMC_BIN</c> to test a binary somewhere else.
    /// </summary>
    public static ProcessResult BlackBox(string[] args, string? stdin = null, string? cwd = null) =>
        TestEnv.Run(Binary(), args, stdin, cwd);

    /// <summary>Full path of the published binary, after checking that it is there and not older than the sources.</summary>
    public static string Binary()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("DMC_BIN");
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            TestEnv.Need(File.Exists(fromEnvironment), $"the binary named by DMC_BIN ({fromEnvironment})");
            return fromEnvironment;
        }

        string binary = Path.Combine(TestEnv.Root, "bin", OperatingSystem.IsWindows() ? "dmc.exe" : "dmc");
        TestEnv.Need(File.Exists(binary), "the published dmc (run: dotnet publish src/Dmc.Cli -r <rid> -o bin)");

        // A binary left over from before the last edit would pass or fail for the wrong reasons.
        var built = File.GetLastWriteTimeUtc(binary);
        var newest = NewestSource();
        if (newest.Time > built)
            Assert.Fail($"{binary} is older than {newest.Path}: republish it with dotnet publish src/Dmc.Cli -r <rid> -o bin");
        return binary;
    }

    static (DateTime Time, string Path) NewestSource()
    {
        string[] extensions = [".cs", ".csproj", ".props"];
        string build = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        string output = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        return Directory.EnumerateFiles(Path.Combine(TestEnv.Root, "src"), "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)) && !f.Contains(build) && !f.Contains(output))
            .Concat(Directory.EnumerateFiles(TestEnv.Root, "Directory.*.props"))
            .Select(f => (Time: File.GetLastWriteTimeUtc(f), Path: f))
            .MaxBy(x => x.Time);
    }
}
