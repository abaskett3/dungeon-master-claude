# Contract: the facts and lists of a record (`records/<slug>.json`)

Part A's contract (`specs/032-who-calls-whom/contracts/records-file.md`) holds; this adds three fields. One fact or
entry an object a line. DMC writes with Write (a new person) and Edit; `dmc` never writes a record.

```json
{
  "person": "Hessa Kettering",
  "sheet": "npc-hessa-kettering.md",
  "rows": [
    { "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" }
  ],
  "limits": [],
  "facts": [
    { "key": "Role in the story", "value": "The harbour's net-mender", "source": "setup" },
    { "key": "Status", "value": "alive", "source": "s5.1" },
    { "key": "AC, max HP, speed, saves, senses", "value": "AC 11; HP 9; speed 30 ft", "card": true, "source": "setup" },
    { "key": "STR", "value": "10", "section": "Stat block", "source": "setup" }
  ],
  "lists": [
    { "list": "Looks", "text": "Grey shawl, a net needle behind one ear.", "card": true, "source": "s1.2" },
    { "list": "Voice", "text": "Slow, dry; ends a sentence with \"so.\"", "card": true, "source": "s1.2" },
    { "list": "What they know", "text": "The smugglers land at the north stair on the 3rd of Mirtul.", "section": "What they know", "source": "s4.1" },
    { "list": "History", "text": "The 5th of Mirtul: mended Pim's sail for nothing.", "section": "History", "source": "s5.1" }
  ]
}
```

The names are invented. A PC's `sheet` is `pc-<name>.md`.

## Errors

One line each, as part A's: `dmc records: <file> <fact|list> <n> ("<key or list>"): <what is wrong>`, for example:

```
dmc records: records/hessa-kettering.json fact 4 ("Hair"): a second value for "Hair" (fact 2 says "brown"): change that fact, or put this one back
dmc records: records/hessa-kettering.json list 3 ("What they know"): "the 3rd" has no month: write the full in-world date (the 3rd of Mirtul)
dmc records: records/hessa-kettering.json fact 6 ("STR"): "strong" is not a score: STR is a whole number from 1 to 30
dmc records: records/hessa-kettering.json: a record with facts or lists names its sheet ("sheet": "npc-hessa-kettering.md")
```

## The print (`dmc records show <campaign> <person> --all`)

```
# Hessa Kettering

- **Role in the story**: The harbour's net-mender (setup)
- **Status**: alive (s5.1)

## Play card

- **AC, max HP, speed, saves, senses**: AC 11; HP 9; speed 30 ft (setup)
- **Looks**: Grey shawl, a net needle behind one ear. (s1.2)
- **Voice**: Slow, dry; ends a sentence with "so." (s1.2)

## What they know

- The smugglers land at the north stair on the 3rd of Mirtul. (s4.1)

## Stat block

- **STR**: 10 (setup)

## History

- The 5th of Mirtul: mended Pim's sail for nothing. (s5.1)
```
