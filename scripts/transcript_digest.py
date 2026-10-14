#!/usr/bin/env python3
"""Condense Claude Code session logs into something small enough to review.

Claude Code records every session of this project as a JSONL file under
~/.claude/projects/<project>/. Those files are far too big to read directly
(page images and file contents are stored inline). This script turns them into
a readable transcript plus the numbers needed to judge how the toolkit's
prompts performed. The review-transcript skill uses it.

Usage:
  python scripts/transcript_digest.py list [--campaign NAME]
  python scripts/transcript_digest.py digest [SESSION ...] [--campaign NAME]
                                             [--after YYYY-MM-DD] [--out DIR]

  SESSION is the start of a session id, as shown by "list". With no SESSION,
  "digest" takes every session that touched the campaign (or every session).

Output of "digest" (default DIR is adventures/.transcript-review, which is
overwritten on each run and is not in the repository):
  DIR/stats.md              the numbers for each session (also printed)
  DIR/<id>-turns.txt        one line per player message: time, context size,
                            the message, then the size and tool calls of the
                            reply. Lines marked "!" may show friction.
  DIR/<id>-transcript.md    the whole session in order, with tool calls and
                            results cut down to a line each

Times are UTC. "ctx" is the number of tokens sent to the model on that call.

Only the Python standard library is needed.
"""
import argparse
import collections
import datetime
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Player messages matching this are marked "!" in the turn index. It only
# points at places to read; most friction has to be found by reading.
FRICTION = re.compile(
    r"request interrupted|\bwrong\b|\bagain\b|\brewind\b|\bconsisten|\bconfus|"
    r"\bwhy (are|did|is|do)\b|you said|\bi (said|told|asked)\b|\bdon'?t\b|\bstop\b|"
    r"\bnot what\b|\bimpossible\b|\bunfair\b|\bno fun\b|\bnever mind\b|"
    r"\bactually\b|\bincorrect\b|\bmistake\b|\bforgot\b|\bsorry\b",
    re.I,
)
CAMPAIGN = re.compile(r"adventures/([a-z0-9][a-z0-9-]*)/")


def log_dir():
    base = os.environ.get("CLAUDE_CONFIG_DIR") or os.path.join(os.path.expanduser("~"), ".claude")
    return os.path.join(base, "projects", re.sub(r"[^A-Za-z0-9]", "-", ROOT))


def cut(text, limit):
    text = text.replace("\r", "")
    if len(text) <= limit:
        return text
    return "%s ...[+%d chars]" % (text[:limit], len(text) - limit)


def one_line(text, limit):
    return cut(" / ".join(text.split("\n")), limit)


def base_name(path):
    return str(path).replace("\\", "/").split("/")[-1]


def describe_call(name, args):
    if name in ("Read", "Write", "Edit", "NotebookEdit"):
        desc = base_name(args.get("file_path", ""))
        if name == "Write":
            desc += " [%d chars]" % len(args.get("content", ""))
        elif name == "Edit":
            desc += " [-%d +%d]" % (len(args.get("old_string", "")), len(args.get("new_string", "")))
        elif args.get("limit") or args.get("pages"):
            desc += " (part)"
        return desc
    return one_line(json.dumps(args), 220)


