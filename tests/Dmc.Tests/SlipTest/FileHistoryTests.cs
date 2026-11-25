using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>The restore of the campaign files to a moment of play from Claude Code's file history (spec 024, FR-004, research R4).</summary>
[TestClass]
public sealed class FileHistoryTests
{
    /// <summary>Every backup of the invented play log.</summary>
    /// <param name="f">The fixtures.</param>
    static List<Backup> Backups(SlipTestFixtures f) =>
        FileHistory.Collect(Folders.Project(f.ClaudeHome, f.Folder), Path.Combine(f.ClaudeHome, "file-history"));

    [TestMethod]
    public void EachFileTakesItsFirstBackupAfterTheTime()
    {
        using var f = new SlipTestFixtures();
        var state = FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T2);
        string a = Path.Combine("adventures", "c", "a.md"), added = Path.Combine("adventures", "c", "new.md");
        Assert.AreEqual(Path.Combine(f.ClaudeHome, "file-history", SlipTestFixtures.Session, "h1@v2"), state[a]);
        Assert.IsNull(state[added]);
        Assert.HasCount(2, state);

        var earlier = FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T1);
        Assert.EndsWith("h1@v1", earlier[a]);
    }

    [TestMethod]
    public void ApplyingTheStateUndoesLaterEditsAndRemovesLaterFiles()
    {
        using var f = new SlipTestFixtures();
        Folders.CopyTree(f.Folder, f.Copy);
        File.WriteAllText(Path.Combine(f.Copy, "adventures", "c", "untouched.md"), "same");
        FileHistory.Apply(FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T2), f.Copy);
        Assert.AreEqual("middle", File.ReadAllText(Path.Combine(f.Copy, "adventures", "c", "a.md")));
        Assert.IsFalse(File.Exists(Path.Combine(f.Copy, "adventures", "c", "new.md")));
        Assert.AreEqual("same", File.ReadAllText(Path.Combine(f.Copy, "adventures", "c", "untouched.md")));
    }

    [TestMethod]
    public void AFileWithNoLaterBackupKeepsItsPresentContent()
    {
        using var f = new SlipTestFixtures();
        var state = FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T2.AddHours(1));
        Assert.IsEmpty(state);
    }

    [TestMethod]
    public void ABackupFromALaterSessionCounts()
    {
        using var f = new SlipTestFixtures();
        string later = "bbbbbbbb-0000-4000-8000-000000000002";
        string a = Path.Combine(f.Folder, "adventures", "c", "a.md");
        Directory.CreateDirectory(Path.Combine(f.ClaudeHome, "file-history", later));
        File.WriteAllText(Path.Combine(f.ClaudeHome, "file-history", later, "k@v1"), "next day");
        var snapshot = new System.Text.Json.Nodes.JsonObject
        {
            ["type"] = "file-history-snapshot",
            ["snapshot"] = new System.Text.Json.Nodes.JsonObject
            {
                ["trackedFileBackups"] = new System.Text.Json.Nodes.JsonObject
                {
                    [a] = new System.Text.Json.Nodes.JsonObject { ["backupFileName"] = "k@v1", ["backupTime"] = SlipTestFixtures.Stamp(SlipTestFixtures.T2.AddDays(1)) },
                },
            },
        };
        File.WriteAllText(Path.Combine(Folders.Project(f.ClaudeHome, f.Folder), later + ".jsonl"), snapshot.ToJsonString() + "\n");
        var state = FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T2.AddHours(1));
        Assert.EndsWith("k@v1", state[Path.Combine("adventures", "c", "a.md")]);
    }

    [TestMethod]
    public void AMissingBackupIsRefused()
    {
        using var f = new SlipTestFixtures();
        File.Delete(Path.Combine(f.ClaudeHome, "file-history", SlipTestFixtures.Session, "h1@v2"));
        Folders.CopyTree(f.Folder, f.Copy);
        var state = FileHistory.StateAt(Backups(f), f.Folder, SlipTestFixtures.T2);
        Assert.Throws<SlipTestException>(() => FileHistory.Apply(state, f.Copy));
    }
}
