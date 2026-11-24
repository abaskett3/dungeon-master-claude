# Data model: Deny the usual shell writes and the SendFeedback tool in play

No campaign data and no `dmc` data change. The only structure is the deny list of `kit/settings.json`, which a DMC
folder gets as `.claude/settings.json`.

## Deny rule

One string in `permissions.deny`.

| Form | Example | Meaning |
|---|---|---|
| Bare tool name | `SendFeedback` | The tool is not offered to the model at all (research R1) |
| `Bash(pattern)` | `Bash(sed -i*)` | A Bash command whose text matches the pattern is refused |
| `PowerShell(pattern)` | `PowerShell(Set-Content *)` | The same for PowerShell, case-insensitive |

In a pattern, `*` matches any text; a trailing ` *` (space, then `*`) also matches the bare command, and needs a word
break, so `py *` does not take `pytest`. The rules this spec adds are listed in
[contracts/kit-settings-deny.md](contracts/kit-settings-deny.md).

## Validation

- Each rule of the contract is in `kit/settings.json` and not in `.claude/settings.json` (toolkit work).
- No rule takes a tool play needs (`TheKitSettingsKeepEverythingPlayNeeds`) or a command a kit prompt names (FR-007).
