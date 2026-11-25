using Dmc.Modules;
using Dmc.Retrieval;
using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Modules.ModuleFixtures;

namespace Dmc.Tests.Modules;

/// <summary>
/// The module check's rules (spec 012 FR-006 to FR-009, research R2 to R5, data-model.md § Module problem), against the
/// library code: what is a problem, what is not, and the lines' wording and order.
/// </summary>
[TestClass]
public sealed class ModuleCheckTests
{
    /// <summary>The test's temporary DMC folder.</summary>
    string _root = "";

    /// <summary>Makes an empty DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = NewFolder();

    /// <summary>Deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>The problems of the fixture module.</summary>
    List<string> Problems() => ModuleCheck.Problems(_root, Module);

    /// <summary>The problems of one file's text.</summary>
    /// <param name="text">The file's text.</param>
    static List<string> Of(string text) => ModuleCheck.FileProblems(MarkdownFile.FromText("f.md", text), "f.md");

    [TestMethod]
    public void ASoundModuleOfTheMeasuredShapeHasNoProblem()
    {
        WriteSoundModule(_root);
        CollectionAssert.AreEqual(Array.Empty<string>(), Problems());
    }

    [TestMethod]
    public void AFileAtTheMarkIsNamedWithItsSizeAndTheRepair()
    {
        WriteSoundModule(_root, monsters: 57);
        var problems = Problems();
        Assert.HasCount(1, problems);
        StringAssert.StartsWith(problems[0], $"modules/{Module}/monsters.md ");
        StringAssert.EndsWith(problems[0], $"at the Read cap mark ({Measurement.FormatTokens(Budgets.NearCapTokens)}): split it by heading into smaller files beside it");
    }

    [TestMethod]
    public void AFileOfManyShortLinesIsAtTheMarkToo()
    {
        var problems = Of(string.Concat(Enumerable.Repeat("x\n", Budgets.NearCapLines)));
        Assert.HasCount(1, problems);
        StringAssert.Contains(problems[0], "1,600 lines");
    }

    [TestMethod]
    public void ASectionOverTheLimitIsNamedWithItsLineAndSize()
    {
        var problems = Of($"# Ch\n\n## 1. The Long Hall (PDF 9)\n\n{Prose(TokenSizes.Chars(Budgets.ModuleSection) + 400)}");
        Assert.HasCount(1, problems);
        StringAssert.StartsWith(problems[0], "f.md:3 \"1. The Long Hall (PDF 9)\" ");
        StringAssert.EndsWith(problems[0], $"over the {Measurement.FormatTokens(Budgets.ModuleSection)} section limit: give each area or entry its own heading");
    }

    [TestMethod]
    public void ASectionJustUnderTheLimitPasses() =>
        CollectionAssert.AreEqual(Array.Empty<string>(), Of($"## A\n\n{Prose(TokenSizes.Chars(Budgets.ModuleSection) - 200)}"));

    [TestMethod]
    public void ASectionIsItsHeadingsOwnTextSoAParentWithSmallChildrenPasses()
    {
        var text = "## Hub (PDF 1)\n\nShort.\n\n" + string.Concat(Enumerable.Range(1, 6).Select(a => $"### {a}. Room (PDF {a})\n\n{Prose(6800, a)}\n"));
        CollectionAssert.AreEqual(Array.Empty<string>(), Of(text));
    }

    [TestMethod]
    public void AHeadingOverSeveralStatBlocksIsNamedWithTheCountAndNotAlsoForItsSize()
    {
        var text = "## Marsh Beasts (PDF 300)\n\n" + StatBlock("a", 1) + StatBlock("b", 2, edition2014: true) + StatBlock("c", 3);
        var problems = Of(text);
        Assert.HasCount(1, problems);
        Assert.AreEqual("f.md:1 \"Marsh Beasts (PDF 300)\" holds 3 stat blocks: give each its own heading", problems[0]);
    }

    [TestMethod]
    public void OneStatBlockAndProseAboutArmorAreNotSeveral()
    {
        var text = "## Beast\n\n" + StatBlock("a", 1) + "Its AC drops to 12 when wet.\nAC 10 if it is prone, and\nnothing else.\n";
        CollectionAssert.AreEqual(Array.Empty<string>(), Of(text));
    }

    [TestMethod]
    public void StatBlocksInsideAFenceDoNotCount() =>
        CollectionAssert.AreEqual(Array.Empty<string>(), Of("## Sketch\n\n```\nAC 12\nHP 30\nAC 13\nHP 31\n```\n"));

