using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Util;

namespace Dmc.Pdf;

/// <summary>
/// The text of one page, in the order the PDF draws it, as lines. It replaces PyMuPDF's
/// <c>page.get_text("text", sort=True)</c> and is PdfPig's <c>ContentOrderTextExtractor</c> (Apache-2.0,
/// https://github.com/UglyToad/PdfPig, version 0.1.16) with three changes:
/// <list type="number">
/// <item>A line break is also made where the text jumps back to the left, by more than two lines of type. PdfPig
///   only looks at how far the baseline fell, and a table whose rows are filled with dot leaders slopes by a tenth of a
///   point per dot, so the end of one row sat closer to the start of the next than to its own start, and the rows ran
///   together ("svernafter"). The page number and the footer, drawn one after the other in the file, were glued the
///   same way.</item>
/// <item>Lines end with LF on every OS (PdfPig uses <see cref="Environment.NewLine"/>), and the text ends with its last
///   character, with no space or line break after it, as PyMuPDF's does.</item>
/// <item>It always separates paragraphs with a blank line (PdfPig's <c>SeparateParagraphsWithDoubleNewline</c> option, as
///   PyMuPDF separates blocks), and it never replaces tabs with spaces (PdfPig's other options, left at their defaults).</item>
/// </list>
/// Everything else, including how a gap becomes a space, is PdfPig's own, so the characters are the same as its
/// extractor gives.
/// </summary>
public static class PageText
{
    /// <summary>A jump to the left of more than this many point sizes starts a new line.</summary>
    const double BackwardJumpInPointSizes = 2;

    /// <summary>The text of one page: its letters in drawing order, with spaces and line breaks added where PdfPig's rules (and the first change above) say.</summary>
    /// <param name="page">A page PdfPig loaded.</param>
    /// <returns>The text with LF line breaks, and no space or line break at the end.</returns>
    public static string Of(Page page)
    {
        var letters = page.Letters;
        var builder = new StringBuilder();
        Letter? previous = null;
        bool hasJustAddedWhitespace = false;
        for (int i = 0; i < letters.Count; i++)
        {
            var letter = letters[i];
            if (string.IsNullOrEmpty(letter.Value))
                continue;

            if (letter.Value == " " && !hasJustAddedWhitespace)
            {
                if (previous is not null && IsNewline(previous, letter, out _))
                    continue;
                builder.Append(' ');
                previous = letter;
                hasJustAddedWhitespace = true;
                continue;
            }

            hasJustAddedWhitespace = false;

            if (previous is not null && letter.Value != " ")
            {
                var nonWhitespacePrevious = NonWhitespacePrevious(letters, i);
                if (nonWhitespacePrevious is not null && IsNewline(nonWhitespacePrevious, letter, out bool isParagraph))
                {
                    if (previous.Value == " ")
                        builder.Length--;
                    builder.Append('\n');
                    if (isParagraph)
                        builder.Append('\n');
                    hasJustAddedWhitespace = true;
                }
                else if (previous.Value != " ")
                {
                    double gap = letter.StartBaseLine.X - previous.EndBaseLine.X;
                    if (WhitespaceSizeStatistics.IsProbablyWhitespace(gap, previous))
                    {
                        builder.Append(' ');
                        hasJustAddedWhitespace = true;
                    }
                }
            }

            builder.Append(letter.Value);
            previous = letter;
        }

        // As PyMuPDF's text ends: with the last character of the last line, no space and no line break after it.
        int end = builder.Length;
        while (end > 0 && builder[end - 1] is ' ' or '\n')
            end--;
        builder.Length = end;
        return builder.ToString();
    }

    /// <summary>The nearest letter before <paramref name="index"/> that is not blank; null if there is none.</summary>
    static Letter? NonWhitespacePrevious(IReadOnlyList<Letter> letters, int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(letters[i].Value))
                return letters[i];
        }
        return null;
    }

    /// <summary>
    /// Whether a line break belongs between two letters: the baseline moved by nearly a line of type (PdfPig's rule), or
    /// the text jumped back to the left by more than <see cref="BackwardJumpInPointSizes"/> point sizes (the added rule).
    /// </summary>
    /// <param name="previous">The earlier letter.</param>
    /// <param name="letter">The later letter.</param>
    /// <param name="isParagraph">True when the break is also a paragraph break: a drop of more than a line and two thirds, downwards.</param>
    static bool IsNewline(Letter previous, Letter letter, out bool isParagraph)
    {
        isParagraph = false;
        int previousSize = (int)Math.Round(previous.PointSize);
        int size = (int)Math.Round(letter.PointSize);
        int smaller = Math.Min(size, previousSize);

        // PdfPig's own rule: the baseline moved by nearly a line of type.
        double fall = Math.Abs(previous.StartBaseLine.Y - letter.StartBaseLine.Y);
        if (fall > smaller * 0.9)
        {
            // PdfPig's "paragraph" rule: a drop of more than a line and two thirds, downwards, is a blank line.
            isParagraph = fall > smaller * 1.7 && previous.StartBaseLine.Y > letter.StartBaseLine.Y;
            return true;
        }

        // Added: the text went back to the left by far more than a letter could (a new row of a sloping table, a footer
        // drawn after a page number), so it is a new line however little the baseline moved.
        double back = previous.EndBaseLine.X - letter.StartBaseLine.X;
        return back > Math.Max(smaller, 1) * BackwardJumpInPointSizes;
    }
}
