using Dmc.Retrieval;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// The code under <c>dmc lookup</c> (<see cref="Lookup"/>, <see cref="LookupSource"/>), called in this process on the
/// fixture chain: matching, sections, lists, the too-large line, sources and their files, and both kinds of rules
/// reference (research R7 to R9).
/// </summary>
[TestClass]
public sealed class LookupLibraryTests
{
    /// <summary>The test's DMC folder, with the fixture chain.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = NewChain();

    /// <summary>Deletes it.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>The message of the one-line failure a lookup throws.</summary>
    /// <param name="source">The source argument.</param>
    /// <param name="heading">The heading.</param>
    string Failure(string source, string heading) =>
        Assert.ThrowsExactly<DmcException>(() => Lookup.Run(_root, source, heading)).Message;

    [TestMethod]
    public void AStatBlockIsFoundByItsNameAndReturnedExactly()
    {
        var lines = Lookup.Run(_root, Module, "goblin boss");

        Assert.AreEqual($"modules/{Module}/monsters.md:3-9", lines[0]);
        CollectionAssert.AreEqual(
            new[] { "## GOBLIN BOSS (PDF 12)", "", "*Small Humanoid (Goblinoid), Neutral Evil*", "", "- **AC** 17", "- **HP** 21 (6d6)", "" },
            lines.Skip(1).ToArray());
    }

    [TestMethod]
    public void ASectionIncludesItsSubSections()
    {
        var lines = Lookup.Run(_root, Module, "area 1 the ferry hut");
        StringAssert.StartsWith(lines[0], $"modules/{Module}/chapters/01-the-ford.md:3-");
        Assert.IsTrue(lines.Contains("### Treasure"), "the ### under the ## is part of it");
        Assert.IsFalse(lines.Any(l => l.StartsWith("## Area 2", StringComparison.Ordinal)), "the next ## is not");
    }

    [TestMethod]
    public void EveryWordOfTheRequestFindsALongerHeading()
    {
        var lines = Lookup.Run(_root, Sequel, "prices");
        Assert.AreEqual("## Prices, debts and promises", lines[1]);
        StringAssert.StartsWith(lines[0], $"adventures/{Sequel}/story/canon.md:");
    }

    [TestMethod]
    public void AHeadingInSeveralFilesListsThemAndPrintsNoSection()
    {
        var lines = Lookup.Run(_root, Sequel, "playing them");
        CollectionAssert.AreEqual(new[]
        {
            $"adventures/{Sequel}/character-sheets/npc-hedda.md:3: ## Playing them",
            $"adventures/{Sequel}/character-sheets/npc-oswin.md:3: ## Playing them",
        }, lines);
    }

    [TestMethod]
    public void ThePathFormNarrowsTheSearchToOneFile()
    {
        var lines = Lookup.Run(_root, $"{Sequel}/character-sheets/npc-oswin.md", "playing them");
        Assert.AreEqual($"adventures/{Sequel}/character-sheets/npc-oswin.md:3-7", lines[0]);
        StringAssert.Contains(lines[3], "**Voice**");
    }

    [TestMethod]
    public void MoreThanEightMatchesAreCounted()
    {
        for (int i = 0; i < 10; i++)
            Write(_root, $"adventures/{Sequel}/character-sheets/npc-extra{i}.md", "# Extra\n\n## Playing them\n\n- **Voice**: plain\n");
        var lines = Lookup.Run(_root, Sequel, "playing them");
        Assert.HasCount(9, lines);
        Assert.AreEqual("(+4 more)", lines[^1]);
    }

    [TestMethod]
    public void NoMatchFailsNamingTheNearestHeadings()
    {
        string message = Failure(Module, "goblin chief");
        StringAssert.StartsWith(message, $"no heading \"goblin chief\" in {Module} (nearest: GOBLIN BOSS (PDF 12)");
    }

    [TestMethod]
    public void NoMatchAndNothingNearFailsWithoutAList()
    {
        Assert.AreEqual($"no heading \"zebra\" in {Module}", Failure(Module, "zebra"));
    }

    [TestMethod]
    public void ASectionAtTheNearCapMarkGivesItsLineRangeInstead()
    {
        string body = string.Concat(Enumerable.Repeat("- a line of filler text that is long enough to count\n", 1700));
        Write(_root, $"modules/{Module}/chapters/02-the-hub.md", "# Chapter 2\n\n## The Hub\n\n" + body);
        var lines = Lookup.Run(_root, Module, "the hub");
        Assert.HasCount(1, lines);
        StringAssert.Matches(lines[0], new System.Text.RegularExpressions.Regex(@"^modules/fen-crossing/chapters/02-the-hub\.md:3-1704: section ## The Hub is [\d.]+k tokens, 1,702 lines: read it in parts$"));
    }

