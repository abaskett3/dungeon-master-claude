using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dmc.Commands;
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
    static string Read(string path) => File.ReadAllText(Path.Combine(TestEnv.Root, path)).Replace("\r\n", "\n");

    static JsonNode Settings(string name) => JsonNode.Parse(Read(Path.Combine(".claude", name)))!;

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

    static IEnumerable<(string Tool, string Pattern)> AllowRules() =>
        Settings("settings.json")["permissions"]!["allow"]!.AsArray().Select(r => (string)r!)
            .Select(r => Rule().Match(r)).Where(m => m.Success).Select(m => (m.Groups[1].Value, m.Groups[2].Value));

    [GeneratedRegex(@"^(\w+)\((.*)\)$")]
    private static partial Regex Rule();

    /// <summary>Whether a rule's pattern, where <c>*</c> stands for anything, takes the whole command.</summary>
    static bool Allowed(string tool, string command) =>
        AllowRules().Any(r => r.Tool == tool && Regex.IsMatch(command, "^" + Regex.Escape(r.Pattern).Replace(@"\*", ".*") + "$", RegexOptions.Singleline));

    /// <summary>A command the prompts tell DMC to run: <c>./bin/dmc</c> and what follows, up to the end of the code span or line.</summary>
    [GeneratedRegex(@"\./bin/dmc(?: [^`\n]*)?")]
    private static partial Regex DmcCommand();

    /// <summary>A subcommand, as in <c>`dmc map`</c> or <c>./bin/dmc pdf info &lt;pdf&gt;</c> (in a code span or a code block).</summary>
    [GeneratedRegex(@"(?:`|\./bin/)dmc ([a-z][a-z-]*)")]
    private static partial Regex Subcommand();

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
            }
            foreach (string command in DmcCommand().Matches(text).Select(m => m.Value.TrimEnd()))
            {
                // The same words run in either shell, so both must be allowed.
                Assert.IsTrue(Allowed("Bash", command), $"{file}: `{command}` is not allowed for Bash in settings.json");
                Assert.IsTrue(Allowed("PowerShell", command), $"{file}: `{command}` is not allowed for PowerShell in settings.json");
            }
        }
        CollectionAssert.IsSubsetOf(new[] { "roll", "map", "pdf", "digest" }, used.ToArray(), "the prompts name every subcommand DMC runs");

        // The way to fetch a missing dmc, which CLAUDE.md gives in both forms.
        string claude = Read("CLAUDE.md");
        StringAssert.Contains(claude, "`./tools/get-dmc.ps1`");
        StringAssert.Contains(claude, "`bash tools/get-dmc.sh`");
        Assert.IsTrue(Allowed("PowerShell", "./tools/get-dmc.ps1"));
        Assert.IsTrue(Allowed("Bash", "bash tools/get-dmc.sh"));
        // Windows names: dmc.exe, and PowerShell's backslashes.
        foreach (string command in new[] { "./bin/dmc.exe roll 1d20", ".\\bin\\dmc roll 1d20", ".\\bin\\dmc.exe roll 1d20" })
            Assert.IsTrue(Allowed("PowerShell", command), command);
        Assert.IsTrue(Allowed("Bash", "./bin/dmc.exe roll 1d20"));
    }

    [TestMethod]
    public void TheAllowListNamesOnlyDmcAndGetDmc()
    {
        foreach (var (tool, pattern) in AllowRules().Where(r => r.Tool is "Bash" or "PowerShell"))
        {
            string path = pattern.Replace('\\', '/');
            Assert.IsTrue(path.StartsWith("./bin/dmc", StringComparison.Ordinal) || path.Contains("tools/get-dmc.", StringComparison.Ordinal),
                $"{tool}({pattern}) is neither dmc nor get-dmc");
            if (Regex.Match(path, @"tools/[\w.-]+") is { Success: true } file)
                Assert.IsTrue(File.Exists(Path.Combine(TestEnv.Root, file.Value)), $"{tool}({pattern}) names {file.Value}, which is missing");
        }
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

    /// <summary>Run a hook's command the way Claude Code does, with bash, for the toolkit at <paramref name="project"/>.</summary>
    static ProcessResult RunHook(string command, string project, string? stdin = null) =>
        TestEnv.Run(Bootstrap.Shell("bash"), ["-c", command], stdin, cwd: project,
            environment: new Dictionary<string, string?> { ["CLAUDE_PROJECT_DIR"] = project });

    [TestMethod]
    public void WithoutBinDmcASessionIsToldOnceHowToGetIt()
    {
        string[] commands = Settings("settings.json")["hooks"]!["SessionStart"]!.AsArray()
            .Where(g => g!["matcher"] is null).SelectMany(g => g!["hooks"]!.AsArray()).Select(h => (string)h!["command"]!)
            .Where(c => c.Contains("get-dmc", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, commands, "one SessionStart hook, for every kind of start, checks for bin/dmc");

        string project = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(project);
        try
        {
            var missing = RunHook(commands[0], project);
            Assert.AreEqual(0, missing.ExitCode, missing.ToString());
            Assert.HasCount(1, missing.StdoutLines, missing.ToString());
            StringAssert.Contains(missing.Stdout, "./tools/get-dmc.ps1");
            StringAssert.Contains(missing.Stdout, "bash tools/get-dmc.sh");
            Assert.AreEqual("", missing.Stderr);

            Directory.CreateDirectory(Path.Combine(project, "bin"));
            File.WriteAllText(Path.Combine(project, "bin", Bootstrap.ExeName), "");
            var present = RunHook(commands[0], project);
            Assert.AreEqual(0, present.ExitCode, present.ToString());
            Assert.AreEqual("", present.Stdout + present.Stderr, "silent when bin/dmc is there");
        }
        finally
        {
            Directory.Delete(project, recursive: true);
        }
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
        string project = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(project);
        try
        {
            var missing = RunHook(hook, project, "{}");
            Assert.AreEqual(0, missing.ExitCode, missing.ToString());
            Assert.AreEqual("", missing.Stdout + missing.Stderr, "a missing bin/dmc must not show in every prompt");

            // With the published dmc in this repository: the reminder, from a log past the threshold.
            DmcRunner.Binary();
            string log = Path.Combine(project, "session.jsonl");
            File.WriteAllText(log, ContextCheck.ContextCheckContract.Assistant(163400).ToJsonString() + "\n");
            var due = RunHook(hook, TestEnv.Root, new JsonObject { ["transcript_path"] = log }.ToJsonString());
            Assert.AreEqual(0, due.ExitCode, due.ToString());
            StringAssert.StartsWith(due.Stdout, "Context 163k, break due since 150k", due.ToString());
        }
        finally
        {
            Directory.Delete(project, recursive: true);
        }
    }

    [TestMethod]
    public void ThePlaySettingsKeepEverythingPlayNeeds()
    {
        var settings = Settings("play-settings.json");
        // Tools play, the dice and map commands, rules lookups and the module and import skills need.
        string[] needed = ["Read", "Write", "Edit", "Glob", "Grep", "Bash", "PowerShell", "Skill", "ToolSearch",
                           "AskUserQuestion", "WebSearch", "WebFetch", "Agent"];
        foreach (string rule in settings["permissions"]?["deny"]?.AsArray().Select(r => (string)r!) ?? [])
        {
            string pattern = "^" + Regex.Escape(rule.Split('(')[0]).Replace(@"\*", ".*") + "$";
            foreach (string tool in needed)
                Assert.IsFalse(Regex.IsMatch(tool, pattern), $"play-settings.json denies {tool} ({rule}), which play needs");
        }
        var overrides = settings["skillOverrides"]?.AsObject();
        foreach (string skill in SkillNames())
            Assert.AreEqual("on", (string?)overrides?[skill] ?? "on", $"play-settings.json hides /{skill}");
        // The project's settings.json still applies under --settings, and its allow list is the one to keep up to date.
        Assert.IsNull(settings["permissions"]?["allow"]);
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

    [TestMethod]
    public void PersonalFilesStayOutOfGit() =>
        CollectionAssert.AreEqual(new[] { "adventures/.gitkeep", "modules/.gitkeep", "rules/.gitkeep" },
            GitLsFiles(["adventures", "modules", "rules", "bin"]).Order(StringComparer.Ordinal).ToArray());

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

    [TestMethod]
    public void PathsNamedInThePromptsExist()
    {
        foreach (string file in PromptFiles())
        {
            foreach (Match m in Regex.Matches(Read(file), @"(?<![\w/.-])(?:templates|tools|\.claude/skills)/[A-Za-z0-9_./-]+"))
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
        var old = new Regex(@"scripts/|python|pymupdf|\.pylib|roll\.sh|roll\.ps1|map_render|pdf_extract|transcript_digest|context_check",
            RegexOptions.IgnoreCase);
        foreach (string file in files)
        {
            var hit = old.Match(Read(file));
            Assert.IsFalse(hit.Success, $"{file} still names {hit.Value}");
        }
    }
}
