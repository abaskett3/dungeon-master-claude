using System.Globalization;
using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Pdf;

/// <summary>
/// The PDFs the tests start from. The Python tests make theirs with PyMuPDF at test time; here a small writer makes
/// the plain ones (<see cref="Build"/>: pages of text, plus an optional image, rotation or crop box), so no PDF library
/// is needed to make a test file, and two committed files stand in for what a writer here cannot make:
/// <c>locked.pdf</c> (AES-256, user password "mellon") and <c>owner-only.pdf</c> (AES-256, only an owner password and
/// print restrictions, which opens without one). Both were made with PyMuPDF:
/// <c>doc.save(path, encryption=pymupdf.PDF_ENCRYPT_AES_256, user_pw=..., owner_pw=...)</c> on the four-page
/// "Silver Egg" document of <see cref="SilverEgg"/> (plus a coloured rectangle on each page).
/// </summary>
public static class PdfFixtures
{
    public const int Pages = 4;

    /// <summary>The line the Python tests put eight times on page 2.</summary>
    public const string Long = "The goblins of the Silver Egg keep watch from the gallery. ";

    public static string FixturesFolder { get; } = Path.Combine(TestEnv.Root, "tests", "Dmc.Tests", "Pdf", "Fixtures");

    public static byte[] Locked() => File.ReadAllBytes(Path.Combine(FixturesFolder, "locked.pdf"));

    public static byte[] OwnerOnly() => File.ReadAllBytes(Path.Combine(FixturesFolder, "owner-only.pdf"));

    /// <summary>One page: its size in points and what is drawn, as PDF content.</summary>
    /// <param name="Rotate">The page's /Rotate, a multiple of 90.</param>
    /// <param name="CropBox">The visible part as left, bottom, right, top; null for the whole page.</param>
    /// <param name="Image">Draw a small grey image on the page.</param>
    /// <param name="XObjects">
    /// The entries of the page's /XObject resources, written as given, in place of what <paramref name="Image"/> puts
    /// there: <c>4 0 R</c> is the image and <c>5 0 R</c> a form that draws it. Null: as <paramref name="Image"/> says.
    /// </param>
    public sealed record Page(string Content, double Width = 612, double Height = 792, int Rotate = 0,
        double[]? CropBox = null, bool Image = false, string? XObjects = null);

    /// <summary>The same as <c>insert_text((x, y), text)</c> of PyMuPDF: Helvetica 11, with y counted from the top of a page.</summary>
    public static string Line(double x, double yFromTop, string text, double height = 792, double size = 11) =>
        Inv($"BT /F1 {size} Tf {x} {height - yFromTop} Td ({Escape(text)}) Tj ET\n");

    /// <summary>
    /// The four-page document of the Python tests: "Silver Egg page N" at the top of each page, and on page 2 eight more
    /// lines of text.
    /// </summary>
    public static byte[] SilverEgg()
    {
        var pages = new List<Page>();
        for (int n = 1; n <= Pages; n++)
        {
            var content = new StringBuilder(Line(72, 72, $"Silver Egg page {n}"));
            if (n == 2)
            {
                for (int i = 0; i < 8; i++)
                    content.Append(Line(72, 100 + i * 14, Long));
            }
            pages.Add(new Page(content.ToString()));
        }
        return Build(pages);
    }

    /// <summary>
    /// A table the way a module lays out a glossary: each row is "word", dot leaders and "translation" in one string, and
    /// the text slopes down by 0.04 per point across, as it does in the Pinebrook module (page 14): a row ends about five
    /// points below where it began, so the end of one row is closer to the start of the next than to its own start.
    /// </summary>
    public static byte[] DotLeaderTable(string[] rows)
    {
        var content = new StringBuilder();
        for (int i = 0; i < rows.Length; i++)
        {
            // [a b c d e f] Tm with a slope: a rotation of about 2.3 degrees
            content.Append(Inv($"BT /F1 11 Tf 1 -0.04 0.04 1 90 {700 - i * 14} Tm ({Escape(rows[i])}) Tj ET\n"));
        }
        return Build([new Page(content.ToString())]);
    }

    /// <summary>A page number at the right edge, drawn first, then a footer line at the left in smaller type, as a module does.</summary>
    public static byte[] PageNumberThenFooter(string number, string footer) => Build(
    [
        new Page(Line(560, 760, number, size: 10) + Line(36, 758, footer, size: 5)),
    ]);

    /// <summary>A PDF of these pages: Helvetica as /F1 on every page, and the image as /Im1 where asked.</summary>
    public static byte[] Build(IReadOnlyList<Page> pages)
    {
        // Objects: 1 catalog, 2 page tree, 3 font, 4 image, 5 a form that draws the image, then a page and its content
        // for each page.
        const int FirstPage = 6;
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + string.Join(" ", pages.Select((_, i) => $"{FirstPage + 2 * i} 0 R")) + $"] /Count {pages.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /XObject /Subtype /Image /Width 2 /Height 2 /ColorSpace /DeviceGray /BitsPerComponent 8 /Length 4 >>\nstream\n" + "\x20\x80\xC0\xFF" + "\nendstream",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 1 1] /Resources << /XObject << /Im1 4 0 R >> >> /Length 8 >>\nstream\n/Im1 Do\n\nendstream",
        };
        for (int i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            string content = page.Content + (page.Image ? "q 50 0 0 50 400 400 cm /Im1 Do Q\n" : "");
            var entries = new StringBuilder();
            entries.Append(Inv($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {page.Width} {page.Height}] "));
            if (page.CropBox is { } box)
                entries.Append(Inv($"/CropBox [{string.Join(' ', box)}] "));
            if (page.Rotate != 0)
                entries.Append($"/Rotate {page.Rotate} ");
            // PyMuPDF counts the images a page's resources list, drawn or not, so what is listed here is what info counts.
            string? xobjects = page.XObjects ?? (page.Image ? "/Im1 4 0 R" : null);
            string images = xobjects is null ? "" : $" /XObject << {xobjects} >>";
            entries.Append($"/Resources << /Font << /F1 3 0 R >>{images} >> /Contents {FirstPage + 1 + 2 * i} 0 R >>");
            objects.Add(entries.ToString());
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}endstream");
        }

        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Length);
            output.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        int xref = output.Length;
        output.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
            output.Append(offset.ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        output.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        // Every character is below 0x100 and the image bytes are written as such, so Latin-1 gives the file byte for byte.
        return Encoding.Latin1.GetBytes(output.ToString());
    }

    /// <summary>Text with numbers in it, formatted with a decimal point whatever the culture.</summary>
    static string Inv(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    static string Escape(string text) => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
