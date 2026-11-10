using System.Text.RegularExpressions;
using Dmc.Campaign;
using Dmc.Tests.Campaign;
using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The golden test of every migration in <c>kit/migrations/</c> (research R11; spec FR-016): its fixture's <c>before/</c> is backed
/// up with the real <c>dmc campaign backup</c>, replaced with <c>after/</c>, and <c>dmc campaign verify</c> with the fixture's
/// accounting must pass and record the migration's standard. Three spoiled copies must fail: one with a content line
/// removed, one claiming a drop class the migration does not declare, and one with the touched files left as they were.
/// This proves the declared drops, the checks and the accounting agree with a correct result; it does not prove a model
/// following the prose produces it (that is the author's live run).
/// </summary>
[TestClass]
public sealed class MigrationGoldenTests
{
    /// <summary>The campaign name the fixtures are imported under.</summary>
    const string Name = MigrationRoot.Name;

    /// <summary>The migration files of the kit, by number.</summary>
    static IEnumerable<(string Number, MigrationFile Migration)> Migrations() =>
        Directory.EnumerateFiles(Path.Combine(TestEnv.Root, "kit", "migrations"), "*.md").Order(StringComparer.Ordinal)
            .Select(f => MigrationFile.Parse(Path.GetFileName(f), File.ReadAllText(f).Replace("\r\n", "\n")))
            .Select(m => (m.Standard.ToString("000", System.Globalization.CultureInfo.InvariantCulture), m));

    /// <summary>
    /// A DMC folder holding the kit's migrations up to <paramref name="number"/>, the fixture's <c>before/</c> as a campaign at
    /// the standard below it, backed up, then replaced by <c>after/</c> with the fixture's accounting beside the backup.
    /// </summary>
    /// <returns>The folder and the backup's file name.</returns>
    static (MigrationRoot Root, string Backup) Migrated(string number)
    {
        int standard = int.Parse(number, System.Globalization.CultureInfo.InvariantCulture);
        var root = new MigrationRoot();
        string migrations = Path.Combine(root.Folder, "migrations");
        Directory.CreateDirectory(migrations);
        foreach (var (n, migration) in Migrations().Where(m => int.Parse(m.Number, System.Globalization.CultureInfo.InvariantCulture) <= standard))
            File.Copy(Path.Combine(TestEnv.Root, "kit", "migrations", migration.FileName), Path.Combine(migrations, migration.FileName));
        MigrationFixtures.CopyInto(number, "before", root.Campaign());
        root.SetStandard($"{standard - 1}\n");
        string backup = root.Backup();
        MigrationFixtures.CopyInto(number, "after", root.Campaign());
        File.WriteAllText(root.InBackups(backup + ".accounting.tsv"),
            File.ReadAllText(Path.Combine(MigrationFixtures.Of(number), "accounting.tsv")).Replace("\r\n", "\n"));
        return (root, backup);
    }

    /// <summary>Runs verify on the fixture campaign.</summary>
    static ProcessResult Verify() => MigrationRoot.Run("verify", Name);

    [TestMethod]
    public void EveryMigrationHasAFixture()
    {
        Assert.IsNotEmpty(Migrations().ToList(), "kit/migrations/ has migrations");
        foreach (var (number, migration) in Migrations())
            Assert.IsTrue(Directory.Exists(MigrationFixtures.Of(number)), $"{migration.FileName} has no fixture");
    }

    [TestMethod]
    public void EveryFixtureVerifiesAndIsRecorded()
    {
        foreach (var (number, migration) in Migrations())
        {
            var (root, _) = Migrated(number);
            using var __ = root;
            var result = Verify();
            Assert.AreEqual(0, result.ExitCode, $"{migration.FileName}: {result}");
            StringAssert.StartsWith(result.Stdout, "verified rorn: ", migration.FileName);
            Assert.AreEqual($"{migration.Standard}\n", root.Read(".standard"), migration.FileName);
        }
    }

    [TestMethod]
    public void RemovingOneContentLineFailsNamingIt()
    {
        foreach (var (number, migration) in Migrations())
        {
            var (root, _) = Migrated(number);
            using var __ = root;
            var (file, line, unit) = OneUniqueMatchedLine(root, number);
            string text = root.Read(file);
            root.Write(file, text.Remove(text.IndexOf(line + "\n", StringComparison.Ordinal), line.Length + 1));
            var result = Verify();
            Assert.AreEqual(1, result.ExitCode, $"{migration.FileName}: removing \"{line}\" from {file} passed");
            StringAssert.Contains(result.Stdout, $"unaccounted: {ContentUnits.Id(unit)} ", migration.FileName);
        }
    }

