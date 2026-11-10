using Dmc.Tests.Support;

namespace Dmc.Tests.Size;

/// <summary>
/// The inputs of <c>dmc size</c> that must fail with one line (feeding the one-line error guard, <c>ErrorPathTests</c>).
/// Each case makes its own DMC folder in its working folder and names it with <c>--folder</c>, so no case can reach the
/// developer's real DMC folder through <c>dmc.json</c>.
/// </summary>
public static class SizeCases
{
    /// <summary>A case run against a DMC folder with a fixture campaign, made in the working folder; <c>&lt;work&gt;</c> in an argument is that folder.</summary>
    /// <param name="name">Shown in the test name.</param>
    /// <param name="args">The arguments after <c>size</c>.</param>
    static CommandCase InFolder(string name, params string[] args) =>
        new(name, work => [.. args.Select(a => a.Replace("<work>", work))], null, work =>
        {
            DmcFolders.MakeDmcFolder(work);
            SizeFixtures.NewCampaign(work);
        });

    /// <summary>The mistakes in the arguments, and every failure of contracts/dmc-size.md § Failures that a case can reach.</summary>
    public static IEnumerable<CommandCase> ErrorCases() =>
    [
        InFolder("an unknown option", "--folder", "<work>", "--bogus"),
        InFolder("two names", "--folder", "<work>", "a", "b"),
        InFolder("--folder with no value", "--folder"),
        InFolder("--check given a value", "--folder", "<work>", "--check=yes"),
        InFolder("--help given a value", "--help=yes"),
        InFolder("a campaign that does not exist", "--folder", "<work>", "no-such-campaign-zz9"),
        InFolder("a staging folder", "--folder", "<work>", ".staging"),
        InFolder("a name that is not one folder", "--folder", "<work>", "../adventures"),
        new CommandCase("a folder that is not a DMC folder", work => ["--folder", work], null, _ => { }),
        new CommandCase("no active campaign", work => ["--folder", work], null, work => DmcFolders.MakeDmcFolder(work)),
    ];
}