    [TestMethod]
    public void ACampaignSourceSkipsImportsExportsBackupsAndDotFolders()
    {
        AddNoise(_root);
        Write(_root, $"adventures/{Sequel}/import/transcript.md", "# Transcript\n\n## Prices, debts and promises\n\n- NOISE\n");
        Write(_root, $"adventures/{Sequel}/.staging/canon.md", "# Staging\n\n## Prices, debts and promises\n\n- NOISE\n");
        var lines = Lookup.Run(_root, Sequel, "prices debts and promises");
        StringAssert.StartsWith(lines[0], $"adventures/{Sequel}/story/canon.md:");
        Assert.IsFalse(lines.Any(l => l.Contains("NOISE")));
    }

    [TestMethod]
    public void AModuleSourceNeverReadsItsSourceFolder()
    {
        Write(_root, $"modules/{Module}/source/extra.md", "# Extra\n\n## Hidden heading\n\ntext\n");
        StringAssert.StartsWith(Failure(Module, "hidden heading"), "no heading");
    }

    [TestMethod]
    public void MarkdownRulesAreSearchedLikeAnySource()
    {
        MarkdownRules(_root);
        var lines = Lookup.Run(_root, "rules", "grappled");
        CollectionAssert.AreEqual(new[] { "rules/conditions.md:3-6", "## Grappled", "", "A grappled creature's speed is 0.", "" }, lines);
    }

    [TestMethod]
    public void PdfRulesGiveTheIndexLinesThatHoldEveryWord()
    {
        PdfRules(_root);
        var lines = Lookup.Run(_root, "rules", "combat movement");
        CollectionAssert.AreEqual(new[] { "rules/index.md:9: | Combat: order, movement and position, attacks, mounted, underwater | 13–16 |" }, lines);
    }

    [TestMethod]
    public void PdfRulesFallBackToThePageNumbersAndNeverToPageText()
    {
        PdfRules(_root);
        var lines = Lookup.Run(_root, "rules", "Fireball");
        CollectionAssert.AreEqual(new[] { "rules: \"Fireball\" is on PDF pages 129, 130 (read them in rules/srd.pdf)" }, lines);
    }

    [TestMethod]
    public void PdfRulesThatHoldNothingFail()
    {
        PdfRules(_root);
        Assert.AreEqual("rules: no \"wish\" in rules/index.md or its pages", Failure("rules", "wish"));
    }

    [TestMethod]
    public void CrlfFilesComeOutWithLfAndOtherwiseUnchanged()
    {
        Write(_root, $"modules/{Module}/items.md", "# Items\r\n\r\n## Rope of Climbing (PDF 20)\r\n\r\n60 feet,  silk\t; knots.\r\n");
        var lines = Lookup.Run(_root, Module, "rope of climbing");
        CollectionAssert.AreEqual(new[] { $"modules/{Module}/items.md:3-5", "## Rope of Climbing (PDF 20)", "", "60 feet,  silk\t; knots." }, lines);
    }

    [TestMethod]
    public void ANameThatIsBothACampaignAndAModuleIsRefused()
    {
        Write(_root, $"modules/{Sequel}/index.md", "# Index\n");
        Assert.AreEqual($"{Sequel} is both a campaign and a module: say adventures/{Sequel} or modules/{Sequel}", Failure(Sequel, "prices"));
        StringAssert.StartsWith(Lookup.Run(_root, $"adventures/{Sequel}", "prices")[0], $"adventures/{Sequel}/story/canon.md:");
    }

    [TestMethod]
    public void UnknownSourcesStagingFoldersAndMissingFilesFail()
    {
        Assert.AreEqual($"no source nowhere: not a campaign, a module or rules (campaigns: {Sequel}, {Predecessor}; modules: {Module})",
            Failure("nowhere", "prices"));
        Assert.AreEqual(".staging is not a source (a folder starting with a dot is a staging folder)", Failure(".staging", "prices"));
        Assert.AreEqual($"no story/nothing.md in {Sequel}", Failure($"{Sequel}/story/nothing.md", "prices"));
        Assert.AreEqual("rules: no rules reference in rules/ (set one up with /dmc-setup-rules)", Failure("rules", "grappled"));
    }
}
