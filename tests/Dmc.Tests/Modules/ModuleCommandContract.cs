using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Modules.ModuleFixtures;

namespace Dmc.Tests.Modules;

/// <summary>
/// What <c>dmc module check</c> prints and returns (spec 012 contracts/dmc-module-check.md), checked twice (see the two
/// subclasses below): against the library code in this process, and against the published binary. Every test checks
/// that the run changed nothing in the DMC folder.
/// </summary>
public abstract class ModuleCommandContract
{
    /// <summary>The test's temporary DMC folder.</summary>
    string _root = "";

    /// <summary>A snapshot of the DMC folder before the command ran.</summary>
    string[] _before = [];

    /// <summary>Run <c>dmc module</c> with these arguments.</summary>
    protected abstract ProcessResult Module(params string[] args);

    /// <summary>Run <c>dmc module check</c> on the fixture module with <c>--folder</c>.</summary>
    ProcessResult Check(string name = ModuleFixtures.Module) => Module("check", name, "--folder", _root);

    /// <summary>Makes a DMC folder with a sound module and takes its snapshot.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewFolder();
        WriteSoundModule(_root, chapters: 6);
        _before = DmcFolders.SnapshotAll(_root);
    }

    /// <summary>Checks the command wrote nothing (FR-009, FR-012), then deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            CollectionAssert.AreEqual(_before, DmcFolders.SnapshotAll(_root), "dmc module check must not write, move or delete anything");
        }
        finally
        {
            DmcFolders.Delete(_root);
        }
    }

    /// <summary>Changes the fixture before the run, then takes the snapshot the cleanup compares against.</summary>
    /// <param name="change">What to change.</param>
    void Arrange(Action change)
    {
        change();
        _before = DmcFolders.SnapshotAll(_root);
    }

    [TestMethod]
    public void ASoundModulePrintsNothingAndExitsZero()
    {
        var result = Check();
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("", result.Stderr);
    }

    [TestMethod]
    public void TheModulesPrefixIsUnderstood() => Assert.AreEqual(0, Check($"modules/{ModuleFixtures.Module}").ExitCode);

    [TestMethod]
    public void EachPlantedProblemIsOneLineAndTheExitIsZero()
    {
        Arrange(() =>
        {
            Write(_root, $"modules/{ModuleFixtures.Module}/monsters-big.md",
                "# Big\n\n" + string.Concat(Enumerable.Range(1, 57).Select(m => $"## Beast {m}\n\n{StatBlock("b", m)}\n")));
            Write(_root, $"modules/{ModuleFixtures.Module}/chapters/07-planted.md",
                "# Ch 7\n\n## 1. Long Hall\n\n" + Prose(20_000) + "## 2. Pen\n\n" + StatBlock("x", 1) + StatBlock("y", 2)
                + "## 3. Door\n\nA.\n\n## 3. Door\n\nB.\n\n## 4. Fate\n\n### PLAYER-SAFE\n\nC.\n\n## 5. Fate\n\n### PLAYER-SAFE\n\nD.\n");
        });
        var result = Check();
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        var lines = result.StdoutLines;
        Assert.HasCount(4, lines, result.Stdout);
        StringAssert.Contains(lines[0], "07-planted.md:3 \"1. Long Hall\"");
        StringAssert.Contains(lines[1], "\"2. Pen\" holds 2 stat blocks");
        StringAssert.Contains(lines[2], "\"3. Door\" repeats line");
        StringAssert.StartsWith(lines[3], $"modules/{ModuleFixtures.Module}/monsters-big.md ");
    }

    [TestMethod]
    public void TwelveLinesAtMostTheLastSayingHowManyMore()
    {
        Arrange(() => Write(_root, $"modules/{ModuleFixtures.Module}/dups.md",
            "# D\n\n" + string.Concat(Enumerable.Repeat("## Same\n\nA.\n\n", 16))));
        var lines = Check().StdoutLines;
        Assert.HasCount(Budgets.CheckLines, lines);
        Assert.AreEqual("... and 4 more", lines[^1]);
    }

    [TestMethod]
    public void AnUnknownModuleFailsInOneLineWithExitOne()
    {
        var result = Check("nope");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc module: no module nope (modules: {ModuleFixtures.Module})\n", result.Stderr);
        Assert.AreEqual("", result.Stdout);
    }

    [TestMethod]
    public void ADotNameIsRefused()
    {
        var result = Check(".staging");
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.StartsWith(result.Stderr, "dmc module: .staging is not a module");
    }

    [TestMethod]
    public void UsageMistakesAreOneLineWithExitTwo()
    {
        foreach (var args in new string[][] { [], ["check"], ["fix", "x"], ["check", "a", "b"], ["check", "a", "--nope"], ["check", "a", "--folder"] })
        {
            var result = Module(args);
            Assert.AreEqual(2, result.ExitCode, string.Join(' ', args));
            Assert.AreEqual(1, result.StderrLines.Length, result.Stderr);
            StringAssert.StartsWith(result.Stderr, "dmc module: ");
        }
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Module("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.StartsWith(result.Stdout, "usage: dmc module check NAME [--folder PATH]");
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("module ", StringComparison.Ordinal)
                                                  && l.Contains("dmc module check NAME")));
    }
}

/// <summary>The module tests against the library code, in this process.</summary>
[TestClass]
public sealed class ModuleInProcessTests : ModuleCommandContract
{
    /// <summary>Runs <c>dmc module</c> in this process.</summary>
    protected override ProcessResult Module(params string[] args) => DmcRunner.InProcess(["module", .. args]);
}

/// <summary>The module tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class ModuleBlackBoxTests : ModuleCommandContract
{
    /// <summary>Runs <c>dmc module</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Module(params string[] args) => DmcRunner.BlackBox(["module", .. args]);
}
