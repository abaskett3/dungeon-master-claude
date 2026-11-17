# Contract: `dmc` compares campaign files ignoring line endings

Research R3. One helper in `src/Dmc.Cli/Campaign/CampaignFiles.cs`, used by every check that compares a campaign
file with a backup or a verified record.

## Text or not

A file is text when its first 8,000 bytes hold no NUL byte. This is git's test for `text=auto`.

## Same content (two byte arrays)

`a` and `b` are the same when either is true:
- they are equal byte for byte, or
- both are text and they are equal after every CRLF in each is turned into LF.

A lone CR is left as it is.

## Matches a stored hash

The current `bytes` match a SHA-256 `hash` when any of these hash to it:
- `bytes`, as they are;
- `lf`, which is `bytes` with every CRLF turned into LF (text only);
- `lf` with every LF turned into CRLF (text only).

## Where it is used

| Caller | Today | After |
|---|---|---|
| `CampaignRestorer.Changed` (`CampaignRestorer.cs:99-101`), the restore's "changed since the migration was verified" count | raw hash ≠ stored hash | not "matches a stored hash" |
| `CampaignVerifier.ClosedDifferences` (`CampaignVerifier.cs:205-223`), used by `dmc campaign verify` on a closed campaign and by the sequel import (`SequelDigest.cs:157`) | raw hash of each side differs | not "same content" |

No record changes:
- `verified.tsv` still stores the hash of the bytes as they were (`CampaignVerifier.cs:294`).
- Export manifests still hash the bytes inside the ZIP.
- Import checks a ZIP's entries against its manifest, byte for byte, as before. Git does not touch a ZIP.

## Output

No message changes. A file that differs only in line endings is no longer counted or listed. A file whose content
changed is reported as before.

## Tests

`tests/Dmc.Tests/Campaign/` (the existing restore and verify tests) gain these cases:
- A closed campaign whose CRLF files were turned to LF still verifies, and a sequel import from it starts.
- A restore after the same conversion warns about no files.
- A real one-word change is still reported by both.
- A binary file (with a NUL) that differs only in a CR byte is still reported.

## The map page is not compared (added in implementation, research R13)

`CampaignFiles.DerivedFiles` names `maps/live.html`, `maps/live-data.js`, `maps/live.png` and `maps/live-source.txt`.
Both callers skip these paths on both sides: they are not `changed`, `added` or `missing`, and not in the count of files
that match. The restore's plan line (`<r> files replaced, <d> removed, <a> added`) still counts every file the restore
rewrites. A toolkit test keeps the gitignore's `/*/maps/` lines equal to `DerivedFiles`.
