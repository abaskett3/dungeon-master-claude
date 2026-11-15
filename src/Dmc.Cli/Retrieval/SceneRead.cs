using System.Text;
using System.Text.RegularExpressions;
using Dmc.Size;

namespace Dmc.Retrieval;

/// <summary>
/// <c>dmc recall NAME --scene</c> (spec 013 FR-007 to FR-009, research R3, contracts/dmc-recall-scene.md): the canon lines
/// the resume reads for the scene it is about to run. The words come from <c>game-state.md</c>, so DMC passes none and
/// <c>dmc size</c> can measure the same read; the lines are canon's outside its always-read sections, ranked as recall
/// ranks them and cut at <see cref="Budgets.SceneReadBytes"/>. Reads only.
/// </summary>
internal static partial class SceneRead
{
    /// <summary>The one line printed for a canon small enough to read whole (under <see cref="Budgets.CanonWholeBelow"/>).</summary>
    public const string SmallLine = "story/canon.md is small: read it whole";

    /// <summary>The line printed when <c>game-state.md</c> gives no word to search for.</summary>
    public const string NothingToSearch = "nothing to search for: game-state.md names no scene, quest or pending line";

    /// <summary>The line printed when no line of the searched sections holds a word of the scene.</summary>
    public const string NothingMatched = "nothing matched: the scene";

    /// <summary>The Scene block's fields whose values are searched; the others (date, conditions) name no subject.</summary>
    static readonly string[] SceneFields = ["Location", "Present", "What is happening", "The player was about to"];

    /// <summary>A list line's leading <c>- **Label**:</c>, removed so the label's words are not searched.</summary>
    [GeneratedRegex(@"^\s*[-*]\s*(\*\*[^*]+\*\*\s*:?)?\s*")]
    private static partial Regex ListLead();

    /// <summary>
    /// The scene read of a campaign: <see cref="SmallLine"/> for a small canon; otherwise the hit lines, best first, within
    /// <see cref="Budgets.SceneReadBytes"/>, then <c>(N more lines matched)</c>; or <see cref="NothingToSearch"/> or
    /// <see cref="NothingMatched"/>.
    /// </summary>
    /// <param name="dmcFolder">The DMC folder.</param>
    /// <param name="campaign">The campaign's folder name, already checked to exist.</param>
    /// <returns>The lines to print.</returns>
    /// <exception cref="DmcException">The campaign has no canon, or a file cannot be read.</exception>
    public static List<string> Run(string dmcFolder, string campaign)
    {
        string root = Path.Combine(dmcFolder, "adventures", campaign);
        string canonPath = Path.Combine(root, "story", "canon.md");
        if (!File.Exists(canonPath))
            throw new DmcException($"no story/canon.md in {campaign}");
        var lines = Measurement.ReadLines(canonPath);
        if (lines.Sum(l => (long)l.Bytes) / 4 < Budgets.CanonWholeBelow)
            return [SmallLine];

        string gameStatePath = Path.Combine(root, "game-state.md");
        var gameState = File.Exists(gameStatePath) ? Measurement.ReadLines(gameStatePath) : [];
        var wanted = Words.Stems(Text(gameState)).ToHashSet(StringComparer.Ordinal);
        if (wanted.Count == 0)
            return [NothingToSearch];

        var ranked = Recall.Rank(Recall.Merge(Units(root, campaign, canonPath, lines)), wanted);
        return ranked.Count == 0 ? [NothingMatched] : Recall.Fit(ranked, wanted, Budgets.SceneReadBytes);
    }

    /// <summary>
    /// Canon's line units outside its always-read sections (<see cref="ReadSet.AlwaysReadMask"/>), labelled with the
    /// campaign as recall labels them. Shared with the hook's reminder, which searches the same lines.
    /// </summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="campaign">The campaign's folder name, the hits' label.</param>
    /// <param name="canonPath">The path of <c>story/canon.md</c>.</param>
    /// <param name="lines">Canon's lines, as <see cref="Measurement.ReadLines"/> split them.</param>
    /// <exception cref="DmcException">Canon cannot be read.</exception>
    internal static List<LineUnit> Units(string root, string campaign, string canonPath, IReadOnlyList<Line> lines)
    {
        MarkdownFile markdown;
        try
        {
            markdown = MarkdownFile.Read(canonPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot read {canonPath}: {e.Message.TrimEnd('.', '\r', '\n')}");
        }
        var always = ReadSet.AlwaysReadMask(lines);
        var file = new ScopeFile(new Place(campaign, root, false, 0), "story/canon.md", Tier.Canon, RecallScope.WeightOf(Tier.Canon));
        // Both split after each "\n", so a unit's line number indexes the mask (a file rewritten between the two reads is
        // the only way they could differ, and then a line past the mask's end counts as searched).
        return LineUnits.Of(file, markdown).Where(u => u.Line > always.Length || !always[u.Line - 1]).ToList();
    }

    /// <summary>
    /// The text the scene's words come from (data-model.md § Scene read): the values of the Scene block's
    /// <see cref="SceneFields"/>, the lines of the <c>### Active</c> quests and of the <c>## Pending</c> section, comments
    /// left out and each line's <c>- **Label**:</c> lead removed.
    /// </summary>
    /// <param name="gameState">The lines of <c>game-state.md</c> (empty when it is missing).</param>
    internal static string Text(IReadOnlyList<Line> gameState)
    {
        var text = new StringBuilder();
        foreach (string line in ReadSet.OutsideComments(Measurement.SectionLines(gameState, "Scene")))
        {
            if (SceneFields.Any(f => line.Contains($"**{f}**", StringComparison.OrdinalIgnoreCase)))
                text.AppendLine(ListLead().Replace(line, ""));
        }
        bool active = false;
        foreach (string line in ReadSet.OutsideComments(Measurement.SectionLines(gameState, "Quests")))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                active = line[4..].Trim().Equals("Active", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (active)
                text.AppendLine(ListLead().Replace(line, ""));
        }
        foreach (string line in ReadSet.OutsideComments(Measurement.SectionLines(gameState, "Pending")))
        {
            if (!line.StartsWith("## ", StringComparison.Ordinal))
                text.AppendLine(ListLead().Replace(line, ""));
        }
        return text.ToString();
    }
}
