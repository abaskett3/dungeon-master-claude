using Dmc.Maps;
using Dmc.Tests.Support;

namespace Dmc.Tests.Map;

/// <summary>
/// The map format description lives in <c>.claude/skills/draw-map/map-format.md</c> (it used to be the comment at the
/// top of <c>map_render.py</c>). It must exist, agree with the code, and be what the skill and <c>dmc map --help</c> point to.
/// </summary>
[TestClass]
public sealed class MapFormatDocTests
{
    static string Skill(string name) => File.ReadAllText(Path.Combine(TestEnv.Root, ".claude", "skills", "draw-map", name));

    [TestMethod]
    public void TheFormatFileNamesEveryScaleSymbolAndTerrain()
    {
        string doc = Skill("map-format.md");
        foreach (int scale in MapRules.Scales)
            StringAssert.Contains(doc, scale.ToString());
        foreach (int symbol in MapRules.Builtin.Keys)
            StringAssert.Contains(doc, Py.FromCodePoint(symbol));
        foreach (string terrain in MapRules.Terrains)
            StringAssert.Contains(doc, $"`{terrain}`");
        foreach (var (kind, _) in MapRules.Kinds)
            StringAssert.Contains(doc, $"`{kind}`");
        foreach (string key in new[] { "scale_note", "north", "edges", "notes", "labels", "base", "legend", "cell_ft", "grid", "tokens" })
            StringAssert.Contains(doc, $"\"{key}\"");
    }

    [TestMethod]
    public void TheSkillPointsToTheFormatFile()
    {
        string skill = Skill("SKILL.md");
        StringAssert.Contains(skill, ".claude/skills/draw-map/map-format.md");
        Assert.DoesNotContain("comment at the top", skill);
    }

    [TestMethod]
    public void HelpPointsToTheFormatFile()
    {
        foreach (var result in new[] { DmcRunner.InProcess(["map", "--help"]), DmcRunner.InProcess(["map", "x.json", "-h"]), DmcRunner.BlackBox(["map", "--help"]) })
        {
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            StringAssert.Contains(result.Stdout, ".claude/skills/draw-map/map-format.md");
            Assert.AreEqual("", result.Stderr);
            Assert.DoesNotContain("\r", result.Stdout); // the help text has "\n" line endings whatever the checkout's
        }
    }
}
