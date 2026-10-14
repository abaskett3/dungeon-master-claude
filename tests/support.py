"""Shared helpers for the toolkit's tests.

Run every test from the repository root with:
  python -m unittest discover -s tests

Only the Python standard library is needed. Tests that need something that
may be missing on a player's machine (bash, PowerShell, PyMuPDF) are skipped
there, but fail on CI, where everything is installed on purpose.
"""
import os
import shutil
import subprocess
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, "scripts")
sys.path.insert(0, SCRIPTS)

ON_CI = bool(os.environ.get("CI"))


def need(found, what):
    """Skip the test when an optional tool is missing; on CI, fail instead."""
    if found:
        return
    if ON_CI:
        raise AssertionError(f"{what} is missing on CI, where it must be installed.")
    raise unittest.SkipTest(f"{what} is not available")


def run(command, **kwargs):
    """Run a command from the repository root and capture its output as text."""
    kwargs.setdefault("cwd", ROOT)
    kwargs.setdefault("timeout", 120)
    return subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                          encoding="utf-8", errors="replace", **kwargs)


def run_script(name, *args, **kwargs):
    """Run one of the Python scripts the way DMC does."""
    return run([sys.executable, os.path.join("scripts", name), *args], **kwargs)


def has_pymupdf():
    sys.path.insert(0, os.path.join(SCRIPTS, ".pylib"))
    try:
        import pymupdf  # noqa: F401
    except ImportError:
        return False
    return True


def find_bash():
    """Full path of a bash that works, or None.

    On Windows a bare "bash" can start the WSL launcher in System32 instead of
    Git Bash, so candidates are tried by full path until one answers.
    """
    candidates = [shutil.which("bash")]
    if os.name == "nt":
        git = shutil.which("git")
        if git:
            candidates.append(os.path.join(os.path.dirname(os.path.dirname(git)), "bin", "bash.exe"))
        for base in (os.environ.get("ProgramFiles"), os.environ.get("ProgramFiles(x86)")):
            if base:
                candidates.append(os.path.join(base, "Git", "bin", "bash.exe"))
    for path in candidates:
        if not path or not os.path.isfile(path):
            continue
        try:
            probe = run([path, "-c", "echo ok"], timeout=30)
        except (OSError, subprocess.TimeoutExpired):
            continue
        if probe.returncode == 0 and probe.stdout.strip() == "ok":
            return path
    return None


def find_powershell():
    return shutil.which("pwsh") or shutil.which("powershell")
