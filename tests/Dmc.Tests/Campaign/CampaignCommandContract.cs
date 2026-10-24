using Dmc.Tests.Support;
using static Dmc.Tests.Campaign.CampaignFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// What <c>dmc campaign</c> prints and returns, checked twice (see the two subclasses below): against the library code in
/// this process, and against the published <c>dmc</c> binary, the way DMC runs it. Only what writes nothing is checked here:
/// <c>show</c> reads one file, and the help text is text. Both runs find the DMC folder through <c>CLAUDE_PROJECT_DIR</c>,
/// which the test sets to its temporary folder (a DMC folder by its manifest) and which the binary inherits. The success paths of
/// <c>export</c> and <c>import</c> stay in the in-process tests that aim the library at a temporary DMC folder.
/// </summary>
public abstract class CampaignCommandContract
{
    /// <summary>A module folder no real toolkit has, so the module line does not depend on the repository's <c>modules/</c>.</summary>
    const string NoSuchModule = "no-such-module-zz9";

    /// <summary>The test's temporary working folder, which holds the sample campaign and its archive.</summary>
    string _folder = "";

    /// <summary>The archive of the sample campaign, <c>export.zip</c> in the working folder.</summary>
    string _zip = "";

    /// <summary>Run <c>dmc campaign</c> with these arguments.</summary>
    protected abstract ProcessResult Campaign(params string[] args);

    /// <summary>Makes the working folder with the sample campaign and an archive of it, with the general player options and an absent module.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        DmcFolders.MakeDmcFolder(_folder);
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", _folder);
        string campaign = WriteCampaign(_folder);
        _zip = Path.Combine(_folder, "export.zip");
        BuildArchive(_zip, PartsWith(campaign));
    }

    /// <summary>The sample campaign's parts with the general player options and a module that is not installed anywhere.</summary>
    static ArchiveParts PartsWith(string campaign)
    {
        var parts = PartsOf(campaign, generalOptions: System.Text.Encoding.UTF8.GetBytes(GeneralOptions), module: NoSuchModule);
        return parts;
    }

    /// <summary>Deletes the working folder and forgets it as the project folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
        DeleteTree(_folder);
    }

    [TestMethod]
    public void ShowPrintsTheContractLines()
    {
        var result = Campaign("show", _zip);

        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        Assert.AreEqual("", result.Stderr);
        int files = ReadFiles(Path.Combine(_folder, "adventures", CampaignName)).Count;
        string size = ExportSizeOfSample();
        CollectionAssert.AreEqual(new[]
        {
            "the-silver-egg: exported 2026-10-21 19:04 UTC by dmc 0.0.0-dev, format 1",
            $"{files} files, {size}, plus the general player options",
            $"module: {NoSuchModule} (not in modules/)",
        }, result.StdoutLines);
        Assert.DoesNotContain("\r", result.Stdout, "output is LF on every system");
    }

    /// <summary>The sample campaign's size as <c>show</c> says it: computed here so the test does not call the code that formats it.</summary>
    string ExportSizeOfSample()
    {
        long bytes = ReadFiles(Path.Combine(_folder, "adventures", CampaignName)).Values.Sum(b => (long)b.Length);
        Assert.IsLessThan(1024 * 1024, bytes, "the sample is small");
        return bytes < 1024 ? $"{bytes} bytes" : (bytes / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
    }

    [TestMethod]
    public void ShowWritesNothing()
    {
        string[] before = Snapshot(_folder);
        Campaign("show", _zip);
        CollectionAssert.AreEqual(before, Snapshot(_folder));
    }

    [TestMethod]
    public void ShowOfAFileThatIsNotAnExportIsOneLineAndExitOne()
    {
        string text = Path.Combine(_folder, "not.zip");
        File.WriteAllText(text, "hello");
        var result = Campaign("show", text);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual($"dmc campaign: {text} is not a dmc campaign export", result.Stderr.TrimEnd());
    }

    [TestMethod]
    public void HelpListsAllThreeVerbsAndTheOptions()
    {
        foreach (string[] args in new[] { new[] { "--help" }, ["export", "--help"], ["import", "--help"], ["show", "--help"], ["-h"] })
        {
            var result = Campaign(args);
            Assert.AreEqual(0, result.ExitCode, string.Join(' ', args) + ": " + result.Stderr);
            Assert.DoesNotContain("\r", result.Stdout);
            foreach (string word in new[] { "dmc campaign export NAME [--out PATH] [--force]", "dmc campaign import FILE [--as NAME]", "dmc campaign show FILE", "--out", "--force", "--as" })
                StringAssert.Contains(result.Stdout, word, $"dmc campaign {string.Join(' ', args)}");
        }
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("campaign ", StringComparison.Ordinal)
            && l.Contains("export or import a whole campaign as one file")));
    }
}

/// <summary>The campaign tests against the library code, in this process.</summary>
[TestClass]
public sealed class CampaignInProcessTests : CampaignCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> in this process.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.InProcess(["campaign", .. args]);
}

/// <summary>The campaign tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class CampaignBlackBoxTests : CampaignCommandContract
{
    /// <summary>Runs <c>dmc campaign</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Campaign(params string[] args) => DmcRunner.BlackBox(["campaign", .. args]);
}
