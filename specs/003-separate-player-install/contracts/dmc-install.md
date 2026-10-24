# Contract: `dmc install [--path <folder>] [--force]`

Installs `dmc` for the current user and writes a DMC folder (FR-007 to FR-010). Research R7; layout in
[data-model.md](../data-model.md#dmc-folder).

## Behaviour

1. **Leftovers** from an earlier update (`*.old`, `.update-*`) in the program folder are removed quietly if possible.
2. **The program.** A release build not running from the program folder copies itself and its three native libraries
   there and puts the program folder on the user's PATH (Windows: user `Path`; Linux: link `~/.local/bin/dmc`). A
   development build (`0.0.0-dev`) does this only with `--force` (research R14).
3. **The folder.** `--path <folder>` if given. Otherwise ask on the terminal `DMC folder [<home>/dmc]: `; Enter takes the
   default. On Linux, when standard input is not a terminal (`curl ... | sh`), read the answer from `/dev/tty`; if no
   terminal can be opened, use the default. `~` at the start of an answer means the home folder. The folder is created if
   missing.
4. **Refuse** a folder inside a checkout of this repository (a `kit/dm-rules.md` in it or above it).
5. **Write** the generated files and `.dmc/manifest.json` (see [dmc-refresh.md](dmc-refresh.md)). Over a folder with no
   manifest, refuse if any file it would write already exists, naming the first. Over a folder with a manifest, behave as
   `dmc refresh`.
6. **Create** `adventures/`, `feedback/`, `modules/` and `rules/` if missing. Write nothing inside them.
7. **Record** the folder in `dmc.json` beside the program (added once; order kept).

## Output (stdout)

```text
dmc install: installed dmc X.Y.Z in <program folder>          (only when step 2 copied the program)
dmc install: DMC folder ready at <folder>
dmc install: open a new terminal, then run dmc play            (or, Linux, when ~/.local/bin is not on PATH:)
dmc install: add ~/.local/bin to your PATH (for example in ~/.profile), then run dmc play
```

At most three lines, plus the refresh's lines (at most two) when it writes over an existing DMC folder; exit 0.

## Errors (one line on stderr, `dmc install: <message>`, exit 1)

| Case | Message |
|---|---|
| Run inside a Claude Code session (`CLAUDECODE` set; research R6) | `cannot run inside a Claude Code session: run it in a terminal` |
| Inside the repository | `<folder> is inside the DMC source repository: choose another folder` |
| A file in the way | `<file> already exists and was not written by dmc: move it, or choose another folder` |
| Folder or program folder not writable | `cannot write <folder>: <reason>` |
| `<folder>` is a file | `<folder> is a file, not a folder` |
| PATH cannot be changed (Windows) | `could not add <folder> to your PATH: <reason>` (after the folder is written) |
