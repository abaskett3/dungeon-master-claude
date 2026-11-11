using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// Fixture DMC folders for the <c>dmc lookup</c> and <c>dmc recall</c> tests: a sequel campaign (<see cref="Sequel"/>)
/// whose Predecessor line names an earlier one (<see cref="Predecessor"/>), which was played on a module
/// (<see cref="Module"/>), plus a markdown and a PDF-style rules reference. Every name and fact is invented: real
/// campaigns are git-ignored and never quoted in tests. Only <c>System.IO</c> is used, so a fixture does not depend on
/// the code under test.
/// </summary>
public static class RetrievalFixtures
{
    /// <summary>The sequel campaign, which is active.</summary>
    public const string Sequel = "new-road";

    /// <summary>The campaign the sequel continues.</summary>
    public const string Predecessor = "old-road";

    /// <summary>The module the predecessor was played on.</summary>
    public const string Module = "fen-crossing";

    /// <summary>A new temporary DMC folder holding the chain, the module and nothing else; <see cref="Sequel"/> is active.</summary>
    /// <returns>The DMC folder; the test deletes it with <see cref="DmcFolders.Delete"/>.</returns>
    public static string NewChain()
    {
        string root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        BuildChain(root);
        return root;
    }

    /// <summary>Writes the chain, the module and the general options into an existing DMC folder, and makes <see cref="Sequel"/> active.</summary>
    /// <param name="root">The DMC folder.</param>
    public static void BuildChain(string root)
    {
        Write(root, "adventures/.active-campaign", Sequel + "\n");
        Write(root, "adventures/player-options.md", "# Player options\n\n- **Dice**: the player rolls for their character\n");

        Write(root, $"adventures/{Sequel}/player-options.md",
            "# Campaign options\n\n- **Edition**: 2024\n- **Predecessor**: `" + Predecessor + "` <!-- a sequel -->\n");
        Write(root, $"adventures/{Sequel}/game-state.md",
            "# Game state\n\n## Scene\n\n- **Location**: the north bank of the Fen, beside the ferry hut\n");
        Write(root, $"adventures/{Sequel}/story/canon.md",
            "# Canon\n\n## Rulings\n\n- Crossing the Fen by ferry takes a quarter of an hour in calm water (s30.2).\n\n" +
            "## Prices, debts and promises\n\n- The ferryman Oswin charges 3 sp a head, 1 gp for a cart (s31.4).\n" +
            "- Hedda the smith promised to mend the cart axle for 5 gp, paid half up front (s31.1).\n\n" +
            "## People\n\n- Oswin: ferryman at the Fen crossing, grey beard, hums sea songs while he poles (s31.4).\n");
        Write(root, $"adventures/{Sequel}/story/canon-archive.md",
            "# Canon archive\n\n- The old toll bridge at Marrow Ford burned down the winter before the party arrived (s2.1).\n");
        Write(root, $"adventures/{Sequel}/story/dm-notes.md",
            "# DM notes\n\n## Secrets\n\n- Oswin is the smugglers' lookout and signals their boats with his lantern.\n");
        Write(root, $"adventures/{Sequel}/story/summary.md",
            "# Summary\n\nThe party left the Landing and reached the Fen, where the ferry is the only way across.\n");
        Write(root, $"adventures/{Sequel}/story/sessions/session-031.md",
            "# Session 31\n\n### Scene 1: The smithy\n\nHedda looks at the axle and names her price, half of it up front.\n\n" +
            "### Scene 4: The ferry\n\nOswin taps the tally on the hut door and waves the cart aboard the raft.\n");
        Write(root, $"adventures/{Sequel}/character-sheets/npc-oswin.md",
            "# Oswin\n\n## Playing them\n\n- **Voice**: slow, hums between sentences, calls everyone \"friend\"\n" +
            "- **Names they use**: calls the river \"the old lady\"\n\n## Secrets\n\n" +
            "- Keeps a ledger of every boat that crosses at night under a loose board in the hut.\n\n" +
            "## History\n\n- Met the party at the Fen crossing and took them over for the usual fare (s31.4).\n");
        Write(root, $"adventures/{Sequel}/character-sheets/npc-hedda.md",
            "# Hedda\n\n## Playing them\n\n- **Voice**: blunt, short sentences, laughs at her own jokes\n");

        Write(root, $"adventures/{Predecessor}/player-options.md", "# Campaign options\n\n- **Predecessor**: none\n");
        Write(root, $"adventures/{Predecessor}/module-progress.md",
            "# Module progress\n\n- **Module**: *Fen Crossing*\n- **Module folder**: `modules/" + Module + "/`\n");
        Write(root, $"adventures/{Predecessor}/story/canon-archive.md",
            "# Canon archive\n\n- Oswin owes the party a free crossing for the giant eel they killed under his raft (s12.3).\n");
        Write(root, $"adventures/{Predecessor}/story/canon.md",
            "# Canon\n\n## Places\n\n- The Fen crossing has one raft, poled along a rope strung between two posts (s11.2).\n");

        Write(root, $"modules/{Module}/index.md",
            "# Fen Crossing\n\n## Chapters\n\n| Chapter | File |\n|---|---|\n| 1. The Ford | chapters/01-the-ford.md |\n");
        Write(root, $"modules/{Module}/chapters/01-the-ford.md",
            "# Chapter 1: The Ford\n\n## Area 1: The ferry hut (PDF 4)\n\nA reed hut on stilts; the ferryman's tally of crossings is cut into its door.\n\n" +
            "### Treasure\n\nUnder the loose board: 12 sp and a ledger of night crossings.\n\n" +
            "## Area 2: The raft (PDF 5)\n\nA raft of lashed logs, poled along a rope; it carries one cart and four people.\n");
        Write(root, $"modules/{Module}/monsters.md",
            "# Monsters\n\n## GOBLIN BOSS (PDF 12)\n\n*Small Humanoid (Goblinoid), Neutral Evil*\n\n- **AC** 17\n- **HP** 21 (6d6)\n\n" +
            "## GIANT EEL (PDF 13)\n\n*Large Beast, Unaligned*\n\n- **AC** 13\n- **HP** 52 (7d10 + 14)\n");
        Write(root, $"modules/{Module}/source/extracted/all-text.txt", "Oswin ferry cart 3 sp 1 gp NOISE from the source text\n");
    }

