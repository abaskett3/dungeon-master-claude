using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokens;

namespace Dmc.Pdf;

/// <summary>
/// The number <c>info</c> prints as "images", counted the way PyMuPDF's <c>len(page.get_images(full=True))</c> counts them
/// (checked against PyMuPDF 1.28.2 on every shape the tests use): one for each entry of type /Image in the page's
/// /Resources /XObject dictionary, drawn or not; the same for the resources of every /Form XObject found there, at any
/// depth, each form read once per page; inline images (BI ... EI in the content) are not counted. The count is by entry,
/// not by image: one image listed under two names, or in two forms, is counted twice.
/// </summary>
public static class ImageCounter
{
    /// <summary>How far up the page tree to look for /Resources the page does not have itself.</summary>
    const int MaxAncestors = 64;

    /// <summary>The count for a page PdfPig loaded.</summary>
    /// <param name="document">The PDF the page belongs to, for following references.</param>
    /// <param name="page">The page.</param>
    public static int Count(PdfDocument document, Page page) => Count(document, page.Dictionary);

    /// <summary>
    /// The count for page <paramref name="number"/> (from 1) when PdfPig cannot load the page itself (a broken font or
    /// content stream): its dictionary is found by walking the page tree. 0 when the tree leads nowhere.
    /// </summary>
    public static int Count(PdfDocument document, int number)
    {
        var root = document.Structure.Catalog.CatalogDictionary;
        return TryGetDictionary(document, root, "Pages", out var pages) && FindPage(document, pages, number, 0) is { } page
            ? Count(document, page)
            : 0;
    }

    /// <summary>The count for a page's dictionary: its resources (its own, or inherited), with a fresh set of forms already visited.</summary>
    static int Count(PdfDocument document, DictionaryToken page)
    {
        var resources = FindResources(document, page);
        var seenForms = new HashSet<IndirectReference>();
        return CountIn(document, resources, seenForms);
    }

    /// <summary>Page <paramref name="number"/> under this page tree node, skipping whole subtrees by their /Count.</summary>
    static DictionaryToken? FindPage(PdfDocument document, DictionaryToken node, int number, int depth)
    {
        if (depth > MaxAncestors || !node.Data.TryGetValue("Kids", out var kidsToken) || Resolve(document, kidsToken) is not ArrayToken kids)
            return null;
        foreach (var kidToken in kids.Data)
        {
            if (Resolve(document, kidToken) is not DictionaryToken kid)
                continue;
            bool isTree = kid.Data.TryGetValue("Kids", out _);
            int pagesHere = !isTree ? 1
                : kid.Data.TryGetValue("Count", out var count) && Resolve(document, count) is NumericToken n ? n.Int : 0;
            if (number <= pagesHere)
                return isTree ? FindPage(document, kid, number, depth + 1) : kid;
            number -= pagesHere;
        }
        return null;
    }

    /// <summary>The images listed in one resource dictionary, plus those in the resources of each form listed there.</summary>
    /// <param name="document">The PDF, for following references.</param>
    /// <param name="resources">A /Resources dictionary; null counts as none.</param>
    /// <param name="seenForms">The forms (by indirect reference) already counted on this page, so a form that refers back to itself or to a sibling is read once.</param>
    static int CountIn(PdfDocument document, DictionaryToken? resources, HashSet<IndirectReference> seenForms)
    {
        if (resources is null || !TryGetDictionary(document, resources, "XObject", out var xobjects))
            return 0;

        int images = 0;
        foreach (var entry in xobjects.Data.Values)
        {
            IndirectReference? reference = entry is IndirectReferenceToken indirect ? indirect.Data : null;
            if (Resolve(document, entry) is not StreamToken stream)
                continue;
            string? subtype = stream.StreamDictionary.Data.TryGetValue("Subtype", out var type) ? (Resolve(document, type) as NameToken)?.Data : null;
            if (subtype == "Image")
            {
                images++;
            }
            else if (subtype == "Form" && (reference is null || seenForms.Add(reference.Value)))
            {
                TryGetDictionary(document, stream.StreamDictionary, "Resources", out var formResources);
                images += CountIn(document, formResources, seenForms);
            }
        }
        return images;
    }

    /// <summary>The page's /Resources, or those of the nearest page tree node above it that has some.</summary>
    static DictionaryToken? FindResources(PdfDocument document, DictionaryToken page)
    {
        var node = page;
        for (int i = 0; i < MaxAncestors; i++)
        {
            if (TryGetDictionary(document, node, "Resources", out var resources))
                return resources;
            if (!TryGetDictionary(document, node, "Parent", out var parent))
                return null;
            node = parent;
        }
        return null;
    }

    /// <summary>The dictionary under <paramref name="key"/> of <paramref name="owner"/>, following a reference; false if there is none or it is not a dictionary.</summary>
    static bool TryGetDictionary(PdfDocument document, DictionaryToken owner, string key, out DictionaryToken dictionary)
    {
        dictionary = null!;
        if (!owner.Data.TryGetValue(key, out var token))
            return false;
        switch (Resolve(document, token))
        {
            case DictionaryToken found:
                dictionary = found;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Follow indirect references to the value: a dictionary, a stream, a name... A reference that leads nowhere gives null.</summary>
    static IToken? Resolve(PdfDocument document, IToken? token)
    {
        for (int i = 0; i < 16 && token is not null; i++)
        {
            switch (token)
            {
                case IndirectReferenceToken indirect:
                    try
                    {
                        token = document.Structure.GetObject(indirect.Data);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    break;
                case ObjectToken obj:
                    token = obj.Data;
                    break;
                default:
                    return token;
            }
        }
        return null;
    }
}
