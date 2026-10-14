"""The toolkit as a whole: settings, skills and the files the prompts point at."""
import glob
import json
import os
import re
import unittest

from support import ROOT, run

SKILLS = os.path.join(ROOT, ".claude", "skills")
PATH_IN_PROSE = re.compile(r"(?:templates|scripts)/[A-Za-z0-9_./-]+")


def read(*parts):
    with open(os.path.join(ROOT, *parts), encoding="utf-8") as handle:
        return handle.read()


def skill_names():
    return sorted(name for name in os.listdir(SKILLS) if os.path.isdir(os.path.join(SKILLS, name)))


def prompt_files():
    """Every file DMC reads as instructions, relative to the repository root."""
    files = ["CLAUDE.md", "README.md"]
    files += [os.path.join(".claude", "skills", name, "SKILL.md") for name in skill_names()]
    for folder, _, names in os.walk(os.path.join(ROOT, "templates")):
        files += [os.path.relpath(os.path.join(folder, n), ROOT) for n in names if n.endswith(".md")]
    return files


class Settings(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.settings = json.loads(read(".claude", "settings.json"))

    def test_every_allowed_script_exists(self):
        allowed = self.settings["permissions"]["allow"]
        named = sorted(set(re.findall(r"scripts/[\w.-]+", " ".join(allowed))))
        self.assertTrue(named, "no scripts are allowed at all")
        for path in named:
            self.assertTrue(os.path.isfile(os.path.join(ROOT, path)), f"settings.json allows {path}, which is missing")

    def test_every_script_can_run_without_a_permission_prompt(self):
        allowed = " ".join(self.settings["permissions"]["allow"])
        for path in glob.glob(os.path.join(ROOT, "scripts", "*.*")):
            name = os.path.basename(path)
            if name.endswith((".py", ".ps1", ".sh")):
                self.assertIn(f"scripts/{name} ", allowed, f"{name} is not in the allow list of settings.json")

    def test_hooks_are_commands(self):
        for event, groups in self.settings["hooks"].items():
            for group in groups:
                for hook in group["hooks"]:
                    self.assertEqual(hook["type"], "command", event)
                    self.assertTrue(hook["command"].strip(), event)


class Skills(unittest.TestCase):
    def test_every_skill_has_a_name_and_description(self):
        self.assertTrue(skill_names())
        for name in skill_names():
            with self.subTest(skill=name):
                text = read(".claude", "skills", name, "SKILL.md")
                match = re.match(r"---\n(.*?)\n---\n(.*)", text, re.S)
                self.assertTrue(match, "SKILL.md must start with a --- front matter block")
                fields = dict(line.split(":", 1) for line in match.group(1).splitlines() if ":" in line)
                self.assertEqual(fields.get("name", "").strip(), name, "name must match the folder")
                self.assertTrue(fields.get("description", "").strip(), "description is empty")
                self.assertTrue(match.group(2).strip(), "the skill has no instructions")

    def test_claude_md_lists_exactly_the_skills_that_exist(self):
        section = read("CLAUDE.md").split("\n## Skills\n", 1)[1].split("\n## ", 1)[0]
        listed = sorted(re.findall(r"^- `/([a-z0-9-]+)`", section, re.M))
        self.assertEqual(listed, skill_names())

    def test_the_readme_lists_every_skill(self):
        readme = read("README.md")
        for name in skill_names():
            self.assertIn(f"`/{name}`", readme, f"README.md does not mention /{name}")

    def test_skills_mentioned_in_prompts_exist(self):
        known = set(skill_names())
        for path in prompt_files():
            for name in re.findall(r"`/([a-z]+(?:-[a-z]+)+)`", read(path)):
                # Only names that follow the toolkit's own pattern; /compact and /clear are Claude Code's.
                self.assertIn(name, known, f"{path} mentions /{name}, which is not a skill")


class References(unittest.TestCase):
    def test_paths_named_in_prompts_exist(self):
        for path in prompt_files():
            for named in sorted(set(PATH_IN_PROSE.findall(read(path)))):
                named = named.rstrip("./")
                if named.startswith("scripts/.pylib"):
                    continue  # installed on the player's machine, not in the repository
                with self.subTest(file=path, path=named):
                    self.assertTrue(os.path.exists(os.path.join(ROOT, named)),
                                    f"{path} points at {named}, which does not exist")

    def test_a_new_campaign_gets_every_file_claude_md_describes(self):
        campaign = os.path.join(ROOT, "templates", "campaign")
        for name in ("player-options.md", "game-state.md", os.path.join("story", "canon.md"),
                     os.path.join("story", "summary.md"), os.path.join("story", "dm-notes.md"),
                     os.path.join("character-sheets", "minor-npcs.md")):
            self.assertTrue(os.path.isfile(os.path.join(campaign, name)), f"templates/campaign/{name} is missing")
        for folder in ("maps", os.path.join("story", "sessions")):
            self.assertTrue(os.path.isdir(os.path.join(campaign, folder)), f"templates/campaign/{folder}/ is missing")

    def test_no_template_is_empty(self):
        for path in prompt_files():
            self.assertTrue(read(path).strip(), f"{path} is empty")


class Scripts(unittest.TestCase):
    def test_python_scripts_compile(self):
        paths = glob.glob(os.path.join(ROOT, "scripts", "*.py"))
        self.assertTrue(paths)
        for path in paths:
            with open(path, encoding="utf-8") as handle:
                compile(handle.read(), path, "exec")

    def test_shell_scripts_have_unix_line_endings(self):
        # A CRLF anywhere in a .sh file stops bash running it; .gitattributes guards this.
        paths = glob.glob(os.path.join(ROOT, "scripts", "*.sh"))
        self.assertTrue(paths)
        for path in paths:
            with open(path, "rb") as handle:
                self.assertNotIn(b"\r", handle.read(), f"{os.path.basename(path)} has Windows line endings")
        self.assertIn("*.sh text eol=lf", read(".gitattributes"))

    def test_personal_files_stay_out_of_git(self):
        if not os.path.isdir(os.path.join(ROOT, ".git")):
            self.skipTest("not a git checkout")
        try:
            result = run(["git", "ls-files", "adventures", "modules", "rules", "scripts/.pylib"])
        except OSError:
            self.skipTest("git is not available")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sorted(result.stdout.split()), ["adventures/.gitkeep", "modules/.gitkeep", "rules/.gitkeep"])


if __name__ == "__main__":
    unittest.main()