    /// <summary>Adds files that match every fixture query but must never be searched: under <c>import/</c>, <c>exports/</c>, <c>backups/</c>, a dot folder and a module's <c>source/</c>.</summary>
    /// <param name="root">The DMC folder.</param>
    public static void AddNoise(string root)
    {
        const string noise = "- NOISE Oswin ferryman ferry cart price charges gp sp crossing Hedda smith axle (s99.9).\n";
        Write(root, $"adventures/{Sequel}/import/transcript.md", "# Transcript\n\n" + noise);
        Write(root, $"adventures/{Sequel}/exports/old.md", "# Export\n\n" + noise);
        Write(root, $"adventures/{Sequel}/backups/notes.md", "# Backup\n\n" + noise);
        Write(root, $"adventures/{Sequel}/.staging/canon.md", "# Staging\n\n" + noise);
        Write(root, $"modules/{Module}/source/notes.md", "# Source notes\n\n" + noise);
        Write(root, $"adventures/{Sequel}/maps/fen.json", "{\"title\": \"NOISE Oswin ferry cart\"}\n");
    }

    /// <summary>A rules reference kept as markdown: <c>rules/index.md</c> and one file of entries.</summary>
    /// <param name="root">The DMC folder.</param>
    public static void MarkdownRules(string root)
    {
        Write(root, "rules/index.md", "# Rules index\n\n- Conditions: `conditions.md`\n");
        Write(root, "rules/conditions.md",
            "# Conditions\n\n## Grappled\n\nA grappled creature's speed is 0.\n\n## Prone\n\nA prone creature can only crawl.\n");
    }

    /// <summary>
    /// A rules reference kept as a PDF: an index of section rows and letter lines, and extracted page text in which
    /// "Fireball" appears on pages 129 and 130 only.
    /// </summary>
    /// <param name="root">The DMC folder.</param>
    public static void PdfRules(string root)
    {
        Write(root, "rules/index.md",
            "# Rules index\n\n**Document:** `rules/srd.pdf`\n\n## Sections\n\n| Section | Pages |\n|---|---|\n" +
            "| Combat: order, movement and position, attacks, mounted, underwater | 13–16 |\n" +
            "| Spell descriptions A–Z | 107–175 |\n\n## Spell descriptions, first page of each letter\n\nA 107, B 112, F 129.\n");
        Write(root, "rules/srd.pdf", "%PDF-1.7 placeholder\n");
        Write(root, "rules/extracted/text/page-128.txt", "Fly\nLevel 3 Transmutation\n");
        Write(root, "rules/extracted/text/page-129.txt", "Fireball\nLevel 3 Evocation\nA bright streak flashes\n");
        Write(root, "rules/extracted/text/page-130.txt", "ball of fire.\nUsing a Higher-Level Spell Slot. The damage of\nthe Fireball increases\n");
    }

    /// <summary>Writes a file under the DMC folder as UTF-8 without a byte-order mark, exactly as given, making its folders.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="relative">The path under it, with <c>/</c>.</param>
    /// <param name="text">The file's text.</param>
    public static void Write(string root, string relative, string text)
    {
        string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
