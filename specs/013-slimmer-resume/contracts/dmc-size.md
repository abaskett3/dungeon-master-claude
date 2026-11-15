# Contract: `dmc size`, the companions and canon

## Companions

- A companion (a `### ` block under Party status other than "With the party, not companions") whose sheet's first `## `
  section is `## Play card` is shown `to its play card` and measured from the top to the line before the next `## `
  heading after the card, against `Budgets.PlayCard` (1,000): `character-sheets/npc-kibil-rornson.md  700  budget 1k
  to its play card`.
- A companion sheet with no card is shown and measured as today (`to its history`, budget 4k), and `--check` prints
  `character-sheets/<file>: a companion with no play card: the full save writes one`. It is the full save's to fix, as
  every flag but the near-cap line and the notices.
- The PC's sheet, and an NPC's read because the scene names them, are unchanged.

## Canon

- Under `Budgets.CanonWholeBelow` (6,000): `read whole`, as today.
- At or over it: the always-read sections plus the bytes `dmc recall NAME --scene` prints, shown `read in sections and
  by scene`. The flag for canon over `Budgets.CanonLimit` (19,000) stays, worded `…: a full save archives it down to
  16k`, without "read it in sections", since the resume no longer decides by it.

## Markers

`ReadSet.Markers` gains `## Play card` and `--scene`; the toolkit test checks both are in `dmc-resume-campaign` § 2.
