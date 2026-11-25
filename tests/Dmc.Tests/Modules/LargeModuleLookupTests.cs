using Dmc.Tests.Support;
using static Dmc.Tests.Modules.ModuleFixtures;

namespace Dmc.Tests.Modules;

/// <summary>
/// Spec 012 SC-003: in a module whose monster file is over the Read cap mark, as the measured 37k file is, a lookup of one
/// stat block still prints that one block, about 650 tokens, unchanged by Phase C.
/// </summary>
[TestClass]
public sealed class LargeModuleLookupTests
{
    [TestMethod]
    public void OneStatBlockFromAThirtySevenThousandTokenFileIsAboutSixHundredFiftyTokens()
    {
        string root = NewFolder();
        try
        {
            WriteSoundModule(root, chapters: 2, monsters: 57);
            Assert.IsGreaterThan(140_000L, new FileInfo(Path.Combine(root, "modules", Module, "monsters.md")).Length, "about 59k tokens");
            var result = DmcRunner.BlackBox(["lookup", "--folder", root, Module, "Marsh Beast 31"]);
            Assert.AreEqual(0, result.ExitCode, result.Stderr);
            Assert.AreEqual($"modules/{Module}/monsters.md", result.StdoutLines[0].Split(':')[0]);
            long tokens = 2 * (result.Stdout.Length / 5); // dmc size's measure (spec 021): 2 tokens for 5 characters
            Assert.IsTrue(tokens is > 880 and < 1200, $"{tokens} tokens");
            StringAssert.Contains(result.Stdout, "## Marsh Beast 31 (PDF 331)");
            Assert.DoesNotContain("Marsh Beast 32", result.Stdout);
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
