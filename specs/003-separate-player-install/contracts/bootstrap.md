# Contract: the install line (`install.ps1`, `install.sh`)

Downloads `dmc` and hands over to `dmc install` (FR-022). Research R8. Kept in `tools/`, attached to every release.

## README lines

```text
Windows (PowerShell):  irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1 | iex
Linux:                 curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh
```

A folder can be given: `& ([scriptblock]::Create((irm <url>))) -Path D:\Games\dmc` and `curl -fsSL <url> | sh -s --
--path ~/games/dmc`.

## Behaviour

1. Detect the platform and processor; refuse anything other than Windows or Linux on x64 or Arm64 (macOS refuses as
   `get-dmc` does today).
2. Download the archive for this machine and `SHA256SUMS.txt` from the release over HTTPS, with the same limits as `get-dmc` today: 10 to 15 seconds to connect, give up when under 1 KB/s for 30 seconds,
   10 minutes at most, and
   read the asset's `digest` from the releases API.
3. Check the SHA-256 against both before unpacking; on a mismatch install nothing. (No signature check here: see
   research R8 and R16. Updates check it.)
4. Unpack into a new temporary folder and run its `dmc --version`.
5. Run that `dmc install` (with `--path` if given), attached to the terminal, and exit with its exit code. The
   temporary folder is removed afterwards.

If the releases API refuses the request because of GitHub's limit for requests without sign-in (60 an hour per
address), the script checks against `SHA256SUMS.txt` alone and says so in one line on stderr
(`install: GitHub's API limit was reached, so only the release's checksum list was checked`); any other API failure
is an error.

All further output is `dmc install`'s ([dmc-install.md](dmc-install.md)).

## Errors (one line on stderr, `install: <message>`, exit 1)

Unsupported system or processor; download fails or stalls; hash mismatch ("nothing was installed"); archive cannot be
unpacked; the downloaded `dmc` does not run.

## Header comment

Each script carries the header `CLAUDE.md` § Git and GitHub requires (what, usage, inputs, outputs, exit codes) and
names its test-only variables: `DMC_INSTALL_TEST_ASSETS` (a local folder instead of the release),
`DMC_INSTALL_TEST_MACHINE`, `DMC_INSTALL_TEST_SYSTEM`.
