using Dmc.Tests.Support;

namespace Dmc.Tests.Sheets;

/// <summary>
/// The inputs of <c>dmc sheet</c> that must fail with one line (feeding the one-line error guard, <c>ErrorPathTests</c>):
/// the usage mistakes and every refusal that is not a list of findings. Each case makes its own DMC folder with the
/// fixture campaign in its working folder and names it with <c>--folder</c>.
/// </summary>
public static class SheetCases
{
    /// <summary>A case run against a DMC folder with the fixture campaign; <c>&lt;work&gt;</c> in an argument is that folder.</summary>
    /// <param name="name">Shown in the test name.</param>
    /// <param name="change">Changes the campaign after it is copied, or null.</param>
    /// <param name="args">The arguments after <c>sheet</c>.</param>
    static CommandCase InFolder(string name, Action<string>? change, params string[] args) =>
        new(name, work => [.. args.Select(a => a.Replace("<work>", work))], null, work =>
        {
            DmcFolders.MakeDmcFolder(work);
            SheetFixtures.CopyCampaign(work);
            change?.Invoke(work);
        });

    /// <summary>The mistakes in the arguments, and every one-line refusal of contracts/dmc-sheet.md § Failures that a case can reach.</summary>
    public static IEnumerable<CommandCase> ErrorCases() =>
    [
        InFolder("an unknown option", null, "--folder", "<work>", "--bogus"),
        InFolder("--party with a name", null, "--folder", "<work>", "--party", "Bram"),
        InFolder("--open with --check", null, "--folder", "<work>", "--open", "--check"),
        InFolder("--campaign with no value", null, "--folder", "<work>", "--campaign"),
        InFolder("--folder with no value", null, "--folder"),
        InFolder("--check given a value", null, "--folder", "<work>", "--check=yes"),
        InFolder("--help given a value", null, "--help=yes"),
        InFolder("an empty name", null, "--folder", "<work>", ""),
        InFolder("a campaign that does not exist", null, "--folder", "<work>", "--campaign", "no-such-campaign-zz9"),
        InFolder("a name with no sheet", null, "--folder", "<work>", "Nobody At All"),
        InFolder("an NPC outside the party", null, "--folder", "<work>", "Hob the miller"),
        InFolder("no player character's sheet", work => File.Delete(SheetFixtures.PathOf(work, "character-sheets/pc-ana.md")), "--folder", "<work>"),
        InFolder("no party block names a sheet", work => File.WriteAllText(SheetFixtures.PathOf(work, "game-state.md"), "# Game state\n\n## Party status\n\n### Shared supplies\n\n- **Rations**: 2\n"),
            "--folder", "<work>", "--party"),
        InFolder("a name that gives two sheets", work => File.WriteAllText(SheetFixtures.PathOf(work, "character-sheets/pc-bram.md"), "# Bram\n"),
            "--folder", "<work>", "Bram"),
        new CommandCase("a folder that is not a DMC folder", work => ["--folder", work], null, _ => { }),
        new CommandCase("no active campaign", work => ["--folder", work], null, work => DmcFolders.MakeDmcFolder(work)),
    ];
}
