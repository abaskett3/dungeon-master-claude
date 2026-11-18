# Data model: Git-friendly campaigns

No new campaign file and no change to any file's shape. This model sorts the files that already exist under
`adventures/` by what the player's repository does with them. The paths come from `.docs/campaign-files.md` § Layout.

## File classes

| Class | Paths (relative to `adventures/`) | In git | Why |
|---|---|---|---|
| Campaign file | `player-options.md`; `<c>/` everything not listed below, `.standard`, `.closed`, `maps/*.json` (with `live.json`) and `maps/*.md` included | Kept | It is DMC's memory (Core 1) |
| Closing backup | `<c>/backups/<c>_closed_<stamp>.zip` | Kept | The record of a closed campaign; a sequel reads it (research R5) |
| Migration backup | `<c>/backups/<c>_backup_<stamp>.zip` and its `.migrating`, `.accounting.tsv`, `.verified.tsv` | Ignored | Whole copies; git's history does their job |
| Derived file | `<c>/maps/live.html`, `live-data.js`, `live.png`, `live-source.txt` | Ignored | `dmc redraw` makes them again from `live.json`; `dmc` does not compare them (`CampaignFiles.DerivedFiles`, research R13) |
| Per-machine state | `.active-campaign`, `.reviewed-sessions`, `.transcript-review/` | Ignored | It describes one computer's play |
| Staging | `.dmc-import-<id>/`, `.restore-<c>-<id>/`, `.replaced-<c>-<id>/`, `<c>/.standard.partial-<id>`, `<c>/.closed.partial-<id>` | Ignored | A running or interrupted command's leftovers |
| Repository rules | `.gitignore`, `.gitattributes` | Kept | Copied from `templates/adventures.gitignore` and `templates/adventures.gitattributes` |

## Rules

- A file DMC searches or the resume reads is never in an ignored class (FR-007; the test in contract
  [adventures-git-rules.md](contracts/adventures-git-rules.md)).
- Text is stored in git with LF (`* text=auto eol=lf`). On disk a campaign file may be LF or CRLF: `dmc` treats the
  two as the same content (contract [line-ending-compare.md](contracts/line-ending-compare.md)).
- Nothing in this feature deletes or moves a file (FR-009). Ignored files stay on disk.
