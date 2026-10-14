"""The session log digest, run against a small made-up Claude Code log."""
import json
import os
import tempfile
import unittest
from unittest import mock

from support import run_script

import transcript_digest as td

SESSION_ID = "abcd1234-0000-4000-8000-000000000000"
NARRATION = "You stand in the goblin hall."


def stamp(minute):
    return "2026-01-02T10:%02d:00.000Z" % minute


def user(minute, content, **extra):
    return dict({"type": "user", "timestamp": stamp(minute), "message": {"role": "user", "content": content}}, **extra)


def assistant(minute, mid, content, fresh, cached, written, output):
    usage = {"input_tokens": fresh, "cache_read_input_tokens": cached,
             "cache_creation_input_tokens": written, "output_tokens": output}
    return {"type": "assistant", "timestamp": stamp(minute),
            "message": {"id": mid, "model": "claude-test", "role": "assistant", "usage": usage, "content": content}}


def tool_use(tid, name, **args):
    return {"type": "tool_use", "id": tid, "name": name, "input": args}


def tool_result(tid, content, **extra):
    return dict({"type": "tool_result", "tool_use_id": tid, "content": content}, **extra)


# One session: two player messages, a compaction between them, one failed edit.
RECORDS = [
    user(0, "Resume adventures/silver-egg/ please"),
    user(0, "<system-reminder>be good</system-reminder>"),
    assistant(1, "m1", [{"type": "text", "text": NARRATION},
                        tool_use("t1", "Read", file_path="D:\\dmc\\adventures\\silver-egg\\game-state.md")],
              100, 900, 0, 50),
    # Claude Code writes one record per content block, repeating the message id.
    assistant(1, "m1", [tool_use("t2", "Edit", file_path="D:\\dmc\\adventures\\silver-egg\\story\\canon.md",
                                 old_string="ab", new_string="abcde")],
              100, 900, 0, 60),
    user(2, [tool_result("t1", "HP 10/12")]),
    user(2, [tool_result("t2", [{"type": "text", "text": "String to replace\nnot found"}], is_error=True)]),
    {"type": "system", "subtype": "compact_boundary", "timestamp": stamp(3)},
    user(3, "The story so far: goblins.", isCompactSummary=True),
    user(3, "Caveat: local command output", isMeta=True),
    user(4, [{"type": "text", "text": "No, that is wrong.\n[Request interrupted by user]"}]),
    assistant(5, "m2", [tool_use("t3", "Skill", skill="draw-map"),
                        tool_use("t4", "Write", file_path="adventures/silver-egg/maps/live.json", content="{}"),
                        tool_use("t5", "Bash", command="bash scripts/roll.sh 1d20")],
              200, 2500, 300, 40),
    user(6, [tool_result("t5", "1d20: [7] = 7")]),
    assistant(7, "m3", [{"type": "text", "text": "Sorry."}], 10, 3400, 90, 5),
]


def write_log(folder, records=RECORDS, session_id=SESSION_ID, garbage=True):
    path = os.path.join(folder, session_id + ".jsonl")
    with open(path, "w", encoding="utf-8") as handle:
        for record in records:
            handle.write(json.dumps(record) + "\n")
        if garbage:
            handle.write("this line is not JSON\n")
    return path


