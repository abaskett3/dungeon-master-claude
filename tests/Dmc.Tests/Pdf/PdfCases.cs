using Dmc.Tests.Support;
using static Dmc.Tests.Pdf.PdfFixtures;

namespace Dmc.Tests.Pdf;

/// <summary>
/// The inputs <c>dmc pdf</c> was compared on against <c>pdf_extract.py</c> (PyMuPDF) until the script was deleted (Phase 6
/// of #3): page sizes and rotations, crop boxes, images, sparse pages, options in every spelling, and every error.
/// <see cref="ErrorCases"/> and <see cref="DmcOnlyErrorCases"/> feed the one-line error guard (<c>ErrorPathTests</c>), and
/// <see cref="PdfCaseTests"/> checks that every other input is still read.
/// </summary>
public static class PdfCases
{
    // ---------- building cases ----------

    /// <summary>The PDFs of a case, by file name, written into the working folder before each program runs.</summary>
    static Action<string> Seed(params (string Name, byte[] Bytes)[] files) => work =>
    {
        foreach (var (name, bytes) in files)
            File.WriteAllBytes(Path.Combine(work, name), bytes);
    };

    static readonly (string, byte[])[] SilverEggFile = [("module.pdf", SilverEgg())];

    /// <summary>These PDFs in the working folder, then the command, the first PDF and these arguments, and <c>--out</c> there.</summary>
    static CommandCase Pdf(string name, (string Name, byte[] Bytes)[] files, string command, params string[] args) =>
        new(name, work => [command, Path.Combine(work, files[0].Name), .. args, "--out", Path.Combine(work, "out")], null, Seed(files));

    static CommandCase Egg(string name, string command, params string[] args) => Pdf(name, SilverEggFile, command, args);

    /// <summary>Lines of x, one under the other, of these lengths: a page whose text is as long as the lengths and line breaks make it.</summary>
    internal static string Lines(params int[] lengths) =>
        string.Concat(lengths.Select((length, i) => Line(72, 72 + i * 14, new string('x', length))));

    // ---------- the cases that are read: simple pages (once compared with PyMuPDF line for line) ----------

    static IEnumerable<CommandCase> SameText()
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
        yield return new CommandCase("an option before the arguments",
            work => ["--out", Path.Combine(work, "out"), "--pages", "4", "text", Path.Combine(work, "module.pdf")], null, Seed(SilverEggFile));
        yield return new CommandCase("the default output folder", _ => ["all", "module.pdf", "--pages", "1", "--dpi", "30"], null, Seed(SilverEggFile));
        yield return new CommandCase("a relative output folder", _ => ["text", "module.pdf", "--out", "sub/dir"], null, Seed(SilverEggFile));
        yield return new CommandCase("an output folder with a trailing slash", _ => ["text", "module.pdf", "--out", "sub/"], null, Seed(SilverEggFile));
        yield return new CommandCase("a PDF in another folder", _ => ["info", "pdfs/module.pdf"], null, work =>
        {
            Directory.CreateDirectory(Path.Combine(work, "pdfs"));
            File.WriteAllBytes(Path.Combine(work, "pdfs", "module.pdf"), SilverEgg());
        });
        yield return new CommandCase("the arguments after a double dash", _ => ["info", "--", "module.pdf"], null, Seed(SilverEggFile));
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

    // ---------- the cases that are read: layouts PyMuPDF spaced differently (once compared character for character) ----------

    static IEnumerable<CommandCase> SameCharacters()
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

    // ---------- the cases: errors ----------

    /// <summary>Every way a run fails that the script reports, or argparse does: exit code, message, and nothing written.</summary>
    public static IEnumerable<CommandCase> ErrorCases()
    {
        string pdf(string work) => Path.Combine(work, "module.pdf");
        string outFolder(string work) => Path.Combine(work, "out");
        CommandCase Args(string name, Func<string, string[]> args) => new(name, args, null, Seed(SilverEggFile));

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
        yield return new CommandCase("a password-protected PDF, with a bad page range too", w => ["text", Path.Combine(w, "module.pdf"), "--pages", "99"], null, Seed(("module.pdf", Locked())));
    }

