using System.Text;

namespace Dmc.Size;

/// <summary>One line of a file: its text, without the line end, for matching, and its size on disk, line end included.</summary>
/// <param name="Text">The line decoded as UTF-8, without <c>\n</c> or <c>\r\n</c> (and without a byte-order mark on the first line).</param>
/// <param name="Bytes">The bytes of the line on disk, its line end (CR included) and a byte-order mark included.</param>
internal readonly record struct Line(string Text, int Bytes);

/// <summary>One <c>## </c> section of a part: its heading (without the <c>## </c>) and its size.</summary>
/// <param name="Heading">The heading text, or <c>(top)</c> for the text before the first heading.</param>
/// <param name="Bytes">The bytes of the section, heading line included.</param>
internal sealed record Section(string Heading, long Bytes)
{
    /// <summary>The estimated tokens: bytes ÷ 4, rounded down.</summary>
    public long Tokens => Bytes / 4;
}

/// <summary>How much of a file the resume reads.</summary>
internal enum PartKind
{
    /// <summary>The whole file.</summary>
    Whole,

    /// <summary>A sheet down to its history section, which is its last.</summary>
    ToHistory,

    /// <summary>The session log from the third-last scene to the end.</summary>
    Tail,

    /// <summary>Canon over its limit: the always-read sections only (the search the resume also does is not counted).</summary>
    AlwaysReadSections,

    /// <summary>One section of a module's chapter digest.</summary>
    Section,
}

/// <summary>Whether a file of the read set could be measured.</summary>
internal enum EntryState
{
    /// <summary>The file exists and was measured.</summary>
    Measured,

    /// <summary>The file does not exist (a new campaign); it counts 0.</summary>
    Missing,

    /// <summary>A Party status block names someone with no sheet.</summary>
    NotFound,

    /// <summary>A module file the module's ingest has not written yet.</summary>
    NotIngested,
}

/// <summary>
/// One file of the resume's read set, measured: the part the resume reads and its size, the budget it has, and the lines
/// read, for the flags.
/// </summary>
/// <param name="Path">The path shown in the report: relative to the campaign folder, or to the DMC folder for <c>adventures/player-options.md</c> and module files; <c>/</c> separators.</param>
/// <param name="Kind">How much of the file is read.</param>
/// <param name="State">Whether the file could be measured.</param>
/// <param name="Budget">The file's budget kind.</param>
/// <param name="Lines">The lines read (empty when the file is not measured).</param>
/// <param name="Note">What the report says about the part (for example <c>to its history</c>, <c>whole (fewer than 3 scenes)</c>), or null.</param>
/// <param name="FileLines">Every line of the file, for a check that looks past the part read (canon's sections when it is read in sections); the same as <paramref name="Lines"/> otherwise.</param>
internal sealed record Part(string Path, PartKind Kind, EntryState State, BudgetKind Budget, IReadOnlyList<Line> Lines,
    string? Note, IReadOnlyList<Line> FileLines)
{
    /// <summary>The bytes of the part read.</summary>
    public long Bytes { get; } = Lines.Sum(l => (long)l.Bytes);

    /// <summary>The estimated tokens of the part read: bytes ÷ 4, rounded down.</summary>
    public long Tokens => Bytes / 4;

    /// <summary>The estimated tokens of the whole file.</summary>
    public long FileTokens => FileLines.Sum(l => (long)l.Bytes) / 4;

    /// <summary>The <c>## </c> sections of the part read, in file order.</summary>
    public IReadOnlyList<Section> Sections { get; } = Measurement.Sections(Lines);

    /// <summary>A file of the read set that was not measured: it counts 0.</summary>
    /// <param name="path">The path shown in the report.</param>
    /// <param name="state">Why it was not measured.</param>
    /// <param name="budget">The budget it would have.</param>
    /// <param name="note">What the report says, or null.</param>
    public static Part Absent(string path, EntryState state, BudgetKind budget, string? note = null) =>
        new(path, PartKind.Whole, state, budget, [], note, []);
}

/// <summary>Reading a file as lines with their sizes, cutting it into sections, and writing token counts.</summary>
internal static class Measurement
{
    /// <summary>The name of the text before a part's first <c>## </c> heading.</summary>
    public const string Top = "(top)";