class Helpers(unittest.TestCase):
    def test_cut(self):
        self.assertEqual(td.cut("abcdef", 6), "abcdef")
        self.assertEqual(td.cut("abcdef", 3), "abc ...[+3 chars]")
        self.assertEqual(td.cut("a\r\nb", 10), "a\nb")

    def test_one_line(self):
        self.assertEqual(td.one_line("a\nb\nc", 20), "a / b / c")
        self.assertEqual(td.one_line("a\nb\nc", 5), "a / b ...[+4 chars]")

    def test_base_name(self):
        self.assertEqual(td.base_name("D:\\dmc\\adventures\\x\\game-state.md"), "game-state.md")
        self.assertEqual(td.base_name("/home/x/adventures/canon.md"), "canon.md")
        self.assertEqual(td.base_name(""), "")

    def test_describe_call(self):
        self.assertEqual(td.describe_call("Read", {"file_path": "a/b.md"}), "b.md")
        self.assertEqual(td.describe_call("Read", {"file_path": "a/b.md", "limit": 50}), "b.md (part)")
        self.assertEqual(td.describe_call("Read", {"file_path": "a/b.pdf", "pages": "1-3"}), "b.pdf (part)")
        self.assertEqual(td.describe_call("Write", {"file_path": "a/b.md", "content": "12345"}), "b.md [5 chars]")
        self.assertEqual(td.describe_call("Edit", {"file_path": "a\\b.md", "old_string": "xx", "new_string": "yyy"}),
                         "b.md [-2 +3]")
        self.assertEqual(td.describe_call("Bash", {"command": "ls"}), '{"command": "ls"}')
        self.assertLess(len(td.describe_call("Bash", {"command": "x" * 5000})), 260)

    def test_result_text(self):
        self.assertEqual(td.result_text("plain"), "plain")
        self.assertEqual(td.result_text([{"type": "text", "text": "a"}, {"type": "image"}, "stray"]), "a <image>")
        self.assertEqual(td.result_text(None), "None")

    def test_friction_marks(self):
        for text in ("No, that is wrong", "you said he was dead", "I told you already", "Don't do that",
                     "[Request interrupted by user]", "why did the goblin know that", "rewind please"):
            self.assertTrue(td.FRICTION.search(text), text)
        for text in ("I attack the goblin", "18 and 4", "I open the door", "Wrongful accusations abound"):
            self.assertFalse(td.FRICTION.search(text), text)

    def test_table(self):
        counter = td.collections.Counter({"Read": 3, "Edit": 1, "": 2})
        self.assertEqual(td.table(counter), "Read x3, ? x2, Edit x1")
        self.assertEqual(td.table(counter, extra={"Read": 90, "Edit": 5, "": 0}, top=1), "Read x3 (90 chars)")
        self.assertEqual(td.table(td.collections.Counter()), "none")

    def test_log_dir_follows_the_claude_config_dir(self):
        with mock.patch.dict(os.environ, {"CLAUDE_CONFIG_DIR": os.path.join("some", "config")}):
            folder = td.log_dir()
        parent, project = os.path.split(folder)
        self.assertEqual(parent, os.path.join("some", "config", "projects"))
        self.assertRegex(project, r"^[A-Za-z0-9-]+$")
        self.assertTrue(project.endswith(os.path.basename(td.ROOT).replace("_", "-").replace(".", "-")))


