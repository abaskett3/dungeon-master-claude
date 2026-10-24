using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Commands;
using Dmc.Digest;
using Dmc.Kit;
using Dmc.Tests.Distribution;
using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The toolkit as a whole: <c>kit/</c>, the source of everything <c>dmc</c> writes into a DMC folder (the rules, the settings,
/// the player skills, the templates), the developer files that stay in the repository, and the files the prompts point at.
/// These are the checks of the deleted <c>tests/test_toolkit.py</c>, rewritten for <c>dmc</c> and then moved from the
/// repository's own <c>CLAUDE.md</c>, <c>.claude/</c> and <c>templates/</c> to <c>kit/</c>: every command the prompts tell DMC
/// to run is a real subcommand and needs no permission prompt, the hooks work with and without <c>dmc</c>, the generated files
/// cannot land in the player's folders, and nothing still points at the old scripts and launchers.
/// </summary>
[TestClass]
public sealed partial class ToolkitTests
{
    /// <summary>The play settings in the kit, which a DMC folder gets as <c>.claude/settings.json</c>.</summary>
    const string KitSettings = "kit/settings.json";

    /// <summary>The developer's own settings in the repository.</summary>
    const string DevSettings = ".claude/settings.json";

    /// <summary>A file of the repository (path relative to the root) as text, with LF line endings.</summary>
    static string Read(string path) => File.ReadAllText(Path.Combine(TestEnv.Root, path)).Replace("\r\n", "\n");

    /// <summary>A settings file of the repository, parsed.</summary>
    static JsonNode Settings(string path) => JsonNode.Parse(Read(path))!;

    /// <summary>The names of the player skills: the folders under <c>kit/skills</c>, in ordinal order.</summary>
    static string[] SkillNames() =>
        Directory.GetDirectories(Path.Combine(TestEnv.Root, "kit", "skills")).Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// The folder names under <c>.claude/skills</c>, in ordinal order: the developer skills. The <c>speckit-*</c> skills are
    /// installed by Spec Kit (#51), not written here, so they are left out.
    /// </summary>
    static string[] DeveloperSkillNames() =>
        Directory.GetDirectories(Path.Combine(TestEnv.Root, ".claude", "skills")).Select(d => Path.GetFileName(d)!)
            .Where(n => !n.StartsWith("speckit-", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();

    /// <summary>Every Markdown file under a folder of the repository, relative to the root, with forward slashes, in ordinal order.</summary>
    static string[] MarkdownUnder(string folder) =>
        Directory.EnumerateFiles(Path.Combine(TestEnv.Root, folder), "*.md", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(TestEnv.Root, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();

    /// <summary>The prompts a DMC folder gets, as files of <c>kit/</c>: the rules, the skills and the templates. DMC reads these in play.</summary>
    static string[] KitPrompts() => ["kit/dm-rules.md", .. MarkdownUnder("kit/skills"), .. MarkdownUnder("kit/templates")];

    /// <summary>The prompts that stay in the repository: the developer <c>CLAUDE.md</c>, the README and the developer skills (not Spec Kit's).</summary>
    static string[] DeveloperPrompts() =>
        ["CLAUDE.md", "README.md", .. MarkdownUnder(".claude/skills").Where(f => !f.StartsWith(".claude/skills/speckit-", StringComparison.Ordinal))];

    /// <summary>Every prompt, of the kit and of the repository.</summary>
    static string[] PromptFiles() => [.. KitPrompts(), .. DeveloperPrompts()];

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

    /// <summary>The rules of the allow list in the kit's settings, which a player's session has.</summary>
    static readonly (string Tool, string Pattern, Regex Matches)[] AllowRules = Rules(KitSettings, "allow");

    /// <summary>The rules of the allow list in the developer's settings.</summary>
    static readonly (string Tool, string Pattern, Regex Matches)[] DevAllowRules = Rules(DevSettings, "allow");

    /// <summary>The rules of the deny list in the kit's settings.</summary>
    static readonly (string Tool, string Pattern, Regex Matches)[] DenyRules = Rules(KitSettings, "deny");

    /// <summary>A permission rule such as <c>Bash(dmc roll *)</c>: group 1 is the tool, group 2 the pattern.</summary>
    [GeneratedRegex(@"^(\w+)\((.*)\)$")]
    private static partial Regex Rule();

    /// <summary>Whether an allow rule of a list takes the whole command.</summary>
    static bool Allowed(IEnumerable<(string Tool, string Pattern, Regex Matches)> rules, string tool, string command) =>
        rules.Any(r => r.Tool == tool && r.Matches.IsMatch(command));

    /// <summary>Whether a deny rule of the kit's settings takes the whole command (one subcommand: Claude Code splits chains itself).</summary>
    static bool Denied(string tool, string command) => DenyRules.Any(r => r.Tool == tool && r.Matches.IsMatch(command));

    /// <summary>
    /// A command the prompts tell DMC to run: <c>dmc</c> and a word, at the start of a line (a code block) or right after a
    /// backtick (a code span), up to the end of the code span or line. A bare "dmc" in a sentence is not one.
    /// </summary>
    [GeneratedRegex(@"(?:(?<=`)|^)dmc [a-z][^`\n]*", RegexOptions.Multiline)]
    private static partial Regex DmcCommand();

    /// <summary>A subcommand, as in <c>`dmc map`</c> or a line <c>dmc pdf info &lt;pdf&gt;</c>, and the word after it, if that is a plain word.</summary>
    [GeneratedRegex(@"(?:(?<=`)|^)dmc ([a-z][a-z-]*)(?: ([a-z][a-z-]*))?", RegexOptions.Multiline)]
    private static partial Regex Subcommand();

    /// <summary>The subcommands of the commands that have their own, taken from the parsers.</summary>
    static readonly Dictionary<string, string[]> Choices = new()
    {
        ["campaign"] = CampaignArguments.Commands,
        ["digest"] = DigestArguments.Commands,
        ["pdf"] = PdfCommand.Commands,
    };

    /// <summary>
    /// The commands a player runs in a terminal, never DMC in play: the prompts may name them for the player, the kit's settings
    /// deny them to DMC, and the program refuses them inside a Claude Code session. <c>update</c> arrives with the next change
    /// (PR 2 of #67), so it is accepted as a name before the command exists.
    /// </summary>
    static readonly string[] PlayerCommands = ["install", "update", "refresh"];

    // ---------- the settings ----------

    [TestMethod]
    public void EveryDmcCommandInThePromptsIsARealSubcommandAndTheOnesDmcRunsNeedNoPermissionPrompt()
    {
        var known = CommandTable.All.Select(c => c.Name).Append("update").ToHashSet();
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
        }
        CollectionAssert.IsSubsetOf(new[] { "roll", "map", "pdf", "digest", "folder" }, used.ToArray(), "the prompts name every subcommand DMC runs");

        // What DMC is told to run in play needs no permission prompt in either shell, and is not one a player-only deny rule takes.
        foreach (string file in KitPrompts())
        {
            foreach (string command in DmcCommand().Matches(Read(file)).Select(m => m.Value.TrimEnd()))
            {
                string name = command.Split(' ')[1];
                if (PlayerCommands.Contains(name))
                {
                    Assert.IsTrue(Denied("Bash", command) && Denied("PowerShell", command), $"{file}: `{command}` is for the player, so DMC must be denied it");
                    continue;
                }
                // The same words run in either shell, so both must be allowed.
                Assert.IsTrue(Allowed(AllowRules, "Bash", command), $"{file}: `{command}` is not allowed for Bash in kit/settings.json");
                Assert.IsTrue(Allowed(AllowRules, "PowerShell", command), $"{file}: `{command}` is not allowed for PowerShell in kit/settings.json");
                Assert.IsFalse(Denied("Bash", command) || Denied("PowerShell", command), $"{file}: `{command}` is denied in kit/settings.json");
            }
        }
        // The developer skills run dmc from the developer's own session.
        foreach (string file in MarkdownUnder(".claude/skills").Where(f => !f.StartsWith(".claude/skills/speckit-", StringComparison.Ordinal)))
        {
            foreach (string command in DmcCommand().Matches(Read(file)).Select(m => m.Value.TrimEnd()).Where(c => !PlayerCommands.Contains(c.Split(' ')[1])))
            {
                Assert.IsTrue(Allowed(DevAllowRules, "Bash", command), $"{file}: `{command}` is not allowed for Bash in .claude/settings.json");
                Assert.IsTrue(Allowed(DevAllowRules, "PowerShell", command), $"{file}: `{command}` is not allowed for PowerShell in .claude/settings.json");
            }
        }

        // The rules for dmc itself, in both shells, and the way to get a missing dmc back: the player reinstalls it.
        foreach (string command in new[] { "dmc roll 1d20", "dmc map x.json --png", "dmc pdf info a.pdf", "dmc --version" })
        {
            Assert.IsTrue(Allowed(AllowRules, "Bash", command), command);
            Assert.IsTrue(Allowed(AllowRules, "PowerShell", command), command);
        }
        string rules = Read("kit/dm-rules.md");
        StringAssert.Contains(rules, "tell the player to run the install line from the DMC README again, or `dmc update` in a terminal");
    }

    [TestMethod]
    public void TheAllowListsNameOnlyDmcAndTheCampaignFolders()
    {
        foreach (var (tool, pattern, _) in AllowRules.Concat(DevAllowRules).Where(r => r.Tool is "Bash" or "PowerShell"))
            Assert.IsTrue(pattern.StartsWith("dmc ", StringComparison.Ordinal) || pattern == "dmc", $"{tool}({pattern}) is not dmc");
        CollectionAssert.AreEquivalent(new[] { "Edit(adventures/**)", "Edit(feedback/**)", "Edit(modules/**)", "Edit(rules/**)", "Bash(dmc *)", "PowerShell(dmc *)" },
            Settings(KitSettings)["permissions"]!["allow"]!.AsArray().Select(r => (string)r!).ToArray());
    }

    /// <summary>The kit's settings allow editing <c>feedback/</c>, so <c>dmc-record-feedback</c> raises no permission prompt in play.</summary>
    [TestMethod]
    public void RecordingFeedbackInPlayRaisesNoPermissionPrompt()
    {
        string[] allowed = Settings(KitSettings)["permissions"]!["allow"]!.AsArray().Select(r => (string)r!).ToArray();
        CollectionAssert.Contains(allowed, "Edit(feedback/**)", "kit/settings.json must allow editing feedback/, where dmc-record-feedback writes");
    }

    /// <summary>
    /// The campaign folders are allowed relative to the current folder, never with a leading <c>/</c>: in project settings that
    /// would mean the settings file's folder only by a rule of Claude Code that the generated-file denies rely on instead
    /// (research R6), and the allow rules are the ones that must keep working from a subfolder.
    /// </summary>
    [TestMethod]
    public void CampaignAllowRulesDoNotStartWithASlash()
    {
        foreach (var (tool, pattern, _) in AllowRules.Where(r => r.Tool is "Edit" or "Write" or "Read"))
            Assert.IsFalse(pattern.StartsWith('/'), $"{tool}({pattern}) starts with /");
    }

    /// <summary>
    /// A player's session may not edit or write the generated files, and may not run <c>dmc update</c>, <c>install</c> or
    /// <c>refresh</c> (FR-005, FR-016): deny rules anchored at the project folder with <c>/</c>, and the three commands in both shells.
    /// </summary>
    [TestMethod]
    public void TheKitSettingsProtectTheGeneratedFiles()
    {
        string[] denied = Settings(KitSettings)["permissions"]!["deny"]!.AsArray().Select(r => (string)r!).ToArray();
        foreach (string tool in new[] { "Edit", "Write" })
        {
            foreach (string path in new[] { "/CLAUDE.md", "/.claude/**", "/templates/**", "/.dmc/**" })
                CollectionAssert.Contains(denied, $"{tool}({path})");
        }
        foreach (string tool in new[] { "Bash", "PowerShell" })
        {
            foreach (string command in new[] { "update", "install", "refresh" })
            {
                CollectionAssert.Contains(denied, $"{tool}(dmc {command}*)");
                Assert.IsTrue(Denied(tool, $"dmc {command}"), $"{tool}: dmc {command}");
                Assert.IsTrue(Denied(tool, $"dmc {command} --all"), $"{tool}: dmc {command} --all");
            }
        }
        Assert.IsFalse(denied.Any(r => r.Contains("speckit", StringComparison.OrdinalIgnoreCase)), "a DMC folder has no Spec Kit to hide");
        Assert.IsNull(Settings(KitSettings)["enabledPlugins"], "the maintainer's own plugin setting is meaningless to players");
    }

    [TestMethod]
    public void HooksAreCommands()
    {
        foreach (string name in new[] { KitSettings, DevSettings })
        {
            foreach (var (eventName, groups) in Settings(name)["hooks"]?.AsObject() ?? [])
            {
                foreach (var hook in groups!.AsArray().SelectMany(g => g!["hooks"]!.AsArray()))
                {
                    Assert.AreEqual("command", (string?)hook!["type"], $"{name} {eventName}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace((string?)hook["command"]), $"{name} {eventName}");
                }
            }
        }
        Assert.IsNull(Settings(DevSettings)["hooks"], "development needs no hook: the play hooks are in kit/settings.json");
    }

    /// <summary>
    /// Run a hook's command the way Claude Code does, with bash, in a fresh DMC-like folder, with <c>dmc</c> found by name on the
    /// <c>PATH</c>. The <c>PATH</c> never holds a real <c>dmc</c> but the one the test asks for: the published program
    /// (<see cref="DmcRunner.Binary"/>, so <c>DMC_BIN</c> is honoured), a file that is not a program, or none.
    /// </summary>
    static ProcessResult RunHook(string command, BinDmc dmc, Func<string, string>? stdin = null, Action<string>? seed = null)
    {
        string project = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(project);
        try
        {
            string bin = Path.Combine(project, "bin");
            string? first = null;
            if (dmc == BinDmc.Published)
                first = Path.GetDirectoryName(DmcRunner.Binary())!;
            if (dmc == BinDmc.Junk)
            {
                Directory.CreateDirectory(bin);
                string exe = Path.Combine(bin, Bootstrap.ExeName);
                File.WriteAllText(exe, "not a program\n");
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                first = bin;
            }
            string[] others = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !HoldsDmc(p)).ToArray();
            string path = string.Join(Path.PathSeparator, first is null ? others : others.Prepend(first));
            seed?.Invoke(project);
            return TestEnv.Run(Bootstrap.Shell("bash"), ["-c", command], stdin?.Invoke(project), cwd: project,
                environment: new Dictionary<string, string?> { ["CLAUDE_PROJECT_DIR"] = project, ["PATH"] = path });
        }
        finally
        {
            Directory.Delete(project, recursive: true);
        }
    }

    /// <summary>Whether a <c>PATH</c> folder holds a program called <c>dmc</c>, which a hook test must not find by accident (a developer may have installed one).</summary>
    static bool HoldsDmc(string folder)
    {
        try
        {
            return File.Exists(Path.Combine(folder.Trim('"'), "dmc")) || File.Exists(Path.Combine(folder.Trim('"'), "dmc.exe"));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>What <c>dmc</c> is on the <c>PATH</c> in a test of a hook.</summary>
    enum BinDmc
    {
        /// <summary>There is none.</summary>
        Missing,

        /// <summary>A file that is not a program.</summary>
        Junk,

        /// <summary>The published binary under test, which runs.</summary>
        Published,
    }

    [TestMethod]
    public void WithoutAWorkingDmcASessionIsToldOnceToReinstall()
    {
        string[] commands = Settings(KitSettings)["hooks"]!["SessionStart"]!.AsArray()
            .Where(g => g!["matcher"] is null).SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!)
            .Where(c => c.Contains("--version", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, commands, "one SessionStart hook, for every kind of start, checks that dmc runs");

        foreach (var dmc in new[] { BinDmc.Missing, BinDmc.Junk })
        {
            var told = RunHook(commands[0], dmc);
            Assert.AreEqual(0, told.ExitCode, told.ToString());
            Assert.HasCount(1, told.StdoutLines, $"{dmc}: {told}");
            StringAssert.Contains(told.Stdout, "dmc is missing or does not run, so dice, maps and PDF reading will not work: reinstall DMC with the install line in its README");
            Assert.AreEqual("", told.Stderr, dmc.ToString());
        }
        var working = RunHook(commands[0], BinDmc.Published);
        Assert.AreEqual(0, working.ExitCode, working.ToString());
        Assert.AreEqual("", working.Stdout + working.Stderr, "silent when dmc runs");
    }

    [TestMethod]
    public void TheCompactMessageNamesTheResumeSkill()
    {
        string[] commands = Settings(KitSettings)["hooks"]!["SessionStart"]!.AsArray()
            .Where(g => (string?)g!["matcher"] == "compact").SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!).ToArray();
        Assert.HasCount(1, commands);
        StringAssert.Contains(commands[0], "dmc-resume-campaign");
        Assert.Contains("dmc-resume-campaign", SkillNames());
    }

    [TestMethod]
    public void PlayRunsTheBreakReminderBeforeEachMessageAndItNeverGetsInTheWay()
    {
        string[] commands = (Settings(KitSettings)["hooks"]?["UserPromptSubmit"]?.AsArray() ?? [])
            .SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!).ToArray();
        Assert.HasCount(1, commands, string.Join("\n", commands));
        string hook = commands[0];
        StringAssert.StartsWith(hook, "dmc context-check");
        // Exit code 2 from a UserPromptSubmit hook blocks the prompt; a shell that cannot find dmc exits 127.
        Assert.IsTrue(hook.TrimEnd().EndsWith("|| true", StringComparison.Ordinal), hook);

        // Without dmc: no output at all, and exit 0.
        var missing = RunHook(hook, BinDmc.Missing, _ => "{}");
        Assert.AreEqual(0, missing.ExitCode, missing.ToString());
        Assert.AreEqual("", missing.Stdout + missing.Stderr, "a missing dmc must not show in every prompt");

        // With the published dmc: the reminder, from a log past the threshold.
        var due = RunHook(hook, BinDmc.Published, project => new JsonObject { ["transcript_path"] = Path.Combine(project, "session.jsonl") }.ToJsonString(),
            project => File.WriteAllText(Path.Combine(project, "session.jsonl"), ContextCheck.ContextCheckContract.Assistant(31000).ToJsonString() + "\n"
                + ContextCheck.ContextCheckContract.User("go on").ToJsonString() + "\n"
                + ContextCheck.ContextCheckContract.Assistant(163400).ToJsonString() + "\n"));
        Assert.AreEqual(0, due.ExitCode, due.ToString());
        StringAssert.StartsWith(due.Stdout, "Context 163k is large, break due since 150k", due.ToString());
    }

    /// <summary>The Stop hook's command in the kit's settings; fails unless there is exactly one.</summary>
    static string StopHook()
    {
        string[] commands = (Settings(KitSettings)["hooks"]?["Stop"]?.AsArray() ?? [])
            .SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!).ToArray();
        Assert.HasCount(1, commands, "kit/settings.json needs one Stop hook: " + string.Join("\n", commands));
        return commands[0];
    }

    /// <summary>
    /// Seeds a folder for a Stop-hook test: an active campaign <c>test</c> whose <c>maps/live.json</c> holds
    /// <paramref name="title"/>, drawn once with the published <c>dmc</c> when <paramref name="drawn"/> is true, as
    /// <c>dmc map</c> does when DMC first shows a map. The folder is a DMC folder, as the hook's project folder is.
    /// </summary>
    static Action<string> Campaign(string title, bool drawn = true) => project =>
    {
        DmcFolders.MakeDmcFolder(project);
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
        StringAssert.Contains(hook, "dmc redraw");

        // Nothing changed, and nothing to draw (no page and no live.json; with a live.json and no page the hook would draw and open it): no output, exit 0.
        Action<string> noMap = project =>
        {
            Campaign("none", drawn: false)(project);
            File.Delete(Path.Combine(project, "adventures", "test", "maps", "live.json"));
        };
        foreach (var seed in new Action<string>?[] { Campaign("same"), project => DmcFolders.MakeDmcFolder(project), noMap })
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
        // A 0.1.2 dmc exits 1 with "unknown command"; no program or one that is not a program exits 126 or 127. None may block or print.
        Action<string> oldDmc = project =>
        {
            Campaign("same", drawn: false)(project);
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
    public void TheKitSettingsKeepEverythingPlayNeeds()
    {
        var settings = Settings(KitSettings);
        // Tools play, the dice and map commands, rules lookups and the module and import skills need.
        string[] needed = ["Read", "Write", "Edit", "Glob", "Grep", "Bash", "PowerShell", "Skill", "ToolSearch",
                           "AskUserQuestion", "WebSearch", "WebFetch", "Agent"];
        // Bash(rm *) removes one command, not the tool, and Edit(/CLAUDE.md) one file, not the tool; every other rule (a bare name or glob, Bash(*), Edit(**)) can take away what play needs.
        foreach (string rule in (settings["permissions"]?["deny"]?.AsArray().Select(r => (string)r!) ?? []).Where(r => !NamesACommand(r) && !NamesAFile(r)))
        {
            string toolName = Rule().Match(rule) is { Success: true } m ? m.Groups[1].Value : rule;
            string pattern = "^" + Regex.Escape(toolName).Replace(@"\*", ".*") + "$";
            foreach (string tool in needed)
                Assert.IsFalse(Regex.IsMatch(tool, pattern), $"kit/settings.json denies {tool} ({rule}), which play needs");
        }
        var overrides = settings["skillOverrides"]?.AsObject();
        foreach (string skill in SkillNames())
            Assert.AreEqual("on", (string?)overrides?[skill] ?? "on", $"kit/settings.json hides /{skill}");
        Assert.IsNotNull(settings["permissions"]?["allow"], "the DMC folder's one settings file carries the allow rules too");
    }

    /// <summary>
    /// Whether a deny rule names one specific command, <c>Bash(rm *)</c> or <c>PowerShell(Remove-Item *)</c>, as opposed
    /// to a whole tool (<c>Bash</c>, <c>Bash(*)</c>, <c>Bash(* --help)</c>) or a file rule (<c>Edit(adventures/**)</c>).
    /// </summary>
    static bool NamesACommand(string rule) =>
        Rule().Match(rule) is { Success: true } m && m.Groups[1].Value is "Bash" or "PowerShell"
        && m.Groups[2].Value.Length > 0 && (char.IsLetter(m.Groups[2].Value[0]) || m.Groups[2].Value[0] == '.');

    /// <summary>
    /// Whether a rule names specific files of one tool, <c>Edit(/CLAUDE.md)</c> or <c>Write(/templates/**)</c>: an
    /// <c>Edit</c>, <c>Write</c> or <c>Read</c> rule whose pattern is more than wildcards and slashes.
    /// </summary>
    static bool NamesAFile(string rule) =>
        Rule().Match(rule) is { Success: true } m && m.Groups[1].Value is "Edit" or "Write" or "Read"
        && m.Groups[2].Value.Trim('*', '/').Length > 0;

    [TestMethod]
    public void OnlyARuleNamingACommandOrAFileEscapesTheToolGuard()
    {
        foreach (string rule in new[] { "Bash(rm *)", "PowerShell(Remove-Item *)", "Bash(git clean *)", "Bash(find * -delete*)", "PowerShell(mv *)", "Bash(dmc update*)" })
            Assert.IsTrue(NamesACommand(rule), rule);
        foreach (string rule in new[] { "Bash", "Bash(*)", "PowerShell(*)", "Bash(* --help)", "Edit(adventures/**)", "Read(**)", "Write(*)", "mcp__*", "Skill(dmc-rewind-campaign)" })
            Assert.IsFalse(NamesACommand(rule), rule);
        foreach (string rule in new[] { "Edit(/CLAUDE.md)", "Write(/.claude/**)", "Edit(adventures/**)" })
            Assert.IsTrue(NamesAFile(rule), rule);
        foreach (string rule in new[] { "Edit", "Edit(*)", "Write(**)", "Read(**)", "Bash(/x)" })
            Assert.IsFalse(NamesAFile(rule), rule);
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
    public void TheKitSettingsDenyShellDeletes()
    {
        string[] denied = Settings(KitSettings)["permissions"]!["deny"]!.AsArray().Select(r => (string)r!).ToArray();
        foreach (string rule in ShellDeleteRules)
            CollectionAssert.Contains(denied, rule, $"kit/settings.json does not deny {rule}");

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
                     "ls adventures", "cat a.md", "dmc roll 1d20", "rmx a.md", "rmdirs a", "mvx a", "git status",
                     "find adventures -name x", "xargs grep x",
                 })
            Assert.IsFalse(Denied("Bash", command), $"Bash: {command}");
        foreach (string command in new[] { "Get-ChildItem adventures", "dmc roll 1d20", "Remove-Itemx a", "ri2 a", "Move-Itemx a", "git status" })
            Assert.IsFalse(Denied("PowerShell", command), $"PowerShell: {command}");

        // An inner * is a wildcard even when the pattern ends in " *": "find * -delete *" does not take a bare "find x -delete".
        Assert.IsTrue(PatternRegex("Bash", "find * -delete *").IsMatch("find x -delete y"));
        Assert.IsFalse(PatternRegex("Bash", "find * -delete *").IsMatch("find x -delete"));
        Assert.IsTrue(PatternRegex("Bash", "rm *").IsMatch("rm"));

        // Toolkit work is not blocked: none of these rules is in the developer's settings.
        string[] shared = Settings(DevSettings)["permissions"]?["deny"]?.AsArray().Select(r => (string)r!).ToArray() ?? [];
        foreach (string rule in ShellDeleteRules)
            CollectionAssert.DoesNotContain(shared, rule, $".claude/settings.json applies to toolkit work and must not deny {rule}");
    }

    // ---------- the shell scripts ----------

    [TestMethod]
    public void ShellScriptsHaveUnixLineEndingsAndAreExecutable()
    {
        // The release workflow still runs these until it is rewritten (PR 2 of #67).
        string[] scripts = ["tools/get-dmc.sh", ".github/release/write-pin.sh"];
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

    // ---------- kit/: what is embedded, mapped and named ----------

    /// <summary>Every file under <c>kit/</c> in the repository, as the name it is embedded under (<c>kit/...</c>, with <c>/</c>), in ordinal order.</summary>
    static string[] KitFileNames() =>
        Directory.EnumerateFiles(Path.Combine(TestEnv.Root, "kit"), "*", SearchOption.AllDirectories)
            .Select(f => "kit/" + Path.GetRelativePath(Path.Combine(TestEnv.Root, "kit"), f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();

    /// <summary>Every file under <c>kit/</c> is an embedded resource and is in the table, and the program's generated files are exactly their targets.</summary>
    [TestMethod]
    public void EveryFileUnderKitIsEmbeddedAndInTheTable()
    {
        string[] files = KitFileNames();
        Assert.IsNotEmpty(files);
        CollectionAssert.AreEqual(files, GeneratedFiles.ResourceNames(typeof(GeneratedFiles).Assembly).ToArray(),
            "the files under kit/ and the embedded resources differ: a file the glob in Dmc.Cli.csproj missed, or one that is gone");
        string[] targets = files.Select(GeneratedFiles.TargetOf).Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(targets, GeneratedFiles.All.Select(f => f.Path).ToArray());
        foreach (var file in GeneratedFiles.All)
        {
            string source = files.First(f => GeneratedFiles.TargetOf(f) == file.Path);
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(TestEnv.Root, source)), file.Bytes, $"{file.Path} is not what {source} holds");
        }
        // The four mappings of the table, by example.
        Assert.AreEqual("CLAUDE.md", GeneratedFiles.TargetOf("kit/dm-rules.md"));
        Assert.AreEqual(".claude/settings.json", GeneratedFiles.TargetOf("kit/settings.json"));
        Assert.AreEqual(".claude/skills/dmc-draw-map/map-format.md", GeneratedFiles.TargetOf("kit/skills/dmc-draw-map/map-format.md"));
        Assert.AreEqual("templates/campaign/game-state.md", GeneratedFiles.TargetOf("kit/templates/campaign/game-state.md"));
    }

    /// <summary>Nothing in the table lands under the player's folders (FR-012), and no file of <c>kit/</c> is left out of it.</summary>
    [TestMethod]
    public void NoTableTargetIsUnderAPlayerFolder()
    {
        foreach (var file in GeneratedFiles.All)
            GeneratedFiles.Guard(file.Path);
        foreach (string folder in GeneratedFiles.PlayerFolders)
            Assert.IsFalse(GeneratedFiles.All.Any(f => f.Path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)), folder);
        foreach (string name in KitFileNames())
            Assert.IsNotNull(GeneratedFiles.TargetOf(name), name);
    }

    /// <summary>The ten player skills are in <c>kit/skills</c> under <c>dmc-&lt;name&gt;</c>, and each says so in its front matter.</summary>
    [TestMethod]
    public void EveryPlayerSkillFolderAndNameIsDmcPrefixed()
    {
        CollectionAssert.AreEqual(new[]
        {
            "dmc-campaign-full-save", "dmc-character-sheet-update", "dmc-draw-map", "dmc-ingest-module", "dmc-new-campaign",
            "dmc-record-feedback", "dmc-resume-campaign", "dmc-rewind-campaign", "dmc-setup-rules", "dmc-update-game-state",
        }, SkillNames());
        foreach (string name in SkillNames())
        {
            var match = Regex.Match(Read($"kit/skills/{name}/SKILL.md"), @"^---\n(.*?)\n---\n", RegexOptions.Singleline);
            Assert.IsTrue(match.Success, name);
            StringAssert.Contains(match.Groups[1].Value, $"name: {name}\n");
        }
    }

    /// <summary>Every skill a prompt of the kit names exists: a player skill in <c>kit/skills</c>, or one of the developer skills a worker is pointed at.</summary>
    [TestMethod]
    public void EverySkillAKitPromptNamesExists()
    {
        string[] real = [.. SkillNames(), .. DeveloperSkillNames()];
        foreach (string file in KitPrompts())
        {
            foreach (Match m in Regex.Matches(Read(file), @"`/?((?:dmc|review)-[a-z]+(?:-[a-z]+)*)`"))
                Assert.Contains(m.Groups[1].Value, real, $"{file} names the skill {m.Groups[1].Value}, which does not exist");
        }
        // The old names are gone from the kit: a player skill is never named without its prefix.
        string[] old = ["campaign-full-save", "character-sheet-update", "draw-map", "ingest-module", "new-campaign", "record-feedback",
                        "resume-campaign", "rewind-campaign", "setup-rules", "update-game-state"];
        foreach (string file in KitPrompts())
        {
            string text = Read(file);
            foreach (string name in old)
                Assert.IsFalse(Regex.IsMatch(text, @"(?<![\w-])" + name + @"(?![\w-])"), $"{file} still names {name} without the dmc- prefix");
        }
    }

    /// <summary>No file of the kit names the launcher days: <c>./bin/dmc</c>, <c>get-dmc</c>, <c>play.ps1</c>, <c>play.sh</c> or a <c>tools/</c> path.</summary>
    [TestMethod]
    public void NoKitFileNamesTheOldInstall()
    {
        foreach (string name in KitFileNames())
        {
            string text = Read(name);
            foreach (string old in new[] { "./bin/dmc", "get-dmc", "play.ps1", "play.sh", "tools/" })
                Assert.DoesNotContain(old, text, $"{name} names {old}");
        }
    }

    [TestMethod]
    public void TheDeveloperClaudeMdHoldsNoDungeonMasterSection()
    {
        string claude = Read("CLAUDE.md");
        foreach (string heading in new[] { "## Dice", "## Sizing fights", "## Maps", "## Narration", "## Starting a session" })
            Assert.DoesNotContain(heading, claude, $"CLAUDE.md still has {heading}");
        StringAssert.Contains(claude, "kit/dm-rules.md");
        StringAssert.Contains(claude, "## Git and GitHub");
        Assert.AreEqual(1, Regex.Matches(claude, "^# ", RegexOptions.Multiline).Count, "one title only");
        // The rules themselves moved whole.
        string rules = Read("kit/dm-rules.md");
        foreach (string heading in new[] { "## Dice", "## Sizing fights", "## Maps", "## Narration", "## Starting a session" })
            StringAssert.Contains(rules, heading);
        Assert.DoesNotContain("## Git and GitHub", rules);
        StringAssert.Contains(rules, "offer to record it as feedback with `dmc-record-feedback`");
    }

    [TestMethod]
    public void TheRepositorysSkillsHoldNoPlayerSkill()
    {
        CollectionAssert.AreEqual(new[] { "review-feedback", "review-transcript" }, DeveloperSkillNames());
        Assert.IsFalse(Directory.Exists(Path.Combine(TestEnv.Root, "templates")), "templates/ moved into kit/");
        Assert.IsFalse(File.Exists(Path.Combine(TestEnv.Root, ".claude", "play-settings.json")), "play-settings.json moved into kit/settings.json");
    }

    // ---------- the skills and the prompts ----------

    [TestMethod]
    public void EverySkillHasANameAndADescription()
    {
        Assert.IsNotEmpty(SkillNames());
        foreach (string folder in new[] { "kit/skills", ".claude/skills" })
        {
            foreach (string name in Directory.GetDirectories(Path.Combine(TestEnv.Root, folder)).Select(d => Path.GetFileName(d)!)
                         .Where(n => !n.StartsWith("speckit-", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            {
                var match = Regex.Match(Read($"{folder}/{name}/SKILL.md"), @"^---\n(.*?)\n---\n(.*)$", RegexOptions.Singleline);
                Assert.IsTrue(match.Success, $"{name}: SKILL.md must start with a --- front matter block");
                var fields = match.Groups[1].Value.Split('\n').Where(l => l.Contains(':'))
                    .ToDictionary(l => l[..l.IndexOf(':')], l => l[(l.IndexOf(':') + 1)..].Trim());
                Assert.AreEqual(name, fields.GetValueOrDefault("name"), $"{name}: the name must match the folder");
                Assert.IsFalse(string.IsNullOrEmpty(fields.GetValueOrDefault("description")), $"{name}: the description is empty");
                Assert.IsFalse(string.IsNullOrWhiteSpace(match.Groups[2].Value), $"{name}: the skill has no instructions");
            }
        }
    }

    [TestMethod]
    public void TheReadmeListsEverySkillAndThePromptsNameOnlyRealOnes()
    {
        string readme = Read("README.md");
        foreach (string name in SkillNames())
            StringAssert.Contains(readme, $"`/{name}`", $"README.md does not mention /{name}");
        // Only names that follow the toolkit's own pattern; /compact and /clear are Claude Code's.
        string[] real = [.. SkillNames(), .. DeveloperSkillNames()];
        foreach (string file in PromptFiles())
        {
            foreach (Match m in Regex.Matches(Read(file), @"`/([a-z]+(?:-[a-z]+)+)`"))
                Assert.Contains(m.Groups[1].Value, real, $"{file} mentions /{m.Groups[1].Value}, which is not a skill");
        }
    }

    /// <summary>The <c>speckit-*</c> skill folders under <c>.claude/skills</c>, in ordinal order: Spec Kit's, not DMC's.</summary>
    static string[] SpecKitSkillNames() =>
        Directory.GetDirectories(Path.Combine(TestEnv.Root, ".claude", "skills")).Select(d => Path.GetFileName(d)!)
            .Where(n => n.StartsWith("speckit-", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Every Spec Kit skill sets <c>disable-model-invocation: true</c>, so it never adds a description to the developer's
    /// context unasked. The value may be quoted, as Spec Kit writes its own front matter (<c>name: "speckit-plan"</c>). This
    /// catches an upgrade or a preset command that silently resets the flag.
    /// </summary>
    [TestMethod]
    public void EverySpecKitSkillDisablesModelInvocation()
    {
        foreach (string name in SpecKitSkillNames())
        {
            var match = Regex.Match(Read($".claude/skills/{name}/SKILL.md"), @"^---\n(.*?)\n---\n", RegexOptions.Singleline);
            Assert.IsTrue(match.Success, $"{name}: SKILL.md must start with a --- front matter block");
            var fields = match.Groups[1].Value.Split('\n').Where(l => l.Contains(':') && !l.StartsWith(' '))
                .ToDictionary(l => l[..l.IndexOf(':')], l => l[(l.IndexOf(':') + 1)..].Trim().Trim('"', '\''));
            Assert.AreEqual("true", fields.GetValueOrDefault("disable-model-invocation"), $"{name}: must set disable-model-invocation: true");
        }
    }

    /// <summary>
    /// Spec Kit is a developer's tool and never reaches a DMC folder: it leaves no hook in the developer's settings, no
    /// <c>SPECKIT START</c> block in <c>CLAUDE.md</c>, and nothing of it in the kit.
    /// </summary>
    [TestMethod]
    public void SpecKitAddsNoHookAndNoClaudeMdBlockAndNothingInTheKit()
    {
        string settings = Read(DevSettings);
        Assert.IsFalse(settings.Contains("speckit", StringComparison.OrdinalIgnoreCase)
            || settings.Contains(".specify", StringComparison.OrdinalIgnoreCase), "settings.json must have no Spec Kit hook");
        Assert.DoesNotContain("SPECKIT START", Read("CLAUDE.md"), "CLAUDE.md must have no SPECKIT START block");
        foreach (string name in KitFileNames())
            Assert.IsFalse(Read(name).Contains("speckit", StringComparison.OrdinalIgnoreCase), $"{name} names Spec Kit");
    }

    /// <summary>
    /// The feedback entry template has the frontmatter keys and every section heading that <c>dmc-record-feedback</c> names,
    /// so that the skill and the template cannot drift apart, and the skill fills in the <c>dmc:</c> key (FR-017).
    /// </summary>
    [TestMethod]
    public void TheFeedbackTemplateHasWhatRecordFeedbackFillsIn()
    {
        string template = Read("kit/templates/feedback.md");
        var front = Regex.Match(template, @"^---\n(.*?)\n---\n", RegexOptions.Singleline);
        Assert.IsTrue(front.Success, "kit/templates/feedback.md must start with a --- front matter block");
        foreach (string key in new[] { "status", "category", "source", "recorded", "dmc" })
            Assert.IsTrue(Regex.IsMatch(front.Groups[1].Value, $@"^{key}:", RegexOptions.Multiline), $"kit/templates/feedback.md has no `{key}:` line");
        Assert.IsTrue(front.Groups[1].Value.IndexOf("recorded:", StringComparison.Ordinal) < front.Groups[1].Value.IndexOf("dmc:", StringComparison.Ordinal), "dmc: comes after recorded:");

        string skill = Read("kit/skills/dmc-record-feedback/SKILL.md");
        var headings = Regex.Matches(template, @"^## (.+)$", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();
        CollectionAssert.AreEqual(new[] { "What happened", "What the player said", "What should happen instead", "Occurrences", "Done in play", "Context", "Worker notes" }, headings);
        // The sections dmc-record-feedback tells DMC to fill in.
        foreach (string heading in new[] { "What happened", "What the player said", "Occurrences", "Done in play", "Context" })
            StringAssert.Contains(skill, heading, $"dmc-record-feedback does not mention the section '{heading}'");
        StringAssert.Contains(skill, "templates/feedback.md");
        StringAssert.Contains(skill, "`dmc:`");
        StringAssert.Contains(skill, "dmc --version");
    }

    /// <summary>The review skills read a DMC folder: they name <c>dmc folder</c> and <c>--folder</c>, and never <c>./bin/dmc</c>; the files to tune are in <c>kit/</c>.</summary>
    [TestMethod]
    public void TheReviewSkillsReadADmcFolder()
    {
        foreach (string name in new[] { "review-feedback", "review-transcript" })
        {
            string text = Read($".claude/skills/{name}/SKILL.md");
            StringAssert.Contains(text, "`dmc folder`", name);
            StringAssert.Contains(text, "--folder", name);
            StringAssert.Contains(text, "kit/", name);
            Assert.DoesNotContain("./bin/dmc", text, name);
            Assert.DoesNotContain("play-settings", text, name);
            Assert.DoesNotContain("`record-feedback`", text, name);
        }
        StringAssert.Contains(Read(".claude/skills/review-feedback/SKILL.md"), "`kit/dm-rules.md`");
    }

    /// <summary>
    /// <c>feedback/.ignore</c> un-ignores the entries, because the Grep tool (ripgrep) skips git-ignored files and
    /// <c>dmc-record-feedback</c> and <c>review-feedback</c> search <c>feedback/</c> with it. (The repository's placeholder goes in PR 3 of #67.)
    /// </summary>
    [TestMethod]
    public void FeedbackEntriesCanBeSearched()
    {
        Assert.AreEqual("!*", Read("feedback/.ignore").Trim(), "feedback/.ignore must re-include every entry for ripgrep");
        StringAssert.Contains(Read(".gitignore"), "!feedback/.ignore");
        StringAssert.Contains(Read(".gitattributes"), "feedback/.ignore text eol=lf");
    }

    /// <summary>Feedback has one route: <c>dmc-record-feedback</c> writes to <c>feedback/</c> and to nothing else, and has no stopgap copy in a campaign.</summary>
    [TestMethod]
    public void FeedbackHasOneRoute()
    {
        string skill = Read("kit/skills/dmc-record-feedback/SKILL.md");
        Assert.DoesNotContain("stopgap", skill, StringComparison.OrdinalIgnoreCase, "dmc-record-feedback must not write a second copy of feedback into a campaign");
        StringAssert.Contains(skill, "Never record such feedback by hand, or anywhere else");
        StringAssert.Contains(Read("kit/dm-rules.md"), "has one route: invoke the `dmc-record-feedback` skill");
    }

    /// <summary>The prompt fixes from the first feedback review are in the files they belong in.</summary>
    [TestMethod]
    public void TheFirstFeedbackReviewFixesAreInThePrompts()
    {
        string claude = Read("kit/dm-rules.md");
        StringAssert.Contains(claude, "Use Write only to create a new file");
        StringAssert.Contains(claude, "no tool call after it, so the player's screen ends on the reply");
        StringAssert.Contains(claude, "Never list routine upkeep");
        StringAssert.Contains(Read("kit/templates/campaign/game-state.md"), "## Reserved by the player");
        StringAssert.Contains(Read("kit/templates/campaign/player-options.md"), "**Story timeline**");
        StringAssert.Contains(Read("kit/templates/session-log.md"), "what the player said out of character stays out");
    }

    /// <summary>Every resume ends the last session and starts a new one, and the citation form is stated in the session log template.</summary>
    [TestMethod]
    public void EveryResumeStartsANewSession()
    {
        string resume = Read("kit/skills/dmc-resume-campaign/SKILL.md");
        StringAssert.Contains(resume, "Every resume ends the previous session and starts a fresh one");
        StringAssert.Contains(resume, "has no scenes yet, use it");
        Assert.DoesNotContain("keep using the current one", resume, "a resume must not continue the previous session");
        StringAssert.Contains(Read("kit/templates/session-log.md"), "`sN.M`");
    }

    /// <summary>A fight is planned with an ending, and fleeing is never an open-ended tool.</summary>
    [TestMethod]
    public void FightsArePlannedWithAnEnding()
    {
        string claude = Read("kit/dm-rules.md");
        StringAssert.Contains(claude, "**How it ends**");
        StringAssert.Contains(claude, "it does not do it");
        StringAssert.Contains(claude, "flee for good at half strength");
    }

    /// <summary>Sheets end with their history, so that resume can read a sheet down to it, and the full save does not re-read whole files.</summary>
    [TestMethod]
    public void SheetsEndWithTheirHistoryAndResumeReadsThemDownToIt()
    {
        foreach (string sheet in new[] { "kit/templates/pc-sheet.md", "kit/templates/npc-sheet.md" })
        {
            string last = Regex.Matches(Read(sheet), @"^## (.+)$", RegexOptions.Multiline).Last().Groups[1].Value;
            Assert.AreEqual("History", last, $"{sheet} must end with its History section");
        }
        StringAssert.Contains(Read("kit/skills/dmc-resume-campaign/SKILL.md"), "Read a sheet down to its history, not through it");
        string save = Read("kit/skills/dmc-campaign-full-save/SKILL.md");
        StringAssert.Contains(save, "with targeted reads");
        StringAssert.Contains(save, "never one Edit per line");
        Assert.DoesNotContain("Read every file this save changed again", save);
    }

    /// <summary>The rules for messages that are not actions, and for NPCs that must not re-ask, are in the rules.</summary>
    [TestMethod]
    public void MessagesThatAreNotActionsAreHandled()
    {
        string claude = Read("kit/dm-rules.md");
        StringAssert.Contains(claude, "A message that arrives while you are working is a new message");
        StringAssert.Contains(claude, "A joke, sarcasm or complaint is never an event in the fiction");
        StringAssert.Contains(claude, "never ask again what was answered");
    }

    /// <summary>Resume and full save never load the feedback folder: play pays nothing in context for feedback once it is written.</summary>
    [TestMethod]
    public void ResumeAndFullSaveDoNotTouchFeedback()
    {
        foreach (string skill in new[] { "dmc-resume-campaign", "dmc-campaign-full-save" })
            Assert.DoesNotContain("feedback/", Read($"kit/skills/{skill}/SKILL.md"), $"{skill} must not load feedback/");
    }

    /// <summary>Tuning DMC happens in one place: only <c>review-feedback</c> files issues or opens PRs; <c>review-transcript</c> only records, and no player skill does either.</summary>
    [TestMethod]
    public void OnlyReviewFeedbackTunesDmc()
    {
        string transcript = Read(".claude/skills/review-transcript/SKILL.md");
        StringAssert.Contains(transcript, "kit/templates/feedback.md");
        StringAssert.Contains(transcript, "review-feedback");
        foreach (string file in MarkdownUnder("kit").Concat(MarkdownUnder(".claude/skills").Where(f => !f.StartsWith(".claude/skills/speckit-", StringComparison.Ordinal)
                     && !f.StartsWith(".claude/skills/review-feedback/", StringComparison.Ordinal))))
        {
            Assert.IsFalse(Regex.IsMatch(Read(file), @"\bgh (issue|pr)\b"), $"{file} mentions gh issue or gh pr; only review-feedback acts on feedback");
        }
    }

    /// <summary>
    /// Every path the prompts name under <c>templates/</c>, <c>tools/</c>, <c>.claude/skills/</c> or <c>.docs/</c> exists, so DMC
    /// is never sent to a file that is not there. A bare folder (<c>.docs/</c>) counts as a path too. A prompt of the kit names
    /// the DMC folder's paths: <c>templates/</c> is <c>kit/templates/</c> here and <c>.claude/skills/</c> is <c>kit/skills/</c>, and
    /// it may not name <c>tools/</c> or <c>.docs/</c>, which a DMC folder does not have. The developer prompts name the repository's own.
    /// </summary>
    [TestMethod]
    public void PathsNamedInThePromptsExist()
    {
        var pattern = new Regex(@"(?<![\w/.-])(?:templates|tools|\.claude/skills|\.docs)/[A-Za-z0-9_./-]*");
        foreach (string file in KitPrompts())
        {
            foreach (Match m in pattern.Matches(Read(file)))
            {
                string named = m.Value.TrimEnd('.', '/');
                string inKit = named.StartsWith("templates", StringComparison.Ordinal) ? "kit/" + named
                    : named.StartsWith(".claude/skills", StringComparison.Ordinal) ? "kit/skills" + named[".claude/skills".Length..]
                    : "";
                Assert.IsTrue(inKit.Length > 0 && Path.Exists(Path.Combine(TestEnv.Root, inKit)), $"{file} points at {named}, which a DMC folder does not have ({inKit})");
            }
        }
        foreach (string file in DeveloperPrompts())
        {
            foreach (Match m in pattern.Matches(Read(file)))
            {
                string named = m.Value.TrimEnd('.', '/');
                bool player = named.StartsWith("templates", StringComparison.Ordinal) || named.StartsWith(".claude/skills", StringComparison.Ordinal);
                if (player && file == "README.md")
                    continue; // the README talks to players about their DMC folder: checked by the kit tests above
                if (player && file.StartsWith(".claude/skills/review-", StringComparison.Ordinal))
                    continue;
                Assert.IsTrue(Path.Exists(Path.Combine(TestEnv.Root, named)), $"{file} points at {named}, which does not exist");
            }
        }
    }

    [TestMethod]
    public void ANewCampaignGetsEveryFileTheRulesDescribeAndNoPromptIsEmpty()
    {
        string campaign = Path.Combine(TestEnv.Root, "kit", "templates", "campaign");
        foreach (string name in new[] { "player-options.md", "game-state.md", "story/canon.md", "story/summary.md", "story/dm-notes.md", "character-sheets/minor-npcs.md" })
            Assert.IsTrue(File.Exists(Path.Combine(campaign, name)), $"kit/templates/campaign/{name} is missing");
        foreach (string folder in new[] { "maps", "story/sessions" })
            Assert.IsTrue(Directory.Exists(Path.Combine(campaign, folder)), $"kit/templates/campaign/{folder}/ is missing");
        foreach (string file in PromptFiles())
            Assert.IsFalse(string.IsNullOrWhiteSpace(Read(file)), $"{file} is empty");
    }

    /// <summary>
    /// The Python scripts and the two dice rollers are gone (Phase 6 of #3). Nothing DMC reads as instructions or
    /// settings, and nothing that tests play, may still send it to them.
    /// </summary>
    [TestMethod]
    public void NothingStillPointsAtTheOldScripts()
    {
        string[] files = [.. PromptFiles(), KitSettings, DevSettings, ".gitignore", ".github/workflows/tests.yml", ".github/workflows/release.yml"];
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

    /// <summary>The launchers are gone: no file of the kit, and none a player runs, names <c>play.ps1</c> or <c>play.sh</c> (FR-020).</summary>
    [TestMethod]
    public void TheLaunchersAreGone()
    {
        Assert.IsFalse(File.Exists(Path.Combine(TestEnv.Root, "play.ps1")));
        Assert.IsFalse(File.Exists(Path.Combine(TestEnv.Root, "play.sh")));
        CollectionAssert.AreEqual(new[] { "--model", "sonnet", "--effort", "high" }, PlayCommand.FixedArguments.ToArray());
    }
}
