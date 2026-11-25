using System.Text.Json.Nodes;
using Dmc.SlipTest;

namespace Dmc.Tests.SlipTest;

/// <summary>
/// The restore when an edit falls between the fork and the next backup, as in real logs: Claude Code writes a backup
/// at each edit and records it at the next message, and keeps the file before a session's first edit as <c>@v1</c>
/// without recording it.
/// </summary>
[TestClass]
public sealed class FileHistoryEditTests
{
    /// <summary>The session of the invented logs below.</summary>
    const string Session = "cccccccc-0000-4000-8000-000000000003";

    /// <summary>The fork time of these tests: an hour after the fixtures' play.</summary>
    static readonly DateTimeOffset At = SlipTestFixtures.T2.AddHours(1);

    /// <summary>The campaign file these tests restore, relative to the DMC folder.</summary>
    static readonly string B = Path.Combine("adventures", "c", "b.md");

    /// <summary>A recorded backup entry.</summary>
    /// <param name="name">The backup's file name, or null.</param>
    /// <param name="t">Its time.</param>
    static JsonObject Entry(string? name, DateTimeOffset t) => new() { ["backupFileName"] = name, ["backupTime"] = SlipTestFixtures.Stamp(t) };

    /// <summary>A <c>file-history-snapshot</c> record of one file.</summary>
    /// <param name="file">The file's full path.</param>
    /// <param name="entry">Its entry.</param>
    static JsonObject Snapshot(string file, JsonObject entry) => new()
    {
        ["type"] = "file-history-snapshot",
        ["snapshot"] = new JsonObject { ["trackedFileBackups"] = new JsonObject { [file] = entry } },
    };

    /// <summary>An assistant record that edits a file at a time.</summary>
    /// <param name="file">The file's full path.</param>
    /// <param name="t">The record's time.</param>
    /// <param name="tool">The edit tool.</param>
    static JsonObject Edit(string file, DateTimeOffset t, string tool = "Edit") => new()
    {
        ["type"] = "assistant", ["timestamp"] = SlipTestFixtures.Stamp(t),
        ["message"] = new JsonObject
        {
            ["content"] = new JsonArray(new JsonObject { ["type"] = "tool_use", ["name"] = tool, ["input"] = new JsonObject { ["file_path"] = file } }),
        },
    };

    /// <summary>Writes a log of the session and the named backup files with their content.</summary>
    /// <param name="f">The fixtures.</param>
    /// <param name="records">The log's records.</param>
    /// <param name="files">Backup file name to content.</param>
    static void Write(SlipTestFixtures f, IEnumerable<JsonObject> records, params (string Name, string Content)[] files)
    {
        string history = Path.Combine(f.ClaudeHome, "file-history", Session);
        Directory.CreateDirectory(history);
        foreach (var (name, content) in files)
            File.WriteAllText(Path.Combine(history, name), content);
        File.WriteAllLines(Path.Combine(Folders.Project(f.ClaudeHome, f.Folder), Session + ".jsonl"), records.Select(r => r.ToJsonString()));
    }

    /// <summary>The state of the fixtures' DMC folder at <see cref="At"/>, with every edit taken into account.</summary>
    /// <param name="f">The fixtures.</param>
    static Dictionary<string, string?> State(SlipTestFixtures f)
    {
        string project = Folders.Project(f.ClaudeHome, f.Folder);
        return FileHistory.StateAt(FileHistory.Collect(project, Path.Combine(f.ClaudeHome, "file-history")), f.Folder, At, FileHistory.CollectEdits(project));
    }

    /// <summary>An edit between the fork and the next backup is in that backup, so the backup before the fork is used.</summary>
    [TestMethod]
    public void AnEditBeforeTheNextBackupTakesTheBackupBefore()
    {
        using var f = new SlipTestFixtures();
        string b = Path.Combine(f.Folder, B);
        Write(f,
            [Snapshot(b, Entry("hb@v2", At.AddMinutes(-5))), Edit(b, At.AddMinutes(2)), Snapshot(b, Entry("hb@v3", At.AddMinutes(9)))],
            ("hb@v2", "at the fork"), ("hb@v3", "after the correction"));
        Assert.EndsWith("hb@v2", State(f)[B]);
    }

    /// <summary>A session's unrecorded <c>@v1</c> is found on disk, under the stem another log gives the file.</summary>
    [TestMethod]
    public void TheUnrecordedFirstBackupIsFoundOnDisk()
    {
        using var f = new SlipTestFixtures();
        string b = Path.Combine(f.Folder, B);
        Write(f,
            [Snapshot(b, Entry("hb@v2", At.AddHours(-3))), Edit(b, At.AddHours(-2)), Edit(b, At.AddMinutes(2)), Snapshot(b, Entry("hb@v3", At.AddMinutes(9)))],
            ("hb@v2", "old"), ("hb@v3", "later"));
        string other = "dddddddd-0000-4000-8000-000000000004";
        string history = Path.Combine(f.ClaudeHome, "file-history", other);
        Directory.CreateDirectory(history);
        File.WriteAllText(Path.Combine(history, "hb@v1"), "at the fork");
        File.WriteAllLines(Path.Combine(Folders.Project(f.ClaudeHome, f.Folder), other + ".jsonl"), [Edit(b, At.AddMinutes(1)).ToJsonString()]);
        Assert.AreEqual(Path.Combine(history, "hb@v1"), State(f)[B]);
    }

    /// <summary>A file with no backup or edit before the fork, which a <c>Write</c> made later, did not exist then.</summary>
    [TestMethod]
    public void AFileAWriteCreatedLaterDidNotExist()
    {
        using var f = new SlipTestFixtures();
        string b = Path.Combine(f.Folder, B);
        Write(f, [Edit(b, At.AddMinutes(2), "Write"), Snapshot(b, Entry("hb@v2", At.AddMinutes(9)))], ("hb@v2", "made later"));
        var state = State(f);
        Assert.IsTrue(state.ContainsKey(B));
        Assert.IsNull(state[B]);
    }

    /// <summary>A file edited on both sides of the fork, with no backup between, refuses the point.</summary>
    [TestMethod]
    public void AFileNoBackupHoldsIsRefused()
    {
        using var f = new SlipTestFixtures();
        string b = Path.Combine(f.Folder, B);
        Write(f,
            [Snapshot(b, Entry("hb@v2", At.AddHours(-3))), Edit(b, At.AddMinutes(-2)), Edit(b, At.AddMinutes(2)), Snapshot(b, Entry("hb@v3", At.AddMinutes(9)))],
            ("hb@v2", "old"), ("hb@v3", "later"));
        Assert.Throws<SlipTestException>(() => State(f));
    }
}
