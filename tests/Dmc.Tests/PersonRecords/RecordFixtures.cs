using System.Text.Json.Nodes;
using Dmc.Tests.Support;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The invented campaign the records tests run on (spec 032 tasks T001, T002), never play text: the write-path test's
/// cast (#263) in <c>kettle-run</c>, which continues the closed campaign <c>kettle-before</c>, whose hag "Mother Gorse"
/// has her record kept in <c>kettle-run</c> (spec 032, the closed earlier campaign). Each test gets its own copy in a
/// temporary DMC folder.
/// </summary>
internal static class RecordFixtures
{
    /// <summary>The campaign's folder name.</summary>
    public const string Campaign = "kettle-run";

    /// <summary>The closed campaign it continues.</summary>
    public const string Earlier = "kettle-before";

    /// <summary>The records of the campaign, by file name.</summary>
    public static readonly IReadOnlyDictionary<string, string> Records = new Dictionary<string, string>
    {
        ["hessa-kettering.json"] = """
            {
              "person": "Hessa Kettering",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" },
                { "kind": "usage", "from": "Pim Kettering", "word": "Mam", "when": "when she is frightened", "source": "s5.1" },
                { "kind": "relationship", "from": "Pim Kettering", "word": "mother", "source": "s1.2" }
              ],
              "limits": []
            }
            """,
        ["pim-kettering.json"] = """
            {
              "person": "Pim Kettering",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Pim", "source": "setup" }
              ],
              "limits": [
                { "text": "Cannot swim yet.", "source": "s2.5" }
              ]
            }
            """,
        ["ilvane-vell.json"] = """
            {
              "person": "Ilvane Vell",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Ilvane", "source": "setup" },
                { "kind": "usage", "from": "Pim Kettering", "word": "Auntie Ilvane", "source": "s1.1" },
                { "kind": "usage", "from": "group: her crew", "word": "Captain", "when": "on deck", "source": "s2.1" },
                { "kind": "usage", "from": "Pim Kettering", "word": "Cap", "never": ["Benno Kettering"], "source": "s2.4" },
                { "kind": "usage", "from": "Benno Kettering", "word": "Da", "when": "as a joke, when she bosses him about", "source": "s5.3" }
              ],
              "limits": [
                { "text": "Cannot read.", "source": "player: \"she's never learned her letters\"" }
              ]
            }
            """,
        ["benno-kettering.json"] = """
            {
              "person": "Benno Kettering",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Benno", "source": "setup" }
              ],
              "limits": []
            }
            """,
        ["grumbald.json"] = """
            {
              "person": "Grumbald",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Grumbald", "source": "setup" },
                { "kind": "usage", "from": "Benno Kettering", "word": "Da", "source": "s1.4" },
                { "kind": "relationship", "from": "Benno Kettering", "word": "father", "source": "s5.2" }
              ],
              "limits": []
            }
            """,
        ["nettlebone.json"] = """
            {
              "person": "Nettlebone",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Nettlebone", "source": "setup" },
                { "kind": "usage", "from": "Pim Kettering", "word": "Nan Nettle", "source": "s4.2" },
                { "kind": "usage", "from": "Nettlebone", "word": "Nan", "never": ["Pim Kettering"], "source": "s4.3" }
              ],
              "limits": []
            }
            """,
        ["mother-gorse.json"] = """
            {
              "person": "Mother Gorse",
              "of": "kettle-before",
              "rows": [
                { "kind": "name", "from": "anyone", "word": "Mother", "source": "kettle-before s2.1" }
              ],
              "limits": []
            }
            """,
    };

