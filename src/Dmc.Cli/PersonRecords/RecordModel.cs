using System.Text.RegularExpressions;

namespace Dmc.PersonRecords;

/// <summary>The kinds of row (spec 032 FR-001, data-model.md § Row).</summary>
internal static class RowKind
{
    /// <summary>A person or a group says the word for the record's person.</summary>
    public const string Usage = "usage";

    /// <summary>Anyone says the word: a name.</summary>
    public const string Name = "name";

    /// <summary>The word is what the record's person is to the speaker (on Dessa's record, from Tam: <c>mother</c>).</summary>
    public const string Relationship = "relationship";

    /// <summary>Every kind, in the order errors name them.</summary>
    public static readonly string[] All = [Usage, Name, Relationship];
}

/// <summary>The speakers a row's <c>from</c> may name besides a person.</summary>
internal static class Speaker
{
    /// <summary>Anyone at all: the speaker of a <c>name</c> row.</summary>
    public const string Anyone = "anyone";

    /// <summary>The start of a group, <c>group: her crew</c>.</summary>
    public const string GroupPrefix = "group: ";

    /// <summary>Whether a speaker is a group.</summary>
    /// <param name="from">The speaker as stored.</param>
    public static bool IsGroup(string from) => from.StartsWith(GroupPrefix, StringComparison.Ordinal) && from.Length > GroupPrefix.Length;

    /// <summary>A speaker for a line of text: a group without its prefix, anyone and persons as they are.</summary>
    /// <param name="from">The speaker as stored.</param>
    public static string Shown(string from) => IsGroup(from) ? from[GroupPrefix.Length..] : from;
}

/// <summary>
/// One row of a record (data-model.md § Row): who says a word for the record's person, and how. Every field is as the
/// file gives it, null when it is missing or not a string, so <see cref="RecordRules"/> can name what is wrong.
/// </summary>
/// <param name="Kind">The kind (<see cref="RowKind"/>).</param>
/// <param name="From">Who says it: a person's full name, <see cref="Speaker.Anyone"/> or a group.</param>
/// <param name="Word">The word.</param>
/// <param name="When">An optional note on when the word is used.</param>
/// <param name="Never">Who never says it; null when the row has none.</param>
/// <param name="Source">Where it comes from (<see cref="RecordSource"/>).</param>
/// <param name="Problems">What is wrong with the row's shape: an unknown key, a field that is not a string, <c>never</c> not a list of strings.</param>
internal sealed record Row(string? Kind, string? From, string? Word, string? When, IReadOnlyList<string>? Never, string? Source, IReadOnlyList<string> Problems);

/// <summary>One limit of a record: what the person cannot do, or where they differ from the usual.</summary>
/// <param name="Text">The fact.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Problems">What is wrong with the limit's shape.</param>
internal sealed record Limit(string? Text, string? Source, IReadOnlyList<string> Problems);

/// <summary>One person's record (data-model.md § Record), read from <c>records/&lt;slug&gt;.json</c>.</summary>
/// <param name="Campaign">The folder name of the campaign whose <c>records/</c> holds it.</param>
/// <param name="FileName">The file's name in <c>records/</c>.</param>
/// <param name="Person">The person's full name.</param>
/// <param name="Of">The linked earlier campaign the person belongs to, for a record kept in a later one; null otherwise.</param>
/// <param name="Rows">The rows that name this person.</param>
/// <param name="Limits">Their limits.</param>
/// <param name="Problems">What is wrong with the record's shape: an unknown key, <c>rows</c> or <c>limits</c> not a list.</param>
internal sealed record PersonRecord(string Campaign, string FileName, string Person, string? Of, IReadOnlyList<Row> Rows, IReadOnlyList<Limit> Limits, IReadOnlyList<string> Problems)
{
    /// <summary>The file as a message names it: <c>records/&lt;file&gt;</c>.</summary>
    public string Label => $"{RecordFiles.Folder}/{FileName}";
}

/// <summary>One row of the migration's draft (data-model.md § Draft): a row with the person it names and the line it came from.</summary>
/// <param name="To">The person the row names (the record it will be on).</param>
/// <param name="Row">The row; its speaker or source may be missing.</param>
/// <param name="Line">The file and line it came from, <c>character-sheets/npc-x.md:14</c>.</param>
internal sealed record DraftRow(string To, Row Row, string Line);

/// <summary>
/// A row's or limit's source (spec 032 FR-003, research R2): a scene of this campaign (<c>s40</c>, <c>s40.2</c>), a scene of
/// a linked earlier campaign (<c>the-silver-egg s8</c>), the player's words as a quotation (<c>player: "…"</c>), the
/// campaign setup, or DMC's inference.
/// </summary>
internal static partial class RecordSource
{
    /// <summary>The source of what the campaign's setup says.</summary>
    public const string Setup = "setup";

    /// <summary>The source of what DMC inferred (#261).</summary>
    public const string Inference = "inference";

    /// <summary>A scene: an optional campaign folder name, then <c>sN</c> or <c>sN.M</c>.</summary>
    [GeneratedRegex(@"^(?:(?<campaign>[a-z0-9][a-z0-9-]*) )?s(?<session>\d+)(?:\.(?<scene>\d+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex ScenePattern();

    /// <summary>The player's words: <c>player: </c> then a quotation in straight or curly double quotes.</summary>
    [GeneratedRegex("^player: [\"“](?<words>.+)[\"”]$", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex PlayerPattern();

    /// <summary>A scene a source names.</summary>
    /// <param name="Campaign">The linked campaign's folder name; null for this campaign.</param>
    /// <param name="Session">The session number.</param>
    /// <param name="Scene">The scene number; null for a whole session.</param>
    internal readonly record struct SceneRef(string? Campaign, int Session, int? Scene);

    /// <summary>The scene a source names, or null when it names none.</summary>
    /// <param name="source">The source.</param>
    public static SceneRef? Scene(string source)
    {
        var m = ScenePattern().Match(source);
        if (!m.Success || !int.TryParse(m.Groups["session"].Value, out int session))
            return null;
        int? scene = m.Groups["scene"].Success && int.TryParse(m.Groups["scene"].Value, out int s) ? s : null;
        return new SceneRef(m.Groups["campaign"].Success ? m.Groups["campaign"].Value : null, session, scene);
    }

    /// <summary>The player's words a source quotes, or null when it quotes none.</summary>
    /// <param name="source">The source.</param>
    public static string? PlayerWords(string source)
    {
        var m = PlayerPattern().Match(source);
        return m.Success ? m.Groups["words"].Value : null;
    }

    /// <summary>Whether a source has one of the shapes a source may have (not whether its scene exists).</summary>
    /// <param name="source">The source.</param>
    public static bool IsWellFormed(string source) =>
        source is Setup or Inference || Scene(source) is not null || PlayerWords(source) is not null;
}
