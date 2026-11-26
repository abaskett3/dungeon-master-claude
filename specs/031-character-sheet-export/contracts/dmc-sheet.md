# Contract: `dmc sheet`

The command's arguments, output, files and exit codes are documented in `.docs/dmc-reference.md` § sheet, which is
their home; the tests in `tests/Dmc.Tests/Sheets/SheetCommandContract.cs` hold them. In short:

| Form | Standard output | Standard error | Exit | Writes |
|---|---|---|---|---|
| export, files in shape | warnings, then `wrote exports/sheets/<campaign>/<file>: N sheets (<titles>)[; skipped <block> (no sheet), ...]` | nothing | 0 | the page |
| export, a finding | findings, then warnings (at most 12 lines) | `dmc sheet: refused: N problems in the files: fix each one and run it again` | 1 | nothing |
| `--check` | findings, then warnings; nothing when all is well | nothing | 0 | nothing |
| a one-line failure | nothing | `dmc sheet: <message>` | 1 | nothing |
| a usage mistake | nothing | `dmc sheet: <message>` | 2 | nothing |

A finding is `<sheet file>: <what is wrong>: <what to do>`; a warning starts `warning: `.
