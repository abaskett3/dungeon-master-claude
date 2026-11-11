# Contract: closing a campaign, and what a closed one does

**Feature**: `010-campaign-close` | **Date**: 2026-11-11

Every failure is one line on standard error, `dmc <command>: <message>`, exit 1; a usage mistake exits 2
(`.docs/decisions.md`, one-line errors). `<name>` is the campaign's folder name. "The refusal" below is always:

```text
dmc <command>: <name> is closed: start a sequel to continue its story
```

## `dmc campaign close NAME [--yes]` (new)

| Case | Output | Exit | Writes |
|---|---|---|---|
| Plan, campaign current | `would close <name>: <N> files to backups/<file> and exports/<file>` | 0 | nothing |
| `--yes`, campaign current | `closed <name>: <N> files to backups/<file> and exports/<file>` | 0 | `.closed`, the closing backup, the second copy |
| No such campaign | `dmc campaign: no campaign "<name>" in adventures/ (...)` (as today) | 1 | nothing |
| Already closed | `dmc campaign: <name> is already closed` | 1 | nothing |
| Unfinished migration | `dmc campaign: <name> has an unfinished migration from <backup>: finish it or restore it before closing` | 1 | nothing |
| Behind | `dmc campaign: <name> is behind (standard C, kit K): bring it up to date before closing` | 1 | nothing |
| Ahead | `dmc campaign: <name> is ahead (standard C, kit K): update dmc` | 1 | nothing |
| A step of `--yes` fails | `dmc campaign: could not close <name>: <step>: <reason>; it is still open` | 1 | nothing left from this run |

`<step>` is one of `mark`, `backup`, `check backup`, `copy`, `check copy`. Before anything else the verb puts back an
interrupted restore and prints that line, as every migration verb does.

## Changed verbs on a closed campaign

| Command | On a closed campaign |
|---|---|
| `dmc campaign standard [NAME]` | `closed: <closed at>`, exit 0, ahead of every other status |
| `dmc campaign standard NAME --init` | the refusal |
| `dmc campaign backups NAME` | `0 backups` or the counted list as today, then `closed: backups/<closing backup>`; never an `ask:` line |
| `dmc campaign backup NAME [--prune ...]` | the refusal |
| `dmc campaign restore NAME [--from F] [--yes]` | the refusal, with or without `--yes` |
| `dmc campaign verify NAME [--backup F] [--all]` | byte check against the closing backup (below); writes nothing |
| `dmc campaign verify NAME --list` or `--accounting F` | `dmc campaign: <name> is closed: --list and --accounting are for migrations` |
| `dmc campaign export NAME [...]` | as today (writes outside `adventures/` only) |
| `dmc campaign show FILE` | as today; for a file that holds a `.closed`, also `closed: <closed at>` |
| `dmc campaign import FILE [--as N]` | as today; the new campaign holds the mark and is closed; the last line is `<new name> is closed: start a sequel to continue its story` |
| `dmc map <file under a closed campaign> [...]` | the refusal (`dmc map:`); `--measure` still works, it writes nothing |
| `dmc redraw` with a closed active campaign | nothing printed, exit 0, nothing drawn |
| `dmc pdf ... --out <folder under a closed campaign>` | the refusal (`dmc pdf:`) |
| `dmc digest show ... --out <folder under a closed campaign>` | the refusal (`dmc digest:`) |
| `dmc lookup`, `dmc recall`, `dmc size` | as today (they write nothing) |
| `dmc update`, `dmc refresh` | a closed campaign is never counted in "campaigns are behind" |

### `verify` on a closed campaign

```text
verified <name>: closed, <N> files match <closing backup>
```

exit 0; or up to 40 of

```text
changed: <path>
added: <path>
missing: <path>
```

(every one with `--all`, else `<n> more` after the 40th), then on standard error
`dmc campaign: verification failed: <n> files differ from <closing backup>`, exit 1. The backup is the one the mark
names, else `--backup F`; a mark that cannot be read and no `--backup` gives
`dmc campaign: adventures/<name>/.closed is unreadable: name the closing backup with --backup`.

## The kit (prompts)

| File | Contract |
|---|---|
| `kit/skills/dmc-close-campaign/SKILL.md` | New. `name: dmc-close-campaign`. Description says to use it only when the player asks to close a campaign or agrees to DMC's one suggestion when the story has ended. Steps as research R9. Names only real commands: `dmc campaign close`, `dmc campaign close --yes`, `dmc size <campaign> --check`; and real skills: `dmc-campaign-full-save`, `dmc-migrate-campaign`, `dmc-new-campaign` |
| `kit/skills/dmc-resume-campaign/SKILL.md` § 1 | On `closed`: say it is closed, read nothing for play, start no session log, change no file of it, offer a sequel with `dmc-new-campaign`; stop |
| `kit/skills/dmc-migrate-campaign/SKILL.md` § 1 | `closed: ...`: say it is closed and never migrated; go on to the next campaign |
| `kit/dm-rules.md`, Accuracy | One clause: a campaign whose folder holds `.closed` is closed; never change its files |
| `kit/settings.json` | Deny `Edit(/adventures/*/.closed)` |
