namespace Dmc.Tests.Support;

/// <summary>
/// The golden fixtures of the migrations (research R11): <c>tests/Dmc.Tests/Fixtures/migrations/NNN/</c> with a
/// <c>before/</c> and an <c>after/</c> campaign folder and the <c>accounting.tsv</c> a correct run writes. Read from the
/// repository, as the PDF fixtures are.
/// </summary>
public static class MigrationFixtures
{
    /// <summary>The folder that holds one fixture folder per migration, named by its three-digit number.</summary>
    public static string Folder { get; } = Path.Combine(TestEnv.Root, "tests", "Dmc.Tests", "Fixtures", "migrations");

    /// <summary>The fixture folders there are, by number, in order.</summary>
    public static IReadOnlyList<string> Numbers() =>
        Directory.Exists(Folder)
            ? [.. Directory.GetDirectories(Folder).Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal)]
            : [];

    /// <summary>The folder of one migration's fixture.</summary>
    /// <param name="number">The migration's three-digit number.</param>
    public static string Of(string number) => Path.Combine(Folder, number);

    /// <summary>
    /// Replaces a campaign's files with a fixture's <c>before/</c> or <c>after/</c>: every file and folder of the campaign is
    /// deleted except its top-level <c>backups/</c> folder, then the fixture's files are copied in.
    /// </summary>
    /// <param name="number">The migration's three-digit number.</param>
    /// <param name="side"><c>before</c> or <c>after</c>.</param>
    /// <param name="campaign">The campaign folder, made if missing.</param>
    public static void CopyInto(string number, string side, string campaign)
    {
        Directory.CreateDirectory(campaign);
        foreach (var entry in new DirectoryInfo(campaign).EnumerateFileSystemInfos())
        {
            if (entry.Name == "backups")
                continue;
            if (entry is DirectoryInfo directory)
                directory.Delete(recursive: true);
            else
                entry.Delete();
        }
        string source = Path.Combine(Of(number), side);
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(campaign, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            // Bytes as they are in the repository, except CRLF from a Windows checkout, so a run on Windows sees what CI sees.
            byte[] bytes = File.ReadAllBytes(file);
            if (file.EndsWith(".md", StringComparison.Ordinal) || file.EndsWith(".tsv", StringComparison.Ordinal))
                bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n"));
            File.WriteAllBytes(target, bytes);
        }
    }
}
