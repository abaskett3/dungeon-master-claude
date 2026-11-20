# Feature Specification: Deny the usual shell writes and `SendFeedback` in play

**Feature Branch**: `020-deny-shell-writes`

**Created**: 2026-11-19

**Status**: Draft

**Input**: Issue #245 (part of #239, plan #238): "Slips 0.2: deny the usual shell writes and the SendFeedback tool in
the DMC folder."

## Background

`kit/dm-rules.md:63` tells DMC to read campaign files with the Read tool and change them with Edit, and to use no shell
commands or scripts on them. Two things from outside the kit work against that rule:

1. **The `auto` permission mode.** In 9 of 114 play logs, Claude Code's text for this mode tells the model to "read
   files with cat, head, or sed -n" and to make changes "with sed, heredocs, or short scripts instead of the dedicated
   Read, Edit, or Write tools".
2. **Claude Code's own `SendFeedback` tool.** Its text tells the model to draft feedback about Claude Code when the
   user is frustrated, and not to tell the user. The kit does not deny it.

Evidence (from the issue):

- DMC changed campaign files with shell commands about 45 times in 5 logs, with no request from the player. About 20
  of the changes were to story files. 4 of the 5 logs carry the `auto` text; of the 105 logs without it, 1 has such a
  change.
- DMC called `SendFeedback` twice in play (logs `108c0877`, `2163a49a`), in the middle of a correction. The tool keeps
  a draft on the player's computer holding the player's words and campaign names. Nothing shows a draft was sent.
- The tool's definition adds about 5,500 characters to each play session.
- In log `9eec3bdf`, DMC wrote `adventures/.active-campaign` with `Set-Content` during `dmc-new-campaign` step 1.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A correction is made with an edit the player can see (Priority: P1)

After the player corrects a fact, DMC changes the character sheet. Today it may run `sed -i` on the sheet, so the
player sees no diff and the one-Edit-per-file rule is not obeyed. With this change Claude Code refuses the command,
and DMC makes the change with Edit.

**Independent Test**: in a scratch DMC folder, ask DMC to change a line of a campaign file with `sed -i`; Claude Code
refuses it (quickstart Q1).

**Acceptance Scenarios**:

1. **Given** a DMC folder from this build, **When** DMC tries `sed -i`, `python`, `py`, `node`, `perl` or `tee` in
   Bash, or `Set-Content`, `Add-Content`, `Out-File`, `python` or `py` in PowerShell, **Then** Claude Code refuses it
   without a prompt.
2. **Given** the same folder, **When** DMC reads with `cat`, `sed -n` or `Get-Content`, or runs `dmc`, **Then**
   nothing changes from today.

### User Story 2 - No hidden feedback drafts, and a smaller session (Priority: P1)

DMC does not call `SendFeedback`, so no draft holding the player's words is left on their computer, and the tool's
definition is no longer in each session's context.

**Independent Test**: `/context` in a play session lists no `SendFeedback` tool (quickstart Q2).

### User Story 3 - A new campaign is made active without a refused command (Priority: P2)

`dmc-new-campaign` § 1 and `dmc-resume-campaign` § 1 name the Write tool for `adventures/.active-campaign`, so DMC
does not reach for `Set-Content`, which is now denied.

### Edge Cases

- A shell write by a redirect (`> file`), `bash -c`, a full program path or another interpreter is not matched. The
  rules match 43 of the 45 shell writes in the logs (research R1). The rule in `kit/dm-rules.md:63` stays the home.
- Toolkit work is not blocked: the developer's `.claude/settings.json` gains none of these rules.
- The hooks and `dmc` itself are not subject to permission rules; no kit hook or skill names one of these commands.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `kit/settings.json` MUST deny `Bash(sed -i*)`, `Bash(sed * -i*)`, `Bash(python *)`, `Bash(python3 *)`,
  `Bash(py *)`, `Bash(node *)`, `Bash(perl *)`, `Bash(tee *)`, `PowerShell(Set-Content *)`,
  `PowerShell(Add-Content *)`, `PowerShell(Out-File *)`, `PowerShell(python *)` and `PowerShell(py *)`.
- **FR-002**: `kit/settings.json` MUST deny the `SendFeedback` tool.
- **FR-003**: The `auto` permission mode MUST stay: the kit sets no `defaultMode` and never `remoteControlAtStartup`.
- **FR-004**: `kit/skills/dmc-new-campaign/SKILL.md` § 1 MUST name the Write tool for `adventures/.active-campaign`;
  `kit/skills/dmc-resume-campaign/SKILL.md` § 1, which writes the same file, MUST too.
- **FR-005**: A toolkit test MUST check that `kit/settings.json` holds the FR-001 and FR-002 rules, what they match and
  do not match, that the developer's settings do not hold them, and FR-003 and FR-004.
- **FR-006**: No line is added to `kit/dm-rules.md`; no campaign file or template changes.

## Success Criteria *(mandatory)*

- **SC-001**: Every rule of FR-001 and FR-002 is in `kit/settings.json`, and the toolkit tests pass on CI.
- **SC-002**: A play session's context is about 5,500 characters smaller (no `SendFeedback` definition).
- **SC-003**: In the next 10 play logs after the release, no campaign file is changed by a command these rules match.

## Assumptions

- Claude Code removes a tool denied by its bare name from the model's tool list, as it does for the tools the kit
  already denies (`NotebookEdit`, `CronCreate` and the rest). To be confirmed live (quickstart Q2).
- Pattern matching is as `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` `PatternRegex` models it, the same model the
  shell-delete rules (issue #36) rest on.
