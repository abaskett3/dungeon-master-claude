namespace Dmc.Tests.Toolkit;

/// <summary>
/// The prompt and template changes of spec 016, the feedback round of #181, that a later edit could drop without any
/// code test noticing: the session log's Present line (FR-003), the companion definition (FR-032), the full save's sheet
/// check and move (FR-005, FR-032) and its arc check (FR-026), the sheet skill's widened default rule (FR-006), and the
/// clauses of <c>kit/dm-rules.md</c> that carry A and B.
/// </summary>
public sealed partial class ToolkitTests
{
    /// <summary>The session log template has the Present line and quotes replies (A3, A7).</summary>
    [TestMethod]
    public void TheSessionLogRecordsWhoWasPresentAndQuotesReplies()
    {
        string log = Read("kit/templates/session-log.md");
        StringAssert.Contains(log, "- Present: everyone in the scene");
        StringAssert.Contains(log, "a reply that matters to a relationship is quoted, never summarised");
        StringAssert.Contains(Read("kit/dm-rules.md"), "check the scene's Present line in the session log");
    }

    /// <summary>The game-state template counts anyone the player directs in a fight as a companion (FR-032).</summary>
    [TestMethod]
    public void ACompanionIsAnyoneThePlayerDirectsInAFight()
    {
        StringAssert.Contains(Read("kit/templates/campaign/game-state.md"), "as one of its own, or directed by the player in a fight");
        StringAssert.Contains(Read("kit/skills/dmc-campaign-full-save/SKILL.md"), "whom the player directed in a fight this session is a companion");
    }

    /// <summary>The full save fixes a sheet line this session overturned and runs the arc check without telling the player.</summary>
    [TestMethod]
    public void TheFullSaveFixesOverturnedLinesAndRunsTheArcCheckSilently()
    {
        string save = Read("kit/skills/dmc-campaign-full-save/SKILL.md");
        StringAssert.Contains(save, "overturns a standing line or the stat block");
        StringAssert.Contains(save, "run `dmc size <campaign> --arc`");
        StringAssert.Contains(save, "Never mention these lines to the player");
    }

    /// <summary>The rules carry the clauses of A and B, the correction rule's approved wording included.</summary>
    [TestMethod]
    public void TheRulesCarryTheClausesOfAAndB()
    {
        string rules = Read("kit/dm-rules.md");
        StringAssert.Contains(rules, "rewrite that line of the sheet (and the stat block), not only its History");
        StringAssert.Contains(rules, "A species or class never says what an established character can do");
        StringAssert.Contains(rules, "read their sheet down to its History, once per session part");
        StringAssert.Contains(rules, "Correct only the fact that was wrong");
        StringAssert.Contains(rules, "read the files and open your reply by quoting what they say, for and against");
        StringAssert.Contains(rules, "change no file until they agree; `game-state.md` may note the fix as waiting on them");
    }
}
