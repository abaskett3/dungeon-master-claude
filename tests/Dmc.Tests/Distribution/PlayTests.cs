using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Commands;
using Dmc.Kit;
using Dmc.Tests.Support;
using static Dmc.Tests.Distribution.Bootstrap;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>dmc play</c> (contract <c>dmc-play.md</c>): the folder it plays in, the refresh before the start, the <c>PATH</c> it
/// gives Claude Code, the fixed arguments and the player's own, and what it says when it cannot start. In-process over a
/// <see cref="TestMachine"/> that records the process it would start, and once through the published binary with a fake
/// <c>claude</c>. Also the pin file's format, which stays until the release workflow stops using it.
/// </summary>
[TestClass]
public sealed class PlayTests
{
    /// <summary>The generated files <see cref="Play"/> uses: <see cref="TestMachine.Files"/>.</summary>
    static IReadOnlyList<GeneratedFile> Files(string rules = "rules v1\n") => TestMachine.Files(rules);

    /// <summary>
    /// Installs the test files into a new DMC folder under the machine, as <c>dmc install</c> would, and returns it.
    /// </summary>
    static string Installed(TestMachine machine, string version = "1.2.3", IReadOnlyList<GeneratedFile>? files = null)
    {
        string folder = Path.Combine(machine.Root, "dmc");
        Refresher.Refresh(folder, files ?? Files(), version, machine.Now);
        return folder;
    }

    /// <summary>What a play run did: the process it would have started, its output, and the exit code.</summary>
    /// <param name="Started">The process <c>dmc play</c> asked to start; null when it did not get that far.</param>
    /// <param name="Stdout">What it printed.</param>
    /// <param name="Exit">The exit code <c>dmc play</c> returned (the fake start returns 7).</param>
    sealed record Played(ProcessStartInfo? Started, string Stdout, int Exit);

    /// <summary>Runs <c>dmc play</c> in-process over the machine, with a start that only records what it was given.</summary>
    /// <param name="machine">The machine; with <paramref name="claudeFolder"/>, its <c>PATH</c> gets a fake claude.</param>
    /// <param name="args">The player's arguments.</param>
    /// <param name="version">The version the running program says it is.</param>
    /// <param name="files">The generated files of this program.</param>
    /// <param name="claudeFolder">A folder with a fake claude to put on the machine's <c>PATH</c>; null to leave the <c>PATH</c> as it is.</param>
    static Played Play(TestMachine machine, string[] args, string version = "1.2.3", IReadOnlyList<GeneratedFile>? files = null, string? claudeFolder = null)
    {
        if (claudeFolder is not null)
            machine.Environment["PATH"] = PathWith(claudeFolder);
        ProcessStartInfo? started = null;
        using var output = new StringWriter { NewLine = "\n" };
        int exit = PlayCommand.Run(args, new Io(new StringReader(""), output, new StringWriter()), machine.Machine(version, files), info =>
        {
            started = info;
            return 7;
        });
        return new Played(started, output.ToString(), exit);
    }

    /// <summary>Runs <paramref name="test"/> with a machine, a fake claude and a current DMC folder under it, and cleans up.</summary>
    static void WithPlay(Action<TestMachine, string, string> test)
    {
        using var machine = new TestMachine();
        string claude = FakeClaude();
        try
        {
            test(machine, Installed(machine), claude);
        }
        finally
        {
            DmcFolders.Delete(claude);
        }
    }

