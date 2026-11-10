namespace Dmc.Campaign;

/// <summary>
/// The migrations of a DMC folder (<c>migrations/</c>, installed from <c>kit/migrations/</c>; research R3), oldest first. The
/// kit's standard is the highest number present, 1 when there is none. Numbers run from 2 without a gap.
/// </summary>
internal sealed class MigrationSet
{
    /// <summary>The folder of a DMC folder that holds the migrations.</summary>
    public const string FolderName = "migrations";

    /// <summary>The migrations, oldest first.</summary>
    public IReadOnlyList<MigrationFile> All { get; }

    /// <summary>The kit's standard: the highest migration number, or <see cref="StandardRecord.Baseline"/> with none.</summary>
    public int Kit => All.Count == 0 ? StandardRecord.Baseline : All[^1].Standard;

    /// <summary>Makes the set from migrations already in order.</summary>
    /// <param name="all">The migrations, oldest first, numbered from 2 without a gap.</param>
    internal MigrationSet(IReadOnlyList<MigrationFile> all) => All = all;

    /// <summary>Reads <c>migrations/</c> of a DMC folder. A missing folder is an empty set.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <exception cref="DmcException">A migration file cannot be read or is malformed, or the numbers do not run from 2 without a gap.</exception>
    public static MigrationSet Read(string root)
    {
        string folder = Path.Combine(root, FolderName);
        if (!Directory.Exists(folder))
            return new MigrationSet([]);
        var files = new DirectoryInfo(folder).EnumerateFiles()
            .Where(f => MigrationFile.IsMigrationName(f.Name))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => MigrationFile.Parse(f.Name, File.ReadAllText(f.FullName)))
            .ToList();
        for (int i = 0; i < files.Count; i++)
        {
            int expected = StandardRecord.Baseline + 1 + i;
            if (files[i].Standard != expected)
                throw new DmcException($"migrations/ is damaged: expected migration {expected:000}, found {files[i].FileName} (run dmc update)");
        }
        return new MigrationSet(files);
    }

    /// <summary>The migrations above a standard, up to the kit's, oldest first.</summary>
    /// <param name="standard">A campaign's standard.</param>
    public IReadOnlyList<MigrationFile> Pending(int standard) => [.. All.Where(m => m.Standard > standard)];

    /// <summary>The drop classes of some migrations, each once.</summary>
    /// <param name="migrations">The pending migrations.</param>
    public static IReadOnlySet<string> Drops(IEnumerable<MigrationFile> migrations) =>
        migrations.SelectMany(m => m.Drops).ToHashSet(StringComparer.Ordinal);

    /// <summary>The checks of some migrations, each once, in the order they are first named.</summary>
    /// <param name="migrations">The pending migrations.</param>
    public static IReadOnlyList<string> Checks(IEnumerable<MigrationFile> migrations) =>
        [.. migrations.SelectMany(m => m.Checks).Distinct(StringComparer.Ordinal)];
}
