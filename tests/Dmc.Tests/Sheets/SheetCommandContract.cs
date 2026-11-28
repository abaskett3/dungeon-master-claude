using Dmc.Commands;
using Dmc.Tests.Support;
using static Dmc.Tests.Sheets.SheetFixtures;

namespace Dmc.Tests.Sheets;

/// <summary>
/// What <c>dmc sheet</c> prints, writes and returns (<c>specs/031-character-sheet-export/contracts/dmc-sheet.md</c>),
/// checked twice: in this process and through the published binary. Each test gets the fixture campaign in a fresh DMC
/// folder, named with <c>--folder</c>, and checks that <c>adventures/</c> is left exactly as it was.
/// </summary>
public abstract class SheetCommandContract
{
    /// <summary>The test's temporary DMC folder.</summary>
    protected string Root = "";

    /// <summary>A hash of <c>adventures/</c> before the run.</summary>
    string _before = "";

    /// <summary>Run <c>dmc sheet</c> with these arguments, the DMC folder named with <c>--folder</c>.</summary>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    protected abstract ProcessResult Sheet(params string[] args);

    /// <summary>Makes the DMC folder with the fixture campaign.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        Root = NewDmcFolder();
        _before = Hash(Path.Combine(Root, "adventures"));
    }

    /// <summary>Checks <c>adventures/</c> is unchanged, then deletes the folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        try
        {
            Assert.AreEqual(_before, Hash(Path.Combine(Root, "adventures")), "dmc sheet must never write under adventures/");
        }
        finally
        {
            DmcFolders.Delete(Root);
        }
    }

    /// <summary>Changes the fixture before the run, then takes the hash the cleanup compares against.</summary>
    /// <param name="change">What to change.</param>
    protected void Arrange(Action change)
    {
        change();
        _before = Hash(Path.Combine(Root, "adventures"));
    }

    [TestMethod]
    public void WithNoNameEveryPlayerCharacterIsWrittenToOnePageAndOneLineIsPrinted()
    {
        var result = Sheet();

        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stderr);
        CollectionAssert.AreEqual(new[] { "wrote exports/sheets/vale/pc-ana.html: 1 sheet (Ana Reed)" }, result.StdoutLines);
        string html = File.ReadAllText(Page(Root, "pc-ana.html"));
        StringAssert.StartsWith(html, "<!doctype html>");
        Assert.Contains("<h1>Ana Reed</h1>", html);
        Assert.Contains("As of 3 Harvestmoon, late afternoon in the story", html, "the as-of line is the Scene's in-world date");
        Assert.DoesNotContain("\r", result.Stdout, "output is LF on every system");
    }

    [TestMethod]
    public void ThePartyIsEveryBlockUnderPartyStatusAndABlockWithNoSheetIsNamedAsSkipped()
    {
        var result = Sheet("--party");

        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "wrote exports/sheets/vale/party.html: 3 sheets (Ana Reed, Bram, Cora); skipped Shared supplies (no sheet)",
        }, result.StdoutLines);
        string html = File.ReadAllText(Page(Root, "party.html"));
        Assert.AreEqual(3, html.Split("<section class=\"sheet\">").Length - 1, "one sheet each");
    }

    /// <summary>FR-014: nothing the allow-list leaves out reaches the page: Secrets, What they know, a companion's motives, Role in the story, History.</summary>
    [TestMethod]
    public void NoFieldOutsideTheAllowListIsPrinted()
    {
        Assert.AreEqual(0, Sheet("--party", "--history").ExitCode);
        string html = File.ReadAllText(Page(Root, "party.html"));
        Assert.DoesNotContain(Spoiler, html);
        Assert.Contains("met Bram at the ferry", html, "--history prints the PC's history");
        Assert.Contains("left the barges", html, "and the history file the full save moved it to");
    }

    /// <summary>Spec 032 FR-009: the limits on each character's record print in their box; its rows (who calls them what) never do; a character with no record prints none.</summary>
    [TestMethod]
    public void TheRecordsLimitsArePrintedAndItsRowsAreNot()
    {
        Assert.AreEqual(0, Sheet("--party").ExitCode);
        string html = File.ReadAllText(Page(Root, "party.html"));
        Assert.Contains("<li>Reads no <em>Elvish</em> script</li><li>Never rides a horse</li>", html, "Ana's limits, in the record's order");
        Assert.Contains("<li>Cannot swim</li>", html, "Bram's");
        Assert.DoesNotContain("SPOILER-TRUE-NAME", html);
        Assert.DoesNotContain("SPOILER-BRAMS-WORD", html);
        string[] sheets = html.Split("<section class=\"sheet\">");
        Assert.Contains("<h3>Limits (from their record)</h3><div class=\"v\"><p class=\"empty\">none</p>", sheets[3], "Cora has no record");
    }

    [TestMethod]
    public void HistoryIsLeftOutUnlessAskedFor()
    {
        Assert.AreEqual(0, Sheet("Ana Reed").ExitCode);
        string html = File.ReadAllText(Page(Root, "pc-ana.html"));
        Assert.DoesNotContain("met Bram at the ferry", html);
        Assert.DoesNotContain("<h2>History</h2>", html);
    }

    [TestMethod]
    public void ANameMatchesATitleOrAFileNameExactly()
    {
        Assert.AreEqual(0, Sheet("pc-ana").ExitCode);
        Assert.AreEqual(0, Sheet("npc-bram.md").ExitCode);
        Assert.IsTrue(File.Exists(Page(Root, "npc-bram.html")));
        var loose = Sheet("Ana");
        Assert.AreEqual(1, loose.ExitCode, "a first name alone is not the title");
        Assert.AreEqual("dmc sheet: no sheet named Ana in vale/character-sheets/ (give its title or its file name)", loose.Stderr.TrimEnd());
    }

    /// <summary>Code review of PR #288: a party block that names two sheets does not stop the export of a PC by name.</summary>
    [TestMethod]
    public void AnAmbiguousPartyBlockDoesNotStopAPcByName()
    {
        Arrange(() => File.WriteAllText(PathOf(Root, "character-sheets/npc-bram2.md"), "# Bram\n"));
        var result = Sheet("Ana Reed");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
    }

    /// <summary>Code review of PR #288: one character whose sheet is called party.md never replaces the party's page.</summary>
    [TestMethod]
    public void OneCharacterNeverWritesThePartyPage()
    {
        Arrange(() => File.Move(PathOf(Root, "character-sheets/npc-bram.md"), PathOf(Root, "character-sheets/party.md")));
        var result = Sheet("Bram");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("wrote exports/sheets/vale/party-sheet.html: 1 sheet (Bram)", result.StdoutLines.Last());
    }

    [TestMethod]
    public void SeveralNamesWriteOnePageOfThemAll()
    {
        var result = Sheet("Bram", "Cora");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "wrote exports/sheets/vale/sheets.html: 2 sheets (Bram, Cora)" }, result.StdoutLines);
    }

    /// <summary>An NPC outside the party is refused in one line: their sheet is a spoiler.</summary>
    [TestMethod]
    public void AnNpcOutsideThePartyIsRefused()
    {
        var result = Sheet("Hob the miller");
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("dmc sheet: Hob the miller is not a player character or in the party: an NPC's sheet holds spoilers, so it is not exported",
            result.Stderr.TrimEnd());
        Assert.IsFalse(Directory.Exists(Path.Combine(Root, "exports")));
    }

    [TestMethod]
    public void TheCheckPrintsNothingForFilesInShapeAndWritesNothing()
    {
        var result = Sheet("--check", "--party");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout + result.Stderr);
        Assert.IsFalse(Directory.Exists(Path.Combine(Root, "exports")));
    }

    /// <summary>
    /// A file out of shape is refused: the findings on standard output, one line each, then one line on standard error,
    /// exit 1, and no page. <c>--check</c> prints the same findings and exits 0.
    /// </summary>
    [TestMethod]
    public void AFileOutOfShapeIsRefusedWithOneLineForEachThingToFix()
    {
        Arrange(() =>
        {
            Edit(Root, "character-sheets/pc-ana.md", "- **Speed**: 30 ft\n", "- **Speed**:\n- **Swim speed**: 15 ft\n");
            Edit(Root, "character-sheets/pc-ana.md", "## Wardrobe and keepsakes", "## Notes for later\n\n- buy rope\n\n## Wardrobe and keepsakes");
        });
        string[] expected =
        [
            "pc-ana.md: ## Combat: \"- **Swim speed**: 15 ft\" has no label of templates/pc-sheet.md: move it to the field it belongs to",
            "pc-ana.md: ## Notes for later is not a section of templates/pc-sheet.md: move its lines into the template's sections",
            "pc-ana.md: Speed is blank: write its value, or none",
        ];

        var refused = Sheet();
        Assert.AreEqual(1, refused.ExitCode);
        CollectionAssert.AreEqual(expected, refused.StdoutLines);
        Assert.AreEqual("dmc sheet: refused: 3 problems in the files: fix each one and run it again", refused.Stderr.TrimEnd());
        Assert.IsFalse(Directory.Exists(Path.Combine(Root, "exports")), "a refused export writes nothing");

        var check = Sheet("--check");
        Assert.AreEqual(0, check.ExitCode);
        CollectionAssert.AreEqual(expected, check.StdoutLines);
        Assert.AreEqual("", check.Stderr);
    }

    /// <summary>A PC sheet from before spec 031 has no Other skills line, which the check names (the eighteen skills, FR-007).</summary>
    [TestMethod]
    public void AnOlderSheetWithNoOtherSkillsLineIsNamed()
    {
        Arrange(() => Edit(Root, "character-sheets/pc-ana.md",
            "- **Other skills** (every other skill, with total bonus): Acrobatics +2, Animal Handling +1, Arcana +0, Deception -1, History +0, Insight +1, Intimidation -1, Investigation +0, Medicine +1, Nature +0, Performance -1, Persuasion -1, Religion +0, Sleight of Hand +2, Stealth +2 (disadvantage in chain mail)\n", ""));
        var result = Sheet("--check");
        CollectionAssert.AreEqual(new[]
        {
            "pc-ana.md: no Other skills line in ## Abilities: write the skills it is not proficient in, each with its bonus (templates/pc-sheet.md)",
        }, result.StdoutLines);
    }

    [TestMethod]
    public void ACompanionWithNoPlayCardIsRefused()
    {
        Arrange(() => Edit(Root, "character-sheets/npc-bram.md", "## Play card", "## Card"));
        var result = Sheet("--check", "Bram");
        Assert.Contains("npc-bram.md: a companion's sheet needs its ## Play card (see the dmc-character-sheet-update skill)", result.StdoutLines);
    }

    /// <summary>FR-012: a written value that the arithmetic does not give is a warning; the export is still written, with the written value.</summary>
    [TestMethod]
    public void AnArithmeticDifferenceWarnsAndTheWrittenValueIsPrinted()
    {
        Arrange(() => Edit(Root, "character-sheets/pc-ana.md", "Athletics +5", "Athletics +6"));
        var result = Sheet();

        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[]
        {
            "warning: pc-ana.md: Athletics is +6 on the sheet; STR +3 and proficiency +2 make +5 (the sheet's value is printed)",
            "wrote exports/sheets/vale/pc-ana.html: 1 sheet (Ana Reed)",
        }, result.StdoutLines);
        Assert.Contains("<td>+6</td>", File.ReadAllText(Page(Root, "pc-ana.html")));
    }

    /// <summary>FR-013: a Party status line off its template warns and is not printed.</summary>
    [TestMethod]
    public void APartyStatusLineOffTheTemplateWarnsAndIsNotPrinted()
    {
        Arrange(() => Edit(Root, "game-state.md", "- **Inspiration**: yes\n", "- **Inspiration**: yes\n- **Wearing**: oilskin coat\n"));
        var result = Sheet();
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("warning: game-state.md: Ana Reed's block: \"- **Wearing**: oilskin coat\" is not a field of the template and is not printed", result.StdoutLines[0]);
        Assert.DoesNotContain("Wearing", File.ReadAllText(Page(Root, "pc-ana.html")));
    }

    [TestMethod]
    public void ANewExportReplacesTheOldPage()
    {
        Assert.AreEqual(0, Sheet().ExitCode);
        Arrange(() => Edit(Root, "character-sheets/pc-ana.md", "blunt and loyal", "blunt and kind"));
        Assert.AreEqual(0, Sheet().ExitCode);
        string html = File.ReadAllText(Page(Root, "pc-ana.html"));
        Assert.Contains("blunt and kind", html);
        CollectionAssert.AreEqual(new[] { "pc-ana.html" },
            Directory.GetFiles(Path.Combine(Root, "exports", "sheets", Name)).Select(Path.GetFileName).ToArray(), "nothing accumulates");
    }

    [TestMethod]
    public void TextFromTheFilesIsEscaped()
    {
        Assert.AreEqual(0, Sheet("--history").ExitCode);
        string html = File.ReadAllText(Page(Root, "pc-ana.html"));
        Assert.Contains("paid the miller's debt &lt;for&gt; the bridge &amp; more", html.Replace("&#x27;", "'"));
        Assert.DoesNotContain("<for>", html);
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Sheet("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.StartsWith(result.Stdout, "usage: dmc sheet [NAME ...] [--campaign NAME] [--party] [--history] [--open] [--folder PATH]");
    }

    [TestMethod]
    public void TheCommandIsListedInTheGeneralHelp()
    {
        var result = DmcRunner.InProcess(["--help"]);
        Assert.IsTrue(result.StdoutLines.Any(l => l.TrimStart().StartsWith("sheet ", StringComparison.Ordinal)
                                                  && l.Contains("a printable character sheet")));
    }
}