    [TestMethod]
    public void PlayStartsClaudeInTheDmcFolderWithTheFixedArgumentsThenThePlayersOwn() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        var played = Play(machine, ["--continue", "two words", "--help"], claudeFolder: claude);
        Assert.AreEqual(7, played.Exit, "claude's exit code is dmc's");
        var started = played.Started!;
        Assert.AreEqual(Path.GetFullPath(folder), Path.GetFullPath(started.WorkingDirectory));
        Assert.IsFalse(started.UseShellExecute);
        Assert.IsFalse(started.RedirectStandardInput || started.RedirectStandardOutput || started.RedirectStandardError, "claude gets the terminal");
        // The fixed arguments, then the player's, unchanged and last; no --settings, plugin or system-prompt flag.
        CollectionAssert.AreEqual(new[] { "--model", "sonnet", "--effort", "high" }, PlayCommand.FixedArguments.ToArray());
        string[] expected = ["--model", "sonnet", "--effort", "high", "--continue", "two words", "--help"];
        CollectionAssert.AreEqual(expected, started.ArgumentList.Skip(started.ArgumentList.Count - expected.Length).ToArray());
        Assert.DoesNotContain("--settings", started.ArgumentList);
    });

    [TestMethod]
    public void PlayGivesClaudeThePathWithTheProgramFolderFirst() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        var played = Play(machine, [], claudeFolder: claude);
        string inherited = machine.Environment["PATH"]!;
        Assert.AreEqual(machine.RunningFolder + Path.PathSeparator + inherited, played.Started!.Environment["PATH"]);
    });

    [TestMethod]
    public void PlayPlaysInTheCurrentFolderWhenItIsADmcFolderAndOtherwiseInTheFirstRecordedOne() => WithPlay((machine, folder, claude) =>
    {
        string second = Installed(machine) + "-two";
        Refresher.Refresh(second, Files(), "1.2.3", machine.Now);
        machine.Cwd = machine.Root; // not a DMC folder
        DmcFolder.Record(machine.RunningFolder, Path.Combine(machine.Root, "gone"));
        DmcFolder.Record(machine.RunningFolder, second);
        DmcFolder.Record(machine.RunningFolder, folder);

        var recorded = Play(machine, [], claudeFolder: claude);
        Assert.AreEqual(Path.GetFullPath(second), Path.GetFullPath(recorded.Started!.WorkingDirectory), "the first recorded folder that is still a DMC folder");

        machine.Cwd = folder;
        var current = Play(machine, []);
        Assert.AreEqual(Path.GetFullPath(folder), Path.GetFullPath(current.Started!.WorkingDirectory), "the current folder comes first");

        // CLAUDE_PROJECT_DIR is for hooks: it does not choose where a session starts.
        machine.Cwd = machine.Root;
        machine.Environment["CLAUDE_PROJECT_DIR"] = folder;
        var viaVariable = Play(machine, []);
        Assert.AreEqual(Path.GetFullPath(second), Path.GetFullPath(viaVariable.Started!.WorkingDirectory));
    });

    [TestMethod]
    public void PlayWithNoDmcFolderIsRefusedBeforeAnythingStarts()
    {
        using var machine = new TestMachine();
        string claude = FakeClaude();
        try
        {
            machine.Environment["PATH"] = PathWith(claude);
            ProcessStartInfo? started = null;
            var error = Assert.ThrowsExactly<DmcException>(() =>
                PlayCommand.Run([], new Io(new StringReader(""), new StringWriter(), new StringWriter()), machine.Machine(), info => (started = info) is null ? 0 : 7));
            Assert.AreEqual("no DMC folder: run dmc install", error.Message);
            Assert.AreEqual(1, error.ExitCode);
            Assert.IsNull(started);
        }
        finally
        {
            DmcFolders.Delete(claude);
        }
    }

    [TestMethod]
    public void AFolderThatIsCurrentIsStartedWithoutAWordOrAWrite() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        string[] before = DmcFolders.SnapshotAll(folder);
        var played = Play(machine, [], claudeFolder: claude);
        Assert.AreEqual("", played.Stdout);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(folder));
    });

    [TestMethod]
    public void AHandChangedRulesFileIsPutBackAndTheChangedCopyKept() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        string rules = Path.Combine(folder, "CLAUDE.md");
        File.WriteAllText(rules, "my own rules\n");
        var played = Play(machine, [], claudeFolder: claude);

        Assert.AreEqual("rules v1\n", File.ReadAllText(rules), "FR-005: the generated file is the program's again");
        string stamp = machine.Now.ToString("yyyyMMdd-HHmmss");
        Assert.AreEqual("my own rules\n", File.ReadAllText(Path.Combine(folder, ".dmc", "replaced", stamp, "CLAUDE.md")));
        CollectionAssert.AreEqual(new[]
        {
            $"dmc refresh: {Path.GetFullPath(folder)}: 1 files updated for dmc 1.2.3",
            $"dmc refresh: {Path.GetFullPath(folder)}: 1 edited files kept in .dmc/replaced/{stamp}/",
        }, played.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.IsNotNull(played.Started, "and play went on");
    });

    [TestMethod]
    public void AMissingSkillIsRestoredBeforeTheStart() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        string skill = Path.Combine(folder, ".claude", "skills", "dmc-draw-map", "SKILL.md");
        File.Delete(skill);
        var played = Play(machine, [], claudeFolder: claude);
        Assert.AreEqual("draw map\n", File.ReadAllText(skill));
        StringAssert.Contains(played.Stdout, "1 files updated for dmc 1.2.3");
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, ".dmc", "replaced")), "nothing was edited, so nothing is kept");
    });

    [TestMethod]
    public void AFolderWrittenByAnotherVersionIsRefreshedBeforeTheStart() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        // The same files, written by 1.0.0: only the manifest names a different version, which is enough to refresh.
        Refresher.Refresh(folder, Files(), "1.0.0", machine.Now);
        Play(machine, [], version: "1.2.3", claudeFolder: claude);
        Assert.AreEqual("1.2.3", Manifest.Read(folder)!.Version);

        // A newer program with a changed file: the old generated copy is replaced, and none is kept (nobody edited it).
        var played = Play(machine, [], version: "1.3.0", files: Files("rules v2\n"));
        Assert.AreEqual("rules v2\n", File.ReadAllText(Path.Combine(folder, "CLAUDE.md")));
        StringAssert.Contains(played.Stdout, "1 files updated for dmc 1.3.0");
        Assert.DoesNotContain("kept", played.Stdout);
    });

    [TestMethod]
    public void LeftoversOfAnEarlierUpdateAreRemovedQuietly() => WithPlay((machine, folder, claude) =>
    {
        machine.Cwd = folder;
        File.WriteAllText(Path.Combine(machine.RunningFolder, "dmc.exe.old"), "old");
        Directory.CreateDirectory(Path.Combine(machine.RunningFolder, ".update-abc"));
        var played = Play(machine, [], claudeFolder: claude);
        Assert.AreEqual("", played.Stdout);
        Assert.IsFalse(File.Exists(Path.Combine(machine.RunningFolder, "dmc.exe.old")));
        Assert.IsFalse(Directory.Exists(Path.Combine(machine.RunningFolder, ".update-abc")));
        Assert.IsTrue(File.Exists(machine.RunningExecutable), "the program itself stays");
    });

    [TestMethod]
    public void WithoutClaudeCodeOnThePathPlayIsRefusedWithCode127() => WithPlay((machine, folder, _) =>
    {
        machine.Cwd = folder;
        machine.Environment["PATH"] = Path.Combine(machine.Root, "empty");
        var error = Assert.ThrowsExactly<DmcException>(() => Play(machine, []));
        Assert.AreEqual("Claude Code (claude) is not on the PATH: install it, then run this again", error.Message);
        Assert.AreEqual(127, error.ExitCode);
    });

    /// <summary>Folders holding empty files of the given names, joined as a PATH; deleted afterwards.</summary>
    static void WithPath(string[][] folders, Action<string[], string> test)
    {
        string[] dirs = folders.Select(_ => Path.Combine(Path.GetTempPath(), "dmc-path-" + Guid.NewGuid().ToString("N"))).ToArray();
        try
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                Directory.CreateDirectory(dirs[i]);
                foreach (string name in folders[i])
                    File.WriteAllText(Path.Combine(dirs[i], name), "");
            }
            test(dirs, string.Join(Path.PathSeparator, dirs));
        }
        finally
        {
            foreach (string dir in dirs)
                Directory.Delete(dir, recursive: true);
        }
    }

    // The three cases of Windows, which FindClaude takes as a flag so that they run on every OS.

    [TestMethod]
    public void OnWindowsClaudeExeAnywhereOnThePathComesBeforeNpmsShims()
    {
        WithPath([["claude.cmd", "claude.ps1"], ["claude.exe"]], (dirs, path) =>
        {
            var launch = PlayCommand.FindClaude(path, windows: true);
            Assert.AreEqual(Path.Combine(dirs[1], "claude.exe"), launch.File);
            Assert.IsEmpty(launch.Prefix);
        });
    }

    [TestMethod]
    public void OnWindowsNpmsShimRunsThroughPowerShellNotCmd()
    {
        WithPath([["claude.cmd", "claude.ps1"], ["pwsh.exe"]], (dirs, path) =>
        {
            var launch = PlayCommand.FindClaude(path, windows: true);
            Assert.AreEqual(Path.Combine(dirs[1], "pwsh.exe"), launch.File);
            CollectionAssert.AreEqual(new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(dirs[0], "claude.ps1") },
                launch.Prefix.ToArray());
        });
    }

    [TestMethod]
    public void OnWindowsALoneCmdShimIsOneClearLineThatDoesNotMentionTheLaunchers()
    {
        WithPath([["claude.cmd"]], (dirs, path) =>
        {
            var error = Assert.ThrowsExactly<DmcException>(() => PlayCommand.FindClaude(path, windows: true));
            Assert.AreEqual($"only {Path.Combine(dirs[0], "claude.cmd")} was found, and dmc does not start Claude Code through cmd.exe: install Claude Code's claude.exe", error.Message);
            Assert.DoesNotContain("play.ps1", error.Message);
        });
    }

    /// <summary>
    /// The published binary finds claude on the PATH (on Windows npm's claude.ps1, never its claude.cmd) and starts it in the
    /// DMC folder it was run in, with its own folder first on the <c>PATH</c> and the fixed arguments, then the player's.
    /// </summary>
    [TestMethod]
    public void ThePublishedDmcStartsClaudeInTheDmcFolder()
    {
        string claude = FakeClaude();
        string folder = DmcFolders.MakeCurrentDmcFolder(DmcFolders.NewTemp());
        try
        {
            var result = TestEnv.Run(DmcRunner.Binary(), ["play", "--continue", "two words"], cwd: folder,
                environment: new Dictionary<string, string?> { ["PATH"] = PathWith(claude) });
            Assert.AreEqual(7, result.ExitCode, result.ToString());
            Assert.AreEqual("", result.Stderr, result.ToString());
            var (workedIn, path, args) = ClaudeSaw(result);
            Assert.AreEqual(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(workedIn).TrimEnd(Path.DirectorySeparatorChar));
            CollectionAssert.AreEqual(ClaudeArgs("--continue", "two words"), args);
            string programFolder = Path.GetDirectoryName(DmcRunner.Binary())!;
            StringAssert.StartsWith(path, programFolder + Path.PathSeparator);
        }
        finally
        {
            DmcFolders.Delete(claude);
            DmcFolders.Delete(folder);
        }
    }

    /// <summary>
    /// The pin is in the form get-dmc.sh reads. The project's version stays the development one, which no release has:
    /// the release workflow gets a release's version from its tag and never writes it into the project, so a build
    /// made here cannot pass for the pinned release. (The pin goes with <c>get-dmc</c> when the release workflow is rewritten.)
    /// </summary>
    [TestMethod]
    public void ThePinIsReadableAndTheProjectsVersionIsTheDevelopmentOne()
    {
        string text = File.ReadAllText(Path.Combine(TestEnv.Root, "tools", "dmc-release.json"));
        using var pin = JsonDocument.Parse(text);
        string project = File.ReadAllText(Path.Combine(TestEnv.Root, "src", "Dmc.Cli", "Dmc.Cli.csproj"));
        StringAssert.Contains(project, "<Version>0.0.0-dev</Version>");
        string tag = pin.RootElement.GetProperty("tag").GetString()!;
        var hashes = pin.RootElement.GetProperty("sha256").EnumerateObject().ToArray();
        if (tag.Length == 0)
        {
            Assert.IsEmpty(hashes, "no hashes without a tag");
            return;
        }
        StringAssert.Matches(tag, new Regex(@"^dmc-v\d+\.\d+\.\d+$"));
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"tag\"\\s*:\\s*\"{tag}\"")), "the tag on a line of its own");
        foreach (var hash in hashes)
        {
            Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"{hash.Name}\"\\s*:\\s*\"[0-9a-f]{{64}}\"")),
                $"the SHA-256 of {hash.Name} on a line of its own");
        }
    }
}
