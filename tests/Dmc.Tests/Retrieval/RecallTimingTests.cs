using System.Diagnostics;
using System.Text;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// SC-004: a recall over a few megabytes of markdown (two campaigns and a module, the size of the measured play folder)
/// finishes in under a second with the release build. CI builds <c>dmc</c> with Native AOT, so the bound there is one
/// second, process start included; a local default build starts slower, so the local bound is looser.
/// </summary>
[TestClass]
public sealed class RecallTimingTests
{
    /// <summary>The words the generated lines are made of, so the index has a realistic vocabulary.</summary>
    static readonly string[] Vocabulary =
    [
        "ferry", "raft", "rope", "lantern", "smuggler", "barge", "river", "reed", "hut", "toll", "silver", "copper", "ledger",
        "night", "fog", "eel", "heron", "willow", "bridge", "ford", "market", "smith", "axle", "cart", "horse", "mule", "inn",
        "cellar", "key", "letter", "seal", "captain", "guard", "watch", "bell", "tower", "chapel", "priest", "oath", "debt",
    ];

    [TestMethod]
    public void ARecallOverAFewMegabytesTakesUnderASecond()
    {
        string root = NewChain();
        try
        {
            var random = new Random(179);
            int written = 0;
            foreach (string folder in new[] { $"adventures/{Sequel}/story/sessions", $"adventures/{Predecessor}/story/sessions", $"modules/{Module}/chapters" })
            {
                for (int file = 0; file < 20; file++)
                {
                    var text = new StringBuilder($"# File {file}\n\n### Scene 1: Somewhere\n\n");
                    while (text.Length < 50_000)
                    {
                        int words = random.Next(12, 40);
                        for (int w = 0; w < words; w++)
                            text.Append(Vocabulary[random.Next(Vocabulary.Length)]).Append(w % 7 == 6 ? ". " : " ");
                        text.Append(random.Next(1000)).Append('\n');
                    }
                    string name = folder.EndsWith("sessions", StringComparison.Ordinal) ? $"session-{file + 100:000}.md" : $"{file + 10:00}-generated.md";
                    Write(root, $"{folder}/{name}", text.ToString());
                    written += text.Length;
                }
            }
            Assert.IsGreaterThan(2_900_000, written, "the fixture is about 3 MB");

            // The best of three runs, so one slow moment on a shared runner (a cold disk cache, a virus scan of the new
            // files) does not fail a change that never touched retrieval.
            double best = double.MaxValue;
            for (int run = 0; run < 3; run++)
            {
                var clock = Stopwatch.StartNew();
                var result = DmcRunner.BlackBox(["recall", "--folder", root, Sequel, "smuggler", "lantern", "ledger", "night"]);
                clock.Stop();
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                best = Math.Min(best, clock.Elapsed.TotalSeconds);
            }

            double limit = TestEnv.OnCi ? 1.0 : 3.0;
            Assert.IsLessThan(limit, best, $"recall over {written / 1_000_000.0:0.0} MB took {best:0.00} s at best of three");
            Console.WriteLine($"recall over {written / 1_000_000.0:0.0} MB: {best * 1000:0} ms at best of three");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
