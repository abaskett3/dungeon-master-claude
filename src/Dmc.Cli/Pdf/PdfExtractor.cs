using System.Drawing;
using System.Text;
using Dmc.Maps;
using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Dmc.Pdf;

/// <summary>What <c>dmc pdf</c> was asked to do.</summary>
/// <param name="Command">info, text, render or all.</param>
/// <param name="Pdf">The PDF, as typed.</param>
/// <param name="Pages">The <c>--pages</c> text; null for every page.</param>
/// <param name="Dpi">The resolution of page images.</param>
/// <param name="Out">The <c>--out</c> folder; null or empty for "extracted" next to the PDF.</param>
public sealed record PdfRequest(string Command, string Pdf, string? Pages, int Dpi, string? Out);

/// <summary>
/// <c>info</c>, <c>text</c>, <c>render</c> and <c>all</c> of <c>scripts/pdf_extract.py</c>: the same output lines, files
/// and messages. The text comes from PdfPig and the pictures from PDFium (PDFtoImage), so the text is not byte for byte
/// PyMuPDF's: see <see cref="PageText"/>.
/// </summary>
public static class PdfExtractor
{
    /// <summary>A page with less text than this is probably a scan, a map or full-page art.</summary>
    public const int SparseText = 200;

    /// <summary>The quality of the page pictures (JPEG, 0 to 100).</summary>
    public const int JpegQuality = 85;

    /// <summary>The largest <c>--dpi</c> that is accepted: 1200 dpi is already 134 million pixels on a letter page.</summary>
    public const int MaxDpi = 1200;

    /// <summary>UTF-8 with no byte-order mark, for the text files.</summary>
    static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Runs one request. The checks that can fail come first (the file, the PDF, the page list, the resolution, the
    /// pages' sizes), so a failed run writes nothing; then <c>info</c> prints, <c>text</c> writes the text files, and
    /// <c>render</c> writes the pictures (<c>all</c> does both).
    /// </summary>
    /// <param name="request">What to do.</param>
    /// <param name="io">Standard output gets the report lines.</param>
    /// <param name="libsslAvailable">For a test: whether OpenSSL is available. See <see cref="PdfFile.Open"/>.</param>
    /// <returns>Always 0; a failure throws instead.</returns>
    /// <exception cref="DmcException">The file is missing or unreadable, the page list is bad, or the resolution is out of range.</exception>
    public static int Run(PdfRequest request, Io io, Func<bool>? libsslAvailable = null)
    {
        if (!File.Exists(request.Pdf))
            throw new DmcException($"No such file: {request.Pdf}");
        string output = string.IsNullOrEmpty(request.Out)
            ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(request.Pdf)) ?? "", "extracted")
            : request.Out;

        using var document = PdfFile.Open(request.Pdf, libsslAvailable);
        List<int> pages = PageRanges.Parse(request.Pages, document.NumberOfPages);
        bool rendering = request.Command is "render" or "all";
        if (rendering && (request.Dpi < 1 || request.Dpi > MaxDpi))
            throw new DmcException($"--dpi must be a whole number from 1 to {MaxDpi}.");

        // PDFium's page sizes are read before anything is written, so a PDF the two libraries count differently stops
        // here with nothing half done.
        SizeF[]? sizes = rendering ? PageSizes(request.Pdf, pages) : null;

