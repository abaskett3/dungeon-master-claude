using Dmc.Commands;
using Dmc.Maps;
using Dmc.Tests.Support;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary>
/// <c>dmc redraw</c>, the Stop hook's command, against a temporary toolkit folder with one active campaign: it redraws
/// the map the player's page shows only when it changed, says nothing otherwise, reports a refused file with exit code
/// 2, and never loops (<c>stop_hook_active</c>). The hook as Claude Code runs it, with the published binary, is tested
/// in <c>ToolkitTests</c>.
/// </summary>
[TestClass]
public sealed class RedrawTests
{
    /// <summary>The temporary toolkit root, which holds <c>adventures/</c>.</summary>
    string _root = "";

    /// <summary>The active campaign's <c>maps/</c> folder.</summary>
    string _maps = "";

    /// <summary>
    /// Creates a toolkit folder with an active campaign called <c>test</c> and the goblin hall and standard scene in its
    /// <c>maps/</c>, both dated a minute back, so that they are older than any drawing the tests make.
    /// </summary>
    [TestInitialize]
    public void MakeRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        _maps = Path.Combine(_root, "adventures", "test", "maps");
        Directory.CreateDirectory(_maps);
        File.WriteAllText(Path.Combine(_root, "adventures", ".active-campaign"), "test\n");
        WriteFile(_maps, "goblin-hall.json", Place());
        WriteFile(_maps, "live.json", Scene());
        foreach (string name in new[] { "goblin-hall.json", "live.json" })
            File.SetLastWriteTimeUtc(InMaps(name), DateTime.UtcNow.AddSeconds(-60));
    }

    /// <summary>Deletes the toolkit folder.</summary>
    [TestCleanup]
    public void RemoveRoot() => Directory.Delete(_root, recursive: true);

    /// <summary>Run <c>dmc redraw</c> in this process against the temporary toolkit, with this hook input.</summary>
    ProcessResult Redraw(string stdin = "{\"stop_hook_active\": false}") =>
        DmcRunner.InProcess(["redraw"], stdin, [new RedrawCommand(_root)]);

    /// <summary>Run <c>dmc redraw</c> and check, from that one run, that it exited 0 and printed nothing at all.</summary>
    void AssertSilent(string stdin = "{\"stop_hook_active\": false}", string? because = null)
    {
        var result = Redraw(stdin);
        Assert.AreEqual(0, result.ExitCode, $"{because}: {result}");
        Assert.AreEqual("", result.Stdout + result.Stderr, because);
    }

    /// <summary>The path of a file in <c>maps/</c>.</summary>
    string InMaps(string name) => Path.Combine(_maps, name);

    /// <summary>The text of <c>live-data.js</c>, the picture the page shows.</summary>
    string Data() => File.ReadAllText(InMaps("live-data.js"));

    /// <summary>The lines of <c>live-source.txt</c>.</summary>
    string[] Record() => File.ReadAllLines(InMaps("live-source.txt")).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

    /// <summary>Draws a file as <c>dmc map</c> does, so the page and the record exist.</summary>
    void Draw(string name, params string[] more) =>
        Assert.AreEqual(0, DmcRunner.InProcess(["map", InMaps(name), .. more]).ExitCode);

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

    /// <summary>A scene with a wall where a creature stands, which <c>dmc map</c> refuses.</summary>
    static PyDict Refused()
    {
        var scene = Scene();
        scene["tokens"] = Arr(Obj(("id", "@"), ("name", "Thorin"), ("at", "A1")));
        return scene;
    }

    [TestMethod]
    public void AMapDrawnByDmcMapIsRecordedWithItsBasePlace()
    {
        Draw("live.json");
        CollectionAssert.AreEqual(new[] { "live.json", "goblin-hall.json" }, Record());
        WriteFile(_maps, "test-bout.json", Place());
        Draw("test-bout.json");
        CollectionAssert.AreEqual(new[] { "test-bout.json" }, Record());
        // --measure draws nothing and records nothing.
        Assert.AreEqual(0, DmcRunner.InProcess(["map", InMaps("live.json"), "--measure", "B2", "E2"]).ExitCode);
        CollectionAssert.AreEqual(new[] { "test-bout.json" }, Record());
    }

    [TestMethod]
    public void NothingChangedMeansNoOutputAndNoDrawing()
    {
        Draw("live.json");
        string before = Data();
        var stamp = File.GetLastWriteTimeUtc(InMaps("live-data.js"));
        AssertSilent();
        Assert.AreEqual(before, Data());
        Assert.AreEqual(stamp, File.GetLastWriteTimeUtc(InMaps("live-data.js")));
    }

    [TestMethod]
    public void AnUnchangedSceneIsNotEvenParsed()
    {
        Draw("live.json");
        // Garbage that is older than the picture: only modification times are looked at, so nothing is said.
        File.WriteAllText(InMaps("live.json"), "not json");
        File.SetLastWriteTimeUtc(InMaps("live.json"), DateTime.UtcNow.AddSeconds(-60));
        File.WriteAllText(InMaps("goblin-hall.json"), "not json either");
        File.SetLastWriteTimeUtc(InMaps("goblin-hall.json"), DateTime.UtcNow.AddSeconds(-60));
        AssertSilent();
    }

    [TestMethod]
    public void AChangedFileIsDrawnAgainInSilence()
    {
        Draw("live.json");
        Edit("live.json", Moved("After the move"));
        AssertSilent();
        StringAssert.Contains(Data(), "After the move");
        // Drawn: the next call finds nothing to do.
        string drawn = Data();
        AssertSilent();
        Assert.AreEqual(drawn, Data());
    }

    [TestMethod]
    public void LiveJsonIsDrawnWhateverThePageShowedBefore()
    {
        WriteFile(_maps, "test-bout.json", Moved("Bout"));
        Draw("test-bout.json");
        Edit("live.json", Moved("The fight"));
        AssertSilent();
        StringAssert.Contains(Data(), "The fight");
        CollectionAssert.AreEqual(new[] { "live.json", "goblin-hall.json" }, Record(), "the record follows the drawing");
    }

    [TestMethod]
    public void AMapDrawnByHandIsFollowedWhileLiveJsonIsUnchanged()
    {
        WriteFile(_maps, "test-bout.json", Moved("Bout"));
        Draw("test-bout.json");
        AssertSilent();
        Edit("test-bout.json", Moved("Bout, round 2"));
        AssertSilent();
        StringAssert.Contains(Data(), "Bout, round 2");
        CollectionAssert.AreEqual(new[] { "test-bout.json", "goblin-hall.json" }, Record());
    }

    [TestMethod]
    public void ANewerBasePlaceDrawsTheSceneAgain()
    {
        Draw("live.json");
        var place = Place();
        place["title"] = "Goblin hall, flooded";
        Edit("goblin-hall.json", place);
        AssertSilent();
        StringAssert.Contains(Data(), "Goblin hall, flooded");
    }

    [TestMethod]
    public void ABasePlaceInASubfolderIsResolvedAsLoadingAMapDoes()
    {
        Directory.CreateDirectory(InMaps("places"));
        WriteFile(InMaps("places"), "hall.json", Place());
        var scene = Scene();
        scene["base"] = "places/hall";
        WriteFile(_maps, "live.json", scene);
        File.SetLastWriteTimeUtc(InMaps("live.json"), DateTime.UtcNow.AddSeconds(-60));
        Draw("live.json");
        CollectionAssert.AreEqual(new[] { "live.json", "places/hall.json" }, Record());
        var place = Place();
        place["title"] = "Hall in a subfolder";
        File.SetLastWriteTimeUtc(InMaps("live-data.js"), DateTime.UtcNow.AddSeconds(-10));
        WriteFile(InMaps("places"), "hall.json", place);
        AssertSilent();
        StringAssert.Contains(Data(), "Hall in a subfolder");
    }

    [TestMethod]
    public void APageDrawnBeforeTheRecordExistedFollowsLiveJson()
    {
        Draw("live.json");
        File.Delete(InMaps("live-source.txt"));
        Edit("live.json", Moved("Old page"));
        AssertSilent();
        StringAssert.Contains(Data(), "Old page");
    }

    [TestMethod]
    public void ARecordThatPointsOutsideMapsIsIgnored()
    {
        Draw("live.json");
        File.WriteAllText(InMaps("live-source.txt"), "..\\..\\outside.json\n");
        Edit("live.json", Moved("Stayed inside"));
        AssertSilent();
        StringAssert.Contains(Data(), "Stayed inside");
    }

    [TestMethod]
    public void ThePictureIsRefreshedOnlyWhenOneAlreadySitsBesideThePage()
    {
        Draw("live.json");
        Edit("live.json", Moved("No picture yet"));
        AssertSilent();
        Assert.IsFalse(File.Exists(InMaps("live.png")), "redraw does not start writing pictures");

        Draw("live.json", "--png");
        Assert.IsTrue(File.Exists(InMaps("live.png")));
        File.SetLastWriteTimeUtc(InMaps("live.png"), DateTime.UtcNow.AddSeconds(-30));
        Edit("live.json", Moved("With a picture"));
        AssertSilent();
        Assert.IsTrue(File.GetLastWriteTimeUtc(InMaps("live.png")) > DateTime.UtcNow.AddSeconds(-20), "live.png was written again");
    }

    [TestMethod]
    public void ARefusedFileIsOneLineOnStderrAndExitCodeTwo()
    {
        Draw("live.json");
        string before = Data();
        Edit("live.json", Refused());
        var result = Redraw();
        Assert.AreEqual(2, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.HasCount(1, result.StderrLines, result.Stderr);
        StringAssert.StartsWith(result.Stderr, "dmc redraw: live.json was not drawn");
        StringAssert.Contains(result.Stderr, "is on A1, which is a wall");
        StringAssert.EndsWith(result.Stderr.Trim(), "Fix it now; if it is still refused it will be reported again when your next reply ends.");
        Assert.AreEqual(before, Data(), "a refused file draws nothing");
        // Still refused and still newer than the picture: the next reply reports it again.
        Assert.AreEqual(2, Redraw().ExitCode);
    }

    [TestMethod]
    public void AStopHookThatIsAlreadyActiveNeverBlocksAgainButTheNextTurnDoes()
    {
        Draw("live.json");
        Edit("live.json", Refused());
        AssertSilent("{\"stop_hook_active\": true}", "a second refusal in the same turn is not reported");
        // The file is still newer than the picture, so the next turn's Stop hook reports it.
        Assert.AreEqual(2, Redraw("{\"stop_hook_active\": false}").ExitCode);

        // The corrected file is drawn even when a Stop hook is active: that is the reply after the one-line report.
        Edit("live.json", Moved("Corrected"));
        AssertSilent("{\"stop_hook_active\": true}");
        StringAssert.Contains(Data(), "Corrected");
    }

    [TestMethod]
    public void RunByHandAtATerminalItDoesNotWaitForInput()
    {
        Draw("live.json");
        Edit("live.json", Refused());
        using var output = new StringWriter { NewLine = "\n" };
        using var errors = new StringWriter { NewLine = "\n" };
        int exitCode = CommandLine.Run(["redraw"], new Io(new UnreadableInput(), output, errors, InIsTerminal: true), [new RedrawCommand(_root)]);
        Assert.AreEqual(2, exitCode, "by hand it acts as if stop_hook_active were false");
        Assert.AreEqual("", output.ToString());
        Assert.HasCount(1, errors.ToString().TrimEnd().Split('\n'));
    }

    /// <summary>Standard input at a terminal: reading it would wait for a person, so a test fails if the command tries.</summary>
    sealed class UnreadableInput : TextReader
    {
        /// <summary>Fails the test: nothing may wait for a terminal.</summary>
        public override string ReadToEnd() => throw new InvalidOperationException("dmc redraw read a terminal");

        /// <summary>Fails the test: nothing may wait for a terminal.</summary>
        public override int Peek() => throw new InvalidOperationException("dmc redraw read a terminal");

        /// <summary>Fails the test: nothing may wait for a terminal.</summary>
        public override int Read() => throw new InvalidOperationException("dmc redraw read a terminal");
    }

    [TestMethod]
    public void NoActiveCampaignOrNoPageYetMeansSilenceAndNoFiles()
    {
        // A campaign with no page yet: DMC has not shown a map, so nothing is drawn behind its back.
        AssertSilent();
        Assert.IsFalse(File.Exists(InMaps("live-data.js")));

        Draw("live.json");
        File.Delete(Path.Combine(_root, "adventures", ".active-campaign"));
        Edit("live.json", Moved("Unseen"));
        AssertSilent(because: "no active campaign");
        Assert.DoesNotContain("Unseen", Data());

        // A name that is a path, an empty file, and a campaign folder that does not exist.
        foreach (string name in new[] { "../test", "", "\n", "missing" })
        {
            File.WriteAllText(Path.Combine(_root, "adventures", ".active-campaign"), name);
            AssertSilent(because: name);
        }
        Assert.DoesNotContain("Unseen", Data());
    }

    [TestMethod]
    public void BadInputOnStdinIsNotAnError()
    {
        Draw("live.json");
        Edit("live.json", Moved("Any input"));
        foreach (string input in new[] { "", "not json", "[]", "{\"stop_hook_active\": \"yes\"}" })
            AssertSilent(input, input);
        StringAssert.Contains(Data(), "Any input");
    }

    [TestMethod]
    public void StopHookActiveIsReadFromTheJson()
    {
        Assert.IsTrue(RedrawCommand.StopHookActive("{\"stop_hook_active\": true, \"cwd\": \"x\"}"));
        foreach (string input in new[] { "{\"stop_hook_active\": false}", "{}", "", "  ", "[]", "[true]", "\"stop_hook_active\"", "{\"stop_hook_active\": 1}", "{" })
            Assert.IsFalse(RedrawCommand.StopHookActive(input), input);
    }
}
