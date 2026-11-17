# Research: Git-friendly campaigns

Facts measured on the maintainer's DMC folder `D:\Claude\dmc` on 2026-11-17, and on `origin/main` at `fb71982`.

## R1. Where the CRLF files come from

**Finding.** Of the 262 files in `adventures/` other than ZIPs and PNGs, 17 have CRLF line endings and 2 are mixed;
the rest are LF. The
templates and migrations `dmc` writes are all LF (`kit/** text eol=lf`, `.gitattributes`). The CRLF files come from
two writers:

- **`dmc map`.** `MapPage.WriteText` turns each `\n` into `Environment.NewLine`
  (`src/Dmc.Cli/Maps/MapPage.cs:44-45`), so on Windows `live.html`, `live-data.js` and `live-source.txt` are CRLF
  (`MapRedraw.cs:108-111`). It copies Python's text mode, from when the map was a Python script.
- **DMC in play, through the shell.** The play transcripts in `~/.claude/projects/D--Claude-dmc*/` show DMC changing
  `story/canon.md`, `story/dm-notes.md`, `story/summary.md`, `game-state.md` and map JSON with Python heredocs
  (`open(p, 'w')` writes CRLF on Windows) and once with PowerShell `Set-Content`. `kit/dm-rules.md:63` forbids shell
  commands on campaign files; DMC breaks it. The Edit tool keeps a file's line endings, so a file stays CRLF once
  converted.

**Decision.** Do not try to stop CRLF at its source in play. Let git normalize (R2), and make `dmc`'s own checks
ignore line endings (R3). Fix `dmc map`, the one `dmc` writer (R7).

**Why.** The shell writes break a rule that already exists. New wording is not likely to stop them, and every word in
`kit/dm-rules.md` is paid every turn (Core 3). Both git and `dmc` can absorb the difference at no cost to play.

## R2. The line-ending rules for git

**Decision.** `adventures/.gitattributes` holds `* text=auto eol=lf`, plus `*.zip binary` and `*.png binary`. Git
stores text files with LF and checks them out with LF on every platform.

**Why.**
- An `eol` attribute overrides the player's `core.autocrlf` (git documentation, gitattributes § `eol`; unverified on
  this machine beyond `core.autocrlf=true` being set in the toolkit's checkout). A Windows player's default git
  setting therefore cannot rewrite files on checkout.
- A CRLF file in the working tree whose text matches the index shows as unchanged, because git normalizes it before
  comparing. A Python write in play adds no noise to the history.
- Explicit `binary` stops git guessing about ZIPs and PNGs.

**Alternatives rejected.**
- **`* -text`** (store bytes as they are). The bytes stay identical, so no hash check changes, but a file flipping
  between CRLF and LF shows as changed in full. Diffs on Windows also show `^M`.
- **No attributes.** The player's `core.autocrlf` would decide, so the result would differ between computers.

## R3. Hash checks and line endings (FR-005)

**Finding.** Four places compare campaign bytes:
- `CampaignRestorer.Changed` compares against `verified.tsv` hashes (`src/Dmc.Cli/Campaign/CampaignRestorer.cs:99-101`).
- `CampaignVerifier.ClosedDifferences` compares a closed campaign with its closing backup (`CampaignVerifier.cs:205-223`).
- The sequel import uses that same comparison (`SequelDigest.cs:157`).
- Export and import check the entries inside a ZIP, which git does not touch.

After a checkout with R2's rules, a file that was CRLF on disk comes back LF. `ClosedDifferences` would then refuse a
sequel import with "has changed since it closed". A restore would warn that files changed, which is wrong.

**Decision.** Add one comparison, used in place of the two raw hash comparisons. Two contents count as the same when
they match byte for byte, or when both are text and match after every CRLF is turned into LF. Against a stored hash,
the current bytes match if any of these hash to it: the bytes as they are, the bytes with CRLF turned into LF, or that
LF form with every LF turned into CRLF. A file is text when its first 8,000 bytes hold no NUL byte, the test git uses
for `text=auto`.

