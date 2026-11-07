#!/usr/bin/env bash
# Decides whether a change needs the toolkit's build and tests: the one home of that rule. It reads the changes on
# standard input as `git diff --name-status --no-renames` prints them, a status letter, a tab and a path relative to
# the repository root, one per line, and prints "true" or "false". A deleted file needs them whatever it is, because
# tests check that some Markdown and text files exist (the .docs/ files a prompt names, the fonts' licences). So does
# any other change to a file that is not Markdown or text, and to the prompts the toolkit tests read as DMC's
# instructions (the root CLAUDE.md and README.md, .claude/skills/ and kit/). No changes at all prints "false". Run by
# .github/workflows/tests.yml on every pull request and push to main, and by ToolkitTests on example lists. It writes
# nothing and always exits 0.

code=false
while IFS=$'\t' read -r status file || [ -n "$status" ]; do
    file=${file%$'\r'}
    case $status in
        '') continue ;;
        D*) code=true; continue ;;
    esac
    case $file in
        CLAUDE.md | README.md | .claude/skills/* | kit/*) code=true ;;
        *.md | *.txt) ;;
        *) code=true ;;
    esac
done
echo "$code"
