using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Kit;

/// <summary><c>.dmc/manifest.json</c>: its exact shape (data-model), a round trip, and how a bad file is refused.</summary>
[TestClass]
public sealed class ManifestTests
{
    /// <summary>A manifest with two files, one of them in a nested folder.</summary>
    static Manifest Sample() => new("0.2.0",
    [
        new ManifestEntry("CLAUDE.md", Manifest.Hash("rules"u8.ToArray())),
        new ManifestEntry(".claude/skills/dmc-resume-campaign/SKILL.md", Manifest.Hash("skill"u8.ToArray())),
    ]);

    [TestMethod]
    public void TheJsonHasTheKeysOfTheDataModel()
    {
        string json = Sample().ToJson();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        CollectionAssert.AreEqual(new[] { "dmc", "files" }, root.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual("0.2.0", root.GetProperty("dmc").GetString());
        foreach (var file in root.GetProperty("files").EnumerateArray())
            CollectionAssert.AreEqual(new[] { "path", "sha256" }, file.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.DoesNotContain("\r", json, "LF line ends");
        Assert.IsTrue(json.EndsWith("}\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PathsUseSlashesAndHashesAreLowercaseHex()
    {
        string json = Sample().ToJson();
        StringAssert.Contains(json, "\"path\": \".claude/skills/dmc-resume-campaign/SKILL.md\"");
        Assert.DoesNotContain("\\\\", json);
        foreach (Match m in Regex.Matches(json, "\"sha256\": \"([^\"]*)\""))
            StringAssert.Matches(m.Groups[1].Value, new Regex("^[0-9a-f]{64}$"));
        Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", Manifest.Hash([]));
    }

    [TestMethod]
    public void TheManifestSurvivesAWriteAndARead()
    {
        string folder = DmcFolders.NewTemp();
        try
        {
            Assert.IsNull(Manifest.Read(folder), "no manifest, no DMC folder");
            Assert.IsFalse(DmcFolder.IsDmcFolder(folder));
            var manifest = Sample();
            Manifest.Write(folder, manifest);
            Assert.IsTrue(DmcFolder.IsDmcFolder(folder));
            var read = Manifest.Read(folder)!;
            Assert.AreEqual(manifest.Version, read.Version);
            CollectionAssert.AreEqual(manifest.Files.ToArray(), read.Files.ToArray());
            Assert.AreEqual(manifest.ToJson(), File.ReadAllText(Manifest.FileOf(folder)));
            Assert.AreEqual(manifest.Files[0].Sha256, read.HashOf("CLAUDE.md"));
            Assert.IsNull(read.HashOf("nope.md"));
            Assert.IsFalse(File.Exists(Manifest.FileOf(folder) + ".tmp"), "the temporary file is moved over the manifest");

            Manifest.Write(folder, new Manifest("0.3.0", []));
            Assert.AreEqual("0.3.0", Manifest.Read(folder)!.Version);
            Assert.IsEmpty(Manifest.Read(folder)!.Files);
        }
        finally
        {
            DmcFolders.Delete(folder);
        }
    }

    [TestMethod]
    public void AFileThatIsNotAManifestIsRefusedInOneLine()
    {
        foreach (string text in new[] { "", "not json", "[]", "{}", "{ \"dmc\": 1, \"files\": [] }", "{ \"dmc\": \"1\" }", "{ \"dmc\": \"1\", \"files\": [ {} ] }",
                                        "{ \"dmc\": \"1\", \"files\": [ { \"path\": \"a\" } ] }", "{ \"dmc\": \"1\", \"files\": [ { \"path\": 1, \"sha256\": \"x\" } ] }" })
        {
            Assert.ThrowsExactly<FormatException>(() => Manifest.Parse(text), text);
            string folder = DmcFolders.NewTemp();
            try
            {
                Directory.CreateDirectory(Path.Combine(folder, ".dmc"));
                File.WriteAllText(Manifest.FileOf(folder), text);
                var error = Assert.ThrowsExactly<DmcException>(() => Manifest.Read(folder), text);
                StringAssert.StartsWith(error.Message, $"cannot read {Manifest.FileOf(folder)}: ");
                Assert.DoesNotContain("\n", error.Message);
            }
            finally
            {
                DmcFolders.Delete(folder);
            }
        }
    }
}
