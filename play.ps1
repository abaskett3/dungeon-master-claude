# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.ps1 --continue works.
#
# Once tools/dmc-release.json pins a dmc release, this first runs tools/get-dmc.ps1 (it installs or updates bin/dmc,
# and prints nothing when it is current), then hands over to "bin/dmc play", which starts Claude Code the same way.
# Until a release is pinned, or if dmc cannot be had, it starts Claude Code itself, as it always has.
Set-Location $PSScriptRoot
$pinned = $false
try { $pinned = [bool] (Get-Content -Raw -LiteralPath tools/dmc-release.json -ErrorAction Stop | ConvertFrom-Json).tag } catch { }
if ($pinned) {
    & ./tools/get-dmc.ps1
    $dmc = if ($PSVersionTable.PSEdition -ne 'Core' -or $IsWindows) { './bin/dmc.exe' } else { './bin/dmc' }
    if (Test-Path -LiteralPath $dmc -PathType Leaf) {
        & $dmc play @args
        exit $LASTEXITCODE
    }
    [Console]::Error.WriteLine('play: dmc is not available, so Claude Code starts without it.')
}
claude --model sonnet --effort high --settings .claude/play-settings.json @args
