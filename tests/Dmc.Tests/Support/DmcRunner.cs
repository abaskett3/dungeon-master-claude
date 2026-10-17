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
    /// <param name="args">The command line, without the program name.</param>
    /// <param name="stdin">What the command reads from standard input; empty when null.</param>
    /// <param name="commands">The commands to choose from, so a test can add a fake one; the real table when null.</param>
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
    /// <c>dotnet publish src/Dmc.Cli -r &lt;rid&gt; -o artifacts/bin</c> (not into <c>bin/</c>, which is the player's install
    /// that <c>get-dmc</c> owns). Set <c>DMC_BIN</c> to test a binary somewhere else.
    /// </summary>
    /// <param name="args">The command line, without the program name.</param>
    /// <param name="stdin">What the process reads from standard input; empty when null.</param>
    /// <param name="cwd">The working folder; the repository root when null.</param>
    public static ProcessResult BlackBox(string[] args, string? stdin = null, string? cwd = null) =>
        TestEnv.Run(Binary(), args, stdin, cwd);

    /// <summary>The version every build that is not a release prints.</summary>
    public const string DevVersion = "0.0.0-dev";

    /// <summary>The command that makes the binary the black-box tests need, quoted in the failure messages.</summary>
    const string Publish = "dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin";

    /// <summary>
    /// Full path of the published binary: <c>DMC_BIN</c> if set, whatever it prints; otherwise <c>artifacts/bin/dmc</c>,
    /// which must be there, not older than the sources, and a development build (a release there would be tests of the
    /// wrong program).
    /// </summary>
    /// <remarks>The tests are skipped, or fail on CI, when the binary is missing (see <see cref="TestEnv.Need"/>).</remarks>
    public static string Binary()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("DMC_BIN");
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            TestEnv.Need(File.Exists(fromEnvironment), $"the binary named by DMC_BIN ({fromEnvironment})");
            return fromEnvironment;
        }

        string binary = Path.Combine(TestEnv.Root, "artifacts", "bin", OperatingSystem.IsWindows() ? "dmc.exe" : "dmc");
        TestEnv.Need(File.Exists(binary), $"the published dmc (run: {Publish})");

        // A binary left over from before the last edit would pass or fail for the wrong reasons.
        var built = File.GetLastWriteTimeUtc(binary);
        var newest = NewestSource();
        if (newest.Time > built)
            Assert.Fail($"{binary} is older than {newest.Path}: republish it with {Publish}");
        string printed = _printed.Value;
        if (printed != "dmc " + DevVersion)
            Assert.Fail($"{binary} prints '{printed}', not 'dmc {DevVersion}': republish it with {Publish}");
        return binary;
    }

    /// <summary>What <c>artifacts/bin/dmc --version</c> prints, asked once for the whole run.</summary>
    static readonly Lazy<string> _printed = new(() =>
        TestEnv.Run(Path.Combine(TestEnv.Root, "artifacts", "bin", OperatingSystem.IsWindows() ? "dmc.exe" : "dmc"), ["--version"]).Stdout.Trim());

    /// <summary>The most recently changed source file of the CLI project (<c>.cs</c>, <c>.csproj</c> and the shared <c>Directory.*.props</c>; not <c>obj/</c> or <c>bin/</c>), with its time.</summary>
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
