using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign close NAME [--yes]</c> (spec 010 contracts/campaign-close.md; research R3 to R5; FR-011): the plan writes
/// nothing; the close writes the mark, the closing backup holding it and a byte-equal copy in <c>exports/</c>; every
/// refusal is one line, exit 1, and writes nothing. Run in this process against a temporary DMC folder; a clash with a
/// file already by the closing backup's name is checked through the library, which can fix the moment.
/// </summary>
[TestClass]
public sealed partial class CampaignCloseTests
{
    /// <summary>The line of a plan or a close, with the file name in both places.</summary>
    [GeneratedRegex(@"^(would close|closed) rorn: (\d+) files to backups/(rorn_closed_\d{14}\.zip) and exports/\3$")]
    private static partial Regex CloseLine();

    /// <summary>A fixed moment, for the library tests that need the closing backup's name in advance.</summary>
    static readonly DateTime Moment = new(2026, 11, 12, 3, 10, 0, DateTimeKind.Utc);

    /// <summary>The number of files <see cref="MigrationRoot.WriteCampaign"/> writes.</summary>
    const int SampleFiles = 4;

    /// <summary>Everything a close could change: the campaigns and <c>exports/</c>.</summary>
    static string[] Everything(MigrationRoot root) => DmcFolders.Snapshot(root.Folder, "adventures", "exports");

    /// <summary>Runs close on the sample campaign.</summary>
    static ProcessResult Close(params string[] extra) => MigrationRoot.Run(["close", MigrationRoot.Name, .. extra]);

