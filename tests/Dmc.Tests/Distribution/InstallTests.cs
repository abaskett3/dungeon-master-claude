using System.Text.Json;
using Dmc.Commands;
using Dmc.Install;
using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>dmc install</c> (contract <c>dmc-install.md</c>): the folder it asks for and takes, what it refuses, what it writes and
/// what it never writes, how the program itself is installed, and what it prints. In-process over a <see cref="TestMachine"/>
/// (a temporary home folder, environment and fake user Path, so that nothing reaches the real ones), and the refusal inside a
/// Claude Code session through the published binary.
/// </summary>
[TestClass]
public sealed class InstallTests
{
    /// <summary>What an install did: its exit code, standard output and standard error.</summary>
    /// <param name="Exit">The exit code.</param>
    /// <param name="Out">Standard output.</param>
    /// <param name="Err">Standard error.</param>
    sealed record Ran(int Exit, string Out, string Err)
    {
        /// <summary>The non-empty lines of standard output.</summary>
        public string[] Lines => Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Runs <c>dmc install</c> in-process over a machine.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="args">The arguments after <c>install</c>.</param>
    /// <param name="answer">What a person at the terminal types (a line); null means standard input is not a terminal.</param>
    /// <param name="version">The version the running program says it is.</param>
    static Ran Install(TestMachine machine, string[] args, string? answer = null, string version = "1.2.3")
    {
        var command = new InstallCommand(machine.Machine(version));
        using var output = new StringWriter { NewLine = "\n" };
        using var errors = new StringWriter { NewLine = "\n" };
        var io = new Io(new StringReader(answer is null ? "" : answer + "\n"), output, errors, InIsTerminal: answer is not null);
        int exit = CommandLine.Run(["install", .. args], io, [command]);
        return new Ran(exit, output.ToString(), errors.ToString());
    }


    [TestMethod]
    public void TheDefaultFolderIsDmcInTheHomeFolderAndEnterTakesIt()
    {
        using var machine = new TestMachine();
        var ran = Install(machine, [], answer: "", version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        string folder = Path.Combine(machine.Home, "dmc");
        StringAssert.StartsWith(ran.Out, $"DMC folder [{folder}]: ");
        Assert.IsTrue(DmcFolder.IsDmcFolder(folder));
        // The answer's own line break is the person's Enter, which a test does not echo: the line follows the question.
        StringAssert.EndsWith(ran.Lines.Last(), $"dmc install: DMC folder ready at {folder}");
    }

    [TestMethod]
    public void PathNamesTheFolderAndNothingIsAsked()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "elsewhere", "my-dmc");
        var ran = Install(machine, ["--path", folder], answer: "this answer is never read", version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        Assert.DoesNotContain("DMC folder [", ran.Out);
        Assert.IsTrue(DmcFolder.IsDmcFolder(folder));
        Assert.IsFalse(Directory.Exists(Path.Combine(machine.Home, "dmc")));
        // --path=FOLDER is read too.
        string second = Path.Combine(machine.Root, "second");
        Assert.AreEqual(0, Install(machine, [$"--path={second}"], version: "0.0.0-dev").Exit);
        Assert.IsTrue(DmcFolder.IsDmcFolder(second));
    }

