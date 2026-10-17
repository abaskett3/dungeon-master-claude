using Dmc.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using static Dmc.Tests.Pdf.PdfFixtures;

namespace Dmc.Tests.Pdf;

// The ParsePages class of tests/test_pdf_extract.py, ported one for one (test_default_is_every_page is
// DefaultIsEveryPage, and so on). In Python they call the module's function directly, so here they call the library
// code of the CLI project in this process. "SystemExit" in the Python is a DmcException here. The CommandLine class is
// in PdfCommandContract.cs.

/// <summary>The <c>--pages</c> list (<see cref="PageRanges"/>), in this process: the default, ranges, spacing, sorting, and every error.</summary>
[TestClass]
public sealed class ParsePagesTests
{
    [TestMethod]
    public void DefaultIsEveryPage()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, PageRanges.Parse(null, 3));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, PageRanges.Parse("", 3));
    }

    [TestMethod]
    public void RangesAndSinglePages()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 5 }, PageRanges.Parse("1-3,5", 10));
        CollectionAssert.AreEqual(new[] { 7 }, PageRanges.Parse("7", 10));
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 9 }, PageRanges.Parse(" 2 - 4 , 9 ", 10));
        CollectionAssert.AreEqual(new[] { 10 }, PageRanges.Parse("10-10", 10));
    }

    [TestMethod]
    public void SortedWithoutRepeats()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 5 }, PageRanges.Parse("5,1-3,2,1", 10));
        CollectionAssert.AreEqual(new[] { 1, 3 }, PageRanges.Parse("3,,1,", 10));
    }

    [TestMethod]
    public void PagesOutsideTheDocument()
    {
        foreach (string spec in new[] { "0", "11", "9-11", "5-2", "0-3" })
        {
            var caught = Assert.ThrowsExactly<DmcException>(() => PageRanges.Parse(spec, 10), spec);
            StringAssert.Contains(caught.Message, "outside 1-10");
        }
    }

    // Beyond the Python test: the script's int() raises ValueError on these, which is a traceback; dmc names the part.

    [TestMethod]
    public void WhatIsNotANumberIsNamed()
    {
        foreach (string spec in new[] { "x", "1-x", "a-b", "-3", "1-2-3", "1 2", "1.5", "٣x" })
        {
            var caught = Assert.ThrowsExactly<DmcException>(() => PageRanges.Parse(spec, 10), spec);
            StringAssert.Contains(caught.Message, "is not a page number or range");
        }
        Assert.AreEqual("Page range '1-x' is not a page number or range.",
            Assert.ThrowsExactly<DmcException>(() => PageRanges.Parse("2, 1-x", 10)).Message);
    }

    [TestMethod]
    public void NumbersAreReadAsPythonReadsThem()
    {
        // int() takes a sign, underscores between digits and digits of other scripts.
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, PageRanges.Parse("+1-3", 10));
        CollectionAssert.AreEqual(new[] { 1_0 }, PageRanges.Parse("1_0", 10));
        CollectionAssert.AreEqual(new[] { 3 }, PageRanges.Parse("٣", 10)); // Arabic-Indic three
        CollectionAssert.AreEqual(new[] { 5 }, PageRanges.Parse("005", 10));
        // A trailing dash is that page alone, as in the script (an empty "last" is the first).
        CollectionAssert.AreEqual(new[] { 5 }, PageRanges.Parse("5-", 10));
        foreach (string bad in new[] { "_1", "1_", "1__0" })
            Assert.ThrowsExactly<DmcException>(() => PageRanges.Parse(bad, 10), bad);
    }

    [TestMethod]
    public void HugeNumbersAreOutsideTheDocumentNotAnOverflow()
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => PageRanges.Parse("1-99999999999999999999", 10));
        StringAssert.Contains(caught.Message, "outside 1-10");
    }
}

/// <summary>What the text of a page looks like: PdfPig's content order, with the changes of <see cref="PageText"/>.</summary>
[TestClass]
public sealed class PageTextTests
{
    /// <summary>The text <see cref="PageText"/> makes of one page of a PDF given as bytes.</summary>
    static string TextOf(byte[] pdf, int page = 1)
    {
        using var document = PdfDocument.Open(pdf);
        return PageText.Of(document.GetPage(page));
    }

