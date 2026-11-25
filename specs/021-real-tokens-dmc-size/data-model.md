# Data model: Real tokens in `dmc size`

No file format changes. The model is the measure inside `dmc` and the numbers it is compared with.

## The measure (`src/Dmc.Cli/Size/Measurement.cs`, its one home)

| Member | What it is | Rule |
|---|---|---|
| `Measurement.Tokens(long characters)` | Text tokens of some text; its comment states the calibrated rate, 0.4 (research R1, R2) | `2 × (characters ÷ 5)`, integer division (research R3) |
| `Measurement.TokensOf(lines)` | Text tokens of some lines | `Tokens` of their summed `Characters` |
| `Measurement.ReadCallTokens` | What one Read call adds beyond its text, 160 (research R1) | Added once a call, never to a budget figure |
| `Measurement.ReadCost(long characters, int calls)` | Read cost | `Tokens(characters) + 160 × calls` |
| `Line.Characters` | A line's characters as the Read tool returns it | `Text.Length`, plus 1 when the line has a line end (LF; a CR before it is not counted). `Characters ≤ Bytes` always (R3) |
| `Section.Characters`, `Section.Tokens` | A section's characters, and `Tokens(Characters)` | Replaces `Bytes ÷ 4`; `Bytes` stays, for the tests that check what is read |
| `Part.Characters`, `Part.Tokens`, `Part.FileTokens` | Characters of the part read (with `ExtraCharacters`, the `dmc recall --scene` output, which replaces `ExtraBytes`), its text tokens, and the whole file's | `Tokens` of the summed characters: sum first, convert once |
| `Part.Calls`, `Part.ReadCost` | The Read calls of the part, and what the resume pays to read it | 1 call, and 1 more when `ExtraCharacters > 0` (the recall command's output); `ReadCost(Characters, Calls)` |
| `SizeResult.Total`, `SizeResult.Calls` | The resume total and the calls it counts | Sum of `ReadCost` and of `Calls` over the parts in the total (`Total` was the sum of `Tokens`) |
| `Flags.IsNearCap(part)` | The one near-cap test, for the flag and the report's note | `ReadCost ≥ NearCapTokens` or lines ≥ `NearCapLines`, never for canon |
| `MarkdownFile.LineCharacters`, `Recall.Characters(line)` | Characters per line of a parsed file, and of a printed recall line | As `Line.Characters`; a printed line counts its text and 1 |

Every caller that divided bytes by 4 (research R6) sums `Characters` and calls `Tokens` or `ReadCost` instead.
`SequelDigest`'s check of a digest file reads the file's lines to count characters (it used `file.Length` in bytes).

## The numbers (`src/Dmc.Cli/Size/Budgets.cs`)

Each budget in tokens is 1.6 times its old value (research R3 table). The near-cap mark changes meaning:

| Member | Old | New | Compared with |
|---|---|---|---|
| `ReadCapTokens` | 25,000 | 25,000 (unchanged; real tokens) | Shown only in the flag line |
| `NearCapTokens` | 20,000 (bytes ÷ 4) | 18,000 (research R5) | `ReadCost` of a part, a module file, a section `dmc lookup` prints, a digest file |
| `NearCapLines` | 1,600 lines | 1,600 lines | Lines read, unchanged |
| Every other token member | B | 1.6 × B | Text tokens (`Tokens`) |

The class's XML summary says "in tokens (2 for 5 characters, `Measurement.Tokens`)" in place of "estimated tokens
(bytes ÷ 4)".

## Rules

- A budget, limit or section threshold is compared with **text tokens**, never Read cost (FR-005).
- A near-cap check is compared with **Read cost** (FR-003). Canon within `CanonLimit` is not checked against it
  (research R5): `Flags.Find`'s near-cap test skips a part whose budget kind is `Canon`, since canon over its limit has
  the archive flag. The `near the Read cap` note in the table is skipped the same way.
- Proof that no part is newly over a budget: research R3 (c ≤ b, and 2 × ⌊c ÷ 5⌋ > 1.6B gives c ≥ 4B + 5).
