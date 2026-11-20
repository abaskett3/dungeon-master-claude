namespace Dmc.Tests.Toolkit;

/// <summary>
/// Spec 020 (issue #245): the kit's settings deny the usual shell writes and Claude Code's <c>SendFeedback</c> tool, so
/// DMC changes campaign files with Edit as <c>kit/dm-rules.md</c> says (FR-001 to FR-003), and the skills that write
/// <c>adventures/.active-campaign</c> name the Write tool (FR-004).
/// </summary>
public sealed partial class ToolkitTests
{
    /// <summary>
    /// The shell writes play must not run (spec 020 FR-001), as <c>Tool(pattern)</c> deny rules: in-place <c>sed</c>, the
    /// script runners and <c>tee</c> in Bash, and the content cmdlets and script runners in PowerShell. They match 43 of the
    /// 45 shell writes in the play logs (research R1).
    /// </summary>
    static readonly string[] ShellWriteRules =
    [
        "Bash(sed -i*)", "Bash(sed * -i*)", "Bash(python *)", "Bash(python3 *)", "Bash(py *)", "Bash(node *)", "Bash(perl *)", "Bash(tee *)",
        "PowerShell(Set-Content *)", "PowerShell(Add-Content *)", "PowerShell(Out-File *)", "PowerShell(python *)", "PowerShell(py *)",
    ];

    [TestMethod]
    public void TheKitSettingsDenyShellWritesAndSendFeedback()
    {
        string[] denied = Settings(KitSettings)["permissions"]!["deny"]!.AsArray().Select(r => (string)r!).ToArray();
        foreach (string rule in ShellWriteRules)
            CollectionAssert.Contains(denied, rule, $"kit/settings.json does not deny {rule}");
        CollectionAssert.Contains(denied, "SendFeedback", "kit/settings.json does not deny SendFeedback (FR-002)");

        foreach (string command in new[]
                 {
                     "sed -i 's/a/b/' a.md", "sed -i.bak 's/a/b/' a.md", "sed -e 's/a/b/' -i a.md", "sed -n '1p' -i a.md",
                     "python fix.py", "python -c \"print(1)\"", "python3 - <<'EOF'", "py fix.py", "node -e \"x\"", "perl -pi -e 's/a/b/' a.md",
                     "tee a.md", "tee -a adventures/x/game-state.md", "python", "python3",
                 })
            Assert.IsTrue(Denied("Bash", command), $"Bash: {command}");
        foreach (string command in new[]
                 {
                     "Set-Content adventures/.active-campaign x", "set-content -Path a.md -Value x", "Add-Content a.md x", "Out-File a.md",
                     "out-file -FilePath a.md -Encoding utf8", "python fix.py", "py -3 fix.py", "Py fix.py",
                 })
            Assert.IsTrue(Denied("PowerShell", command), $"PowerShell: {command}");

        // What play does run is not caught: reading, dmc, and commands that only start with the same letters.
        foreach (string command in new[]
                 {
                     "sed -n '1,5p' a.md", "sed 's/a/b/' a.md", "cat a.md", "dmc roll 1d20", "pythonx a", "nodemon a", "perlx a", "teex a", "pyx a", "ls adventures",
                 })
            Assert.IsFalse(Denied("Bash", command), $"Bash: {command}");
        foreach (string command in new[] { "Get-Content a.md", "dmc roll 1d20", "Set-Contentx a", "Out-Filex a", "pyx a", "Get-ChildItem adventures" })
            Assert.IsFalse(Denied("PowerShell", command), $"PowerShell: {command}");

        // Toolkit work is not blocked: none of these rules is in the developer's settings.
        string[] shared = Settings(DevSettings)["permissions"]?["deny"]?.AsArray().Select(r => (string)r!).ToArray() ?? [];
        foreach (string rule in ShellWriteRules.Append("SendFeedback"))
            CollectionAssert.DoesNotContain(shared, rule, $".claude/settings.json applies to toolkit work and must not deny {rule}");

        // The auto mode stays (research R4): the kit never sets a permission mode or Remote Control at startup.
        var settings = Settings(KitSettings);
        Assert.IsNull(settings["permissions"]?["defaultMode"], "kit/settings.json must not set a permission mode");
        Assert.IsNull(settings["remoteControlAtStartup"], "kit/settings.json must never set remoteControlAtStartup");
    }

    /// <summary>
    /// The two skills that write <c>adventures/.active-campaign</c> name the Write tool for it, so DMC does not reach for
    /// <c>Set-Content</c>, which the kit now denies (spec 020 FR-004, log <c>9eec3bdf</c>).
    /// </summary>
    [TestMethod]
    public void TheSkillsWriteTheActiveCampaignWithTheWriteTool()
    {
        StringAssert.Contains(Read("kit/skills/dmc-new-campaign/SKILL.md"), "as the only line of `adventures/.active-campaign` with the Write tool.");
        StringAssert.Contains(Read("kit/skills/dmc-resume-campaign/SKILL.md"), "Write the chosen folder name to `adventures/.active-campaign` with the Write tool.");
    }
}
