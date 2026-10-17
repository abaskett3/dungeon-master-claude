using System.Diagnostics;
using System.Text;

namespace Dmc.Tests.Support;

/// <summary>
/// Shared helpers for the toolkit's tests (the counterpart of <c>tests/support.py</c>). Run everything from
/// anywhere with <c>dotnet test</c>.
/// </summary>
public static class TestEnv
{
    /// <summary>The repository root: the folder above <c>tests/</c>, found by walking up to <c>dmc.sln</c>.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>True when the <c>CI</c> environment variable is set, as GitHub Actions sets it.</summary>
    public static bool OnCi => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

    /// <summary>
    /// Skip the test when an optional tool is missing; on CI, fail instead. A tool that may be missing on a
    /// player's machine (bash, PowerShell, Playwright's Chromium, a published <c>dmc</c>) is installed on CI on purpose.
    /// </summary>
    /// <param name="found">Whether the tool is there.</param>
    /// <param name="what">The tool's name, for the message.</param>
    public static void Need(bool found, string what)
    {
        if (found)
            return;
        if (OnCi)
            Assert.Fail($"{what} is missing on CI, where it must be installed.");
        Assert.Inconclusive($"{what} is not available");
    }

    /// <summary>
    /// Run a command from the repository root and capture its output as text. Each entry of
    /// <paramref name="environment"/> sets a variable, or removes it when the value is null.
    /// </summary>
    /// <param name="file">The program to run.</param>
    /// <param name="args">Its arguments, passed one by one (no shell is involved).</param>
    /// <param name="stdin">What to write to its standard input; nothing when null. It is closed afterwards.</param>
    /// <param name="cwd">The working folder; the repository root when null.</param>
    /// <param name="timeoutSeconds">How long to wait before killing the process and its children.</param>
    /// <param name="environment">Variables to set or remove, on top of the test process's own.</param>
    /// <exception cref="TimeoutException">The process did not finish in time.</exception>
    public static ProcessResult Run(string file, IEnumerable<string> args, string? stdin = null, string? cwd = null,
        int timeoutSeconds = 120, IReadOnlyDictionary<string, string?>? environment = null)
    {
        var start = new ProcessStartInfo(file)
        {
            WorkingDirectory = cwd ?? Root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string arg in args)
            start.ArgumentList.Add(arg);
        foreach (var (name, value) in environment ?? new Dictionary<string, string?>())
        {
            if (value is null)
                start.Environment.Remove(name);
            else
                start.Environment[name] = value;
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"could not start {file}");
        // Raw bytes, not a decoded reader: a reader quietly drops a leading byte-order mark, and a test must see it.
        var stdout = ReadAll(process.StandardOutput.BaseStream);
        var stderr = ReadAll(process.StandardError.BaseStream);
        try
        {
            process.StandardInput.Write(stdin ?? "");
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // The program exited without reading its input: its exit code says why.
        }

        if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{file} did not finish in {timeoutSeconds} s");
        }
        process.WaitForExit(); // let the output readers drain
        // GetString keeps a leading U+FEFF where a StreamReader would swallow the BOM.
        var utf8 = new UTF8Encoding(false);
        return new ProcessResult(process.ExitCode, utf8.GetString(stdout.Result), utf8.GetString(stderr.Result))
        {
            StdoutBytes = stdout.Result,
            StderrBytes = stderr.Result,
        };
    }

    /// <summary>Reads a stream to its end as bytes. Started for output and error together, so that neither pipe fills up and blocks the process.</summary>
    static async Task<byte[]> ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    /// <summary>Walks up from the test assembly's folder to the first folder that holds <c>dmc.sln</c>.</summary>
    static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "dmc.sln")))
                return dir.FullName;
        }
        throw new InvalidOperationException("dmc.sln not found above " + AppContext.BaseDirectory);
    }
}

/// <summary>What a command printed and returned.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Stdout">Standard output decoded as UTF-8; a leading byte-order mark is kept.</param>
/// <param name="Stderr">Standard error, decoded the same way.</param>
public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr)
{
    /// <summary>The raw bytes of stdout and stderr, for a process run through <see cref="TestEnv.Run"/>; null in-process.</summary>
    public byte[]? StdoutBytes { get; init; }

    /// <summary>The raw bytes of stderr; see <see cref="StdoutBytes"/>.</summary>
    public byte[]? StderrBytes { get; init; }

    /// <summary>The non-empty lines of stdout, with any CR removed (PowerShell writes CRLF).</summary>
    public string[] StdoutLines => Lines(Stdout);

    /// <summary>The non-empty lines of stderr, with any CR removed.</summary>
    public string[] StderrLines => Lines(Stderr);

    /// <summary>The exit code and both streams, which is what a failed assertion should show.</summary>
    public override string ToString() => $"exit {ExitCode}\nstdout: {Stdout}\nstderr: {Stderr}";

    /// <summary>The non-empty lines of <paramref name="text"/>, with each trailing CR removed.</summary>
    static string[] Lines(string text) =>
        text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToArray();
}
