using System.Text;
using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The closed mark, <c>adventures/&lt;campaign&gt;/.closed</c> (spec 010, data-model.md § Closed mark; research R1, R8): its
/// one-line form, that its presence alone closes a campaign, and the one refusal every writing command gives.
/// </summary>
[TestClass]
public sealed class ClosedCampaignTests
{
    /// <summary>A moment with no fraction of a second, as the mark keeps it.</summary>
    static readonly DateTime Moment = new(2026, 11, 12, 3, 10, 0, DateTimeKind.Utc);

    [TestMethod]
    public void WriteThenReadRoundTripsTheTimeAndTheFileName()
    {
        using var root = new MigrationRoot();
        string campaign = root.WriteCampaign();
        ClosedCampaign.Write(campaign, Moment, "rorn_closed_20261111191000.zip");
        var mark = ClosedCampaign.Read(campaign);
        Assert.IsNotNull(mark);
        Assert.AreEqual(Moment, mark.ClosedAt);
        Assert.AreEqual(DateTimeKind.Utc, mark.ClosedAt.Kind);
        Assert.AreEqual("rorn_closed_20261111191000.zip", mark.Backup);
        Assert.AreEqual("closed: 2026-11-12T03:10:00Z", ClosedCampaign.Line(mark));
    }

    [TestMethod]
    public void TheMarkIsOneLineEndingInALineFeedWithNoByteOrderMark()
    {
        using var root = new MigrationRoot();
        string campaign = root.WriteCampaign();
        ClosedCampaign.Write(campaign, Moment, "rorn_closed_20261111191000.zip");
        byte[] bytes = File.ReadAllBytes(Path.Combine(campaign, ".closed"));
        CollectionAssert.AreEqual(new UTF8Encoding(false).GetBytes("2026-11-12T03:10:00Z rorn_closed_20261111191000.zip\n"), bytes);
        Assert.AreEqual(1, Directory.GetFiles(campaign, ".closed*").Length, "no partial file is left");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("garbled")]
    [DataRow("2026-11-12T03:10:00Z")]
    [DataRow("2026-11-12 rorn_closed_20261111191000.zip\n")]
    [DataRow("2026-11-12T03:10:00Z backups/rorn_closed_20261111191000.zip\n")]
    [DataRow("2026-11-12T03:10:00Z rorn_closed_20261111191000.zip extra\n")]
    [DataRow("2026-11-12T03:10:00Z rorn_closed_20261111191000.zip\n\n")]
    public void AGarbledMarkStillClosesTheCampaignButCannotBeRead(string text)
    {
        using var root = new MigrationRoot();
        string campaign = root.WriteCampaign();
        Assert.IsFalse(ClosedCampaign.IsClosed(campaign));
        root.Write(".closed", text);
        Assert.IsTrue(ClosedCampaign.IsClosed(campaign));
        Assert.IsNull(ClosedCampaign.Read(campaign));
        Assert.AreEqual("closed: (mark unreadable)", ClosedCampaign.Line(null));
    }

    [TestMethod]
    public void ACrlfLineEndIsRead()
    {
        var mark = ClosedCampaign.Parse(Encoding.UTF8.GetBytes("2026-11-12T03:10:00Z rorn_closed_20261111191000.zip\r\n"));
        Assert.AreEqual("rorn_closed_20261111191000.zip", mark?.Backup);
    }

    [TestMethod]
    public void RefuseThrowsForAnyPathInAClosedCampaign()
    {
        using var root = new MigrationRoot();
        string campaign = root.WriteCampaign();
        root.Write(".closed", "x");
        foreach (string path in new[]
        {
            campaign,
            campaign + Path.DirectorySeparatorChar,
            Path.Combine(campaign, "story", "canon.md"),
            Path.Combine(campaign, "maps", "new", "deeper"),
        })
        {
            var e = Assert.ThrowsExactly<DmcException>(() => ClosedCampaign.Refuse(root.Folder, path), path);
            Assert.AreEqual("rorn is closed: start a sequel to continue its story", e.Message);
        }
    }

    [TestMethod]
    public void RefuseLetsEveryOtherPathThrough()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.Write(".closed", "x");
        string open = root.WriteCampaign("open");
        Directory.CreateDirectory(Path.Combine(root.Adventures, ".transcript-review"));
        foreach (string path in new[]
        {
            open,
            Path.Combine(open, "story", "canon.md"),
            root.Adventures,
            Path.Combine(root.Adventures, "player-options.md"),
            Path.Combine(root.Adventures, ".transcript-review", "rorn"),
            Path.Combine(root.Folder, "exports"),
            Path.Combine(root.Folder, "rorn"),
            Path.Combine(root.Adventures, "rorn-sequel", "x.md"),
        })
        {
            ClosedCampaign.Refuse(root.Folder, path);
        }
    }

    [TestMethod]
    public void RefuseIgnoresCaseOnlyWhereTheFileSystemDoes()
    {
        using var root = new MigrationRoot();
        string campaign = root.WriteCampaign();
        root.Write(".closed", "x");
        string upper = Path.Combine(root.Folder, "ADVENTURES", "RORN", "story");
        if (OperatingSystem.IsLinux())
            ClosedCampaign.Refuse(root.Folder, upper);
        else
            Assert.ThrowsExactly<DmcException>(() => ClosedCampaign.Refuse(root.Folder, upper));
        Assert.IsTrue(Directory.Exists(campaign));
    }
}
