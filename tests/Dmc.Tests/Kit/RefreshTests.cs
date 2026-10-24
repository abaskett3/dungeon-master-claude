using System.Security.Cryptography;
using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Kit;

/// <summary>
/// The states of a generated file (data-model, research R4): missing, same as the program's copy, same as the manifest's hash,
/// edited by hand, dropped by the program, and in the way of a first install; an interrupted refresh repaired by the next; and
/// the promise that no file of the player changes (SC-002), proved with hashes.
/// </summary>
[TestClass]
public sealed class RefreshTests
{
    /// <summary>The moment the refreshes are made, which names the <c>.dmc/replaced/</c> folder.</summary>
    static readonly DateTime Now = new(2026, 10, 24, 12, 30, 45);

    /// <summary>The name of that folder.</summary>
    const string Stamp = "20261024-123045";

    /// <summary>A fresh empty folder to be a DMC folder, deleted by <see cref="WithFolder"/>.</summary>
    static void WithFolder(Action<string> test)
    {
        string folder = Path.Combine(DmcFolders.NewTemp(), "dmc");
        try
        {
            test(folder);
        }
        finally
        {
            DmcFolders.Delete(Path.GetDirectoryName(folder)!);
        }
    }

    /// <summary>The path of a generated file in a folder.</summary>
    static string At(string folder, string relative) => Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Refreshes a folder with the test files of a version.</summary>
    static RefreshResult Refresh(string folder, string rules = "rules v1\n", string version = "1.0.0") =>
        Refresher.Refresh(folder, TestMachine.Files(rules), version, Now);

    [TestMethod]
    public void AFirstInstallWritesEveryFileAndTheManifest() => WithFolder(folder =>
    {
        var result = Refresh(folder);
        Assert.AreEqual(4, result.Updated);
        Assert.AreEqual(0, result.Kept);
        Assert.IsNull(result.Stamp);
        Assert.AreEqual("rules v1\n", File.ReadAllText(At(folder, "CLAUDE.md")));
        Assert.AreEqual("draw map\n", File.ReadAllText(At(folder, ".claude/skills/dmc-draw-map/SKILL.md")));
        var manifest = Manifest.Read(folder)!;
        Assert.AreEqual("1.0.0", manifest.Version);
        CollectionAssert.AreEqual(TestMachine.Files().Select(f => f.Path).Order(StringComparer.Ordinal).ToArray(), manifest.Files.Select(f => f.Path).ToArray());
        foreach (var entry in manifest.Files)
            Assert.AreEqual(Manifest.Hash(File.ReadAllBytes(At(folder, entry.Path))), entry.Sha256);
        Assert.IsFalse(Directory.Exists(At(folder, ".dmc/replaced")));
    });

