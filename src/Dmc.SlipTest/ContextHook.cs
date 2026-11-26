using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dmc.SlipTest;

/// <summary>
/// The hook that puts an arm's text next to the player's message at a fork point (spec 029, #270): one
/// <c>UserPromptSubmit</c> hook in the copy folder's <c>.claude/settings.local.json</c> that runs
/// <c>slip-test context &lt;file&gt;</c>, so Claude Code records the text as a hook output, as it does the kit's.
/// </summary>
internal static class ContextHook
{
    /// <summary>The command name the hook runs: <c>slip-test context &lt;file&gt;</c>.</summary>
    public const string Command = "context";

    /// <summary>The copy folder's local settings file, which the hook is added to.</summary>
    /// <param name="folder">A DMC folder or its copy.</param>
    public static string SettingsPath(string folder) => Path.Combine(folder, ".claude", "settings.local.json");

    /// <summary>
    /// Puts the copy's <c>.claude/settings.local.json</c> back as the DMC folder has it, or removes it when the DMC
    /// folder has none (FR-002). The runner calls it before and after every fork run, so a run that was stopped leaves
    /// no hook for the next one.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="copy">The copy folder.</param>
    public static void Reset(string folder, string copy)
    {
        string original = SettingsPath(folder), target = SettingsPath(copy);
        if (File.Exists(original))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(original, target, overwrite: true);
        }
        else if (File.Exists(target))
            File.Delete(target);
    }

    /// <summary>
    /// Adds the hook that prints <paramref name="file"/> to the copy's local settings, keeping every setting and hook
    /// already there (FR-002). Call <see cref="Reset"/> first.
    /// </summary>
    /// <param name="copy">The copy folder.</param>
    /// <param name="file">The context file to print.</param>
    /// <param name="command">The hook's command line (<see cref="CommandLine"/>).</param>
    /// <exception cref="SlipTestException">The local settings are not a JSON object.</exception>
    public static void Add(string copy, string file, string command)
    {
        string path = SettingsPath(copy);
        JsonObject settings;
        try
        {
            settings = File.Exists(path)
                ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new SlipTestException($"{path} is not a JSON object")
                : [];
        }
        catch (JsonException e)
        {
            throw new SlipTestException($"{path} is not valid JSON: {e.Message}");
        }
        if (settings["hooks"] is not JsonObject hooks)
            settings["hooks"] = hooks = [];
        if (hooks["UserPromptSubmit"] is not JsonArray prompt)
            hooks["UserPromptSubmit"] = prompt = [];
        prompt.Add(new JsonObject
        {
            ["hooks"] = new JsonArray(new JsonObject { ["type"] = "command", ["command"] = command.Replace("{file}", Slashed(file), StringComparison.Ordinal) }),
        });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>
    /// The hook's command line, with <c>{file}</c> where the context file goes (FR-003): the running program, or
    /// <c>dotnet</c> and this assembly when the program is <c>dotnet</c> itself, then <c>context "{file}"</c>. Paths
    /// are quoted and use <c>/</c>, because Claude Code runs hooks in a POSIX shell on Windows too.
    /// </summary>
    /// <param name="processPath">The running program (<see cref="Environment.ProcessPath"/>).</param>
    /// <param name="assembly">This assembly's file.</param>
    public static string CommandLine(string processPath, string assembly)
    {
        string program = $"\"{Slashed(processPath)}\"";
        if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            program += $" \"{Slashed(assembly)}\"";
        return $"{program} {Command} \"{{file}}\"";
    }

    /// <summary>The command line for this process (<see cref="CommandLine"/>).</summary>
    public static string ThisCommandLine() =>
        CommandLine(Environment.ProcessPath ?? throw new SlipTestException("cannot find the slip-test program to run as a hook"), typeof(ContextHook).Assembly.Location);

    /// <summary>A path with <c>/</c> for every separator.</summary>
    /// <param name="path">The path.</param>
    static string Slashed(string path) => path.Replace('\\', '/');

    /// <summary>
    /// <c>slip-test context &lt;file&gt;</c>, which the hook runs: prints the file as it is, and nothing when it does
    /// not exist, so a hook never stops a run (FR-003).
    /// </summary>
    /// <param name="file">The context file.</param>
    /// <param name="output">Where to print it.</param>
    public static void Print(string file, TextWriter output)
    {
        if (File.Exists(file))
            output.Write(File.ReadAllText(file));
    }
}
