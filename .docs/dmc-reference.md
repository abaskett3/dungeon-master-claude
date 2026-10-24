# dmc reference

This reference is for anyone who runs `dmc` by hand, writes prompts that call it, or changes it. It covers every subcommand and option, what each prints, and its exit codes. `dmc --help` and each subcommand's `--help` are the authority; the examples below are from real runs of a developer build, with long folder paths shortened to `<folder>`.

## Running it

| Who | Path |
|---|---|
| A player | `dmc`, found on the `PATH`: `dmc install` copies the program to `%LOCALAPPDATA%\Programs\dmc` (Windows, on the user's `Path`) or `~/.local/share/dmc` (Linux, linked as `~/.local/bin/dmc`). `dmc play` also puts the program's folder first on the `PATH` of the Claude Code session it starts. |
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
  campaign      export or import a whole campaign as one file: dmc campaign export|import|show NAME-or-FILE [--out PATH] [--force] [--as NAME]
  play          start a play session in your DMC folder: claude --model sonnet --effort high [args]
  install       install dmc and write a DMC folder to play in: dmc install [--path FOLDER] [--force]
  folder        print the DMC folder this command would use
  refresh       bring a DMC folder's rules, skills, settings and templates up to date: dmc refresh [FOLDER | --all]
  --version     print the version

$ dmc --version
dmc 0.0.0-dev
```

A release prints its own version (`dmc 0.1.2`); every other build prints `dmc 0.0.0-dev`. `-h` and `help` work like `--help`.

## Rules that apply to every command

**One-line errors.** Everything `dmc` prints lands in DMC's context, so a failure is always one line on stderr, `dmc <command>: <message>`, never a stack trace and never the usage text. A message that would contain a line break is folded onto one line. An unexpected failure is still one line, with the exception's type in brackets at the end.

```
$ dmc
dmc: no command given (try roll, context-check, map, redraw, pdf, digest, campaign, play, install, folder, refresh, --version)
$ dmc foo
dmc: unknown command "foo" (try roll, context-check, map, redraw, pdf, digest, campaign, play, install, folder, refresh, --version)
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
```

Moves a whole campaign in and out of one file: a backup, a copy under another name, or a move to another computer. Each verb works on one campaign and never deletes or overwrites anything that was already there, except a file named by `--out` with `--force`.

| Argument | Meaning |
|---|---|
| `NAME` | `export`: a folder under `adventures/`. |
| `FILE` | `import`, `show`: an export file. Its name does not matter: the campaign name is read from the manifest inside it, so a renamed export imports the same. |
| `--out` | `export`: an existing folder (the default file name is used inside it) or a file path ending in `.zip`. Default: `exports/` in the DMC folder, made when needed. Never inside `adventures/`. |
| `--force` | `export`: replace an existing file named by `--out`. It has no effect on the default name, which is never replaced (the next free `-2`, `-3`, ... is used). |
| `--as` | `import`: the folder name to import under; lowercase letters, digits and hyphens, starting with a letter or digit. Default: the campaign name inside the file. |

**export** writes one ZIP: `manifest.json` first, then the campaign under `campaign/`, and the general `adventures/player-options.md` as `player-options.md` when there is one. Every file is stored byte for byte (no line-ending or encoding change), empty folders are kept, and the manifest lists each file's size and SHA-256 with the module the campaign runs (read from the `**Module folder**:` line of `module-progress.md`). The default file name is `<NAME>-<YYYYMMDDHHmm>.zip` in local time to the minute, for example `the-silver-egg-202610220605.zip`; a second export in the same minute is `...-2.zip`. The file is written under a `.partial-` name and renamed when complete, so a file with its final name is always whole. Nothing under `adventures/` changes. The export holds the whole campaign, spoilers included (`story/dm-notes.md`). A published module is not exported: `modules/` is copyrighted and stays on its machine.

**import** checks the whole file before it writes anything: it is a ZIP with a manifest that has every field, its format is not newer than this `dmc`'s, every path is safe on every operating system (relative, `/` separators, no `..`, no `<>:"|?*`, no trailing dot or space, no `CON`, `NUL` and the other reserved names, no two paths that differ only by case), and the entries match the manifest exactly. It then extracts into `adventures/.dmc-import-<random>/`, checking each file's size and SHA-256 as it is written, and commits with renames on one volume: the general player options first, only when `adventures/player-options.md` does not exist, then the campaign folder. A campaign therefore appears whole or not at all. Every extracted file gets the export's time as its modification time, and a manifest that declares more than 100,000 files or 1 GB in all is refused. It never changes an existing campaign or an existing `adventures/player-options.md`, never replaces a folder of the same name, and never writes `adventures/.active-campaign`: to play the copy, start a session and ask DMC to resume it.

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

`export` prints one line (`and the general player options` is left out when there is no general file). `import` prints up to four lines: the result; one for the general player options (`written to adventures/player-options.md` or `kept your existing adventures/player-options.md`, absent when the export holds none); one when the module is not in `modules/` (`module <folder> is not in modules/: ingest it from your own copy before play`, or `this is a module campaign, but its module-progress.md names no module folder`); and how to play it. `show` prints two or three: name, time (UTC), `dmc` version and format; file count, size and whether the general player options are included; and, for a module campaign, `module: <folder> (in modules/)`, `(not in modules/)` or `module: unknown (module-progress.md names no module folder)`. The size is in bytes below 1 KB, KB (1024 bytes) below 1 MB, else MB, with one decimal and a point.

| Message (after `dmc campaign: `) | Exit |
|---|---|
| `give export, import or show (try dmc campaign --help)` (no verb, or an unknown one) | 2 |
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

This changed after release 0.1.2, so it reaches players with the next release. Release 0.1.2 and earlier use a flat 150,000 and word the line `Context 197k, break due since 190k: ...`.

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

Rewrites the generated files of a DMC folder from the copy inside this `dmc`, and nothing else (contract `dmc-refresh.md`). The folder is the one given, or with `--all` every folder in `dmc.json` that is still a DMC folder, or else the folder `dmc folder` finds. For each file of the program's table (`kit/dm-rules.md` is `CLAUDE.md`, `kit/settings.json` is `.claude/settings.json`, `kit/skills/**` is `.claude/skills/**`, `kit/templates/**` is `templates/**`):

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

| Output (stdout, at most two lines per folder) | When |
|---|---|
| nothing | nothing to do |
| `dmc refresh: <folder>: <n> files updated for dmc X.Y.Z` | files written, replaced or removed |
| `dmc refresh: <folder>: <n> edited files kept in .dmc/replaced/<stamp>/` | hand edits were kept |

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