    /// <summary>A new temporary DMC folder holding the fixture; the test deletes it with <see cref="DmcFolders.Delete"/>.</summary>
    /// <param name="present">The scene's Present line (after <c>- **Present**: </c>).</param>
    /// <param name="withRecords">False for the campaign as it was before migration 010: no <c>records/</c>.</param>
    public static string NewFolder(string present = "Pim Kettering, Hessa Kettering", bool withRecords = true)
    {
        string folder = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());
        string adventures = Path.Combine(folder, "adventures");
        string root = Path.Combine(adventures, Campaign);
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(adventures, ".active-campaign"), Campaign);
        Sessions(root, sessions: 6, scenes: 6);
        File.WriteAllText(Path.Combine(root, "game-state.md"), $"# Game state\n\n## Scene\n\n- **Present**: {present}\n");
        File.WriteAllText(Path.Combine(root, "player-options.md"), $"# Player options\n\n## Continuity\n\n- **Continues**: {Earlier}\n");
        Directory.CreateDirectory(Path.Combine(root, "story"));
        File.WriteAllText(Path.Combine(root, "story", "canon.md"), """
            # Canon

            ## People

            - **Hessa Kettering** (also: Hessa, Mam, Nan): keeps the barge's galley (s1.2)
            - **Nettlebone** (also: Nan, Nan Nettle): the reed-witch (s4.2)
            - **Mother Gorse** (also: Mother): the hag of the marsh (s2)

            ## Places
            """);
        string sheets = Path.Combine(root, "character-sheets");
        Directory.CreateDirectory(sheets);
        File.WriteAllText(Path.Combine(sheets, "npc-pim-kettering.md"), """
            # Pim Kettering

            ## Play card

            - **Voice** (and one sample line): quick and shrill. "Nan, look at the fish!" (to Nettlebone)
            - **Names they use**: Hessa is Mam; never Nan for Nettlebone
            """);
        File.WriteAllText(Path.Combine(sheets, "npc-ilvane-vell.md"), """
            # Ilvane Vell

            ## Play card

            - **Voice** (and one sample line): gruff. "I'll read the river, not your letters."
            """);
        if (withRecords)
        {
            Directory.CreateDirectory(Path.Combine(root, "records"));
            foreach (var (name, text) in Records)
                File.WriteAllText(Path.Combine(root, "records", name), text.Replace("\r\n", "\n") + "\n");
        }

        string earlier = Path.Combine(adventures, Earlier);
        Directory.CreateDirectory(earlier);
        Sessions(earlier, sessions: 2, scenes: 2);
        File.WriteAllText(Path.Combine(earlier, ".closed"), "closed: 2026-11-01T00:00:00Z\n");
        return folder;
    }

    /// <summary>Writes session logs with numbered scene headings, as the session-log template has them.</summary>
    /// <param name="root">The campaign folder.</param>
    /// <param name="sessions">How many sessions.</param>
    /// <param name="scenes">Scenes in each.</param>
    static void Sessions(string root, int sessions, int scenes)
    {
        string folder = Path.Combine(root, "story", "sessions");
        Directory.CreateDirectory(folder);
        for (int s = 1; s <= sessions; s++)
        {
            var lines = new List<string> { $"# Session {s}", "", "## Scenes" };
            for (int n = 1; n <= scenes; n++)
                lines.AddRange(["", $"### Scene {n}: an invented scene (the barge, day {s})", "", "Nothing of note."]);
            File.WriteAllText(Path.Combine(folder, $"session-{s:000}.md"), string.Join("\n", lines) + "\n");
        }
    }

    /// <summary>The hook's JSON for a player message in a DMC folder, with an optional log.</summary>
    /// <param name="folder">The DMC folder (the hook's <c>cwd</c>).</param>
    /// <param name="prompt">The player's message.</param>
    /// <param name="log">The session's log; null for none.</param>
    public static string Prompt(string folder, string prompt, string? log = null)
    {
        var input = new JsonObject { ["prompt"] = prompt, ["cwd"] = folder };
        if (log is not null)
            input["transcript_path"] = log;
        return input.ToJsonString();
    }

    /// <summary>The <c>PostToolUse</c> hook's JSON for an Edit or Write of a file.</summary>
    /// <param name="folder">The DMC folder (the hook's <c>cwd</c>).</param>
    /// <param name="tool">The tool's name.</param>
    /// <param name="path">The file's full path.</param>
    /// <param name="oldText">The Edit's old text; null for a Write.</param>
    /// <param name="newText">The Edit's new text, or the Write's content.</param>
    /// <param name="log">The session's log; null for none.</param>
    public static string Edited(string folder, string tool, string path, string? oldText, string newText, string? log = null)
    {
        var toolInput = new JsonObject { ["file_path"] = path };
        if (tool == "Write")
            toolInput["content"] = newText;
        else
        {
            toolInput["old_string"] = oldText;
            toolInput["new_string"] = newText;
        }
        var input = new JsonObject { ["tool_name"] = tool, ["tool_input"] = toolInput, ["cwd"] = folder };
        if (log is not null)
            input["transcript_path"] = log;
        return input.ToJsonString();
    }

    /// <summary>A session log of typed player messages, as Claude Code writes user records.</summary>
    /// <param name="folder">Where to write it.</param>
    /// <param name="messages">The player's messages.</param>
    public static string Log(string folder, params string[] messages)
    {
        string path = Path.Combine(folder, "session.jsonl");
        File.WriteAllLines(path, messages.Select(m => new JsonObject { ["type"] = "user", ["message"] = new JsonObject { ["role"] = "user", ["content"] = m } }.ToJsonString()));
        return path;
    }
}
