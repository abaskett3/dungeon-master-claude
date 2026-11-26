using System.Text.Json.Nodes;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The sheet-request line <c>dmc context-check</c> prints last (spec 031 FR-019), in this process and through the
/// published binary: one line for a message that names a sheet, the slip test's plain request among them, and nothing
/// for any other message or a command.
/// </summary>
public abstract class SheetRequestReminderContract
{
    /// <summary>The line, as spec 031 FR-019 drafts it.</summary>
    const string Line = "Sheets: if the player wants to see one, run `dmc sheet` (the player characters), `dmc sheet --party` (the whole party) or `dmc sheet NAME` and give the path it prints; never type a sheet out.";

    /// <summary>The out-of-character line, which comes before it.</summary>
    const string Ooc = "Out of character: if this says a fact is wrong or asks how someone knew, read the files, open your reply by quoting what they say, for and against, say what you think, propose one fix, and change no file until the player agrees.";

    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>The hook's JSON for a prompt, with no folder or log, so no other part prints.</summary>
    static string Input(string prompt) => new JsonObject { ["prompt"] = prompt }.ToJsonString();

    /// <summary>A message that names a sheet, party or single, gets the line once (fork points A and B of the slip test among them).</summary>
    [TestMethod]
    public void AMessageThatNamesASheetGetsTheLine()
    {
        foreach (string prompt in new[] { "I need character sheets please", "can I see her full character sheet?", "Sheets?", "show me the party's SHEETS", "stat sheet for Pip" })
        {
            var result = ContextCheck(Input(prompt));
            Assert.AreEqual(0, result.ExitCode, prompt);
            CollectionAssert.AreEqual(new[] { Line }, result.StdoutLines, prompt);
        }
    }

    /// <summary>Any other message, a word that only holds "sheet" or a command included, gets nothing.</summary>
    [TestMethod]
    public void AnyOtherMessageGetsNothing()
    {
        foreach (string prompt in new[] { "I pull the bedsheet over the body", "/dmc-character-sheet-update", "I attack the goblin" })
            Assert.AreEqual("", ContextCheck(Input(prompt)).Stdout, prompt);
    }

    /// <summary>After the out-of-character line when both apply.</summary>
    [TestMethod]
    public void ItComesAfterTheOutOfCharacterLine() =>
        CollectionAssert.AreEqual(new[] { Ooc, Line }, ContextCheck(Input("ooc can I see my sheet")).StdoutLines);
}

/// <summary>The sheet-request line's tests against the library code, in this process.</summary>
[TestClass]
public sealed class SheetRequestReminderInProcessTests : SheetRequestReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> in this process.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The sheet-request line's tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class SheetRequestReminderBlackBoxTests : SheetRequestReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}
