using Dmc.Retrieval;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// The pieces both retrieval commands share: words and stems (<see cref="Words"/>, research R2), headings, fences,
/// sections and the heading key (<see cref="MarkdownHeadings"/>, R7), and the trim around a match (<see cref="Trim"/>, R4).
/// </summary>
[TestClass]
public sealed class WordsAndHeadingsTests
{
    [TestMethod]
    public void StemsFollowThePrototypeIncludingItsKnownGaps()
    {
        Assert.AreEqual("ruling", Words.Stem("rulings"));
        Assert.AreEqual("teach", Words.Stem("teaching"));
        Assert.AreEqual("oswin", Words.Stem("oswin's"));
        Assert.AreEqual("ruled", Words.Stem("ruled"), "a stem under four letters is left whole");
        Assert.AreEqual("nation", Words.Stem("nation"), "a stem under four letters is left whole");
        Assert.AreEqual("hors", Words.Stem("horses"));
        Assert.AreEqual("horse", Words.Stem("horse"), "the prototype's known gap: horse and horses stem apart");
    }

    [TestMethod]
    public void CommonAndOneLetterWordsAreDroppedAndAccentedLettersKept()
    {
        CollectionAssert.AreEqual(new[] { "ferryman", "rörn", "charg", "gp" }, Words.Stems("The ferryman of Rörn charges a 1 gp, said Oswin's... no").Take(4).ToArray());
        CollectionAssert.AreEqual(new[] { "oswin" }, Words.Stems("Oswin’s"), "the typographic apostrophe is folded");
        Assert.IsEmpty(Words.Stems("the and of what who"));
    }

    [TestMethod]
    public void HeadingsAreAtxLinesOutsideFences()
    {
        var file = MarkdownFile.FromText("x.md",
            "# Title\n#hashtag\n    # four spaces\n   ### three spaces\n```\n## in a fence\n```\n~~~~\n# in a tilde fence\n~~~\n~~~~\n###### six\n####### seven\n## Closed ##\n");
        CollectionAssert.AreEqual(
            new[] { "1:Title:1", "3:three spaces:4", "6:six:12", "2:Closed:14" },
            file.Headings.Select(h => $"{h.Level}:{h.Text}:{h.Line}").ToArray());
        Assert.IsTrue(file.Fenced[5], "a line inside a backtick fence");
        Assert.IsTrue(file.Fenced[9], "three tildes do not close a fence opened with four");
        Assert.IsFalse(file.Fenced[11]);
    }

    [TestMethod]
    public void ASectionEndsBeforeTheNextHeadingOfTheSameOrAHigherLevel()
    {
        var file = MarkdownFile.FromText("x.md", "# A\n## B\ntext\n### C\nmore\n## D\nlast\n");
        Assert.AreEqual(5, MarkdownHeadings.SectionEnd(file.Headings, 1, file.Lines.Length), "## B runs over ### C to the line before ## D");
        Assert.AreEqual(7, MarkdownHeadings.SectionEnd(file.Headings, 3, file.Lines.Length), "the last section runs to the end");
        Assert.AreEqual(7, MarkdownHeadings.SectionEnd(file.Headings, 0, file.Lines.Length));
    }

    [TestMethod]
    public void CrlfLinesAndAByteOrderMarkAreRemoved()
    {
        var file = MarkdownFile.FromText("x.md", "﻿# A\r\nline\r\n");
        CollectionAssert.AreEqual(new[] { "# A", "line" }, file.Lines);
    }

    [TestMethod]
    public void TheKeyIgnoresCaseEmphasisPunctuationLinksAndAPageReference()
    {
        Assert.AreEqual("goblin boss", MarkdownHeadings.Key("GOBLIN BOSS (PDF 212)"));
        Assert.AreEqual(MarkdownHeadings.Key("goblin boss"), MarkdownHeadings.Key("GOBLIN BOSS (PDF 212)"));
        Assert.AreEqual("prices debts promises", MarkdownHeadings.Key("**Prices**, debts & promises"));
        Assert.AreEqual("area 1 the hut", MarkdownHeadings.Key("Area 1: [The hut](maps/hut.md) (p. 4)"));
        Assert.AreEqual("the raft pp 5", MarkdownHeadings.Key("The raft (pp 5)"), "only p., pp., page or PDF start a page reference");
    }

    [TestMethod]
    public void AShortLineIsShownWhole()
    {
        Assert.AreEqual("short", Trim.Around("short", new HashSet<string> { "short" }));
    }

    [TestMethod]
    public void ALongLineIsCutToAWindowAroundTheMatch()
    {
        string text = string.Join(' ', Enumerable.Repeat("filler", 300)) + " the ferryman Oswin charges a cart toll " + string.Join(' ', Enumerable.Repeat("filler", 300));
        string shown = Trim.Around(text, Words.Stems("Oswin cart").ToHashSet());

        StringAssert.StartsWith(shown, Trim.Cut + "Oswin");
        StringAssert.EndsWith(shown, Trim.Cut);
        StringAssert.Contains(shown, "cart toll");
        Assert.IsLessThanOrEqualTo(Trim.Width + 2 * Trim.Cut.Length, shown.Length);
        Assert.IsFalse(shown.TrimEnd('…').EndsWith("fill", StringComparison.Ordinal), "the cut falls at a word boundary");
    }

    [TestMethod]
    public void AMatchNearTheEndSlidesTheWindowBack()
    {
        string text = string.Join(' ', Enumerable.Repeat("filler", 300)) + " Oswin";
        string shown = Trim.Around(text, Words.Stems("Oswin").ToHashSet());
        StringAssert.EndsWith(shown, "Oswin");
        StringAssert.StartsWith(shown, Trim.Cut);
        Assert.IsGreaterThan(Trim.Width - 10, shown.Length, "a window near the end is still full");
    }
}