    [TestMethod]
    public void ThePlanSaysWhatItWouldWriteAndWritesNothing()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string[] before = Everything(root);
        var result = Close();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var match = CloseLine().Match(result.StdoutLines.Single());
        Assert.IsTrue(match.Success, result.Stdout);
        Assert.AreEqual("would close", match.Groups[1].Value);
        Assert.AreEqual((SampleFiles + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), match.Groups[2].Value, "the campaign's files and the mark");
        CollectionAssert.AreEqual(before, Everything(root));
        Assert.IsFalse(root.Has(".closed"));
    }

    [TestMethod]
    public void YesWritesTheMarkTheClosingBackupAndAnEqualCopy()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var result = Close("--yes");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        var match = CloseLine().Match(result.StdoutLines.Single());
        Assert.IsTrue(match.Success, result.Stdout);
        Assert.AreEqual("closed", match.Groups[1].Value);
        string file = match.Groups[3].Value;

        var mark = ClosedCampaign.Read(root.Campaign());
        Assert.IsNotNull(mark);
        Assert.AreEqual(file, mark.Backup);

        string backup = root.InBackups(file);
        string copy = Path.Combine(root.Folder, "exports", file);
        var summary = ExportSummary.Open(backup);
        Assert.AreEqual(mark.Time, summary.Manifest.ExportedAt);
        Assert.AreEqual(int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), summary.Manifest.Files.Count);
        var held = summary.ReadCampaign();
        Assert.IsTrue(held.ContainsKey(".closed"), "the closing backup holds the mark");
        CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(root.Campaign(), ".closed")), held[".closed"]);
        Assert.IsFalse(summary.Manifest.Files.Any(f => f.Path.StartsWith("backups", StringComparison.Ordinal)), "no backups/ in the backup");
        CollectionAssert.AreEqual(SHA256.HashData(File.ReadAllBytes(backup)), SHA256.HashData(File.ReadAllBytes(copy)));
        Assert.IsEmpty(Directory.GetFiles(root.Folder, "*.partial-*", SearchOption.AllDirectories));

        CollectionAssert.AreEqual(new[] { "closed: " + mark.Time }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
        Assert.AreEqual(file, BackupFolder.ClosedName(MigrationRoot.Name, mark.ClosedAt));
    }

    [TestMethod]
    [DataRow("backups")]
    [DataRow("exports")]
    public void AFileAlreadyByTheClosingBackupsNameStopsTheCloseAndIsKept(string folder)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string file = BackupFolder.ClosedName(MigrationRoot.Name, Moment);
        string where = folder == "backups" ? root.Backups() : Path.Combine(root.Folder, "exports");
        Directory.CreateDirectory(where);
        File.WriteAllText(Path.Combine(where, file), "a leftover");
        string[] before = Everything(root);

        foreach (Func<ClosePlan> run in new Func<ClosePlan>[]
        {
            () => CampaignCloser.Plan(root.Folder, MigrationRoot.Name, Moment),
            () => CampaignCloser.Close(root.Folder, MigrationRoot.Name, Moment),
        })
        {
            var e = Assert.ThrowsExactly<DmcException>(() => run());
            Assert.AreEqual($"rorn cannot be closed: {folder}/{file} already exists", e.Message);
            CollectionAssert.AreEqual(before, Everything(root));
        }
        Assert.AreEqual("a leftover", File.ReadAllText(Path.Combine(where, file)));
    }

    [TestMethod]
    public void NoSuchCampaignIsRefused()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string[] before = Everything(root);
        foreach (string[] args in new[] { new[] { "close", "nope" }, ["close", "nope", "--yes"] })
        {
            var result = MigrationRoot.Run(args);
            Assert.AreEqual(1, result.ExitCode);
            Assert.AreEqual("", result.Stdout);
            StringAssert.StartsWith(result.Stderr, "dmc campaign: no campaign \"nope\" in adventures/");
        }
        CollectionAssert.AreEqual(before, Everything(root));
    }

    [TestMethod]
    public void AClosedCampaignCannotBeClosedAgain()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, Close("--yes").ExitCode);
        string[] before = Everything(root);
        foreach (string[] extra in new[] { Array.Empty<string>(), ["--yes"] })
        {
            var result = Close(extra);
            Assert.AreEqual(1, result.ExitCode);
            Assert.AreEqual("", result.Stdout);
            Assert.AreEqual("dmc campaign: rorn is already closed: start a sequel to continue its story", result.Stderr.TrimEnd());
        }
        CollectionAssert.AreEqual(before, Everything(root));
    }

    [TestMethod]
    [DataRow("unfinished", "rorn has an unfinished migration from {0}: finish it or restore it before closing")]
    [DataRow("behind", "rorn is behind (standard 2, kit 4): bring it up to date before closing")]
    [DataRow("ahead", "rorn is ahead (standard 9, kit 4): update dmc")]
    public void OnlyACurrentCampaignCanBeClosed(string state, string message)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard(state == "ahead" ? "9\n" : "2\n");
        string marked = state == "unfinished" ? root.Backup() : "";
        string[] before = Everything(root);
        foreach (string[] extra in new[] { Array.Empty<string>(), ["--yes"] })
        {
            var result = Close(extra);
            Assert.AreEqual(1, result.ExitCode, state);
            Assert.AreEqual("", result.Stdout);
            Assert.AreEqual("dmc campaign: " + string.Format(System.Globalization.CultureInfo.InvariantCulture, message, marked), result.Stderr.TrimEnd());
        }
        CollectionAssert.AreEqual(before, Everything(root));
    }

    [TestMethod]
    public void AnInterruptedRestoreIsPutBackFirst()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.SetStandard("1\n");
        root.WriteMigrations(2);
        root.Backup();
        CampaignRestorer.Hook = s =>
        {
            if (s == "aside")
                throw new InvalidOperationException("killed");
        };
        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CampaignRestorer.Run(root.Folder, MigrationRoot.Name, null, yes: true));
        }
        finally
        {
            CampaignRestorer.Hook = null;
        }
        Assert.IsFalse(Directory.Exists(root.Campaign()));

        var result = Close();
        Assert.AreEqual("recovered rorn from an interrupted restore", result.StdoutLines[0]);
        Assert.IsTrue(Directory.Exists(root.Campaign()));
        Assert.AreEqual(1, result.ExitCode, "the restored campaign is in an unfinished migration, so it is not closed");
        StringAssert.Contains(result.Stderr, "has an unfinished migration");
    }

    [TestMethod]
    public void ClosingASequelLeavesItsPredecessorAndThePredecessorLineAlone()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, Close("--yes").ExitCode);
        root.WriteCampaign("rorn-two");
        root.Write("player-options.md", "# Player options\n\n- **Predecessor**: rorn\n", "rorn-two");
        string[] predecessor = DmcFolders.Snapshot(root.Adventures, "rorn");
        string options = root.Read("player-options.md", "rorn-two");

        var result = MigrationRoot.Run("close", "rorn-two", "--yes");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(predecessor, DmcFolders.Snapshot(root.Adventures, "rorn"));
        Assert.AreEqual(options, root.Read("player-options.md", "rorn-two"));
        Assert.IsTrue(root.Has(".closed", "rorn-two"));
    }

    [TestMethod]
    public void TheClosingBackupIsNeverCountedAsABackup()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Assert.AreEqual(0, Close("--yes").ExitCode);
        Assert.IsEmpty(BackupFolder.List(root.Campaign(), MigrationRoot.Name));
        Assert.HasCount(1, BackupFolder.Closing(root.Campaign(), MigrationRoot.Name));
        Assert.IsNull(BackupFolder.ParseClosed(MigrationRoot.Name, "rorn_closed_20261111191000-2.zip"), "never suffixed");
        Assert.IsNull(BackupFolder.ParseClosed("ror", "rorn_closed_20261111191000.zip"), "another campaign's");
    }
}
