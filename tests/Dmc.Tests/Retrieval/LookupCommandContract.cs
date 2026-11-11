using Dmc.Tests.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// What <c>dmc lookup</c> prints and returns (contracts/dmc-lookup.md), checked twice (see the two subclasses below):
/// against the library code in this process, and against the published <c>dmc</c> binary, the way DMC runs it. Both
/// find the DMC folder through <c>CLAUDE_PROJECT_DIR</c> or <c>--folder</c>. Every test checks that the run changed
/// nothing.
/// </summary>
public abstract class LookupCommandContract
{
    /// <summary>The test's temporary DMC folder, with the fixture chain.</summary>
    string _root = "";

    /// <summary>A hash of the DMC folder before the test ran the command.</summary>
    string _before = "";

    /// <summary>Run <c>dmc lookup</c> with these arguments.</summary>
    protected abstract ProcessResult Lookup(params string[] args);

    /// <summary>Makes the DMC folder and points <c>CLAUDE_PROJECT_DIR</c> at it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = NewChain();
        PdfRules(_root);
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _root);
        _before = SizeFixtures.Hash(_root);
    }

    /// <summary>Checks the command wrote nothing, then deletes the folder and forgets it as the project folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            Assert.AreEqual(_before, SizeFixtures.Hash(_root), "dmc lookup must not write, move or delete anything");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
            DmcFolders.Delete(_root);
        }
    }

    [TestMethod]
    public void ASectionIsPrintedUnderItsHeaderLineWithExitZero()
    {
        var result = Lookup(Module, "giant", "eel");

        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stderr);
        Assert.AreEqual($"modules/{Module}/monsters.md:10-15\n## GIANT EEL (PDF 13)\n\n*Large Beast, Unaligned*\n\n- **AC** 13\n- **HP** 52 (7d10 + 14)\n", result.Stdout);
    }

    [TestMethod]
    public void AListOfMatchesExitsZero()
    {
        var result = Lookup(Sequel, "Playing them");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.HasCount(2, result.StdoutLines);
    }

    [TestMethod]
    public void TheFolderOptionIsUsedInsteadOfTheProjectFolder()
    {
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
        var result = Lookup("--folder", _root, "rules", "Fireball");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("rules: \"Fireball\" is on PDF pages 129, 130 (read them in rules/srd.pdf)\n", result.Stdout);
    }

    [TestMethod]
    public void NoHeadingIsOneLineOnStderrWithExitOne()
    {
        var result = Lookup(Module, "zebra");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual($"dmc lookup: no heading \"zebra\" in {Module}\n", result.Stderr);
    }

    [TestMethod]
    public void AMissingHeadingIsAUsageMistake()
    {
        var result = Lookup(Module);
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc lookup: the following arguments are required: HEADING\n", result.Stderr);
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Lookup("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.StartsWith(result.Stdout, "usage: dmc lookup SOURCE HEADING... [--folder PATH]");
        Assert.DoesNotContain("\r", result.Stdout);
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("lookup ", StringComparison.Ordinal) && l.Contains("by its heading")));
    }
}

/// <summary>The lookup tests against the library code, in this process.</summary>
[TestClass]
public sealed class LookupInProcessTests : LookupCommandContract
{
    /// <summary>Runs <c>dmc lookup</c> in this process.</summary>
    protected override ProcessResult Lookup(params string[] args) => DmcRunner.InProcess(["lookup", .. args]);
}

/// <summary>The lookup tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class LookupBlackBoxTests : LookupCommandContract
{
    /// <summary>Runs <c>dmc lookup</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Lookup(params string[] args) => DmcRunner.BlackBox(["lookup", .. args]);
}
