using Dmc.Retrieval;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc lookup SOURCE HEADING... [--folder PATH]</c>: one section of a campaign file, a module's digest or the rules
/// reference, exactly as written, found by its heading; for a rules PDF, the index rows or the pages to read
/// (contracts/dmc-lookup.md).
/// </summary>
/// <remarks>
/// It never writes, moves or deletes a file. An answer exits 0, a list of matches and a "too large" line included,
/// because DMC acts on what it reads; a failure is one line and exit 1, a usage mistake exit 2. The work is in
/// <see cref="Lookup"/>, which takes the DMC folder as a parameter; only this class finds it.
/// </remarks>
public sealed class LookupCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "lookup";

    /// <inheritdoc />
    public string Summary => "one section of a campaign, module or rules file, by its heading: dmc lookup SOURCE HEADING... [--folder PATH]";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc lookup SOURCE HEADING... [--folder PATH]

Prints one section, from its heading down to the next heading of the same or a higher level, exactly as written. A
heading matches when it equals the request ignoring case, punctuation and a trailing page reference such as (PDF 12),
or else when it holds every word of the request. Several matches print a list; a section too large for one Read prints
its line range. For a rules reference kept as a PDF it prints the matching rows of rules/index.md, or the PDF pages
whose text holds the words. It changes nothing.

  SOURCE    a campaign folder in adventures/, a module folder in modules/, or rules; add /<path> to search one file
            (new-road/story/canon.md), or start with adventures/ or modules/ to say which
  HEADING   the heading's words
  --folder  the DMC folder (default: the one dmc folder prints)
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments, finds the DMC folder, looks the heading up and prints the answer.</summary>
    /// <param name="args">The arguments after <c>lookup</c>.</param>
    /// <param name="io">Standard output gets the answer.</param>
    /// <returns>0 for an answer or <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2; every other failure exits 1.</returns>
    public int Run(string[] args, Io io)
    {
        var request = RetrievalArguments.ParseLookup(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        string folder = Folder(request.Folder);
        foreach (string line in Lookup.Run(folder, request.Source, request.Heading))
            io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>The DMC folder: <c>--folder</c> when given, checked; otherwise the one <see cref="DmcFolder.Require"/> finds.</summary>
    /// <param name="given">The <c>--folder</c> value, or null.</param>
    /// <exception cref="DmcException">The folder given is not a DMC folder, or none is found.</exception>
    internal static string Folder(string? given)
    {
        if (given is null)
            return DmcFolder.Require();
        string folder = Path.GetFullPath(given);
        return DmcFolder.IsDmcFolder(folder) ? folder : throw new DmcException($"{given} is not a DMC folder");
    }
}
