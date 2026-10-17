# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.ps1 --continue works.
#
# Once tools/dmc-release.json pins a dmc release, this first runs tools/get-dmc.ps1 (it installs or updates bin/dmc,
# and prints nothing when it is current), then hands over to "bin/dmc play", which starts Claude Code the same way.
# Until a release is pinned, or if dmc cannot be had, it starts Claude Code itself, as it always has.
#
# Usage:     ./play.ps1 [claude arguments...]     (from any folder; it moves to the toolkit root first)
# Inputs:    tools/dmc-release.json, to see whether a release is pinned; the arguments, passed on unchanged.
# Outputs:   whatever Claude Code prints. When no bin/dmc that runs can be had, a warning on stderr first: get-dmc's own
#            error line, if get-dmc failed ("get-dmc: ..."), then this script's line ("play: dmc is not available ..."),
#            so one line if get-dmc succeeded but the dmc it left does not run, two if get-dmc failed. get-dmc may install
#            dmc into bin/ (see tools/get-dmc.ps1); this script writes nothing itself.
# Exit code: that of "bin/dmc play" or of claude, which is the session's. Known bug: if claude is not on the PATH,
#            PowerShell reports the error but sets no exit code, and the script then exits with whatever $LASTEXITCODE
#            held (0 if nothing set it, or the code of an earlier command such as get-dmc), so it can exit 0 with no
#            session started. play.sh exits 127 in that case.
#
# It never stops play because dmc is missing or broken: it starts Claude Code without it, and says so. It never hands
# the session to a bin/dmc that fails to print its version, and never changes the arguments it was given.
Set-Location $PSScriptRoot
$pinned = $false
try { $pinned = [bool] (Get-Content -Raw -LiteralPath tools/dmc-release.json -ErrorAction Stop | ConvertFrom-Json).tag } catch { }
if ($pinned) {
    & ./tools/get-dmc.ps1
    $dmc = if ($PSVersionTable.PSEdition -ne 'Core' -or $IsWindows) { './bin/dmc.exe' } else { './bin/dmc' }
    # Only a dmc that runs here (not blocked, not damaged) gets the session.
    $runs = $false
    if (Test-Path -LiteralPath $dmc -PathType Leaf) {
        try { & $dmc --version *> $null; $runs = $LASTEXITCODE -eq 0 } catch { }
    }
    if ($runs) {
        & $dmc play @args
        exit $LASTEXITCODE
    }
    [Console]::Error.WriteLine('play: dmc is not available, so Claude Code starts without it, and DMC cannot roll dice, draw maps or read PDFs until get-dmc succeeds.')
}
claude --model sonnet --effort high --settings .claude/play-settings.json @args
exit $LASTEXITCODE
