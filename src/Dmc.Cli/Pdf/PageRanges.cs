using System.Numerics;
using Dmc.Maps;

namespace Dmc.Pdf;

/// <summary>The <c>--pages</c> option: <c>parse_pages</c> of <c>scripts/pdf_extract.py</c>.</summary>
public static class PageRanges
{
    /// <summary>Turn "1-20,35" into a sorted list of 1-based page numbers, each once. No spec means every page.</summary>
    /// <exception cref="DmcException">A part that is not a page or range, or is outside 1 to <paramref name="count"/>.</exception>
    public static List<int> Parse(string? spec, int count)
    {
        if (string.IsNullOrEmpty(spec))
            return Enumerable.Range(1, count).ToList();

        var pages = new SortedSet<int>();
        foreach (string raw in spec.Split(','))
        {
            string part = Py.Strip(raw);
            if (part.Length == 0)
                continue;

            // "first-last"; a lone number, or a trailing dash, is that one page.
            int dash = part.IndexOf('-');
            string firstText = dash < 0 ? part : part[..dash];
            string lastText = dash < 0 ? "" : part[(dash + 1)..];
            // The script's int() raises ValueError here, which is a traceback; dmc says it in one line.
            if (!Py.TryParseInt(firstText, out BigInteger first) || (lastText.Length > 0 && !Py.TryParseInt(lastText, out _)))
                throw new DmcException($"Page range {Py.Repr(part)} is not a page number or range.");
            BigInteger last = lastText.Length > 0 ? ParseLast(lastText) : first;

            if (first < 1 || last > count || first > last)
                throw new DmcException($"Page range {Py.Repr(part)} is outside 1-{count}.");
            for (int n = (int)first; n <= (int)last; n++)
                pages.Add(n);
        }
        return [.. pages];
    }

    static BigInteger ParseLast(string text)
    {
        Py.TryParseInt(text, out BigInteger last);
        return last;
    }
}
