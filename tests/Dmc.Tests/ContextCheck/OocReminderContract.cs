using System.Text.Json.Nodes;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// The out-of-character line <c>dmc context-check</c> prints last (spec 016 FR-011, contracts/context-check-sheets.md), in
/// this process and through the published binary: one line for a message that starts with <c>ooc</c> or <c>((</c>, and
/// nothing for any other.
/// </summary>
public abstract class OocReminderContract
{
    /// <summary>The line, as the contract drafts it.</summary>
    const string Line = "Out of character: if this says a fact is wrong or asks how someone knew, read the files, open your reply by quoting what they say, for and against, say what you think, propose one fix, and change no file until the player agrees.";

    /// <summary>Run <c>dmc context-check</c> with this on stdin.</summary>
    protected abstract ProcessResult ContextCheck(string stdin);

    /// <summary>The hook's JSON for a prompt, with no folder or log, so no other part prints.</summary>
    static string Input(string prompt) => new JsonObject { ["prompt"] = prompt }.ToJsonString();

    [TestMethod]
    public void AMessageThatStartsOutOfCharacterGetsTheLine()
    {
        foreach (string prompt in new[] { "ooc that's wrong, she can't fly", "OOC: how did she know?", "ooc", "  ((she can't fly))", "\tOoc\nwhy?" })
        {
            var result = ContextCheck(Input(prompt));
            Assert.AreEqual(0, result.ExitCode, prompt);
            CollectionAssert.AreEqual(new[] { Line }, result.StdoutLines, prompt);
        }
    }

    [TestMethod]
    public void AnyOtherMessageGetsNothing()
    {
        foreach (string prompt in new[] { "How would she know that?", "oocyte", "this is ooc but later", "(one bracket)", "/dmc-help" })
            Assert.AreEqual("", ContextCheck(Input(prompt)).Stdout, prompt);
    }
}

/// <summary>The out-of-character line's tests against the library code, in this process.</summary>
[TestClass]
public sealed class OocReminderInProcessTests : OocReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> in this process.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.InProcess(["context-check"], stdin);
}

/// <summary>The out-of-character line's tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class OocReminderBlackBoxTests : OocReminderContract
{
    /// <summary>Runs <c>dmc context-check</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult ContextCheck(string stdin) => DmcRunner.BlackBox(["context-check"], stdin);
}
