# Dungeon Master Claude (DMC), developer instructions

This repository is the source of DMC, the Dungeon Master Claude toolkit. Help as a normal assistant. The Dungeon Master rules are in `kit/dm-rules.md` and are not instructions for this session.

## Git and GitHub

For work on the toolkit only. Nothing here applies during play.

- **Branches.** Never commit to `main`. Start each change on a new branch from an up-to-date `origin/main`, and keep one change per branch. Delete a branch once its PR has merged.
- **What goes in.** Campaigns, modules and the rules reference are personal or copyrighted and are git-ignored: never force-add anything under `adventures/`, `modules/` or `rules/`.
- **Commits.** Commit when the user asks, or as part of a plan they agreed to. Never skip hooks or signing. Never rewrite history that has been pushed.
- **Pull requests.** Every PR has a full description: what changed, why, the sources it rests on, how it was tested, and which documents it changed or why none needed to. After opening a PR, by you or by a subagent, run `gh pr view <n> --json body` and check the description is really there.
- **Documentation.** Every change updates the documentation in the same PR; a PR that leaves it stale is not finished. In code: an XML doc comment on every type and member it adds or changes, private ones included (the build catches only missing public ones), and a header comment on every script and workflow it adds or changes. In `.docs/`: the documents that describe what changed, including `.docs/roadmap.md` for status and `.docs/decisions.md` for any decision and its reason. A change to CLAUDE.md, a skill or a template changes how the product behaves, so it counts.
- **Linking issues.** A PR that finishes an issue says `Closes #N` in its description, so merging it closes the issue. A PR that does only part of one (a phase of a plan) says `Part of #N` instead and leaves it open; the PR that completes the last part uses `Closes #N`. Write each issue on its own line, one keyword per issue.
- **Review.** Run Claude Code's built-in code-review command once, on the finished PR. Check the fixes for its findings yourself; review again only if a fix turns into a redesign. Check the documentation is current, whether or not the review found anything.
- **Spec Kit.** Every change except prompt and context tuning from feedback goes through the `/speckit-*` workflow, as `.docs/development.md` § Spec-driven changes says.
- **Merging.** CI must pass before you ask the user to merge. The user merges; do not merge PRs yourself unless they ask.
