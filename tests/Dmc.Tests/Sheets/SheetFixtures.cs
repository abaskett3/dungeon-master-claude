using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Sheets;

/// <summary>
/// The fixture campaign of the <c>dmc sheet</c> tests (spec 031): <c>tests/Dmc.Tests/Fixtures/sheets/campaign/</c>, an
/// invented campaign of a PC (Ana Reed), a companion on the NPC template (Bram), a companion on the PC template (Cora), an
/// NPC outside the party (Hob the miller) and a Party status block that names no sheet (Shared supplies). Every field
/// the page must never print holds a word starting <see cref="Spoiler"/>. Only <c>System.IO</c> is used, so a fixture
/// does not depend on the code under test.
/// </summary>
public static class SheetFixtures
{
    /// <summary>The fixture campaign's folder name in <c>adventures/</c>.</summary>
    public const string Name = "vale";

    /// <summary>The word every field that must not be exported starts with.</summary>
    public const string Spoiler = "SPOILER";

    /// <summary>The fixture campaign in the repository.</summary>
    public static string Source => Path.Combine(TestEnv.Root, "tests", "Dmc.Tests", "Fixtures", "sheets", "campaign");

    /// <summary>The golden page of <c>dmc sheet --party</c> on the fixture, dated <see cref="Today"/>.</summary>
    public static string GoldenParty => Path.Combine(TestEnv.Root, "tests", "Dmc.Tests", "Fixtures", "sheets", "party.golden.html");

    /// <summary>The date the in-process tests stamp a page with.</summary>
    public static DateOnly Today { get; } = new(2026, 1, 5);

    /// <summary>A new temporary DMC folder holding the fixture campaign, which is active.</summary>
    /// <returns>The DMC folder; the test deletes it with <see cref="DmcFolders.Delete"/>.</returns>
    public static string NewDmcFolder()
    {
        string root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        CopyCampaign(root);
        return root;
    }

    /// <summary>Copies the fixture campaign into <c>adventures/</c> of a DMC folder and makes it active.</summary>
    /// <param name="root">The DMC folder.</param>
    public static void CopyCampaign(string root)
    {
        string campaign = Path.Combine(root, "adventures", Name);
        foreach (string file in Directory.EnumerateFiles(Source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(campaign, Path.GetRelativePath(Source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        File.WriteAllText(Path.Combine(root, "adventures", ".active-campaign"), Name + "\n");
    }

    /// <summary>The path of a file of the fixture campaign in a DMC folder.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    public static string PathOf(string root, string relative) =>
        Path.Combine(root, "adventures", Name, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Replaces text in a file of the fixture campaign; the text must be there.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="relative">The path in the campaign, with <c>/</c>.</param>
    /// <param name="old">The text to replace.</param>
    /// <param name="replacement">What replaces it.</param>
    public static void Edit(string root, string relative, string old, string replacement)
    {
        string path = PathOf(root, relative);
        string text = File.ReadAllText(path);
        Assert.Contains(old, text, $"the fixture {relative} no longer holds the text a test changes");
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text.Replace(old, replacement)));
    }

    /// <summary>The page an export wrote, under <c>exports/sheets/vale/</c>.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="file">The page's file name.</param>
    public static string Page(string root, string file) => Path.Combine(root, "exports", "sheets", Name, file);

    /// <summary>A hash of every file under a folder, names included, so a test can tell the folder was left as it was.</summary>
    /// <param name="folder">The folder.</param>
    public static string Hash(string folder)
    {
        var text = new StringBuilder();
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            text.Append(Path.GetRelativePath(folder, file)).Append('\n').Append(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)))).Append('\n');
        return text.ToString();
    }
}
