namespace Dmc.Tests.Campaign;

/// <summary>
/// <c>dmc campaign standard [NAME] [--init]</c> (contracts/campaign-commands.md; data-model.md § Standard): one line for where a
/// campaign stands against the kit's <c>migrations/</c>, in a temporary DMC folder with hand-made migration files.
/// </summary>
[TestClass]
public sealed class StandardTests
{
    [TestMethod]
    public void NoRecordAndKitFourIsBehindWithThePendingMigrations()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        var result = MigrationRoot.Run("standard", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "behind: standard 1, kit 4: 002-test-2.md, 003-test-3.md, 004-test-4.md" }, result.StdoutLines);
    }

    [TestMethod]
    public void ARecordEqualToTheKitIsCurrent()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard("4\n");
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
        root.SetStandard("3\r\n");
        CollectionAssert.AreEqual(new[] { "behind: standard 3, kit 4: 004-test-4.md" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void ARecordAboveTheKitIsAhead()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard("9\n");
        CollectionAssert.AreEqual(new[] { "ahead: standard 9, kit 4: update dmc" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void AMarkedBackupIsUnfinishedWhateverTheRecordSays()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard("2\n");
        string backup = root.Backup();
        root.SetStandard("4\n");
        CollectionAssert.AreEqual(new[] { $"unfinished: {backup}" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    [DataRow("x")]
    [DataRow("0\n")]
    [DataRow("4\n5\n")]
    [DataRow("")]
    [DataRow(" 4")]
    public void AnUnreadableRecordIsOneLineAndExitOne(string record)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard(record);
        var result = MigrationRoot.Run("standard", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("dmc campaign: adventures/rorn/.standard is unreadable: it must hold one whole number", result.Stderr.TrimEnd());
    }

    [TestMethod]
    public void NoMigrationsMeansKitOneSoNoRecordIsCurrent()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void NoNameMeansTheActiveCampaign()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(2);
        File.WriteAllText(Path.Combine(root.Adventures, ".active-campaign"), "rorn\n");
        CollectionAssert.AreEqual(new[] { "behind: standard 1, kit 2: 002-test-2.md" }, MigrationRoot.Run("standard").StdoutLines);
        File.Delete(Path.Combine(root.Adventures, ".active-campaign"));
        var none = MigrationRoot.Run("standard");
        Assert.AreEqual(1, none.ExitCode);
        Assert.AreEqual(1, none.StderrLines.Length);
    }

    [TestMethod]
    public void InitWritesTheKitsStandardOnce()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        var result = MigrationRoot.Run("standard", MigrationRoot.Name, "--init");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "initialised rorn at standard 4" }, result.StdoutLines);
        Assert.AreEqual("4\n", root.Read(".standard"));
        CollectionAssert.AreEqual(new[] { "current" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);

        var again = MigrationRoot.Run("standard", MigrationRoot.Name, "--init");
        Assert.AreEqual(1, again.ExitCode);
        Assert.AreEqual("dmc campaign: rorn already has a standard: only dmc campaign verify changes it", again.Stderr.TrimEnd());
        Assert.AreEqual("4\n", root.Read(".standard"));
    }

    [TestMethod]
    public void AMissingCampaignOrADotFolderIsOneLineAndExitOne()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        Directory.CreateDirectory(Path.Combine(root.Adventures, ".dmc-import-1234"));
        foreach (string name in new[] { "nope", ".dmc-import-1234" })
        {
            var result = MigrationRoot.Run("standard", name);
            Assert.AreEqual(1, result.ExitCode, name);
            Assert.AreEqual("", result.Stdout);
            StringAssert.StartsWith(result.Stderr, $"dmc campaign: no campaign \"{name}\" in adventures/");
        }
    }

    /// <summary>A readable closed mark, as <c>dmc campaign close --yes</c> writes it.</summary>
    const string Mark = "2026-11-12T03:10:00Z rorn_closed_20261111191000.zip\n";

    [TestMethod]
    [DataRow("unfinished")]
    [DataRow("behind")]
    [DataRow("ahead")]
    [DataRow("current")]
    public void AClosedCampaignIsClosedAheadOfEveryOtherStatus(string otherwise)
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        switch (otherwise)
        {
            case "unfinished":
                root.SetStandard("2\n");
                root.Backup();
                break;
            case "behind":
                root.SetStandard("2\n");
                break;
            case "ahead":
                root.SetStandard("9\n");
                break;
            default:
                root.SetStandard("4\n");
                break;
        }
        StringAssert.StartsWith(MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines[0], otherwise);
        root.Write(".closed", Mark);
        var result = MigrationRoot.Run("standard", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "closed: 2026-11-12T03:10:00Z" }, result.StdoutLines);
    }

    [TestMethod]
    public void AClosedCampaignIsClosedEvenWithAnUnreadableRecordOrKit()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard("x");
        File.Delete(Path.Combine(root.Folder, "migrations", "003-test-3.md"));
        root.Write(".closed", Mark);
        CollectionAssert.AreEqual(new[] { "closed: 2026-11-12T03:10:00Z" }, MigrationRoot.Run("standard", MigrationRoot.Name).StdoutLines);
    }

    [TestMethod]
    public void TheActiveCampaignClosedIsClosedWithNoName()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.Write(".closed", Mark);
        File.WriteAllText(Path.Combine(root.Adventures, ".active-campaign"), "rorn\n");
        CollectionAssert.AreEqual(new[] { "closed: 2026-11-12T03:10:00Z" }, MigrationRoot.Run("standard").StdoutLines);
    }

    [TestMethod]
    public void AGarbledMarkStillReadsClosed()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.Write(".closed", "nonsense\nmore nonsense\n");
        var result = MigrationRoot.Run("standard", MigrationRoot.Name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "closed: (mark unreadable)" }, result.StdoutLines);
    }

    [TestMethod]
    public void InitOnAClosedCampaignIsRefusedAndWritesNothing()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.Write(".closed", Mark);
        var result = MigrationRoot.Run("standard", MigrationRoot.Name, "--init");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("dmc campaign: rorn is closed: start a sequel to continue its story", result.Stderr.TrimEnd());
        Assert.IsFalse(root.Has(".standard"));
    }

    [TestMethod]
    public void RefreshesBehindLineDoesNotCountAClosedCampaign()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        root.SetStandard("2\n");
        StringAssert.Contains(Dmc.Kit.Refresher.BehindLine("dmc refresh", root.Folder), "1 campaign is behind");
        root.Write(".closed", Mark);
        Assert.IsNull(Dmc.Kit.Refresher.BehindLine("dmc refresh", root.Folder));
    }

    [TestMethod]
    public void ABrokenMigrationsFolderIsOneLine()
    {
        using var root = new MigrationRoot();
        root.WriteCampaign();
        root.WriteMigrations(4);
        File.Delete(Path.Combine(root.Folder, "migrations", "003-test-3.md"));
        var result = MigrationRoot.Run("standard", MigrationRoot.Name);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc campaign: migrations/ is damaged: expected migration 003, found 004-test-4.md (run dmc update)", result.Stderr.TrimEnd());
    }
}
