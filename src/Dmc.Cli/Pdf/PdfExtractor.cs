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

    static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

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

        if (request.Command == "info")
            Info(document, pages, io);
        if (request.Command is "text" or "all")
            Text(document, pages, output, io);
        if (rendering)
            Render(request, pages, output, io);
        return 0;
    }

    /// <summary>The text of a page, or the script's note when it cannot be read: a bad page does not stop the others.</summary>
    static string TextOf(Page page)
    {
        try
        {
            return PageText.Of(page);
        }
        catch (Exception e)
        {
            return $"[text extraction failed: {e.Message.ReplaceLineEndings(" ")}]";
        }
    }

    static void Info(PdfDocument document, List<int> pages, Io io)
    {
        io.Out.WriteLine($"pages: {document.NumberOfPages}");
        var sparse = new List<int>();
        foreach (int n in pages)
        {
            var page = document.GetPage(n);
            int chars = Py.Len(Py.Strip(TextOf(page)));
            int images = page.GetImages().Count();
            // PdfPig's width and height are of the page as it is shown (the crop box, turned by /Rotate), as PyMuPDF's page.rect is.
            io.Out.WriteLine($"p{n}: {chars} chars, {images} images, {Py.F1(page.Width / 72)} x {Py.F1(page.Height / 72)} in");
            if (chars < SparseText)
                sparse.Add(n);
        }
        if (sparse.Count > 0)
            io.Out.WriteLine("little or no text (scan, map or art; look at the page image): " + string.Join(", ", sparse));
    }

    static void Text(PdfDocument document, List<int> pages, string output, Io io)
    {
        string textFolder = Path.Combine(output, "text");
        Directory.CreateDirectory(textFolder);
        bool whole = pages.Count == document.NumberOfPages;
        var combined = new List<string>();
        foreach (int n in pages)
        {
            string text = TextOf(document.GetPage(n));
            WriteText(Path.Combine(textFolder, $"page-{n:000}.txt"), text);
            combined.Add($"===== PDF PAGE {n} =====\n{text}");
        }
        string all = Path.Combine(output, "all-text.txt");
        if (whole)
            WriteText(all, string.Join("\n\n", combined));
        io.Out.WriteLine($"text: {pages.Count} pages -> {textFolder}" + (whole ? $" and {all}" : ""));
    }

    static void Render(PdfRequest request, List<int> pages, string output, Io io)
    {
        string pageFolder = Path.Combine(output, "pages");
        Directory.CreateDirectory(pageFolder);
        using var stream = File.OpenRead(request.Pdf);
        var sizes = Conversion.GetPageSizes(stream, leaveOpen: true);

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
                using (var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality))
                using (var file = File.Create(Path.Combine(pageFolder, $"page-{numbers[index]:000}.jpg")))
                    jpeg.SaveTo(file);
                index++;
            }
        }
        io.Out.WriteLine($"render: {pages.Count} pages at {request.Dpi} dpi -> {pageFolder}");
    }

    /// <summary>
    /// The size of a page's picture as PyMuPDF makes it: the size in points at <paramref name="dpi"/>, rounded up (MuPDF's
    /// <c>fz_round_rect</c> allows a thousandth of a pixel before it does).
    /// </summary>
    public static (int Width, int Height) PixelSize(SizeF points, int dpi) =>
        (Math.Max(1, (int)Math.Ceiling(points.Width * dpi / 72.0 - 0.001)),
         Math.Max(1, (int)Math.Ceiling(points.Height * dpi / 72.0 - 0.001)));

    /// <summary>Write the way Python's text mode does: UTF-8 with no byte-order mark, and the OS's line ending.</summary>
    static void WriteText(string path, string text) =>
        File.WriteAllBytes(path, Utf8.GetBytes(text.Replace("\n", Environment.NewLine)));
}
