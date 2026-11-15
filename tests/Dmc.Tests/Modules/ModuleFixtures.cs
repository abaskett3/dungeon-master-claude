using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Modules;

/// <summary>
/// Synthetic ingested modules for the spec 012 tests: an index, chapter digests with one heading per keyed area, and a
/// monster file with one heading per stat block, written the way <c>dmc-ingest-module</c> asks, plus the planted
/// problems of quickstart.md § On CI. Every name and number is invented: real modules are copyrighted and git-ignored,
/// and never quoted in tests. Only <c>System.IO</c> is used, so a fixture does not depend on the code under test.
/// </summary>
public static class ModuleFixtures
{
    /// <summary>The fixture module's folder name.</summary>
    public const string Module = "salt-marsh";

    /// <summary>A new temporary DMC folder with no module in it.</summary>
    /// <returns>The DMC folder; the test deletes it with <see cref="DmcFolders.Delete"/>.</returns>
    public static string NewFolder() => DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());

    /// <summary>Writes a file under the DMC folder, making its folders; text is written as UTF-8 with LF line ends.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="relative">The path under it, with <c>/</c>.</param>
    /// <param name="text">The file's text.</param>
    public static void Write(string root, string relative, string text)
    {
        string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"), new UTF8Encoding(false));
    }

    /// <summary>Prose of about <paramref name="bytes"/> bytes in lines of about 80 characters, each ending with a newline.</summary>
    /// <param name="bytes">About how many bytes.</param>
    /// <param name="seed">Varies the words, so two blocks are not the same text.</param>
    public static string Prose(int bytes, int seed = 0)
    {
        string[] words = ["reed", "salt", "tide", "heron", "boat", "rope", "lantern", "mud", "post", "net", "gull", "path"];
        var text = new StringBuilder();
        int i = seed;
        while (text.Length < bytes)
        {
            var line = new StringBuilder();
            while (line.Length < 76)
                line.Append(words[i++ % words.Length]).Append(' ');
            text.Append(line.ToString().TrimEnd()).Append(".\n");
        }
        return text.ToString();
    }

    /// <summary>
    /// One stat block of about 650 tokens, as the measured module writes them: a list of <c>AC</c>, <c>HP</c> and the
    /// other lines, then prose. <paramref name="edition2014"/> writes <c>Armor Class</c> and <c>Hit Points</c> instead.
    /// </summary>
    /// <param name="name">The creature's name.</param>
    /// <param name="seed">Varies the numbers and words.</param>
    /// <param name="edition2014">True for the 2014 wording.</param>
    public static string StatBlock(string name, int seed, bool edition2014 = false)
    {
        string ac = edition2014 ? $"- **Armor Class** {12 + seed % 6} (natural armor)" : $"- AC {12 + seed % 6}; Initiative +{seed % 4}";
        string hp = edition2014 ? $"- **Hit Points** {20 + seed * 3} (5d8 + 5)" : $"- HP {20 + seed * 3} (5d8 + 5)";
        return $"{ac}\n{hp}\n- Speed 30 ft., swim 30 ft.\n- STR 14, DEX 12, CON 13, INT 6, WIS 10, CHA 7\n"
            + $"- Actions: Bite +4, 1d8 + 2 piercing; the {name} drags a grappled target under.\n\n" + Prose(2400, seed);
    }

    /// <summary>
    /// Writes a sound module: an index, <paramref name="chapters"/> chapter digests (chapter 5 the hub, with
    /// <paramref name="hubAreas"/> keyed areas of about 1.7k tokens each), and a monster file with
    /// <paramref name="monsters"/> stat blocks, each under its own heading.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="chapters">How many chapter files.</param>
    /// <param name="hubAreas">How many keyed areas the hub chapter has.</param>
    /// <param name="monsters">How many stat blocks the monster file holds.</param>
    /// <param name="chapterBytes">About how many bytes of prose each area of the other chapters holds.</param>
    public static void WriteSoundModule(string root, int chapters = 38, int hubAreas = 8, int monsters = 20, int chapterBytes = 1200)
    {
        var index = new StringBuilder("# The Salt Marsh\n\n## Chapters\n\n");
        for (int c = 1; c <= chapters; c++)
        {
            string file = $"chapters/{c:00}-{(c == 5 ? "the-hub" : $"chapter-{c}")}.md";
            index.Append($"- {c}. {file}\n");
            var text = new StringBuilder($"# Chapter {c}\n\n## Overview (PDF {c * 10})\n\n{Prose(400, c)}\n");
            int areas = c == 5 ? hubAreas : 3;
            for (int a = 1; a <= areas; a++)
                text.Append($"## {a}. Area {c}-{a} (PDF {c * 10 + a})\n\n{Prose(c == 5 ? 6800 : chapterBytes, c * 100 + a)}\n### Treasure\n\n- {a * 7} sp in a tin box.\n\n");
            Write(root, $"modules/{Module}/{file}", text.ToString());
        }
        Write(root, $"modules/{Module}/index.md", index.ToString());

        var bestiary = new StringBuilder("# Monsters\n\n");
        for (int m = 1; m <= monsters; m++)
            bestiary.Append($"## Marsh Beast {m} (PDF {300 + m})\n\n{StatBlock($"marsh beast {m}", m, edition2014: m % 2 == 0)}\n");
        Write(root, $"modules/{Module}/monsters.md", bestiary.ToString());
        Write(root, $"modules/{Module}/source/extracted/all-text.txt", "never read: " + Prose(30_000));
        Write(root, $"modules/{Module}/source/notes.md", "# Not checked\n\n## Twice\n\n## Twice\n\n" + Prose(90_000));
    }

    /// <summary>The old skill's format: a chapter's NPCs under one heading, with two of their stat blocks.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="reHeaded">True for the same chapter with one heading per NPC, which passes.</param>
    public static void WriteOldFormatChapter(string root, bool reHeaded)
    {
        string npcs = reHeaded
            ? $"## NPCs in this chapter\n\n### Marta the Reeve (PDF 61)\n\n{StatBlock("reeve", 1)}\n### Old Tam (PDF 62)\n\n{StatBlock("fisher", 2)}"
            : $"## NPCs in this chapter\n\nMarta the Reeve (PDF 61)\n\n{StatBlock("reeve", 1)}\nOld Tam (PDF 62)\n\n{StatBlock("fisher", 2)}";
        Write(root, $"modules/{Module}/chapters/06-the-dyke.md", $"# Chapter 6\n\n## 1. The Dyke (PDF 60)\n\n{Prose(600)}\n{npcs}");
    }
}
