using System.Globalization;
using System.Text;

namespace Dmc.Campaign;

/// <summary>How a unit that is not found in the migrated campaign is accounted for.</summary>
internal enum Disposition
{
    /// <summary>The unit was reworded: its anchor must occur in the migrated campaign.</summary>
    Reworded,

    /// <summary>The unit was dropped under a class the pending migrations declare.</summary>
    Dropped,
}

/// <summary>One accounted unit.</summary>
/// <param name="Line">Its line number in the accounting file.</param>
/// <param name="Id">The unit's 12-digit id, lower case.</param>
/// <param name="Disposition">Reworded or dropped.</param>
/// <param name="Class">The drop class; null for a rewording.</param>
/// <param name="Detail">The anchor of a rewording, or the reason for a drop.</param>
internal sealed record AccountingEntry(int Line, string Id, Disposition Disposition, string? Class, string Detail);

/// <summary>A file DMC left unmigrated (FR-010).</summary>
/// <param name="Line">Its line number in the accounting file.</param>
/// <param name="File">The campaign path of the file.</param>
/// <param name="Reason">Why it was left.</param>
internal sealed record SkippedFile(int Line, string File, string Reason);

/// <summary>
/// The accounting file <c>&lt;backup file&gt;.accounting.tsv</c> (data-model.md § Accounting file) that DMC writes during a
/// migration: <c>&lt;id&gt;\t&lt;disposition&gt;\t&lt;detail&gt;</c> per unit, <c>skipped\t&lt;file&gt;\t&lt;reason&gt;</c> per file
/// left alone, lines starting <c>#</c> and blank lines ignored.
/// </summary>
/// <param name="Entries">The accounted units, the first line for each id.</param>
/// <param name="Skipped">The files left unmigrated.</param>
internal sealed record AccountingFile(IReadOnlyDictionary<string, AccountingEntry> Entries, IReadOnlyList<SkippedFile> Skipped)
{
    /// <summary>The shortest anchor a rewording may give.</summary>
    public const int MinAnchor = 8;

    /// <summary>The disposition of a file line.</summary>
    const string SkippedWord = "skipped";

    /// <summary>The disposition prefix of a drop.</summary>
    const string DroppedPrefix = "dropped:";

    /// <summary>An accounting file with nothing in it, as a missing file counts.</summary>
    public static AccountingFile Empty { get; } = new(new Dictionary<string, AccountingEntry>(), []);

    /// <summary>Reads an accounting file; a missing file is <see cref="Empty"/>.</summary>
    /// <param name="path">The file.</param>
    /// <param name="label">How the messages name it.</param>
    /// <exception cref="DmcException">A line is malformed; the message names its number.</exception>
    public static AccountingFile Read(string path, string label) =>
        File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8), label) : Empty;

    /// <summary>Reads the text of an accounting file.</summary>
    /// <param name="text">The text.</param>
    /// <param name="label">How the messages name the file.</param>
    /// <exception cref="DmcException">
    /// A line has fewer than three tab-separated fields; an id that is not 12 hex digits; a disposition other than
    /// <c>reworded</c> or <c>dropped:&lt;class&gt;</c>; a class that is not lowercase words joined by hyphens; an anchor
    /// shorter than 8 characters; an empty reason; or a skipped path that is not a campaign path (absolute, with <c>..</c>,
    /// or under <c>backups/</c>).
    /// </exception>
    public static AccountingFile Parse(string text, string label)
    {
        var entries = new Dictionary<string, AccountingEntry>(StringComparer.Ordinal);
        var skipped = new List<SkippedFile>();
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i].TrimEnd('\r');
            if (i == 0)
                line = line.TrimStart('﻿');
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
                continue;
            string[] fields = line.Split('\t', 3);
            if (fields.Length < 3)
                throw Error(label, number, "expected three fields separated by tabs: id, disposition, detail");
            string first = fields[0].Trim(), second = fields[1].Trim(), detail = fields[2].Trim();

            if (first == SkippedWord)
            {
                string file = second.Replace('\\', '/');
                if (ArchivePaths.Refusal(file) is { } why)
                    throw Error(label, number, $"skipped file {second} is not a campaign path ({why})");
                if (!CampaignFiles.Counts(file))
                    throw Error(label, number, $"skipped file {second} is not a campaign file");
                if (detail.Length == 0)
                    throw Error(label, number, "a skipped file needs a reason");
                skipped.Add(new SkippedFile(number, file, detail));
                continue;
            }

            string id = first.ToLowerInvariant();
            if (id.Length != ContentUnits.IdLength || !id.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')))
                throw Error(label, number, $"{first} is not an id of {ContentUnits.IdLength} hex digits (take it from dmc campaign verify --list)");
            AccountingEntry entry;
            if (second == "reworded")
            {
                if (detail.Length < MinAnchor)
                    throw Error(label, number, string.Create(CultureInfo.InvariantCulture, $"a reworded unit needs an anchor of at least {MinAnchor} characters"));
                entry = new AccountingEntry(number, id, Disposition.Reworded, null, detail);
            }
            else if (second.StartsWith(DroppedPrefix, StringComparison.Ordinal))
            {
                string cls = second[DroppedPrefix.Length..];
                if (!MigrationFile.IsClassName(cls))
                    throw Error(label, number, $"{cls} is not a drop class (lowercase words joined by hyphens)");
                if (detail.Length == 0)
                    throw Error(label, number, "a dropped unit needs a reason");
                entry = new AccountingEntry(number, id, Disposition.Dropped, cls, detail);
            }
            else
            {
                throw Error(label, number, $"{second} is not a disposition: use reworded or dropped:<class>");
            }
            entries.TryAdd(id, entry);
        }
        return new AccountingFile(entries, skipped);
    }

    /// <summary>The one-line error for a bad line.</summary>
    /// <param name="label">The file.</param>
    /// <param name="line">The line number.</param>
    /// <param name="message">What is wrong.</param>
    static DmcException Error(string label, int line, string message) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{label} line {line}: {message}"));
}
