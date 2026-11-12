using System.Diagnostics;
using System.Text;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// Spec 009 SC-004 and spec 011 SC-006: a recall over a few megabytes of markdown (a chain of three campaigns, a side
/// campaign, a same-world campaign, a digest and a large module, the size of the measured play folder) finishes in under
/// a second with the release build. CI builds <c>dmc</c> with Native AOT, so the bound there is one second, process start
/// included; a local default build starts slower, so the local bound is looser.
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

    /// <summary>
    /// A recall over a linked set of about 3 MB (a chain of three, a side and a same-world campaign, a digest and a
    /// large module) takes under a second on CI, the best of three runs (spec 011 SC-006; 3 seconds on other machines).
    /// </summary>
    [TestMethod]
    public void ARecallOverAFewMegabytesTakesUnderASecond()
    {
        string root = NewLinkedSet();
        try
        {
            // The first campaign of the chain was played on a large module.
            Write(root, $"adventures/{First}/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/" + Module + "/`\n");
            Write(root, $"modules/{Module}/index.md", "# Fen Crossing\n");
            var random = new Random(179);
            int written = 0;
            string[] folders =
            [
                $"adventures/{Last}/story/sessions", $"adventures/{Middle}/story/sessions", $"adventures/{First}/story/sessions",
                $"adventures/{SideTale}/story/sessions", $"adventures/{FarTale}/story/sessions", $"adventures/{Last}/digests/{Middle}",
                $"modules/{Module}/chapters",
            ];
            foreach (string folder in folders)
            {
                for (int file = 0; file < 9; file++)
                {
                    var text = new StringBuilder($"# File {file}\n\n### Scene 1: Somewhere\n\n");
                    while (text.Length < 50_000)
                    {
                        int words = random.Next(12, 40);
                        for (int w = 0; w < words; w++)
                            text.Append(Vocabulary[random.Next(Vocabulary.Length)]).Append(w % 7 == 6 ? ". " : " ");
                        text.Append(random.Next(1000)).Append('\n');
                    }
                    string name = folder.EndsWith("sessions", StringComparison.Ordinal) ? $"session-{file + 100:000}.md"
                        : folder.Contains("/digests/", StringComparison.Ordinal) ? $"people-{file + 1}.md" : $"{file + 10:00}-generated.md";
                    Write(root, $"{folder}/{name}", text.ToString());
                    written += text.Length;
                }
            }
            Assert.IsGreaterThan(2_900_000, written, "the fixture is about 3 MB");

            // Up to three runs, stopping at the first under the limit, so one slow moment on a shared runner (a cold disk cache, a virus scan of the new
            // files) does not fail a change that never touched retrieval.
            double limit = TestEnv.OnCi ? 1.0 : 3.0;
            double best = double.MaxValue;
            for (int run = 0; run < 3; run++)
            {
                var clock = Stopwatch.StartNew();
                var result = DmcRunner.BlackBox(["recall", "--folder", root, Last, "smuggler", "lantern", "ledger", "night"]);
                clock.Stop();
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                best = Math.Min(best, clock.Elapsed.TotalSeconds);
                if (best < limit)
                    break; // one run under the limit is enough
            }

            Assert.IsLessThan(limit, best, $"recall over {written / 1_000_000.0:0.0} MB took {best:0.00} s at best of three");
            Console.WriteLine($"recall over {written / 1_000_000.0:0.0} MB: {best * 1000:0} ms at best of three");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