    [TestMethod]
    public void ClaimingAnUndeclaredDropClassFailsNamingIt()
    {
        int spoiled = 0;
        foreach (var (number, migration) in Migrations())
        {
            var (root, backup) = Migrated(number);
            using var __ = root;
            string path = root.InBackups(backup + ".accounting.tsv");
            string accounting = File.ReadAllText(path);
            var drop = Regex.Match(accounting, @"\tdropped:([a-z0-9-]+)\t");
            if (!drop.Success)
                continue;
            File.WriteAllText(path, accounting.Remove(drop.Index, drop.Length).Insert(drop.Index, "\tdropped:not-a-declared-class\t"));
            var result = Verify();
            Assert.AreEqual(1, result.ExitCode, migration.FileName);
            StringAssert.Contains(result.Stdout, "(drop class not-a-declared-class is not declared by the pending migrations)", migration.FileName);
            spoiled++;
        }
        Assert.IsGreaterThan(0, spoiled, "at least one fixture drops something, so the case is tested");
    }

    [TestMethod]
    public void LeavingTheTouchedFilesAsTheyWereFailsTheShapeCheck()
    {
        foreach (var (number, migration) in Migrations())
        {
            var (root, _) = Migrated(number);
            using var __ = root;
            string before = Path.Combine(MigrationFixtures.Of(number), "before");
            string after = Path.Combine(MigrationFixtures.Of(number), "after");
            foreach (string file in Directory.EnumerateFiles(after, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(after, file).Replace('\\', '/');
                if (!migration.Touches.Any(t => relative == t || (t.EndsWith('/') && relative.StartsWith(t, StringComparison.Ordinal))))
                    continue;
                string old = Path.Combine(before, relative);
                if (File.Exists(old))
                    root.Write(relative, File.ReadAllText(old).Replace("\r\n", "\n"));
                else
                    File.Delete(Path.Combine(root.Campaign(), relative));
            }
            var result = Verify();
            Assert.AreEqual(1, result.ExitCode, $"{migration.FileName}: the old shape passed: {result}");
            Assert.IsTrue(result.StdoutLines.Any(l => migration.Checks.Any(c => l.StartsWith($"check {c}: ", StringComparison.Ordinal))),
                $"{migration.FileName}: no check of its own failed: {result.Stdout}");
        }
    }

    [TestMethod]
    public void OnFixtureFourEveryFinishedFightIsArchivedWithATimelineLineAndTheMixTrackerIsOneLine()
    {
        string after = Path.Combine(MigrationFixtures.Of("004"), "after");
        string notes = File.ReadAllText(Path.Combine(after, "story", "dm-notes.md")).Replace("\r\n", "\n");
        string archive = File.ReadAllText(Path.Combine(after, "story", "dm-notes-archive.md")).Replace("\r\n", "\n");
        string canon = File.ReadAllText(Path.Combine(after, "story", "canon.md")).Replace("\r\n", "\n");
        string old = File.ReadAllText(Path.Combine(MigrationFixtures.Of("004"), "before", "story", "dm-notes.md")).Replace("\r\n", "\n");
        var dead = old.Split('\n').Where(Dmc.Size.Flags.NoneAlive).ToList();
        Assert.HasCount(2, dead, "the fixture has two finished fights");
        foreach (string row in dead)
        {
            Assert.DoesNotContain(row, notes);
            StringAssert.Contains(archive, row);
            string who = row.Split('|')[1].Trim();
            Assert.IsTrue(Regex.IsMatch(canon, $@"^- .*{Regex.Escape(who)}.*\(s\d+\.\d+\)$", RegexOptions.Multiline | RegexOptions.IgnoreCase), who);
        }
        Assert.IsFalse(notes.Split('\n').Any(Dmc.Size.Flags.NoneAlive), "no dead row remains");
        Assert.AreEqual(1, Regex.Matches(notes, @"^Last played", RegexOptions.Multiline).Count);
        Assert.DoesNotContain("Mix tracker", notes);
    }

    /// <summary>A line of the migrated campaign whose unit appears once in it, in the backup too, and is not accounted for: removing it must fail.</summary>
    static (string File, string Line, string Unit) OneUniqueMatchedLine(MigrationRoot root, string number)
    {
        string before = Path.Combine(MigrationFixtures.Of(number), "before");
        var oldUnits = Directory.EnumerateFiles(before, "*.md", SearchOption.AllDirectories)
            .SelectMany(f => ContentUnits.Split(File.ReadAllText(f))).ToHashSet(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var where = new List<(string File, string Line, string Unit)>();
        foreach (string file in Directory.EnumerateFiles(root.Campaign(), "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string relative = Path.GetRelativePath(root.Campaign(), file).Replace('\\', '/');
            if (relative.StartsWith("backups/", StringComparison.Ordinal))
                continue;
            foreach (string line in File.ReadAllText(file).Split('\n'))
            {
                if (ContentUnits.Normalise(line) is not { } unit)
                    continue;
                counts[unit] = counts.GetValueOrDefault(unit) + 1;
                where.Add((relative, line, unit));
            }
        }
        // A plain content line (not a heading, so its removal leaves the file's shape alone), whose text is not inside another line.
        var pick = where.FirstOrDefault(w => counts[w.Unit] == 1 && oldUnits.Contains(w.Unit) && !w.Line.StartsWith('#') && !w.Line.StartsWith('|')
            && !where.Any(o => o.Unit != w.Unit && o.Unit.Contains(w.Unit, StringComparison.Ordinal)));
        Assert.IsNotNull(pick.File, $"fixture {number} has no line to remove");
        return pick;
    }
}
