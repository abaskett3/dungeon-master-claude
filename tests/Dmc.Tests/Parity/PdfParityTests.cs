using System.Text;
using System.Text.RegularExpressions;
using Dmc.Tests.Pdf;
using Dmc.Tests.Support;
using SkiaSharp;
using static Dmc.Tests.Pdf.PdfFixtures;

namespace Dmc.Tests.Parity;

/// <summary>
/// <c>dmc pdf</c> against <c>scripts/pdf_extract.py</c> on the same PDFs: stdout, stderr, the exit code and the files
/// written, case by case. The two programs use different libraries (PyMuPDF; PdfPig for text and PDFium for pictures), so
/// "the same" has two levels, and each case says which it needs:
/// <list type="bullet">
/// <item><b>Same text</b> (<see cref="Same"/>): simple pages where one line is one line and the order cannot be argued
///   over. Every line of output and every text file must be identical, except trailing spaces at the end of a line (PyMuPDF
///   keeps them, dmc does not, as PdfPig does not) and a blank line at the end of a page.</item>
/// <item><b>Same characters</b> (<see cref="Loose"/>): pages with a layout both tools read differently (sloping dot-leader
///   rows, a page number and a footer). Only the non-whitespace characters, in order, must be identical, the way the Phase 0
///   spike compared the two on the Pinebrook module, and the character counts that <c>info</c> prints are not compared.
///   PyMuPDF pads lines with runs of spaces to show columns, and PdfPig writes each line once, so the text is shorter by
///   design (31% on the module).</item>
/// <item>Either way, a picture has to be a JPEG of the same width and height: its bytes differ because the encoders and
///   the page renderers are different programs, and what DMC pays for is the pixels (<see cref="JpegSize"/>).</item>
/// </list>
/// Error messages, exit codes and the files written (or not) on a failure are compared exactly, apart from the prefix:
/// the script's <c>sys.exit</c> messages have none, and dmc says "dmc pdf: " first, as the dispatcher does for every command;
/// argparse's usage text before an argument error is left out by dmc (one line only).
/// </summary>
[TestClass]
public sealed partial class PdfParityTests
{
    [GeneratedRegex(@"^usage: pdf_extract\.py .*\n( {5,}.*\n)*", RegexOptions.Multiline)]
    private static partial Regex ArgparseUsage();

    [GeneratedRegex(@"^(p\d+): \d+ chars", RegexOptions.Multiline)]
    private static partial Regex CharCount();

    [GeneratedRegex(@"[ \t]+\n")]
    private static partial Regex TrailingSpaces();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>The script's messages come with no prefix; dmc's with "dmc pdf: ".</summary>
    static string Errors(string text)
    {
        // Python 3.12.something and later list the choices bare, 3.11 and the first 3.12s quote each one; dmc writes them bare.
        text = ArgparseUsage().Replace(text, "").Replace("pdf_extract.py: error: ", "dmc pdf: ")
            .Replace("(choose from 'info', 'text', 'render', 'all')", "(choose from info, text, render, all)");
        return text.Length > 0 && !text.StartsWith("dmc pdf: ", StringComparison.Ordinal) ? "dmc pdf: " + text : text;
    }

    /// <summary>The size of a JPEG, which is what two page renderers have to agree on.</summary>
    static string? JpegSize(string name, byte[] bytes)
    {
        if (!name.EndsWith(".jpg", StringComparison.Ordinal))
            return null;
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException($"{name} is not a picture");
        return $"JPEG {codec.Info.Width} x {codec.Info.Height}";
    }

    static string PythonLine(string text) => text.Replace("\r\n", "\n");

    static ProcessResult RunScript(string work, string[] args, string? stdin)
    {
        string? python = Legacy.Python();
        TestEnv.Need(python is not null, "Python 3");
        TestEnv.Need(Legacy.HasPyMuPdf(), "PyMuPDF");
        return TestEnv.Run(python!, [Legacy.Script("pdf_extract.py"), .. args], stdin, work,
            environment: new Dictionary<string, string> { ["PYTHONIOENCODING"] = "utf-8" });
    }

