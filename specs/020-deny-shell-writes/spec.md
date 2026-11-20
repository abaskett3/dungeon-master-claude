# Feature Specification: Deny the usual shell writes and `SendFeedback` in play

**Feature Branch**: `020-deny-shell-writes`

**Created**: 2026-11-19

**Status**: Draft

**Input**: Issue #245 (part of #239, plan #238): "Slips 0.2: deny the usual shell writes and the SendFeedback tool in
the DMC folder."

## Background

`kit/dm-rules.md:63` tells DMC to read campaign files with the Read tool and change them with Edit, one file per call,
to use Write only to create a new file, and to use no shell commands or scripts on them. Two things from outside the
kit work against that rule (issue #245, from the maintainer's play logs; the logs are not in the repository):

1. **The `auto` permission mode.** In 9 of 114 play logs, Claude Code's text for this mode tells the model to "read
   files with cat, head, or sed -n" and to make changes "with sed, heredocs, or short scripts instead of the dedicated
   Read, Edit, or Write tools".
2. **Claude Code's own `SendFeedback` tool.** Its text tells the model to draft feedback about Claude Code when the
   user is frustrated, and not to tell the user. `kit/settings.json` does not deny it.

Evidence, as issue #245 gives it:

- DMC changed campaign files with shell commands about 45 times in 5 logs, with no request from the player. About 20
  of the changes were to story files. 4 of the 5 logs carry the `auto` text; of the 105 logs without it, 1 has such a
  change.
- DMC called `SendFeedback` twice in play (logs `108c0877`, `2163a49a`), in the middle of a correction. The tool keeps
  a draft on the player's computer; the drafts hold the player's words and campaign names. Nothing shows a draft was
  sent.
- The tool's definition adds about 5,500 characters to each play session.
- In log `9eec3bdf`, DMC wrote `adventures/.active-campaign` with `Set-Content` during `dmc-new-campaign` § 1
  (`kit/skills/dmc-new-campaign/SKILL.md:14`, which names no tool for that write).

What the kit has today (checked on this branch, based on `origin/main` at `68c3396`):

- `kit/settings.json` `permissions.deny` already refuses the shell deletes and moves (`Bash(rm *)`,
  `PowerShell(Remove-Item *)` and the rest, issue #36) and a list of tools by bare name (`NotebookEdit`,
  `CronCreate`, `PushNotification` and others). It has no rule for a shell write and none for `SendFeedback`.
- `kit/settings.json` sets no `defaultMode` and no `remoteControlAtStartup`.
- No kit skill, rule or hook names `sed`, `python`, `python3`, `py`, `node`, `perl`, `tee`, `Set-Content`,
  `Add-Content`, `Out-File` or `SendFeedback` (`grep -rnwE` over `kit/`: no hit).
- `kit/skills/dmc-resume-campaign/SKILL.md:12` also writes `adventures/.active-campaign` and names no tool.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A correction is made with an edit the player can see (Priority: P1)

The player corrects a fact, and DMC changes the character sheet. Today it may run `sed -i` on the sheet: the player
sees no diff, and the one-Edit-per-file rule is not obeyed. After this change Claude Code refuses the command, and DMC
makes the change with Edit, which the player can see.

**Why this priority**: it is the issue's main slip, about 45 unasked changes in 5 logs, about 20 of them to story
files.

**Independent Test**: in a scratch DMC folder written by this build, ask DMC to change a line of a campaign file with
`sed -i`; Claude Code refuses the command, and the change is made with Edit.

**Acceptance Scenarios**:

1. **Given** a DMC folder from this build, **When** DMC runs `sed -i`, `python`, `python3`, `py`, `node`, `perl` or
   `tee` in Bash, or `Set-Content`, `Add-Content`, `Out-File`, `python` or `py` in PowerShell, **Then** Claude Code
   refuses it without asking the player.
2. **Given** the same folder, **When** DMC runs a command that only reads (`cat`, `sed -n`, `Get-Content`) or runs
   `dmc`, **Then** nothing changes from today.

---

### User Story 2 - No hidden feedback drafts, and a smaller session (Priority: P1)

DMC does not call `SendFeedback`, so no draft holding the player's words is left on their computer without their
knowing, and the tool's definition no longer costs context in each session.

**Why this priority**: it is a privacy slip, and its fix also lowers the cost of every session (Core 3).

**Independent Test**: in a play session in a DMC folder from this build, Claude Code's context listing shows no
`SendFeedback` tool.

**Acceptance Scenarios**:

1. **Given** a DMC folder from this build, **When** a play session starts, **Then** `SendFeedback` is not among the
   tools DMC can call.

---

### User Story 3 - A campaign is made active without a refused command (Priority: P2)

When DMC starts or resumes a campaign, it writes the folder name to `adventures/.active-campaign` with a file tool,
because the skill names the tool, so it does not reach for `Set-Content`, which the new rules refuse.

**Why this priority**: without it the new rules turn a known slip (log `9eec3bdf`) into a refused command at the start
of a campaign.

**Independent Test**: start a new campaign and resume one in a scratch DMC folder; `adventures/.active-campaign` holds
the folder name, written with no shell command and no permission prompt.

**Acceptance Scenarios**:

1. **Given** no `adventures/.active-campaign`, **When** `dmc-new-campaign` § 1 makes a campaign, **Then** DMC writes the
   file with the tool the skill names.
2. **Given** an `adventures/.active-campaign` that names another campaign, **When** `dmc-resume-campaign` § 1 makes
   the chosen campaign active, **Then** DMC changes the file with the tool the skill names.

---

### Edge Cases

- A shell write by a redirect (`> file`), `bash -c`, a full program path or another interpreter is not matched. The
  rules match 43 of the 45 shell writes in the logs (issue #245, Limits). `kit/dm-rules.md:63` stays the rule's home.
- `sed` with `-i` after other options (`sed -e 's/a/b/' -i file`) must be refused as well as `sed -i file`.
- A command that starts with one of the names but is another program (`nodemon`, `pythonw`, `teeth`) is not a target.
- `dmc-resume-campaign` replaces an `adventures/.active-campaign` that exists, while `kit/dm-rules.md:63` says to use
  Write only to create a new file and never to replace one that exists.
- Toolkit work in this repository is not blocked: the developer's `.claude/settings.json` gains none of these rules.
- The hooks and `dmc` itself run outside Claude Code's permission rules; no kit hook or skill names one of these
  commands, so none is refused.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The kit's settings MUST deny, in Bash, `sed -i` (with `-i` first or after other options), `python`,
  `python3`, `py`, `node`, `perl` and `tee`, and, in PowerShell, `Set-Content`, `Add-Content`, `Out-File`, `python`
  and `py`: the rules issue #245 lists.
- **FR-002**: The kit's settings MUST deny the `SendFeedback` tool, so that it is not offered to DMC in play.
- **FR-003**: The `auto` permission mode MUST stay available: the kit sets no default permission mode, and never sets
  `remoteControlAtStartup`.
- **FR-004**: `dmc-new-campaign` § 1 MUST name the file tool DMC uses to write `adventures/.active-campaign`.
- **FR-005**: `dmc-resume-campaign` § 1, which writes the same file, MUST name its tool too, without contradicting
  `kit/dm-rules.md:63`.
- **FR-006**: A toolkit test MUST check that the kit's settings hold the FR-001 and FR-002 rules, that they refuse the
  commands of User Story 1 scenario 1 and none of scenario 2, that the developer's settings hold none of them, and
  FR-003 to FR-005.
- **FR-007**: No line MUST be added to `kit/dm-rules.md`, and no campaign file, template or file-shape rule changes.

### Key Entities

- **Deny rule**: one entry of `permissions.deny` in the kit's settings: a tool name, with or without a command
  pattern. Claude Code refuses a call it matches without asking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every rule of FR-001 and FR-002 is in the kit's settings, and the toolkit tests pass on CI.
- **SC-002**: A play session's context is about 5,500 characters smaller, with no `SendFeedback` definition in it.
- **SC-003**: In the first 10 play logs after the release, no campaign file is changed by a command these rules match.
- **SC-004**: No session in those logs shows a refused command when a campaign is made active.

## Assumptions

- Claude Code removes a tool denied by its bare name from the model's tool list, as it does for the tools the kit
  already denies by name. Unverified; checked live in a play session.
- Claude Code matches these patterns the way `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` `PatternRegex` models it: `*`
  is any text, a trailing ` *` also takes the bare command, PowerShell ignores case, and chains are split into
  subcommands. The shell-delete rules (issue #36) rest on the same model.
- `Edit(adventures/**)` in the allow list also covers Write, so a file tool on `adventures/.active-campaign` raises no
  prompt (`.docs/development.md`, the toolkit layer: Claude Code applies `Edit` rules to `Write` too).
- The `auto` mode stays: without it, Claude Code asked for permission on 274 shell commands in 53 of 114 logs, on the
  player's phone too (issue #245).
- No migration is needed: no campaign file or template changes, and `dmc update` writes the new settings and skills
  as for every release.
