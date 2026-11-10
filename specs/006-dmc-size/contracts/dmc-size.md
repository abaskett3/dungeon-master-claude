# Contract: `dmc size`

The command's interface, as the prompts, the player and the tests see it. The entities and rules behind it are in
[../data-model.md](../data-model.md); the decisions in [../research.md](../research.md).

## Synopsis

```
dmc size [NAME] [--check] [--folder PATH]
```

| Argument | Meaning | Default |
|---|---|---|
| `NAME` | a campaign folder in `adventures/` | the active campaign, `adventures/.active-campaign` |
| `--check` | the play form: print only what to act on, nothing when all is well | the full report |
| `--folder` | the DMC folder | the one `dmc folder` prints |
| `--help` | print the usage and what the report means | |

Options are read as `dmc digest` and `dmc campaign` read theirs (`Argparse`): a unique prefix works, `--folder=PATH`
works, a mistake is one line and exit 2.

## Exit codes

| Code | When | Output |
|---|---|---|
| 0 | the campaign was measured, with or without flags | the report (full) or the flag lines (`--check`, possibly none) |
| 1 | a failure the user can fix | one line on standard error: `dmc size: <message>` |
| 2 | a usage mistake | one line on standard error: `dmc size: <message>` |

The command never writes, moves or deletes a file.

## Failures (exit 1)

| Situation | Message |
|---|---|
| no DMC folder | `dmc size: no DMC folder: run dmc install` |
| `--folder` is not a DMC folder | `dmc size: <path> is not a DMC folder` |
| no `NAME` and no active campaign | `dmc size: no active campaign: name one (dmc size NAME)` |
| `NAME` is not a campaign folder | `dmc size: no campaign <NAME> in adventures/` |
| `NAME` starts with a dot | `dmc size: <NAME> is not a campaign (a folder starting with a dot is a staging folder)` |
| a file cannot be read | `dmc size: cannot read <path>: <reason>` |

## Full report

Plain text, UTF-8 without a byte-order mark, `\n` line ends, at most about 40 lines for the long campaign. Tokens are
shown as thousands with one decimal (`6.6k`); below 1,000 as a whole number (`812`). The layout, for a campaign over
in places:

```
rorns-landing: what a resume reads, in estimated tokens (bytes / 4)
  adventures/player-options.md          1.1k
  player-options.md                     1.6k
  game-state.md                         6.6k   budget 4k     over
  story/summary.md                      5.1k   budget 3k     over
  story/dm-notes.md                    20.4k   budget 10k    over, near the Read cap
  character-sheets/pc-shalefire...md    5.4k   budget 6k     to its history
  character-sheets/npc-kibil-rornson.md 3.5k   budget 4k     to its history
  ...
  story/sessions/session-039.md          824                 whole (fewer than 3 scenes)
  story/canon.md                       15.5k   limit 19k     read whole
  total                                66.0k
largest sections:
  game-state.md: Party status 2.1k, Plan and standing orders 1.5k, Quests 886, Pending 773 (+8 more)
  story/dm-notes.md: Opposition 4.5k, The five stages and the hero's journey 2.8k, Open threads 2.8k, Mix tracker 2.4k (+9 more)
  ...
flags:
  story/dm-notes.md 20.4k is near the Read cap (25k): it may be cut short at resume
  story/dm-notes.md 20.4k is over its 10k budget
  story/dm-notes.md Mix tracker: a log of sessions, where one Last played line belongs
  story/dm-notes.md: 10 lines marked DONE or RESOLVED
  story/dm-notes.md: 69 lines over 400 characters
  ...
```

With nothing to flag, the `flags:` block is the single line `nothing to flag`. When canon is over its limit, its line
says `limit 19k   read in sections` and counts only its always-read sections, and a line under it says
`(the search for the scene's names is not counted)`. A missing file shows `missing`, a sheet that cannot be found
`not found`, a module chapter that is not ingested `not ingested`.

## `--check`

One line per flag, the same wording as the full report's flag lines, at most 12; past that, the twelfth line is
`... and N more: run dmc size NAME`. Nothing at all when there is no flag. A canon over its limit always comes first,
because the resume acts on it before reading:

```
story/canon.md 21.2k is over its 19k limit: read it in sections; a full save archives it down to 16k (move about 5.2k)
story/dm-notes.md 20.4k is near the Read cap (25k): it may be cut short at resume
```

A line about a file the full save never trims (a session log or module file near the cap) ends `(not the full save's to trim)`.

How the prompts act on the lines:

| Line | Resume (`dmc-resume-campaign` step 2) | Full save (`dmc-campaign-full-save` step 7) |
|---|---|---|
| canon over its limit | reads canon in sections (item 7) | archives at least the amount the line names, in one go (step 4), runs the check again |
| near the Read cap | reads that file in parts if it comes back cut; tells the player in one line out of character | trims the file |
| over its budget, a section over its budget | ignored | trims dead text; or names it in the step 8 report with why it stays |
| a bloat flag | ignored | fixes it; or names it in the step 8 report with why it stays |

## Stability

Issue #172's migration (`specs/005-campaign-migrations/`) runs `dmc size NAME --check` after a migration and reads its lines. It merges after this feature and adds that step itself. The subcommand's name, `--check`, its silence when all is well and the wording of its lines are therefore a contract with it: a change to any of them is made with that feature in mind and said in the PR.

## What the prompts may write

A prompt names the command as `dmc size <campaign> --check` (play) or `dmc size --folder <dmc folder> <campaign>`
(`review-transcript`). The toolkit tests check that `size` is a real subcommand and that each form is allowed by
`kit/settings.json` (`Bash(dmc *)`, `PowerShell(dmc *)`, already there) or `.claude/settings.json` (gains
`Bash(dmc size *)` and `PowerShell(dmc size *)`).

## Library surface (for tests and issue #172)

`Dmc.Size.SizeReport.Measure(string dmcFolder, string campaign)` returns the read set's measurements and flags, with
no output and no writes. `SizeCommand` formats it. The migration verification of issue #172 calls `Measure`
in-process.
