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
#            dmc into bin/ (see tools/get-dmc.ps1); this script writes nothing itself. If no session can be started, one
#            more line on stderr, after any of those and with no Claude Code output: either "play: Claude Code (claude)
#            is not on the PATH: install it, then run this again" (claude is not there), or "play: Claude Code could not
#            be started: <reason>" (it is there but cannot run: blocked, damaged, or a shim that throws).
# Exit code: that of "bin/dmc play" or of claude, which is the session's. 127 if claude is not on the PATH (as play.sh
#            does, and as dmc play does). 1 if claude is found but cannot be started. Never a stale code left by an
#            earlier command: it is cleared before claude is started.
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
# A claude that cannot be run (not found, blocked, damaged, or a shim that throws) sets no $LASTEXITCODE, so a bare
# "exit $LASTEXITCODE" would pass on a stale value, 0 if nothing had run. So: find claude once, as the program or script
# PowerShell would run; start it through that; turn a failure to start into a message and an exit code; and clear
# $LASTEXITCODE first so that only claude's own exit code can come out of it.
$claude = Get-Command claude -CommandType Application, ExternalScript -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $claude) {
    [Console]::Error.WriteLine('play: Claude Code (claude) is not on the PATH: install it, then run this again')
    exit 127
}
$global:LASTEXITCODE = $null
try {
    & $claude --model sonnet --effort high --settings .claude/play-settings.json @args
    # A script that ends without "exit" leaves no code; then whether it ran to the end decides.
    $ran = $?
}
catch {
    # The innermost exception holds the reason; the outer message adds the script line and a caret.
    $reason = $_.Exception
    while ($reason.InnerException) { $reason = $reason.InnerException }
    [Console]::Error.WriteLine("play: Claude Code could not be started: $($reason.Message -replace '\s*[\r\n]+\s*', ' ')")
    exit 1
}
if ($null -ne $LASTEXITCODE) { exit $LASTEXITCODE }
exit $(if ($ran) { 0 } else { 1 })
