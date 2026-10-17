#!/usr/bin/env bash
# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.sh --continue works.
#
# Once tools/dmc-release.json pins a dmc release, this first runs tools/get-dmc.sh (it installs or updates bin/dmc,
# and prints nothing when it is current), then hands over to "bin/dmc play", which starts Claude Code the same way.
# Until a release is pinned, or if dmc cannot be had, it starts Claude Code itself, as it always has.
#
# Usage:     ./play.sh [claude arguments...]     (from any folder; it moves to the toolkit root first)
# Inputs:    tools/dmc-release.json, to see whether a release is pinned; the arguments, passed on unchanged.
# Outputs:   whatever Claude Code prints. When no bin/dmc that runs can be had, a warning on stderr first: get-dmc's own
#            error line, if get-dmc failed ("get-dmc: ..."), then this script's line ("play: dmc is not available ..."),
#            so one line if get-dmc succeeded but the dmc it left does not run, two if get-dmc failed. get-dmc may install
#            dmc into bin/ (see tools/get-dmc.sh); this script writes nothing itself.
# Exit code: that of "bin/dmc play" or of claude, which is the session's. 1 if it cannot move to the toolkit root. If
#            claude is not on the PATH, exec fails and the shell exits with 127.
#
# It never stops play because dmc is missing or broken: it starts Claude Code without it, and says so. It never hands
# the session to a bin/dmc that fails to print its version, and never changes the arguments it was given.
cd "$(dirname "$0")" || exit 1
if grep -Eq '^[[:space:]]*"tag"[[:space:]]*:[[:space:]]*"dmc-v' tools/dmc-release.json 2>/dev/null; then
    bash tools/get-dmc.sh
    # Only a dmc that runs here (not blocked, not damaged) gets the session. Git Bash runs bin/dmc.exe as ./bin/dmc.
    if { [ -f bin/dmc ] || [ -f bin/dmc.exe ]; } && ./bin/dmc --version >/dev/null 2>&1; then
        exec ./bin/dmc play "$@"
    fi
    echo "play: dmc is not available, so Claude Code starts without it, and DMC cannot roll dice, draw maps or read PDFs until get-dmc succeeds." >&2
fi
exec claude --model sonnet --effort high --settings .claude/play-settings.json "$@"
