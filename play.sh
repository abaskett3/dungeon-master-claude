#!/usr/bin/env bash
# Start a DMC play session: Sonnet at high effort, with the settings made for play.
# Extra arguments go to Claude Code, so ./play.sh --continue works.
cd "$(dirname "$0")" || exit 1
exec claude --model sonnet --effort high --settings .claude/play-settings.json "$@"
