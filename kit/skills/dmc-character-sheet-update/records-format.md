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
