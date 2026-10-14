"""The break reminder the play launcher runs before each player message."""
import json
import os
import tempfile
import unittest

from support import run_script

import context_check


def assistant(context, sidechain=False):
    # Split the context the way the API reports it: a little fresh input, most of it from the cache.
    usage = {"input_tokens": 10, "cache_read_input_tokens": context - 1010, "cache_creation_input_tokens": 1000,
             "output_tokens": 500}
    return {"type": "assistant", "isSidechain": sidechain, "message": {"id": "m", "usage": usage, "content": []}}


def user(text):
    return {"type": "user", "message": {"role": "user", "content": text}}


class ContextCheck(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)

    def log(self, *records, raw=""):
        path = os.path.join(self.folder.name, "session.jsonl")
        with open(path, "w", encoding="utf-8") as handle:
            for record in records:
                handle.write(json.dumps(record) + "\n")
            handle.write(raw)
        return path

    def check(self, path, hook_input=None):
        stdin = json.dumps({"session_id": "s", "transcript_path": path}) if hook_input is None else hook_input
        result = run_script("context_check.py", input=stdin)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stderr, "")
        return result.stdout.strip()

    def test_nothing_below_the_threshold(self):
        self.assertEqual(self.check(self.log(user("hi"), assistant(149999), user("go on"))), "")

    def test_the_first_band_from_150k(self):
        line = self.check(self.log(assistant(90000), user("go on"), assistant(163400), user("I attack")))
        self.assertEqual(line, "Context 163k, break due since 150k: at the next break point, offer a full save "
                               "and a fresh session, unless the player has declined since 150k.")

    def test_the_band_rises_every_40k(self):
        line = self.check(self.log(assistant(195000), user("go on")))
        self.assertTrue(line.startswith("Context 195k, break due since 190k:"), line)
        self.assertTrue(line.endswith("declined since 190k."), line)

    def test_only_the_last_call_counts(self):
        self.assertEqual(self.check(self.log(assistant(200000), assistant(80000), user("go on"))), "")

    def test_subagents_and_empty_usage_are_skipped(self):
        empty = {"type": "assistant", "message": {"id": "x", "model": "<synthetic>", "usage": {}, "content": []}}
        line = self.check(self.log(assistant(160000), assistant(20000, sidechain=True), empty))
        self.assertTrue(line.startswith("Context 160k"), line)

    def test_nothing_right_after_a_compaction(self):
        compacted = {"type": "system", "subtype": "compact_boundary"}
        self.assertEqual(self.check(self.log(assistant(240000), compacted, user("summary"))), "")

    def test_a_cut_last_line_and_junk_are_ignored(self):
        self.assertTrue(self.check(self.log(assistant(170000), raw='{"type": "assistant", "mess')).startswith(
            "Context 170k"))

    def test_a_log_longer_than_one_chunk(self):
        filler = user("x" * (context_check.CHUNK // 2))  # each line crosses a chunk boundary
        path = self.log(assistant(175000), *[filler] * 5, user("go on"))
        self.assertGreater(os.path.getsize(path), 2 * context_check.CHUNK)
        self.assertTrue(self.check(path).startswith("Context 175k"))

    def test_nothing_for_a_missing_log(self):
        self.assertEqual(self.check(os.path.join(self.folder.name, "missing.jsonl")), "")

    def test_nothing_for_a_log_without_usage(self):
        self.assertEqual(self.check(self.log(user("hi"))), "")
        self.assertEqual(self.check(self.log()), "")

    def test_nothing_for_bad_hook_input(self):
        for stdin in ("", "not json", "[1, 2]", '{"transcript_path": 5}', '{"session_id": "s"}'):
            with self.subTest(stdin=stdin):
                self.assertEqual(self.check(None, hook_input=stdin), "")


if __name__ == "__main__":
    unittest.main()
