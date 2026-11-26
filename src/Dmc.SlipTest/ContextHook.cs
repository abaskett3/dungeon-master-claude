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

    /// <summary>The most characters a context file may hold: Claude Code shortens a longer hook output, so the pilot could not find it.</summary>
    public const int MaxLength = 10_000;

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
            ["hooks"] = new JsonArray(new JsonObject { ["type"] = "command", ["command"] = command.Replace("{file}", Quoted(file), StringComparison.Ordinal) }),
        });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>
    /// The hook's command line, with <c>{file}</c> where the quoted context file goes (FR-003): the running program,
    /// or <c>dotnet</c> and this assembly when the program is <c>dotnet</c> itself, then <c>context {file}</c>. Paths
    /// use <c>/</c> and are single-quoted (<see cref="Quoted"/>), because Claude Code runs hooks in a POSIX shell on
    /// Windows too.
    /// </summary>
    /// <param name="processPath">The running program (<see cref="Environment.ProcessPath"/>).</param>
    /// <param name="assembly">This assembly's file.</param>
    public static string CommandLine(string processPath, string assembly)
    {
        string program = Quoted(processPath);
        if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            program += " " + Quoted(assembly);
        return $"{program} {Command} {{file}}";
    }

    /// <summary>The command line for this process (<see cref="CommandLine"/>).</summary>
    public static string ThisCommandLine() =>
        CommandLine(Environment.ProcessPath ?? throw new SlipTestException("cannot find the slip-test program to run as a hook"), typeof(ContextHook).Assembly.Location);

    /// <summary>
    /// A path with <c>/</c> for every separator, in POSIX single quotes, with each <c>'</c> in it written <c>'\''</c>, so
    /// the shell expands nothing in it (<c>$</c>, a backtick, <c>"</c>).
    /// </summary>
    /// <param name="path">The path.</param>
    public static string Quoted(string path) => "'" + path.Replace('\\', '/').Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    /// <summary>
    /// <c>slip-test context &lt;file&gt;</c>, which the hook runs (FR-003): prints the file as the hook's
    /// <c>additionalContext</c> in Claude Code's JSON form, so a file that is itself JSON is not read as the hook's
    /// control fields; prints nothing when the file does not exist, so a hook never stops a run.
    /// </summary>
    /// <param name="file">The context file.</param>
    /// <param name="output">Where to print it.</param>
    public static void Print(string file, TextWriter output)
    {
        if (!File.Exists(file))
            return;
        var json = new JsonObject
        {
            ["hookSpecificOutput"] = new JsonObject { ["hookEventName"] = "UserPromptSubmit", ["additionalContext"] = File.ReadAllText(file) },
        };
        output.WriteLine(json.ToJsonString());
    }

    /// <summary>
    /// Refuses, before any run, what would make a context run fail or lose its text: a context file longer than
    /// <see cref="MaxLength"/>, or a DMC folder's <c>.claude/settings.local.json</c> that is not a JSON object.
    /// </summary>
    /// <param name="trial">The trial.</param>
    /// <exception cref="SlipTestException">A file is too long, or the settings do not parse.</exception>
    public static void Check(Trial trial)
    {
        if (trial.Context.Count == 0)
            return;
        foreach (var (arm, _) in trial.Context)
            foreach (var point in trial.Points)
            {
                string file = trial.ContextFile(arm, point.Id)!;
                if (File.ReadAllText(file).Length > MaxLength)
                    throw new SlipTestException($"context of arm {arm}: {point.Id}.md holds more than {MaxLength:N0} characters, which Claude Code shortens in a hook output");
            }
        string settings = SettingsPath(trial.Folder);
        if (!File.Exists(settings))
            return;
        try
        {
            if (JsonNode.Parse(File.ReadAllText(settings)) is not JsonObject)
                throw new SlipTestException($"{settings} is not a JSON object, so the context hook cannot be added to it");
        }
        catch (JsonException e)
        {
            throw new SlipTestException($"{settings} is not valid JSON, so the context hook cannot be added to it: {e.Message}");
        }
    }
}
