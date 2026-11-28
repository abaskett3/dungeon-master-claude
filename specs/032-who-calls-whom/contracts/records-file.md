# Contract: a record file (`records/<slug>.json`)

UTF-8 JSON, two-space indent, one row or limit object a line. DMC writes it with Write (a new person) and changes it
with Edit; `dmc` never writes it. The rules are [data-model.md](../data-model.md) § Rules.

```json
{
  "person": "Hessa Kettering",
  "rows": [
    { "kind": "name", "from": "anyone", "word": "Hessa", "source": "setup" },
    { "kind": "usage", "from": "Pim Kettering", "word": "Mam", "when": "when she is frightened", "source": "s5.1" },
    { "kind": "usage", "from": "group: the dock crews", "word": "the Salt-wife", "never": ["Pim Kettering"], "source": "player: \"the dockers call her the Salt-wife, never Pim\"" },
    { "kind": "relationship", "from": "Pim Kettering", "word": "mother", "source": "s1.2" }
  ],
  "limits": [
    { "text": "Cannot read.", "source": "inference" }
  ]
}
```

The names above are invented. A person of a linked earlier campaign adds `"of": "<campaign>"`, and their scene sources
name that campaign: `"source": "the-silver-egg s8"`.

## Errors

One line each, `dmc records: <file> row <n> ("<word>"): <what is wrong>`, for example:

```
dmc records: records/hessa-kettering.json row 3 ("the Salt-wife"): no speaker (from): say who uses the word, or anyone for a name
dmc records: records/hessa-kettering.json row 2 ("Mam"): scene s9.1 is not in the session logs
dmc records: records/hessa-kettering.json: not valid JSON (line 4)
```