    /// <summary>
    /// Reads a file as bytes and cuts it after each <c>\n</c>, so each line keeps its line end and the sizes add up to the
    /// file's size on disk. A last line with no line end is a line too.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The lines, in order.</returns>
    /// <exception cref="DmcException">The file cannot be read: <c>cannot read &lt;path&gt;: &lt;reason&gt;</c>.</exception>
    public static List<Line> ReadLines(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot read {path}: {e.Message.TrimEnd('.', '\r', '\n')}");
        }
        return Split(bytes);
    }

    /// <summary>Cuts bytes into lines after each <c>\n</c>; see <see cref="ReadLines"/>.</summary>
    /// <param name="bytes">The bytes of a file.</param>
    public static List<Line> Split(byte[] bytes)
    {
        var lines = new List<Line>();
        int start = 0;
        while (start < bytes.Length)
        {
            int end = Array.IndexOf(bytes, (byte)'\n', start);
            int next = end < 0 ? bytes.Length : end + 1;
            int textEnd = end < 0 ? bytes.Length : end;
            if (textEnd > start && bytes[textEnd - 1] == '\r')
                textEnd--;
            string text = Encoding.UTF8.GetString(bytes, start, textEnd - start);
            if (start == 0)
                text = text.TrimStart('﻿');
            lines.Add(new Line(text, next - start));
            start = next;
        }
        return lines;
    }

    /// <summary>
    /// The <c>## </c> sections of some lines, in order: each runs from its heading to the next one. Text before the first
    /// heading is a section named <see cref="Top"/>, when there is any. A <c>### </c> heading does not start a section.
    /// </summary>
    /// <param name="lines">The lines of a part.</param>
    public static List<Section> Sections(IReadOnlyList<Line> lines)
    {
        var sections = new List<Section>();
        string heading = Top;
        long bytes = 0;
        bool any = false;
        foreach (var line in lines)
        {
            if (IsSectionHeading(line.Text))
            {
                if (any)
                    sections.Add(new Section(heading, bytes));
                heading = line.Text[3..].Trim();
                bytes = 0;
            }
            bytes += line.Bytes;
            any = true;
        }
        if (any)
            sections.Add(new Section(heading, bytes));
        return sections;
    }

    /// <summary>
    /// The lines of content: those under a <c>## </c> heading (not the preamble before the first) and outside HTML
    /// comments, where the templates keep their guidance. A comment may span lines; a comment within a line is ignored
    /// with the line kept.
    /// </summary>
    /// <param name="lines">The lines of a part.</param>
    public static IEnumerable<Line> ContentLines(IReadOnlyList<Line> lines)
    {
        bool inSection = false, inComment = false;
        foreach (var line in lines)
        {
            string text = line.Text;
            if (inComment)
            {
                if (text.Contains("-->", StringComparison.Ordinal))
                    inComment = false;
                continue;
            }
            int open = text.IndexOf("<!--", StringComparison.Ordinal);
            if (open >= 0 && text.IndexOf("-->", open, StringComparison.Ordinal) < 0)
            {
                inComment = true;
                continue;
            }
            if (IsSectionHeading(text))
            {
                inSection = true;
                continue;
            }
            if (inSection && !(text.TrimStart().StartsWith("<!--", StringComparison.Ordinal) && text.TrimEnd().EndsWith("-->", StringComparison.Ordinal)))
                yield return line;
        }
    }

    /// <summary>Whether a line is a <c>## </c> heading (exactly two hashes).</summary>
    /// <param name="text">A line's text.</param>
    public static bool IsSectionHeading(string text) => text.StartsWith("## ", StringComparison.Ordinal);

    /// <summary>
    /// The lines of the <c>## </c> section whose heading is <paramref name="heading"/> (compared ignoring case), from its
    /// heading to the next <c>## </c> heading; empty when there is none.
    /// </summary>
    /// <param name="lines">The lines of a file.</param>
    /// <param name="heading">The heading text, without <c>## </c>.</param>
    public static List<Line> SectionLines(IReadOnlyList<Line> lines, string heading)
    {
        var found = new List<Line>();
        bool inside = false;
        foreach (var line in lines)
        {
            if (IsSectionHeading(line.Text))
                inside = string.Equals(line.Text[3..].Trim(), heading, StringComparison.OrdinalIgnoreCase);
            if (inside)
                found.Add(line);
        }
        return found;
    }

    /// <summary>
    /// A token count as the report writes it: whole numbers below 1,000 (<c>812</c>), thousands with one decimal above
    /// (<c>6.6k</c>, <c>20.4k</c>), and no decimal when it is 0 (<c>19k</c>). The decimal is rounded up, so a count just
    /// over a budget is never shown equal to it (4,001 is <c>4.1k</c>, over its <c>4k</c> budget).
    /// </summary>
    /// <param name="tokens">An estimated token count.</param>
    public static string FormatTokens(long tokens)
    {
        if (tokens < 1000)
            return tokens.ToString(System.Globalization.CultureInfo.InvariantCulture);
        long tenths = (tokens + 99) / 100;
        return tenths % 10 == 0
            ? $"{tenths / 10}k"
            : $"{tenths / 10}.{tenths % 10}k";
    }
}