    [TestMethod]
    public void ATildeAtTheStartOfTheAnswerIsTheHomeFolder()
    {
        using var machine = new TestMachine();
        var ran = Install(machine, [], answer: "~/games/dmc", version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        Assert.IsTrue(DmcFolder.IsDmcFolder(Path.Combine(machine.Home, "games", "dmc")));
        Assert.AreEqual(0, Install(machine, [], answer: "  ~  ", version: "0.0.0-dev").Exit);
        Assert.IsTrue(DmcFolder.IsDmcFolder(machine.Home), "~ alone is the home folder itself");
    }

    [TestMethod]
    public void ARelativeAnswerIsRelativeToTheCurrentFolder()
    {
        using var machine = new TestMachine();
        machine.Cwd = machine.Root;
        Assert.AreEqual(0, Install(machine, [], answer: "rel-dmc", version: "0.0.0-dev").Exit);
        Assert.IsTrue(DmcFolder.IsDmcFolder(Path.Combine(machine.Root, "rel-dmc")));
    }

    [TestMethod]
    public void WithNoTerminalTheDefaultIsTakenWithoutAsking()
    {
        using var machine = new TestMachine();
        var ran = Install(machine, [], answer: null, version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        Assert.DoesNotContain("DMC folder [", ran.Out, "no terminal, so no question");
        Assert.IsTrue(DmcFolder.IsDmcFolder(Path.Combine(machine.Home, "dmc")));
    }

    /// <summary>
    /// On Linux, under <c>curl ... | sh</c>, standard input is the pipe, so the answer is read from the terminal itself. (A dev
    /// version, so that no link is made and the test runs on any system.)
    /// </summary>
    [TestMethod]
    public void WhenStandardInputIsNotATerminalTheAnswerComesFromTheTerminalOnLinux()
    {
        using var machine = new TestMachine { Windows = false, Terminal = new StringReader("~/from-the-tty\n") };
        var ran = Install(machine, [], answer: null, version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        StringAssert.StartsWith(ran.Out, "DMC folder [");
        Assert.IsTrue(DmcFolder.IsDmcFolder(Path.Combine(machine.Home, "from-the-tty")));

        // On Windows the console is standard input: a machine that offers a terminal there does not get it read.
        using var windows = new TestMachine { Windows = true, Terminal = new StringReader("~/never\n") };
        Assert.AreEqual(0, Install(windows, [], answer: null, version: "0.0.0-dev").Exit);
        Assert.IsTrue(DmcFolder.IsDmcFolder(Path.Combine(windows.Home, "dmc")));
    }

    [TestMethod]
    public void AFolderInsideACheckoutOfTheRepositoryIsRefused()
    {
        using var machine = new TestMachine();
        string checkout = Path.Combine(machine.Root, "checkout");
        Directory.CreateDirectory(Path.Combine(checkout, "kit"));
        File.WriteAllText(Path.Combine(checkout, "kit", "dm-rules.md"), "rules");
        foreach (string folder in new[] { checkout, Path.Combine(checkout, "play"), Path.Combine(checkout, "a", "b") })
        {
            var ran = Install(machine, ["--path", folder], version: "0.0.0-dev");
            Assert.AreEqual(1, ran.Exit);
            Assert.AreEqual("", ran.Out);
            Assert.AreEqual($"dmc install: {folder} is inside the DMC source repository: choose another folder\n", ran.Err);
        }
        Assert.IsFalse(Directory.Exists(Path.Combine(checkout, "play")), "nothing was made");
        Assert.IsFalse(File.Exists(Path.Combine(checkout, "CLAUDE.md")));
    }

    [TestMethod]
    public void AFileInTheWayIsRefusedByNameAndNothingIsWrittenNotEvenTheProgram()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "mine");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "CLAUDE.md"), "my own notes for another tool");
        string[] before = DmcFolders.SnapshotAll(machine.Root);
        var ran = Install(machine, ["--path", folder], version: "1.2.3");
        Assert.AreEqual(1, ran.Exit);
        Assert.AreEqual("", ran.Out);
        Assert.AreEqual($"dmc install: {Path.Combine(folder, "CLAUDE.md")} already exists and was not written by dmc: move it, or choose another folder\n", ran.Err);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(machine.Root), "nothing written anywhere");
        Assert.IsFalse(Directory.Exists(machine.ProgramFolder), "the program is not installed for a folder that is refused");
    }

    [TestMethod]
    public void AFolderThatIsAFileIsRefused()
    {
        using var machine = new TestMachine();
        string file = Path.Combine(machine.Root, "afile");
        File.WriteAllText(file, "x");
        var ran = Install(machine, ["--path", file], version: "0.0.0-dev");
        Assert.AreEqual(1, ran.Exit);
        Assert.AreEqual($"dmc install: {file} is a file, not a folder\n", ran.Err);
    }

    [TestMethod]
    public void ThePlayerFoldersAreMadeEmptyAndNothingIsWrittenInThem()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "dmc");
        Assert.AreEqual(0, Install(machine, ["--path", folder], version: "0.0.0-dev").Exit);
        foreach (string name in new[] { "adventures", "feedback", "modules", "rules" })
        {
            Assert.IsTrue(Directory.Exists(Path.Combine(folder, name)), name);
            Assert.IsEmpty(Directory.GetFileSystemEntries(Path.Combine(folder, name)), $"{name} stays empty");
        }
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, "exports")), "exports/ is made by dmc campaign export");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "CLAUDE.md")));
        Assert.IsTrue(File.Exists(Path.Combine(folder, ".dmc", "manifest.json")));
    }

    [TestMethod]
    public void ARealKitInstallPutsInTheDmcFolderWhatItShould()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "dmc");
        var command = new InstallCommand(machine.Machine("0.0.0-dev", GeneratedFiles.All));
        using var output = new StringWriter();
        Assert.AreEqual(0, CommandLine.Run(["install", "--path", folder], new Io(new StringReader(""), output, new StringWriter()), [command]));
        foreach (string expected in new[] { "CLAUDE.md", ".claude/settings.json", ".claude/skills/dmc-resume-campaign/SKILL.md", "templates/feedback.md", ".dmc/manifest.json" })
            Assert.IsTrue(File.Exists(Path.Combine(folder, expected.Replace('/', Path.DirectorySeparatorChar))), expected);
        foreach (string absent in new[] { "src", "tests", ".docs", ".claude/skills/review-feedback", ".claude/skills/speckit-plan", "kit", "specs" })
            Assert.IsFalse(Path.Exists(Path.Combine(folder, absent.Replace('/', Path.DirectorySeparatorChar))), absent);
    }

    [TestMethod]
    public void ASecondInstallOverTheFolderBehavesAsARefresh()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "dmc");
        Assert.AreEqual(0, Install(machine, ["--path", folder], version: "0.0.0-dev").Exit);
        File.WriteAllText(Path.Combine(folder, "adventures", "mine.md"), "the player's");
        File.WriteAllText(Path.Combine(folder, "CLAUDE.md"), "edited by hand");
        string stamp = machine.Now.ToString("yyyyMMdd-HHmmss");

        var ran = Install(machine, ["--path", folder], version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        CollectionAssert.AreEqual(new[]
        {
            $"dmc install: {folder}: 1 files updated for dmc 0.0.0-dev",
            $"dmc install: {folder}: 1 edited files kept in .dmc/replaced/{stamp}/",
            $"dmc install: DMC folder ready at {folder}",
        }, ran.Lines);
        Assert.AreEqual("rules v1\n", File.ReadAllText(Path.Combine(folder, "CLAUDE.md")));
        Assert.AreEqual("edited by hand", File.ReadAllText(Path.Combine(folder, ".dmc", "replaced", stamp, "CLAUDE.md")));
        Assert.AreEqual("the player's", File.ReadAllText(Path.Combine(folder, "adventures", "mine.md")));
        Assert.HasCount(1, DmcFolder.Recorded(machine.RunningFolder), "recorded once");
    }

    [TestMethod]
    public void ADevelopmentBuildDoesNotCopyTheProgramWithoutForce()
    {
        using var machine = new TestMachine();
        string folder = Path.Combine(machine.Root, "dmc");
        var ran = Install(machine, ["--path", folder], version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        Assert.IsFalse(Directory.Exists(machine.ProgramFolder), "the installed release is left alone");
        CollectionAssert.AreEqual(new[] { $"dmc install: DMC folder ready at {folder}" }, ran.Lines);
        Assert.AreEqual(0, machine.UserPath.Writes);
        // The folder is recorded beside the build that wrote it, not in the program folder of the installed release.
        CollectionAssert.AreEqual(new[] { folder }, DmcFolder.Recorded(machine.RunningFolder).ToArray());
    }

    [TestMethod]
    public void ForceInstallsADevelopmentBuildAsTheProgram()
    {
        using var machine = new TestMachine { Windows = true };
        string folder = Path.Combine(machine.Root, "dmc");
        var ran = Install(machine, ["--path", folder, "--force"], version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        string program = machine.ProgramFolder;
        CollectionAssert.AreEqual(new[]
        {
            $"dmc install: installed dmc 0.0.0-dev in {program}",
            $"dmc install: DMC folder ready at {folder}",
            "dmc install: open a new terminal, then run dmc play",
        }, ran.Lines);
        Assert.IsTrue(File.Exists(Path.Combine(program, "dmc.exe")));
        Assert.IsTrue(File.Exists(Path.Combine(program, TestMachine.NativeLibrary)), "the native libraries come along");
        Assert.IsFalse(File.Exists(Path.Combine(program, "notes.pdb")), "and nothing else");
        CollectionAssert.AreEqual(new[] { folder }, DmcFolder.Recorded(program).ToArray(), "recorded beside the installed program");
    }

    [TestMethod]
    public void AReleaseIsInstalledAsTheProgramAndPutOnTheUserPathOnWindows()
    {
        using var machine = new TestMachine { Windows = true };
        machine.UserPath.Value = @"C:\Tools;%USERPROFILE%\bin";
        string folder = Path.Combine(machine.Root, "dmc");
        var ran = Install(machine, ["--path", folder], version: "1.2.3");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        string program = machine.ProgramFolder;
        StringAssert.EndsWith(program, Path.Combine("Programs", "dmc"));
        Assert.AreEqual("dmc install: installed dmc 1.2.3 in " + program, ran.Lines[0]);
        Assert.HasCount(3, ran.Lines);
        Assert.AreEqual(@"C:\Tools;%USERPROFILE%\bin;" + program, machine.UserPath.Value, "appended, with the variables not expanded");
        Assert.AreEqual(1, machine.UserPath.Broadcasts);

        // Installed again: already on the Path, so it is not written or broadcast again.
        Install(machine, ["--path", folder]);
        Assert.AreEqual(1, machine.UserPath.Writes);
        Assert.AreEqual(1, machine.UserPath.Broadcasts);
    }

    [TestMethod]
    public void APathThatCannotBeChangedIsReportedAfterTheFolderIsWritten()
    {
        using var machine = new TestMachine { Windows = true };
        machine.UserPath.FailWith = "the registry is locked";
        string folder = Path.Combine(machine.Root, "dmc");
        var ran = Install(machine, ["--path", folder], version: "1.2.3");
        Assert.AreEqual(1, ran.Exit);
        Assert.AreEqual($"dmc install: could not add {machine.ProgramFolder} to your PATH: the registry is locked\n", ran.Err);
        Assert.IsTrue(DmcFolder.IsDmcFolder(folder), "the folder was written first");
        CollectionAssert.Contains(ran.Lines, $"dmc install: DMC folder ready at {folder}");
    }

    /// <summary>On Linux a release is linked as <c>~/.local/bin/dmc</c>, and the player is told when that folder is not on the PATH.</summary>
    [TestMethod]
    public void OnLinuxAReleaseIsLinkedAndThePlayerIsToldWhenTheLinkFolderIsNotOnThePath()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Inconclusive("the link in ~/.local/bin is made on Linux");
        using var machine = new TestMachine { Windows = false };
        string folder = Path.Combine(machine.Root, "dmc");
        var ran = Install(machine, ["--path", folder], version: "1.2.3");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        string program = machine.ProgramFolder;
        Assert.AreEqual(Path.Combine(machine.Home, ".local", "share", "dmc"), program);
        CollectionAssert.AreEqual(new[]
        {
            $"dmc install: installed dmc 1.2.3 in {program}",
            $"dmc install: DMC folder ready at {folder}",
            "dmc install: add ~/.local/bin to your PATH (for example in ~/.profile), then run dmc play",
        }, ran.Lines);
        var link = new FileInfo(Path.Combine(machine.Home, ".local", "bin", "dmc"));
        Assert.AreEqual(Path.Combine(program, "dmc"), link.LinkTarget);

        // With ~/.local/bin on the PATH the player only has to open a terminal; and a second install replaces the link.
        machine.Environment["PATH"] = Path.Combine(machine.Home, ".local", "bin") + Path.PathSeparator + machine.Environment["PATH"];
        var again = Install(machine, ["--path", folder], version: "1.2.3");
        Assert.AreEqual("dmc install: open a new terminal, then run dmc play", again.Lines[^1]);
        Assert.AreEqual(Path.Combine(program, "dmc"), new FileInfo(link.FullName).LinkTarget);
    }

    [TestMethod]
    public void LeftoversOfAnEarlierUpdateAreRemovedQuietly()
    {
        using var machine = new TestMachine { Windows = true };
        string program = machine.ProgramFolder;
        Directory.CreateDirectory(Path.Combine(program, ".update-xyz"));
        File.WriteAllText(Path.Combine(program, "dmc.exe.old"), "old");
        File.WriteAllText(Path.Combine(program, "dmc.json"), "{ \"folders\": [] }");
        var ran = Install(machine, ["--path", Path.Combine(machine.Root, "dmc")], version: "0.0.0-dev");
        Assert.AreEqual(0, ran.Exit, ran.Err);
        Assert.IsFalse(Directory.Exists(Path.Combine(program, ".update-xyz")));
        Assert.IsFalse(File.Exists(Path.Combine(program, "dmc.exe.old")));
        Assert.IsTrue(File.Exists(Path.Combine(program, "dmc.json")), "dmc.json is never an update's leftover");
    }

    [TestMethod]
    public void TheFoldersOfEveryInstallAreRecordedOnceInOrder()
    {
        using var machine = new TestMachine { Windows = true };
        string a = Path.Combine(machine.Root, "a"), b = Path.Combine(machine.Root, "b");
        Install(machine, ["--path", a]);
        Install(machine, ["--path", b]);
        Install(machine, ["--path", a]);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(machine.ProgramFolder, "dmc.json")));
        CollectionAssert.AreEqual(new[] { a, b }, document.RootElement.GetProperty("folders").EnumerateArray().Select(f => f.GetString()).ToArray());
    }

    [TestMethod]
    public void AFreshInstallPrintsAtMostThreeLines()
    {
        using var machine = new TestMachine { Windows = true };
        var ran = Install(machine, ["--path", Path.Combine(machine.Root, "dmc")], version: "1.2.3");
        Assert.IsLessThanOrEqualTo(3, ran.Lines.Length, ran.Out);
        Assert.AreEqual("", ran.Err);
    }

    [TestMethod]
    public void InsideAClaudeCodeSessionNothingIsWrittenAndOneLineSaysWhy()
    {
        using var machine = new TestMachine { Windows = true };
        machine.Environment["CLAUDECODE"] = "1";
        string[] before = DmcFolders.SnapshotAll(machine.Root);
        var ran = Install(machine, ["--path", Path.Combine(machine.Root, "dmc"), "--force"], version: "1.2.3");
        Assert.AreEqual(1, ran.Exit);
        Assert.AreEqual("", ran.Out);
        Assert.AreEqual("dmc install: cannot run inside a Claude Code session: run it in a terminal\n", ran.Err);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(machine.Root));
        Assert.AreEqual(0, machine.UserPath.Writes);
    }

    [TestMethod]
    public void ThePublishedDmcRefusesInsideAClaudeCodeSession()
    {
        string folder = Path.Combine(DmcFolders.NewTemp(), "dmc");
        try
        {
            var result = TestEnv.Run(DmcRunner.Binary(), ["install", "--path", folder], environment: new Dictionary<string, string?> { ["CLAUDECODE"] = "1" });
            Assert.AreEqual(1, result.ExitCode, result.ToString());
            Assert.AreEqual("", result.Stdout);
            Assert.AreEqual("dmc install: cannot run inside a Claude Code session: run it in a terminal\n", result.Stderr);
            Assert.IsFalse(Directory.Exists(folder));
        }
        finally
        {
            DmcFolders.Delete(Path.GetDirectoryName(folder)!);
        }
    }

    [TestMethod]
    public void AnUnknownArgumentOrAPathWithNoFolderIsAUsageMistake()
    {
        using var machine = new TestMachine();
        var unknown = Install(machine, ["--bogus"]);
        Assert.AreEqual(2, unknown.Exit);
        Assert.AreEqual("dmc install: unrecognized arguments: --bogus\n", unknown.Err);
        var noValue = Install(machine, ["--path"]);
        Assert.AreEqual(2, noValue.Exit);
        Assert.AreEqual("dmc install: argument --path: expected one argument\n", noValue.Err);
        Assert.AreEqual(2, Install(machine, ["--path="]).Exit);
        var help = Install(machine, ["--help"]);
        Assert.AreEqual(0, help.Exit);
        StringAssert.StartsWith(help.Out, "usage: dmc install [--path FOLDER] [--force]");
        Assert.DoesNotContain("\r", help.Out);
        Assert.IsFalse(Directory.Exists(Path.Combine(machine.Home, "dmc")), "nothing is installed by a usage mistake or --help");
    }
}
