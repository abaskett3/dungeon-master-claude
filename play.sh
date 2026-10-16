#!/usr/bin/env bash
# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.sh --continue works.
#
# Once tools/dmc-release.json pins a dmc release, this first runs tools/get-dmc.sh (it installs or updates bin/dmc,
# and prints nothing when it is current), then hands over to "bin/dmc play", which starts Claude Code the same way.
# Until a release is pinned, or if dmc cannot be had, it starts Claude Code itself, as it always has.
cd "$(dirname "$0")" || exit 1
if grep -Eq '^[[:space:]]*"tag"[[:space:]]*:[[:space:]]*"dmc-v' tools/dmc-release.json 2>/dev/null; then
    bash tools/get-dmc.sh
    # Only a dmc that runs here (not blocked, not damaged) gets the session. Git Bash runs bin/dmc.exe as ./bin/dmc.
    if { [ -f bin/dmc ] || [ -f bin/dmc.exe ]; } && ./bin/dmc --version >/dev/null 2>&1; then
        exec ./bin/dmc play "$@"
    fi
    echo "play: dmc is not available, so Claude Code starts without it." >&2
fi
exec claude --model sonnet --effort high --settings .claude/play-settings.json "$@"