    static readonly ParityTool Same = new("pdf_extract.py", "pdf", RunScript,
        Normalize: Errors,
        NormalizeFile: (_, text) => TrailingSpaces().Replace(PythonLine(text), "\n").TrimEnd('\n'),
        Fingerprint: JpegSize);

    static readonly ParityTool Loose = new("pdf_extract.py", "pdf", RunScript,
        Normalize: text => CharCount().Replace(Errors(text), "$1: <n> chars"),
        NormalizeFile: (_, text) => Whitespace().Replace(text, ""),
        Fingerprint: JpegSize);

    // ---------- building cases ----------

    /// <summary>The PDFs of a case, by file name, written into the working folder before each program runs.</summary>
    static Action<string> Seed(params (string Name, byte[] Bytes)[] files) => work =>
    {
        foreach (var (name, bytes) in files)
            File.WriteAllBytes(Path.Combine(work, name), bytes);
    };

    static readonly (string, byte[])[] SilverEggFile = [("module.pdf", SilverEgg())];

    /// <summary>These PDFs in the working folder, then the command, the first PDF and these arguments, and <c>--out</c> there.</summary>
    static ParityCase Pdf(string name, (string Name, byte[] Bytes)[] files, string command, params string[] args) =>
        new(name, work => [command, Path.Combine(work, files[0].Name), .. args, "--out", Path.Combine(work, "out")], null, Seed(files));

    static ParityCase Egg(string name, string command, params string[] args) => Pdf(name, SilverEggFile, command, args);

    /// <summary>Lines of x, one under the other, of these lengths: a page whose text is as long as the lengths and line breaks make it.</summary>
    static string Lines(params int[] lengths) =>
        string.Concat(lengths.Select((length, i) => Line(72, 72 + i * 14, new string('x', length))));

    // ---------- the cases: same text ----------

