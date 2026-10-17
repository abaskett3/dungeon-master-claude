using System.Text;
using System.Text.RegularExpressions;
using Dmc.Tests.Support;
using SkiaSharp;
using static Dmc.Tests.Pdf.PdfFixtures;

namespace Dmc.Tests.Pdf;

/// <summary>
/// The <c>CommandLine</c> class of <c>tests/test_pdf_extract.py</c>, ported one for one (<c>test_info</c> is
/// <see cref="Info"/>, and so on), against a four-page PDF made on the spot. They run twice, through the two subclasses
/// below: against the library code in this process, and against the published <c>dmc</c> binary, the way DMC runs it.
/// In Python they run the script as a subprocess; the error prefix is "dmc pdf: " here, where the script's messages had none.
/// After them come tests of what the script left to argparse or to a crash, which dmc says in one line.
/// </summary>
public abstract partial class PdfCommandContract
{
    /// <summary>The test's temporary working folder.</summary>
    string _folder = "";

    /// <summary>The path of <c>module.pdf</c>, the four-page PDF written into the working folder for every test.</summary>
    string _pdf = "";

    /// <summary>The output folder, <c>out</c> in the working folder; created by the command.</summary>
    string _out = "";

    /// <summary>Run <c>dmc pdf</c> with these arguments.</summary>
    protected abstract ProcessResult Pdf(params string[] args);

    /// <summary>The <c>info</c> line of the made-up PDF's first page: 17 characters, no images, letter size.</summary>
    [GeneratedRegex(@"^p1: 17 chars, 0 images, 8\.5 x 11\.0 in$")]
    private static partial Regex FirstPageLine();

