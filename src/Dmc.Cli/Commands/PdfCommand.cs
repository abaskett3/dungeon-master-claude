using System.Numerics;
using Dmc.Maps;
using Dmc.Pdf;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc pdf info|text|render|all &lt;pdf&gt; [--pages 1-20,35] [--dpi 110] [--out DIR]</c>: replaces
/// <c>scripts/pdf_extract.py</c>. It writes the text of each page and a picture of each page to disk, so that DMC can
/// read them (the Read tool cannot open very large PDFs).
/// </summary>
/// <remarks>
/// Arguments are read the way Python's <c>argparse</c> reads them (an option may be shortened to a unique prefix, and
/// <c>--option=value</c> is understood), with its messages, but a mistake is one line and exit code 2, never the usage text.
/// </remarks>
public sealed class PdfCommand : ICommand
{
    public string Name => "pdf";

    public string Summary => "text and page pictures of a PDF: dmc pdf info|text|render|all <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]";

    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc pdf info   <pdf> [--pages 1-20,35]
       dmc pdf text   <pdf> [--pages 1-20,35] [--out DIR]
       dmc pdf render <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]
       dmc pdf all    <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]

Extracts text and page pictures from a PDF so DMC can read them. Output (default DIR is "extracted" next to the PDF):
  DIR/all-text.txt         every page's text, split by "===== PDF PAGE N =====" lines (only when every page is done)
  DIR/text/page-NNN.txt    one file per page
  DIR/pages/page-NNN.jpg   one picture per page
Page numbers are PDF page numbers, starting at 1.

  --pages  PDF pages, for example "1-20,35" (default: all)
  --dpi    resolution of page pictures, 1 to 1200 (default 110)
  --out    output folder
""".Replace("\r\n", "\n");

    static readonly string[] Options = ["--help", "--pages", "--dpi", "--out"];

    static readonly string[] Commands = ["info", "text", "render", "all"];

    public int Run(string[] args, Io io)
    {
        var request = Parse(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        return PdfExtractor.Run(request, io);
    }

    /// <summary>Read the arguments as <c>argparse</c> does, in order, with its messages. Null means "--help".</summary>
    static PdfRequest? Parse(string[] args)
    {
        string? command = null, pdf = null, pages = null, output = null;
        BigInteger dpi = 110;
        var extra = new List<string>();

        var kinds = Argparse.ClassifyAll(args, Options);

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (kinds[i] is not { } kind)
                continue;
            if (!kind.IsOption)
            {
                if (command is null)
                {
                    if (!Commands.Contains(arg))
                        // Python 3.12.something and later list the choices bare; 3.11 and the first 3.12s put each in quotes.
                        throw Argparse.Usage($"argument command: invalid choice: {Py.Repr(arg)} (choose from {string.Join(", ", Commands)})");
                    command = arg;
                }
                else if (pdf is null)
                {
                    pdf = arg;
                }
                else
                {
                    extra.Add(arg);
                }
                continue;
            }
            if (kind.Option is null)
            {
                extra.Add(arg); // an option that does not exist: reported at the end, after the required arguments
                continue;
            }

            if (kind.Option == "--help")
            {
                if (kind.Explicit is not null)
                    throw Argparse.Usage($"argument -h/--help: ignored explicit argument {Py.Repr(kind.Explicit)}");
                return null;
            }

            // The other options take one value: written after "=", or the next argument if that is not an option.
            string value;
            if (kind.Explicit is not null)
                value = kind.Explicit;
            else if (i + 1 < args.Length && kinds[i + 1] is { IsOption: false })
                value = args[++i];
            else
                throw Argparse.Usage($"argument {kind.Option}: expected one argument");

            switch (kind.Option)
            {
                case "--pages":
                    pages = value;
                    break;
                case "--out":
                    output = value;
                    break;
                case "--dpi":
                    if (!Py.TryParseInt(value, out dpi))
                        throw Argparse.Usage($"argument --dpi: invalid int value: {Py.Repr(value)}");
                    break;
            }
        }

        var missing = new List<string>();
        if (command is null)
            missing.Add("command");
        if (pdf is null)
            missing.Add("pdf");
        if (missing.Count > 0)
            throw Argparse.Usage("the following arguments are required: " + string.Join(", ", missing));
        if (extra.Count > 0)
            throw Argparse.Usage("unrecognized arguments: " + string.Join(' ', extra));
        // Anything outside 1 to the largest dpi is refused when pictures are made; this keeps a huge number in range.
        int clamped = dpi < 0 ? 0 : dpi > PdfExtractor.MaxDpi + 1 ? PdfExtractor.MaxDpi + 1 : (int)dpi;
        return new PdfRequest(command!, pdf!, pages, clamped, output);
    }
}