    static IEnumerable<ParityCase> SameText()
    {
        yield return Egg("info", "info");
        yield return Egg("info of one page", "info", "--pages", "2");
        yield return Egg("info of some pages", "info", "--pages", "1-2,4");
        yield return Egg("text", "text");
        yield return Egg("text of some pages", "text", "--pages", "2,4");
        yield return Egg("render", "render", "--pages", "1,3", "--dpi", "40");
        yield return Egg("render at the default dpi", "render", "--pages", "2");
        yield return Egg("all", "all", "--dpi", "40");
        yield return Egg("all of some pages", "all", "--pages", "3-4", "--dpi", "40");
        yield return Egg("dpi given with an equals sign", "render", "--pages", "1", "--dpi=72");
        yield return Egg("dpi with a plus sign and underscore", "render", "--pages", "1", "--dpi", "+4_0");
        yield return Egg("a shortened option", "text", "--pa", "2");
        yield return Egg("pages given with an equals sign", "text", "--pages=1,3");
        yield return Egg("a trailing dash is one page", "text", "--pages", "2-");
        yield return Egg("pages with spaces", "info", "--pages", " 2 - 3 , 1 ");
        yield return Egg("a repeated option, the last wins", "info", "--pages", "1", "--pages", "3");
        yield return new ParityCase("an option before the arguments",
            work => ["--out", Path.Combine(work, "out"), "--pages", "4", "text", Path.Combine(work, "module.pdf")], null, Seed(SilverEggFile));
        yield return new ParityCase("the default output folder", _ => ["all", "module.pdf", "--pages", "1", "--dpi", "30"], null, Seed(SilverEggFile));
        yield return new ParityCase("a relative output folder", _ => ["text", "module.pdf", "--out", "sub/dir"], null, Seed(SilverEggFile));
        yield return new ParityCase("an output folder with a trailing slash", _ => ["text", "module.pdf", "--out", "sub/"], null, Seed(SilverEggFile));
        yield return new ParityCase("a PDF in another folder", _ => ["info", "pdfs/module.pdf"], null, work =>
        {
            Directory.CreateDirectory(Path.Combine(work, "pdfs"));
            File.WriteAllBytes(Path.Combine(work, "pdfs", "module.pdf"), SilverEgg());
        });
        yield return new ParityCase("the arguments after a double dash", _ => ["info", "--", "module.pdf"], null, Seed(SilverEggFile));
        yield return Pdf("a print-restricted PDF reads", [("module.pdf", OwnerOnly())], "all", "--dpi", "40");

        // Page sizes: the same size in pixels, from the rounding PyMuPDF does (MuPDF's fz_round_rect).
        yield return Pdf("an A4 page", [("module.pdf", Build([new Page(Line(72, 72, "A4", height: 841.89), 595.276, 841.89)]))], "all");
        yield return Pdf("an A4 page at 72 dpi", [("module.pdf", Build([new Page(Line(72, 72, "A4", height: 841.89), 595.276, 841.89)]))], "render", "--dpi", "72");
        yield return Pdf("an A5 page at 150 dpi", [("module.pdf", Build([new Page(Line(72, 72, "A5", height: 595.28), 419.53, 595.28)]))], "render", "--dpi", "150");
        yield return Pdf("a page of 7 inches at 300 dpi", [("module.pdf", Build([new Page(Line(72, 72, "x", height: 720), 504, 720)]))], "render", "--dpi", "300");
        yield return Pdf("a page that is not a round number of points", [("module.pdf", Build([new Page(Line(72, 72, "x", height: 700.4), 500.7, 700.4)]))], "all", "--dpi", "96");
        yield return Pdf("pages of different sizes", [("module.pdf", Build(
        [
            new Page(Line(72, 72, "letter"), 612, 792),
            new Page(Line(72, 72, "A4", height: 841.89), 595.276, 841.89),
            new Page(Line(72, 72, "letter again"), 612, 792),
            new Page(Line(72, 72, "wide", height: 400), 900, 400),
        ]))], "all", "--dpi", "50");
        yield return Pdf("a page turned a quarter turn", [("module.pdf", Build([new Page(Line(72, 72, "Landscape", height: 612), 792, 612, Rotate: 90)]))], "all", "--dpi", "40");
        yield return Pdf("a page turned three quarters", [("module.pdf", Build([new Page(Line(72, 72, "Landscape", height: 612), 792, 612, Rotate: 270)]))], "info");
        yield return Pdf("a cropped page", [("module.pdf", Build([new Page(Line(72, 72, "Cropped"), CropBox: [0, 396, 612, 792])]))], "all", "--dpi", "40");
        yield return Pdf("a page with an image", [("module.pdf", Build([new Page(Line(72, 72, "Picture"), Image: true), new Page(Line(72, 72, "Plain"))]))], "all", "--dpi", "40");
        yield return Pdf("a page with no text", [("module.pdf", Build([new Page("", Image: true)]))], "all", "--dpi", "40");
        // info counts the images the page's resources list, as PyMuPDF's get_images does, not the images the page draws.
        const string drawImage = "q 50 0 0 50 400 400 cm /Im1 Do Q\n";
        yield return Pdf("images listed and drawn in different ways", [("module.pdf", Build(
        [
            new Page(Line(72, 72, "drawn twice") + drawImage + drawImage, XObjects: "/Im1 4 0 R"),
            new Page(Line(72, 72, "listed, not drawn"), XObjects: "/Im1 4 0 R"),
            new Page(Line(72, 72, "one image under two names"), XObjects: "/Im1 4 0 R /Im2 4 0 R"),
            new Page(Line(72, 72, "only inside a form") + "q 50 0 0 50 400 400 cm /Fm1 Do Q\n", XObjects: "/Fm1 5 0 R"),
            new Page(Line(72, 72, "one form under two names"), XObjects: "/Fm1 5 0 R /Fm2 5 0 R"),
            new Page(Line(72, 72, "an inline image") + "q 50 0 0 50 400 400 cm BI /W 1 /H 1 /CS /G /BPC 8 ID \x80 EI Q\n"),
        ]))], "info");
        yield return Pdf("a page of exactly 200 characters is not sparse", [("module.pdf", Build([new Page(Lines(49, 49, 49, 50))]))], "info");
        yield return Pdf("a page of 199 characters is sparse", [("module.pdf", Build([new Page(Lines(49, 49, 49, 49))]))], "info");
    }

