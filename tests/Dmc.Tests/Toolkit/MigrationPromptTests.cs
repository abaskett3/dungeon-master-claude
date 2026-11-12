using System.Text.RegularExpressions;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The prompt checks of campaign migrations (spec 005): the <c>dmc-migrate-campaign</c> skill runs the <c>dmc</c> steps in their
/// order and holds none of the rules <c>dmc</c> owns; resume and new-campaign call <c>dmc campaign standard</c>; the feedback
/// flow requires a migration for a change to a campaign-file standard; and no skill repairs an older campaign file as it goes.
/// </summary>
public sealed partial class ToolkitTests
{
    /// <summary>The migrate skill.</summary>
    const string MigrateSkill = "kit/skills/dmc-migrate-campaign/SKILL.md";

    /// <summary>The position of a text in another, failing when it is not there.</summary>
    static int At(string text, string what, string file)
    {
        int at = text.IndexOf(what, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, at, $"{file} does not say \"{what}\"");
        return at;
    }

    /// <summary>
    /// Migration 005 asks about an open predecessor before its backup (spec 011 FR-028a): with the standard check, the close
    /// skill, the player's stop line word for word, and the active campaign left alone (the close names its campaign). The
    /// migrate skill follows any migration's section before it backs up.
    /// </summary>
    [TestMethod]
    public void MigrationFiveAsksAboutAnOpenPredecessorBeforeTheBackup()
    {
        string text = Read("kit/migrations/005-continuity-links.md");
        int start = At(text, "## Before the backup", "migration 005"), end = At(text, "## Steps", "migration 005");
        string section = text[start..end];
        StringAssert.Contains(section, "dmc campaign standard");
        StringAssert.Contains(section, "dmc-close-campaign");
        StringAssert.Contains(section.Replace("\n", " "),
            "Migration stopped until the previous campaign is closed. You can close it by running: /dmc-close-campaign <predecessor>");
        Assert.DoesNotContain(".active-campaign", section, "the close is pointed at the predecessor by name, never by switching the active campaign");
        // The live run asked "are you sure you want to keep it open?", where yes means stop: the question says what yes means.
        StringAssert.Contains(section.Replace("\n", " ").Replace("    ", " "), "whether they are sure they want to go on without closing it. On yes, go on");

        string migrate = Read(MigrateSkill);
        int ask = At(migrate, "## 2. Ask", MigrateSkill), backUp = At(migrate, "## 3. Back up", MigrateSkill);
        StringAssert.Contains(migrate[ask..backUp], "## Before the backup");
        StringAssert.Contains(migrate[ask..backUp].Replace("\n", " "), "come back here and go on to step 3");
    }

    [TestMethod]
    public void TheMigrateSkillRunsTheDmcStepsInOrder()
    {
        string text = Read(MigrateSkill);
        var front = Regex.Match(text, @"^---\n(.*?)\n---\n", RegexOptions.Singleline);
        Assert.IsTrue(front.Success, "front matter");
        string description = front.Groups[1].Value.Split('\n').Single(l => l.StartsWith("description:", StringComparison.Ordinal));
        StringAssert.Contains(description, "bring their files up to date", "the player's words trigger it");

        int standard = At(text, "`dmc campaign standard <campaign>`", MigrateSkill);
        int backups = At(text, "`dmc campaign backups <campaign>`", MigrateSkill);
        int backup = At(text, "`dmc campaign backup <campaign>`", MigrateSkill);
        int migrate = At(text, "## 4. Migrate", MigrateSkill);
        int list = At(text, "`dmc campaign verify <campaign> --list`", MigrateSkill);
        int verify = At(text, "`dmc campaign verify <campaign>`.", MigrateSkill);
        At(text, "`dmc campaign restore <campaign>", MigrateSkill);
        Assert.IsTrue(standard < backups && backups < backup && backup < migrate && migrate < list && list < verify,
            "standard, backups, backup, the steps, verify --list, then verify");
        Assert.IsGreaterThan(verify, At(text, "`dmc size <campaign> --check`", MigrateSkill), "dmc size runs after a passing verify");
    }

    [TestMethod]
    public void TheMigrateSkillLeavesTheRulesToDmc()
    {
        string text = Read(MigrateSkill);
        StringAssert.Contains(text, "only if the player said yes", "--prune is passed only on the player's yes");
        StringAssert.Contains(text, "If its last line starts `ask:`");
        Assert.IsFalse(Regex.IsMatch(text, @"\b(three|3) backups\b|\bkeep(ing)? (the latest )?(two|three)\b", RegexOptions.IgnoreCase),
            "the backup count lives in dmc, which prints the question");
        StringAssert.Contains(text, "take no new backup", "an unfinished migration takes no backup");
        StringAssert.Contains(text, "middle of a fight");
        StringAssert.Contains(text, "roll pending");
        StringAssert.Contains(text, "Do not edit `.standard`, anything in `migrations/`, or anything in the campaign's\n`backups/` folder except the accounting file");
        StringAssert.Contains(text, "`skipped`", "a file left alone is named in the accounting file");
        StringAssert.Contains(text, "Do not go on with play until the\n  player has chosen");
        Assert.IsFalse(Regex.IsMatch(text, @"\d+ tokens|budget", RegexOptions.IgnoreCase), "no size rule of dmc size is quoted");
    }

    [TestMethod]
    public void ResumeChecksTheStandardAndNewCampaignRecordsIt()
    {
        string resume = SkillSection("kit/skills/dmc-resume-campaign/SKILL.md", 1);
        StringAssert.Contains(resume, "`dmc campaign standard <campaign>`");
        StringAssert.Contains(resume, "say nothing if it prints `current`");
        StringAssert.Contains(resume, "on `behind`");
        StringAssert.Contains(resume, "`dmc-migrate-campaign`");
        StringAssert.Contains(resume, "on `ahead`");
        StringAssert.Contains(resume, "on `unfinished`, that a migration was interrupted and play waits");
        StringAssert.Contains(SkillSection("kit/skills/dmc-new-campaign/SKILL.md", 1), "`dmc campaign standard <folder> --init`");
    }

    [TestMethod]
    public void AChangeToACampaignFileStandardRequiresAMigration()
    {
        string feedback = Read(".claude/skills/review-feedback/SKILL.md");
        StringAssert.Contains(feedback, "kit/migrations/", "review-feedback names the migrations folder");
        StringAssert.Contains(feedback, "tests/Dmc.Tests/Fixtures/migrations/", "and the golden fixture");
        StringAssert.Contains(feedback, "or says in the PR why none is needed");
        string development = Read(".docs/development.md");
        StringAssert.Contains(development, "kit/migrations/");
        StringAssert.Contains(development, "tests/Dmc.Tests/Fixtures/migrations/");
        StringAssert.Contains(development, "why none is needed");
        StringAssert.Contains(Read("CLAUDE.md"), "which migration it adds, or why none is needed");
    }

    /// <summary>Wording that tells DMC to repair an older campaign file as it goes: that work is a migration's (FR-017, SC-008).</summary>
    [GeneratedRegex(@"if an older|lacks it\)|move it there if it is not|the full save moves its history last", RegexOptions.IgnoreCase)]
    private static partial Regex CatchUpClause();

    [TestMethod]
    public void NoSkillRepairsAnOlderCampaignFileAsItGoes()
    {
        foreach (string file in MarkdownUnder("kit/skills"))
        {
            var match = CatchUpClause().Match(Read(file));
            Assert.IsFalse(match.Success, $"{file} says \"{match.Value}\": a repair of older files belongs in a migration in kit/migrations/");
        }
    }
}
