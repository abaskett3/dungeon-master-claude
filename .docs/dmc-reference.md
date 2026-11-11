# dmc reference

This reference is for anyone who runs `dmc` by hand, writes prompts that call it, or changes it. It covers every subcommand and option, what each prints, and its exit codes. `dmc --help` and each subcommand's `--help` are the authority; the examples below are from real runs of a developer build, with long folder paths shortened to `<folder>`.

## Running it

| Who | Path |
|---|---|
| A player | `dmc`, found on the `PATH`: `dmc install` copies the program to `%LOCALAPPDATA%\Programs\dmc` (Windows, on the user's `Path`) or `~/.local/share/dmc` (Linux, linked as `~/.local/bin/dmc`). `dmc play` also puts the program's folder first on the `PATH` of the Claude Code session it starts. The first install is the install line of the README (`tools/install.ps1`, `tools/install.sh`), which downloads a release, checks it and runs `dmc install`; later, `dmc update` replaces the program. |
| A developer's build | `artifacts/bin/dmc` (see [development.md](development.md)). `artifacts/bin/dmc install --force` installs it as the player's `dmc`. |

The prompts write `dmc <command> ...`. `Bash(dmc *)` and `PowerShell(dmc *)` are on the allow list in the DMC folder's `.claude/settings.json` (from `kit/settings.json`), so DMC never meets a permission prompt for it. The same file denies `dmc install`, `dmc refresh` and `dmc update` to DMC.

```
$ dmc --help
usage: dmc <command> [args]
  roll          roll dice: dmc roll 1d20+5 4d6kh3, or with a label and target "Pip, shot:2d20kl1+5@AC14"
  context-check read the hook's JSON on stdin; print a line when the context is large (never fails)
  map           draw a map for the player's browser: dmc map <map.json> [--png] [--open] [--measure A B]
  redraw        Stop hook: redraw the player's map if its file changed (exit 2 with one line if the file is refused)
  pdf           text and page pictures of a PDF: dmc pdf info|text|render|all <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]
  digest        condense Claude Code session logs for review: dmc digest list|show [SESSION ...] [--campaign NAME] [--after YYYY-MM-DD] [--folder DIR] [--logs DIR] [--out DIR]
  campaign      export or import a whole campaign as one file, and bring its files up to date: dmc campaign export|import|show|standard|backups|backup|verify|restore NAME-or-FILE
  play          start a play session in your DMC folder: claude --model sonnet --effort high [args]
  install       install dmc and write a DMC folder to play in: dmc install [--path FOLDER] [--force]
  folder        print the DMC folder this command would use
  refresh       bring a DMC folder's rules, skills, settings and templates up to date: dmc refresh [FOLDER | --all]
  update        replace dmc with the latest release and refresh your DMC folders: dmc update
  --version     print the version

$ dmc --version
dmc 0.0.0-dev
```

A release prints its own version (`dmc 0.1.2`); every other build prints `dmc 0.0.0-dev`. `-h` and `help` work like `--help`.

## Rules that apply to every command

**One-line errors.** Everything `dmc` prints lands in DMC's context, so a failure is always one line on stderr, `dmc <command>: <message>`, never a stack trace and never the usage text. A message that would contain a line break is folded onto one line. An unexpected failure is still one line, with the exception's type in brackets at the end.

```
$ dmc
dmc: no command given (try roll, context-check, map, redraw, pdf, digest, campaign, size, lookup, recall, play, install, folder, refresh, update, --version)
$ dmc foo
dmc: unknown command "foo" (try roll, context-check, map, redraw, pdf, digest, campaign, size, lookup, recall, play, install, folder, refresh, update, --version)
```

**Arguments.** `map`, `pdf`, `digest` and `campaign` read their arguments as Python's `argparse` would: an option may be shortened to a unique prefix (`--pa` for `--pages`), `--option=value` works, a negative number is a plain argument, and `--` ends the options. A mistake is one line and exit code 2.

**Exit codes.**

| Code | Meaning |
|---|---|
| 0 | Success. `context-check` always exits 0. |
| 1 | A failure the user can fix: a bad dice expression, a map with a mistake in it, a missing or unreadable file, no command or an unknown command. |
| 2 | A mistake in the arguments (missing, unknown or malformed), and `roll` when the operating system's random source cannot be read. |
| 127 | `play`: Claude Code is not on the PATH. |
| other | `play` passes on Claude Code's own exit code. |

**Output.** UTF-8 with no byte-order mark, and LF line ends on every system. A character that cannot be encoded prints as `?`.

**The DMC folder.** `dmc` finds `adventures/` and the Claude Code logs in the DMC folder, never relative to its own location: `CLAUDE_PROJECT_DIR` if that is a DMC folder (Claude Code sets it for hooks), else the current folder if it is one, else the first folder recorded in `dmc.json` beside the program that is still a DMC folder. A folder is a DMC folder when it has `.dmc/manifest.json`. A command that needs a folder and finds none fails with `dmc <command>: no DMC folder: run dmc install`. `dmc folder` prints the folder it finds.

## roll

```
dmc roll [label:]<dice>[@AC<n>|@DC<n>] [...]
dmc roll --help
```

Rolls each argument and prints one line per argument on stdout, errors included. An argument may carry a label and a target (see below); with neither it is a bare dice expression.

| Form | Meaning |
|---|---|
| `NdS` | N dice of S sides. N may be left out (`d6` is `1d6`). 1 to 100 dice, 2 to 1000 sides. |
| `NdSkhK` | Keep the highest K. Advantage is `2d20kh1+5`; ability scores are `4d6kh3`. |
| `NdSklK` | Keep the lowest K. Disadvantage is `2d20kl1+5`. |
| `+M`, `-M` | A modifier, up to 9223372036854775807. |

Upper case works too (`1D20`). Dice come from the operating system's random source (`/dev/urandom` on Linux).

```
$ dmc roll 1d20+5 2d20kh1+5 4d6kh3 3d6-2 d6
1d20+5: [3] +5 = 8
2d20kh1+5: [7, 7] keep [7] +5 = 12
4d6kh3: [2, 1, 1, 3] keep [3, 2, 1] = 6
3d6-2: [3, 1, 3] -2 = 5
d6: [6] = 6
```

Errors are printed in place of the expression's line; the good expressions are still rolled, and the exit code is 1:

```
$ dmc roll 1d20+5 xyz 0d6 2d6kh3
1d20+5: [8] +5 = 13
xyz: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)
0d6: out of range (1-100 dice, 2-1000 sides)
2d6kh3: cannot keep 3 of 2 dice
```

With no argument, it prints `usage: dmc roll [label:]<dice>[@AC<n>|@DC<n>] ...   e.g. 1d20+5 2d20kh1+5 "Pip, shot:1d20+5@AC14"` on stdout and exits 1. `--help` (or `-h`) prints the longer help and exits 0. If the random source cannot be read, it prints `dmc roll: cannot read /dev/urandom, no dice rolled` and exits 2.

### Label and target

An argument may be `label:dice@target` (added in release 0.1.2). Label and target are each optional. The label is everything before the first colon, so it may hold spaces and commas but no colon; quote the whole argument so the shell passes it as one. The target is `@AC` or `@DC` (any case) and a whole number, after the last `@`. The line is the plain line with the label before it and the verdict after:

```
$ dmc roll "Pip, shot:2d20kl1+5@AC14" "Kole, Dex save:1d20+3@DC13" "Krag, damage:2d6+4" 1d20+5
Pip, shot: 2d20kl1+5: [17, 9] keep [9] +5 = 14 vs AC 14, hit
Kole, Dex save: 1d20+3: [9] +3 = 12 vs DC 13, fail
Krag, damage: 2d6+4: [3, 5] +4 = 12
1d20+5: [12] +5 = 17
```

- An AC target says `hit` or `miss`; a DC target says `success` or `fail`. A total that meets the target succeeds, as in 5e.
- **Natural 20 and 1.** Against an AC only, a natural 20 hits and a natural 1 misses whatever the total, and the line says so: `1d20-5: [20] -5 = 15 vs AC 30, hit (natural 20)`, `1d20+30: [1] +30 = 31 vs AC 5, miss (natural 1)`. This applies only when exactly one d20 counts: a single d20, or the kept die under advantage or disadvantage (`2d20kh1`, `2d20kl1`). Two d20s added up have no natural roll. A DC has no such rule: checks and saves have no automatic success or failure in 5e, so there is no note and no override. The line does not compute critical damage.
- **Errors** are one line, start with the label, draw no dice and exit 1; the other arguments are still rolled. A bad target gives `Pip: 1d20: target "@14" is not @AC<n> or @DC<n> (try @AC14, @DC13)`. An empty label (`: 1d20`) or a line break in a label is refused.
- The bare syntax is untouched: an argument with no colon and no `@` prints exactly the line it always did.

CLAUDE.md § Dice has DMC call `dmc roll` with a label and target on every roll it makes and paste the line unchanged. If a campaign's house rule changes a verdict, DMC pastes the line unchanged and states the ruling after it, because `dmc` follows strict 5e.

## map

```
dmc map <map.json> [--png] [--open]
dmc map <map.json> --measure FROM TO
```

Checks a place or scene file, draws it, and prints the distance between every pair of creatures. The file format is in [`.claude/skills/draw-map/map-format.md`](../.claude/skills/draw-map/map-format.md), summarised in [campaign-files.md](campaign-files.md#the-map-files).

| Option | Effect |
|---|---|
| `--png` | Also write `live.png`, the same picture at the same pixel size as the SVG, for DMC to look at. |
| `--open` | Also open `live.html` in the default browser. Used once per session; afterwards the open page updates itself. |
| `--measure FROM TO` | Print only the distance between two cells, such as `B2 H4`, and draw nothing. The map is still loaded and checked, except for its turn order, which a stale `initiative` therefore cannot block. |

It writes next to the map file: `live.html` (the page the player keeps open), `live-data.js` (the current picture, rewritten on every run; the page polls it every two seconds), `live-source.txt` (the name of the map file just drawn and of its base place, which `dmc redraw` reads; from release 0.1.3) and, with `--png`, `live.png`.

```
$ dmc map maps/live.json --png
Drew "Cellar": 10 x 5 cells, 3 creatures.
Player's page: <folder>/maps/live.html
Picture to check: <folder>/maps/live.png
Distances (1 cell = 5 ft, straight line, walls ignored; 5 ft is adjacent):
  @ Hero (B2)  <->  1 Goblin (G4):  25 ft
  @ Hero (B2)  <->  2 Goblin archer (H2):  30 ft
  1 Goblin (G4)  <->  2 Goblin archer (H2):  10 ft

$ dmc map maps/live.json --measure B2 H4
B2 to H4: 6 squares, 30 ft (straight line, walls ignored)
```

**Turn order** (added in release 0.1.2). A scene may hold `"initiative"`, the creatures in the order they act, and `"current"`, whose turn it is. Each is a token's `id` (preferred) or the name of exactly one token. The picture, in `live.html` and `live.png`, then has a numbered "Turn order" list at the top of the panel beside the grid, each creature with its token's colour and mark and the current turn on a highlighted plate tagged `TURN`. Without `"initiative"` the picture is exactly as before. A scene does not inherit these keys from its base place. Mistakes are one line and nothing is drawn:

```
dmc map: "initiative" names "Goblin", the name of 2 tokens: use their ids (1, 2).
dmc map: "current" is "Pip", who is not in "initiative".
```

Distances are measured between the nearest cells of two creatures, in a straight line that ignores walls. At 5 ft and 30 ft they count squares the 5e way, a diagonal step costing the same as a straight one; at 30 ft they read "about N ft", and two creatures in one cell read "same square (within 30 ft)". On a mile-scale map they are the true straight-line distance between cell centres, "about N miles".

A map with a mistake in it is refused with one line and exit code 1, and nothing is drawn:

```
$ dmc map maps/bad.json
dmc map: <folder>/maps/bad.json: "cell_ft" must be a standard map scale in feet (5, 30, 5280 (1 mile), 31680 (6 miles), 316800 (60 miles)), not 10: every map is drawn to scale.
$ dmc map maps/live.json --measure B2 Z9
dmc map: --measure: Z9 is off the map (the map is A1 to J5).
$ dmc map maps/missing.json
dmc map: no such file: <folder>/maps/missing.json
```

Argument mistakes exit 2: `dmc map: the following arguments are required: map`, `dmc map: argument --measure: expected 2 arguments`, `dmc map: unrecognized arguments: --bogus`.

## redraw

```
dmc redraw < hook-input.json
```

Added in release 0.1.3. The play settings' `Stop` hook runs this when DMC has finished a reply. Claude Code passes a JSON object on stdin; `dmc` reads `stop_hook_active` from it (see [Claude Code's hooks reference](https://code.claude.com/docs/en/hooks), "Stop"). Run by hand at a terminal it does not wait for input, and acts as if `stop_hook_active` were false. It then:

1. finds the active campaign (`adventures/.active-campaign`) and its `maps/` folder, in the DMC folder;
2. does nothing, silently, if there is no active campaign, or no `live-data.js` yet and no `live.json` (DMC has not shown a map). With no page yet but a `live.json`, it draws that;
3. picks what to draw. A fight always uses `maps/live.json`, so if that is newer than `live-data.js` it is drawn, whatever the page showed before. Otherwise, if the record `maps/live-source.txt` (written by `dmc map`) names another file, such as a place DMC drew by hand for the player, that file is drawn again if it, or the base place the record gives, is newer than `live-data.js`. With no record (a page drawn by an older `dmc`) only `live.json` is checked, and a change to its base place alone is not noticed;
4. draws it exactly as `dmc map` does, which also rewrites the record, and refreshes `live.png` too if one already sits beside `live.html`. When it draws `live.json` and the page did not show it (no page yet, or the record names another file, as after a place map), it also opens `live.html` in the browser, as `dmc map --open` does (from release 0.1.4). It opens the page on that switch only, not on every redraw, so a page already showing the scene gets no second tab. It prints nothing even when it draws. If nothing is newer it does nothing, silently.

"Newer" is a strictly later modification time. A file edited in the same clock tick as the last drawing is missed until its next edit. With nothing changed the cost is the reads of two tiny files (`.active-campaign` and the record) and a few `stat` calls; no map file is parsed.

If `dmc map` would refuse the file, nothing is drawn and `dmc redraw` prints one line to stderr and exits 2, which for a Stop hook makes Claude Code give the line to DMC as feedback so that it corrects the file:

```
dmc redraw: live.json was not drawn, so the player's map is out of date: token @ (Thorin) is on A1, which is a wall. Fix it now; if it is still refused it will be reported again when your next reply ends.
```

When the input has `"stop_hook_active": true` (Claude Code is already continuing because a Stop hook asked it to), the file is still drawn if it is now fine, but a refusal is not reported a second time: it exits 0 with nothing printed, so a file DMC cannot fix cannot make it loop. Nothing is lost by that silence: a file that is still refused stays newer than the picture, so the Stop hook of the next reply, a new turn, finds it again and exits 2 again. The message says this. On any other failure `dmc redraw` prints nothing and exits 0, like `context-check`.

The hook command in `kit/settings.json` (a DMC folder's `.claude/settings.json`) is `out=$(dmc redraw 2>&1); s=$?; if [ "$s" = 2 ]; then printf '%s\n' "$out" >&2; exit 2; fi; exit 0`. Only exit 2 gets through, so a `dmc` that does not know `redraw` (0.1.2 prints `dmc: unknown command "redraw"` and exits 1), a missing `dmc` (exit 127) and a file that is not a program (exit 126) all end in a silent exit 0. A command-line option such as `dmc map --pending` would not have been safe: 0.1.2 treats it as a usage mistake, exit code 2, which would have blocked every reply.

## pdf

```
dmc pdf info   <pdf> [--pages 1-20,35]
dmc pdf text   <pdf> [--pages 1-20,35] [--out DIR]
dmc pdf render <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]
dmc pdf all    <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]
```

Writes the text and a picture of each page to disk, so DMC can read a book of any size with its Read tool.

| Subcommand | Does |
|---|---|
| `info` | Lists every page with its number of characters, number of images and size, then the pages with little or no text (under 200 characters: scans, maps, full-page art). Writes nothing. |
| `text` | Writes the text of each page. |
| `render` | Writes a JPEG picture of each page (quality 85). |
| `all` | Both. |

| Option | Effect |
|---|---|
| `--pages` | PDF page numbers, starting at 1, such as `1-20,35`. Default: every page. |
| `--dpi` | Resolution of the pictures, 1 to 1200. Default 110. |
| `--out` | Output folder. Default: `extracted/` next to the PDF. |

Output layout:

```
DIR/all-text.txt         every page's text, split by "===== PDF PAGE N =====" lines (only when every page is done)
DIR/text/page-NNN.txt    one file per page
DIR/pages/page-NNN.jpg   one picture per page
```

```
$ dmc pdf info sample.pdf
pages: 4
p1: 17 chars, 0 images, 8.5 x 11.0 in
p2: 490 chars, 0 images, 8.5 x 11.0 in
p3: 17 chars, 0 images, 8.5 x 11.0 in
p4: 17 chars, 0 images, 8.5 x 11.0 in
little or no text (scan, map or art; look at the page image): 1, 3, 4

$ dmc pdf text sample.pdf
text: 4 pages -> <folder>/extracted/text and <folder>/extracted/all-text.txt

$ dmc pdf all sample.pdf --pages 1
text: 1 pages -> <folder>/extracted/text
render: 1 pages at 110 dpi -> <folder>/extracted/pages
```

A PDF with only an owner password (print or copy restrictions) is read. A page that cannot be read gets a placeholder line and the others carry on.

**All or nothing.** `text`, `render` and `all` either finish or leave the output folder as it was. The run first makes the output folder, which fails at once if the path is a file or cannot be written to, and writes into a temporary folder inside it, `<out>/.dmc-partial-<random>`. Only when every page is done does it move the files into place, replacing files of the same name and leaving the others, and print its report lines. A page whose picture would be more than 65,500 pixels on a side at the `--dpi` asked for (the most a JPEG can hold) is refused before anything is drawn or made. If a run fails after that (a full disk, a page that will not encode, or something found at the end: a file where a folder must go, or a folder where a file must go, such as `out/text` being a file), it deletes the temporary folder and any folder it made, and prints only the error: no new files, and nothing already in the output folder replaced or deleted. The move into place is a series of renames, not one step, so a failure part way through it (a full disk, a read-only file in the way) can leave some files moved; the obstacles that can be seen coming are checked before the first rename. A `.dmc-partial-<random>` folder stays in the output folder, safe to delete, only if the process is killed (Ctrl+C, a power cut) or another program holds a file in it open so that it cannot be deleted; when cleaning up fails, one line on stderr (`dmc pdf: could not delete <folder>; it is safe to delete.`, before the error if the run failed, or `dmc pdf: done, but could not delete <folder>; ...` after the report if it succeeded) says where. The cost of all this is disk space: a run keeps its new files in the temporary folder while old ones are still in place, so a re-run needs room for both sets for a moment. `info` writes nothing. Errors, each one line:

| Message | Exit |
|---|---|
| `dmc pdf: This PDF is password-protected. Ask the player for an unlocked copy.` | 1 |
| `dmc pdf: This PDF is encrypted, and reading it needs OpenSSL (libssl), which is not installed.` (Linux only) | 1 |
| `dmc pdf: No such file: <path>` | 1 |
| `dmc pdf: Page range '9-12' is outside 1-4.` | 1 |
| `dmc pdf: --dpi must be a whole number from 1 to 1200.` | 1 |
| `dmc pdf: Page 2 could not be saved as a JPEG at 1200 dpi; try a lower --dpi.` (the page's picture would be over 65,500 pixels on a side, or would not encode; the output folder is untouched) | 1 |
| `dmc pdf: <out>/text is a file, so page-001.txt cannot be written into it. Move it away, then run this again. Nothing was written.` (or `... is a folder, so the file of that name cannot be written there ...`) | 1 |
| `dmc pdf: argument command: invalid choice: 'bogus' (choose from info, text, render, all)` | 2 |
| `dmc pdf: the following arguments are required: pdf` | 2 |

## digest

```
dmc digest list [--campaign NAME] [--after YYYY-MM-DD] [--folder DIR] [--logs DIR]
dmc digest show [SESSION ...] [--campaign NAME] [--after YYYY-MM-DD] [--folder DIR] [--logs DIR] [--out DIR]
```

Condenses Claude Code's session logs, which are far too big to read, for the `review-transcript` skill. Claude Code keeps them under `~/.claude/projects/<project>/` (or `$CLAUDE_CONFIG_DIR/projects/<project>/`), where `<project>` is the toolkit's path with every character outside `A-Z`, `a-z` and `0-9` replaced by `-`.

| Argument | Meaning |
|---|---|
| `SESSION` | The start of a session id, as `list` shows it. With none, `show` takes every session that touched the campaign (or every session). |
| `--campaign` | Only sessions that touched `adventures/<NAME>/`. |
| `--after` | Only sessions that ended on or after this date (UTC). |
| `--folder` | The DMC folder whose Claude Code logs are read and whose `adventures/` holds the default output folder. Default: the folder `dmc folder` prints. An error (`dmc digest: <folder> is not a folder`) when it does not exist. |
| `--logs` | Folder of session `.jsonl` files. Default: the Claude Code logs of the DMC folder. |
| `--out` | Output folder of `show`. Default `adventures/.transcript-review` in the DMC folder, one folder whatever the campaign, not in git. Each run rewrites `stats.md` and the files of the sessions it digests; files left by earlier runs for other sessions are not deleted. |

`list` prints a table, one session per row: id, start and end (UTC), size in KB, number of player messages, peak context, campaign and the first message.

```
$ dmc digest list --logs <empty folder>
id        start (UTC)       end (UTC)             KB  msgs peak ctx  campaign             first message
```

`show` writes these files, prints the statistics, and ends with the line `Wrote stats.md and, for each session, <id>-turns.txt and <id>-transcript.md to <folder>`:

| File | Holds |
|---|---|
| `DIR/stats.md` | The numbers for each session: context, tokens, tool calls, reads and writes, errors, compactions. |
| `DIR/<id>-turns.txt` | One line per player message: time, context size, the message, then the size and tool calls of the reply. Lines marked `!` may show friction. |
| `DIR/<id>-transcript.md` | The whole session in order, with tool calls and results cut to a line each. |

Times are UTC; "ctx" is the number of tokens sent to the model on that call. Errors include `dmc digest: No session logs found at <folder>. Pass --logs DIR.` and `dmc digest: No matching sessions with player messages.` (exit 1), and `dmc digest: argument command: invalid choice: 'digest' (choose from list, show)` (exit 2). A log `dmc` cannot read is reported with its file name, exit 1: `dmc digest: <file>.jsonl: not valid UTF-8`, or `dmc digest: <file>.jsonl line N: <what is wrong>` for the first record of the wrong shape. That includes an assistant record with a token count (`input_tokens`, `cache_read_input_tokens`, `cache_creation_input_tokens` or `output_tokens`) of `NaN` or `Infinity` (`a token count that is not a finite number`), an `output_tokens` that is not a number (`"output_tokens" is not a number`), or input and cache counts whose sum overflows. Every log the request loads is read before anything is printed or written, so one bad log stops `list` and `show` alike, with no heading, rows or files from the sessions before it. The session ids rule logs out by file name before they are read, but `--campaign` and `--after` are applied only after a log has been read, so a bad log is refused even if those would have left it out. (Before 0.1.3 a non-finite count ended in `(LogShapeException)`, after `show` had written the earlier sessions' files.) The subcommand `show` was called `digest` in 0.1.0.

## campaign

Added in release 0.1.4.

```
dmc campaign export NAME [--out PATH] [--force]
dmc campaign import FILE [--as NAME]
dmc campaign show FILE
dmc campaign standard [NAME] [--init]
dmc campaign backups NAME
dmc campaign backup NAME [--prune oldest|old]
dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]
dmc campaign restore NAME [--from FILE] [--yes]
```

Moves a whole campaign in and out of one file: a backup, a copy under another name, or a move to another computer. `export`, `import` and `show` work on one campaign and never delete or overwrite anything that was already there, except a file named by `--out` with `--force`. The other five verbs are the exact steps of bringing a campaign's files up to the kit's standard; see [Migrations](#migrations) below.

| Argument | Meaning |
|---|---|
| `NAME` | `export`: a folder under `adventures/`. |
| `FILE` | `import`, `show`: an export file. Its name does not matter: the campaign name is read from the manifest inside it, so a renamed export imports the same. |
| `--out` | `export`: an existing folder (the default file name is used inside it) or a file path ending in `.zip`. Default: `exports/` in the DMC folder, made when needed. Never inside `adventures/`. |
| `--force` | `export`: replace an existing file named by `--out`. It has no effect on the default name, which is never replaced (the next free `-2`, `-3`, ... is used). |
| `--as` | `import`: the folder name to import under; lowercase letters, digits and hyphens, starting with a letter or digit. Default: the campaign name inside the file. |

**export** writes one ZIP: `manifest.json` first, then the campaign under `campaign/`, and the general `adventures/player-options.md` as `player-options.md` when there is one. Every file is stored byte for byte (no line-ending or encoding change), empty folders are kept, and the manifest lists each file's size and SHA-256 with the module the campaign runs (read from the `**Module folder**:` line of `module-progress.md`). The default file name is `<NAME>-<YYYYMMDDHHmm>.zip` in local time to the minute, for example `the-silver-egg-202610220605.zip`; a second export in the same minute is `...-2.zip`. The file is written under a `.partial-` name and renamed when complete, so a file with its final name is always whole. Nothing under `adventures/` changes. The campaign's top-level `backups/` folder is left out, so an export is the same size however many backups the campaign keeps. The export holds the whole campaign, spoilers included (`story/dm-notes.md`). A published module is not exported: `modules/` is copyrighted and stays on its machine.

**import** checks the whole file before it writes anything: it is a ZIP with a manifest that has every field, its format is not newer than this `dmc`'s, every path is safe on every operating system (relative, `/` separators, no `..`, no `<>:"|?*`, no trailing dot or space, no `CON`, `NUL` and the other reserved names, no two paths that differ only by case), and the entries match the manifest exactly. Files under a top-level `backups/` are left out: an export never holds one, but an export made by an older `dmc` holds whatever `backups/` folder the campaign had, and the import says `left out backups/ (<n> files): an export made by an older dmc held it, and backups stay with their campaign` after its `imported` line (which counts the files written). It then extracts into `adventures/.dmc-import-<random>/`, checking each file's size and SHA-256 as it is written, and commits with renames on one volume: the general player options first, only when `adventures/player-options.md` does not exist, then the campaign folder. A campaign therefore appears whole or not at all. Every extracted file gets the export's time as its modification time, and a manifest that declares more than 100,000 files or 1 GB in all is refused. It never changes an existing campaign or an existing `adventures/player-options.md`, never replaces a folder of the same name, and never writes `adventures/.active-campaign`: to play the copy, start a session and ask DMC to resume it.

**show** prints what a file holds, from its manifest alone, and writes nothing.

```
$ dmc campaign export the-silver-egg
exported the-silver-egg: 78 files and the general player options to <root>/exports/the-silver-egg-202610220712.zip
$ dmc campaign show exports/the-silver-egg-202610220712.zip
the-silver-egg: exported 2026-10-22 14:12 UTC by dmc 0.0.0-dev, format 1
78 files, 1.3 MB, plus the general player options
module: peril-in-pinebrook (in modules/)
$ dmc campaign import exports/the-silver-egg-202610220712.zip --as silver-egg-copy
imported silver-egg-copy: 78 files to <root>/adventures/silver-egg-copy
general player options: kept your existing adventures/player-options.md
to play it, start a session and ask DMC to resume silver-egg-copy
```

`export` prints one line (`and the general player options` is left out when there is no general file). `import` prints up to four lines: the result; one for the general player options (`written to adventures/player-options.md` or `kept your existing adventures/player-options.md`, absent when the export holds none); one when the module is not in `modules/` (`module <folder> is not in modules/: ingest it from your own copy before play`, or `this is a module campaign, but its module-progress.md names no module folder`); how to play it; and, when the campaign is not on the kit's standard, `<name> is behind (standard C, kit K): ask DMC to bring it up to date` or `<name> is ahead (standard C, kit K): update dmc`. The imported files are unchanged either way. `show` prints two or three: name, time (UTC), `dmc` version and format; file count, size and whether the general player options are included; and, for a module campaign, `module: <folder> (in modules/)`, `(not in modules/)` or `module: unknown (module-progress.md names no module folder)`. The size is in bytes below 1 KB, KB (1024 bytes) below 1 MB, else MB, with one decimal and a point.

| Message (after `dmc campaign: `) | Exit |
|---|---|
| `give export, import, show, standard, backups, backup, verify or restore (try dmc campaign --help)` (no verb, or an unknown one) | 2 |
| `the following arguments are required: name` (or `file`), `unrecognized arguments: ...`, `argument --out: expected one argument` | 2 |
| `--as must be lowercase letters, digits and hyphens, starting with a letter or digit` | 2 |
| `no campaign "<name>" in adventures/ (there are: a, b)` (`(there are none)` when empty; folders starting with `.` are not campaigns) | 1 |
| `will not write an export inside adventures/: choose another --out` | 1 |
| `<path> is not an existing folder or a file name ending in .zip: choose another --out` (and `--out is empty: ...`) | 1 |
| `<path> exists: add --force to replace it, or choose another --out` | 1 |
| `<relative path> is a link: exports hold only the campaign's own files` (a symbolic link or a junction) | 1 |
| `<relative path> cannot be exported: <reason>` (a path an import would refuse) | 1 |
| `could not export: <reason>` (nothing is left under the final name) | 1 |
| `cannot read <path>: <reason>` (`no such file`, or the system's reason) | 1 |
| `<path> is not a dmc campaign export` (not a ZIP, no valid manifest, a field missing or of the wrong type) | 1 |
| `<path> was made by a newer dmc (format <n>): update dmc and try again` | 1 |
| `adventures/<name> already exists: import under another name with --as NAME` | 1 |
| `<path> holds an entry that cannot be imported: <entry> (<reason>)` (an unsafe or unportable path, a campaign name that is not one plain folder name, a duplicate when case is ignored) | 1 |
| `<path> is damaged: <entry> is not in the manifest`, `... is missing`, `... appears twice`, `... is larger than the size in the manifest`, `... is smaller than ...`, `... does not match its checksum in the manifest` | 1 |
| `could not import: <reason>` (a write failed; the staging folder is deleted and nothing is left under the target name) | 1 |

If cleaning up after a failure fails, one more line on stderr names the leftover staging folder (`dmc campaign: could not delete <folder>; it is safe to delete.`), as `dmc pdf` does; a successful import whose staging folder cannot be deleted says `dmc campaign: done, but could not delete <folder>; it is safe to delete.`

### Migrations

Added in release 0.3.1 (spec 005, issue #172). A **standard** is a whole number. The kit's standard is the highest
migration in `migrations/` of the DMC folder (`NNN-<slug>.md`, installed from `kit/migrations/`; 1 when there is none).
A campaign's is the number in `adventures/<NAME>/.standard`, or 1 when there is no such file. A migration takes a
campaign from `NNN-1` to `NNN`; DMC follows it (the `dmc-migrate-campaign` skill), and these verbs do the parts that
must be exact. The format of a migration file is `specs/005-campaign-migrations/contracts/migration-file.md`.

**Campaign files** are every file under `adventures/<NAME>/` except the top-level `backups/` folder and `.standard`:
`verify` and `restore` compare only these.

**standard** prints one line and changes nothing, except with `--init`. Without `NAME`, the active campaign.

| Output | Meaning |
|---|---|
| `current` | the campaign's standard equals the kit's |
| `behind: standard C, kit K: 002-slug.md, 003-slug.md` | the pending migrations, oldest first |
| `ahead: standard C, kit K: update dmc` | the campaign was written by a newer `dmc` |
| `unfinished: <backup file>` | a backup has a `.migrating` mark: a migration was started and not verified or restored (takes precedence) |
| `initialised NAME at standard K` | `--init` on a campaign with no `.standard` (`dmc-new-campaign` runs it) |

**backups** lists the counted backups, oldest first (by the UTC time in each one's manifest, so a clock going back
an hour does not misorder them; by the local time in the name when the manifest cannot be read), after a line
`N backups`. Counted means named
`<NAME>_backup_<yyyyMMddHHmmss>.zip` or `...-<n>.zip`; any other file in `backups/` is never counted or removed. When the
removal question is due, a last line holds it with the flag a yes allows: at exactly three,
`ask: may the oldest backup, <file>, be removed? yes: --prune oldest`; above three,
`ask: may the <n-2> oldest backups be removed, keeping the latest two? yes: --prune old`; below three, or while a
migration is unfinished, none. DMC relays the question and holds none of these numbers.

**backup** refuses while a migration is unfinished (`NAME has an unfinished migration from <file>: finish it or restore
it`). Otherwise it writes an export of the campaign to `adventures/NAME/backups/NAME_backup_<yyyyMMddHHmmss>.zip` (local
time; `-2`, `-3` on a clash; never replacing a file), checks it whole (manifest, sizes and the SHA-256 of every file),
writes `<file>.migrating` beside it when a migration is pending (a backup of a current campaign gets no mark, since
`verify` would have nothing to clear it with), and only then, with `--prune`, removes the oldest counted backup (`oldest`) or every
counted one before it but the latest two (`old`), each with its companions `<file>.accounting.tsv`, `<file>.verified.tsv`
and `<file>.migrating`. It prints `backed up NAME: <n> files to backups/<file>`, with `; removed <k> older` after a
prune and `; could not remove <files>` if a removal failed (the backup still stands). A failure before the mark removes
the new file and removes nothing.

**verify** compares the campaign with a backup: `--backup` (a file in `backups/` or a path; default the marked backup,
else the latest) and its accounting file (`--accounting`; default `backups/<file>.accounting.tsv`; a missing file is
empty). The pending migrations are those above the backup's `.standard`, up to the kit's. It passes only when:

1. the backup is whole and is a backup of NAME;
2. every **content unit** of the backup's campaign files is matched in the campaign or accounted for. A unit is a
   non-blank line of a `.md` or `.txt` file, trimmed, with runs of whitespace collapsed, a leading `- `, `* `, `+ `,
   `N. ` or `> ` and a heading's `#`s removed; table separator rows, horizontal rules and HTML comments are skipped. Any
   other file is one unit, `file <path>`. A unit is matched when the same text is a unit of the campaign, or, for one of
   20 characters or more, is contained in a longer one. Its id is the first 12 hex digits of the SHA-256 of its text;
3. each unmatched unit has a line `<id>\treworded\t<anchor>` (an anchor of at least 8 characters that occurs in the
   campaign) or `<id>\tdropped:<class>\t<reason>` (a class one of the pending migrations declares in `drops`);
4. the accounting file has no `skipped\t<file>\t<reason>` line (a file DMC left unmigrated);
5. every check the pending migrations name passes.

On a pass it writes `.standard` (the kit's standard) and `<file>.verified.tsv` (the path and SHA-256 of each campaign
file), removes every `.migrating` mark of the campaign (so `--backup` naming another backup leaves none), and prints `verified NAME: <u> units, <m> matched, <a> accounted, <n> dropped;
standard K` (`<a>` reworded, `<n>` dropped; `u = m + a + n`). On a failure it prints, then exits 1 with
`verification failed: <what>; <file> is untouched`:

```
unaccounted: <id> <file> <first 60 characters> [(anchor not found: ...) | (drop class X is not declared by the pending migrations)]
<n> more
skipped: <file> <reason>
check <name>: <what is wrong>
```

The first 40 unaccounted lines are printed, then `<n> more`; `--all` prints them all. `--list` prints the same lines
(`skipped: <file>` without the reason) and `<k> to account in backups/<file>.accounting.tsv`, writes nothing and exits 0:
this is how DMC gets the ids. A run that cannot verify is one line, exit 1: the campaign is ahead of the kit
(`NAME is ahead (standard C, kit K): update dmc`), `NAME has no backup: dmc campaign backup NAME takes one`,
`no backup <FILE>: ...`, a damaged backup, `<file> is a backup of <other>, not NAME`,
`no migration is pending: <file> is at standard B and the kit at K`, a bad accounting line
(`<file> line <n>: <what is wrong>`), or `cannot verify: two different units share the id <id> (...)`.

The checks (`MigrationChecks`): `sheets-end-with-history` (on each `character-sheets/pc-*.md` and `npc-*.md`, not the
`-history.md` files, a `## History`, `## Notes` or `## History with the PC` section is last, with only history-like
sections after it); `npc-sheets-have-names-line` (each `npc-*.md` has a "Names they use" line);
`no-dead-opposition-rows` (`story/dm-notes.md` has an Opposition table and no row whose second cell starts `0 /` or
`0/`); `fights-have-timeline` (each row that was finished in the backup is a row of `story/dm-notes-archive.md`, and a
line under `## Timeline` in `story/canon.md` has a session reference such as `s3.2` and the row's Who text, ignoring
case); `one-last-played-line` (`story/dm-notes.md` has at most one line starting Last played and no Mix tracker
section).

**restore** puts the campaign back as a backup holds it (`--from`: default the marked backup, else the latest). Without
`--yes` it prints the plan and changes nothing:

```
restore NAME from <file>: <r> files replaced, <d> removed, <a> added
warning: NAME has changed since the migration was verified (<c> files): those changes will be lost
confirm with --yes
```

The warning compares the campaign with `<file>.verified.tsv`, so it names play after the migration, not the
migration's own changes; with no such record it compares with the backup and says `has changed since the backup`. With
`--yes` it checks the backup whole, extracts it into `adventures/.restore-NAME-<id>/`, moves the campaign's `backups/`
into it, renames the campaign to `adventures/.replaced-NAME-<id>`, renames the staging into place, deletes the old
campaign and removes every `.migrating` mark; it prints `restored NAME from <file>: <n> files`. A failure before the
rename into place puts the campaign back (`could not restore NAME: <reason>; the campaign is as it was`). A run that
finds the campaign renamed aside by an interrupted restore puts it back first and says
`recovered NAME from an interrupted restore`. With `--yes` the backup is read once, checked as it is extracted.

**Recovery.** `standard`, `backups`, `backup`, `verify` and `restore` each first put back what an interrupted restore
left, and print `recovered NAME from an interrupted restore` before their own output, followed by `; <folder> ...` for
anything kept. A restore's staging and aside folders share an id, so they show how far it got. An aside with no
campaign folder goes back as the campaign. An aside beside a campaign folder, with its staging gone, means the restore
was done: its marks are removed and the aside deleted. An aside, a campaign folder and its staging together mean
something made a new campaign folder meanwhile: nothing is touched, and the command fails with `an interrupted restore
left NAME as adventures/<aside>, and adventures/NAME exists too: keep the one to play, delete the other, and run the
command again`. A staging's `backups/` goes back into the campaign's (entry by entry when the campaign has a
`backups/` again, never replacing a file), and the staging is deleted only when it holds none of them.
## size

Added in release 0.3.1.

```
dmc size [NAME] [--check] [--folder PATH]
```

Measures what a resume of a campaign reads, in estimated tokens (bytes ÷ 4): each file `dmc-resume-campaign` step 2 reads, chosen by that step's rules, and the part of it the resume reads. It never writes, moves or deletes anything.

| Argument | Meaning |
|---|---|
| `NAME` | A campaign folder under `adventures/`. Default: the active campaign (`adventures/.active-campaign`). A name starting with a dot is a staging folder and is refused. |
| `--check` | The form DMC runs in play: print only what to act on, one line each, at most 12 (the twelfth then says how many more), and nothing when all is well. |
| `--folder` | The DMC folder. Default: the one `dmc folder` prints. `review-transcript` passes it, since it runs in this repository. |

**What it reads.** Both player-options files, `game-state.md`, `story/summary.md`, `story/dm-notes.md`; every PC's sheet, each companion's (a `###` block under Party status other than "With the party, not companions") and each NPC's whose title, first or last word is in the Present line or the Combat tracker, each down to its history when that is its last section (not `minor-npcs.md` or a `-history.md` file); the latest `story/sessions/session-NNN.md` from its third-last scene, or whole when it has fewer than three or the last full save does not cover it; `story/canon.md`, whole under its limit and otherwise only its always-read sections (the search the resume also does is not counted); and for a module campaign `module-progress.md`, the module's `index.md` and the digest section it names. A missing file is listed and counts nothing. A toolkit test keeps these rules in step with the skill.

**What it flags.** A part read of 20,000 estimated tokens or 1,600 lines (near the Read tool's cap); a file over its budget, or a section of `dm-notes.md` over its own; canon over its limit; more than one Last played line or a Mix tracker; Opposition rows with none left alive; DONE or RESOLVED lines outside the Arc plan; a Prices, debts and promises section over 3,000; and lines over 400 characters in a trimmed file's content. The budgets are in [campaign-files.md](campaign-files.md#how-big-the-files-may-be).

```
$ dmc size --check
story/canon.md 21.2k is over its 19k limit: read it in sections; a full save archives it down to 16k (move about 5.2k)
story/dm-notes.md 20.5k is near the Read cap (25k): it may be cut short at resume
story/dm-notes.md 20.5k is over its 10k budget
story/dm-notes.md Opposition: 4 rows with none left alive
```

The full report has a header line, one line per file (path, tokens, budget or limit, what is read), a `total` line, `largest sections:` (at most four per file read whole, `(+N more)`), and `flags:` with the same lines as `--check` or `nothing to flag`; about 40 lines for a long campaign. Tokens are shown below 1,000 as a whole number and above as thousands with one decimal, rounded up. A line about a file the full save never trims (a session log near the cap) ends `(not the full save's to trim)`, and the full save leaves it. The largest sections of canon are those of the whole file, even when it is read in sections. A sheet's name is matched on its whole title, or on its first or last word unless that is an article, a preposition or a title such as Captain or Sister. The name and the `--check` lines are relied on by issue #172's migration, which runs `dmc size NAME --check` after migrating: a change to them is a change to that contract too. A measured campaign exits 0 whatever it flags; a failure is one line and exit 1 (`no active campaign: name one (dmc size NAME)`, `no campaign <NAME> in adventures/`, `<path> is not a DMC folder`, `cannot read <path>: <reason>`), a usage mistake exit 2.

## lookup

Added in the next release (spec 009, issue #179).

```
dmc lookup SOURCE HEADING... [--folder PATH]
```

Prints one section of a markdown file exactly as written, found by its heading: from the heading down to the line before the next heading of the same or a higher level, sub-sections included. It never writes, moves or deletes anything. DMC runs it in play for a module's area, NPC or stat block and for a rules entry (`kit/dm-rules.md`); the resume and the full save do not use it.

| Argument | Meaning |
|---|---|
| `SOURCE` | A campaign folder in `adventures/`, a module folder in `modules/`, or `rules`. `/<path>` after it searches one file (`new-road/story/canon.md`); a leading `adventures/` or `modules/` says which, when a campaign and a module share a name. A name starting with a dot is refused. |
| `HEADING...` | The heading's words, as one argument or several. |
| `--folder` | The DMC folder. Default: the one `dmc folder` prints. |

**What it searches.** A campaign: its `*.md` outside `import/`, `exports/`, `backups/` and dot folders. A module: `index.md`, `chapters/` and its other top-level `*.md` (`monsters.md`, `npcs.md`, `items.md`), never `source/`. `rules`: its markdown other than `index.md`, outside `extracted/`; when there is none, the reference is a PDF (below).

**Matching.** Headings are ATX headings (`#` to `######`) outside fenced code blocks. A heading matches when its text equals the request ignoring case, `*`, `_`, `` ` ``, punctuation, links' brackets and a trailing page reference in parentheses (`(PDF 212)`, `(p. 34)`); when none does, a heading holding every word of the request (by the stems `dmc recall` uses) matches. There is no fuzzy matching, so a near miss never prints the wrong stat block.

**Output.** One match: a header line, `<file>:<first>-<last>` (relative to the DMC folder), then the section's lines with LF line ends and nothing else changed. Several: one line each, `<file>:<line>: <heading>`, at most 8, then `(+N more)`; ask again with more words or the `/<path>` form. A section of 20,000 estimated tokens or 1,600 lines (the mark `dmc size` uses for the Read tool's cap) prints one line instead: `<file>:<first>-<last>: section <heading> is <tokens> tokens, <lines> lines: read it in parts`.

**A rules reference kept as a PDF** (no markdown besides `rules/index.md`): the lines of `rules/index.md` that hold every word of the request, at most 8, as `rules/index.md:<n>: <line>`; when none does and the PDF's text was extracted, one line naming the pages whose text holds the request as a phrase, at most 10: `rules: "fireball" is on PDF pages 131, 132 (read them in rules/<file>.pdf)`. It never prints text from the extracted pages, whose entry ends are guesses and whose digits can drop.

All of these exit 0. A failure is one line and exit 1: `no heading "<heading>" in <source> (nearest: <a>; <b>; …)`, `<name> is both a campaign and a module: say adventures/<name> or modules/<name>`, `no source <name>: not a campaign, a module or rules (campaigns: …; modules: …)`, `<name> is not a source (a folder starting with a dot is a staging folder)`, `no <path> in <source>`, `rules: no rules reference in rules/ (set one up with /dmc-setup-rules)`, `rules: no "<words>" in rules/index.md or its pages`, `cannot read <path>: <reason>`, and the DMC folder failures every command has. A usage mistake exits 2.

## recall

Added in the next release (spec 009, issue #179).

```
dmc recall [NAME] WORDS... [--folder PATH]
```

Prints the lines that best match some words across a campaign, the campaigns its Predecessor lines name and their modules, best first, in at most 6,000 bytes (about 1,500 estimated tokens). It builds its index in memory on every call and stores nothing, so it cannot go stale; it never writes, moves or deletes anything. DMC runs it in play when a subject returns or a fact from an earlier session or campaign is needed (`kit/dm-rules.md`); the resume and the full save do not use it.

| Argument | Meaning |
|---|---|
| `NAME` | A campaign folder in `adventures/`. It is the campaign only when it names one and at least one more argument follows; otherwise every argument is a word and the campaign is the active one. A misspelled name is therefore searched as a word. |
| `WORDS...` | What to search for, as one argument or several. |
| `--folder` | The DMC folder. Default: the one `dmc folder` prints. |

**What it searches.** The campaign's `*.md` outside `import/`, `exports/`, `backups/` and dot folders; each campaign named by a `- **Predecessor**:` line in `player-options.md`, followed to the end of the chain (a loop once; backticks, `adventures/` and slashes allowed; `none` ends it); and each of those campaigns' modules, named by `- **Module folder**:` in `module-progress.md`, searched once, never their `source/`. A line is a unit unless it is under 25 characters, a heading, a comment line, a table separator or inside a fenced code block.

**Ranking.** Okapi BM25 over line units (k1 1.4, b 0.6) on words lower-cased, with about 100 common English words dropped and a small suffix stripper (`'s`, then `ation`, `ion`, `ing`, `ed`, `es` or `s` when four letters stay), times the file's weight: `story/canon.md` 1.30; the archives and `-history.md` files 1.20; the sheets 1.15; the other campaign files 1.05; session logs 1.00; a module 0.80; each step back the Predecessor chain × 0.95. These are the settings of the issue's recall prototype, to be tuned if the recall test (below) shows a fall. A line matching about as well in canon comes before one in a session log, but a strong session-log match can beat a weak canon one.

**Output.** First, at most three notices, then `(+N more skipped)`: `<campaign>: Predecessor <name> is not in adventures/, not searched`, `<campaign>: module folder modules/<name>/ not found, not searched`, `<path>: cannot be read (<reason>), skipped`. Then one line per hit:

```
<campaign or modules/<name>> <file>:<line> [<sN.M>] [DM] | <text>
```

The session is the scene's `sN.M` for a session-log line, or the first `sN` or `sN.M` the line cites. `DM` marks a line only the DM may see: `dm-notes.md`, `dm-notes-archive.md`, `module-progress.md`, a module, or an NPC sheet's Secrets section. A line over 700 characters shows the 700-character window around its matching words, with `…` where it was cut. When more lines match than fit, the last line is `(N more lines matched)`; when none does, the only line is `nothing matched: <words>`. Both exit 0.

A failure is one line and exit 1: `nothing to search for in "<words>": every word is a common one`, `<name> is not a campaign (a folder starting with a dot is a staging folder)`, `no active campaign: name one (dmc recall NAME WORDS)`, `the active campaign <name> is not in adventures/`, and the DMC folder failures. A usage mistake exits 2.

**The recall test.** `tools/recall-test.ps1` runs recall for each question of a questions file and counts the answers found; see [development.md](development.md#the-recall-test).

## context-check

```
dmc context-check < hook-input.json
```

The play settings' `UserPromptSubmit` hook runs this before each player message. Claude Code passes a JSON object on stdin whose `transcript_path` names the session's log. `dmc` reads the log from the end and finds the context size of the latest call. A break is due when the context is large, and "large" is relative to what the session loaded: the first band starts at the larger of 150,000 tokens and the **baseline** plus 40,000, and a band follows every 40,000 tokens after that. Below the first band it prints nothing. From it on it prints one line, which Claude Code adds to DMC's context; the band it names rises every 40,000 tokens, so a declined break is offered again at the next band.

**The baseline** is where the context settled after the session loaded its files. `dmc` finds it from the log alone, in the part after the latest compaction boundary (`/clear` starts a new log, so it needs no rule). It is the context of the last assistant call before the first player message in that part: after `/clear` and `/resume-campaign`, the end of the recap; after a compaction, the end of the reload that follows it, if that comes before the player's next message. If no call comes before that message, it is the context of the first reply to it. If no player message has come yet, it is the latest context.

A player message is a user record with a text block that is typed text: not a tool result, a meta record, a compaction summary, a sub-agent's message, a slash-command echo, a shell-command record (`<bash-input>`), a task notice, an interruption marker (`[Request interrupted`) or another injection. Any text block of the message may be the typed text. `dmc digest` shares the injection test.

This needs no marker for the resume, so it holds however the files were loaded: the slash command, DMC's `Skill` call or reads one by one. A resume the player runs later in the session comes after a player message, so it cannot move the baseline up and hide that the context has grown. Limit: after an automatic compaction, DMC reloads after the player's next message, so the baseline is the small first reply and the 150,000 floor applies.

A resume that settles at 142,000 tokens therefore puts the first band at 182,000, not 150,000. A session that never loads much keeps the 150,000 floor. The size is found by reading the log from the end and stopping at the first call. The baseline is found by reading from the start and parsing only the records up to the first player message (and, past it, only lines that could be a compaction boundary), so the cost is mostly one pass of reading: on the largest local log, 18.3 MB, the command took about 72 ms against 51 ms for a run with no log, and on a 1.3 MB log about 60 ms.

```
$ echo '{"transcript_path":"<folder>/session.jsonl"}' | dmc context-check
Context 197k is large, break due since 190k: at the next break point, offer a full save and a fresh session, unless the player has declined since 190k.
```

The bands are counted from the first band, so a break the player declined is offered again after another 40,000 tokens. A compaction, which restarts the search for the baseline, starts the count again.

This changed in release 0.1.3. Release 0.1.2 and earlier use a flat 150,000 and word the line `Context 197k, break due since 190k: ...`.

It never fails: on any error (input that is not JSON, a missing or unreadable log) it prints nothing and exits 0. 

## play

```
dmc play [args...]
```

Starts a play session in the DMC folder (contract `dmc-play.md` of the spec in `specs/003-separate-player-install/`). It does these in order:

1. removes what an earlier update left in the program's folder (`*.old`, `.update-*`), quietly;
2. finds the DMC folder: the current folder if it is one, else the first folder in `dmc.json` that still is. `CLAUDE_PROJECT_DIR` does not choose it (that variable is for hooks);
3. refreshes the folder first if its manifest's version is not this `dmc`'s, or a generated file is missing or differs from the program's copy, printing the refresh's own lines (see [refresh](#refresh)). A hand-edited generated file is put back, and the edited copy is kept in `.dmc/replaced/<stamp>/`;
4. finds Claude Code;
5. starts it in the DMC folder, with this program's folder first on the child's `PATH`, as

```
claude --model sonnet --effort high [args...]
```

with every argument passed on unchanged and last (`dmc play --continue` works, and `dmc play --help` is Claude Code's help). The rules, skills and settings load because the folder is a Claude Code project, so no `--settings` is passed. It waits for Claude Code, ignores Ctrl+C while it runs (Claude Code handles it), and exits with Claude Code's exit code.

On Windows it starts `claude.exe` from the PATH, or failing that npm's `claude.ps1` through PowerShell. It never starts npm's `claude.cmd` through `cmd.exe`, which would re-parse the arguments.

| Message | Exit |
|---|---|
| `dmc play: no DMC folder: run dmc install` | 1 |
| `dmc play: Claude Code (claude) is not on the PATH: install it, then run this again` | 127 |
| `dmc play: only <path>\claude.cmd was found, and dmc does not start Claude Code through cmd.exe: install Claude Code's claude.exe` | 1 |
| the refresh's message, such as `dmc play: cannot write <path>: <reason>` | 1 |

`dmc play` is not refused inside a Claude Code session: it refreshes before Claude Code starts, so there is no session yet.

## install

```
dmc install [--path FOLDER] [--force]
```

Installs `dmc` for the current user and writes a DMC folder (contract `dmc-install.md`). In order:

1. **Refuses first** inside a Claude Code session (`CLAUDECODE` set): `dmc install: cannot run inside a Claude Code session: run it in a terminal`. Nothing is read or written.
2. Removes leftovers of an earlier update from the program folder, quietly.
3. **The folder.** `--path FOLDER` if given; otherwise it asks `DMC folder [<home>/dmc]: ` and Enter takes the default. A leading `~` means the home folder, and a relative answer is relative to the current folder. When standard input is not a terminal (Linux, `curl ... | sh`) the answer is read from `/dev/tty`; with no terminal at all the default is used without asking. On Windows the console is standard input.
4. Refuses a folder that is a file, or inside a checkout of the source repository (a `kit/dm-rules.md` in it or above it), and, before writing anything, a first install over a folder where a file it would write already exists and was not written by `dmc` (it names the first).
5. **The program.** A release build that is not already running from the program folder copies itself and its native libraries (every `*.dll` or `*.so` beside it) there: `%LOCALAPPDATA%\Programs\dmc` on Windows, which is added to the user's `Path` (registry value `Path` under `HKEY_CURRENT_USER\Environment`, kept as an expandable string, followed by a `WM_SETTINGCHANGE` broadcast); `~/.local/share/dmc` on Linux, with the link `~/.local/bin/dmc`. A development build (`0.0.0-dev`) copies itself only with `--force`.
6. **The folder's files.** Writes the generated files and `.dmc/manifest.json` with the same rules as [refresh](#refresh); over a folder that already has a manifest it behaves as `dmc refresh`. Makes `adventures/`, `feedback/`, `modules/` and `rules/` if missing, and writes nothing in them.
7. **Records** the folder in `dmc.json` beside the program (once; order kept): in the program folder when the program was installed or already runs from there, otherwise beside the running build.

```
dmc install: installed dmc 0.2.0 in <program folder>          (only when the program was copied)
dmc install: DMC folder ready at <folder>
dmc install: open a new terminal, then run dmc play            (when the program was copied; on Linux, when ~/.local/bin is not on the PATH:)
dmc install: add ~/.local/bin to your PATH (for example in ~/.profile), then run dmc play
```

At most three lines, and, over a folder that already had a manifest, the refresh's lines (at most two, prefixed `dmc install:`) before the second one. Errors, each one line on stderr, exit 1 unless said:

| Message | When |
|---|---|
| `dmc install: cannot run inside a Claude Code session: run it in a terminal` | `CLAUDECODE` is set |
| `dmc install: <folder> is inside the DMC source repository: choose another folder` | a `kit/dm-rules.md` in or above the folder |
| `dmc install: <file> already exists and was not written by dmc: move it, or choose another folder` | a first install over a foreign file |
| `dmc install: <folder> is a file, not a folder` | the folder is a file |
| `dmc install: cannot write <path>: <reason>` | a folder, the link or a file cannot be written |
| `dmc install: could not add <program folder> to your PATH: <reason>` | Windows, after the folder is written |
| `dmc install: unrecognized arguments: <argument>`, `argument --path: expected one argument` | usage mistakes, exit 2 |

## refresh

```
dmc refresh [FOLDER | --all]
```

Rewrites the generated files of a DMC folder from the copy inside this `dmc`, and nothing else (contract `dmc-refresh.md`). The folder is the one given, or with `--all` every folder in `dmc.json` that is still a DMC folder, or else the folder `dmc folder` finds. For each file of the program's table (`kit/dm-rules.md` is `CLAUDE.md`, `kit/settings.json` is `.claude/settings.json`, `kit/skills/**` is `.claude/skills/**`, `kit/templates/**` is `templates/**`, `kit/migrations/**` is `migrations/**`):

| State of the file | What happens |
|---|---|
| missing | written |
| same as the program's copy | left |
| same as the manifest's hash (an older generated version) | replaced |
| different from both (edited by hand) | copied to `.dmc/replaced/<yyyyMMdd-HHmmss>/<path>`, then replaced |
| in the old manifest, not in the program, unchanged | removed (and the folder it leaves empty under `.claude/skills/`) |
| in the old manifest, not in the program, edited | moved to `.dmc/replaced/` |
| exists, no manifest at all (a first install) | refused, naming it; nothing is written |

The manifest is written last, so an interrupted refresh leaves the old one and the next refresh repairs the folder. Nothing under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/` is ever written, replaced or removed, whatever the table or a manifest says. Like `install`, it refuses first inside a Claude Code session.

| Output (stdout, at most three lines per folder) | When |
|---|---|
| nothing | nothing to do |
| `dmc refresh: <folder>: <n> files updated for dmc X.Y.Z` | files written, replaced or removed |
| `dmc refresh: <folder>: <n> edited files kept in .dmc/replaced/<stamp>/` | hand edits were kept |
| `dmc refresh: <folder>: <n> campaigns are behind the new rules: ask DMC to bring them up to date` (`1 campaign is behind ... bring it up to date`) | files were updated, and campaigns of the folder are behind the new `migrations/` or have an unfinished migration |

| Message (stderr, exit 1 unless said) | When |
|---|---|
| `dmc refresh: cannot run inside a Claude Code session: run it in a terminal` | `CLAUDECODE` is set |
| `dmc refresh: <folder> is not a DMC folder: run dmc install` | the folder given has no manifest |
| `dmc refresh: no DMC folder: run dmc install` | no folder given and none found |
| `dmc refresh: cannot write <path>: <reason>` | the old manifest stays, so the next refresh repairs the folder |
| `dmc refresh: unrecognized arguments: <argument>`, `a folder and --all cannot be used together` | usage mistakes, exit 2 |

## folder

```
dmc folder
```

Prints the DMC folder `dmc` would use (see [The DMC folder](#rules-that-apply-to-every-command)), one line, exit 0; or `dmc folder: no DMC folder: run dmc install` on stderr, exit 1. The developer skills `review-feedback` and `review-transcript` use it, and `dmc digest` when it is given no `--folder`.

## update

```
dmc update
```

Replaces the installed program in place with the latest release, then rewrites the generated files of every DMC folder (contract `dmc-update.md`; the trust model is in [releases.md](releases.md#how-a-release-is-trusted) and [decisions.md](decisions.md)). In order:

1. **Refuses first** inside a Claude Code session (`CLAUDECODE` set), then a development build (`0.0.0-dev`), before anything is read. Leftovers of an earlier update (`*.old`, `.update-*`) are removed if possible.
2. Reads the latest release of `abaskett3/dungeon-master-claude` from the GitHub API (HTTPS). Its tag is `dmc-vX.Y.Z`. If that is not newer than this `dmc` (compared as numbers), it says so and stops, after refreshing any recorded DMC folder that is not what this program would write.
3. Downloads `SHA256SUMS.txt` and `SHA256SUMS.txt.sig` and verifies the signature (ECDSA P-256 over SHA-256, DER) with the two public keys built into `dmc`; either key is enough. Then downloads the archive for this machine (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`): 15 seconds to connect, no data for 30 seconds ends it, 10 minutes at most. Its SHA-256 must match both the signed list and the `digest` the API reports for the asset, before anything is unpacked. No setting and no environment variable skips a check or changes the keys.
4. Unpacks into `<program folder>/.update-<random>/`, runs the new `dmc --version` (it must say `dmc X.Y.Z`), swaps the files (each old file is renamed to `*.old`, the new ones are moved in, the program last; a failure moves back what was moved), then runs the new `dmc refresh --all` and passes its lines through. Before the swap nothing has changed. Player files are never touched.

For the tests, `DMC_UPDATE_TEST_ASSETS=<folder>` takes the release from a local folder instead of GitHub: `latest.json` (the shape of the API's answer: `tag_name` and `assets` with `name` and `digest`), the archives, `SHA256SUMS.txt` and its `.sig`. The signature and hash checks still apply.

| Output (stdout) | Exit |
|---|---|
| `dmc update: installed dmc X.Y.Z (was A.B.C)`, then the refresh's lines | 0 |
| `dmc update: dmc X.Y.Z is already the latest` (X.Y.Z is this program's version), then the lines of refreshing any stale folder | 0 |

| Message (stderr) | When |
|---|---|
| `dmc update: cannot run inside a Claude Code session: run it in a terminal` | `CLAUDECODE` is set |
| `dmc update: this is a development build (0.0.0-dev): install a release to update` | a build that is not a release |
| `dmc update: could not reach GitHub: <reason>` | no network, the API or a download fails, stalls or answers with an error (including its limit for requests without sign-in), the answer is not a release, or the latest tag is not `dmc-vX.Y.Z` |
| `dmc update: the latest release has no archive for <rid>` | the release has no archive for this machine |
| `dmc update: release X.Y.Z has no signature: nothing was installed` | the release has no `SHA256SUMS.txt.sig` (or no `SHA256SUMS.txt`) |
| `dmc update: release X.Y.Z is not signed by the DMC release key: nothing was installed` | the signature is invalid or made by an unknown key |
| `dmc update: the download does not match the signed checksums (or GitHub's digest): nothing was installed` | the archive's hash differs from the signed list or the digest, or either does not name it |
| `dmc update: could not unpack <archive>: <reason>` | the archive (whose hash matched) cannot be unpacked |
| `dmc update: the new dmc does not run: nothing was installed` | the new program is missing, fails, or says another version |
| `dmc update: cannot write <folder>: <reason>` | the program folder is not writable, or the swap failed (and was undone) |
| `dmc update: installed dmc X.Y.Z, but the DMC folder was not refreshed: <reason>; dmc play will refresh it` | the new program's refresh failed; the program stays installed |
| `dmc update: unrecognized arguments: <argument>` | usage mistake, exit 2 |

All of them exit 1 unless said.
