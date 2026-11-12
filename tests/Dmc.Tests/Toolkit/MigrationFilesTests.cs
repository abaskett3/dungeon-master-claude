using Dmc.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The build-time checks of every migration in <c>kit/migrations/</c> (contracts/migration-file.md § What dmc checks): the
/// numbering, the front matter, the checks it names, the files it touches, the five headings, the declared drops and the
/// golden fixture.
/// </summary>
[TestClass]
public sealed class MigrationFilesTests
{
    /// <summary>The migration files of the kit, by name, in order.</summary>
    static string[] Files() =>
        [.. Directory.EnumerateFiles(Path.Combine(TestEnv.Root, "kit", "migrations"), "*.md")
            .Select(f => Path.GetFileName(f)!).Order(StringComparer.Ordinal)];

    /// <summary>Every migration of the kit, read by the program's own reader (which also checks the headings and the declared drops).</summary>
    static List<MigrationFile> Migrations() =>
        [.. Files().Select(name => MigrationFile.Parse(name, File.ReadAllText(Path.Combine(TestEnv.Root, "kit", "migrations", name)).Replace("\r\n", "\n")))];

    /// <summary>
    /// The optional <c>## Before the backup</c> section (spec 011 R13), asked before the backup so a stop changes nothing,
    /// sits after <c>## What changes</c> and before <c>## Steps</c> wherever a migration has it; one without it is read too.
    /// </summary>
    [TestMethod]
    public void BeforeTheBackupSitsBetweenWhatChangesAndStepsWhereverItIs()
    {
        foreach (var migration in Migrations().Where(m => m.Headings.Contains("Before the backup")))
        {
            var headings = migration.Headings.ToList();
            int before = headings.IndexOf("Before the backup");
            Assert.IsTrue(headings.IndexOf("What changes") < before && before < headings.IndexOf("Steps"), $"{migration.FileName}: {string.Join(", ", headings)}");
        }
        Assert.IsTrue(Migrations().Any(m => !m.Headings.Contains("Before the backup")), "a migration without the section is read as before");
        Assert.IsTrue(Migrations().Any(m => m.Headings.Contains("Before the backup")), "migration 005 has the section");
    }

    [TestMethod]
    public void EveryFileIsAMigrationAndTheyRunFromTwoWithoutAGap()
    {
        string[] files = Files();
        foreach (string name in files)
            Assert.IsTrue(MigrationFile.IsMigrationName(name), $"{name} in kit/migrations/ is not named NNN-slug.md");
        var migrations = Migrations();
        for (int i = 0; i < migrations.Count; i++)
            Assert.AreEqual(i + 2, migrations[i].Standard, migrations[i].FileName);
        Assert.AreEqual(migrations.Count, new MigrationSet(migrations).Kit - 1);
    }

    [TestMethod]
    public void EveryCheckANameNamesExists()
    {
        foreach (var migration in Migrations())
        {
            Assert.IsNotEmpty(migration.Checks, migration.FileName);
            foreach (string check in migration.Checks)
                Assert.IsTrue(MigrationChecks.Exists(check), $"{migration.FileName} names the check {check}, which MigrationChecks does not have");
        }
    }

    [TestMethod]
    public void EveryTouchedPathIsACampaignPath()
    {
        foreach (var migration in Migrations())
        {
            Assert.IsNotEmpty(migration.Touches, migration.FileName);
            foreach (string path in migration.Touches)
            {
                string trimmed = path.TrimEnd('/');
                Assert.IsNull(ArchivePaths.Refusal(trimmed), $"{migration.FileName} touches {path}, which is not a relative campaign path");
                Assert.IsTrue(CampaignFiles.Counts(trimmed), $"{migration.FileName} touches {path}, which is not a campaign file");
            }
        }
    }

    [TestMethod]
    public void EveryMigrationHasItsHeadingsInOrderAndADeclaredLinePerDrop()
    {
        foreach (var migration in Migrations())
        {
            var order = MigrationFile.RequiredHeadings.Select(h => migration.Headings.ToList().IndexOf(h)).ToList();
            CollectionAssert.AreEqual(order.Order().ToList(), order, migration.FileName);
            foreach (string drop in migration.Drops)
                Assert.IsTrue(migration.DeclaredDrops.Any(l => l.Contains($"`{drop}`", StringComparison.Ordinal)), $"{migration.FileName}: {drop} has no line under ## Declared drops");
        }
    }

    [TestMethod]
    public void EveryMigrationHasAGoldenFixture()
    {
        var numbers = MigrationFixtures.Numbers();
        foreach (var migration in Migrations())
        {
            string number = migration.Standard.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
            CollectionAssert.Contains(numbers.ToArray(), number, $"{migration.FileName} has no fixture in tests/Dmc.Tests/Fixtures/migrations/{number}/");
            string folder = MigrationFixtures.Of(number);
            Assert.IsTrue(Directory.Exists(Path.Combine(folder, "before")), $"{number}/before");
            Assert.IsTrue(Directory.Exists(Path.Combine(folder, "after")), $"{number}/after");
            Assert.IsTrue(File.Exists(Path.Combine(folder, "accounting.tsv")), $"{number}/accounting.tsv");
        }
        Assert.HasCount(Migrations().Count, numbers, "a fixture folder per migration, and no other");
    }
}