    /// <summary>
    /// Runs that fail in dmc and are not compared with the script, because the script crashes with a traceback on them (a
    /// page range that is not a number, a file that is not a PDF) or does something else (a resolution of 0, which it
    /// passes on to PyMuPDF). They are in the one-line guard of <c>ErrorPathTests</c>, with <see cref="ErrorCases"/>.
    /// </summary>
    public static IEnumerable<CommandCase> DmcOnlyErrorCases()
    {
        CommandCase Egg(string name, params string[] args) => new(name, _ => args, null, Seed(SilverEggFile));

        yield return new CommandCase("a file that is not a PDF", _ => ["info", "module.pdf"], null, Seed(("module.pdf", "%PDF-1.7\nthis is not really a pdf\n"u8.ToArray())));
        yield return new CommandCase("a file with nothing in it", _ => ["text", "module.pdf"], null, Seed(("module.pdf", [])));
        yield return new CommandCase("a text file", _ => ["info", "module.pdf"], null, Seed(("module.pdf", "just some words\n"u8.ToArray())));
        yield return Egg("an empty name", "info", "");
        foreach (string spec in new[] { "x", "1-x", "-3", "1--2", "1.5", "1 2", "99999999999999999999" })
            yield return Egg($"pages {spec}", "text", "module.pdf", "--pages", spec);
        foreach (string dpi in new[] { "0", "-5", "1201", "99999999999999999999" })
        {
            yield return Egg($"render at {dpi} dpi", "render", "module.pdf", "--dpi", dpi);
            yield return Egg($"all at {dpi} dpi", "all", "module.pdf", "--dpi", dpi);
        }
    }

    /// <summary>Every input that must be read without an error.</summary>
    public static IEnumerable<CommandCase> Succeeding => [.. SameText(), .. SameCharacters()];
}

