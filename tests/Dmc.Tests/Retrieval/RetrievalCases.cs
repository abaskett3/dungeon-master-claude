using Dmc.Tests.Support;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// The inputs of <c>dmc lookup</c> and <c>dmc recall</c> that must fail with one line (feeding the one-line error guard,
/// <c>ErrorPathTests</c>). Each case makes its own DMC folder in its working folder and names it with <c>--folder</c>,
/// so no case can reach the developer's real DMC folder through <c>dmc.json</c>.
/// </summary>
public static class RetrievalCases
{
    /// <summary>A case run against a DMC folder holding the fixture chain, made in the working folder; <c>&lt;work&gt;</c> in an argument is that folder.</summary>
    /// <param name="name">Shown in the test name.</param>
    /// <param name="args">The arguments after the command.</param>
    static CommandCase InFolder(string name, params string[] args) =>
        new(name, work => [.. args.Select(a => a.Replace("<work>", work))], null, work =>
        {
            DmcFolders.MakeDmcFolder(work);
            RetrievalFixtures.BuildChain(work);
            RetrievalFixtures.Write(work, $"modules/{RetrievalFixtures.Sequel}/index.md", "# Index\n");
        });

    /// <summary>Every failure of contracts/dmc-lookup.md § Failures that a case can reach, and the argument mistakes.</summary>
    public static IEnumerable<CommandCase> LookupErrors() =>
    [
        InFolder("no heading", "--folder", "<work>", RetrievalFixtures.Module, "zebra"),
        InFolder("a source that is both", "--folder", "<work>", RetrievalFixtures.Sequel, "prices"),
        InFolder("an unknown source", "--folder", "<work>", "nowhere", "prices"),
        InFolder("a staging folder", "--folder", "<work>", ".staging", "prices"),
        InFolder("a file not in the source", "--folder", "<work>", $"{RetrievalFixtures.Predecessor}/story/none.md", "places"),
        InFolder("no rules reference", "--folder", "<work>", "rules", "grappled"),
        InFolder("no arguments", "--folder", "<work>"),
        InFolder("no heading given", "--folder", "<work>", RetrievalFixtures.Module),
        InFolder("an unknown option", "--folder", "<work>", "--bogus", RetrievalFixtures.Module, "eel"),
        InFolder("--folder with no value", RetrievalFixtures.Module, "eel", "--folder"),
        new CommandCase("a folder that is not a DMC folder", work => ["--folder", work, RetrievalFixtures.Module, "eel"], null, _ => { }),
    ];

    /// <summary>Every failure of contracts/dmc-recall.md § Failures that a case can reach, and the argument mistakes.</summary>
    public static IEnumerable<CommandCase> RecallErrors() =>
    [
        InFolder("only common words", "--folder", "<work>", "the", "and", "of"),
        new CommandCase("a staging folder named", work => ["--folder", work, ".staging", "ferry"], null, work =>
        {
            DmcFolders.MakeDmcFolder(work);
            RetrievalFixtures.BuildChain(work);
            RetrievalFixtures.Write(work, "adventures/.staging/game-state.md", "# Game state\n");
        }),
        InFolder("no words", "--folder", "<work>"),
        InFolder("an unknown option", "--folder", "<work>", "--bogus", "ferry"),
        InFolder("--folder with no value", "ferry", "--folder"),
        new CommandCase("no active campaign", work => ["--folder", work, "ferry"], null, work => DmcFolders.MakeDmcFolder(work)),
        new CommandCase("an active campaign that is missing", work => ["--folder", work, "ferry"], null, work =>
        {
            DmcFolders.MakeDmcFolder(work);
            RetrievalFixtures.Write(work, "adventures/.active-campaign", "gone\n");
        }),
        new CommandCase("a folder that is not a DMC folder", work => ["--folder", work, "ferry"], null, _ => { }),
    ];
}
