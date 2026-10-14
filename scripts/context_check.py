#!/usr/bin/env python3
"""Tell DMC when the conversation has grown big enough to offer a break.

Run by the play launcher's UserPromptSubmit hook (.claude/play-settings.json)
before each player message. Claude Code passes the hook a JSON object on stdin
that includes "transcript_path", the session's log, and adds whatever this
script prints to DMC's context. DMC cannot see its own context size; this is
how it learns it.

The size is the context of the last API call in the log: its input tokens plus
the tokens read from and written to the cache, as transcript_digest.py counts
them. Below THRESHOLD nothing is printed. From THRESHOLD on, one line is
printed on every message, naming a band that rises every BAND tokens, so an
offer the player declined is made again once the next band is reached.

It must never get in the way of a prompt: on any error it prints nothing and
exits 0. Only the Python standard library is needed.

Usage (as the hook runs it):
  python scripts/context_check.py < hook-input.json
"""
import json
import os
import sys

THRESHOLD = 150000
BAND = 40000
CHUNK = 1 << 20  # bytes of the log read at a time, from the end


def context_of(record):
    """The context size of an assistant record from the main conversation, or None."""
    if record.get("type") != "assistant" or record.get("isSidechain"):
        return None
    usage = (record.get("message") or {}).get("usage") or {}
    size = (usage.get("input_tokens", 0) + usage.get("cache_read_input_tokens", 0)
            + usage.get("cache_creation_input_tokens", 0))
    return size or None  # synthetic messages (errors, interruptions) carry zero usage


def last_context(path):
    """Context of the last call in the log; 0 if a compaction came after it or there is none."""
    with open(path, "rb") as handle:
        handle.seek(0, os.SEEK_END)
        end = handle.tell()
        tail = b""
        while end > 0:
            start = max(0, end - CHUNK)
            handle.seek(start)
            tail = handle.read(end - start) + tail
            end = start
            lines = tail.split(b"\n")
            if end > 0:
                tail, lines = lines[0], lines[1:]  # the first line may be cut; read it whole next time
            else:
                tail = b""
            for raw in reversed(lines):
                try:
                    record = json.loads(raw)
                except ValueError:
                    continue
                if not isinstance(record, dict):
                    continue
                if record.get("type") == "system" and record.get("subtype") == "compact_boundary":
                    return 0
                size = context_of(record)
                if size:
                    return size
    return 0


def message(size):
    if size < THRESHOLD:
        return ""
    band = THRESHOLD + (size - THRESHOLD) // BAND * BAND
    return ("Context %dk, break due since %dk: at the next break point, offer a full save and a fresh "
            "session, unless the player has declined since %dk." % (size // 1000, band // 1000, band // 1000))


def main():
    try:
        hook = json.loads(sys.stdin.read() or "{}")
        path = hook.get("transcript_path") if isinstance(hook, dict) else None
        if path:
            line = message(last_context(os.path.expanduser(path)))
            if line:
                print(line)
    except Exception:  # noqa: BLE001 - a broken check must never block the player's message
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