/// <summary>The sheet tests against the library code, in this process, with a fixed date and a recorder for <c>--open</c>.</summary>
[TestClass]
public sealed class SheetInProcessTests : SheetCommandContract
{
    /// <summary>The addresses <c>--open</c> asked to open.</summary>
    readonly List<string> _opened = [];

    /// <summary>Runs <c>dmc sheet</c> in this process.</summary>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    protected override ProcessResult Sheet(params string[] args) =>
        DmcRunner.InProcess(["sheet", "--folder", Root, .. args], commands: [new SheetCommand(() => Today, _opened.Add)]);

    [TestMethod]
    public void OpenOpensTheWrittenPage()
    {
        Assert.AreEqual(0, Sheet("--open").ExitCode);
        CollectionAssert.AreEqual(new[] { new Uri(Page(Root, "pc-ana.html")).AbsoluteUri }, _opened);
    }

    [TestMethod]
    public void ARefusedExportOpensNothing()
    {
        Arrange(() => Edit(Root, "character-sheets/pc-ana.md", "- **Speed**: 30 ft", "- **Speed**:"));
        Assert.AreEqual(1, Sheet("--open").ExitCode);
        Assert.IsEmpty(_opened);
    }

    /// <summary>The party page on the fixture is byte for byte the golden page: the layout's boxes, their order and their contents (FR-008). Set DMC_UPDATE_GOLDEN=1 to rewrite it after a deliberate change, and review the diff.</summary>
    [TestMethod]
    public void ThePartyPageIsTheGoldenPage()
    {
        Assert.AreEqual(0, Sheet("--party").ExitCode);
        string html = File.ReadAllText(Page(Root, "party.html"));
        if (Environment.GetEnvironmentVariable("DMC_UPDATE_GOLDEN") == "1")
            File.WriteAllText(GoldenParty, html);
        Assert.AreEqual(File.ReadAllText(GoldenParty).Replace("\r\n", "\n"), html);
    }
}

/// <summary>The sheet tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class SheetBlackBoxTests : SheetCommandContract
{
    /// <summary>Runs <c>dmc sheet</c> as the published binary.</summary>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    protected override ProcessResult Sheet(params string[] args) => DmcRunner.BlackBox(["sheet", "--folder", Root, .. args]);
}
