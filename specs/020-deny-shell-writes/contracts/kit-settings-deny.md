# Contract: the deny rules this spec adds to `kit/settings.json`

The toolkit test `TheKitSettingsDenyShellWritesAndSendFeedback` holds this list as `ShellWriteRules` and checks each
row.

## Rules

| Rule | Spec | Takes | Does not take |
|---|---|---|---|
| `Bash(sed -i*)` | FR-001 | `sed -i 's/a/b/' f`, `sed -i.bak ...`, `sed -i -e x f` | `sed -n 1,20p f`, `sed 's/a/b/' f` |
| `Bash(sed * -i*)` | FR-001 | `sed -e x -i f`, `sed -E -i 's/a/b/' f` | `sed -n 1p f` |
| `Bash(sed -Ei*)` | FR-001 | `sed -Ei 's/a/b/' f`, `sed -Ei.bak ...` | `sed -E 's/a/b/' f`, `sed -En 1p f` |
| `Bash(sed -ri*)` | FR-001 | `sed -ri 's/a/b/' f` | `sed -n 1p f` |
| `Bash(sed --in-place*)` | FR-001 | `sed --in-place 's/a/b/' f`, `sed --in-place=.bak ...` | `sed -n 1p f` |
| `Bash(sed * --in-place*)` | FR-001 | `sed -E --in-place 's/a/b/' f` | `sed -n 1p f` |
| `Bash(python *)` | FR-001 | `python fix.py`, `python -c "..."`, `python` | `pythonw x`, `pytest` |
| `Bash(python3 *)` | FR-001 | `python3 fix.py` | `python3x` |
| `Bash(py *)` | FR-001 | `py fix.py`, `py -c "..."` | `pytest`, `pyright` |
| `Bash(node *)` | FR-001 | `node x.js`, `node -e "..."` | `nodemon x` |
| `Bash(perl *)` | FR-001 | `perl -pi -e 's/a/b/' f` | `perldoc x` |
| `Bash(tee *)` | FR-001 | `tee f`, `tee -a f` | `teed x` |
| `PowerShell(Set-Content *)` | FR-002 | `Set-Content f x`, `set-content -Path f -Value x` | `Get-Content f`, `Set-ContentX f` |
| `PowerShell(Add-Content *)` | FR-002 | `Add-Content f x` | `Get-Content f` |
| `PowerShell(Out-File *)` | FR-002 | `Out-File f` | `Out-Host` |
| `PowerShell(python *)` | FR-002 | `python fix.py` | `pytest` |
| `PowerShell(py *)` | FR-002 | `py fix.py` | `pytest` |
| `PowerShell(python3 *)` | FR-002 | `python3 fix.py` | `pytest` |
| `PowerShell(node *)` | FR-002 | `node x.js`, `node -e "..."` | `nodemon x` |
| `PowerShell(perl *)` | FR-002 | `perl -pi -e 's/a/b/' f` | `perldoc x` |
| `SendFeedback` | FR-003 | the tool, in every call | every other tool |

## Unchanged

- Every rule already in `kit/settings.json`, and the allow list.
- `.claude/settings.json` (toolkit work) gains none of these rules.

## Not taken (the limits, spec Edge Cases)

A redirect (`> f`), `bash -c '...'`, a full program path (`/usr/bin/python3`), `cmd /c`, `powershell -c`, `cp`,
`awk -i inplace`, `-i` bundled with a flag other than `-E` or `-r` (`sed -ni`), and in PowerShell `Tee-Object`, `tee`
and `ac`. A read holding ` -i` after the first word (`sed -n '/ -id/p' f`) is refused.