        if (request.Command == "info")
            Info(request.Pdf, document, pages, io);
        if (request.Command is "text" or "all")
            Text(document, pages, output, io);
        if (rendering)
            Render(request, pages, sizes!, output, io);
        return 0;
    }

    /// <summary>
    /// The text of a page, or the script's note when it cannot be read: a bad page does not stop the others. PdfPig reads
    /// a page's content and fonts when it loads the page, so loading it is inside the guard too.
    /// </summary>
    static string TextOf(PdfDocument document, int n) => TryText(document, n, out _);

    /// <summary>The text of page <paramref name="n"/>, as <see cref="TextOf"/> gives it, and the loaded page.</summary>
    /// <param name="document">The PDF.</param>
    /// <param name="n">The page number, from 1.</param>
    /// <param name="page">The page PdfPig loaded; null when loading or reading it failed.</param>
    static string TryText(PdfDocument document, int n, out Page? page)
    {
        page = null;
        try
        {
            page = document.GetPage(n);
            return PageText.Of(page);
        }
        catch (Exception e)
        {
            return $"[text extraction failed: {e.Message.ReplaceLineEndings(" ")}]";
        }
    }

    /// <summary>The size of each page as PDFium sees it, checked to cover <paramref name="pages"/>.</summary>
    static SizeF[] PageSizes(string pdf, List<int> pages)
    {
        using var stream = File.OpenRead(pdf);
        SizeF[] sizes = Conversion.GetPageSizes(stream, leaveOpen: true).ToArray();
        int last = pages.Count > 0 ? pages[^1] : 0;
        if (sizes.Length < last)
            throw new DmcException($"The PDF reader finds {sizes.Length} pages in this PDF where the text reader finds more; page {last} cannot be drawn. The file may be damaged.");
        return sizes;
    }

    /// <summary>
    /// <c>info</c>: the page count, then a line for each page with its characters of text, images and size in inches,
    /// then a list of the pages with less than <see cref="SparseText"/> characters.
    /// </summary>
    /// <param name="pdf">The PDF's path, for PDFium's page sizes when PdfPig cannot load a page.</param>
    /// <param name="document">The PDF.</param>
    /// <param name="pages">The pages to report, from 1.</param>
    /// <param name="io">Standard output.</param>
    static void Info(string pdf, PdfDocument document, List<int> pages, Io io)
    {
        io.Out.WriteLine($"pages: {document.NumberOfPages}");
        var sparse = new List<int>();
        SizeF[]? pdfiumSizes = null;
        foreach (int n in pages)
        {
            int chars = Py.Len(Py.Strip(TryText(document, n, out Page? page)));
            int images;
            double width, height;
            if (page is not null)
            {
                images = ImageCounter.Count(document, page);
                // PdfPig's width and height are of the page as it is shown (the crop box, turned by /Rotate), as PyMuPDF's page.rect is.
                (width, height) = (page.Width, page.Height);
            }
            else
            {
                // PdfPig could not load the page: count from its dictionary, and take the size from PDFium.
                images = ImageCounter.Count(document, n);
                SizeF size = (pdfiumSizes ??= PageSizes(pdf, pages))[n - 1];
                (width, height) = (size.Width, size.Height);
            }
            io.Out.WriteLine($"p{n}: {chars} chars, {images} images, {Py.F1(width / 72)} x {Py.F1(height / 72)} in");
            if (chars < SparseText)
                sparse.Add(n);
        }
        if (sparse.Count > 0)
            io.Out.WriteLine("little or no text (scan, map or art; look at the page image): " + string.Join(", ", sparse));
    }

    /// <summary>
    /// <c>text</c>: one file per page in <c>text/</c>, and, only when every page of the PDF was asked for,
    /// <c>all-text.txt</c> with the pages one after another under "===== PDF PAGE N =====" lines.
    /// </summary>
    /// <param name="document">The PDF.</param>
    /// <param name="pages">The pages to write, from 1.</param>
    /// <param name="output">The output folder, created if needed.</param>
    /// <param name="io">Standard output.</param>
    static void Text(PdfDocument document, List<int> pages, string output, Io io)
    {
        string textFolder = Path.Combine(output, "text");
        Directory.CreateDirectory(textFolder);
        bool whole = pages.Count == document.NumberOfPages;
        var combined = new List<string>();
        foreach (int n in pages)
        {
            string text = TextOf(document, n);
            WriteText(Path.Combine(textFolder, $"page-{n:000}.txt"), text);
            combined.Add($"===== PDF PAGE {n} =====\n{text}");
        }
        string all = Path.Combine(output, "all-text.txt");
        if (whole)
            WriteText(all, string.Join("\n\n", combined));
        io.Out.WriteLine($"text: {pages.Count} pages -> {textFolder}" + (whole ? $" and {all}" : ""));
    }

    /// <summary><c>render</c>: one JPEG per page in <c>pages/</c>, drawn by PDFium at the requested resolution.</summary>
    /// <param name="request">Gives the PDF's path and the resolution.</param>
    /// <param name="pages">The pages to draw, from 1.</param>
    /// <param name="sizes">PDFium's size of every page, in points, from <see cref="PageSizes"/>.</param>
    /// <param name="output">The output folder, created if needed.</param>
    /// <param name="io">Standard output.</param>
    /// <exception cref="DmcException">A page could not be encoded as a JPEG.</exception>
    static void Render(PdfRequest request, List<int> pages, SizeF[] sizes, string output, Io io)
    {
        string pageFolder = Path.Combine(output, "pages");
        Directory.CreateDirectory(pageFolder);
        using var stream = File.OpenRead(request.Pdf);

        // PDFtoImage makes a picture a pixel smaller than PyMuPDF's whenever the size in pixels is not a whole number
        // (an A4 page at 110 dpi: 909 x 1286, where PyMuPDF's is 910 x 1287), so the size is given to it. Pages of one size
        // are drawn in one run, which opens the document once.
        var bySize = pages.GroupBy(n => PixelSize(sizes[n - 1], request.Dpi));
        foreach (var group in bySize)
        {
            // White behind the page, as PyMuPDF's pixmap without alpha; notes and form fields are drawn, as it draws them.
            var options = new RenderOptions(Dpi: request.Dpi, Width: group.Key.Width, Height: group.Key.Height,
                WithAnnotations: true, WithFormFill: true, BackgroundColor: SKColors.White);
            int[] numbers = [.. group];
            // PDFtoImage counts pages from 0.
            int index = 0;
            foreach (var bitmap in Conversion.ToImages(stream, numbers.Select(n => n - 1), leaveOpen: true, options: options))
            {
                using (bitmap)
                using (var image = SKImage.FromBitmap(bitmap))
                using (var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality)
                    // Skia gives null rather than an exception when it cannot encode (a picture too big for it, say), so
                    // look before the file is made: no empty JPEG is left behind.
                    ?? throw new DmcException($"Page {numbers[index]} could not be saved as a JPEG at {request.Dpi} dpi; try a lower --dpi."))
                using (var file = File.Create(Path.Combine(pageFolder, $"page-{numbers[index]:000}.jpg")))
                    jpeg.SaveTo(file);
                index++;
            }
        }
        io.Out.WriteLine($"render: {pages.Count} pages at {request.Dpi} dpi -> {pageFolder}");
    }

    /// <summary>
    /// The size of a page's picture as PyMuPDF makes it: the size in points at <paramref name="dpi"/>, rounded up (MuPDF's
    /// <c>fz_round_rect</c> allows a thousandth of a pixel before it does). Never less than 1 pixel on a side.
    /// </summary>
    /// <param name="points">The page's size in points (1/72 inch).</param>
    /// <param name="dpi">The resolution.</param>
    public static (int Width, int Height) PixelSize(SizeF points, int dpi) =>
        (Math.Max(1, (int)Math.Ceiling(points.Width * dpi / 72.0 - 0.001)),
         Math.Max(1, (int)Math.Ceiling(points.Height * dpi / 72.0 - 0.001)));

    /// <summary>Write the way Python's text mode does: UTF-8 with no byte-order mark, and the OS's line ending.</summary>
    static void WriteText(string path, string text) =>
        File.WriteAllBytes(path, Utf8.GetBytes(text.Replace("\n", Environment.NewLine)));
}