    [TestMethod]
    public void AHeadingRepeatedUnderOneParentIsNamedWithTheFirstLine()
    {
        var problems = Of("# Monsters\n\n## Eel (PDF 4)\n\nA.\n\n## Heron\n\nB.\n\n## Eel (PDF 9)\n\nC.\n");
        Assert.HasCount(1, problems);
        Assert.AreEqual("f.md:11 \"Eel (PDF 9)\" repeats line 3: make each heading unique", problems[0]);
    }

    [TestMethod]
    public void ASubheadingEachEntryRepeatsUnderItsOwnParentIsNotRepeated()
    {
        var text = "# Fates\n\n## The Tide\n\n### PLAYER-SAFE\n\nA.\n\n### SPOILER: touchpoints\n\nB.\n\n"
                   + "## The Gull\n\n### PLAYER-SAFE\n\nC.\n\n### SPOILER: touchpoints\n\nD.\n";
        CollectionAssert.AreEqual(Array.Empty<string>(), Of(text));
    }

    [TestMethod]
    public void AHeadingTwoFilesShareIsNotAProblem()
    {
        Write(_root, $"modules/{Module}/index.md", "# Index\n\n## Eel\n");
        Write(_root, $"modules/{Module}/monsters.md", "# Monsters\n\n## Eel\n\nA.\n");
        CollectionAssert.AreEqual(Array.Empty<string>(), Problems());
    }

    [TestMethod]
    public void HeadingsInFencesAndCommentsAreNotHeadings() =>
        CollectionAssert.AreEqual(Array.Empty<string>(), Of("## A\n\n```\n## A\n```\n\n<!--\n## A\n-->\n"));

    [TestMethod]
    public void SourceIsNeverReadAndAChapterNotYetWrittenIsNoProblem()
    {
        WriteSoundModule(_root, chapters: 3);
        Write(_root, $"modules/{Module}/index.md", "# Index\n\n- 1. chapters/01-chapter-1.md\n- 9. chapters/09-not-yet.md (not ingested)\n");
        CollectionAssert.AreEqual(Array.Empty<string>(), Problems());
    }

    [TestMethod]
    public void TheOldFormatFailsOnItsNpcHeadingAndTheReHeadedCopyPasses()
    {
        WriteOldFormatChapter(_root, reHeaded: false);
        var problems = Problems();
        Assert.HasCount(1, problems);
        StringAssert.Contains(problems[0], "\"NPCs in this chapter\" holds 2 stat blocks");
        WriteOldFormatChapter(_root, reHeaded: true);
        CollectionAssert.AreEqual(Array.Empty<string>(), Problems());
    }

    [TestMethod]
    public void ProblemsComeByFileThenByLineWithTheFileLineFirst()
    {
        Write(_root, $"modules/{Module}/b.md", "# B\n\n## X\n\n## X\n");
        Write(_root, $"modules/{Module}/a.md", "# A\n\n## Big\n\n" + Prose(Dmc.Tests.Support.TokenSizes.ReadChars(Budgets.NearCapTokens) + 100) + "## Y\n\n## Y\n");
        var problems = Problems();
        Assert.HasCount(4, problems);
        StringAssert.StartsWith(problems[0], $"modules/{Module}/a.md ");
        StringAssert.Contains(problems[1], "a.md:3 \"Big\"");
        StringAssert.Contains(problems[2], "\"Y\" repeats");
        StringAssert.StartsWith(problems[3], $"modules/{Module}/b.md:5 \"X\" repeats line 3");
    }

    [TestMethod]
    public void ANameResolvesPlainOrWithModulesAndOtherwiseFailsInOneLine()
    {
        WriteSoundModule(_root, chapters: 1);
        Assert.AreEqual(Module, ModuleCheck.Resolve(_root, Module));
        Assert.AreEqual(Module, ModuleCheck.Resolve(_root, $"modules/{Module}/"));
        Assert.AreEqual($"no module nope (modules: {Module})", Assert.Throws<DmcException>(() => ModuleCheck.Resolve(_root, "nope")).Message);
        Assert.AreEqual(".x is not a module (a folder starting with a dot is a staging folder)",
            Assert.Throws<DmcException>(() => ModuleCheck.Resolve(_root, ".x")).Message);
        Directory.CreateDirectory(Path.Combine(_root, "modules", "empty", "source"));
        Assert.AreEqual($"no module empty (modules: empty, {Module})", Assert.Throws<DmcException>(() => ModuleCheck.Resolve(_root, "empty")).Message);
    }
}
