using System.Text;
using Dmc.Commands;
using Dmc.Maps;
using Dmc.Tests.Support;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary>
/// <c>dmc redraw</c>, the Stop hook's command, against a temporary toolkit folder with one active campaign: it redraws
/// the map file the player's page shows only when that file changed, says nothing otherwise, reports a refused file
/// with exit code 2, and never loops (<c>stop_hook_active</c>). The hook as Claude Code runs it, with the published
/// binary, is tested in <c>ToolkitTests</c>.
/// </summary>
[TestClass]
public sealed class RedrawTests
{
    /// <summary>The temporary toolkit root, which holds <c>adventures/</c>.</summary>
    string _root = "";

    /// <summary>The active campaign's <c>maps/</c> folder.</summary>
    string _maps = "";

    /// <summary>Creates a toolkit folder with an active campaign called <c>test</c> and the goblin hall and standard scene in its <c>maps/</c>.</summary>
    [TestInitialize]
    public void MakeRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        _maps = Path.Combine(_root, "adventures", "test", "maps");
        Directory.CreateDirectory(_maps);
        File.WriteAllText(Path.Combine(_root, "adventures", ".active-campaign"), "test\n");
        WriteFile(_maps, "goblin-hall.json", Place());
        WriteFile(_maps, "live.json", Scene());
    }

    /// <summary>Deletes the toolkit folder.</summary>
    [TestCleanup]
    public void RemoveRoot() => Directory.Delete(_root, recursive: true);

    /// <summary>Run <c>dmc redraw</c> in this process against the temporary toolkit, with this hook input.</summary>
    ProcessResult Redraw(string stdin = "{\"stop_hook_active\": false}") =>
        DmcRunner.InProcess(["redraw"], stdin, [new RedrawCommand(_root)]);

    /// <summary>The path of a file in <c>maps/</c>.</summary>
    string InMaps(string name) => Path.Combine(_maps, name);

    /// <summary>The text of <c>live-data.js</c>, the picture the page shows.</summary>
    string Data() => File.ReadAllText(InMaps("live-data.js"));

    /// <summary>Draws a file as <c>dmc map</c> does, so the page and the record exist.</summary>
    void Draw(string name) => Assert.AreEqual(0, DmcRunner.InProcess(["map", InMaps(name)]).ExitCode);

    /// <summary>
    /// Rewrites a map file after the drawing, as an edit would: the picture is dated ten seconds back first, so that the
    /// file is newer than it without being dated in the future (a redraw has to be able to catch up with it).
    /// </summary>
    void Edit(string name, object data)
    {
        File.SetLastWriteTimeUtc(InMaps("live-data.js"), DateTime.UtcNow.AddSeconds(-10));
        WriteFile(_maps, name, data);
    }

    /// <summary>The standard scene with this title, so that a redraw shows in the data.</summary>
    static PyDict Moved(string title)
    {
        var scene = Scene();
        scene["title"] = title;
        return scene;
    }

    [TestMethod]
    public void AMapDrawnByDmcMapIsRecordedSoRedrawKnowsWhichFileThePageShows()
    {
        Draw("live.json");
        Assert.AreEqual("live.json", File.ReadAllText(InMaps("live-source.txt")).Trim());
        WriteFile(_maps, "test-bout.json", Scene());
        Draw("test-bout.json");
        Assert.AreEqual("test-bout.json", File.ReadAllText(InMaps("live-source.txt")).Trim());
        // --measure draws nothing and records nothing.
        Assert.AreEqual(0, DmcRunner.InProcess(["map", InMaps("live.json"), "--measure", "B2", "E2"]).ExitCode);
        Assert.AreEqual("test-bout.json", File.ReadAllText(InMaps("live-source.txt")).Trim());
    }

    [TestMethod]
    public void NothingChangedMeansNoOutputAndNoDrawing()
    {
        Draw("live.json");
        string before = Data();
        var stamp = File.GetLastWriteTimeUtc(InMaps("live-data.js"));
        var result = Redraw();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout + result.Stderr);
        Assert.AreEqual(before, Data());
        Assert.AreEqual(stamp, File.GetLastWriteTimeUtc(InMaps("live-data.js")));
    }

    [TestMethod]
    public void AChangedFileIsDrawnAgainInSilence()
    {
        Draw("live.json");
        Edit("live.json", Moved("After the move"));
        var result = Redraw();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout + result.Stderr);
        StringAssert.Contains(Data(), "After the move");
        // Drawn: the next call finds nothing to do.
        string drawn = Data();
        Assert.AreEqual("", Redraw().Stdout);
        Assert.AreEqual(drawn, Data());
    }

    [TestMethod]
    public void RedrawFollowsTheFileThePageLastShowedNotLiveJson()
    {
        WriteFile(_maps, "test-bout.json", Moved("Bout"));
        Draw("test-bout.json");
        // An unrelated, newer live.json is left alone; the bout file is the page's.
        Edit("live.json", Moved("Not the page"));
        Assert.AreEqual("", Redraw().Stdout);
        StringAssert.Contains(Data(), "Bout");
        Edit("test-bout.json", Moved("Bout, round 2"));
        Assert.AreEqual(0, Redraw().ExitCode);
        StringAssert.Contains(Data(), "Bout, round 2");
        Assert.AreEqual("test-bout.json", File.ReadAllText(InMaps("live-source.txt")).Trim());
    }

    [TestMethod]
    public void ANewerBasePlaceDrawsTheSceneAgain()
    {
        Draw("live.json");
        var place = Place();
        place["title"] = "Goblin hall, flooded";
        Edit("goblin-hall.json", place);
        Assert.AreEqual(0, Redraw().ExitCode);
        StringAssert.Contains(Data(), "Goblin hall, flooded");
    }

    [TestMethod]
    public void APageDrawnBeforeTheRecordExistedFollowsLiveJson()
    {
        Draw("live.json");
        File.Delete(InMaps("live-source.txt"));
        Edit("live.json", Moved("Old page"));
        Assert.AreEqual(0, Redraw().ExitCode);
        StringAssert.Contains(Data(), "Old page");
    }

    [TestMethod]
    public void ARecordThatPointsOutsideMapsIsIgnored()
    {
        Draw("live.json");
        File.WriteAllText(InMaps("live-source.txt"), "..\\..\\outside.json\n");
        Edit("live.json", Moved("Stayed inside"));
        Assert.AreEqual(0, Redraw().ExitCode);
        StringAssert.Contains(Data(), "Stayed inside");
    }

    [TestMethod]
    public void ARefusedFileIsOneLineOnStderrAndExitCodeTwo()
    {
        Draw("live.json");
        string before = Data();
        Edit("live.json", With(Scene(), "tokens", Arr(Obj(("id", "@"), ("name", "Thorin"), ("at", "A1")))));
        var result = Redraw();
        Assert.AreEqual(2, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.HasCount(1, result.StderrLines, result.Stderr);
        StringAssert.StartsWith(result.Stderr, "dmc redraw: live.json was not drawn");
        StringAssert.Contains(result.Stderr, "is on A1, which is a wall");
        Assert.AreEqual(before, Data(), "a refused file draws nothing");
        // Still refused and still changed: the next reply reports it again.
        Assert.AreEqual(2, Redraw().ExitCode);
    }

    [TestMethod]
    public void AStopHookThatIsAlreadyActiveNeverBlocksAgain()
    {
        Draw("live.json");
        Edit("live.json", With(Scene(), "tokens", Arr(Obj(("id", "@"), ("name", "Thorin"), ("at", "A1")))));
        var refused = Redraw("{\"stop_hook_active\": true}");
        Assert.AreEqual(0, refused.ExitCode, refused.ToString());
        Assert.AreEqual("", refused.Stdout + refused.Stderr);

        // The corrected file is drawn even then: that is the reply after the one-line report.
        Edit("live.json", Moved("Corrected"));
        var fixedUp = Redraw("{\"stop_hook_active\": true}");
        Assert.AreEqual(0, fixedUp.ExitCode, fixedUp.ToString());
        StringAssert.Contains(Data(), "Corrected");
    }

    [TestMethod]
    public void NoActiveCampaignOrNoPageYetMeansSilenceAndNoFiles()
    {
        // A campaign with no page yet: DMC has not shown a map, so nothing is drawn behind its back.
        var none = Redraw();
        Assert.AreEqual(0, none.ExitCode, none.ToString());
        Assert.AreEqual("", none.Stdout + none.Stderr);
        Assert.IsFalse(File.Exists(InMaps("live-data.js")));

        Draw("live.json");
        File.Delete(Path.Combine(_root, "adventures", ".active-campaign"));
        Edit("live.json", Moved("Unseen"));
        var noCampaign = Redraw();
        Assert.AreEqual(0, noCampaign.ExitCode, noCampaign.ToString());
        Assert.AreEqual("", noCampaign.Stdout + noCampaign.Stderr);
        Assert.DoesNotContain("Unseen", Data());

        // A name that is a path, an empty file, and a campaign folder that does not exist.
        foreach (string name in new[] { "../test", "", "\n", "missing" })
        {
            File.WriteAllText(Path.Combine(_root, "adventures", ".active-campaign"), name);
            Assert.AreEqual("", Redraw().Stdout + Redraw().Stderr, name);
        }
        Assert.DoesNotContain("Unseen", Data());
    }

    [TestMethod]
    public void BadInputOnStdinIsNotAnError()
    {
        Draw("live.json");
        Edit("live.json", Moved("Any input"));
        foreach (string input in new[] { "", "not json", "[]", "{\"stop_hook_active\": \"yes\"}" })
        {
            var result = Redraw(input);
            Assert.AreEqual(0, result.ExitCode, $"{input}: {result}");
            Assert.AreEqual("", result.Stdout + result.Stderr, input);
        }
        StringAssert.Contains(Data(), "Any input");
    }

    [TestMethod]
    public void StopHookActiveIsReadFromTheJson()
    {
        Assert.IsTrue(RedrawCommand.StopHookActive("{\"stop_hook_active\": true, \"cwd\": \"x\"}"));
        foreach (string input in new[] { "{\"stop_hook_active\": false}", "{}", "", "  ", "[]", "[true]", "\"stop_hook_active\"", "{\"stop_hook_active\": 1}", "{" })
            Assert.IsFalse(RedrawCommand.StopHookActive(input), input);
    }

    /// <summary>A map with this one key set (the Python <c>dict(PLACE, key=...)</c>).</summary>
    static PyDict With(PyDict map, string key, object value)
    {
        map[key] = value;
        return map;
    }
}