    /// <summary>Creates the working folder and writes the four-page PDF into it.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _pdf = Path.Combine(_folder, "module.pdf");
        _out = Path.Combine(_folder, "out");
        File.WriteAllBytes(_pdf, SilverEgg());
    }

    /// <summary>Deletes the working folder.</summary>
    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>The Python test's <c>extract</c>: the command, the PDF, the arguments and <c>--out</c> (unless asked not to).</summary>
    ProcessResult Extract(string command, string[] args, string? pdf = null, bool outFolder = true) =>
        Pdf([command, pdf ?? _pdf, .. args, .. outFolder ? new[] { "--out", _out } : []]);

    /// <summary><see cref="Extract(string, string[], string?, bool)"/> on the standard PDF, with <c>--out</c>.</summary>
    ProcessResult Extract(string command, params string[] args) => Extract(command, args, pdf: null);

    /// <summary>The sorted names in a folder of the output, or null if it is not there.</summary>
    string[]? Listing(params string[] parts)
    {
        string folder = Path.Combine([_out, .. parts]);
        return Directory.Exists(folder)
            ? Directory.GetFileSystemEntries(folder).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray()
            : null;
    }

    /// <summary>A file of the output folder (its path given in parts) as text, with LF line endings.</summary>
    string Read(params string[] parts) => File.ReadAllText(Path.Combine([_out, .. parts]), Encoding.UTF8).Replace("\r\n", "\n");

    /// <summary>Asserts that the run failed with this exit code and an error line that starts "dmc pdf: " and holds <paramref name="expected"/>, and that nothing was written.</summary>
    void AssertFails(ProcessResult result, string expected, int exitCode = 1)
    {
        Assert.AreEqual(exitCode, result.ExitCode, result.ToString());
        StringAssert.Contains(result.Stderr, "dmc pdf: ");
        StringAssert.Contains(result.Stderr, expected);
        Assert.IsNull(Listing(), "a failed run must not write anything");
    }

    // ---------- the ten tests of the Python CommandLine class ----------

    [TestMethod]
    public void Info()
    {
        var result = Extract("info");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        string[] lines = result.StdoutLines;
        Assert.AreEqual($"pages: {Pages}", lines[0]);
        CollectionAssert.AreEqual(new[] { "p1", "p2", "p3", "p4" }, lines[1..(Pages + 1)].Select(l => l.Split(':')[0]).ToArray());
        Assert.IsTrue(FirstPageLine().IsMatch(lines[1]), lines[1]);
        Assert.IsTrue(lines[^1].EndsWith("1, 3, 4", StringComparison.Ordinal), "pages with little text should be listed");
        Assert.IsNull(Listing(), "info must not write anything");
    }

    [TestMethod]
    public void InfoForSomePagesStillReportsTheWholePageCount()
    {
        var result = Extract("info", "--pages", "2");
        string[] lines = result.StdoutLines;
        Assert.HasCount(2, lines);
        Assert.AreEqual($"pages: {Pages}", lines[0]);
        StringAssert.StartsWith(lines[1], "p2: ");
    }

    [TestMethod]
    public void Text()
    {
        var result = Extract("text");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "all-text.txt", "text" }, Listing());
        CollectionAssert.AreEqual(Enumerable.Range(1, Pages).Select(n => $"page-00{n}.txt").ToArray(), Listing("text"));
        Assert.AreEqual("Silver Egg page 3", Read("text", "page-003.txt").Trim());
        StringAssert.Contains(Read("text", "page-002.txt"), Long.Trim());

        string combined = Read("all-text.txt");
        int[] marks = Enumerable.Range(1, Pages).Select(n => combined.IndexOf($"===== PDF PAGE {n} =====\nSilver Egg page {n}", StringComparison.Ordinal)).ToArray();
        Assert.IsTrue(marks.All(m => m >= 0), "every page has its separator and its text: " + string.Join(", ", marks));
        CollectionAssert.AreEqual(marks.Order().ToArray(), marks);
    }

    [TestMethod]
    public void TextForSomePagesLeavesTheCombinedFileAlone()
    {
        var result = Extract("text", "--pages", "2,4");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "text" }, Listing());
        CollectionAssert.AreEqual(new[] { "page-002.txt", "page-004.txt" }, Listing("text"));
    }

    [TestMethod]
    public void Render()
    {
        var result = Extract("render", "--pages", "1,3", "--dpi", "40");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        StringAssert.Contains(result.Stdout, "render: 2 pages at 40 dpi");
        CollectionAssert.AreEqual(new[] { "pages" }, Listing());
        CollectionAssert.AreEqual(new[] { "page-001.jpg", "page-003.jpg" }, Listing("pages"));
        var size = SKBitmap.DecodeBounds(Path.Combine(_out, "pages", "page-001.jpg"));
        Assert.AreEqual((340, 440), (size.Width, size.Height)); // 8.5 x 11 in at 40 dpi
    }

    [TestMethod]
    public void All()
    {
        var result = Extract("all", "--dpi", "40");
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "all-text.txt", "pages", "text" }, Listing());
        Assert.HasCount(Pages, Listing("pages")!);
        Assert.HasCount(Pages, Listing("text")!);
    }

    [TestMethod]
    public void DefaultOutputIsNextToThePdf()
    {
        var result = Extract("text", ["--pages", "1"], outFolder: false);
        Assert.AreEqual(0, result.ExitCode, result.Stderr);
        CollectionAssert.AreEqual(new[] { "page-001.txt" },
            Directory.GetFileSystemEntries(Path.Combine(_folder, "extracted", "text")).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public void MissingFile()
    {
        var result = Extract("info", [], pdf: Path.Combine(_folder, "nope.pdf"));
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stderr, "No such file");
    }

    [TestMethod]
    public void PageOutsideTheDocument()
    {
        var result = Extract("text", "--pages", "3-9");
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stderr, $"outside 1-{Pages}");
        Assert.IsNull(Listing());
    }

    [TestMethod]
    public void PasswordProtected()
    {
        string locked = Path.Combine(_folder, "locked.pdf");
        File.WriteAllBytes(locked, Locked());
        var result = Extract("text", [], pdf: locked);
        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Stderr, "password-protected");
        Assert.IsNull(Listing());
    }

    // ---------- beyond the Python tests ----------

    [TestMethod]
    public void APageThatCannotBeReadDoesNotStopTheOthers()
    {
        // Page 2 uses a font its resources do not list, so PdfPig fails as it loads the page, before any text is read.
        string broken = Path.Combine(_folder, "broken.pdf");
        File.WriteAllBytes(broken, Build(
        [
            new Page(Line(72, 72, "First page")),
            new Page("BT /F9 11 Tf 72 720 Td (Lost) Tj ET\n", Image: true),
            new Page(Line(72, 72, "Third page")),
        ]));

        var text = Extract("text", [], pdf: broken);
        Assert.AreEqual(0, text.ExitCode, text.ToString());
        StringAssert.StartsWith(Read("text", "page-002.txt"), "[text extraction failed: ");
        Assert.AreEqual("Third page", Read("text", "page-003.txt"));
        StringAssert.Contains(Read("all-text.txt"), "===== PDF PAGE 3 =====\nThird page");

        var info = Extract("info", [], pdf: broken, outFolder: false);
        Assert.AreEqual(0, info.ExitCode, info.ToString());
        StringAssert.Matches(info.StdoutLines[2], new Regex(@"^p2: \d+ chars, 1 images, 8\.5 x 11\.0 in$"));
        StringAssert.StartsWith(info.StdoutLines[3], "p3: 10 chars, 0 images");
    }

    [TestMethod]
    public void AnOwnerPasswordAloneDoesNotStopReading()
    {
        // Commercial modules are often print-restricted this way; PyMuPDF and dmc both open them.
        string restricted = Path.Combine(_folder, "restricted.pdf");
        File.WriteAllBytes(restricted, OwnerOnly());
        var result = Extract("all", ["--dpi", "40"], pdf: restricted);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("Silver Egg page 2", Read("text", "page-002.txt").Split('\n')[0]);
        Assert.HasCount(Pages, Listing("pages")!);
    }

    [TestMethod]
    public void APasswordProtectedPdfIsRefusedByEveryCommand()
    {
        string locked = Path.Combine(_folder, "locked.pdf");
        File.WriteAllBytes(locked, Locked());
        foreach (string command in new[] { "info", "render", "all" })
        {
            var result = Extract(command, [], pdf: locked);
            AssertFails(result, "password-protected");
        }
    }

    [TestMethod]
    public void NotAPdf()
    {
        string notPdf = Path.Combine(_folder, "notes.pdf");
        File.WriteAllText(notPdf, "%PDF-1.7\nthis is not really a pdf\n");
        AssertFails(Extract("info", [], pdf: notPdf), "not a readable PDF");
    }

    [TestMethod]
    public void AFolderIsNotAFile()
    {
        AssertFails(Extract("info", [], pdf: _folder), "No such file: ");
    }

    [TestMethod]
    public void EveryBadPageRangeIsRefusedBeforeAnythingIsWritten()
    {
        foreach (string spec in new[] { "0", "5", "3-9", "5-2", "0-3", "-1", "x", "1-x", "1--2", "1 2", "1.5", "99999999999999999999999" })
            AssertFails(Extract("all", "--pages", spec), "Page range ");
    }

    [TestMethod]
    public void PagesGivenWithAnEqualsSignOrAShortenedName()
    {
        Assert.AreEqual(0, Extract("text", "--pages=2").ExitCode);
        CollectionAssert.AreEqual(new[] { "page-002.txt" }, Listing("text"));
        Assert.AreEqual(0, Pdf("info", _pdf, "--p", "3,4").ExitCode);
        Assert.AreEqual(0, Pdf("text", _pdf, "--o=" + _out, "--pa", "1").ExitCode);
        CollectionAssert.AreEqual(new[] { "page-001.txt", "page-002.txt" }, Listing("text")); // the first run's page is still there
    }

    [TestMethod]
    public void AnEmptyOutFolderMeansTheDefault()
    {
        var result = Pdf("text", _pdf, "--pages", "1", "--out", "");
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.IsTrue(File.Exists(Path.Combine(_folder, "extracted", "text", "page-001.txt")));
    }

    [TestMethod]
    public void TheCommandMustBeOneOfTheFour()
    {
        var result = Pdf("extract", _pdf);
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("dmc pdf: argument command: invalid choice: 'extract' (choose from info, text, render, all)\n", result.Stderr);
    }

    [TestMethod]
    public void MissingArguments()
    {
        Assert.AreEqual("dmc pdf: the following arguments are required: command, pdf\n", Pdf().Stderr);
        Assert.AreEqual("dmc pdf: the following arguments are required: pdf\n", Pdf("info").Stderr);
        Assert.AreEqual(2, Pdf("info").ExitCode);
        Assert.AreEqual("dmc pdf: argument --pages: expected one argument\n", Pdf("info", _pdf, "--pages").Stderr);
        Assert.AreEqual("dmc pdf: argument --out: expected one argument\n", Pdf("info", "--out", "--dpi", "5").Stderr);
        Assert.AreEqual("dmc pdf: unrecognized arguments: --bogus extra\n", Pdf("info", _pdf, "--bogus", "extra").Stderr);
    }

    [TestMethod]
    public void TheDpiMustBeAWholeNumberInRange()
    {
        Assert.AreEqual("dmc pdf: argument --dpi: invalid int value: 'high'\n", Pdf("render", _pdf, "--dpi", "high").Stderr);
        Assert.AreEqual("dmc pdf: argument --dpi: invalid int value: '1.5'\n", Pdf("render", _pdf, "--dpi=1.5").Stderr);
        foreach (string dpi in new[] { "0", "-5", "1201", "99999999999999999999" })
        {
            var result = Pdf("all", _pdf, "--dpi", dpi, "--out", _out);
            AssertFails(result, "--dpi must be a whole number from 1 to 1200");
        }
        // The text commands do not use the resolution, so a bad one is not their business, as in the script.
        Assert.AreEqual(0, Pdf("info", _pdf, "--dpi", "0").ExitCode);
        Assert.AreEqual(0, Pdf("render", _pdf, "--pages", "1", "--dpi", "1", "--out", _out).ExitCode);
    }

    [TestMethod]
    public void AnOptionMayComeBeforeThePositionalArguments()
    {
        var result = Pdf("--out", _out, "--pages", "4", "text", _pdf);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        CollectionAssert.AreEqual(new[] { "page-004.txt" }, Listing("text"));
    }

    [TestMethod]
    public void HelpPrintsTheUsage()
    {
        var result = Pdf("--help");
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.Contains(result.Stdout, "usage: dmc pdf info");
        Assert.AreEqual("", result.Stderr);
    }

    [TestMethod]
    public void ThePictureOfAPageIsAsBigAsPyMuPdfMakesIt()
    {
        // A4 is 595.276 x 841.89 points, so 110 dpi is 909.4 x 1286.2 pixels: PyMuPDF's pixmap rounds the size up.
        string a4 = Path.Combine(_folder, "a4.pdf");
        File.WriteAllBytes(a4, Build([new Page(Line(72, 72, "A4", height: 841.89), Width: 595.276, Height: 841.89)]));
        Assert.AreEqual(0, Extract("render", [], pdf: a4).ExitCode);
        var size = SKBitmap.DecodeBounds(Path.Combine(_out, "pages", "page-001.jpg"));
        Assert.AreEqual((910, 1287), (size.Width, size.Height));
    }

    [TestMethod]
    public void ARotatedPageIsSizedAsItIsSeen()
    {
        string turned = Path.Combine(_folder, "turned.pdf");
        File.WriteAllBytes(turned, Build([new Page(Line(72, 72, "Landscape", height: 612), Width: 792, Height: 612, Rotate: 90)]));
        var info = Extract("info", [], pdf: turned);
        StringAssert.Contains(info.Stdout, "p1: 9 chars, 0 images, 8.5 x 11.0 in");
        Assert.AreEqual(0, Extract("render", ["--dpi", "40"], pdf: turned).ExitCode);
        var size = SKBitmap.DecodeBounds(Path.Combine(_out, "pages", "page-001.jpg"));
        Assert.AreEqual((340, 440), (size.Width, size.Height));
    }

    [TestMethod]
    public void OnlyTheCropBoxIsShown()
    {
        string cropped = Path.Combine(_folder, "cropped.pdf");
        File.WriteAllBytes(cropped, Build([new Page(Line(72, 72, "Cropped"), CropBox: [0, 396, 612, 792])]));
        StringAssert.Contains(Extract("info", [], pdf: cropped).Stdout, "8.5 x 5.5 in");
        Assert.AreEqual(0, Extract("render", ["--dpi", "40"], pdf: cropped).ExitCode);
        var size = SKBitmap.DecodeBounds(Path.Combine(_out, "pages", "page-001.jpg"));
        Assert.AreEqual((340, 220), (size.Width, size.Height));
    }

    [TestMethod]
    public void ImagesOnAPageAreCounted()
    {
        string pictured = Path.Combine(_folder, "pictured.pdf");
        File.WriteAllBytes(pictured, Build([new Page(Line(72, 72, "Picture"), Image: true), new Page(Line(72, 72, "Plain"))]));
        string[] lines = Extract("info", [], pdf: pictured).StdoutLines;
        StringAssert.Contains(lines[1], "7 chars, 1 images");
        StringAssert.Contains(lines[2], "5 chars, 0 images");
    }

    [TestMethod]
    public void ARunIsRepeatable()
    {
        Assert.AreEqual(0, Extract("all", "--dpi", "40").ExitCode);
        string before = Read("all-text.txt");
        Assert.AreEqual(0, Extract("all", "--dpi", "40").ExitCode);
        Assert.AreEqual(before, Read("all-text.txt"));
    }

    /// <summary>
    /// A run that fails on a later page leaves nothing behind. The PDF has a small first page and a second 4000 points
    /// wide: at 1200 dpi page 1 draws and is written, but page 2 is 66,667 pixels across, more than Skia encodes as a JPEG,
    /// so <c>dmc pdf</c> reports "could not be saved as a JPEG". No test hook is needed. Checked for <c>all</c> (the text
    /// files are written first) and <c>render</c>, with an output folder that does not exist, one with its parents
    /// missing, and one that already holds files, which must come out byte for byte as they were. A failed run is also
    /// silent on stdout, and leaves no <c>.partial-</c> folder beside the output.
    /// </summary>
    [TestMethod]
    public void AFailureOnALaterPageLeavesNoNewFilesAndChangesNoOldOnes()
    {
        string wide = Path.Combine(_folder, "wide.pdf");
        File.WriteAllBytes(wide, Build([new Page(Line(2, 10, "a", height: 20, size: 5), 20, 20), new Page("", 4000, 10)]));
        string deep = Path.Combine(_folder, "deep", "er", "out");
        string[] before = Directory.GetFileSystemEntries(_folder).Order(StringComparer.Ordinal).ToArray();

        foreach (string command in new[] { "all", "render" })
        {
            foreach (string output in new[] { _out, deep })
            {
                var result = Pdf(command, wide, "--dpi", "1200", "--out", output);
                Assert.AreEqual(1, result.ExitCode, result.ToString());
                StringAssert.Contains(result.Stderr, "dmc pdf: Page 2 could not be saved as a JPEG at 1200 dpi");
                Assert.AreEqual("", result.Stdout, "nothing is reported for a run that did not finish");
                CollectionAssert.AreEqual(before, Directory.GetFileSystemEntries(_folder).Order(StringComparer.Ordinal).ToArray(),
                    $"{command} into {Path.GetFileName(output)}: no output folder, parent or .partial- folder is left");
            }
        }

        // Files already in the output folder are neither replaced nor deleted by the failed run.
        Directory.CreateDirectory(Path.Combine(_out, "pages"));
        Directory.CreateDirectory(Path.Combine(_out, "text"));
        var old = new Dictionary<string, string>
        {
            [Path.Combine(_out, "pages", "page-001.jpg")] = "an old picture",
            [Path.Combine(_out, "text", "page-001.txt")] = "old text",
            [Path.Combine(_out, "all-text.txt")] = "old combined text",
            [Path.Combine(_out, "notes.md")] = "the player's own file",
        };
        foreach (var (path, content) in old)
            File.WriteAllText(path, content);
        Assert.AreEqual(1, Pdf("all", wide, "--dpi", "1200", "--out", _out).ExitCode);
        CollectionAssert.AreEquivalent(old.Keys.ToArray(), Directory.GetFiles(_out, "*", SearchOption.AllDirectories));
        foreach (var (path, content) in old)
            Assert.AreEqual(content, File.ReadAllText(path), path);
        CollectionAssert.AreEquivalent(new[] { "all-text.txt", "notes.md", "pages", "text" }, Listing());
        Assert.AreEqual(before.Length + 1, Directory.GetFileSystemEntries(_folder).Length, "only the output folder is new: no .partial- folder");
    }

    [TestMethod]
    public void TheCombinedFileIsThePagesJoinedBySeparators()
    {
        Assert.AreEqual(0, Extract("text").ExitCode);
        string expected = string.Join("\n\n", Enumerable.Range(1, Pages).Select(n => $"===== PDF PAGE {n} =====\n{Read("text", $"page-00{n}.txt")}"));
        Assert.AreEqual(expected, Read("all-text.txt"));
    }
}

/// <summary>The PDF tests against the library code, in this process.</summary>
[TestClass]
public sealed class PdfInProcessTests : PdfCommandContract
{
    /// <summary>Runs <c>dmc pdf</c> in this process.</summary>
    protected override ProcessResult Pdf(params string[] args) => DmcRunner.InProcess(["pdf", .. args]);
}

/// <summary>The PDF tests against the published <c>dmc</c> binary, in a subprocess.</summary>
[TestClass]
public sealed class PdfBlackBoxTests : PdfCommandContract
{
    /// <summary>Runs <c>dmc pdf</c> as the published binary, in a subprocess.</summary>
    protected override ProcessResult Pdf(params string[] args) => DmcRunner.BlackBox(["pdf", .. args]);
}
