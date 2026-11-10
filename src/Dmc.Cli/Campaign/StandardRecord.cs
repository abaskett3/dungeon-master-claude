using System.Globalization;
using System.Text;

namespace Dmc.Campaign;

/// <summary>Where a campaign stands against the kit (data-model.md § Standard).</summary>
internal enum StandardState
{
    /// <summary>The campaign's standard equals the kit's.</summary>
    Current,

    /// <summary>The campaign's standard is below the kit's: migrations are pending.</summary>
    Behind,

    /// <summary>The campaign's standard is above the kit's: this <c>dmc</c> is older than the one that wrote it.</summary>
    Ahead,

    /// <summary>A backup of the campaign has a <c>.migrating</c> mark: a migration was started and not finished or restored.</summary>
    Unfinished,
}

/// <summary>A campaign's standard, the kit's, and what that makes the campaign.</summary>
/// <param name="State">Current, behind, ahead or unfinished.</param>
/// <param name="Campaign">The campaign's standard (1 when it has no record).</param>
/// <param name="Kit">The kit's standard: the highest migration in <c>migrations/</c>, 1 with none.</param>
/// <param name="Pending">The migrations above the campaign's standard, oldest first; empty unless behind.</param>
/// <param name="Marked">The backup file name with the <c>.migrating</c> mark; null unless unfinished.</param>
internal sealed record StandardStatus(StandardState State, int Campaign, int Kit, IReadOnlyList<MigrationFile> Pending, string? Marked)
{
    /// <summary>
    /// The one line <c>dmc campaign standard</c> prints for this status (contracts/campaign-commands.md): <c>current</c>,
    /// <c>behind: standard C, kit K: 002-slug.md, ...</c>, <c>ahead: standard C, kit K: update dmc</c> or
    /// <c>unfinished: &lt;backup file&gt;</c>.
    /// </summary>
    public string Line => State switch
    {
        StandardState.Current => "current",
        StandardState.Behind => string.Create(CultureInfo.InvariantCulture,
            $"behind: standard {Campaign}, kit {Kit}: {string.Join(", ", Pending.Select(m => m.FileName))}"),
        StandardState.Ahead => string.Create(CultureInfo.InvariantCulture, $"ahead: standard {Campaign}, kit {Kit}: update dmc"),
        _ => $"unfinished: {Marked}",
    };
}

/// <summary>
/// The campaign record <c>adventures/&lt;campaign&gt;/.standard</c> (research R2): one line, an integer, a newline. A missing
/// file means standard 1, the baseline; anything else in it is unreadable. Only <c>dmc campaign verify</c> (on a pass) and
/// <c>dmc campaign standard NAME --init</c> write it.
/// </summary>
internal static class StandardRecord
{
    /// <summary>The record's file name in the campaign folder.</summary>
    public const string FileName = ".standard";

    /// <summary>The standard a campaign with no record is taken to follow.</summary>
    public const int Baseline = 1;

    /// <summary>Reads a campaign's standard.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="name">The campaign's name, for the message.</param>
    /// <returns>The standard; <see cref="Baseline"/> when there is no record.</returns>
    /// <exception cref="DmcException">The record is not one integer of at least 1.</exception>
    public static int Read(string campaignFolder, string name)
    {
        string path = Path.Combine(campaignFolder, FileName);
        if (!File.Exists(path))
            return Baseline;
        return Parse(File.ReadAllBytes(path)) ?? throw Unreadable(name);
    }

    /// <summary>Reads the standard from the bytes of a record, as held in a backup or on disk.</summary>
    /// <param name="bytes">The record's bytes.</param>
    /// <returns>The standard, or null when the bytes are not one integer of at least 1 followed by at most one line end.</returns>
    public static int? Parse(byte[] bytes)
    {
        string text = Encoding.UTF8.GetString(bytes);
        if (text.EndsWith("\r\n", StringComparison.Ordinal))
            text = text[..^2];
        else if (text.EndsWith('\n'))
            text = text[..^1];
        if (text.Length is 0 or > 9 || !text.All(char.IsAsciiDigit))
            return null;
        int value = int.Parse(text, CultureInfo.InvariantCulture);
        return value >= Baseline ? value : null;
    }

    /// <summary>The message for a record that cannot be read.</summary>
    /// <param name="name">The campaign's name.</param>
    public static DmcException Unreadable(string name) =>
        new($"adventures/{name}/{FileName} is unreadable: it must hold one whole number");

    /// <summary>Writes a campaign's standard as one line, through a temporary file so a reader never sees half a record.</summary>
    /// <param name="campaignFolder">The campaign folder.</param>
    /// <param name="standard">The standard to record.</param>
    public static void Write(string campaignFolder, int standard)
    {
        string path = Path.Combine(campaignFolder, FileName);
        string partial = path + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        File.WriteAllText(partial, standard.ToString(CultureInfo.InvariantCulture) + "\n", new UTF8Encoding(false));
        File.Move(partial, path, overwrite: true);
    }

    /// <summary>Where a campaign stands: unfinished when one of its backups is marked, else by comparing its standard with the kit's.</summary>
    /// <param name="root">The DMC folder: its <c>migrations/</c> gives the kit's standard.</param>
    /// <param name="name">The campaign's folder name in <c>adventures/</c>.</param>
    /// <exception cref="DmcException">The record or a migration file cannot be read.</exception>
    public static StandardStatus Status(string root, string name)
    {
        string campaign = Path.Combine(root, "adventures", name);
        var set = MigrationSet.Read(root);
        if (BackupFolder.Marked(campaign, name) is { } marked)
            return new StandardStatus(StandardState.Unfinished, Read(campaign, name), set.Kit, [], marked);
        return Compare(Read(campaign, name), set);
    }

    /// <summary>Compares a standard with the kit's.</summary>
    /// <param name="standard">A campaign's standard.</param>
    /// <param name="set">The kit's migrations.</param>
    public static StandardStatus Compare(int standard, MigrationSet set)
    {
        var state = standard == set.Kit ? StandardState.Current : standard < set.Kit ? StandardState.Behind : StandardState.Ahead;
        return new StandardStatus(state, standard, set.Kit, state == StandardState.Behind ? set.Pending(standard) : [], null);
    }
}