**Why.**
- It covers records already written: a `verified.tsv` hash of a CRLF file still matches the LF file a checkout gives.
- Nothing about the record formats changes, so no record and no campaign needs a migration.
- A real change in content still differs.

**Alternatives rejected.**
- **Store normalized hashes in `verified.tsv` from now on.** Old records would still hold raw hashes, so the
  comparison would need both forms anyway.
- **Make the line-ending case impossible with `-text`.** Rejected in R2.
- **A migration to LF.** See R4.

**Limit.** A file with mixed endings that git normalizes matches only by content (in `ClosedDifferences`, where both
sides are bytes). Against a stored hash it does not match, because the original mix cannot be rebuilt from a hash.
In the maintainer's folder, two mixed files exist, both in the-silver-egg's `exports/` (R1). The-silver-egg is a closed
campaign, and its check compares content with the bytes in its closing backup, so it is covered.

## R4. No line-ending migration (spec FR-004 changes)

**Decision.** No migration. The spec's FR-004 is replaced: git normalizes text on commit (R2), and `dmc`'s checks
ignore line endings (R3).

**Why.**
- A migration is a run of DMC following a file, with a backup and a verify, paid in tokens by the player
  (`.docs/campaign-files.md` § Bringing a campaign up to date).
- The next Python write in play would bring CRLF back (R1).
- Line endings are not part of what campaign files should hold, so Development 2 asks for no migration.

**Needs the maintainer's approval**, because it changes FR-004 and User Story 3's first scenario.

## R5. What the ignore rules leave out

**Decision.** `adventures/.gitignore` leaves out:
- **Per-machine state:** `/.active-campaign`, `/.reviewed-sessions`, `/.transcript-review/`, and the staging
  folders `/.dmc-import-*/`, `/.restore-*/` and `/.replaced-*/`.
- **The map renders:** `*/maps/live.html`, `*/maps/live-data.js`, `*/maps/live.png` and `*/maps/live-source.txt`.
- **Migration backups:** `*/backups/*`, with an exception, `!*/backups/*_closed_*.zip`, that keeps each closing backup.

It keeps everything else, `maps/live.json`, `.standard` and `.closed` included.

**Why.**
- **Closing backups are kept.** They are the record of a closed campaign, written once and never pruned
  (`.docs/campaign-files.md:28`). A sequel import reads one and checks the closed campaign against it
  (`SequelDigest.cs:150-157`). Without it in git, a sequel could not be started on a new computer. Each is written
  once, so it adds one binary to the history per closed campaign. The-silver-egg's is
  `backups/the-silver-egg_closed_20261113212205.zip`, named in its `.closed` (`ls`, 2026-11-17).
- **Migration backups are left out.** Three of them make up 2.2 of rorns-landing's 4.2 MB, and git's history already
  does their job.
- **`live.json` is kept.** It is the scene on screen and is read at resume.
- **`.active-campaign` is left out.** The resume asks which campaign to play when it is missing
  (`kit/dm-rules.md:18`), so a new computer just asks once.
- **The renders are left out.** `dmc redraw` draws `live.json` again when `live-data.js` is missing
  (`MapRedraw.cs:54-55`), so the Stop hook rebuilds them after the first reply on a fresh checkout.

**Alternatives rejected.**
- **Keep `.active-campaign`.** Every campaign switch would be a commit line, and the spec names it per-machine state.
- **Leave out all of `backups/`.** No sequel could start on a new computer.

## R6. Search is not hidden (FR-007)

**Finding.** The Grep tool uses ripgrep, which skips git-ignored files inside a git repository
(`.docs/decisions.md:224`). With `adventures/` as a repository, `adventures/.gitignore` applies to every search
under it. `feedback/`, `modules/` and `rules/` sit outside the repository and are not affected.

