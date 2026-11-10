using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// A temporary DMC folder for the tests of campaign migrations: a folder <c>dmc</c> takes for a DMC folder (through
/// <c>CLAUDE_PROJECT_DIR</c>, which it sets and clears), with hand-made migration files in <c>migrations/</c> and campaigns
/// in <c>adventures/</c>, and <c>dmc campaign</c> run in this process against it. Deleted on dispose.
/// </summary>
internal sealed class MigrationRoot : IDisposable
{
    /// <summary>The campaign most tests use.</summary>
    public const string Name = "rorn";

    /// <summary>The DMC folder.</summary>
    public string Folder { get; }

    /// <summary>Makes the folder, with an empty <c>adventures/</c>, and points <c>dmc</c> at it.</summary>
    public MigrationRoot()
    {
        Folder = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        Directory.CreateDirectory(Adventures);
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", Folder);
    }

    /// <summary>The <c>adventures/</c> folder.</summary>
    public string Adventures => Path.Combine(Folder, "adventures");

    /// <summary>A campaign's folder, whether or not it exists.</summary>
    /// <param name="name">The campaign's name.</param>
    public string Campaign(string name = Name) => Path.Combine(Adventures, name);

    /// <summary>A campaign's <c>backups/</c> folder.</summary>
    /// <param name="name">The campaign's name.</param>
    public string Backups(string name = Name) => Path.Combine(Campaign(name), "backups");

    /// <summary>Writes a UTF-8 file (no byte-order mark) under a campaign, making its folder.</summary>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    /// <param name="text">What it holds.</param>
    /// <param name="name">The campaign's name.</param>
    public void Write(string relative, string text, string name = Name)
    {
        string path = Path.Combine(Campaign(name), relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text));
    }

    /// <summary>Reads a file of a campaign as text.</summary>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    /// <param name="name">The campaign's name.</param>
    public string Read(string relative, string name = Name) =>
        File.ReadAllText(Path.Combine(Campaign(name), relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>True when a file of a campaign exists.</summary>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    /// <param name="name">The campaign's name.</param>
    public bool Has(string relative, string name = Name) =>
        File.Exists(Path.Combine(Campaign(name), relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Writes a small campaign: a game state, a canon, DM notes with an Opposition table, and a PC sheet.</summary>
    /// <param name="name">The campaign's name.</param>
    /// <returns>The campaign folder.</returns>
    public string WriteCampaign(string name = Name)
    {
        Write("game-state.md", "# Game state\n\n- **Campaign**: Rorn's Landing\n- **Current session**: 3\n\n## Scene\n\n- **Location**: the ferry\n", name);
        Write("story/canon.md", "# Canon\n\n## People\n\n- Mara keeps the ferry and charges 2 sp. (s1)\n\n## Timeline\n\n- The party crossed the river. (s1.2)\n", name);
        Write("story/dm-notes.md", "# DM notes\n\n## Opposition\n\n| Who | How many (alive / started) | Where now | Stat block (AC, HP, attacks) | The party knows |\n|---|---|---|---|---|\n| Bog trolls | 2 / 2 | the marsh | AC 15, HP 84 | rumours |\n\n## Open threads\n\n- Who paid the smugglers?\n", name);
        Write("character-sheets/pc-wren.md", "# Wren\n\n- **Class and level**: Rogue 3\n\n## Story\n\n- **Backstory**: raised on the docks\n\n## History\n\n- s1: met Mara.\n", name);
        return Campaign(name);
    }

    /// <summary>The text of a well-formed migration file.</summary>
    /// <param name="number">Its standard.</param>
    /// <param name="drops">Its drop classes.</param>
    /// <param name="checks">Its checks; one that passes on any campaign without sheets in the wrong shape by default.</param>
    public static string MigrationText(int number, string[]? drops = null, string[]? checks = null)
    {
        drops ??= [];
        checks ??= ["sheets-end-with-history"];
        var text = new StringBuilder();
        text.Append("---\n");
        text.Append($"standard: {number}\n");
        text.Append($"title: Test migration {number}\n");
        text.Append("shipped: PR #1\n");
        text.Append("touches: [story/dm-notes.md]\n");
        text.Append($"drops: [{string.Join(", ", drops)}]\n");
        text.Append($"checks: [{string.Join(", ", checks)}]\n");
        text.Append("---\n\n# Test\n\n## What changes\n\nSomething.\n\n## Steps\n\n1. Do it.\n\n## Judgments\n\n- None.\n\n## Declared drops\n\n");
        foreach (string drop in drops)
            text.Append($"- `{drop}`: gone.\n");
        if (drops.Length == 0)
            text.Append("None.\n");
        text.Append("\n## Done when\n\nThe check passes.\n");
        return text.ToString();
    }

    /// <summary>Writes migrations 2 to <paramref name="kit"/> into <c>migrations/</c>, so the kit's standard is <paramref name="kit"/>.</summary>
    /// <param name="kit">The kit's standard.</param>
    /// <param name="drops">The drop classes of the last migration.</param>
    /// <param name="checks">The checks of every migration.</param>
    public void WriteMigrations(int kit, string[]? drops = null, string[]? checks = null)
    {
        string folder = Path.Combine(Folder, "migrations");
        Directory.CreateDirectory(folder);
        for (int n = 2; n <= kit; n++)
            File.WriteAllText(Path.Combine(folder, $"{n:000}-test-{n}.md"), MigrationText(n, n == kit ? drops : null, checks));
    }

    /// <summary>Writes the standard record of a campaign.</summary>
    /// <param name="value">What it holds.</param>
    /// <param name="name">The campaign's name.</param>
    public void SetStandard(string value, string name = Name) => Write(".standard", value, name);

    /// <summary>Runs <c>dmc campaign</c> in this process.</summary>
    /// <param name="args">The arguments after <c>campaign</c>.</param>
    public static ProcessResult Run(params string[] args) => DmcRunner.InProcess(["campaign", .. args]);

    /// <summary>Takes a backup with <c>dmc campaign backup</c> and returns its file name.</summary>
    /// <param name="name">The campaign's name.</param>
    public string Backup(string name = Name)
    {
        var result = Run("backup", name);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        string line = result.StdoutLines.Single();
        int at = line.IndexOf("backups/", StringComparison.Ordinal);
        return line[(at + "backups/".Length)..].Split(';')[0];
    }

    /// <summary>The full path of a file in a campaign's <c>backups/</c>.</summary>
    /// <param name="file">The file name.</param>
    /// <param name="name">The campaign's name.</param>
    public string InBackups(string file, string name = Name) => Path.Combine(Backups(name), file);

    /// <summary>Removes the <c>.migrating</c> mark of a backup, so another can be taken.</summary>
    /// <param name="file">The backup's file name.</param>
    /// <param name="name">The campaign's name.</param>
    public void Unmark(string file, string name = Name) => File.Delete(InBackups(file + ".migrating", name));

    /// <summary>Writes the accounting file of a backup.</summary>
    /// <param name="file">The backup's file name.</param>
    /// <param name="lines">Its lines, each written with a line feed.</param>
    public void Account(string file, params string[] lines) =>
        File.WriteAllText(InBackups(file + ".accounting.tsv"), string.Concat(lines.Select(l => l + "\n")));

    /// <summary>Forgets the folder as the project folder and deletes it.</summary>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CLAUDE_PROJECT_DIR", null);
        DmcFolders.Delete(Folder);
    }
}
