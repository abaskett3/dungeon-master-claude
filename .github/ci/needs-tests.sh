#!/usr/bin/env bash
# Decides whether a change needs the toolkit's build and tests: the one home of that rule. It reads the changed paths
# on standard input, one per line, relative to the repository root with / separators, and prints "true" or "false".
# Anything that is not a Markdown or text file needs them, and so do the prompts the toolkit tests read as DMC's
# instructions (the root CLAUDE.md and README.md, .claude/skills/ and kit/). No paths at all prints "false". Run by
# .github/workflows/tests.yml on every pull request and push to main, and by ToolkitTests on example lists. It writes
# nothing and always exits 0.

code=false
while IFS= read -r file || [ -n "$file" ]; do
    file=${file%$'\r'}
    case $file in
        '') ;;
        CLAUDE.md | README.md | .claude/skills/* | kit/*) code=true ;;
        *.md | *.txt) ;;
        *) code=true ;;
    esac
done
echo "$code"
