# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.ps1 --continue works.
Set-Location $PSScriptRoot
claude --model sonnet --effort high --settings .claude/play-settings.json @args
