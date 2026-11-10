using System.Globalization;
using System.Text.RegularExpressions;

namespace Dmc.Campaign;

/// <summary>
/// One migration, <c>migrations/NNN-&lt;slug&gt;.md</c>, as <c>dmc</c> reads it (contracts/migration-file.md): the front matter
/// and the body's headings. The prose is DMC's to follow; <c>dmc</c> reads only what <c>verify</c> needs (the drops and the
/// checks) and what the build checks (the headings and the declared drops).
/// </summary>
/// <param name="FileName">The file's name, <c>NNN-slug.md</c>.</param>
/// <param name="Standard">The standard the migration brings a campaign to; equal to the name's number.</param>
/// <param name="Title">One line for the player.</param>
/// <param name="Shipped">The PR that changed the standard.</param>
/// <param name="Touches">The campaign files the migration may change, relative to the campaign.</param>
/// <param name="Drops">The classes of content the migration may drop.</param>
/// <param name="Checks">The built-in checks <c>verify</c> runs for it.</param>
/// <param name="Headings">The body's <c>## </c> headings, in order, without the <c>## </c>.</param>
/// <param name="DeclaredDrops">The non-blank lines under <c>## Declared drops</c>.</param>
internal sealed partial record MigrationFile(string FileName, int Standard, string Title, string Shipped, IReadOnlyList<string> Touches,
    IReadOnlyList<string> Drops, IReadOnlyList<string> Checks, IReadOnlyList<string> Headings, IReadOnlyList<string> DeclaredDrops)
{
    /// <summary>The five body headings every migration has, in this order.</summary>
    public static readonly string[] RequiredHeadings = ["What changes", "Steps", "Judgments", "Declared drops", "Done when"];

    /// <summary>The front-matter keys, the only ones allowed.</summary>
    static readonly string[] Keys = ["standard", "title", "shipped", "touches", "drops", "checks"];

    /// <summary>The keys whose value is a list.</summary>
    static readonly string[] ListKeys = ["touches", "drops", "checks"];

    /// <summary>A migration's file name: three digits, a hyphen, a lowercase slug, <c>.md</c>.</summary>
    [GeneratedRegex(@"^(\d{3})-[a-z0-9]+(?:-[a-z0-9]+)*\.md$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern { get; }

    /// <summary>A drop class or a check name: lowercase words (letters and digits) joined by hyphens.</summary>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ClassPattern { get; }

    /// <summary>True for a file name of the migration form; other files in <c>migrations/</c> are not migrations.</summary>
    /// <param name="fileName">A file name.</param>
    public static bool IsMigrationName(string fileName) => NamePattern.IsMatch(fileName);

    /// <summary>True for a drop class or check name: lowercase words joined by hyphens.</summary>
    /// <param name="name">The name.</param>
    public static bool IsClassName(string name) => ClassPattern.IsMatch(name);

    /// <summary>Reads a migration file.</summary>
    /// <param name="fileName">Its name, <c>NNN-slug.md</c>.</param>
    /// <param name="text">Its text.</param>
    /// <returns>The migration.</returns>
    /// <exception cref="DmcException">
    /// The name is not of the migration form; the front matter has no opening or closing <c>---</c>, a line that is not
    /// <c>key: value</c>, an unknown or duplicate key, a list that is not <c>[a, b]</c>, a missing key, a <c>standard</c> that
    /// is not the name's number, or a drop class or check that is not lowercase words joined by hyphens; or <c>checks</c> is
    /// empty, or a body heading is missing or out of order, or a drop class has no line under <c>## Declared drops</c>. The
    /// message names the file and, where there is one, the line.
    /// </exception>
    public static MigrationFile Parse(string fileName, string text)
    {
        var match = NamePattern.Match(fileName);
        if (!match.Success)
            throw Error(fileName, null, "the name must be NNN-slug.md (three digits, then lowercase words joined by hyphens)");
        int number = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
            throw Error(fileName, 1, "the front matter must start with ---");
        int close = Array.FindIndex(lines, 1, l => l.Trim() == "---");
        if (close < 0)
            throw Error(fileName, null, "the front matter has no closing ---");

        var values = new Dictionary<string, (int Line, string Value)>(StringComparer.Ordinal);
        for (int i = 1; i < close; i++)
        {
            string line = lines[i];
            if (line.Trim().Length == 0)
                continue;
            int colon = line.IndexOf(':');
            if (colon <= 0)
                throw Error(fileName, i + 1, "expected key: value");
            string key = line[..colon].Trim();
            if (!Keys.Contains(key))
                throw Error(fileName, i + 1, $"unknown key {key}");
            if (!values.TryAdd(key, (i + 1, line[(colon + 1)..].Trim())))
                throw Error(fileName, i + 1, $"duplicate key {key}");
        }

        foreach (string key in Keys.Where(k => k != "drops"))
        {
            if (!values.ContainsKey(key))
                throw Error(fileName, null, $"the front matter has no {key}");
        }

        var (standardLine, standardText) = values["standard"];
        if (!int.TryParse(standardText, NumberStyles.None, CultureInfo.InvariantCulture, out int standard))
            throw Error(fileName, standardLine, "standard must be a whole number");
        if (standard != number)
            throw Error(fileName, standardLine, $"standard {standard} differs from the name's number {number}");

        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string key in ListKeys)
        {
            if (!values.TryGetValue(key, out var entry))
            {
                lists[key] = [];
                continue;
            }
            lists[key] = ParseList(fileName, entry.Line, key, entry.Value);
            if (key != "touches")
            {
                foreach (string item in lists[key].Where(item => !IsClassName(item)))
                    throw Error(fileName, entry.Line, $"{item} in {key} is not lowercase words joined by hyphens");
            }
        }
        if (lists["checks"].Count == 0)
            throw Error(fileName, values["checks"].Line, "checks must name at least one check");

        var (headings, declared) = ReadBody(lines, close + 1);
        CheckHeadings(fileName, headings);
        foreach (string drop in lists["drops"])
        {
            if (!declared.Any(l => l.Contains(drop, StringComparison.Ordinal)))
                throw Error(fileName, values["drops"].Line, $"drop class {drop} has no line under ## Declared drops");
        }

        return new MigrationFile(fileName, standard, values["title"].Value, values["shipped"].Value, lists["touches"],
            lists["drops"], lists["checks"], headings, declared);
    }

    /// <summary>Reads a <c>[a, b]</c> list; <c>[]</c> is empty.</summary>
    /// <param name="fileName">The file, for the message.</param>
    /// <param name="line">The line number, for the message.</param>
    /// <param name="key">The key, for the message.</param>
    /// <param name="value">The text after the colon.</param>
    static List<string> ParseList(string fileName, int line, string key, string value)
    {
        if (value.Length < 2 || value[0] != '[' || value[^1] != ']')
            throw Error(fileName, line, $"{key} must be a list: [a, b]");
        string inside = value[1..^1].Trim();
        if (inside.Length == 0)
            return [];
        var items = inside.Split(',').Select(s => s.Trim()).ToList();
        if (items.Any(s => s.Length == 0))
            throw Error(fileName, line, $"{key} has an empty item");
        return items;
    }

    /// <summary>The body's <c>## </c> headings and the non-blank lines under <c>## Declared drops</c>.</summary>
    /// <param name="lines">The file's lines.</param>
    /// <param name="start">The first line after the front matter.</param>
    static (List<string> Headings, List<string> Declared) ReadBody(string[] lines, int start)
    {
        var headings = new List<string>();
        var declared = new List<string>();
        bool inDeclared = false;
        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                string heading = line[3..].Trim();
                headings.Add(heading);
                inDeclared = heading == "Declared drops";
                continue;
            }
            if (line.StartsWith("# ", StringComparison.Ordinal))
                inDeclared = false;
            else if (inDeclared && line.Trim().Length > 0)
                declared.Add(line.Trim());
        }
        return (headings, declared);
    }

    /// <summary>Requires the five headings of <see cref="RequiredHeadings"/>, each once, in that order (other headings may sit between them).</summary>
    /// <param name="fileName">The file, for the message.</param>
    /// <param name="headings">The body's headings.</param>
    static void CheckHeadings(string fileName, List<string> headings)
    {
        int last = -1;
        foreach (string required in RequiredHeadings)
        {
            int at = headings.IndexOf(required);
            if (at < 0)
                throw Error(fileName, null, $"the body has no ## {required}");
            if (at < last)
                throw Error(fileName, null, $"## {required} is out of order: the headings are {string.Join(", ", RequiredHeadings)}");
            last = at;
        }
    }

    /// <summary>The one-line error for a migration file.</summary>
    /// <param name="fileName">The file.</param>
    /// <param name="line">The line number, or null when the error is about the whole file.</param>
    /// <param name="message">What is wrong.</param>
    static DmcException Error(string fileName, int? line, string message) =>
        new(line is null ? $"migrations/{fileName}: {message}" : $"migrations/{fileName} line {line}: {message}");
}
