using System.Security.Cryptography;
using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Size;

/// <summary>
/// Fixture campaigns for the <c>dmc size</c> tests: a temporary DMC folder with a campaign copied from
/// <c>kit/templates/campaign/</c> (real campaigns are git-ignored), and builders for the files the read rules look at,
/// grown to a chosen size. Only <c>System.IO</c> is used, so a fixture does not depend on the code under test.
/// </summary>
public static class SizeFixtures
{
    /// <summary>The fixture campaign's folder name.</summary>
    public const string Name = "test-campaign";

    /// <summary>The kit in this repository, whose templates a fixture is made from.</summary>
    static string Kit => Path.Combine(TestEnv.Root, "kit");

    /// <summary>A new temporary DMC folder (by its manifest) holding a fresh fixture campaign, which is active.</summary>
    /// <returns>The DMC folder; the test deletes it with <see cref="DmcFolders.Delete"/>.</returns>
    public static string NewDmcFolderWithCampaign()
    {
        string root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        NewCampaign(root);
        return root;
    }

    /// <summary>
    /// Copies <c>kit/templates/campaign/</c> to <c>adventures/&lt;name&gt;/</c>, writes the general
    /// <c>adventures/player-options.md</c> from <c>kit/templates/player-options.md</c> if there is none, and makes the
    /// campaign active.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <returns>The campaign folder.</returns>
    public static string NewCampaign(string root, string name = Name)
    {
        string source = Path.Combine(Kit, "templates", "campaign");
        string campaign = Path.Combine(root, "adventures", name);
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(campaign, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        string general = Path.Combine(root, "adventures", "player-options.md");
        if (!File.Exists(general))
            File.Copy(Path.Combine(Kit, "templates", "player-options.md"), general);
        File.WriteAllText(Path.Combine(root, "adventures", ".active-campaign"), name + "\n");
        return campaign;
    }

    /// <summary>The campaign folder of the fixture campaign in a DMC folder.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    public static string CampaignFolder(string root, string name = Name) => Path.Combine(root, "adventures", name);

    /// <summary>Writes a file of the fixture campaign as UTF-8 without a byte-order mark, exactly as given.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    /// <param name="text">The file's text.</param>
    public static void Write(string root, string relative, string text) => WriteAt(CampaignFolder(root), relative, text);

    /// <summary>Writes a file under a folder as UTF-8 without a byte-order mark, making its folders.</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="relative">The path under it, with <c>/</c>.</param>
    /// <param name="text">The file's text.</param>
    public static void WriteAt(string folder, string relative, string text)
    {
        string path = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text));
    }

    /// <summary>
    /// Lines of filler of exactly <paramref name="bytes"/> bytes in all (LF line ends), each line
    /// <paramref name="lineLength"/> bytes long with its line end, the last one shorter if need be.
    /// </summary>
    /// <param name="bytes">The total size, in bytes.</param>
    /// <param name="lineLength">The size of each line, line end included (at least 2).</param>
    public static string Filler(int bytes, int lineLength = 80)
    {
        var text = new StringBuilder();
        while (text.Length < bytes)
        {
            int length = Math.Min(lineLength, bytes - text.Length);
            text.Append('a', length - 1).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>A sheet: a <c># </c> title, a section of <paramref name="bodyBytes"/> bytes, and a last section <paramref name="history"/> of 400 bytes (none when null).</summary>
    /// <param name="title">The character's name.</param>
    /// <param name="bodyBytes">The size of the part before the history, title included.</param>
    /// <param name="history">The history heading, for example <c>## History</c>, or null for none.</param>
    /// <param name="historyFirst">True to put the history before the body's last section (history in the middle).</param>
    public static string Sheet(string title, int bodyBytes = 1000, string? history = "## History", bool historyFirst = false)
    {
        string head = $"# {title}\n\n";
        string about = "## About\n\n";
        string body = head + about + Filler(Math.Max(0, bodyBytes - head.Length - about.Length));
        if (history is null)
            return body;
        string past = history + "\n\n" + Filler(400 - history.Length - 2);
        return historyFirst ? head + past + about + Filler(Math.Max(0, bodyBytes - head.Length - about.Length)) : body + past;
    }

    /// <summary>A <c>game-state.md</c> shaped as the template: a Last full save line, a Scene block with a Present line, Party status blocks and a Combat tracker.</summary>
    /// <param name="companions">The Party status blocks (each a <c>### </c> heading).</param>
    /// <param name="present">The text of the Present line.</param>
    /// <param name="lastFullSave">The text of the Last full save line.</param>
    /// <param name="tracker">The Combat tracker's text.</param>
    /// <param name="notCompanions">The names under "With the party, not companions".</param>
    public static string GameState(string[] companions, string present = "", string lastFullSave = "never",
        string tracker = "No fight in progress.", string notCompanions = "none") =>
        "# Game state\n\n"
        + $"- **Campaign**: Test\n- **Current session**: 3\n- **Last full save**: {lastFullSave}\n\n"
        + $"## Scene\n\n- **Location**: a road\n- **Present**: {present}\n- **What is happening**: walking\n\n"
        + "## Party status\n\n<!-- One block per PC and companion. -->\n\n"
        + string.Concat(companions.Select(c => $"### {c}\n\n- **HP**: 10 / 10\n\n"))
        + $"### With the party, not companions\n\n- {notCompanions}\n\n"
        + $"## Combat tracker\n\n<!--\n- Troll (an example): AC 15\n-->\n\n{tracker}\n\n"
        + "## Pending\n\n- none\n";

    /// <summary>A session log with <paramref name="scenes"/> real scenes of <paramref name="sceneBytes"/> bytes each, after a header that holds the template's commented example heading.</summary>
    /// <param name="scenes">How many <c>### Scene K:</c> headings.</param>
    /// <param name="sceneBytes">The size of each scene, heading included.</param>
    public static string LogOf(int scenes, int sceneBytes = 300)
    {
        var text = new StringBuilder("# Session 3\n\n<!--\n### Scene N: Title\n-->\n\n");
        for (int k = 1; k <= scenes; k++)
        {
            string heading = $"### Scene {k}: Somewhere\n";
            text.Append(heading).Append(Filler(sceneBytes - heading.Length));
        }
        return text.ToString();
    }

    /// <summary>A hash of every file under a folder, path and bytes, so a test can show a run changed nothing.</summary>
    /// <param name="folder">The folder.</param>
    public static string Hash(string folder)
    {
        using var sha = SHA256.Create();
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            byte[] name = Encoding.UTF8.GetBytes(Path.GetRelativePath(folder, file) + "\0");
            sha.TransformBlock(name, 0, name.Length, null, 0);
            byte[] bytes = File.ReadAllBytes(file);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }
}
