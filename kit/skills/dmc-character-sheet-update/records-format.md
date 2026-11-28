# Records: who calls whom what, and limits

Each person has one record, `records/<name>.json` in the campaign folder: the name in lower case, each run of other
characters a hyphen (`records/hessa-kettering.json`). It holds the words people use for them, the relationships that
name them, and what they cannot do. Read it before you change it; change it with Edit, and create a new one with
Write. `dmc` checks every record right after each edit and refuses a bad one in one line: fix it, or put it back as it
was, before you reply. `dmc records show <campaign> <name>` prints a record as text.

```json
{
  "person": "Hessa Kettering",
  "rows": [
    { "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" },
    { "kind": "usage", "from": "Pim Kettering", "word": "Mam", "when": "when she is frightened", "source": "s5.1" },
    { "kind": "usage", "from": "group: the dock crews", "word": "the Salt-wife", "never": ["Pim Kettering"], "source": "s3.1" },
    { "kind": "relationship", "from": "Pim Kettering", "word": "mother", "source": "s1.2" }
  ],
  "limits": [
    { "text": "Cannot read.", "source": "s2.4" }
  ]
}
```

## Rows

A row sits on the record of the person it names, and says who uses the word:

- **`usage`**: one person, or a group (`group: …`), says the word for this person. "Pim calls her mother Hessa Mam" is a
  usage from Pim on Hessa's record, **not** a relationship.
- **`name`**: anyone says it; `from` is `anyone`.
- **`relationship`**: what this person is to the speaker, never a word anyone calls them. On Hessa's record, from Pim,
  word `mother`: Hessa is Pim's mother. Never write a usage because a relationship exists: someone may call a person
  "Mama" who is not their mother.

`from` and `never` name persons in full, as their records do. Each form of a name is its own row, with a short `when`
when it is used at some times only; choosing a form in a scene is yours, so the record holds no rule for it. `never`
lists who never says the word for this person.

## Sources

Every row and limit has a `source`: a scene of this campaign's session logs (`s4.2`), a scene of a campaign this one
continues (`the-silver-egg s8`), the player's own words quoted exactly (`player: "…"`), `setup`, or `inference`.
Quote the player only with words they wrote in this session. If a change has no speaker or no source, or contradicts a
row, write nothing and tell the player what is missing or what it contradicts; never guess one, and never change a row
to get past a refusal.

## Limits

One fact a line: what the person cannot do, or where they differ from what their kind usually can ("Cannot fly yet").
The hook shows a person's limits in each scene they are in.

## A person of an earlier campaign

A person who belongs to a campaign this one continues, and has no record there, gets one here with
`"of": "<that campaign>"`, sourced to that campaign's scenes. Never change a closed campaign.

## The rest of a sheet: facts and lists

A person whose sheet is in their record names it, `"sheet": "npc-hessa-kettering.md"` (`pc-<name>.md` for a player
character), and holds it as facts and lists, which `dmc records show <campaign> <name> --all` prints as the sheet:

```json
  "facts": [
    { "key": "Status", "value": "alive", "source": "s5.1" },
    { "key": "AC, max HP, speed, saves, senses", "value": "AC 11; HP 9; speed 30 ft", "card": true, "source": "setup" },
    { "key": "STR", "value": "10", "section": "Stat block", "source": "setup" }
  ],
  "lists": [
    { "list": "Looks", "text": "Grey shawl, a net needle behind one ear.", "card": true, "source": "s1.2" },
    { "list": "What they know", "text": "The smugglers land at the north stair on the 3rd of Mirtul.", "section": "What they know", "source": "s4.1" }
  ]
```

- **A fact** is one key and its one value: a status, an AC, a score. A key is any words, the template's label where it
  has one. `dmc` refuses a second fact with a key the record has: change that fact instead. When you add a key, `dmc`
  lists the record's keys: use the one that means the same ("Hair", not "Hair colour").
- **A list entry** is one fact in free text: one thing they wear, one thing they know, one event of their history.
  Several facts are several entries of one list.
- **`section`** is the sheet heading it prints under (`What they know`, `Stat block`); none for the opening fields.
  **`"card": true`** prints it on the play card instead, once: never write a fact twice for the card.
- **Numbers `dmc` computes with**: `STR` to `CHA` (a score from 1 to 30) and `STR save` to `CHA save` (signed) print as
  the ability table in `Abilities`; `Proficiency bonus` and `Passive Perception`. Attacks of a PC are entries of the
  list `Attacks` in `Combat`: `Boathook | +5 | 1d8+3 piercing | reach 10 ft`.
- **A date** is a full in-world date: the day, its month of the Harptos calendar and its year (`the 14th of Eleasis,
  1493 DR`), never a day alone. `dmc` refuses "ready on the 14th" and "the 14th of Eleasis".
- **Every fact and entry has a source**, as a row does.