class Parsing(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.tmp.cleanup)
        cls.s = td.Session(write_log(cls.tmp.name))

    def test_identity_and_times(self):
        s = self.s
        self.assertEqual(s.id, SESSION_ID)
        self.assertEqual((s.start, s.end), (stamp(0), stamp(7)))
        self.assertEqual(s.campaign(), "silver-egg")
        self.assertGreater(s.size, 0)

    def test_only_what_the_player_typed_is_a_turn(self):
        self.assertEqual([t["text"] for t in self.s.turns],
                         ["Resume adventures/silver-egg/ please", "No, that is wrong.\n[Request interrupted by user]"])
        self.assertEqual([t["time"] for t in self.s.turns], ["10:00:00", "10:04:00"])
        self.assertEqual(self.s.interrupts, 1)
        self.assertEqual(self.s.compactions, 1)

    def test_replies_are_credited_to_the_message_they_answer(self):
        first, second = self.s.turns
        self.assertEqual(first["reply_chars"], len(NARRATION))
        self.assertEqual(first["tools"], ["Read:game-state.md", "Edit:canon.md"])
        self.assertEqual(second["reply_chars"], len("Sorry."))
        self.assertEqual(second["tools"], ["Skill", "Write:live.json", "Bash"])

    def test_context_size_counts_each_api_call_once(self):
        s = self.s
        self.assertEqual(s.ctx, [1000, 3000, 3500])
        self.assertEqual([t["ctx"] for t in s.turns], [0, 1000])
        self.assertEqual(len(s.calls), 3)
        self.assertEqual(s.models, {"claude-test": 3})

    def test_tokens_take_the_last_usage_of_each_call(self):
        self.assertEqual(dict(self.s.tokens()), {"input_tokens": 310, "output_tokens": 105,
                                                 "cache_read_input_tokens": 6800,
                                                 "cache_creation_input_tokens": 390})

    def test_tool_counts(self):
        s = self.s
        self.assertEqual(s.tools, {"Read": 1, "Edit": 1, "Skill": 1, "Write": 1, "Bash": 1})
        self.assertEqual(s.skills, {"draw-map": 1})
        self.assertEqual(s.reads, {"game-state.md": 1})
        self.assertEqual(s.read_chars, {"game-state.md": len('"HP 10/12"')})
        self.assertEqual(s.writes, {"canon.md": 1, "live.json": 1})
        self.assertEqual(s.write_chars, {"canon.md": 7, "live.json": 2})
        self.assertEqual(set(s.result_chars), {"Read", "Edit", "Bash"})

    def test_tool_errors(self):
        self.assertEqual(self.s.errors, ["10:02:00 Edit canon.md: String to replace / not found"])

    def test_transcript_lines(self):
        text = "\n".join(self.s.lines)
        for expected in ("### PLAYER 10:00:00\nResume adventures/silver-egg/ please",
                         "### (injected) 10:00:00 <system-reminder>be good",
                         "### DM 10:01:00 [ctx 1000]\n" + NARRATION,
                         "  -> Read game-state.md",
                         "  -> Edit canon.md [-2 +5]",
                         "  <- 10 chars: HP 10/12",
                         " ERROR: String to replace / not found",
                         "--- CONTEXT COMPACTED 10:03:00",
                         "### COMPACTION SUMMARY 10:03:00\nThe story so far: goblins.",
                         "### (injected) 10:03:00 Caveat: local command output",
                         "  -> Write live.json [2 chars]"):
            self.assertIn(expected, text)
        order = [text.index(part) for part in ("### PLAYER 10:00:00", "### DM 10:01:00", "CONTEXT COMPACTED",
                                               "### PLAYER 10:04:00", "### DM 10:07:00")]
        self.assertEqual(order, sorted(order))

    def test_long_text_is_cut_in_the_transcript_but_counted_in_full(self):
        with tempfile.TemporaryDirectory() as folder:
            wall = "x" * 9000
            s = td.Session(write_log(folder, [user(0, "go"), assistant(1, "m1", [{"type": "text", "text": wall}],
                                                                       1, 2, 3, 4)]))
        self.assertEqual(s.turns[0]["reply_chars"], 9000)
        self.assertIn("...[+3000 chars]", "\n".join(s.lines))

    def test_an_empty_log(self):
        with tempfile.TemporaryDirectory() as folder:
            s = td.Session(write_log(folder, []))
        self.assertEqual((s.turns, s.ctx, s.start, s.campaign()), ([], [], "", "-"))
        self.assertIn("## Session abcd1234", td.stats(s))
        self.assertEqual(td.turn_index(s), "\n")

    def test_stats(self):
        text = td.stats(self.s)
        for expected in ("## Session abcd1234",
                         "2026-01-02 10:00 to 2026-01-02 10:07 UTC. Campaign: silver-egg.",
                         "**Models**: claude-test x3",
                         '**Player messages**: 2 (1 marked "!", 1 interrupted). **API calls**: 3.',
                         "starts at 1000 tokens, ends at 3500, peak 3500. About 1250 tokens added per player message. "
                         "Compactions: 1.",
                         "**Tokens**: output 105; input read from cache 6800, written to cache 390, uncached 310.",
                         "**Replies to the player**: average 17 chars, longest 29; 0 of 2 over 1,500 chars.",
                         "**Skills run**: draw-map x1",
                         "canon.md x1 (7 chars)",
                         "game-state.md x1 (10 chars)",
                         '+1000 at 10:00:00: "Resume adventures/silver-egg/ please" -> Read:game-state.md Edit:canon.md',
                         "**Tool errors** (1):"):
            self.assertIn(expected, text)

    def test_turn_index(self):
        lines = td.turn_index(self.s).splitlines()
        self.assertEqual(lines, [
            "   1 10:00:00 ctx=0k | Resume adventures/silver-egg/ please",
            "       reply %d chars; tools: Read:game-state.md Edit:canon.md" % len(NARRATION),
            "!  2 10:04:00 ctx=1k | No, that is wrong. / [Request interrupted by user]",
            "       reply 6 chars; tools: Skill Write:live.json Bash",
        ])


