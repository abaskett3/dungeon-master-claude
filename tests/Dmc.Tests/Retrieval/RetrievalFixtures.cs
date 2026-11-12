using System.Text;
using Dmc.Tests.Support;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// Fixture DMC folders for the <c>dmc lookup</c> and <c>dmc recall</c> tests: a sequel campaign (<see cref="Sequel"/>)
/// whose Continues line names an earlier one (<see cref="Predecessor"/>), which was played on a module
/// (<see cref="Module"/>), plus a markdown and a PDF-style rules reference; and, apart from it, a linked set of five
/// campaigns with digests (<see cref="NewLinkedSet"/>, spec 011). Every name and fact is invented: real campaigns are
/// git-ignored and never quoted in tests. Only <c>System.IO</c> is used, so a fixture does not depend on the code under
/// test.
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
            "# Campaign options\n\n## Rules\n\n- **Edition**: 2024\n\n" + Continuity("`" + Predecessor + "` <!-- a sequel -->"));
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

        Write(root, $"adventures/{Predecessor}/player-options.md", "# Campaign options\n\n" + Continuity());
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

    /// <summary>A Continuity section with these three values (spec 011 data-model.md § Continuity section).</summary>
    /// <param name="continues">The Continues value.</param>
    /// <param name="side">The Side campaign of value.</param>
    /// <param name="shares">The Shares a world with value.</param>
    public static string Continuity(string continues = "none", string side = "none", string shares = "none") =>
        $"## Continuity\n\n- **Continues**: {continues}\n- **Side campaign of**: {side}\n- **Shares a world with**: {shares}\n";

    /// <summary>The first campaign of the linked set's chain, closed.</summary>
    public const string First = "tale-one";

    /// <summary>The middle of the chain: closed, it continues <see cref="First"/> and holds its digest.</summary>
    public const string Middle = "tale-two";

    /// <summary>The last of the chain, active: it continues <see cref="Middle"/> and holds both digests.</summary>
    public const string Last = "tale-three";

    /// <summary>A side campaign of <see cref="Last"/>.</summary>
    public const string SideTale = "side-tale";

    /// <summary>A campaign that shares a world with <see cref="Last"/>.</summary>
    public const string FarTale = "far-tale";

    /// <summary>A line only <see cref="First"/> holds, in its canon and (word for word) in both copies of its digest.</summary>
    public const string FirstLine = "- The miller Tobin sank the abbey bell in the millpond to hide it from the raiders (s4.2).";

    /// <summary>A line <see cref="Middle"/>'s canon holds, copied word for word into its digest.</summary>
    public const string MiddleLine = "- Sister Wren keeps the ferry tolls in an iron box under the chapel altar (s20.1).";

    /// <summary>
    /// A new temporary DMC folder holding only the linked set, apart from <see cref="NewChain"/>'s so the chain tests keep
    /// their campaign list: <see cref="First"/> ← <see cref="Middle"/> ← <see cref="Last"/> (both closed before it), with
    /// <see cref="SideTale"/> a side campaign of <see cref="Last"/> and <see cref="FarTale"/> in the same world.
    /// </summary>
    /// <returns>The DMC folder; the test deletes it with <see cref="DmcFolders.Delete"/>.</returns>
    public static string NewLinkedSet()
    {
        string root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        BuildLinkedSet(root);
        return root;
    }

    /// <summary>Writes the linked set into an existing DMC folder and makes <see cref="Last"/> active.</summary>
    /// <param name="root">The DMC folder.</param>
    public static void BuildLinkedSet(string root)
    {
        Write(root, "adventures/.active-campaign", Last + "\n");
        Write(root, $"adventures/{First}/player-options.md", "# Campaign options\n\n" + Continuity());
        Write(root, $"adventures/{Middle}/player-options.md", "# Campaign options\n\n" + Continuity(First));
        Write(root, $"adventures/{Last}/player-options.md", "# Campaign options\n\n" + Continuity(Middle));
        Write(root, $"adventures/{SideTale}/player-options.md", "# Campaign options\n\n" + Continuity(side: Last));
        Write(root, $"adventures/{FarTale}/player-options.md", "# Campaign options\n\n" + Continuity(shares: Last));
        // The closed mark as dmc campaign close writes it: one line, the time and the closing backup.
        Write(root, $"adventures/{First}/.closed", $"2026-10-01T09:00:00Z {First}_closed_20261001090000.zip\n");
        Write(root, $"adventures/{Middle}/.closed", $"2026-11-01T09:00:00Z {Middle}_closed_20261101090000.zip\n");

        Write(root, $"adventures/{First}/story/canon.md", "# Canon\n\n## Places\n\n" + FirstLine + "\n");
        Write(root, $"adventures/{Middle}/story/canon.md", "# Canon\n\n## Places\n\n" + MiddleLine + "\n" +
            "- The east gate of Harrowby is barred at sunset by the watch (s20.4).\n");
        Write(root, $"adventures/{Last}/story/canon.md", "# Canon\n\n## Places\n\n" +
            "- The east gate of Harrowby is barred at sunset by the reeve (s40.1).\n" +
            "- The reeve of Harrowby wears a copper chain of office and never smiles (s41.2).\n");
        Write(root, $"adventures/{SideTale}/story/canon.md", "# Canon\n\n- The Thornwick bridge burned on the night of the eclipse (s3.1).\n");
        Write(root, $"adventures/{FarTale}/story/canon.md", "# Canon\n\n- Copper prices in the valley doubled after the Greywater mine flooded (s7.1).\n");

        // The middle campaign's copy of the first's digest, and the last campaign's two digests (FR-013's shape).
        foreach (string holder in new[] { Middle, Last })
            Digest(root, holder, First, places: FirstLine + "\n", people: "## Tobin\n\n- Tobin is the miller of Abbeyford and the bell-ringer's brother (s3.1).\n",
                world: "## Rulings\n\n- A bell can be heard across the whole valley on a still night (s4.1).\n", secrets: "");
        Digest(root, Last, Middle, places: MiddleLine + "\n",
            people: "## Sister Wren\n\n- Sister Wren is the abbey's last nun and keeps all its keys (s19.2).\n",
            world: "## Rulings\n\n- The east gate of Harrowby is barred at sunset by the abbot (s20.4).\n",
            secrets: "## The abbot\n\n- The abbot sold the chapel relic to the smugglers to pay his gambling debts (s22.3), never revealed.\n");
    }

    /// <summary>Writes one digest in FR-013's shape: an index, the subject files, a spine and the secrets.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="holder">The campaign that holds it.</param>
    /// <param name="digested">The campaign it digests.</param>
    /// <param name="places">The places file's subject lines.</param>
    /// <param name="people">The people file's body.</param>
    /// <param name="world">The world file's body.</param>
    /// <param name="secrets">The secrets file's body.</param>
    static void Digest(string root, string holder, string digested, string places, string people, string world, string secrets)
    {
        string folder = $"adventures/{holder}/digests/{digested}";
        Write(root, $"{folder}/index.md", $"# Digest of {digested}\n\n- **Format**: 1\n- **Campaign**: {digested}\n- **Chain**: none\n- **Carried**: none\n");
        Write(root, $"{folder}/places.md", "# Places\n\n## Harrowby valley\n\n" + places);
        Write(root, $"{folder}/people.md", "# People\n\n" + people);
        Write(root, $"{folder}/world.md", "# The world\n\n" + world);
        Write(root, $"{folder}/spine.md", "# Spine\n\n## Session 1 (session-001.md)\n\n- s1.1 The road in (the valley road, morning)\n");
        Write(root, $"{folder}/secrets.md", "# Secrets\n\n" + secrets);
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
