using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dmc.Commands;
using Dmc.Digest;
using Dmc.Tests.Distribution;
using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The toolkit as a whole: the settings, the launchers, the skills and the files the prompts point at. These are the
/// checks of the deleted <c>tests/test_toolkit.py</c>, rewritten for <c>dmc</c>: every command the prompts tell DMC to
/// run is a real subcommand and needs no permission prompt, the hooks work with and without <c>bin/dmc</c>, and nothing
/// still points at the old scripts.
/// </summary>
[TestClass]
public sealed partial class ToolkitTests
{
    /// <summary>A file of the repository (path relative to the root) as text, with LF line endings.</summary>
    static string Read(string path) => File.ReadAllText(Path.Combine(TestEnv.Root, path)).Replace("\r\n", "\n");

    /// <summary>One of the settings files in <c>.claude/</c>, parsed.</summary>
    static JsonNode Settings(string name) => JsonNode.Parse(Read(Path.Combine(".claude", name)))!;

    /// <summary>The folder names under <c>.claude/skills</c>, in ordinal order: the skills' names.</summary>
    static string[] SkillNames() =>
        Directory.GetDirectories(Path.Combine(TestEnv.Root, ".claude", "skills")).Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal).ToArray();

    /// <summary>Every file DMC reads as instructions, relative to the repository root, with forward slashes.</summary>
    static string[] PromptFiles() =>
    [
        "CLAUDE.md", "README.md",
        .. new[] { ".claude/skills", "templates" }.SelectMany(folder =>
            Directory.EnumerateFiles(Path.Combine(TestEnv.Root, folder), "*.md", SearchOption.AllDirectories))
            .Select(f => Path.GetRelativePath(TestEnv.Root, f).Replace('\\', '/')).Order(StringComparer.Ordinal),
    ];

    /// <summary>
    /// The <c>Tool(pattern)</c> rules of one permission list (<c>allow</c> or <c>deny</c>) of a settings file, each with
    /// its pattern as a regex. One parser serves both lists so that they cannot disagree about what a pattern matches.
    /// </summary>
    static (string Tool, string Pattern, Regex Matches)[] Rules(string file, string list) =>
        (Settings(file)["permissions"]?[list]?.AsArray() ?? []).Select(r => Rule().Match((string)r!)).Where(m => m.Success)
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value, PatternRegex(m.Groups[1].Value, m.Groups[2].Value)))
            .ToArray();

    /// <summary>
    /// A rule's pattern as a regex over one command, as Claude Code documents the matching: <c>*</c> stands for any
    /// text wherever it is; a trailing <c> *</c> also matches the bare command, but only when it is the pattern's only
    /// wildcard (<c>find * -delete *</c> does not match <c>find x -delete</c>); PowerShell matching ignores case.
    /// </summary>
    static Regex PatternRegex(string tool, string pattern)
    {
        string regex = pattern.EndsWith(" *", StringComparison.Ordinal) && pattern.Count(c => c == '*') == 1
            ? Regex.Escape(pattern[..^2]) + "( .*)?"
            : Regex.Escape(pattern).Replace(@"\*", ".*");
        return new Regex("^" + regex + "$", RegexOptions.Singleline | (tool == "PowerShell" ? RegexOptions.IgnoreCase : RegexOptions.None));
    }

    /// <summary>The rules of the allow list in settings.json.</summary>
    static readonly (string Tool, string Pattern, Regex Matches)[] AllowRules = Rules("settings.json", "allow");

    /// <summary>The rules of the deny list in play-settings.json.</summary>
    static readonly (string Tool, string Pattern, Regex Matches)[] DenyRules = Rules("play-settings.json", "deny");

    /// <summary>A permission rule such as <c>Bash(./bin/dmc roll *)</c>: group 1 is the tool, group 2 the pattern.</summary>
    [GeneratedRegex(@"^(\w+)\((.*)\)$")]
    private static partial Regex Rule();

    /// <summary>Whether an allow rule's pattern takes the whole command.</summary>
    static bool Allowed(string tool, string command) => AllowRules.Any(r => r.Tool == tool && r.Matches.IsMatch(command));

    /// <summary>Whether a deny rule of play-settings.json takes the whole command (one subcommand: Claude Code splits chains itself).</summary>
    static bool Denied(string tool, string command) => DenyRules.Any(r => r.Tool == tool && r.Matches.IsMatch(command));

    /// <summary>A command the prompts tell DMC to run: <c>./bin/dmc</c> and what follows, up to the end of the code span or line.</summary>
    [GeneratedRegex(@"\./bin/dmc(?: [^`\n]*)?")]
    private static partial Regex DmcCommand();

    /// <summary>
    /// A subcommand, as in <c>`dmc map`</c> or <c>./bin/dmc pdf info &lt;pdf&gt;</c> (in a code span or a code block),
    /// and the word after it, if that is a plain word.
    /// </summary>
    [GeneratedRegex(@"(?:`|\./bin/)dmc ([a-z][a-z-]*)(?: ([a-z][a-z-]*))?")]
    private static partial Regex Subcommand();

    /// <summary>The subcommands of the commands that have their own, taken from the parsers.</summary>
    static readonly Dictionary<string, string[]> Choices = new()
    {
        ["digest"] = DigestArguments.Commands,
        ["pdf"] = PdfCommand.Commands,
    };

    // ---------- the settings ----------

    [TestMethod]
    public void EveryDmcCommandInThePromptsIsARealSubcommandThatNeedsNoPermissionPrompt()
    {
        var known = CommandTable.All.Select(c => c.Name).ToHashSet();
        var used = new HashSet<string>();
        foreach (string file in PromptFiles())
        {
            string text = Read(file);
            foreach (Match m in Subcommand().Matches(text))
            {
                Assert.Contains(m.Groups[1].Value, known, $"{file} names `dmc {m.Groups[1].Value}`, which is not a dmc subcommand");
                used.Add(m.Groups[1].Value);
                if (Choices.TryGetValue(m.Groups[1].Value, out var choices) && m.Groups[2].Success)
                    Assert.Contains(m.Groups[2].Value, choices, $"{file} names `dmc {m.Groups[1].Value} {m.Groups[2].Value}`, which is not one of: {string.Join(", ", choices)}");
            }
            foreach (string command in DmcCommand().Matches(text).Select(m => m.Value.TrimEnd()))
            {
                // The same words run in either shell, so both must be allowed.
                Assert.IsTrue(Allowed("Bash", command), $"{file}: `{command}` is not allowed for Bash in settings.json");
                Assert.IsTrue(Allowed("PowerShell", command), $"{file}: `{command}` is not allowed for PowerShell in settings.json");
            }
        }
        CollectionAssert.IsSubsetOf(new[] { "roll", "map", "pdf", "digest" }, used.ToArray(), "the prompts name every subcommand DMC runs");

        // The way to fetch a missing dmc, which CLAUDE.md gives in two forms that work whatever PowerShell's execution policy.
        const string bash = "bash tools/get-dmc.sh", powershell = "powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1";
        string claude = Read("CLAUDE.md");
        StringAssert.Contains(claude, $"`{bash}`");
        StringAssert.Contains(claude, $"`{powershell}`");
        Assert.IsTrue(Allowed("Bash", bash));
        Assert.IsTrue(Allowed("PowerShell", powershell));
        // Windows names: dmc.exe, and PowerShell's backslashes.
        foreach (string command in new[] { "./bin/dmc.exe roll 1d20", ".\\bin\\dmc roll 1d20", ".\\bin\\dmc.exe roll 1d20" })
            Assert.IsTrue(Allowed("PowerShell", command), command);
        Assert.IsTrue(Allowed("Bash", "./bin/dmc.exe roll 1d20"));
    }

    [TestMethod]
    public void TheAllowListNamesOnlyDmcAndGetDmc()
    {
        foreach (var (tool, pattern, _) in AllowRules.Where(r => r.Tool is "Bash" or "PowerShell"))
        {
            string path = pattern.Replace('\\', '/');
            Assert.IsTrue(path.StartsWith("./bin/dmc", StringComparison.Ordinal) || path.Contains("tools/get-dmc.", StringComparison.Ordinal),
                $"{tool}({pattern}) is neither dmc nor get-dmc");
            if (Regex.Match(path, @"tools/[\w.-]+") is { Success: true } file)
                Assert.IsTrue(File.Exists(Path.Combine(TestEnv.Root, file.Value)), $"{tool}({pattern}) names {file.Value}, which is missing");
        }
    }

    /// <summary><c>settings.json</c> allows editing <c>feedback/</c>, so <c>record-feedback</c> raises no permission prompt in play.</summary>
    [TestMethod]
    public void RecordingFeedbackInPlayRaisesNoPermissionPrompt()
    {
        string[] allowed = Settings("settings.json")["permissions"]!["allow"]!.AsArray().Select(r => (string)r!).ToArray();
        CollectionAssert.Contains(allowed, "Edit(feedback/**)", "settings.json must allow editing feedback/, where record-feedback writes");
    }

    [TestMethod]
    public void HooksAreCommands()
    {
        foreach (string name in new[] { "settings.json", "play-settings.json" })
        {
            foreach (var (eventName, groups) in Settings(name)["hooks"]!.AsObject())
            {
                foreach (var hook in groups!.AsArray().SelectMany(g => g!["hooks"]!.AsArray()))
                {
                    Assert.AreEqual("command", (string?)hook!["type"], $"{name} {eventName}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace((string?)hook["command"]), $"{name} {eventName}");
                }
            }
        }
    }

    /// <summary>
    /// Run a hook's command the way Claude Code does, with bash, in a fresh toolkit folder. Its <c>bin/</c> holds a copy of
    /// the published <c>dmc</c> (<see cref="DmcRunner.Binary"/>, so <c>DMC_BIN</c> is honoured), or a file that is not a
    /// program, or nothing.
    /// </summary>
    static ProcessResult RunHook(string command, BinDmc dmc, Func<string, string>? stdin = null, Action<string>? seed = null)
    {
        string project = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(project);
        try
        {
            string exe = Path.Combine(project, "bin", Bootstrap.ExeName);
            if (dmc != BinDmc.Missing)
                Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
            if (dmc == BinDmc.Published)
                File.Copy(DmcRunner.Binary(), exe);
            if (dmc == BinDmc.Junk)
            {
                File.WriteAllText(exe, "not a program\n");
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            seed?.Invoke(project);
            return TestEnv.Run(Bootstrap.Shell("bash"), ["-c", command], stdin?.Invoke(project), cwd: project,
                environment: new Dictionary<string, string?> { ["CLAUDE_PROJECT_DIR"] = project });
        }
        finally
        {
            Directory.Delete(project, recursive: true);
        }
    }

    /// <summary>What <c>bin/dmc</c> is in a test of the session-start hook.</summary>
    enum BinDmc
    {
        /// <summary>There is no file.</summary>
        Missing,

        /// <summary>A file that is not a program.</summary>
        Junk,

        /// <summary>A copy of the published binary under test, which runs.</summary>
        Published,
    }

    [TestMethod]
    public void WithoutAWorkingBinDmcASessionIsToldOnceHowToGetIt()
    {
        string[] commands = Settings("settings.json")["hooks"]!["SessionStart"]!.AsArray()
            .Where(g => g!["matcher"] is null).SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!)
            .Where(c => c.Contains("get-dmc", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, commands, "one SessionStart hook, for every kind of start, checks bin/dmc");

        foreach (var dmc in new[] { BinDmc.Missing, BinDmc.Junk })
        {
            var told = RunHook(commands[0], dmc);
            Assert.AreEqual(0, told.ExitCode, told.ToString());
            Assert.HasCount(1, told.StdoutLines, $"{dmc}: {told}");
            StringAssert.Contains(told.Stdout, "bash tools/get-dmc.sh");
            StringAssert.Contains(told.Stdout, "powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1");
            Assert.AreEqual("", told.Stderr, dmc.ToString());
        }
        var working = RunHook(commands[0], BinDmc.Published);
        Assert.AreEqual(0, working.ExitCode, working.ToString());
        Assert.AreEqual("", working.Stdout + working.Stderr, "silent when bin/dmc runs");
    }

    [TestMethod]
    public void PlayRunsTheBreakReminderBeforeEachMessageAndItNeverGetsInTheWay()
    {
        string[] commands = (Settings("play-settings.json")["hooks"]?["UserPromptSubmit"]?.AsArray() ?? [])
            .SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!).ToArray();
        Assert.HasCount(1, commands, string.Join("\n", commands));
        string hook = commands[0];
        StringAssert.Contains(hook, "\"$CLAUDE_PROJECT_DIR/bin/dmc\" context-check");
        // Exit code 2 from a UserPromptSubmit hook blocks the prompt; a shell that cannot find bin/dmc exits 127.
        Assert.IsTrue(hook.TrimEnd().EndsWith("|| true", StringComparison.Ordinal), hook);

        // Without bin/dmc: no output at all, and exit 0.
        var missing = RunHook(hook, BinDmc.Missing, _ => "{}");
        Assert.AreEqual(0, missing.ExitCode, missing.ToString());
        Assert.AreEqual("", missing.Stdout + missing.Stderr, "a missing bin/dmc must not show in every prompt");

        // With the published dmc: the reminder, from a log past the threshold.
        var due = RunHook(hook, BinDmc.Published, project => new JsonObject { ["transcript_path"] = Path.Combine(project, "session.jsonl") }.ToJsonString(),
            project => File.WriteAllText(Path.Combine(project, "session.jsonl"), ContextCheck.ContextCheckContract.Assistant(31000).ToJsonString() + "\n"
                + ContextCheck.ContextCheckContract.User("go on").ToJsonString() + "\n"
                + ContextCheck.ContextCheckContract.Assistant(163400).ToJsonString() + "\n"));
        Assert.AreEqual(0, due.ExitCode, due.ToString());
        StringAssert.StartsWith(due.Stdout, "Context 163k is large, break due since 150k", due.ToString());
    }

    /// <summary>The Stop hook's command in play-settings.json; fails unless there is exactly one.</summary>
    static string StopHook()
    {
        string[] commands = (Settings("play-settings.json")["hooks"]?["Stop"]?.AsArray() ?? [])
            .SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!).ToArray();
        Assert.HasCount(1, commands, "play-settings.json needs one Stop hook: " + string.Join("\n", commands));
        return commands[0];
    }

    /// <summary>
    /// Seeds a toolkit folder for a Stop-hook test: an active campaign <c>test</c> whose <c>maps/live.json</c> holds
    /// <paramref name="title"/>, drawn once with the published <c>dmc</c> when <paramref name="drawn"/> is true, as
    /// <c>dmc map</c> does when DMC first shows a map.
    /// </summary>
    static Action<string> Campaign(string title, bool drawn = true) => project =>
    {
        string maps = Path.Combine(project, "adventures", "test", "maps");
        Directory.CreateDirectory(maps);
        File.WriteAllText(Path.Combine(project, "adventures", ".active-campaign"), "test\n");
        File.WriteAllText(Path.Combine(maps, "live.json"), PlaceJson(title));
        if (drawn)
        {
            var map = TestEnv.Run(DmcRunner.Binary(), ["map", Path.Combine(maps, "live.json")]);
            Assert.AreEqual(0, map.ExitCode, map.ToString());
        }
    };

    /// <summary>A small valid map file with this title; with <paramref name="refused"/>, one that <c>dmc map</c> refuses (rows of different lengths).</summary>
    static string PlaceJson(string title, bool refused = false) =>
        new JsonObject { ["title"] = title, ["grid"] = new JsonArray("#####", refused ? "#..#" : "#...#", "#####") }.ToJsonString();

    /// <summary>Edits <c>maps/live.json</c> after the drawing: the picture is dated ten seconds back, then the file gets new text.</summary>
    static Action<string> Edited(string title, bool refused = false) => project =>
    {
        Campaign("first")(project);
        string live = Path.Combine(project, "adventures", "test", "maps", "live.json");
        File.SetLastWriteTimeUtc(Path.Combine(Path.GetDirectoryName(live)!, "live-data.js"), DateTime.UtcNow.AddSeconds(-10));
        File.WriteAllText(live, PlaceJson(title, refused));
    };

    /// <summary>Hook input as Claude Code sends it to a Stop hook.</summary>
    static string StopInput(bool active) => new JsonObject { ["hook_event_name"] = "Stop", ["stop_hook_active"] = active }.ToJsonString();

    [TestMethod]
    public void PlayRedrawsTheMapAfterEachReplyAndOnlyRefusalsAreHeard()
    {
        string hook = StopHook();
        StringAssert.Contains(hook, "\"$CLAUDE_PROJECT_DIR/bin/dmc\" redraw");

        // Nothing changed, and nothing to draw: no output, exit 0.
        foreach (var seed in new Action<string>?[] { Campaign("same"), null, Campaign("not drawn", drawn: false) })
        {
            var quiet = RunHook(hook, BinDmc.Published, _ => StopInput(false), seed);
            Assert.AreEqual(0, quiet.ExitCode, quiet.ToString());
            Assert.AreEqual("", quiet.Stdout + quiet.Stderr);
        }

        // A changed file is drawn again, silently; the hook runs in a subshell so that the picture can be read after it.
        string showPicture = "(" + hook + "); echo \"hook exit $?\"; cat \"$CLAUDE_PROJECT_DIR/adventures/test/maps/live-data.js\"";
        var drawn = RunHook(showPicture, BinDmc.Published, _ => StopInput(false), Edited("After the reply"));
        Assert.AreEqual(0, drawn.ExitCode, drawn.ToString());
        StringAssert.StartsWith(drawn.Stdout, "hook exit 0");
        StringAssert.Contains(drawn.Stdout, "After the reply");
        Assert.AreEqual("", drawn.Stderr);

        // A refused file: one line on stderr and exit 2, which makes Claude Code hand it to DMC.
        var refused = RunHook(hook, BinDmc.Published, _ => StopInput(false), Edited("Broken", refused: true));
        Assert.AreEqual(2, refused.ExitCode, refused.ToString());
        Assert.AreEqual("", refused.Stdout);
        Assert.HasCount(1, refused.StderrLines, refused.ToString());
        StringAssert.StartsWith(refused.Stderr, "dmc redraw: live.json was not drawn");

        // With a Stop hook already active this turn, a refusal is not reported again.
        var again = RunHook(hook, BinDmc.Published, _ => StopInput(true), Edited("Broken", refused: true));
        Assert.AreEqual(0, again.ExitCode, again.ToString());
        Assert.AreEqual("", again.Stdout + again.Stderr);
    }

    [TestMethod]
    public void TheStopHookIsSilentWithoutADmcThatKnowsRedraw()
    {
        string hook = StopHook();
        // A 0.1.2 dmc exits 1 with "unknown command"; no binary or one that is not a program exits 126 or 127. None may block or print.
        Action<string> oldDmc = project =>
        {
            Campaign("same")(project);
            string exe = Path.Combine(project, "bin", "dmc");
            Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
            File.WriteAllText(exe, "#!/bin/sh\necho 'dmc: unknown command \"redraw\" (try roll, context-check, map, pdf, digest, play, --version)' >&2\nexit 1\n");
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        };
        foreach (var (dmc, seed) in new (BinDmc, Action<string>?)[] { (BinDmc.Missing, oldDmc), (BinDmc.Missing, Campaign("same", drawn: false)), (BinDmc.Junk, Campaign("same", drawn: false)) })
        {
            var result = RunHook(hook, dmc, _ => StopInput(false), seed);
            Assert.AreEqual(0, result.ExitCode, $"{dmc}: {result}");
            Assert.AreEqual("", result.Stdout + result.Stderr, dmc.ToString());
        }
    }

    [TestMethod]
    public void ThePlaySettingsKeepEverythingPlayNeeds()
    {
        var settings = Settings("play-settings.json");
        // Tools play, the dice and map commands, rules lookups and the module and import skills need.
        string[] needed = ["Read", "Write", "Edit", "Glob", "Grep", "Bash", "PowerShell", "Skill", "ToolSearch",
                           "AskUserQuestion", "WebSearch", "WebFetch", "Agent"];
        // Bash(rm *) removes one command, not the tool; every other rule (a bare name or glob, Bash(*), Edit(adventures/**)) can take away what play needs.
        foreach (string rule in (settings["permissions"]?["deny"]?.AsArray().Select(r => (string)r!) ?? []).Where(r => !NamesACommand(r)))
        {
            string toolName = Rule().Match(rule) is { Success: true } m ? m.Groups[1].Value : rule;
            string pattern = "^" + Regex.Escape(toolName).Replace(@"\*", ".*") + "$";
            foreach (string tool in needed)
                Assert.IsFalse(Regex.IsMatch(tool, pattern), $"play-settings.json denies {tool} ({rule}), which play needs");
        }
        var overrides = settings["skillOverrides"]?.AsObject();
        foreach (string skill in SkillNames())
            Assert.AreEqual("on", (string?)overrides?[skill] ?? "on", $"play-settings.json hides /{skill}");
        // The project's settings.json still applies under --settings, and its allow list is the one to keep up to date.
        Assert.IsNull(settings["permissions"]?["allow"]);
    }

    /// <summary>
    /// Whether a deny rule names one specific command, <c>Bash(rm *)</c> or <c>PowerShell(Remove-Item *)</c>, as opposed
    /// to a whole tool (<c>Bash</c>, <c>Bash(*)</c>, <c>Bash(* --help)</c>) or a file rule (<c>Edit(adventures/**)</c>).
    /// </summary>
    static bool NamesACommand(string rule) =>
        Rule().Match(rule) is { Success: true } m && m.Groups[1].Value is "Bash" or "PowerShell"
        && m.Groups[2].Value.Length > 0 && (char.IsLetter(m.Groups[2].Value[0]) || m.Groups[2].Value[0] == '.');

    [TestMethod]
    public void OnlyARuleNamingACommandEscapesTheToolGuard()
    {
        foreach (string rule in new[] { "Bash(rm *)", "PowerShell(Remove-Item *)", "Bash(git clean *)", "Bash(find * -delete*)", "PowerShell(mv *)" })
            Assert.IsTrue(NamesACommand(rule), rule);
        foreach (string rule in new[] { "Bash", "Bash(*)", "PowerShell(*)", "Bash(* --help)", "Edit(adventures/**)", "Read(**)", "Write(*)", "mcp__*", "Skill(rewind-campaign)" })
            Assert.IsFalse(NamesACommand(rule), rule);
    }

    /// <summary>
    /// The shell deletes and moves play must not run (issue #36), as <c>Tool(pattern)</c> deny rules. <c>Remove-Item</c>,
    /// <c>Move-Item</c> and their aliases are listed one by one so a rule does not depend on how Claude Code canonicalizes
    /// an alias. <c>xargs * rm*</c> covers <c>xargs</c> with flags, which Claude Code does not strip before matching
    /// (a bare <c>xargs rm</c> is matched as <c>rm</c>).
    /// </summary>
    static readonly string[] ShellDeleteRules =
    [
        "Bash(rm *)", "Bash(rmdir *)", "Bash(unlink *)", "Bash(mv *)", "Bash(git clean *)",
        "Bash(find * -delete*)", "Bash(xargs rm*)", "Bash(xargs * rm*)",
        "PowerShell(Remove-Item *)", "PowerShell(rm *)", "PowerShell(del *)", "PowerShell(erase *)",
        "PowerShell(rd *)", "PowerShell(ri *)", "PowerShell(rmdir *)",
        "PowerShell(Move-Item *)", "PowerShell(mi *)", "PowerShell(move *)", "PowerShell(mv *)", "PowerShell(git clean *)",
    ];

    [TestMethod]
    public void ThePlaySettingsDenyShellDeletes()
    {
        string[] denied = Settings("play-settings.json")["permissions"]!["deny"]!.AsArray().Select(r => (string)r!).ToArray();
        foreach (string rule in ShellDeleteRules)
            CollectionAssert.Contains(denied, rule, $"play-settings.json does not deny {rule}");

        foreach (string command in new[]
                 {
                     "rm a.md", "rm -rf adventures/x", "rm", "rmdir adventures/x", "unlink a.md", "mv a.md b.md", "git clean -fd",
                     "find adventures -name x -delete", "find . -delete", "xargs rm", "xargs rm -f", "xargs -n1 rm a", "xargs -0 -n1 rm -rf",
                 })
            Assert.IsTrue(Denied("Bash", command), $"Bash: {command}");
        foreach (string command in new[]
                 {
                     "Remove-Item a.md", "remove-item -Recurse -Force adventures/x", "Remove-Item", "rm a.md", "del a.md", "erase a.md",
                     "rd adventures/x", "ri a.md", "rmdir adventures/x", "RI a.md", "Move-Item a.md b.md", "mi a b", "move a b", "mv a b", "git clean -fd",
                 })
            Assert.IsTrue(Denied("PowerShell", command), $"PowerShell: {command}");

        // What play does run is not caught: reading, listing, dmc, and commands that only start with the same letters.
        foreach (string command in new[]
                 {
                     "ls adventures", "cat a.md", "./bin/dmc roll 1d20", "rmx a.md", "rmdirs a", "mvx a", "git status",
                     "find adventures -name x", "xargs grep x",
                 })
            Assert.IsFalse(Denied("Bash", command), $"Bash: {command}");
        foreach (string command in new[] { "Get-ChildItem adventures", "./bin/dmc roll 1d20", "Remove-Itemx a", "ri2 a", "Move-Itemx a", "git status" })
            Assert.IsFalse(Denied("PowerShell", command), $"PowerShell: {command}");

        // An inner * is a wildcard even when the pattern ends in " *": "find * -delete *" does not take a bare "find x -delete".
        Assert.IsTrue(PatternRegex("Bash", "find * -delete *").IsMatch("find x -delete y"));
        Assert.IsFalse(PatternRegex("Bash", "find * -delete *").IsMatch("find x -delete"));
        Assert.IsTrue(PatternRegex("Bash", "rm *").IsMatch("rm"));

        // Toolkit work is not blocked: none of these rules is in settings.json, which applies to it as well.
        string[] shared = Settings("settings.json")["permissions"]?["deny"]?.AsArray().Select(r => (string)r!).ToArray() ?? [];
        foreach (string rule in ShellDeleteRules)
            CollectionAssert.DoesNotContain(shared, rule, $"settings.json applies to toolkit work and must not deny {rule}");
    }

    // ---------- the launchers and shell scripts ----------

    [TestMethod]
    public void TheLaunchersFetchDmcThenStartPlayWithThePlaySettings()
    {
        CollectionAssert.AreEqual(new[] { "--model", "sonnet", "--effort", "high", "--settings", ".claude/play-settings.json" },
            PlayCommand.FixedArguments.ToArray());
        foreach (var (name, getDmc, play, passOn) in new[]
                 {
                     ("play.ps1", "& ./tools/get-dmc.ps1", "& $dmc play @args", "@args"),
                     ("play.sh", "bash tools/get-dmc.sh", "exec ./bin/dmc play \"$@\"", "\"$@\""),
                 })
        {
            string text = Read(name);
            int fetch = text.IndexOf(getDmc, StringComparison.Ordinal), handOver = text.IndexOf(play, StringComparison.Ordinal);
            Assert.IsTrue(fetch >= 0 && handOver > fetch, $"{name} runs get-dmc, then hands over to dmc play with the player's arguments");
            // When dmc cannot be had, Claude Code still starts, the same way.
            StringAssert.Contains(text, "claude --model sonnet --effort high --settings .claude/play-settings.json " + passOn, name);
        }
    }

    [TestMethod]
    public void ShellScriptsHaveUnixLineEndingsAndAreExecutable()
    {
        string[] scripts = ["play.sh", "tools/get-dmc.sh", ".github/release/write-pin.sh"];
        foreach (string script in scripts)
            Assert.DoesNotContain((byte)'\r', File.ReadAllBytes(Path.Combine(TestEnv.Root, script)), $"{script} has Windows line endings");
        StringAssert.Contains(Read(".gitattributes"), "*.sh text eol=lf");

        var git = GitLsFiles(["-s", .. scripts]);
        foreach (string script in scripts)
            Assert.IsTrue(git.Any(l => l.StartsWith("100755 ", StringComparison.Ordinal) && l.EndsWith("\t" + script, StringComparison.Ordinal)),
                $"{script} is not executable in git:\n{string.Join('\n', git)}");
    }

    /// <summary>Only the <c>.gitkeep</c> of each personal folder (<c>adventures/</c>, <c>feedback/</c>, <c>modules/</c>, <c>rules/</c>), and the <c>.ignore</c> that makes <c>feedback/</c> searchable, are tracked, and nothing under <c>bin/</c>.</summary>
    [TestMethod]
    public void PersonalFilesStayOutOfGit() =>
        CollectionAssert.AreEqual(new[] { "adventures/.gitkeep", "feedback/.gitkeep", "feedback/.ignore", "modules/.gitkeep", "rules/.gitkeep" },
            GitLsFiles(["adventures", "feedback", "modules", "rules", "bin"]).Order(StringComparer.Ordinal).ToArray());

    /// <summary>The files git tracks under these paths (<c>git ls-files</c>, from the repository root). The test is skipped, or fails on CI, outside a git checkout.</summary>
    static string[] GitLsFiles(string[] args)
    {
        TestEnv.Need(Directory.Exists(Path.Combine(TestEnv.Root, ".git")) || File.Exists(Path.Combine(TestEnv.Root, ".git")), "a git checkout");
        var result = TestEnv.Run("git", ["ls-files", .. args]);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        return result.StdoutLines;
    }

    // ---------- the skills and the prompts ----------

    [TestMethod]
    public void EverySkillHasANameAndADescription()
    {
        Assert.IsTrue(SkillNames().Length > 0);
        foreach (string name in SkillNames())
        {
            var match = Regex.Match(Read($".claude/skills/{name}/SKILL.md"), @"^---\n(.*?)\n---\n(.*)$", RegexOptions.Singleline);
            Assert.IsTrue(match.Success, $"{name}: SKILL.md must start with a --- front matter block");
            var fields = match.Groups[1].Value.Split('\n').Where(l => l.Contains(':'))
                .ToDictionary(l => l[..l.IndexOf(':')], l => l[(l.IndexOf(':') + 1)..].Trim());
            Assert.AreEqual(name, fields.GetValueOrDefault("name"), $"{name}: the name must match the folder");
            Assert.IsFalse(string.IsNullOrEmpty(fields.GetValueOrDefault("description")), $"{name}: the description is empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(match.Groups[2].Value), $"{name}: the skill has no instructions");
        }
    }

    [TestMethod]
    public void TheReadmeListsEverySkillAndThePromptsNameOnlyRealOnes()
    {
        string readme = Read("README.md");
        foreach (string name in SkillNames())
            StringAssert.Contains(readme, $"`/{name}`", $"README.md does not mention /{name}");
        // Only names that follow the toolkit's own pattern; /compact and /clear are Claude Code's.
        foreach (string file in PromptFiles())
        {
            foreach (Match m in Regex.Matches(Read(file), @"`/([a-z]+(?:-[a-z]+)+)`"))
                Assert.Contains(m.Groups[1].Value, SkillNames(), $"{file} mentions /{m.Groups[1].Value}, which is not a skill");
        }
    }

    /// <summary>
    /// The feedback entry template has the frontmatter keys and every section heading that <c>record-feedback</c> names,
    /// so that the skill and the template cannot drift apart.
    /// </summary>
    [TestMethod]
    public void TheFeedbackTemplateHasWhatRecordFeedbackFillsIn()
    {
        string template = Read("templates/feedback.md");
        var front = Regex.Match(template, @"^---\n(.*?)\n---\n", RegexOptions.Singleline);
        Assert.IsTrue(front.Success, "templates/feedback.md must start with a --- front matter block");
        foreach (string key in new[] { "status", "category", "source", "recorded" })
            Assert.IsTrue(Regex.IsMatch(front.Groups[1].Value, $@"^{key}:", RegexOptions.Multiline), $"templates/feedback.md has no `{key}:` line");

        string skill = Read(".claude/skills/record-feedback/SKILL.md");
        var headings = Regex.Matches(template, @"^## (.+)$", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();
        CollectionAssert.AreEqual(new[] { "What happened", "What the player said", "What should happen instead", "Occurrences", "Done in play", "Context", "Worker notes" }, headings);
        // The sections record-feedback tells DMC to fill in.
        foreach (string heading in new[] { "What happened", "What the player said", "Occurrences", "Done in play", "Context" })
            StringAssert.Contains(skill, heading, $"record-feedback does not mention the section '{heading}'");
        StringAssert.Contains(skill, "templates/feedback.md");
    }

    /// <summary>
    /// <c>feedback/.ignore</c> un-ignores the entries, because the Grep tool (ripgrep) skips git-ignored files and
    /// <c>record-feedback</c> and <c>review-feedback</c> search <c>feedback/</c> with it.
    /// </summary>
    [TestMethod]
    public void FeedbackEntriesCanBeSearched()
    {
        Assert.AreEqual("!*", Read("feedback/.ignore").Trim(), "feedback/.ignore must re-include every entry for ripgrep");
        StringAssert.Contains(Read(".gitignore"), "!feedback/.ignore");
        StringAssert.Contains(Read(".gitattributes"), "feedback/.ignore text eol=lf");
    }

    /// <summary>Resume and full save never load the feedback folder: play pays nothing in context for feedback once it is written.</summary>
    [TestMethod]
    public void ResumeAndFullSaveDoNotTouchFeedback()
    {
        foreach (string skill in new[] { "resume-campaign", "campaign-full-save" })
            Assert.DoesNotContain("feedback/", Read($".claude/skills/{skill}/SKILL.md"), $"{skill} must not load feedback/");
    }

    /// <summary>Tuning DMC happens in one place: only <c>review-feedback</c> files issues or opens PRs; <c>review-transcript</c> only records.</summary>
    [TestMethod]
    public void OnlyReviewFeedbackTunesDmc()
    {
        string transcript = Read(".claude/skills/review-transcript/SKILL.md");
        StringAssert.Contains(transcript, "templates/feedback.md");
        StringAssert.Contains(transcript, "review-feedback");
        foreach (string name in SkillNames().Where(n => n != "review-feedback"))
        {
            string text = Read($".claude/skills/{name}/SKILL.md");
            Assert.IsFalse(Regex.IsMatch(text, @"\bgh (issue|pr)\b"), $"{name} mentions gh issue or gh pr; only review-feedback acts on feedback");
        }
    }

    /// <summary>
    /// Every path the prompts name under <c>templates/</c>, <c>tools/</c>, <c>.claude/skills/</c> or <c>.docs/</c> exists,
    /// so DMC is never sent to a file that is not there. A bare folder (<c>.docs/</c>) counts as a path too.
    /// <c>.docs/</c> is included because CLAUDE.md's documentation rule sends every change to the product documents
    /// there, by name (<c>.docs/roadmap.md</c>, <c>.docs/decisions.md</c>).
    /// </summary>
    [TestMethod]
    public void PathsNamedInThePromptsExist()
    {
        foreach (string file in PromptFiles())
        {
            foreach (Match m in Regex.Matches(Read(file), @"(?<![\w/.-])(?:templates|tools|\.claude/skills|\.docs)/[A-Za-z0-9_./-]*"))
            {
                string named = m.Value.TrimEnd('.', '/');
                Assert.IsTrue(Path.Exists(Path.Combine(TestEnv.Root, named)), $"{file} points at {named}, which does not exist");
            }
        }
    }

    [TestMethod]
    public void ANewCampaignGetsEveryFileClaudeMdDescribesAndNoPromptIsEmpty()
    {
        string campaign = Path.Combine(TestEnv.Root, "templates", "campaign");
        foreach (string name in new[] { "player-options.md", "game-state.md", "story/canon.md", "story/summary.md", "story/dm-notes.md", "character-sheets/minor-npcs.md" })
            Assert.IsTrue(File.Exists(Path.Combine(campaign, name)), $"templates/campaign/{name} is missing");
        foreach (string folder in new[] { "maps", "story/sessions" })
            Assert.IsTrue(Directory.Exists(Path.Combine(campaign, folder)), $"templates/campaign/{folder}/ is missing");
        foreach (string file in PromptFiles())
            Assert.IsFalse(string.IsNullOrWhiteSpace(Read(file)), $"{file} is empty");
    }

    /// <summary>
    /// The Python scripts and the two dice rollers are gone (Phase 6 of #3). Nothing DMC reads as instructions or
    /// settings, and nothing that starts or tests play, may still send it to them.
    /// </summary>
    [TestMethod]
    public void NothingStillPointsAtTheOldScripts()
    {
        string[] files = [.. PromptFiles(), ".claude/settings.json", ".claude/play-settings.json", "play.ps1", "play.sh",
                          ".gitignore", ".github/workflows/tests.yml", ".github/workflows/release.yml"];
        // The deleted files, and the library and installer they needed. The one mention allowed is the .gitignore rule for
        // the scripts/.pylib/ folder an older checkout may still hold.
        var old = new Regex(@"map_render|pdf_extract|transcript_digest|context_check\.py|roll\.ps1|roll\.sh|requirements\.txt|test_\w+\.py|pymupdf|\.pylib|pip install",
            RegexOptions.IgnoreCase);
        foreach (string file in files)
        {
            string text = file == ".gitignore" ? Read(file).Replace("\nscripts/.pylib/\n", "\n") : Read(file);
            var hit = old.Match(text);
            Assert.IsFalse(hit.Success, $"{file} still names {hit.Value}");
        }
    }
}
