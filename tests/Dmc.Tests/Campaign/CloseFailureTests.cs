using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// A close that fails part-way leaves the campaign open and nothing from the run (spec 010 FR-011, SC-004; research R3): a
/// failure injected through <see cref="CampaignCloser.BeforeStep"/> at each step is one line naming the step, and the mark,
/// the closing backup, its copy and every partial file are gone, with every campaign file as it was. The mark is written
/// last, so a close stopped before it leaves the campaign open too.
/// </summary>
[TestClass]
public sealed class CloseFailureTests
{
    /// <summary>Clears the fault seam after every test, so no other test sees it.</summary>
    [TestCleanup]
    public void ClearSeam() => CampaignCloser.BeforeStep = null;

    [TestMethod]
    [DataRow("backup")]
    [DataRow("check backup")]
    [DataRow("copy")]
    [DataRow("check copy")]
    [DataRow("mark")]
    public void AFailureAtAnyStepLeavesTheCampaignOpenAndNothingFromTheRun(string step)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Directory.CreateDirectory(root.Backups());
        File.WriteAllText(root.InBackups("rorn_backup_20200110120000.zip"), "stand-in");
        Directory.CreateDirectory(Path.Combine(root.Folder, "exports"));
        string[] before = DmcFolders.Snapshot(root.Folder, "adventures", "exports");

        CampaignCloser.BeforeStep = s =>
        {
            if (s == step)
                throw new IOException("the disk is full");
        };
        var result = MigrationRoot.Run("close", MigrationRoot.Name, "--yes");

        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual($"dmc campaign: could not close rorn: {step}: the disk is full; it is still open", result.Stderr.TrimEnd());
        CollectionAssert.AreEqual(before, DmcFolders.Snapshot(root.Folder, "adventures", "exports"));
        Assert.IsFalse(root.Has(".closed"));
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    [DataRow("mark")]
    [DataRow("check copy")]
    public void FoldersTheRunMadeAreRemovedToo(string step)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        string[] before = DmcFolders.Snapshot(root.Folder, "adventures", "exports");
        CampaignCloser.BeforeStep = s =>
        {
            if (s == step)
                throw new IOException("the disk is full");
        };
        Assert.AreEqual(1, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        CollectionAssert.AreEqual(before, DmcFolders.Snapshot(root.Folder, "adventures", "exports"));
        Assert.IsFalse(Directory.Exists(root.Backups()));
        Assert.IsFalse(Directory.Exists(Path.Combine(root.Folder, "exports")));
    }

    [TestMethod]
    public void TheCloseCanBeRunAgainAfterAFailure()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        CampaignCloser.BeforeStep = s =>
        {
            if (s == "copy")
                throw new IOException("the disk is full");
        };
        Assert.AreEqual(1, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        CampaignCloser.BeforeStep = null;
        var again = MigrationRoot.Run("close", MigrationRoot.Name, "--yes");
        Assert.AreEqual(0, again.ExitCode, again.ToString());
        Assert.IsTrue(root.Has(".closed"));
    }

    [TestMethod]
    public void AStoppedCloseLeavesTheCampaignOpenAndCanBeRunAgain()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        var moment = new DateTime(2026, 11, 11, 18, 0, 0, DateTimeKind.Utc);
        // An exception no rollback catches stands in for the process being stopped just before the last step.
        CampaignCloser.BeforeStep = s =>
        {
            if (s == "mark")
                throw new OperationCanceledException("stopped");
        };
        Assert.ThrowsExactly<OperationCanceledException>(() => CampaignCloser.Close(root.Folder, MigrationRoot.Name, moment));
        Assert.IsFalse(root.Has(".closed"));
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);

        CampaignCloser.BeforeStep = null;
        var plan = CampaignCloser.Close(root.Folder, MigrationRoot.Name, moment.AddSeconds(1));
        Assert.AreEqual(plan.Backup, ClosedCampaign.Read(root.Campaign())!.Backup);
        Assert.AreEqual(0, MigrationRoot.Run("verify", MigrationRoot.Name).ExitCode);
        CollectionAssert.Contains(MigrationRoot.Run("backups", MigrationRoot.Name).StdoutLines, $"closed: backups/{plan.Backup}");
    }

    [TestMethod]
    public void EveryStepIsOffered()
    {
        var seen = new List<string>();
        using var root = new MigrationRoot();
        root.WriteCampaign();
        CampaignCloser.BeforeStep = seen.Add;
        Assert.AreEqual(0, MigrationRoot.Run("close", MigrationRoot.Name, "--yes").ExitCode);
        CollectionAssert.AreEqual(CampaignCloser.Steps, seen);
    }
}
