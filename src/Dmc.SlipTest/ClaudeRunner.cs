using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>What one <c>claude -p --output-format json</c> run reports.</summary>
/// <param name="SessionId">The session id of the log the run wrote.</param>
/// <param name="Input">Uncached input tokens.</param>
/// <param name="Output">Output tokens.</param>
/// <param name="CacheRead">Input tokens read from the cache.</param>
/// <param name="CacheWrite">Input tokens written to the cache.</param>
/// <param name="CostUsd">The cost Claude Code reports (<c>total_cost_usd</c>).</param>
/// <param name="IsError">Whether Claude Code reported an error result.</param>
internal sealed record ClaudeResult(string SessionId, long Input, long Output, long CacheRead, long CacheWrite, double CostUsd, bool IsError);

/// <summary>
/// The settings of one replay's command line: where it runs and how the original session ran (FR-003).
/// </summary>
/// <param name="Resume">The session id to resume with <c>--fork-session</c>, or null for a new session (a fresh-mode base).</param>
/// <param name="Model">The model, or null for Claude Code's default.</param>
/// <param name="Effort">The effort, or null.</param>
/// <param name="Permission">The permission mode, or null.</param>
internal sealed record ClaudeCall(string? Resume, string? Model, string? Effort, string? Permission);

/// <summary>How the runner starts Claude Code; the tests give a fake that writes a fork log itself.</summary>
internal interface IClaude
{
    /// <summary>Claude Code's version, or "unknown".</summary>
    string Version();

    /// <summary>Runs one call in a folder with the message on standard input.</summary>
    /// <param name="cwd">The folder to run in.</param>
    /// <param name="call">The call.</param>
    /// <param name="message">The player message.</param>
    /// <param name="raw">The whole standard output.</param>
    ClaudeResult Run(string cwd, ClaudeCall call, string message, out string raw);
}

/// <summary>Claude Code itself, as a program on this computer.</summary>
/// <param name="program">The <c>claude</c> program's path.</param>
internal sealed class ClaudeProcess(string program) : IClaude
{
    /// <inheritdoc/>
    public string Version() => ClaudeRunner.Version(program);

    /// <inheritdoc/>
    public ClaudeResult Run(string cwd, ClaudeCall call, string message, out string raw) => ClaudeRunner.Run(program, cwd, call, message, out raw);
}

/// <summary>Builds and runs the <c>claude</c> command line of a replay, and reads its JSON output.</summary>
internal static class ClaudeRunner
{
    /// <summary>The arguments of a call: print mode with JSON output, the resume and fork, and the original's model, effort and permission mode. The message goes on standard input, so it needs no quoting.</summary>
    /// <param name="call">The call.</param>
    public static List<string> Arguments(ClaudeCall call)
    {
        var args = new List<string> { "-p", "--output-format", "json" };
        if (call.Resume is not null)
            args.AddRange(["--resume", call.Resume, "--fork-session"]);
        if (call.Model is not null)
            args.AddRange(["--model", call.Model]);
        if (call.Effort is not null)
            args.AddRange(["--effort", call.Effort]);
        if (call.Permission is not null)
            args.AddRange(["--permission-mode", call.Permission]);
        return args;
    }

    /// <summary>
    /// Finds Claude Code on the PATH: <c>claude.exe</c> on Windows, <c>claude</c> elsewhere. npm's <c>claude.cmd</c>
    /// shim is not used, because <c>cmd.exe</c> re-parses arguments; name the program with <c>--claude</c> instead.
    /// </summary>
    /// <exception cref="SlipTestException">It is not on the PATH.</exception>
    public static string Find()
    {
        string name = OperatingSystem.IsWindows() ? "claude.exe" : "claude";
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(dir, name);
            if (File.Exists(candidate))
                return candidate;
        }
        throw new SlipTestException($"{name} is not on the PATH: install Claude Code, or name it with --claude");
    }

    /// <summary>
    /// Runs one call in a folder with the message on standard input, and reads its output. <c>CLAUDECODE</c> is
    /// removed from the child's environment, so a run started from inside a Claude Code session starts like one from
    /// a terminal.
    /// </summary>
    /// <param name="claude">The program.</param>
    /// <param name="cwd">The folder to run in (the copy folder).</param>
    /// <param name="call">The call.</param>
    /// <param name="message">The player message.</param>
    /// <param name="raw">Claude Code's whole standard output, to keep beside the run.</param>
    /// <exception cref="SlipTestException">The program cannot start, exits with an error, or prints no JSON result.</exception>
    public static ClaudeResult Run(string claude, string cwd, ClaudeCall call, string message, out string raw)
    {
        var info = new ProcessStartInfo(claude)
        {
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string arg in Arguments(call))
            info.ArgumentList.Add(arg);
        info.Environment.Remove("CLAUDECODE");
        Process process;
        try
        {
            process = Process.Start(info) ?? throw new SlipTestException($"could not start {claude}");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new SlipTestException($"could not start {claude}: {e.Message}");
        }
        using (process)
        {
            var stderr = process.StandardError.ReadToEndAsync();
            process.StandardInput.Write(message);
            process.StandardInput.Close();
            raw = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            string error = stderr.Result.Trim();
            if (process.ExitCode != 0 && raw.Trim().Length == 0)
                throw new SlipTestException($"claude exited with code {process.ExitCode}: {FirstLine(error)}");
            return Parse(raw);
        }
    }

    /// <summary>The first line of a text, or "no output".</summary>
    /// <param name="text">Some text.</param>
    static string FirstLine(string text) => text.Length == 0 ? "no output" : text.Split('\n')[0].Trim();

    /// <summary>Reads Claude Code's JSON result: the session id, the four token counts and the cost.</summary>
    /// <param name="json">The standard output of <c>claude -p --output-format json</c>.</param>
    /// <exception cref="SlipTestException">It is not a JSON object with a session id.</exception>
    public static ClaudeResult Parse(string json)
    {
        JsonObject? o;
        try
        {
            o = JsonNode.Parse(json.Trim()) as JsonObject;
        }
        catch (JsonException)
        {
            o = null;
        }
        if (o is null || LogFile.Text(o, "session_id") is not { Length: > 0 } id)
            throw new SlipTestException($"claude printed no JSON result: {FirstLine(json)}");
        var usage = o["usage"] as JsonObject;
        return new ClaudeResult(
            id,
            Count(usage, "input_tokens"),
            Count(usage, "output_tokens"),
            Count(usage, "cache_read_input_tokens"),
            Count(usage, "cache_creation_input_tokens"),
            o["total_cost_usd"] is JsonValue c && c.TryGetValue(out double cost) ? cost : 0,
            o["is_error"]?.GetValueKind() == JsonValueKind.True);
    }

    /// <summary>A whole-number property, or 0.</summary>
    /// <param name="o">The object, or null.</param>
    /// <param name="name">The property.</param>
    static long Count(JsonObject? o, string name) => o?[name] is JsonValue v && v.TryGetValue(out long n) ? n : 0;

    /// <summary>Claude Code's version (<c>claude --version</c>), or "unknown".</summary>
    /// <param name="claude">The program.</param>
    public static string Version(string claude)
    {
        try
        {
            var info = new ProcessStartInfo(claude, "--version") { UseShellExecute = false, RedirectStandardOutput = true };
            info.Environment.Remove("CLAUDECODE");
            using var p = Process.Start(info);
            if (p is null)
                return "unknown";
            string v = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return v.Length > 0 ? v.Split(' ')[0] : "unknown";
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return "unknown";
        }
    }
}