    // ---------- the cases: same characters ----------

    static IEnumerable<ParityCase> SameCharacters()
    {
        string[] rows =
        [
            "above .............................svern", "after ................................ghent",
            "air ....................................thrae", "and .....................................vur",
            "animal ........................baeshra", "armor ...............................litrix",
        ];
        yield return Pdf("a dot-leader table", [("module.pdf", DotLeaderTable(rows))], "all", "--dpi", "40");
        // Not here: a page number drawn before its footer. PyMuPDF sorts by position and puts the footer first; dmc follows the file and
        // puts the number first, so the characters differ in order by design (PageTextTests.ThePageNumberIsNotGluedToTheFooter).
        yield return Pdf("a paragraph in two lines and a heading", [("module.pdf", Build(
        [
            new Page(Line(72, 72, "Heading", size: 18) + Line(72, 100, "The goblins keep watch from the gallery,") + Line(72, 114, "and the heroes creep along the wall.")),
        ]))], "all", "--dpi", "40");
    }

    // ---------- the cases: errors (the same in both tiers) ----------

    /// <summary>Every way a run fails that the script reports, or argparse does: exit code, message, and nothing written.</summary>
    public static IEnumerable<ParityCase> ErrorCases()
    {
        string pdf(string work) => Path.Combine(work, "module.pdf");
        string outFolder(string work) => Path.Combine(work, "out");
        ParityCase Args(string name, Func<string, string[]> args) => new(name, args, null, Seed(SilverEggFile));

        yield return Args("no arguments", _ => []);
        yield return Args("no PDF", _ => ["info"]);
        yield return Args("no command", _ => ["module.pdf"]);
        yield return Args("a command that does not exist", w => ["extract", pdf(w)]);
        yield return Args("a command in capitals", w => ["INFO", pdf(w)]);
        yield return Args("an extra argument", w => ["info", pdf(w), "extra"]);
        yield return Args("two extra arguments", w => ["info", pdf(w), "extra", "more"]);
        yield return Args("an option that does not exist", w => ["info", pdf(w), "--bogus"]);
        yield return Args("an option that does not exist, with a value", w => ["info", pdf(w), "--bogus", "x"]);
        yield return Args("a single-dash option", w => ["info", pdf(w), "-x"]);
        yield return Args("pages without a value", w => ["info", pdf(w), "--pages"]);
        yield return Args("pages followed by an option", w => ["info", pdf(w), "--pages", "--dpi", "5"]);
        yield return Args("out without a value", w => ["text", pdf(w), "--out"]);
        yield return Args("dpi without a value", w => ["render", pdf(w), "--dpi"]);
        yield return Args("dpi that is a word", w => ["render", pdf(w), "--dpi", "high"]);
        yield return Args("dpi with a decimal point", w => ["render", pdf(w), "--dpi", "1.5"]);
        yield return Args("dpi that is empty", w => ["render", pdf(w), "--dpi="]);
        yield return Args("dpi that is a word, on info", w => ["info", pdf(w), "--dpi", "x"]);
        yield return Args("a flag given a value", w => ["info", pdf(w), "--help=x"]);
        yield return Args("the invalid command is reported first", w => ["extract", pdf(w), "--dpi", "x"]);
        yield return Args("the invalid dpi is reported first", w => ["--dpi", "x", "extract", pdf(w)]);
        yield return Args("a missing file", w => ["info", Path.Combine(w, "nope.pdf")]);
        yield return Args("a folder for a file", w => ["info", w]);
        yield return Args("a missing file, with a bad page range", w => ["text", Path.Combine(w, "nope.pdf"), "--pages", "9"]);
        foreach (string spec in new[] { "0", "5", "3-9", "5-2", "0-3", "99-100", "1,5", "2,3-9", "4-3" })
        {
            yield return Args($"pages {spec}", w => ["all", pdf(w), "--pages", spec, "--out", outFolder(w)]);
            yield return Args($"info of pages {spec}", w => ["info", pdf(w), "--pages", spec]);
        }
        yield return Pdf("a password-protected PDF, text", [("module.pdf", Locked())], "text");
        yield return Pdf("a password-protected PDF, info", [("module.pdf", Locked())], "info");
        yield return Pdf("a password-protected PDF, render", [("module.pdf", Locked())], "render");
        yield return Pdf("a password-protected PDF, all", [("module.pdf", Locked())], "all", "--pages", "1");
        yield return new ParityCase("a password-protected PDF, with a bad page range too", w => ["text", Path.Combine(w, "module.pdf"), "--pages", "99"], null, Seed(("module.pdf", Locked())));
    }

