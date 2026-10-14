"""The dice rollers: roll.sh and roll.ps1 must behave the same way."""
import re
import unittest

from support import find_bash, find_powershell, need, run

LINE = re.compile(r"^(\S+): \[([\d, ]+)\](?: keep \[([\d, ]+)\])?(?: ([+-]\d+))? = (-?\d+)$")


def numbers(text):
    return [int(n) for n in text.split(", ")]


class RollerTests:
    """Tests shared by both rollers. Subclasses say how to start the script."""

    command = None

    def roll(self, *exprs):
        return run(self.command + list(exprs))

    def parse(self, line):
        match = LINE.match(line)
        self.assertTrue(match, f"unexpected output line: {line!r}")
        expr, dice, kept, mod, total = match.groups()
        return {"expr": expr, "dice": numbers(dice), "kept": numbers(kept) if kept else None,
                "mod": int(mod) if mod else 0, "total": int(total)}

    def one(self, expr):
        result = self.roll(expr)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        lines = result.stdout.splitlines()
        self.assertEqual(len(lines), 1, result.stdout)
        return self.parse(lines[0])

    def test_single_die_with_modifier(self):
        r = self.one("1d20+5")
        self.assertEqual(r["expr"], "1d20+5")
        self.assertEqual(len(r["dice"]), 1)
        self.assertTrue(1 <= r["dice"][0] <= 20)
        self.assertEqual(r["mod"], 5)
        self.assertEqual(r["total"], r["dice"][0] + 5)

    def test_count_defaults_to_one(self):
        r = self.one("d6")
        self.assertEqual(len(r["dice"]), 1)
        self.assertEqual(r["total"], r["dice"][0])

    def test_negative_modifier(self):
        r = self.one("3d6-2")
        self.assertEqual(len(r["dice"]), 3)
        self.assertEqual(r["mod"], -2)
        self.assertEqual(r["total"], sum(r["dice"]) - 2)

    def test_no_modifier_is_not_shown(self):
        result = self.roll("2d8")
        self.assertRegex(result.stdout.strip(), r"^2d8: \[\d, \d\] = \d+$")

    def test_keep_highest(self):
        r = self.one("4d6kh3")
        self.assertEqual(len(r["dice"]), 4)
        self.assertEqual(r["kept"], sorted(r["dice"], reverse=True)[:3])
        self.assertEqual(r["total"], sum(r["kept"]))

    def test_advantage_and_disadvantage(self):
        # Repeated so that a roller keeping the wrong die cannot pass by luck.
        result = self.roll(*(["2d20kh1+5", "2d20kl1+5"] * 10))
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        for line in result.stdout.splitlines():
            r = self.parse(line)
            pick = max if "kh" in r["expr"] else min
            self.assertEqual(r["kept"], [pick(r["dice"])], line)
            self.assertEqual(r["total"], pick(r["dice"]) + 5, line)

    def test_upper_case_is_accepted(self):
        r = self.one("2D20KH1")
        self.assertEqual(r["expr"], "2D20KH1")
        self.assertEqual(r["kept"], [max(r["dice"])])

    def test_leading_zeros(self):
        r = self.one("02d06")
        self.assertEqual(len(r["dice"]), 2)
        self.assertTrue(all(1 <= d <= 6 for d in r["dice"]))

    def test_several_expressions_in_order(self):
        result = self.roll("1d20+5", "2d6+3", "4d6kh3")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        rolls = [self.parse(line) for line in result.stdout.splitlines()]
        self.assertEqual([r["expr"] for r in rolls], ["1d20+5", "2d6+3", "4d6kh3"])

    def test_every_die_is_in_range_and_every_face_turns_up(self):
        result = self.roll("100d4", "100d4", "100d1000", "100d2")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        d4a, d4b, d1000, d2 = [self.parse(line)["dice"] for line in result.stdout.splitlines()]
        self.assertEqual(len(d1000), 100)
        self.assertTrue(all(1 <= d <= 1000 for d in d1000))
        # 200 d4s missing a face has odds of about 1 in 10^24.
        self.assertEqual(set(d4a + d4b), {1, 2, 3, 4})
        self.assertEqual(set(d2), {1, 2})

    def test_no_arguments_prints_usage(self):
        result = self.roll()
        self.assertEqual(result.returncode, 1)
        self.assertIn("usage:", result.stdout)

    def test_rejects_what_is_not_dice(self):
        for expr in ("banana", "1d", "d", "1d20+", "1d20kh", "2d6+1d4", "1d20 +5"):
            with self.subTest(expr=expr):
                result = self.roll(expr)
                self.assertEqual(result.returncode, 1)
                self.assertIn("not a dice expression", result.stdout)
                self.assertNotIn(" = ", result.stdout)

    def test_rejects_out_of_range(self):
        for expr in ("101d6", "0d6", "1d1", "1d1001"):
            with self.subTest(expr=expr):
                result = self.roll(expr)
                self.assertEqual(result.returncode, 1)
                self.assertIn("out of range", result.stdout)
                self.assertNotIn(" = ", result.stdout)

    def test_rejects_keeping_more_dice_than_rolled(self):
        for expr, text in (("2d20kh3", "cannot keep 3 of 2 dice"), ("2d20kl0", "cannot keep 0 of 2 dice")):
            with self.subTest(expr=expr):
                result = self.roll(expr)
                self.assertEqual(result.returncode, 1)
                self.assertIn(text, result.stdout)
                self.assertNotIn(" = ", result.stdout)

    def test_a_bad_expression_does_not_stop_the_good_ones(self):
        result = self.roll("1d20", "banana", "1d6")
        self.assertEqual(result.returncode, 1)
        lines = result.stdout.splitlines()
        self.assertEqual(len(lines), 3, result.stdout)
        self.assertEqual(self.parse(lines[0])["expr"], "1d20")
        self.assertIn("banana: not a dice expression", lines[1])
        self.assertEqual(self.parse(lines[2])["expr"], "1d6")


class BashRollerTest(RollerTests, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        bash = find_bash()
        need(bash, "bash")
        cls.command = [bash, "scripts/roll.sh"]


class PowerShellRollerTest(RollerTests, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        shell = find_powershell()
        need(shell, "PowerShell")
        cls.command = [shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/roll.ps1"]


if __name__ == "__main__":
    unittest.main()
