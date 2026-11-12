using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// Nothing changes a closed campaign (spec 010 US2, SC-001, FR-015, FR-016; research R8): with a checksum of every file
/// under <c>adventures/&lt;campaign&gt;/</c> taken after the close, every <c>dmc</c> command and hook that could touch it is
/// run, and not one file is changed, added or removed. The writers refuse in one line with their own prefix; <c>redraw</c>
/// does nothing and says nothing; the readers work. Run in this process against a temporary DMC folder, with the campaign
/// active.
/// </summary>
[TestClass]
public sealed class ClosedCampaignFreezeTests
{
    /// <summary>The refusal, without the command's prefix.</summary>
    const string Refusal = "rorn is closed: start a sequel to continue its story";

    /// <summary>A small valid map file.</summary>
    const string Place = "{\"title\":\"The ferry\",\"grid\":[\"#####\",\"#...#\",\"#####\"]}";

    /// <summary>
    /// A closed campaign with a session log and a drawn map, whose <c>maps/live.json</c> is newer than its picture, so an
    /// open campaign would be redrawn by the Stop hook. Active.
    /// </summary>
    static MigrationRoot ClosedWithAMap()
    {
        var root = new MigrationRoot();
        root.WriteCampaign();
        root.Write("story/sessions/session-001.md", "# Session 1\n\n### Scene 1: The ferry\n\nWren crossed.\n");
        root.Write("maps/live.json", Place);
        string live = Path.Combine(root.Campaign(), "maps", "live.json");
        Assert.AreEqual(0, DmcRunner.InProcess(["map", live]).ExitCode);
        File.SetLastWriteTimeUtc(Path.Combine(root.Campaign(), "maps", "live-data.js"), DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(live, DateTime.UtcNow.AddMinutes(-1));
        File.WriteAllText(Path.Combine(root.Adventures, ".active-campaign"), "rorn\n");
        var close = MigrationRoot.Run("close", MigrationRoot.Name, "--yes");
        Assert.AreEqual(0, close.ExitCode, close.ToString());
        return root;
    }

    /// <summary>Every file under the campaign folder, with its SHA-256.</summary>
    static string[] Campaign(MigrationRoot root) => DmcFolders.Snapshot(root.Adventures, MigrationRoot.Name);

    [TestMethod]
    public void EveryWriterRefusesAndNothingChanges()
    {
        using var root = ClosedWithAMap();
        string[] before = Campaign(root);
        string live = Path.Combine(root.Campaign(), "maps", "live.json");
        string pdf = Path.Combine(root.Folder, "book.pdf");
        File.WriteAllText(pdf, "not read: the refusal comes first");

        foreach (var (args, prefix) in new (string[], string)[]
        {
            (["campaign", "backup", "rorn"], "campaign"),
            (["campaign", "backup", "rorn", "--prune", "old"], "campaign"),
            (["campaign", "restore", "rorn"], "campaign"),
            (["campaign", "restore", "rorn", "--yes"], "campaign"),
            (["campaign", "standard", "rorn", "--init"], "campaign"),
            (["map", live], "map"),
            (["map", live, "--png"], "map"),
            (["pdf", "text", pdf, "--out", Path.Combine(root.Campaign(), "extracted")], "pdf"),
            (["pdf", "all", Path.Combine(root.Campaign(), "maps", "..", "book.pdf")], "pdf"),
            (["digest", "show", "--out", Path.Combine(root.Campaign(), "review")], "digest"),
        })
        {
            if (args[0] == "pdf" && args[1] == "all")
                File.Copy(pdf, Path.Combine(root.Campaign(), "book.pdf"));
            var result = DmcRunner.InProcess(args);
            Assert.AreEqual(1, result.ExitCode, string.Join(' ', args) + ": " + result);
            Assert.AreEqual($"dmc {prefix}: {Refusal}", result.Stderr.TrimEnd(), string.Join(' ', args));
            if (args[0] == "pdf" && args[1] == "all")
                File.Delete(Path.Combine(root.Campaign(), "book.pdf"));
            CollectionAssert.AreEqual(before, Campaign(root), string.Join(' ', args));
        }
    }

    [TestMethod]
    public void RedrawAndTheHooksDoNothing()
    {
        using var root = ClosedWithAMap();
        string[] before = Campaign(root);
        var redraw = DmcRunner.InProcess(["redraw"], "{}");
        Assert.AreEqual(0, redraw.ExitCode, redraw.ToString());
        Assert.AreEqual("", redraw.Stdout + redraw.Stderr);
        var check = DmcRunner.InProcess(["context-check"], "{}");
        Assert.AreEqual(0, check.ExitCode, check.ToString());
        CollectionAssert.AreEqual(before, Campaign(root));
    }

    [TestMethod]
    public void TheReadersWorkAndWriteNothingInTheCampaign()
    {
        using var root = ClosedWithAMap();
        string[] before = Campaign(root);
        string live = Path.Combine(root.Campaign(), "maps", "live.json");

        var export = MigrationRoot.Run("export", MigrationRoot.Name);
        Assert.AreEqual(0, export.ExitCode, export.ToString());
        string archive = export.StdoutLines.Single()[(export.StdoutLines.Single().IndexOf(" to ", StringComparison.Ordinal) + 4)..];
        foreach (string[] args in new[]
        {
            new[] { "campaign", "show", archive },
            ["campaign", "standard", "rorn"],
            ["campaign", "backups", "rorn"],
            ["campaign", "verify", "rorn"],
            ["map", live, "--measure", "B2", "D2"],
            ["lookup", "rorn", "People"],
            ["recall", "rorn", "Mara"],
            ["size", "rorn"],
            ["size", "rorn", "--check"],
        })
        {
            var result = DmcRunner.InProcess(args);
            Assert.AreEqual(0, result.ExitCode, string.Join(' ', args) + ": " + result);
            CollectionAssert.AreEqual(before, Campaign(root), string.Join(' ', args));
        }

        var list = MigrationRoot.Run("verify", MigrationRoot.Name, "--list");
        Assert.AreEqual(1, list.ExitCode);
        Assert.AreEqual("dmc campaign: rorn is closed: --list and --accounting are for migrations", list.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, Campaign(root));
    }
}