    /// <summary>Glossary rows as the Pinebrook module has them: a word, dot leaders and the translation.</summary>
    static string[] DotLeaderRows { get; } =
    [
        "above .............................svern",
        "after ................................ghent",
        "air ....................................thrae",
        "and .....................................vur",
        "animal ........................baeshra",
        "armor ...............................litrix",
    ];

    [TestMethod]
    public void EveryRowOfADotLeaderTableIsItsOwnLine()
    {
        // The fix of Phase 3 for what the spike found on page 14 of the Pinebrook module: rows ran together.
        string[] lines = TextOf(DotLeaderTable(DotLeaderRows)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.AreEqual(DotLeaderRows, lines);
    }

    [TestMethod]
    public void PdfPigAloneRunsTheRowsTogether()
    {
        // The reason PageText exists. If this starts failing, PdfPig has fixed it: drop PageText's own rule.
        using var document = PdfDocument.Open(DotLeaderTable(DotLeaderRows));
        string theirs = ContentOrderTextExtractor.GetText(document.GetPage(1));
        Assert.IsLessThan(DotLeaderRows.Length, theirs.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
            "PdfPig's own extractor put the rows on separate lines:\n" + theirs);
        StringAssert.Contains(theirs, "svernafter");
    }

    [TestMethod]
    public void ARowWithNoDotLeadersIsNotSplit()
    {
        // A backward jump only counts when it is large: ordinary text in one string stays on its line.
        string text = TextOf(Build([new Page(Line(72, 72, "one line of ordinary text, kept whole"))]));
        Assert.AreEqual("one line of ordinary text, kept whole", text);
    }

    [TestMethod]
    public void ThePageNumberIsNotGluedToTheFooter()
    {
        // Page 14 of the module ends "...personal use only.14 | Appendix A", and others begin "5TM & ©2023".
        string[] lines = TextOf(PageNumberThenFooter("5", "TM & (c)2023 WIZARDS OF THE COAST LLC. Permission is granted"))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.AreEqual(new[] { "5", "TM & (c)2023 WIZARDS OF THE COAST LLC. Permission is granted" }, lines);
    }

    [TestMethod]
    public void TextEndsWithItsLastCharacterAndLinesAreLf()
    {
        // As PyMuPDF's does: no line break and no trailing space (the last line of the Python test's page has one).
        string text = TextOf(SilverEgg(), page: 2);
        Assert.IsTrue(text.EndsWith("gallery.", StringComparison.Ordinal), text);
        Assert.IsFalse(text.Contains('\r'));
        Assert.AreEqual(9, text.Count(c => c == '\n')); // the heading, a blank line, and eight lines
        StringAssert.StartsWith(text, "Silver Egg page 2\n\nThe goblins");
    }

    [TestMethod]
    public void ABigGapIsABlankLine()
    {
        Assert.AreEqual("first\n\nsecond\nthird", TextOf(Build([new Page(Line(72, 72, "first") + Line(72, 100, "second") + Line(72, 114, "third"))])));
    }

    [TestMethod]
    public void APageWithNoTextHasNoText()
    {
        Assert.AreEqual("", TextOf(Build([new Page("")])));
    }

    [TestMethod]
    public void LinesFallingByAWholeLineAreSeparate()
    {
        Assert.AreEqual("first\nsecond", TextOf(Build([new Page(Line(72, 72, "first") + Line(72, 86, "second"))])));
    }
}

/// <summary>Opening a PDF, and the guard against a missing OpenSSL on Linux.</summary>
[TestClass]
public sealed class PdfFileTests
{
    /// <summary>The test's temporary folder, holding the PDFs it writes.</summary>
    string _folder = "";

