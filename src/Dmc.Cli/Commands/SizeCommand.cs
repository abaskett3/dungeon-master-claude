using Dmc.Size;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc size [NAME] [--check] [--folder PATH]</c>: what a resume of a campaign reads, in estimated tokens (bytes ÷ 4),
/// with what to trim. It lists the files <c>dmc-resume-campaign</c> step 2 reads, by that step's rules, and the part of
/// each; the full report adds the total, the largest sections and the flags, and <c>--check</c>, the form DMC runs in
/// play, prints only the flags and any problem with the campaign's link lines (spec 011), or nothing when all is well.
/// </summary>
/// <remarks>
/// It never writes, moves or deletes a file. A measured campaign exits 0 whatever the flags, because DMC acts on what it
/// reads and a non-zero exit would show as a tool error (research R7); a failure is one line and exit 1, a usage mistake
/// exit 2. The work is in <c>Dmc.Size</c>, which takes the DMC folder as a parameter; only this class finds it, from
/// <c>--folder</c> or <see cref="DmcFolder.Require"/>.
/// </remarks>
public sealed class SizeCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "size";

    /// <inheritdoc />
    public string Summary => "what a resume reads, in estimated tokens, with what to trim: dmc size [NAME] [--check] [--folder PATH]";

    /// <summary>The text <c>--help</c> prints: usage, the two forms and what the options mean.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc size [NAME] [--check] [--folder PATH]

Measures what a resume of a campaign reads: each file dmc-resume-campaign reads, the part of it the resume reads, and
its size in estimated tokens (bytes / 4), with the total, the largest sections and anything to trim (a file near the
Read tool's cap, over its budget, canon over its limit, leftovers the full save should clear). It changes nothing.

  NAME      a campaign folder in adventures/ (default: the active campaign)
  --check   print only what to act on, one line each, and nothing when all is well (the form DMC runs in play):
            the flags, then any problem with the links of this campaign or an open linked one (a "notice:"
            line each, at most three)
  --folder  the DMC folder (default: the one dmc folder prints)
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments, finds the DMC folder and the campaign, measures it and prints the chosen form.</summary>
    /// <param name="args">The arguments after <c>size</c>.</param>
    /// <param name="io">Standard output gets the report, or the flag lines with <c>--check</c>.</param>
    /// <returns>0 when the campaign was measured or for <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2; every other failure exits 1.</returns>
    public int Run(string[] args, Io io)
    {
        var request = SizeArguments.Parse(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        string folder = DmcFolder.FromOption(request.Folder);
        string campaign = SizeReport.Target(folder, request.Name);
        var result = SizeReport.Measure(folder, campaign);
        var lines = request.Check ? Flags.CheckLines(result.Flags, campaign) : SizeReport.FullReport(result);
        if (request.Check)
            lines.AddRange(LinkLines(folder, campaign));
        foreach (string line in lines)
            io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>
    /// The link notices <c>--check</c> prints after the flags (spec 011 contracts/campaign-links.md): resume and the full
    /// save already run it, so play learns of a broken link with no command of its own. Only the lines of campaigns that
    /// are not closed are named, since a closed campaign's file can no longer be changed. Each line, the
    /// <c>(+N more)</c> line included, ends with the flags' "not the full save's to trim" mark, so the full save leaves
    /// it; a campaign with no problem gets none.
    /// </summary>
    /// <param name="folder">The DMC folder.</param>
    /// <param name="campaign">The campaign measured.</param>
    static List<string> LinkLines(string folder, string campaign) =>
        Dmc.Campaign.CampaignLinks.NoticeLines(
            [.. Dmc.Campaign.CampaignLinks.Build(folder, campaign).Notices.Where(n => !n.InClosed)], "notice: ", " " + Flags.NotTheSaves);
}
