using Dmc.Commands;
using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>dmc refresh [folder | --all]</c> (contract <c>dmc-refresh.md</c>): its output and errors in-process over a
/// <see cref="TestMachine"/>, and, through the published binary, the promise of SC-002: after <c>dmc install --path</c> again and
/// <c>dmc refresh</c> over a filled DMC folder every file of the player is the same, and none is added or removed.
/// </summary>
[TestClass]
public sealed class RefreshCommandTests
{
    /// <summary>The player's folders and files that no command of dmc may change.</summary>
    static readonly string[] PlayerFolders = ["adventures", "feedback", "modules", "rules", "exports"];

    /// <summary>Runs <c>dmc refresh</c> in-process over a machine.</summary>
    static (int Exit, string Out, string Err) Refresh(TestMachine machine, params string[] args)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var errors = new StringWriter { NewLine = "\n" };
        int exit = CommandLine.Run(["refresh", .. args], new Io(new StringReader(""), output, errors), [new RefreshCommand(machine.Machine())]);
        return (exit, output.ToString(), errors.ToString());
    }

    /// <summary>A DMC folder under the machine, installed with the test files of version 1.2.3.</summary>
    static string Installed(TestMachine machine, string name = "dmc")
    {
        string folder = Path.Combine(machine.Root, name);
        Refresher.Refresh(folder, TestMachine.Files(), "1.2.3", machine.Now);
        return folder;
    }

    [TestMethod]
    public void ACurrentFolderSaysNothing()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        var (exit, output, error) = Refresh(machine, folder);
        Assert.AreEqual(0, exit, error);
        Assert.AreEqual("", output);
        Assert.AreEqual("", error);
    }

    [TestMethod]
    public void RewrittenFilesAreOneLineAndKeptEditsAreAnother()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        File.Delete(Path.Combine(folder, "templates", "feedback.md"));
        File.WriteAllText(Path.Combine(folder, "CLAUDE.md"), "edited");
        string stamp = machine.Now.ToString("yyyyMMdd-HHmmss");
        var (exit, output, error) = Refresh(machine, folder);
        Assert.AreEqual(0, exit, error);
        CollectionAssert.AreEqual(new[]
        {
            $"dmc refresh: {folder}: 2 files updated for dmc 1.2.3",
            $"dmc refresh: {folder}: 1 edited files kept in .dmc/replaced/{stamp}/",
        }, output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    [TestMethod]
    public void WithNoArgumentsItRefreshesTheFolderThatDmcFolderFinds()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        File.Delete(Path.Combine(folder, "CLAUDE.md"));
        machine.Cwd = folder;
        var (exit, output, _) = Refresh(machine);
        Assert.AreEqual(0, exit);
        Assert.AreEqual($"dmc refresh: {folder}: 1 files updated for dmc 1.2.3\n", output);

        machine.Cwd = machine.Root;
        var (none, _, error) = Refresh(machine);
        Assert.AreEqual(1, none);
        Assert.AreEqual("dmc refresh: no DMC folder: run dmc install\n", error);
    }

    [TestMethod]
    public void AllRefreshesEveryRecordedFolderThatIsStillADmcFolder()
    {
        using var machine = new TestMachine();
        string a = Installed(machine, "a"), b = Installed(machine, "b"), gone = Installed(machine, "gone"), plain = Path.Combine(machine.Root, "plain");
        Directory.CreateDirectory(plain);
        foreach (string folder in new[] { a, gone, plain, b })
            DmcFolder.Record(machine.RunningFolder, folder);
        DmcFolders.Delete(gone);
        File.Delete(Path.Combine(a, "CLAUDE.md"));
        File.Delete(Path.Combine(b, "CLAUDE.md"));
        var (exit, output, error) = Refresh(machine, "--all");
        Assert.AreEqual(0, exit, error);
        CollectionAssert.AreEqual(new[]
        {
            $"dmc refresh: {a}: 1 files updated for dmc 1.2.3",
            $"dmc refresh: {b}: 1 files updated for dmc 1.2.3",
        }, output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.AreEqual(0, Refresh(machine, "--all").Exit);
    }

    [TestMethod]
    public void AFolderThatIsNotADmcFolderIsRefusedInOneLine()
    {
        using var machine = new TestMachine();
        string plain = Path.Combine(machine.Root, "plain");
        Directory.CreateDirectory(plain);
        foreach (string target in new[] { plain, Path.Combine(machine.Root, "missing") })
        {
            var (exit, output, error) = Refresh(machine, target);
            Assert.AreEqual(1, exit);
            Assert.AreEqual("", output);
            Assert.AreEqual($"dmc refresh: {target} is not a DMC folder: run dmc install\n", error);
        }
        Assert.IsEmpty(Directory.GetFileSystemEntries(plain), "nothing written");
    }

    [TestMethod]
    public void APathThatCannotBeWrittenIsOneLineAndTheOldManifestStays()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        string template = Path.Combine(folder, "templates", "feedback.md");
        File.Delete(template);
        Directory.CreateDirectory(template);
        var (exit, _, error) = Refresh(machine, folder);
        Assert.AreEqual(1, exit);
        StringAssert.StartsWith(error, $"dmc refresh: cannot write {template}");
        Assert.HasCount(1, error.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    [TestMethod]
    public void UsageMistakesAreExitTwo()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        Assert.AreEqual(2, Refresh(machine, "--bogus").Exit);
        Assert.AreEqual(2, Refresh(machine, folder, "--all").Exit);
        Assert.AreEqual(2, Refresh(machine, folder, folder).Exit);
        var help = Refresh(machine, "--help");
        Assert.AreEqual(0, help.Exit);
        StringAssert.StartsWith(help.Out, "usage: dmc refresh [FOLDER | --all]");
    }

    [TestMethod]
    public void InsideAClaudeCodeSessionItRefusesFirstAndWritesNothing()
    {
        using var machine = new TestMachine();
        string folder = Installed(machine);
        File.Delete(Path.Combine(folder, "CLAUDE.md"));
        machine.Environment["CLAUDECODE"] = "1";
        string[] before = DmcFolders.SnapshotAll(machine.Root);
        foreach (string[] args in new[] { new[] { folder }, ["--all"], [], ["--bogus"] })
        {
            var (exit, output, error) = Refresh(machine, args);
            Assert.AreEqual(1, exit, string.Join(' ', args));
            Assert.AreEqual("", output);
            Assert.AreEqual("dmc refresh: cannot run inside a Claude Code session: run it in a terminal\n", error);
        }
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(machine.Root));
    }

    [TestMethod]
    public void ThePublishedDmcRefusesInsideAClaudeCodeSession()
    {
        string folder = DmcFolders.MakeCurrentDmcFolder(Path.Combine(DmcFolders.NewTemp(), "dmc"));
        try
        {
            File.Delete(Path.Combine(folder, "CLAUDE.md"));
            var result = TestEnv.Run(DmcRunner.Binary(), ["refresh", folder], environment: new Dictionary<string, string?> { ["CLAUDECODE"] = "1" });
            Assert.AreEqual(1, result.ExitCode, result.ToString());
            Assert.AreEqual("dmc refresh: cannot run inside a Claude Code session: run it in a terminal\n", result.Stderr);
            Assert.IsFalse(File.Exists(Path.Combine(folder, "CLAUDE.md")), "nothing was written");
        }
        finally
        {
            DmcFolders.Delete(Path.GetDirectoryName(folder)!);
        }
    }

    /// <summary>
    /// SC-002, through the published binary: a filled DMC folder (campaigns, both player-options files, feedback entries, a module,
    /// a rules file, <c>exports/</c>, and the player's own settings and skill) is hashed, then <c>dmc install --path</c> is run over
    /// it again and <c>dmc refresh</c> (also with a hand-edited and a deleted generated file); the hashes are identical and no file or
    /// folder is added or removed.
    /// </summary>
    [TestMethod]
    public void InstallAndRefreshOverAFilledFolderChangeNoFileOfThePlayer()
    {
        string root = DmcFolders.NewTemp();
        string folder = Path.Combine(root, "dmc");
        string record = Path.Combine(Path.GetDirectoryName(DmcRunner.Binary())!, "dmc.json");
        byte[]? savedRecord = File.Exists(record) ? File.ReadAllBytes(record) : null;
        try
        {
            DmcFolders.MakeCurrentDmcFolder(folder);
            foreach (string file in new[]
                     {
                         "adventures/player-options.md", "adventures/.active-campaign", "adventures/silver-egg/player-options.md", "adventures/silver-egg/game-state.md",
                         "adventures/silver-egg/story/dm-notes.md", "adventures/red-keep/game-state.md", "feedback/2026-10-01-a.md", "feedback/2026-10-02-b.md",
                         "modules/lmop/index.md", "modules/lmop/chapters/1.md", "rules/srd.txt", "exports/silver-egg-202610011200.zip",
                         ".claude/settings.local.json", ".claude/skills/my-skill/SKILL.md", "my-notes.txt",
                     })
            {
                string path = Path.Combine(folder, file.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "content of " + file);
            }
            Directory.CreateDirectory(Path.Combine(folder, "adventures", "empty"));
            string[] Player() => [.. DmcFolders.Snapshot(folder, PlayerFolders), .. DmcFolders.Snapshot(folder, ".claude/skills/my-skill"),
                .. new[] { ".claude/settings.local.json", "my-notes.txt" }.Select(f => f + " " + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(folder, f)))))];
            string[] before = Player();

            var environment = new Dictionary<string, string?>
            {
                ["CLAUDECODE"] = null, ["USERPROFILE"] = root, ["HOME"] = root, ["LOCALAPPDATA"] = root, ["XDG_DATA_HOME"] = root,
            };
            var install = TestEnv.Run(DmcRunner.Binary(), ["install", "--path", folder], environment: environment);
            Assert.AreEqual(0, install.ExitCode, install.ToString());
            Assert.AreEqual($"dmc install: DMC folder ready at {folder}\n", install.Stdout, "a development build installs the folder only, and the folder is current");
            CollectionAssert.AreEqual(before, Player(), "after dmc install --path again");

            File.WriteAllText(Path.Combine(folder, "CLAUDE.md"), "edited by hand");
            File.Delete(Path.Combine(folder, ".claude", "skills", "dmc-resume-campaign", "SKILL.md"));
            var refresh = TestEnv.Run(DmcRunner.Binary(), ["refresh", folder], environment: environment);
            Assert.AreEqual(0, refresh.ExitCode, refresh.ToString());
            Assert.HasCount(2, refresh.StdoutLines);
            StringAssert.Contains(refresh.Stdout, "2 files updated for dmc 0.0.0-dev");
            StringAssert.Contains(refresh.Stdout, "1 edited files kept in .dmc/replaced/");
            CollectionAssert.AreEqual(before, Player(), "after dmc refresh");
            Assert.IsTrue(File.Exists(Path.Combine(folder, ".claude", "skills", "dmc-resume-campaign", "SKILL.md")));

            var all = TestEnv.Run(DmcRunner.Binary(), ["refresh", "--all"], environment: environment);
            Assert.AreEqual(0, all.ExitCode, all.ToString());
            Assert.AreEqual("", all.Stdout, "nothing left to refresh");
            CollectionAssert.AreEqual(before, Player(), "after dmc refresh --all");
        }
        finally
        {
            // dmc install records the folder beside the program that ran: put dmc.json back as it was.
            if (savedRecord is null)
                File.Delete(record);
            else
                File.WriteAllBytes(record, savedRecord);
            DmcFolders.Delete(root);
        }
    }
}