/// <summary>
/// The inputs of <see cref="PdfCases"/> that must work, run through the published binary, and the two things of
/// <c>info</c> that only the comparison with PyMuPDF used to pin down: where a page counts as having little text, and
/// how images are counted.
/// </summary>
[TestClass]
public sealed class PdfCaseTests
{
    /// <summary>
    /// Every input the old parity check read is still read: exit 0, nothing on stderr, and the files of its command in
    /// the folder it names (<c>--out DIR</c>, <c>--out DIR/</c>, or <c>extracted/</c> next to the PDF): <c>text/page-NNN.txt</c>
    /// for each page asked for, <c>all-text.txt</c> when that is every page, and a <c>pages/page-NNN.jpg</c> for each page.
    /// </summary>
    [TestMethod]
    public void EveryPdfThatShouldBeReadIsRead() => CommandCase.RunAll(PdfCases.Succeeding, "pdf", (test, result, folder) =>
    {
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stderr);
        Assert.IsGreaterThan(0, result.StdoutLines.Length, result.ToString());
        AssertFilesWritten(test.Args(folder), folder);
    });

    /// <summary>The files a successful run writes, from its arguments read the way <c>dmc pdf</c> reads them.</summary>
    static void AssertFilesWritten(string[] args, string folder)
    {
        var positional = new List<string>();
        var values = new Dictionary<string, string>();
        bool options = true;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (options && arg == "--")
                options = false;
            else if (options && arg.StartsWith("--", StringComparison.Ordinal))
            {
                // Every option of these cases takes a value, written after "=" or as the next argument, and may be shortened.
                string name = arg.Split('=')[0];
                string option = new[] { "--out", "--pages", "--dpi" }.Single(o => o.StartsWith(name, StringComparison.Ordinal));
                values[option] = arg.Contains('=') ? arg[(arg.IndexOf('=') + 1)..] : args[++i];
            }
            else
                positional.Add(arg);
        }
        string command = positional[0], pdf = Path.Combine(folder, positional[1]);
        string output = values.TryGetValue("--out", out string? o) ? Path.Combine(folder, o) : Path.Combine(Path.GetDirectoryName(pdf)!, "extracted");
        if (command == "info")
        {
            Assert.IsFalse(Directory.Exists(output), "info writes nothing");
            return;
        }

        int count;
        using (var document = Dmc.Pdf.PdfFile.Open(pdf))
            count = document.NumberOfPages;
        var pages = Dmc.Pdf.PageRanges.Parse(values.GetValueOrDefault("--pages"), count);
        bool text = command is "text" or "all", pictures = command is "render" or "all";
        foreach (int n in pages)
        {
            // A page with no text has an empty text file, so only the file's existence is checked.
            Assert.AreEqual(text, File.Exists(Path.Combine(output, "text", $"page-{n:000}.txt")), $"text/page-{n:000}.txt in {output}");
            string jpg = Path.Combine(output, "pages", $"page-{n:000}.jpg");
            Assert.AreEqual(pictures, File.Exists(jpg) && new FileInfo(jpg).Length > 0, $"pages/page-{n:000}.jpg in {output}");
        }
        string all = Path.Combine(output, "all-text.txt");
        Assert.AreEqual(text && pages.Count == count, File.Exists(all) && new FileInfo(all).Length > 0, $"all-text.txt in {output}");
    }

    static string Info(byte[] pdf)
    {
        string info = "";
        new CommandCase("info", "info", "module.pdf") { Seed = work => File.WriteAllBytes(Path.Combine(work, "module.pdf"), pdf) }
            .Run("pdf", (result, _) =>
            {
                Assert.AreEqual(0, result.ExitCode, result.ToString());
                info = string.Join("\n", result.StdoutLines);
            });
        return info;
    }

    [TestMethod]
    public void APageUnder200CharactersIsNamedAsHavingLittleText()
    {
        Assert.AreEqual("pages: 1\np1: 200 chars, 0 images, 8.5 x 11.0 in", Info(Build([new Page(PdfCases.Lines(49, 49, 49, 50))])));
        Assert.AreEqual("pages: 1\np1: 199 chars, 0 images, 8.5 x 11.0 in\nlittle or no text (scan, map or art; look at the page image): 1",
            Info(Build([new Page(PdfCases.Lines(49, 49, 49, 49))])));
    }

    /// <summary>
    /// Images are counted as the page's resources list them, drawn or not, the way PyMuPDF's get_images(full=True)
    /// counts: one image under two names is two, an image inside a form counts, a form listed under two names is read
    /// once, and an inline image is not counted.
    /// </summary>
    [TestMethod]
    public void ImagesAreCountedAsThePageListsThem()
    {
        const string drawImage = "q 50 0 0 50 400 400 cm /Im1 Do Q\n";
        string info = Info(Build(
        [
            new Page(Line(72, 72, "drawn twice") + drawImage + drawImage, XObjects: "/Im1 4 0 R"),
            new Page(Line(72, 72, "listed, not drawn"), XObjects: "/Im1 4 0 R"),
            new Page(Line(72, 72, "one image under two names"), XObjects: "/Im1 4 0 R /Im2 4 0 R"),
            new Page(Line(72, 72, "only inside a form") + "q 50 0 0 50 400 400 cm /Fm1 Do Q\n", XObjects: "/Fm1 5 0 R"),
            new Page(Line(72, 72, "one form under two names"), XObjects: "/Fm1 5 0 R /Fm2 5 0 R"),
            new Page(Line(72, 72, "an inline image") + "q 50 0 0 50 400 400 cm BI /W 1 /H 1 /CS /G /BPC 8 ID \x80 EI Q\n"),
        ]));
        int[] counts = info.Split('\n').Where(l => l.StartsWith('p') && l.Contains(" images")).Select(l => int.Parse(l.Split(", ")[1].Split(' ')[0])).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 1, 2, 1, 1, 0 }, counts, info);
    }
}
