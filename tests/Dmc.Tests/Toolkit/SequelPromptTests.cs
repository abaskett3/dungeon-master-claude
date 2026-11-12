namespace Dmc.Tests.Toolkit;

/// <summary>
/// The prompt checks of the sequel import (spec 011 PR 2, contracts/prompts.md): <c>dmc-new-campaign</c>'s § 1c runs the
/// <c>dmc campaign sequel</c> forms and keeps its progress, and the rule for a carried character's sheet lives once, in
/// <c>dmc-character-sheet-update</c>.
/// </summary>
public sealed partial class ToolkitTests
{
    /// <summary>The new-campaign skill.</summary>
    const string NewCampaignSkill = "kit/skills/dmc-new-campaign/SKILL.md";

    /// <summary>The character-sheet skill.</summary>
    const string SheetSkill = "kit/skills/dmc-character-sheet-update/SKILL.md";

    /// <summary>The text of the new-campaign skill's § 1c, up to § 2.</summary>
    static string SequelSection()
    {
        string text = Read(NewCampaignSkill);
        int start = At(text, "## 1c. A sequel", NewCampaignSkill), end = At(text, "## 2. Session zero", NewCampaignSkill);
        Assert.IsGreaterThan(At(text, "## 1b.", NewCampaignSkill), start, "§ 1c comes after § 1b");
        return text[start..end];
    }

    /// <summary>§ 1c runs the four forms of <c>dmc campaign sequel</c>, writes the digest and keeps its progress in <c>import/progress.md</c>.</summary>
    [TestMethod]
    public void TheSequelSectionRunsTheFourForms()
    {
        string section = SequelSection();
        StringAssert.Contains(section, "dmc campaign sequel");
        foreach (string form in new[] { "--start", "--list", "--check", "--sample", "--round" })
            StringAssert.Contains(section, form);
        StringAssert.Contains(section, "import/progress.md");
        StringAssert.Contains(section, "digests/");
        StringAssert.Contains(section, "§ 1b", "the parts and subagents follow § 1b, which § 1c points to");
        StringAssert.Contains(section, "only after the sample passes");
    }

    /// <summary>The skill's description says it starts a sequel, so the player's words find it.</summary>
    [TestMethod]
    public void TheNewCampaignSkillSaysItStartsASequel()
    {
        string description = Read(NewCampaignSkill).Split('\n').Single(l => l.StartsWith("description:", StringComparison.Ordinal));
        StringAssert.Contains(description, "continue a closed campaign as a sequel");
    }

    /// <summary>
    /// The sheet rule for a character from a linked campaign lives in <c>dmc-character-sheet-update</c>; § 1c names that skill
    /// and does not restate the rule.
    /// </summary>
    [TestMethod]
    public void TheCarriedCharactersRuleLivesInTheSheetSkill()
    {
        string sheet = Read(SheetSkill);
        StringAssert.Contains(sheet, "A character from a linked campaign");
        StringAssert.Contains(sheet, "digests/");
        Assert.IsTrue(sheet.Contains("dmc recall", StringComparison.Ordinal) || sheet.Contains("dmc lookup", StringComparison.Ordinal), SheetSkill);
        StringAssert.Contains(sheet, "species or class defaults only where it says nothing");

        string section = SequelSection();
        StringAssert.Contains(section, "dmc-character-sheet-update");
        Assert.DoesNotContain("species or class defaults", section, "the rule has one home");
    }
}