**Decision.** A toolkit test makes a temporary git repository with the shipped rules and a tree holding every
campaign path from `.docs/campaign-files.md` § Layout. It asks `git check-ignore` about each path. Every kept path
must come back not ignored, and every left-out path must come back ignored. It calls `TestEnv.Need` for git
(Development 7).

**Why git and not ripgrep.** Git is installed on CI. Ripgrep follows the same gitignore syntax (unverified for the
`!` exception under a folder that is not itself ignored; quickstart Q4 checks it with the Grep tool).

## R7. `dmc map` writes LF (FR-003)

**Decision.** `MapPage.WriteText` writes `\n` as it is, with no `Environment.NewLine`. Its doc comment changes.

**Why.** It is the only `dmc` writer that uses the platform's line ending in a campaign. `PdfExtractor.cs:303` does
too, but it writes `modules/`, which is outside the repository and not a campaign. The renders are git-ignored, so
the change matters for consistency, not for git noise.

## R8. Stable bytes from `dmc` (FR-006)

**Finding.** The kept files `dmc` writes are:
- `.standard`: a number and `\n` (`StandardRecord.cs:104`).
- `.closed`: written once (`ClosedCampaign.cs:103`).
- The sequel digests: written once, at import (`SequelDigest.cs:778`).

`verified.tsv` and the accounting files sit in `backups/`, which is ignored. The renders carry a time
(`MapPage.cs:30-31`) and are ignored.

**Decision.** No code change. FR-006 holds today for every file the rules keep.

## R9. How the rules reach `adventures/`

**Decision.** The kit ships `kit/templates/adventures.gitignore` and `kit/templates/adventures.gitattributes`.
`dmc install` and `dmc refresh` write them to the DMC folder's `templates/` (`src/Dmc.Cli/Kit/GeneratedFiles.cs:32`).
The user guide gives one copy command per shell that puts them in `adventures/` as `.gitignore` and
`.gitattributes`.

**Why.**
- `dmc` keeps never writing `adventures/` outside the campaign commands (`.docs/campaign-files.md:58`).
- The names avoid a real `.gitignore` inside the toolkit's `kit/`, where it would act on the toolkit's own repository.

**Alternatives rejected.**
- **A `dmc` command that writes the two files.** It is new surface to keep up for a step a player does once.
- **A DMC skill.** It would cost play tokens and blur FR-011.

**Limit.** When a release changes the rules, the player copies them again. The release notes say so.

## R10. Cost (Core 3, Development 1)

No line is added to `kit/dm-rules.md` or to any skill. The resume reads nothing new. Play cost: zero tokens.

## R11. The player's own git settings

- A global excludes file (`core.excludesFile`) or `.git/info/exclude` could hide a campaign file from search. The
  guide says not to add campaign files to either.
- Git warns "CRLF will be replaced by LF" when adding a file DMC wrote with CRLF. The guide says the warning is
  expected and harmless.

## R12. Two full saves in a row (SC-005)

This is model behavior, not code. The quickstart measures it in a headless run (Q6). No change is planned unless it
fails. If it does, the result goes to `feedback/`, not to this feature.

## R13. The map page on a clone (found in implementation)

**Finding.** Quickstart Q3 on a clone of the maintainer's campaigns: `dmc campaign verify the-silver-egg` failed with
`missing:` for the four map page files, which the rules leave out, and the restore's warning counted them too (6 files
on the clone against 2 in the real folder). The sequel uses the same check, so a sequel could not have started on a new
computer, which is R5's reason for keeping closing backups.

**Decision.** The two comparisons skip the map page files (`CampaignFiles.DerivedFiles`). They are derived, hold no
play, and `dmc redraw` draws them again. A test keeps the gitignore's map lines equal to the list.

**After the fix.** On the clone, verify passes with 105 files (the real folder's 109, less the four), the restore warns
about the same 2 files as the real folder, a one-word edit makes it 3, and `dmc campaign sequel rorns-landing --check`
prints the same line in both.
