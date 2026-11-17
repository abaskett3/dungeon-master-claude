# Quickstart: validate git-friendly campaigns

Run on copies only. Never run git in `D:\Claude\dmc`. Copy what you need into the job's scratch folder, or use
`D:\Claude\test-campaign`, the folder kept for Claude's play tests.

## Prerequisites

- A `dmc` built from this branch (`.docs/development.md`), installed into a test DMC folder with `dmc install`.
- Git on the PATH. On Windows, test once with `core.autocrlf=true`, git's installer default, and once with it
  unset.
- A copy of `D:\Claude\dmc\adventures\` (rorns-landing, the-silver-egg and the general options) put in the test DMC
  folder's `adventures/`.

## Q1. First commit leaves out the noise (SC-001, US1 scenario 1)

1. Copy `templates/adventures.gitignore` and `templates/adventures.gitattributes` into `adventures/` as
   `.gitignore` and `.gitattributes`, with the commands the user guide gives.
2. In `adventures/`, run `git init`, `git add -A` and `git commit -m start`.
3. Check:
   - `git ls-files | grep -E 'backup_|maps/live\.(html|png)|live-data|live-source|^\.active|^\.reviewed'` prints
     nothing.
   - `git ls-files | grep _closed_` prints the-silver-egg's closing backup.
   - The size of `.git` after `git gc` is at most 25% of the two campaign folders' size on disk. Record both numbers.

## Q2. A clean checkout stays clean (SC-003, US1 scenario 3)

1. Run `git status --porcelain`. Expected: empty, although 17 working files are CRLF.
2. Run `git clone adventures adventures-clone`. In the clone, `git status --porcelain` is empty, and every text
   file is LF.
3. Do both on Windows with `core.autocrlf=true`, and on Linux (WSL counts as Linux here).

## Q3. `dmc`'s checks see no change on a checkout (SC-003, US2 scenario 1)

Use a test DMC folder whose `adventures/` is the clone from Q2. Copy in `backups/` from the original, because
migration backups are not in git.

1. `dmc campaign verify the-silver-egg` reports it unchanged since it closed.
2. A dry run of a restore of rorns-landing from its latest backup, without `--yes`, warns about no changed files.
3. Change one word in `rorns-landing/story/canon.md`, and the same dry run warns about 1 file.
4. Run `dmc campaign show` and the sequel import's first step from the-silver-egg, and check that no "has changed
   since it closed" error appears.

## Q4. Search finds the same (SC-004, US2 scenario 2)

1. Run the toolkit test from contract [adventures-git-rules.md](contracts/adventures-git-rules.md).
2. In a Claude Code session started in the test DMC folder, with `adventures/` a git repository, use the Grep tool
   to search `adventures/` for a name that appears only in `story/canon-archive.md`, then for one only in a session
   log, then for one only in `digests/`. Each must be found. This also checks ripgrep's handling of the `!` exception
   (research R6).
3. Search for a word that appears only in a migration backup's `verified.tsv`. It must not be found, which shows the
   rules are in force.

## Q5. One played scene changes only what it changed (SC-002, US1 scenario 2)

Run headless (`claude -p`, with `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`) on the clone. Resume, play one scene
with a token move, then end. Check:
- `git status --porcelain` lists only campaign files: the session log, `game-state.md`, `maps/live.json`, and
  whatever canon or sheets the scene touched.
- No render, backup or per-machine file is listed.
- After a fresh checkout, the map page is drawn again by the first reply's Stop hook (`dmc redraw`).

## Q6. Two full saves in a row (SC-005, US3 scenario 2)

Headless, on the clone: commit, ask for a full save, commit, ask for another full save, then run `git diff`.
Record what changed. The pass is: only the lines that record when the save ran. This is model behavior (research
R12). A failure goes to `feedback/`, and the PR description reports it. It does not block this feature.

## Q7. `dmc map` writes LF (FR-003)

On Windows, draw a map. `live.html`, `live-data.js` and `live-source.txt` have no CR bytes.

## Q8. The guide (SC-006)

A reader who has not used git follows the user guide's new section, on a copy, from install to a resumed clone in a
second DMC folder, without other help. Record where they stopped, if anywhere.
