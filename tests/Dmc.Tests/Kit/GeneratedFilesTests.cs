using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Kit;

/// <summary>
/// The fixed table from <c>kit/</c> to a DMC folder, the version the generated files carry, and the guard that keeps every
/// target out of the player's own folders (FR-012).
/// </summary>
[TestClass]
public sealed class GeneratedFilesTests
{
    [TestMethod]
    public void TheTableMapsTheFourSourcesToTheirPlaces()
    {
        Assert.AreEqual("CLAUDE.md", GeneratedFiles.TargetOf("kit/dm-rules.md"));
        Assert.AreEqual(".claude/settings.json", GeneratedFiles.TargetOf("kit/settings.json"));
        Assert.AreEqual(".claude/skills/dmc-resume-campaign/SKILL.md", GeneratedFiles.TargetOf("kit/skills/dmc-resume-campaign/SKILL.md"));
        Assert.AreEqual(".claude/skills/dmc-draw-map/map-format.md", GeneratedFiles.TargetOf("kit/skills/dmc-draw-map/map-format.md"));
        Assert.AreEqual("templates/feedback.md", GeneratedFiles.TargetOf("kit/templates/feedback.md"));
        Assert.AreEqual("templates/campaign/story/sessions/.gitkeep", GeneratedFiles.TargetOf("kit/templates/campaign/story/sessions/.gitkeep"));
        // MSBuild writes the platform's separator into the name of a nested resource.
        Assert.AreEqual("templates/campaign/game-state.md", GeneratedFiles.TargetOf("kit/templates\\campaign\\game-state.md"));
    }

    [TestMethod]
    public void AFileOfKitThatIsNotInTheTableIsAProgramError()
    {
        foreach (string name in new[] { "kit/other.txt", "kit/skills", "kit/templates", "kit/dm-rules.md.bak", "kit/DM-RULES.md", "elsewhere/x" })
            Assert.ThrowsExactly<InvalidOperationException>(() => GeneratedFiles.TargetOf(name), name);
    }

    [TestMethod]
    public void TheProgramsGeneratedFilesAreTheEmbeddedKitInOrdinalOrder()
    {
        var files = GeneratedFiles.All;
        Assert.IsNotEmpty(files);
        CollectionAssert.AreEqual(files.Select(f => f.Path).Order(StringComparer.Ordinal).ToArray(), files.Select(f => f.Path).ToArray());
        Assert.AreEqual(files.Count, files.Select(f => f.Path).Distinct().Count());
        foreach (var file in files)
        {
            StringAssert.Matches(file.Sha256, new System.Text.RegularExpressions.Regex("^[0-9a-f]{64}$"));
            Assert.DoesNotContain("\\", file.Path, "paths use /");
        }
        string rules = files.Single(f => f.Path == "CLAUDE.md").Sha256;
        Assert.AreEqual(Manifest.Hash(File.ReadAllBytes(Path.Combine(TestEnv.Root, "kit", "dm-rules.md"))), rules);
    }

    [TestMethod]
    public void TheVersionIsTheProgramsVersion() => Assert.AreEqual(CommandLine.Version, GeneratedFiles.Version);

    [TestMethod]
    public void TheGuardRefusesEveryPlayerFolder()
    {
        CollectionAssert.AreEquivalent(new[] { "adventures", "feedback", "modules", "rules", "exports" }, GeneratedFiles.PlayerFolders.ToArray());
        foreach (string folder in GeneratedFiles.PlayerFolders)
        {
            foreach (string path in new[] { folder, $"{folder}/x.md", $"{folder}/a/b/c.md", $"./{folder}/x.md", $"{folder}\\x.md", folder.ToUpperInvariant() + "/x.md" })
                Assert.ThrowsExactly<InvalidOperationException>(() => GeneratedFiles.Guard(path), path);
        }
    }

    [TestMethod]
    public void TheGuardRefusesAPathThatLeavesTheFolder()
    {
        foreach (string path in new[] { "", "../x", "a/../../x", "/etc/passwd", "C:\\Windows\\x", "C:/x" })
            Assert.ThrowsExactly<InvalidOperationException>(() => GeneratedFiles.Guard(path), path);
    }

    [TestMethod]
    public void TheGuardAllowsEverythingDmcWrites()
    {
        foreach (string path in new[] { "CLAUDE.md", ".claude/settings.json", ".claude/skills/dmc-x/SKILL.md", "templates/a.md", ".dmc/manifest.json",
                                         ".dmc/replaced/20261024-123045/CLAUDE.md", "adventures-not.md", "rules.md" })
            GeneratedFiles.Guard(path);
        foreach (var file in GeneratedFiles.All)
            GeneratedFiles.Guard(file.Path);
    }
}
