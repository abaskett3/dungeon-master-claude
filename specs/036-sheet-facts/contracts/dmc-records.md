# Contract: `dmc records`, what spec 036 adds

Part A's contract (`specs/032-who-calls-whom/contracts/dmc-records.md`) holds.

## `dmc records show <campaign> [<person>...] [--card | --sheet | --all]`

- Without a flag: part A's lines, then `Facts:` and each list's entries, one line each, with sources, for a record
  that has them.
- `--card`: the record's print with sources, from the top to the end of `## Play card` (all of it when it has no
  card). A record with no `sheet` prints part A's lines instead.
- `--sheet`: the print with sources down to the `## History` heading.
- `--all`: the whole print with sources.

A person named whose record has no `sheet`, with one of these flags, prints part A's lines. Exit 0.

## `dmc records check <campaign>`

Adds `sheet-and-record: character-sheets/<file>: <person>'s record prints this sheet; remove the Markdown sheet once
its lines are in the record`.

## `dmc records draft <campaign> --sheets`

Writes the sheet draft (data-model.md § Sheet draft) next to the latest migration backup and prints
`draft: <path>, N entries (M with a source)`. Refuses, one line, exit 1, when that draft exists or there is no backup.

## `dmc records edited`

| Input | Output | Exit |
|---|---|---|
| A record edit that breaks rule 8 to 12 | `dmc records: <the error>. Fix the record now, or put it back as it was.` on stderr | 2 |
| A `player:` source on a fact or entry that is not in the player's messages | as for rows, naming `fact <n>` or `list <n>` | 2 |
| A record edit that adds a key or a list name | `{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"dmc records: records/<file> adds the key \"<key>\" (close to \"<other>\"); its keys: <k1>, <k2>; its lists: <l1>, <l2>."}}` on stdout | 0 |