    [TestMethod]
    public void ARefreshOfACurrentFolderChangesNothing() => WithFolder(folder =>
    {
        Refresh(folder);
        string[] before = DmcFolders.SnapshotAll(folder);
        var again = Refresh(folder);
        Assert.AreEqual(0, again.Updated);
        Assert.AreEqual(0, again.Kept);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(folder));
        Assert.IsTrue(Refresher.IsCurrent(folder, TestMachine.Files(), "1.0.0"));
    });

    [TestMethod]
    public void AMissingFileIsWritten() => WithFolder(folder =>
    {
        Refresh(folder);
        File.Delete(At(folder, "templates/feedback.md"));
        var result = Refresh(folder);
        Assert.AreEqual(1, result.Updated);
        Assert.AreEqual("feedback\n", File.ReadAllText(At(folder, "templates/feedback.md")));
        Assert.IsFalse(Directory.Exists(At(folder, ".dmc/replaced")), "nothing was lost, so nothing is kept");
    });

    [TestMethod]
    public void AFileTheSameAsTheProgramsCopyIsLeftEvenWithNoManifest() => WithFolder(folder =>
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(At(folder, "CLAUDE.md"), "rules v1\n");
        var result = Refresh(folder);
        Assert.AreEqual(3, result.Updated, "the other three are written");
        Assert.IsNotNull(Manifest.Read(folder)!.HashOf("CLAUDE.md"));
    });

    [TestMethod]
    public void AnOlderGeneratedVersionIsReplacedWithoutKeepingIt() => WithFolder(folder =>
    {
        Refresh(folder, "rules v1\n", "1.0.0");
        var result = Refresh(folder, "rules v2\n", "1.1.0");
        Assert.AreEqual(1, result.Updated);
        Assert.AreEqual(0, result.Kept);
        Assert.AreEqual("rules v2\n", File.ReadAllText(At(folder, "CLAUDE.md")));
        Assert.AreEqual("1.1.0", Manifest.Read(folder)!.Version);
        Assert.IsFalse(Directory.Exists(At(folder, ".dmc/replaced")));
    });

    [TestMethod]
    public void AFileEditedByHandIsCopiedToReplacedThenReplaced() => WithFolder(folder =>
    {
        Refresh(folder);
        File.WriteAllText(At(folder, "CLAUDE.md"), "my rules\n");
        File.WriteAllText(At(folder, ".claude/skills/dmc-draw-map/SKILL.md"), "my map skill\n");
        var result = Refresh(folder);
        Assert.AreEqual(2, result.Updated);
        Assert.AreEqual(2, result.Kept);
        Assert.AreEqual(Stamp, result.Stamp);
        Assert.AreEqual("rules v1\n", File.ReadAllText(At(folder, "CLAUDE.md")));
        Assert.AreEqual("my rules\n", File.ReadAllText(At(folder, $".dmc/replaced/{Stamp}/CLAUDE.md")));
        Assert.AreEqual("my map skill\n", File.ReadAllText(At(folder, $".dmc/replaced/{Stamp}/.claude/skills/dmc-draw-map/SKILL.md")));
        CollectionAssert.AreEqual(new[]
        {
            $"dmc refresh: {folder}: 2 files updated for dmc 1.0.0",
            $"dmc refresh: {folder}: 2 edited files kept in .dmc/replaced/{Stamp}/",
        }, Refresher.Report("dmc refresh", folder, result, "1.0.0").ToArray());
    });

    [TestMethod]
    public void AFileTheProgramNoLongerHasIsRemovedWhenUnchangedAndMovedWhenEdited() => WithFolder(folder =>
    {
        var withExtra = TestMachine.Files().Concat(
        [
            new GeneratedFile(".claude/skills/dmc-gone/SKILL.md", "gone\n"u8.ToArray()),
            new GeneratedFile(".claude/skills/dmc-gone/notes.md", "notes\n"u8.ToArray()),
            new GeneratedFile(".claude/skills/dmc-edited/SKILL.md", "edited\n"u8.ToArray()),
            new GeneratedFile("templates/old.md", "old\n"u8.ToArray()),
        ]).ToList();
        Refresher.Refresh(folder, withExtra, "1.0.0", Now);
        File.WriteAllText(At(folder, ".claude/skills/dmc-edited/SKILL.md"), "i changed it\n");

        var result = Refresh(folder, version: "1.1.0");
        Assert.AreEqual(4, result.Updated);
        Assert.AreEqual(1, result.Kept);
        Assert.IsFalse(File.Exists(At(folder, ".claude/skills/dmc-gone/SKILL.md")));
        Assert.IsFalse(Directory.Exists(At(folder, ".claude/skills/dmc-gone")), "the folder a dropped skill leaves empty is removed");
        Assert.IsFalse(Directory.Exists(At(folder, ".claude/skills/dmc-edited")));
        Assert.IsFalse(File.Exists(At(folder, "templates/old.md")));
        Assert.AreEqual("i changed it\n", File.ReadAllText(At(folder, $".dmc/replaced/{Stamp}/.claude/skills/dmc-edited/SKILL.md")));
        Assert.IsTrue(Directory.Exists(At(folder, ".claude/skills/dmc-draw-map")), "the skills the program has stay");
        Assert.IsTrue(Directory.Exists(At(folder, ".claude/skills")));
        Assert.IsNull(Manifest.Read(folder)!.HashOf(".claude/skills/dmc-gone/SKILL.md"));
    });

    [TestMethod]
    public void ARemovedFileThatIsAlreadyGoneIsNotAnError() => WithFolder(folder =>
    {
        Refresher.Refresh(folder, [.. TestMachine.Files(), new GeneratedFile("templates/old.md", "old\n"u8.ToArray())], "1.0.0", Now);
        File.Delete(At(folder, "templates/old.md"));
        var result = Refresh(folder, version: "1.1.0");
        Assert.AreEqual(0, result.Updated);
        Assert.IsNull(Manifest.Read(folder)!.HashOf("templates/old.md"));
    });

    [TestMethod]
    public void AFileInTheWayOfAFirstInstallIsRefusedByNameAndNothingIsWritten() => WithFolder(folder =>
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(At(folder, "CLAUDE.md"), "somebody else's rules\n");
        File.WriteAllText(At(folder, "notes.txt"), "mine");
        string[] before = DmcFolders.SnapshotAll(folder);
        var error = Assert.ThrowsExactly<DmcException>(() => Refresh(folder));
        Assert.AreEqual($"{At(folder, "CLAUDE.md")} already exists and was not written by dmc: move it, or choose another folder", error.Message);
        CollectionAssert.AreEqual(before, DmcFolders.SnapshotAll(folder), "nothing written, not even the files that were free");
        Assert.IsFalse(Directory.Exists(At(folder, ".dmc")));
        Assert.ThrowsExactly<DmcException>(() => Refresher.Preflight(folder, TestMachine.Files()));
    });

    [TestMethod]
    public void AFolderInTheWayOfAFirstInstallIsRefusedToo() => WithFolder(folder =>
    {
        Directory.CreateDirectory(At(folder, "CLAUDE.md"));
        var error = Assert.ThrowsExactly<DmcException>(() => Refresh(folder));
        StringAssert.Contains(error.Message, "already exists and was not written by dmc");
    });

    [TestMethod]
    public void AFileNotInTheManifestThatDiffersIsKeptAndReplacedOnARefresh() => WithFolder(folder =>
    {
        Refresher.Refresh(folder, TestMachine.Files().Where(f => f.Path != "templates/feedback.md").ToList(), "1.0.0", Now);
        Directory.CreateDirectory(At(folder, "templates"));
        File.WriteAllText(At(folder, "templates/feedback.md"), "the player's own\n");
        var result = Refresh(folder, version: "1.1.0");
        Assert.AreEqual(1, result.Kept);
        Assert.AreEqual("the player's own\n", File.ReadAllText(At(folder, $".dmc/replaced/{Stamp}/templates/feedback.md")));
        Assert.AreEqual("feedback\n", File.ReadAllText(At(folder, "templates/feedback.md")));
    });

    [TestMethod]
    public void AnInterruptedRefreshIsRepairedByTheNext() => WithFolder(folder =>
    {
        Refresh(folder, "rules v1\n", "1.0.0");
        // The new program's refresh gets as far as CLAUDE.md and then fails on a template: a folder is in its place.
        string template = At(folder, "templates/feedback.md");
        File.Delete(template);
        Directory.CreateDirectory(template);
        Assert.ThrowsExactly<DmcException>(() => Refresh(folder, "rules v2\n", "1.1.0"));
        Assert.AreEqual("rules v2\n", File.ReadAllText(At(folder, "CLAUDE.md")), "the files before the failure were written");
        Assert.AreEqual("1.0.0", Manifest.Read(folder)!.Version, "the manifest is written last, so the old one stays");
        Assert.IsFalse(Refresher.IsCurrent(folder, TestMachine.Files("rules v2\n"), "1.1.0"));

        Directory.Delete(template);
        var repaired = Refresh(folder, "rules v2\n", "1.1.0");
        Assert.AreEqual(1, repaired.Updated, "only the template is left to write");
        Assert.AreEqual(0, repaired.Kept, "the CLAUDE.md the first attempt wrote is the program's own copy");
        Assert.AreEqual("1.1.0", Manifest.Read(folder)!.Version);
        Assert.IsTrue(Refresher.IsCurrent(folder, TestMachine.Files("rules v2\n"), "1.1.0"));
    });

    [TestMethod]
    public void AManifestNamingAPlayerFileIsRefusedAndThatFileIsKept() => WithFolder(folder =>
    {
        Refresh(folder);
        Directory.CreateDirectory(At(folder, "adventures/x"));
        File.WriteAllText(At(folder, "adventures/x/game-state.md"), "precious");
        var tampered = new Manifest("1.0.0", [.. Manifest.Read(folder)!.Files, new ManifestEntry("adventures/x/game-state.md", Manifest.Hash("precious"u8.ToArray()))]);
        Manifest.Write(folder, tampered);
        Assert.ThrowsExactly<InvalidOperationException>(() => Refresh(folder, version: "1.1.0"));
        Assert.AreEqual("precious", File.ReadAllText(At(folder, "adventures/x/game-state.md")));

        Manifest.Write(folder, new Manifest("1.0.0", [new ManifestEntry("../outside.md", "00")]));
        Assert.ThrowsExactly<InvalidOperationException>(() => Refresh(folder, version: "1.1.0"));
    });

    [TestMethod]
    public void NoFileOfThePlayerChanges() => WithFolder(folder =>
    {
        string[] player = ["adventures", "feedback", "modules", "rules", "exports"];
        // A filled DMC folder: campaigns, both player-options files, feedback, a module, a rules file, exports, and the player's own
        // settings and skill, which are not dmc's.
        foreach (string file in new[]
                 {
                     "adventures/player-options.md", "adventures/.active-campaign", "adventures/silver-egg/player-options.md", "adventures/silver-egg/game-state.md",
                     "adventures/silver-egg/story/dm-notes.md", "feedback/2026-10-01-a.md", "modules/lmop/index.md", "rules/srd.txt", "exports/silver-egg-202610011200.zip",
                     ".claude/settings.local.json", ".claude/skills/my-skill/SKILL.md", "my-notes.txt",
                 })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(At(folder, file))!);
            File.WriteAllText(At(folder, file), "content of " + file);
        }
        Directory.CreateDirectory(At(folder, "adventures/empty-folder"));

        string[] Everything() => [.. DmcFolders.Snapshot(folder, player), .. DmcFolders.Snapshot(folder, ".claude/skills/my-skill"),
            .. new[] { ".claude/settings.local.json", "my-notes.txt" }.Select(f => f + " " + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(At(folder, f)))))];
        string[] before = Everything();

        Refresh(folder, "rules v1\n", "1.0.0");
        CollectionAssert.AreEqual(before, Everything(), "a first install");
        Refresh(folder, "rules v2\n", "1.1.0");
        CollectionAssert.AreEqual(before, Everything(), "a refresh with a changed file");
        File.WriteAllText(At(folder, "CLAUDE.md"), "edited");
        Refresher.Refresh(folder, TestMachine.Files("rules v3\n").Where(f => f.Path != "templates/feedback.md").ToList(), "1.2.0", Now);
        CollectionAssert.AreEqual(before, Everything(), "a refresh that keeps an edit and drops a file");
        Assert.IsTrue(Directory.Exists(At(folder, "adventures/empty-folder")));
    });

    [TestMethod]
    public void IsCurrentNeedsTheManifestTheVersionAndEveryFile() => WithFolder(folder =>
    {
        Assert.IsFalse(Refresher.IsCurrent(folder, TestMachine.Files(), "1.0.0"), "no folder");
        Refresh(folder);
        Assert.IsTrue(Refresher.IsCurrent(folder, TestMachine.Files(), "1.0.0"));
        Assert.IsFalse(Refresher.IsCurrent(folder, TestMachine.Files(), "1.0.1"), "another version");
        Assert.IsFalse(Refresher.IsCurrent(folder, TestMachine.Files("rules v2\n"), "1.0.0"), "a file that differs from the program's");
        File.Delete(At(folder, "CLAUDE.md"));
        Assert.IsFalse(Refresher.IsCurrent(folder, TestMachine.Files(), "1.0.0"), "a missing file");
    });

    [TestMethod]
    public void TheReportIsNothingWhenNothingChanged()
    {
        Assert.IsEmpty(Refresher.Report("dmc refresh", "f", new RefreshResult(0, 0, null), "1.0.0").ToArray());
        CollectionAssert.AreEqual(new[] { "dmc install: f: 3 files updated for dmc 1.0.0" }, Refresher.Report("dmc install", "f", new RefreshResult(3, 0, null), "1.0.0").ToArray());
    }

    [TestMethod]
    public void TheRealKitInstallsAndIsThenCurrent() => WithFolder(folder =>
    {
        var result = Refresher.Refresh(folder, GeneratedFiles.All, GeneratedFiles.Version, Now);
        Assert.AreEqual(GeneratedFiles.All.Count, result.Updated);
        Assert.IsTrue(Refresher.IsCurrent(folder, GeneratedFiles.All, GeneratedFiles.Version));
        Assert.IsTrue(File.Exists(At(folder, "CLAUDE.md")));
        Assert.IsTrue(File.Exists(At(folder, ".claude/skills/dmc-resume-campaign/SKILL.md")));
        Assert.IsFalse(Directory.Exists(At(folder, "adventures")), "the refresh writes no player folder; dmc install makes them");
    });
}
