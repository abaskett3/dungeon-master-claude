using Dmc.Tests.Size;
using Dmc.Tests.Support;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// What <c>dmc recall NAME --scene</c> prints and returns (spec 013 contracts/dmc-recall-scene.md), in this process and
/// through the published binary: the small-canon line, the campaign argument, usage mistakes, a missing canon, and that
/// the run writes nothing.
/// </summary>
public abstract class RecallSceneContract
{
    /// <summary>The test's DMC folder, with the fixture campaign (active).</summary>
    string _root = "";

    /// <summary>A hash of the DMC folder before the test ran the command.</summary>
    string _before = "";

    /// <summary>Run <c>dmc recall</c> with these arguments.</summary>
    protected abstract ProcessResult Recall(params string[] args);

    /// <summary>Makes the DMC folder and points <c>CLAUDE_PROJECT_DIR</c> at it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _root = SizeFixtures.NewDmcFolderWithCampaign();
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _root);
        _before = SizeFixtures.Hash(_root);
    }

    /// <summary>Checks the command wrote nothing, then deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            Assert.AreEqual(_before, SizeFixtures.Hash(_root), "dmc recall --scene must not write anything");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
            DmcFolders.Delete(_root);
        }
    }

    [TestMethod]
    public void TheActiveCampaignsSmallCanonIsToBeReadWhole()
    {
        var result = Recall("--scene");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("story/canon.md is small: read it whole\n", result.Stdout);
    }

    [TestMethod]
    public void ANamedCampaignIsRead()
    {
        var result = Recall(SizeFixtures.Name, "--scene");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("story/canon.md is small: read it whole\n", result.Stdout);
    }

    [TestMethod]
    public void WordsWithSceneAreAUsageMistake()
    {
        var result = Recall(SizeFixtures.Name, "ferry", "--scene");
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc recall: --scene takes no words\n", result.Stderr);
    }

    [TestMethod]
    public void ACampaignWithNoCanonFailsInOneLine()
    {
        string canon = Path.Combine(SizeFixtures.CampaignFolder(_root), "story", "canon.md");
        File.Delete(canon);
        _before = SizeFixtures.Hash(_root);
        var result = Recall("--scene");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual($"dmc recall: no story/canon.md in {SizeFixtures.Name}\n", result.Stderr);
    }

    [TestMethod]
    public void HelpShowsTheSceneForm()
    {
        StringAssert.Contains(Recall("--help").Stdout, "dmc recall [NAME] --scene [--folder PATH]");
    }
}

/// <summary>The scene read's tests against the library code, in this process.</summary>
[TestClass]
public sealed class RecallSceneInProcessTests : RecallSceneContract
{
    /// <summary>Runs <c>dmc recall</c> in this process.</summary>
    protected override ProcessResult Recall(params string[] args) => DmcRunner.InProcess(["recall", .. args]);
}

/// <summary>The scene read's tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class RecallSceneBlackBoxTests : RecallSceneContract
{
    /// <summary>Runs <c>dmc recall</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Recall(params string[] args) => DmcRunner.BlackBox(["recall", .. args]);
}
