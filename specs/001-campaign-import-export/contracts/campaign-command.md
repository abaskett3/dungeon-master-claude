# Contract: `dmc campaign`

The command-line interface this feature adds. Rules shared by every command (one-line errors, `argparse`-style options, exit codes, UTF-8 output with LF, the stale-binary warning, the toolkit root) are in `.docs/dmc-reference.md` § Rules that apply to every command and are not repeated here. Paths printed are absolute unless shown otherwise; `<root>` is the toolkit root.

## Usage

```
dmc campaign export NAME [--out PATH] [--force]
dmc campaign import FILE [--as NAME]
dmc campaign show FILE
dmc campaign --help
```

- `NAME` (export): a folder under `<root>/adventures/`.
- `--out PATH`: an existing folder (the default file name is used inside it) or a file path ending in `.zip`. Default: `<root>/exports/`, created if missing.
- Default file name: `<NAME>-<YYYYMMDDHHmm>.zip`, the local date and time of the export to the minute, for example `the-silver-egg-202610220605.zip`. When it is taken, `-2`, `-3`, ... is added before `.zip`.
- `--force`: allow replacing an existing file named by `--out`. Has no effect on the default name, which is never replaced (the next free `-N` is used).
- `FILE`: an export file. Its name does not matter: the campaign name is read from the manifest inside it, so a renamed export imports the same.
- `--as NAME`: the folder name to import under; must match `[a-z0-9][a-z0-9-]*`. Default: the `campaign` in the manifest.

## Success output (stdout, exit 0)

### export

```
exported the-silver-egg: 61 files and the general player options to <root>/exports/the-silver-egg-202610220605.zip
```

Without a general options file, `and the general player options` is left out.

### import

```
imported the-silver-egg: 61 files to <root>/adventures/the-silver-egg
general player options: written to adventures/player-options.md
module lost-mine-of-phandelver is not in modules/: ingest it from your own copy before play
to play it, start a session and ask DMC to resume the-silver-egg
```

- Line 2 is one of: `written to adventures/player-options.md`, `kept your existing adventures/player-options.md`, or absent when the export holds none.
- Line 3 appears only when the module is missing; with the module unnamed it reads `this is a module campaign, but its module-progress.md names no module folder`.

### show

```
the-silver-egg: exported 2026-10-21 19:04 UTC by dmc 0.1.4, format 1
61 files, 1.4 MB, plus the general player options
module: lost-mine-of-phandelver (not in modules/)
```

Line 3 is absent for a campaign without a module and says `(in modules/)` when it is present. `show` writes nothing.

## Failures (stderr, one line)

| Situation | Exit | Message after `dmc campaign: ` |
|---|---|---|
| No verb, or an unknown verb | 2 | `give export, import or show (try dmc campaign --help)` |
| Missing `NAME` or `FILE`, unknown option | 2 | the `argparse` message, as other commands print it |
| Export: no such campaign | 1 | `no campaign "<name>" in adventures/ (there are: a, b)` (`(there are none)` when empty) |
| Export: destination inside `adventures/` | 1 | `will not write an export inside adventures/: choose another --out` |
| Export: `--out` file exists, no `--force` | 1 | `<path> exists: add --force to replace it, or choose another --out` |
| Export: `--out` is neither an existing folder nor a name ending in `.zip` | 1 | `<path> is not an existing folder or a file name ending in .zip: choose another --out` (and `--out is empty: give a folder or a file name ending in .zip`) |
| Export: a path in the campaign that an import would refuse | 1 | `<relative path> cannot be exported: <reason>` |
| Export: a read or write fails | 1 | `could not export: <reason>`; no file is left under the final name |
| Export: a symbolic link or junction in the campaign | 1 | `<relative path> is a link: exports hold only the campaign's own files` |
| Export: the campaign folder itself, or the general `adventures/player-options.md`, is a link | 1 | `<name> is a link: ...` or `adventures/player-options.md is a link: ...`, ending as the row above |
| Import or show: file missing or unreadable | 1 | `cannot read <path>: <reason>` |
| Import or show: not a ZIP, or no valid manifest | 1 | `<path> is not a dmc campaign export` |
| Import or show: newer format | 1 | `<path> was made by a newer dmc (format <n>): update dmc and try again` |
| Import: name taken | 1 | `adventures/<name> already exists: import under another name with --as NAME` |
| Import: `--as` not a valid name | 2 | `--as must be lowercase letters, digits and hyphens, starting with a letter or digit` |
| Import: unsafe or unportable path, or a manifest declaring more than 100,000 files or 1 GB in all | 1 | `<path> holds an entry that cannot be imported: <entry> (<reason>)`; for the caps the entry is `manifest.json` and the reason `more than 100000 files` or `more than 1 GB in all` |
| Import: archive and manifest disagree, or a size or checksum differs | 1 | `<path> is damaged: <entry> <what differs>` |
| Import: a write fails | 1 | `could not import: <reason>`; nothing is left under the target name |

When cleaning up after a failure itself fails, one more line on stderr names the leftover staging folder, as `dmc pdf` does.

## Guarantees

- `export` and `show` change nothing under `adventures/`.
- `import` never changes an existing campaign folder or an existing `adventures/player-options.md`, and on any failure leaves no folder under the target name.
- `import` never writes `adventures/.active-campaign`.
- A round trip `export` then `import --as other` produces a folder identical to the original in every file's bytes and in its empty folders, on any supported system.
- File times are not part of the round trip: every file `import` writes gets the export time (`exportedAt`) as its last-write time, so the map redraw, which compares modification times, cannot see an extracted `live.json` as newer than its picture.
