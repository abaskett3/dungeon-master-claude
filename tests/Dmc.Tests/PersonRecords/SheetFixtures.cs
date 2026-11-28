namespace Dmc.Tests.PersonRecords;

/// <summary>
/// Records that hold the rest of a sheet (spec 036), on the invented campaign of <see cref="RecordFixtures"/>: Hessa
/// Kettering's record with facts and lists, as <c>contracts/records-file.md</c> shows it, and the Markdown sheet it
/// replaces. Invented text only.
/// </summary>
internal static class SheetFixtures
{
    /// <summary>Hessa's record with facts and lists, printing <c>npc-hessa-kettering.md</c>.</summary>
    public const string Hessa = """
        {
          "person": "Hessa Kettering",
          "sheet": "npc-hessa-kettering.md",
          "rows": [
            { "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" },
            { "kind": "usage", "from": "Pim Kettering", "word": "Mam", "when": "when she is frightened", "source": "s5.1" },
            { "kind": "relationship", "from": "Pim Kettering", "word": "mother", "source": "s1.2" }
          ],
          "limits": [],
          "facts": [
            { "key": "Role in the story", "value": "The harbour's net-mender", "source": "setup" },
            { "key": "Status", "value": "alive", "source": "s5.1" },
            { "key": "AC, max HP, speed, saves, senses", "value": "AC 11; HP 9; speed 30 ft", "card": true, "source": "setup" },
            { "key": "STR", "value": 10, "section": "Stat block", "source": "setup" }
          ],
          "lists": [
            { "list": "Looks", "text": "Grey shawl, a net needle behind one ear.", "card": true, "source": "s1.2" },
            { "list": "Voice", "text": "Slow, dry; ends a sentence with \"so.\" \"Sit, Pim, so.\" (to Pim Kettering)", "card": true, "source": "s1.2" },
            { "list": "What they know", "text": "The smugglers land at the north stair on the 3rd of Mirtul, 1492 DR.", "section": "What they know", "source": "s4.1" },
            { "list": "History", "text": "The 5th of Mirtul, 1492 DR: mended Pim's sail for nothing.", "section": "History", "source": "s5.1" }
          ]
        }
        """;

    /// <summary>A PC's record: the ability table's facts, an attack, and a section of the PC template.</summary>
    public const string Benno = """
        {
          "person": "Benno Kettering",
          "sheet": "pc-benno-kettering.md",
          "rows": [
            { "kind": "name", "from": "anyone", "word": "Benno", "source": "setup" }
          ],
          "limits": [],
          "facts": [
            { "key": "Player character", "value": "yes", "source": "setup" },
            { "key": "Class and level", "value": "Fighter 3", "source": "s6.1" },
            { "key": "STR", "value": "16", "section": "Abilities", "source": "setup" },
            { "key": "DEX", "value": "12", "section": "Abilities", "source": "setup" },
            { "key": "CON", "value": "14", "section": "Abilities", "source": "setup" },
            { "key": "INT", "value": "8", "section": "Abilities", "source": "setup" },
            { "key": "WIS", "value": "10", "section": "Abilities", "source": "setup" },
            { "key": "CHA", "value": "13", "section": "Abilities", "source": "setup" },
            { "key": "STR save", "value": "+5", "section": "Abilities", "source": "setup" },
            { "key": "Proficiency bonus", "value": "+2", "section": "Abilities", "source": "setup" },
            { "key": "Max HP", "value": "28", "section": "Combat", "source": "s6.1" }
          ],
          "lists": [
            { "list": "Attacks", "text": "Boathook | +5 | 1d8+3 piercing | reach 10 ft", "section": "Combat", "source": "setup" },
            { "list": "Personality", "text": "Laughs at his own jokes first.", "section": "Story", "source": "s1.4" }
          ]
        }
        """;

    /// <summary>Writes a record into the fixture campaign's <c>records/</c>, replacing any of that name.</summary>
    /// <param name="folder">The DMC folder (<see cref="RecordFixtures.NewFolder"/>).</param>
    /// <param name="file">The record's file name.</param>
    /// <param name="text">Its JSON.</param>
    public static string Put(string folder, string file, string text)
    {
        string path = Path.Combine(folder, "adventures", RecordFixtures.Campaign, "records", file);
        File.WriteAllText(path, text.Replace("\r\n", "\n") + "\n");
        return path;
    }
}
