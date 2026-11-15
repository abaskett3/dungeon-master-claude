using System.Diagnostics;
using System.Text;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// Spec 013 SC-007 and plan § Performance: the scene read and the hook's canon reminder each take under half a second on
/// a canon the size of Rorn's Landing's (about 74 KB) with its archive, process start included, on the release build CI
/// makes with Native AOT. A local default build starts slower, so the local bound is looser.
/// </summary>
[TestClass]
public sealed class SceneTimingTests
{
    /// <summary>The words the generated lines are made of.</summary>
    static readonly string[] Vocabulary =
    [
        "ferry", "raft", "rope", "lantern", "smuggler", "barge", "river", "reed", "hut", "toll", "silver", "copper", "ledger",
        "night", "fog", "eel", "heron", "willow", "bridge", "ford", "market", "smith", "axle", "cart", "horse", "mule", "inn",
        "Oswin", "Kole", "Bruni", "Marta", "tower", "chapel", "priest", "oath", "debt", "Ember", "council", "wall", "crew",
    ];

    /// <summary>The bound in seconds: half a second on CI, two elsewhere.</summary>
    static double Limit => TestEnv.OnCi ? 0.5 : 2.0;

    /// <summary>A DMC folder whose fixture campaign has a canon and an archive of generated lines, People entries in the register's shape, and a scene.</summary>
    static string NewFolder()
    {
        string root = NewDmcFolderWithCampaign();
        var random = new Random(13);
        string Lines(int bytes)
        {
            var text = new StringBuilder();
            while (text.Length < bytes)
            {
                text.Append("- ");
                int words = random.Next(12, 40);
                for (int w = 0; w < words; w++)
                    text.Append(Vocabulary[random.Next(Vocabulary.Length)]).Append(' ');
                text.Append($"(s{random.Next(1, 54)}).\n");
            }
            return text.ToString();
        }
        Write(root, "story/canon.md", "# Canon\n\n## Rulings\n\n" + Lines(11_000) + "\n## People\n\n"
            + "- **Oswin Reed** (also: the ferryman): keeps the ferry (s3).\n- **Captain Emmajeen Kole** (also: Em): the captain (s1).\n"
            + "\n## Prices, debts and promises\n\n" + Lines(10_000) + "\n## Timeline\n\n" + Lines(52_000));
        Write(root, "story/canon-archive.md", "# Canon archive\n\n## Timeline\n\n" + Lines(140_000));
        Write(root, "game-state.md", GameState(["Kibil"], present: "Oswin, Kole and Bruni").Replace("a road", "the ferry at the ford"));
        return root;
    }

    /// <summary>Runs a command up to three times and returns the best time, so one slow moment on a shared runner does not fail it.</summary>
    /// <param name="run">The run, which checks its own result.</param>
    static double Best(Action run)
    {
        double best = double.MaxValue;
        for (int i = 0; i < 3 && best >= Limit; i++)
        {
            var clock = Stopwatch.StartNew();
            run();
            best = Math.Min(best, clock.Elapsed.TotalSeconds);
        }
        return best;
    }

    [TestMethod]
    public void TheSceneReadTakesUnderHalfASecond()
    {
        string root = NewFolder();
        try
        {
            double best = Best(() =>
            {
                var result = DmcRunner.BlackBox(["recall", "--folder", root, Name, "--scene"]);
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                StringAssert.EndsWith(result.StdoutLines[^1], "more lines matched)");
            });
            Assert.IsLessThan(Limit, best, $"the scene read took {best:0.000} s");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }

    [TestMethod]
    public void TheHooksCanonReminderTakesUnderHalfASecond()
    {
        string root = NewFolder();
        try
        {
            string input = new System.Text.Json.Nodes.JsonObject
            {
                ["prompt"] = "We should ask Oswin and Kole about the ferry",
                ["cwd"] = root,
                ["transcript_path"] = "",
            }.ToJsonString();
            double best = Best(() =>
            {
                var result = DmcRunner.BlackBox(["context-check"], input);
                Assert.AreEqual(0, result.ExitCode, result.Stderr);
                StringAssert.StartsWith(result.Stdout, "Canon on Oswin Reed; Captain Emmajeen Kole (");
            });
            Assert.IsLessThan(Limit, best, $"the hook took {best:0.000} s");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
