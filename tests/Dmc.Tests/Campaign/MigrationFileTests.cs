using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>The hand-written reader of a migration's front matter and headings (contracts/migration-file.md), as unit tests.</summary>
[TestClass]
public sealed class MigrationFileTests
{
    /// <summary>A well-formed migration 4 with drops, whose front matter a test changes one line of.</summary>
    static string Text(string? replace = null, string? with = null)
    {
        string text = MigrationRoot.MigrationText(4, drops: ["mix-tracker", "overtaken-plan"], checks: ["one-last-played-line"]);
        return replace is null ? text : text.Replace(replace, with);
    }

    /// <summary>The message of the error a text makes.</summary>
    static string ErrorOf(string text, string name = "004-lean.md") =>
        Assert.ThrowsExactly<DmcException>(() => MigrationFile.Parse(name, text)).Message;

    [TestMethod]
    public void AWellFormedFileIsRead()
    {
        var migration = MigrationFile.Parse("004-lean.md", Text("touches: [story/dm-notes.md]", "touches: [story/dm-notes.md, game-state.md]"));
        Assert.AreEqual(4, migration.Standard);
        Assert.AreEqual("Test migration 4", migration.Title);
        Assert.AreEqual("PR #1", migration.Shipped);
        CollectionAssert.AreEqual(new[] { "story/dm-notes.md", "game-state.md" }, migration.Touches.ToArray());
        CollectionAssert.AreEqual(new[] { "mix-tracker", "overtaken-plan" }, migration.Drops.ToArray());
        CollectionAssert.AreEqual(new[] { "one-last-played-line" }, migration.Checks.ToArray());
        CollectionAssert.AreEqual(MigrationFile.RequiredHeadings, migration.Headings.ToArray());
    }

    [TestMethod]
    public void EmptyListsAndCrlfAreRead()
    {
        var migration = MigrationFile.Parse("004-lean.md", MigrationRoot.MigrationText(4).Replace("\n", "\r\n"));
        Assert.IsEmpty(migration.Drops);
        Assert.IsEmpty(migration.Touches.Where(t => t.Length == 0));
    }

    [TestMethod]
    public void AMissingDropsKeyIsAnEmptyList()
    {
        var migration = MigrationFile.Parse("004-lean.md", MigrationRoot.MigrationText(4).Replace("drops: []\n", ""));
        Assert.IsEmpty(migration.Drops);
    }

    [TestMethod]
    [DataRow("size: [game-state.md]", "line 8: unknown key size")]
    [DataRow("sizes: 3", "line 8: unknown key sizes")]
    [DataRow("checks: [one-last-played-line]", "line 8: duplicate key checks")]
    [DataRow("no colon here", "line 8: expected key: value")]
    public void ABadExtraLineIsAnErrorNamingTheLine(string line, string message)
    {
        string text = Text("checks: [one-last-played-line]\n", "checks: [one-last-played-line]\n" + line + "\n");
        Assert.AreEqual("migrations/004-lean.md " + message, ErrorOf(text));
    }

    [TestMethod]
    public void AStandardThatDiffersFromTheNameIsAnError() =>
        Assert.AreEqual("migrations/004-lean.md line 2: standard 5 differs from the name's number 4", ErrorOf(Text("standard: 4", "standard: 5")));

    [TestMethod]
    [DataRow("Mix_Tracker")]
    [DataRow("mix tracker")]
    [DataRow("-mix")]
    public void AClassThatIsNotLowercaseHyphenWordsIsAnError(string cls) =>
        StringAssert.Contains(ErrorOf(Text("mix-tracker,", cls + ",")), $"line 6: {cls} in drops is not lowercase words joined by hyphens");

    [TestMethod]
    public void AMissingClosingFenceIsAnError() =>
        Assert.AreEqual("migrations/004-lean.md: the front matter has no closing ---",
            ErrorOf("---\nstandard: 4\ntitle: x\n\n## What changes\n"));

    [TestMethod]
    public void ANameNotOfTheFormIsAnError() =>
        StringAssert.Contains(ErrorOf(Text(), "4-lean.md"), "the name must be NNN-slug.md");

    [TestMethod]
    public void EmptyChecksAreAnError() =>
        StringAssert.Contains(ErrorOf(Text("checks: [one-last-played-line]", "checks: []")), "checks must name at least one check");

    [TestMethod]
    public void AMissingHeadingOrOneOutOfOrderIsAnError()
    {
        StringAssert.Contains(ErrorOf(Text("## Judgments", "## Thoughts")), "the body has no ## Judgments");
        string swapped = Text("## Steps", "## TEMP").Replace("## Judgments", "## Steps").Replace("## TEMP", "## Judgments");
        StringAssert.Contains(ErrorOf(swapped), "is out of order");
    }

    [TestMethod]
    public void ADropWithoutADeclaredLineIsAnError() =>
        StringAssert.Contains(ErrorOf(Text("- `overtaken-plan`: gone.\n", "")), "drop class overtaken-plan has no line under ## Declared drops");
}