class CommandLine(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.logs = os.path.join(self.tmp.name, "logs")
        self.out = os.path.join(self.tmp.name, "out")
        os.mkdir(self.logs)
        write_log(self.logs)
        other = [user(0, "Start adventures/red-keep/ now"), assistant(1, "m9", [{"type": "text", "text": "Ok."}], 5, 5, 5, 5)]
        other = [dict(r, timestamp=r["timestamp"].replace("2026-01-02", "2026-03-04")) for r in other]
        write_log(self.logs, other, session_id="ffff9999-1111", garbage=False)
        write_log(self.logs, [user(0, "<system-reminder>nobody spoke</system-reminder>")], session_id="eeee0000-2222")
        with open(os.path.join(self.logs, "notes.txt"), "w") as handle:
            handle.write("not a session log")

    def digest(self, *args):
        return run_script("transcript_digest.py", "digest", "--logs", self.logs, "--out", self.out, *args)

    def test_list(self):
        result = run_script("transcript_digest.py", "list", "--logs", self.logs)
        self.assertEqual(result.returncode, 0, result.stderr)
        lines = result.stdout.splitlines()
        self.assertEqual(len(lines), 4)  # the heading and three sessions
        rows = {line.split()[0]: line for line in lines[1:]}
        self.assertEqual(set(rows), {"abcd1234", "ffff9999", "eeee0000"})
        self.assertIn("silver-egg", rows["abcd1234"])
        self.assertIn("Resume adventures/silver-egg/ please", rows["abcd1234"])
        self.assertIn("2026-01-02 10:00", rows["abcd1234"])
        self.assertIn("3500", rows["abcd1234"])
        self.assertIn("red-keep", rows["ffff9999"])

    def test_list_filters(self):
        def ids(*args):
            result = run_script("transcript_digest.py", "list", "--logs", self.logs, *args)
            self.assertEqual(result.returncode, 0, result.stderr)
            return {line.split()[0] for line in result.stdout.splitlines()[1:]}
        self.assertEqual(ids("--campaign", "red-keep"), {"ffff9999"})
        self.assertEqual(ids("--campaign", "nowhere"), set())
        self.assertEqual(ids("--after", "2026-02-01"), {"ffff9999"})
        self.assertEqual(ids("--after", "2026-01-02"), {"abcd1234", "ffff9999", "eeee0000"})
        self.assertEqual(ids("--after", "2026-01-03"), {"ffff9999"})

    def test_digest_writes_three_kinds_of_file(self):
        result = self.digest()
        self.assertEqual(result.returncode, 0, result.stderr)
        # The session where the player never spoke is left out.
        self.assertEqual(sorted(os.listdir(self.out)), [
            "abcd1234-transcript.md", "abcd1234-turns.txt", "ffff9999-transcript.md", "ffff9999-turns.txt", "stats.md"])
        with open(os.path.join(self.out, "stats.md"), encoding="utf-8") as handle:
            report = handle.read()
        self.assertTrue(report.startswith("# Transcript statistics"))
        self.assertIn("## Session abcd1234", report)
        self.assertIn("## Session ffff9999", report)
        self.assertIn(report, result.stdout, "the statistics are also printed")
        with open(os.path.join(self.out, "abcd1234-turns.txt"), encoding="utf-8") as handle:
            self.assertIn("!  2 10:04:00", handle.read())
        with open(os.path.join(self.out, "abcd1234-transcript.md"), encoding="utf-8") as handle:
            self.assertIn("### DM 10:01:00 [ctx 1000]", handle.read())

    def test_digest_of_chosen_sessions(self):
        result = self.digest("ffff")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sorted(os.listdir(self.out)), ["ffff9999-transcript.md", "ffff9999-turns.txt", "stats.md"])

    def test_digest_of_one_campaign(self):
        result = self.digest("--campaign", "silver-egg")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sorted(os.listdir(self.out)), ["abcd1234-transcript.md", "abcd1234-turns.txt", "stats.md"])

    def test_digest_with_nothing_to_show(self):
        for args in (["zzzz"], ["eeee"], ["--campaign", "nowhere"], ["--after", "2030-01-01"]):
            with self.subTest(args=args):
                result = self.digest(*args)
                self.assertEqual(result.returncode, 1)
                self.assertIn("No matching sessions", result.stderr)
                self.assertFalse(os.path.exists(self.out))

    def test_missing_log_folder(self):
        result = run_script("transcript_digest.py", "list", "--logs", os.path.join(self.tmp.name, "nope"))
        self.assertEqual(result.returncode, 1)
        self.assertIn("No session logs found", result.stderr)

    def test_non_ascii_text_survives(self):
        write_log(self.logs, [user(0, "Thrór says \u201chello\u201d \u2014 \u5317"),
                              assistant(1, "m1", [{"type": "text", "text": "\u2694 Roll 1d20."}], 1, 1, 1, 1)],
                  session_id="cccc7777-3333")
        result = run_script("transcript_digest.py", "digest", "cccc", "--logs", self.logs, "--out", self.out,
                            env=dict(os.environ, PYTHONIOENCODING="ascii"))
        self.assertEqual(result.returncode, 0, result.stderr)
        with open(os.path.join(self.out, "cccc7777-transcript.md"), encoding="utf-8") as handle:
            text = handle.read()
        self.assertIn("Thrór says \u201chello\u201d \u2014 \u5317", text)
        self.assertIn("\u2694 Roll 1d20.", text)


if __name__ == "__main__":
    unittest.main()
