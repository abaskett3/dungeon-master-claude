# Contract: `dmc records`

One class, `src/Dmc.Cli/Commands/RecordsCommand.cs`, in `CommandTable.All`. `<campaign>` is a folder name in
`adventures/`, as `dmc sheet` and `dmc recall` take it. Errors are one line, `dmc records: <message>`, exit 1.

## `dmc records show <campaign> [<person>...]`

The text view (FR-009). With no person, every record. A person is named by full name, by a record's or a sheet's file
name (`kibil-rornson`, `npc-kibil-rornson.md`), or by words of their name in any order and case, quotes and
punctuation ignored; a name several records fit gives them all. Output, per record:

```
Hessa Kettering, called:
- "Hessa": anyone (setup)
- "Mam": Pim Kettering, when she is frightened (s5.1)
- "the Salt-wife": the dock crews; never Pim Kettering (player: "…")
Relationships: Pim Kettering's mother (s1.2)
Hessa Kettering says: "Pim" for Pim Kettering (setup)
Limits: Cannot read. (inference)
```

Exit 0. A name that fits no record is named on a last line, `no record for "X" (dmc records show <campaign> prints
everyone)`, after the others' records; when no name fits any record: one line on stderr, exit 1.

## `dmc records check <campaign> [--draft]`

The full-save check (FR-007), or with `--draft` the migration's draft (research R7). One line per finding,
`<class>: <where>: <what>`, then `N findings` (or `no findings`). Exit 0 either way: findings are output, not
failures. A record that breaks a rule is a finding of its own (`invalid: …`).

## `dmc records draft <campaign>`

Writes the migration's draft from the regular shapes (each People `(also: …)` item), next to the latest migration
backup; refuses when the draft exists. Prints `draft: <path>, N rows`. Exit 0.

## `dmc records edited`

The `PostToolUse` hook (research R3). Reads the hook's JSON on standard input. `NeverFails`: any failure prints
nothing and exits 0, except a refusal: one line on standard error and exit 2.

| Input | Output | Exit |
|---|---|---|
| A file outside the active campaign, or no active campaign | nothing | 0 |
| A record edit that breaks a rule | `dmc records: <the error>. Fix the record now, or put it back as it was.` on stderr | 2 |
| A `player:` source the edit adds whose quotation is not in the player's typed messages (added: its words were not a quotation of the file as it was, `tool_response.originalFile`, when given and a record; else the edit's new text holds its words more often than its old; for a Write, every one; T058, T066, T073) | `dmc records: <file> row <n>: the player's words "<…>" are not in this session's messages; quote them exactly, or use another source.` | 2 |
| An Edit of a campaign Markdown file that changes a line and adds a credit to the player | `dmc records: <file>: this edit changes the text and credits the player in the same edit; credit only words the player wrote.` | 2 |
| A new usage giving one speaker one word for two persons | `{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"dmc records: to <speaker>, \"<word>\" now means <A> and <B>; the hook prints both."}}` on stdout | 0 |
| A row of the record edited whose speaker or never-person has a sheet and no record yet (T059, T068; `check` refuses it) | `{"hookSpecificOutput":{…,"additionalContext":"dmc records: <file> row <n> (\"<word>\"): \"<person>\" has a sheet and no record yet; write theirs (the full save's check refuses the row until then)."}}` on stdout | 0 |
| Anything else | nothing | 0 |