    /// <summary>Creates the temporary folder.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>Write a file in the temporary folder and return its path.</summary>
    string Write(string name, byte[] bytes)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [TestMethod]
    public void AUserPasswordIsRefusedInOneLine()
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => PdfFile.Open(Write("locked.pdf", Locked())));
        Assert.AreEqual("This PDF is password-protected. Ask the player for an unlocked copy.", caught.Message);
    }

    [TestMethod]
    public void AnOwnerPasswordAloneOpens()
    {
        using var document = PdfFile.Open(Write("owner-only.pdf", OwnerOnly()));
        Assert.AreEqual(Pages, document.NumberOfPages);
    }

    [TestMethod]
    public void AFileThatIsNotAPdfIsNamedAsSuch()
    {
        var caught = Assert.ThrowsExactly<DmcException>(() => PdfFile.Open(Write("x.pdf", "%PDF-1.7\nnot really\n"u8.ToArray())));
        StringAssert.StartsWith(caught.Message, "not a readable PDF: ");
        Assert.DoesNotContain("\n", caught.Message);
    }

    [TestMethod]
    public void WithoutOpenSslAnEncryptedPdfIsRefusedBeforePdfPigTouchesIt()
    {
        // On Linux without libssl the runtime aborts the process inside the decryption: the check has to come first.
        string path = Write("locked.pdf", Locked());
        var caught = Assert.ThrowsExactly<DmcException>(() => PdfFile.Open(path, libsslAvailable: () => false));
        Assert.AreEqual("This PDF is encrypted, and reading it needs OpenSSL (libssl), which is not installed.", caught.Message);
        // The same file with OpenSSL there is refused for its password, which is the real reason.
        var password = Assert.ThrowsExactly<DmcException>(() => PdfFile.Open(path, libsslAvailable: () => true));
        StringAssert.Contains(password.Message, "password-protected");
    }

    [TestMethod]
    public void WithoutOpenSslAPlainPdfStillOpens()
    {
        using var document = PdfFile.Open(Write("plain.pdf", SilverEgg()), libsslAvailable: () => false);
        Assert.AreEqual(Pages, document.NumberOfPages);
    }

    [TestMethod]
    public void WithoutOpenSslAnOwnerOnlyPdfIsRefusedToo()
    {
        // It needs decryption as well, so on such a machine it cannot be read either.
        var caught = Assert.ThrowsExactly<DmcException>(() => PdfFile.Open(Write("owner-only.pdf", OwnerOnly()), libsslAvailable: () => false));
        StringAssert.Contains(caught.Message, "OpenSSL");
    }

    [TestMethod]
    public void TheEncryptEntryIsFoundInTheFirstOrTheLastMebibyte()
    {
        Assert.IsTrue(PdfFile.LooksEncrypted(Write("locked.pdf", Locked())));
        Assert.IsFalse(PdfFile.LooksEncrypted(Write("plain.pdf", SilverEgg())));

        byte[] filler = new byte[3 << 20];
        Array.Fill(filler, (byte)'x');
        byte[] Wrapped(string head, string tail) =>
            [.. System.Text.Encoding.ASCII.GetBytes(head), .. filler, .. System.Text.Encoding.ASCII.GetBytes(tail)];

        Assert.IsTrue(PdfFile.LooksEncrypted(Write("big-head.pdf", Wrapped("%PDF-1.7\n/Encrypt 5 0 R\n", "%%EOF\n"))), "in the first MiB");
        Assert.IsTrue(PdfFile.LooksEncrypted(Write("big-tail.pdf", Wrapped("%PDF-1.7\n", "trailer << /Encrypt 5 0 R >>\n%%EOF\n"))), "in the last MiB");
        Assert.IsFalse(PdfFile.LooksEncrypted(Write("big-plain.pdf", Wrapped("%PDF-1.7\n", "trailer << /Root 1 0 R >>\n%%EOF\n"))));
    }

    [TestMethod]
    public void TheEncryptEntryIsFoundInACrossReferenceStreamFarFromBothEnds()
    {
        // The dictionary of the last cross-reference stream sits 3 MiB in, followed by 3 MiB of its compressed data.
        byte[] filler = new byte[3 << 20];
        Array.Fill(filler, (byte)'x');
        byte[] Pdf(string dictionary)
        {
            byte[] head = [.. System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n"), .. filler];
            string tail = $"\nendstream\nendobj\nstartxref\n{head.Length}\n%%EOF\n";
            return [.. head, .. System.Text.Encoding.ASCII.GetBytes($"9 0 obj\n{dictionary}\nstream\n"), .. filler,
                .. System.Text.Encoding.ASCII.GetBytes(tail)];
        }

        Assert.IsTrue(PdfFile.LooksEncrypted(Write("xref-stream.pdf", Pdf("<< /Type /XRef /Encrypt 5 0 R /Root 1 0 R >>"))));
        Assert.IsFalse(PdfFile.LooksEncrypted(Write("xref-stream-plain.pdf", Pdf("<< /Type /XRef /Root 1 0 R >>"))));
    }

    [TestMethod]
    public void OpenSslIsFoundOnThisMachineOrNotWithoutCrashing()
    {
        // Only that the probe answers: it is a load attempt, and on Windows and macOS it is never asked.
        _ = PdfFile.HasLibssl();
    }
}