def result_text(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return " ".join(c.get("text", "<%s>" % c.get("type", "?")) for c in content if isinstance(c, dict))
    return str(content)


class Session:
    def __init__(self, path):
        self.path = path
        self.id = os.path.basename(path)[:-len(".jsonl")]
        self.size = os.path.getsize(path)
        self.start = self.end = ""
        self.campaigns = collections.Counter()
        self.models = collections.Counter()
        self.calls = {}            # API message id -> usage (last one wins)
        self.ctx = []              # context size of each API call, in order
        self.compactions = 0
        self.interrupts = 0
        self.tools = collections.Counter()
        self.skills = collections.Counter()
        self.writes = collections.Counter()
        self.write_chars = collections.Counter()
        self.reads = collections.Counter()
        self.read_chars = collections.Counter()
        self.result_chars = collections.Counter()
        self.errors = []
        self.turns = []            # player messages with the reply that followed
        self.lines = []            # transcript lines
        self._parse()

    def _parse(self):
        with open(self.path, encoding="utf-8") as handle:
            self._parse_lines(handle)

    def _parse_lines(self, handle):
        tool_of = {}               # tool_use id -> (name, file)
        seen_ids = set()
        ctx_now = 0
        for raw in handle:
            try:
                rec = json.loads(raw)
            except ValueError:
                continue
            for name in CAMPAIGN.findall(raw.replace("\\\\", "/")):
                self.campaigns[name] += 1
            stamp = rec.get("timestamp") or ""
            if stamp:
                self.start = self.start or stamp
                self.end = stamp
            clock = stamp[11:19]
            kind = rec.get("type")
            msg = rec.get("message") or {}

            if kind == "system":
                if rec.get("subtype") == "compact_boundary":
                    self.compactions += 1
                    self.lines.append("\n--- CONTEXT COMPACTED %s" % clock)
                continue

            if kind == "assistant":
                usage = msg.get("usage") or {}
                ctx_now = (usage.get("input_tokens", 0) + usage.get("cache_read_input_tokens", 0)
                           + usage.get("cache_creation_input_tokens", 0))
                mid = msg.get("id")
                self.calls[mid] = usage
                if mid not in seen_ids:
                    seen_ids.add(mid)
                    self.ctx.append(ctx_now)
                    self.models[msg.get("model")] += 1
                for block in msg.get("content") or []:
                    if block.get("type") == "text" and block.get("text", "").strip():
                        text = block["text"]
                        self.lines.append("\n### DM %s [ctx %d]\n%s" % (clock, ctx_now, cut(text, 6000)))
                        if self.turns:
                            self.turns[-1]["reply_chars"] += len(text)
                    elif block.get("type") == "tool_use":
                        name, args = block.get("name", "?"), block.get("input") or {}
                        target = base_name(args.get("file_path", "")) if "file_path" in args else ""
                        tool_of[block.get("id")] = (name, target)
                        self.tools[name] += 1
                        if name == "Skill":
                            self.skills[args.get("skill", "?")] += 1
                        if name in ("Edit", "Write"):
                            self.writes[target] += 1
                            self.write_chars[target] += sum(
                                len(args.get(k, "")) for k in ("old_string", "new_string", "content"))
                        if name == "Read":
                            self.reads[target] += 1
                        self.lines.append("  -> %s %s" % (name, describe_call(name, args)))
                        if self.turns:
                            self.turns[-1]["tools"].append(name + (":" + target if target else ""))
                continue

            if kind != "user":
                continue
            content = msg.get("content")
            blocks = [{"type": "text", "text": content}] if isinstance(content, str) else (content or [])
            for block in blocks:
                if block.get("type") == "tool_result":
                    name, target = tool_of.get(block.get("tool_use_id"), ("?", ""))
                    size = len(json.dumps(block.get("content")))
                    self.result_chars[name] += size
                    if name == "Read":
                        self.read_chars[target] += size
                    text = result_text(block.get("content"))
                    flag = ""
                    if block.get("is_error"):
                        flag = " ERROR"
                        self.errors.append("%s %s %s: %s" % (clock, name, target, one_line(text, 160)))
                    self.lines.append("  <- %d chars%s: %s" % (size, flag, one_line(text, 160)))
                elif block.get("type") == "text":
                    text = block.get("text", "")
                    if rec.get("isCompactSummary"):
                        self.lines.append("\n### COMPACTION SUMMARY %s\n%s" % (clock, cut(text, 4000)))
                    elif rec.get("isMeta") or text.startswith(("<local-command", "<command-name>", "<system-reminder>")):
                        self.lines.append("\n### (injected) %s %s" % (clock, one_line(text, 200)))
                    else:
                        if "[Request interrupted" in text:
                            self.interrupts += 1
                        self.lines.append("\n### PLAYER %s\n%s" % (clock, cut(text, 3000)))
                        self.turns.append({"time": clock, "ctx": ctx_now, "text": text,
                                           "reply_chars": 0, "tools": []})

    def tokens(self):
        total = collections.Counter()
        for usage in self.calls.values():
            for key in ("input_tokens", "output_tokens", "cache_read_input_tokens", "cache_creation_input_tokens"):
                total[key] += usage.get(key, 0)
        return total

    def campaign(self):
        return self.campaigns.most_common(1)[0][0] if self.campaigns else "-"


def table(counter, extra=None, top=8):
    rows = []
    for key, count in counter.most_common(top):
        note = " (%d chars)" % extra[key] if extra else ""
        rows.append("%s x%d%s" % (key or "?", count, note))
    return ", ".join(rows) or "none"


def stats(s):
    tok = s.tokens()
    replies = [t["reply_chars"] for t in s.turns if t["reply_chars"]]
    out = ["## Session %s" % s.id[:8], ""]
    out.append("- **When**: %s to %s UTC. Campaign: %s. Log size %d KB." % (
        s.start[:16].replace("T", " "), s.end[:16].replace("T", " "), s.campaign(), s.size // 1024))
    out.append("- **Models**: %s" % table(s.models))
    out.append("- **Player messages**: %d (%d marked \"!\", %d interrupted). **API calls**: %d." % (
        len(s.turns), sum(1 for t in s.turns if FRICTION.search(t["text"])), s.interrupts, len(s.calls)))
    if s.ctx:
        growth = (s.ctx[-1] - s.ctx[0]) // max(1, len(s.turns))
        out.append("- **Context**: starts at %d tokens, ends at %d, peak %d. About %d tokens added per player message. Compactions: %d." % (
            s.ctx[0], s.ctx[-1], max(s.ctx), growth, s.compactions))
    out.append("- **Tokens**: output %d; input read from cache %d, written to cache %d, uncached %d." % (
        tok["output_tokens"], tok["cache_read_input_tokens"], tok["cache_creation_input_tokens"], tok["input_tokens"]))
    if replies:
        out.append("- **Replies to the player**: average %d chars, longest %d; %d of %d over 1,500 chars." % (
            sum(replies) // len(replies), max(replies), sum(1 for r in replies if r > 1500), len(replies)))
    out.append("- **Tool calls**: %s" % table(s.tools, top=12))
    out.append("- **Skills run**: %s" % table(s.skills))
    out.append("- **Files written** (calls, chars sent): %s" % table(s.writes, s.write_chars))
    out.append("- **Files read** (calls, chars returned): %s" % table(s.reads, s.read_chars))
    out.append("- **Tool output by tool** (chars): %s" % (
        ", ".join("%s %d" % kv for kv in s.result_chars.most_common(8)) or "none"))
    jumps = sorted(((b["ctx"] - a["ctx"], a) for a, b in zip(s.turns, s.turns[1:]) if b["ctx"] > a["ctx"]),
                   key=lambda j: -j[0])[:5]
    if jumps:
        out.append("- **Biggest context jumps** (tokens added answering one message):")
        for size, turn in jumps:
            out.append("  - +%d at %s: \"%s\" -> %s" % (
                size, turn["time"], one_line(turn["text"], 70), " ".join(turn["tools"][:10]) or "no tools"))
    if s.errors:
        out.append("- **Tool errors** (%d):" % len(s.errors))
        out.extend("  - %s" % e for e in s.errors[:10])
    return "\n".join(out) + "\n"


def turn_index(s):
    out = []
    for n, turn in enumerate(s.turns, 1):
        mark = "!" if FRICTION.search(turn["text"]) else " "
        out.append("%s%3d %s ctx=%dk | %s" % (mark, n, turn["time"], turn["ctx"] // 1000, one_line(turn["text"], 600)))
        out.append("       reply %d chars; tools: %s" % (turn["reply_chars"], " ".join(turn["tools"]) or "none"))
    return "\n".join(out) + "\n"


def load(args):
    folder = args.logs or log_dir()
    if not os.path.isdir(folder):
        sys.exit("No session logs found at %s. Pass --logs DIR." % folder)
    paths = [os.path.join(folder, f) for f in os.listdir(folder) if f.endswith(".jsonl")]
    paths.sort(key=os.path.getmtime)
    sessions = []
    for path in paths:
        wanted = getattr(args, "session", None)
        if wanted and not any(os.path.basename(path).startswith(w) for w in wanted):
            continue
        s = Session(path)
        if args.campaign and args.campaign not in s.campaigns:
            continue
        if getattr(args, "after", None) and s.end[:10] < args.after:
            continue
        sessions.append(s)
    return sessions


def cmd_list(args):
    print("%-9s %-17s %-17s %6s %5s %8s  %-20s %s" % (
        "id", "start (UTC)", "end (UTC)", "KB", "msgs", "peak ctx", "campaign", "first message"))
    for s in load(args):
        first = one_line(s.turns[0]["text"], 50) if s.turns else ""
        print("%-9s %-17s %-17s %6d %5d %8d  %-20s %s" % (
            s.id[:8], s.start[:16].replace("T", " "), s.end[:16].replace("T", " "), s.size // 1024,
            len(s.turns), max(s.ctx) if s.ctx else 0, s.campaign(), first))


def cmd_digest(args):
    sessions = [s for s in load(args) if s.turns]
    if not sessions:
        sys.exit("No matching sessions with player messages.")
    out_dir = args.out or os.path.join(ROOT, "adventures", ".transcript-review")
    os.makedirs(out_dir, exist_ok=True)
    report = ["# Transcript statistics", "",
              "Generated %s by scripts/transcript_digest.py." % datetime.date.today().isoformat(), ""]
    for s in sessions:
        report.append(stats(s))
        with open(os.path.join(out_dir, "%s-turns.txt" % s.id[:8]), "w", encoding="utf-8") as fh:
            fh.write(turn_index(s))
        with open(os.path.join(out_dir, "%s-transcript.md" % s.id[:8]), "w", encoding="utf-8") as fh:
            fh.write("\n".join(s.lines) + "\n")
    text = "\n".join(report)
    with open(os.path.join(out_dir, "stats.md"), "w", encoding="utf-8") as fh:
        fh.write(text)
    sys.stdout.write(text)
    print("\nWrote stats.md and, for each session, <id>-turns.txt and <id>-transcript.md to %s" % out_dir)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    for name, func in (("list", cmd_list), ("digest", cmd_digest)):
        p = sub.add_parser(name)
        p.set_defaults(func=func)
        p.add_argument("--campaign", help="only sessions that touched adventures/<NAME>/")
        p.add_argument("--after", help="only sessions that ended on or after this date (YYYY-MM-DD, UTC)")
        p.add_argument("--logs", help="folder of session .jsonl files (default: this project's Claude Code logs)")
        if name == "digest":
            p.add_argument("session", nargs="*", help="start of a session id")
            p.add_argument("--out", help="output folder")
    args = parser.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    args.func(args)


if __name__ == "__main__":
    main()
