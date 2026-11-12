# Contract: recall and lookup over links and digests

PR 1. Changes to `specs/009-retrieval-lookup-recall/contracts/dmc-recall.md` and `dmc-lookup.md`; what is not named
here is unchanged (the 6,000-byte cap, the trim, the notices' cap, the exit codes).

## Scope

The campaign; every campaign of its linked set ([campaign-links.md](campaign-links.md)); every `digests/<name>/` in any
of them, except its `index.md` (no facts); every module any of them names, once, at the smallest distance. Excluded as
today: `import/`, `exports/`, `backups/`, dot folders, a module's `source/`.

## Weights

| File | Weight |
|---|---|
| A campaign's own files | Tier weight (canon 1.30, archive 1.20, sheet 1.15, story 1.05, session 1.00) × 0.95^distance |
| The same, for a last-resort campaign (its digest is held nearer the start) | × 0.85 more |
| `digests/<name>/*.md` | 1.20 × 0.95^(distance of `<name>`, or the holder's distance + 1 if `<name>` is not in the set) |
| A module | 0.80 × 0.95^distance |

Ties break as today: score at two decimals, then tier (Digest sorts between Canon and Archive), distance, place, file,
line.

## DM-only

As today, plus every line of `digests/*/secrets*.md` and every line under a digest's `## Module` heading (it copies
`module-progress.md`).

## Duplicates

Lines whose normalised text is the same are one line, merged before ranking: the copy with the highest weight (then
the tie order above) is ranked and printed, `DM` when any copy is DM-only. The other copies are not ranked, printed or
counted in `(N more lines matched)`.

## Hit line

```
<place>[ (<label>)] <file>:<line>[ <session>][ DM] | <text>
<place> digests/<name>/<file>:<line> (<label>)[ <session>][ DM] | <text>
```

The first form is for a campaign's own files and a module's; the label follows the place of any campaign but the one
recalled in. The second is a digest hit: the label is the digested campaign's and follows the file, so that it is not
read as the holder's.

```
rorns-landing story/canon.md:41 s12 | Kibil owes the ferryman of Thornwick two silver for the crossing (s12).
rorns-landing digests/the-silver-egg/people.md:88 (continues) s31.2 | Kibil is not immune to cold: ...
the-silver-egg (continues) story/sessions/session-031.md:40 s31.2 | ...
side-quest (side) story/canon.md:7 s3 | The Thornwick bridge burned on the night of the eclipse (s3).
modules/the-crooked-moon chapters/03.md:120 | ...
```

A digest hit is in the recalled campaign's place and carries the digested campaign's label. A module hit has no label.

## Notices

Recall's notices become the linked set's ([campaign-links.md](campaign-links.md) § Notices), printed as recall printed
Phase A's: without the `notice: ` prefix, with `, not searched` added to the missing-folder and not-a-folder-name
notices; with the existing module-folder and unreadable-file notices. The Phase A line `<campaign>: Predecessor <name> is not in
adventures/, not searched` is replaced by `<campaign>: <line> <name> is not in adventures/, not searched`, and is not
printed when a campaign in the set holds `<name>`'s digest.

## Lookup

No change in resolution. A digest is a campaign file, so:

- `dmc lookup <sequel> <subject>` matches a digest heading like any heading of the campaign; if the sequel's own sheet
  has the same heading, the usual list of matches is printed.
- `dmc lookup <sequel>/digests/<name>/people.md <subject>` searches that file only.
