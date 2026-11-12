using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Retrieval;

/// <summary>One line recall can rank (research R3; data-model.md § Line unit).</summary>
/// <param name="File">The file it is in.</param>
/// <param name="Line">Its 1-based line number.</param>
/// <param name="Text">The line, trimmed of surrounding spaces.</param>
/// <param name="Stems">Its stems, repeats kept.</param>
/// <param name="Session">The session it belongs to (<c>sN.M</c> for a session log's scene) or cites; null when neither.</param>
/// <param name="DmOnly">True for a line only the DM may see.</param>
internal sealed record LineUnit(ScopeFile File, int Line, string Text, List<string> Stems, string? Session, bool DmOnly);

/// <summary>Turns the files of recall's scope into line units.</summary>
internal static partial class LineUnits
{
    /// <summary>The prototype's floor: a line shorter than this carries no fact on its own and is not ranked.</summary>
    const int ShortestUnit = 25;

    /// <summary>
    /// The units of one file. Every line is DM-only in a module, the DM notes, module progress and a digest's secrets
    /// (<c>digests/&lt;campaign&gt;/secrets*.md</c>, spec 011 FR-012); under a Secrets heading of an NPC sheet; and
    /// under a digest's <c>## Module</c> heading, which copies module progress.
    /// </summary>
    /// <param name="file">The file's place, path and weight.</param>
    /// <param name="markdown">The file, read.</param>
    /// <returns>Its units, in line order.</returns>
    public static List<LineUnit> Of(ScopeFile file, MarkdownFile markdown)
    {
        var units = new List<LineUnit>();
        string relative = file.Relative.ToLowerInvariant();
        bool dmFile = file.Place.IsModule || relative is "story/dm-notes.md" or "story/dm-notes-archive.md" or "module-progress.md"
            || (file.Tier == Tier.Digest && Path.GetFileName(relative).StartsWith("secrets", StringComparison.Ordinal));
        bool npcSheet = relative.StartsWith("character-sheets/npc-", StringComparison.Ordinal);
        var sessionFile = file.Tier == Tier.Session ? SessionFileName().Match(Path.GetFileName(relative)) : Match.Empty;
        string? session = sessionFile.Success ? $"s{int.Parse(sessionFile.Groups[1].Value, CultureInfo.InvariantCulture)}" : null;
        string? scene = session;
        // The headings above the current line, by level, so a line under ## Secrets (or below it) is known.
        var open = new string?[7];

        for (int i = 0; i < markdown.Lines.Length; i++)
        {
            if (markdown.Fenced[i])
                continue;
            // The text outside HTML comments: a template's guidance and examples are not facts of the campaign.
            string text = markdown.Visible[i].Trim();
            if (MarkdownHeadings.Parse(markdown.Visible[i]) is { } heading)
            {
                open[heading.Level] = heading.Text;
                for (int level = heading.Level + 1; level < open.Length; level++)
                    open[level] = null;
                if (session is not null && SceneHeading().Match(heading.Text) is { Success: true } m)
                    scene = $"{session}.{int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)}";
                continue;
            }
            if (text.Length < ShortestUnit || text.StartsWith("|--", StringComparison.Ordinal) || TableSeparator().IsMatch(text))
                continue;

            string? cited = session is not null ? scene : Citation().Match(text) is { Success: true } c ? c.Value : null;
            bool secret = (npcSheet && open.Any(h => h is not null && h.Contains("secret", StringComparison.OrdinalIgnoreCase)))
                || (file.Tier == Tier.Digest && open[2] is { } section && section.Equals("Module", StringComparison.OrdinalIgnoreCase));
            units.Add(new LineUnit(file, i + 1, text, Words.Stems(text), cited, dmFile || secret));
        }
        return units;
    }

    /// <summary>A session log's file name, <c>session-NNN.md</c>; group 1 is the session number.</summary>
    [GeneratedRegex(@"^session-(\d+)\.md$")]
    private static partial Regex SessionFileName();

    /// <summary>A scene heading's text, <c>Scene M: …</c>; group 1 is the scene number.</summary>
    [GeneratedRegex(@"^Scene\s+(\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SceneHeading();

    /// <summary>A session citation in a line, <c>s12</c> or <c>s12.3</c>.</summary>
    [GeneratedRegex(@"\bs\d+(?:\.\d+)?\b")]
    private static partial Regex Citation();

    /// <summary>A markdown table's separator row, such as <c>| --- | :---: |</c>.</summary>
    [GeneratedRegex(@"^\|[\s:|\-]+$")]
    private static partial Regex TableSeparator();
}