    /// <summary>
    /// Runs that fail in dmc and are not compared with the script, because the script crashes with a traceback on them (a
    /// page range that is not a number, a file that is not a PDF) or does something else (a resolution of 0, which it
    /// passes on to PyMuPDF). They are in the one-line guard of <c>ErrorPathTests</c>, with <see cref="ErrorCases"/>.
    /// </summary>
    public static IEnumerable<ParityCase> DmcOnlyErrorCases()
    {
        ParityCase Egg(string name, params string[] args) => new(name, _ => args, null, Seed(SilverEggFile));

        yield return new ParityCase("a file that is not a PDF", _ => ["info", "module.pdf"], null, Seed(("module.pdf", "%PDF-1.7\nthis is not really a pdf\n"u8.ToArray())));
        yield return new ParityCase("a file with nothing in it", _ => ["text", "module.pdf"], null, Seed(("module.pdf", [])));
        yield return new ParityCase("a text file", _ => ["info", "module.pdf"], null, Seed(("module.pdf", "just some words\n"u8.ToArray())));
        yield return Egg("an empty name", "info", "");
        foreach (string spec in new[] { "x", "1-x", "-3", "1--2", "1.5", "1 2", "99999999999999999999" })
            yield return Egg($"pages {spec}", "text", "module.pdf", "--pages", spec);
        foreach (string dpi in new[] { "0", "-5", "1201", "99999999999999999999" })
        {
            yield return Egg($"render at {dpi} dpi", "render", "module.pdf", "--dpi", dpi);
            yield return Egg($"all at {dpi} dpi", "all", "module.pdf", "--dpi", dpi);
        }
    }

    public static IEnumerable<object[]> SameData => SameText().Select(c => new object[] { c });

    public static IEnumerable<object[]> SameCharactersData => SameCharacters().Select(c => new object[] { c });

    public static IEnumerable<object[]> ErrorData => ErrorCases().Select(c => new object[] { c });

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    [TestMethod]
    [DynamicData(nameof(SameData), DynamicDataDisplayName = nameof(CaseName))]
    public void PdfMatchesPdfExtractPy(ParityCase test) => ParityCheck.Run(Same, test);

    [TestMethod]
    [DynamicData(nameof(SameCharactersData), DynamicDataDisplayName = nameof(CaseName))]
    public void PdfHasTheSameCharactersAsPdfExtractPy(ParityCase test) => ParityCheck.Run(Loose, test);

    [TestMethod]
    [DynamicData(nameof(ErrorData), DynamicDataDisplayName = nameof(CaseName))]
    public void PdfFailsLikePdfExtractPy(ParityCase test) => ParityCheck.Run(Same, test);
}
