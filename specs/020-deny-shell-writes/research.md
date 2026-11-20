# Research: Deny the usual shell writes and `SendFeedback` in play

## R1. Which commands to deny

- **Decision**: the thirteen shell rules of FR-001, as issue #245 lists them.
- **Rationale**: they match 43 of the 45 shell writes the issue found in the play logs. `sed -i*` takes `sed -i`,
  `sed -i.bak` and `sed -i ''`; `sed * -i*` takes `-i` after other flags (`sed -e 's/a/b/' -i a.md`). A plain `sed`
  without `-i` only prints, so reads keep working. `python`, `python3` and `py` (the Windows launcher), `node` and
  `perl` are the script runners; `tee` writes its input to a file; `Set-Content`, `Add-Content` and `Out-File` are
  PowerShell's content writers. Matching is checked against `PatternRegex`: a trailing ` *` also takes the bare
  command, and a pattern such as `node *` does not take `nodemon`.
- **Alternatives rejected**: `Bash(*)` or the whole `Bash` tool, which would take away `dmc` (play needs it,
  `TheKitSettingsKeepEverythingPlayNeeds`); a rule per redirect or `bash -c`, which Claude Code's rule text cannot
  express without blocking reads too.
- **Checked**: no kit skill, rule or hook names one of these commands (`grep -rnE
  '\b(python3?|py|node|perl|tee|Set-Content|Add-Content|Out-File|sed)\b' kit` outside `kit/settings.json`: no hit).

## R2. `SendFeedback`

- **Decision**: deny it by its bare name, as the kit denies `NotebookEdit`, `CronCreate` and the others.
- **Rationale**: it was called twice in play (`108c0877`, `2163a49a`), mid-correction, and leaves a draft with the
  player's words on their computer. A bare-name deny removes the tool's definition (about 5,500 characters) from each
  session. `TheKitSettingsKeepEverythingPlayNeeds` checks the rule takes no tool play needs.
- **Unverified**: that the definition leaves the context; quickstart Q2 checks it live.

## R3. `.active-campaign`

- **Decision**: `dmc-new-campaign` § 1 says "with the Write tool"; so does `dmc-resume-campaign` § 1, which writes the
  same file in the same words.
- **Rationale**: in `9eec3bdf` DMC wrote it with `Set-Content`, now refused. The resume line is not in the issue, but
  it is the same write with no tool named, so leaving it would leave the same refused command one skill away. The
  `Edit(adventures/**)` allow covers Write too (decisions.md, the A13 entry), so it raises no prompt.
- **Cost**: 20 bytes in each of the two skills; 0 in `kit/dm-rules.md`.

## R4. The `auto` mode stays

- **Decision**: no `defaultMode` in the kit, and never `remoteControlAtStartup`.
- **Rationale**: without the `auto` mode Claude Code asked for permission on 274 shell commands in 53 of 114 logs,
  on the player's phone too. The test asserts neither key is set.

## R5. Migration

None: no campaign file, template or file-shape rule changes. `dmc update` writes the new settings and skills as it
does for every release.
