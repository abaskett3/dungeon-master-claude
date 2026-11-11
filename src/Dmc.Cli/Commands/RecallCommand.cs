using Dmc.Retrieval;
using Dmc.Size;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc recall [NAME] WORDS... [--folder PATH]</c>: the lines that best match some words across a campaign, its
/// Predecessor chain and their modules, best first, within about 1,500 estimated tokens (contracts/dmc-recall.md).
/// </summary>
/// <remarks>
/// It never writes, moves or deletes a file and stores nothing. Hits and "nothing matched" exit 0; a failure is one
/// line and exit 1, a usage mistake exit 2. The work is in <see cref="Recall"/>, which takes the DMC folder as a
/// parameter; only this class finds it and decides the campaign (research R10).
/// </remarks>
public sealed class RecallCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "recall";

    /// <inheritdoc />
    public string Summary => "the lines that best match some words in a campaign and what it continues: dmc recall [NAME] WORDS... [--folder PATH]";

    /// <summary>The text <c>--help</c> prints.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc recall [NAME] WORDS... [--folder PATH]

Searches a campaign's files, the campaigns its Predecessor lines name and their modules, and prints the lines that
best match the words, best first: canon above archives, sheets, other story files and session logs, an earlier
campaign a little lower, a module lowest. Each line shows its campaign, file and line, its session, and DM for a line
only the DM may see; a long line is cut to the part around the match. The output stays within about 1,500 estimated
tokens. import/, exports/ and backups/ are never searched. It changes nothing and stores nothing.

  NAME      a campaign folder in adventures/, when it names one and words follow (default: the active campaign)
  WORDS     what to search for
  --folder  the DMC folder (default: the one dmc folder prints)
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments, finds the DMC folder and the campaign, recalls and prints.</summary>
    /// <param name="args">The arguments after <c>recall</c>.</param>
    /// <param name="io">Standard output gets the hits.</param>
    /// <returns>0 for hits, for "nothing matched" or for <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2; every other failure exits 1.</returns>
    public int Run(string[] args, Io io)
    {
        var request = RetrievalArguments.ParseRecall(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        string folder = LookupCommand.Folder(request.Folder);
        var (campaign, words) = Target(folder, request.Words);
        foreach (string line in Recall.Run(folder, campaign, string.Join(' ', words)))
            io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>
    /// The campaign and the words (research R10): the first argument is the campaign when it names a folder in
    /// <c>adventures/</c> and at least one more argument follows; otherwise every argument is a word, in the active
    /// campaign.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="arguments">The positional arguments.</param>
    /// <exception cref="DmcException">A staging folder is named, there is no active campaign, or the active one is missing.</exception>
    static (string Campaign, IReadOnlyList<string> Words) Target(string dmcFolder, IReadOnlyList<string> arguments)
    {
        string adventures = Path.Combine(dmcFolder, "adventures");
        string first = arguments[0];
        bool plain = first.Length > 0 && first.IndexOfAny(['/', '\\', ':']) < 0 && first is not "." and not "..";
        if (arguments.Count > 1 && plain && Directory.Exists(Path.Combine(adventures, first)))
        {
            if (first.StartsWith('.'))
                throw new DmcException($"{first} is not a campaign (a folder starting with a dot is a staging folder)");
            return (first, arguments.Skip(1).ToList());
        }
        string active = SizeReport.ActiveName(adventures) ?? throw new DmcException("no active campaign: name one (dmc recall NAME WORDS)");
        if (active.StartsWith('.') || active.IndexOfAny(['/', '\\', ':']) >= 0 || !Directory.Exists(Path.Combine(adventures, active)))
            throw new DmcException($"the active campaign {active} is not in adventures/");
        return (active, arguments);
    }
}
