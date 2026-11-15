using System.Diagnostics;
using Dmc.Tests.Support;
using static Dmc.Tests.Modules.ModuleFixtures;

namespace Dmc.Tests.Modules;

/// <summary>
/// Spec 012 SC-006: <c>dmc module check</c> over about 1.5 MB of module markdown (the size of the measured 638-page
/// module's digests) finishes in under a second with the release build. CI builds <c>dmc</c> with Native AOT, so the bound
/// there is one second, process start included; a local default build starts slower, so the local bound is looser, as
/// <see cref="Retrieval.RecallTimingTests"/> does.
/// </summary>
[TestClass]
public sealed class ModuleCheckTimingTests
{
    /// <summary>A check of a module of about 1.5 MB takes under a second on CI, the best of three runs (3 seconds on other machines).</summary>
    [TestMethod]
    public void ACheckOfALargeModuleTakesUnderASecond()
    {
        string root = NewFolder();
        try
        {
            WriteSoundModule(root, chapters: 38, hubAreas: 8, monsters: 50, chapterBytes: 11500);
            long written = Directory.EnumerateFiles(Path.Combine(root, "modules", Module), "*.md", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}source{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Sum(f => new FileInfo(f).Length);
            Assert.IsGreaterThan(1_400_000, written, "the fixture is about 1.5 MB");

            double limit = TestEnv.OnCi ? 1.0 : 3.0;
            double best = double.MaxValue;
            for (int run = 0; run < 3; run++)
            {
                var clock = Stopwatch.StartNew();
                var result = DmcRunner.BlackBox(["module", "check", Module, "--folder", root]);
                clock.Stop();
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                best = Math.Min(best, clock.Elapsed.TotalSeconds);
                if (best < limit)
                    break; // one run under the limit is enough
            }
            Assert.IsLessThan(limit, best, $"the check over {written / 1_000_000.0:0.0} MB took {best:0.00} s at best of three");
            Console.WriteLine($"module check over {written / 1_000_000.0:0.0} MB: {best * 1000:0} ms at best of three");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
