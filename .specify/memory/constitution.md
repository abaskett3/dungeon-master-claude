# Dungeon Master Claude Toolkit Constitution

This constitution governs changes to the toolkit: `dmc`, the launchers, the skills, the templates and the prompts.
It adds no rule of its own. Each principle states its obligation in a line and points at the file where the rule
lives, so a rule has one home and a change to it is made there. The MUST language gives `/speckit-plan` and
`/speckit-analyze` something to check a feature against; it is the source's obligation, not a new one.

## Core Principles

### I. The story stays consistent and the player has fun
A toolkit change MUST NOT make the story or the game state less accurate, and MUST NOT make the game less
enjoyable for the player. Where the two pull against each other, neither is sacrificed.
Source: `CLAUDE.md`, the two things that matter above everything else.

### II. Play pays nothing for toolkit work
Toolkit work MUST NOT cost the player context or an install: `CLAUDE.md` is loaded into every session and stays
lean, and players install nothing but Claude Code.
Sources: `.docs/development.md` § Writing prompts; `.docs/decisions.md` § .NET 10 Native AOT on every platform;
`.docs/decisions.md` § Spec Kit installed in `.claude/skills/` and blocked in play.

### III. Each rule has one home
A rule MUST be written in the one place it belongs, and other places point to it.
Source: `.docs/development.md` § Writing prompts.

### IV. Documentation goes with every change
Every change MUST update its documentation in the same PR; a PR that leaves it stale is not finished. What
that covers is in the sources.
Sources: `CLAUDE.md` § Git and GitHub (Documentation, Pull requests); `.docs/development.md` § Documentation goes
with every change; `.docs/decisions.md` § Every change updates its documentation.

### V. Tests skip locally and fail on CI
A test whose tool is missing MUST skip locally and fail on CI, and CI MUST pass before the user is asked to merge.
Sources: `.docs/development.md` § Skip locally, fail on CI; `CLAUDE.md` § Git and GitHub (Merging).

## Build and Output Constraints

- `dmc` is compiled with Native AOT for each platform, and warnings are errors in `src/Dmc.Cli`. Source:
  `.docs/decisions.md` § .NET 10 Native AOT on every platform; `src/Dmc.Cli/Dmc.Cli.csproj`;
  `.docs/development.md` § What the build checks.
- Every `dmc` failure is one line, `dmc <command>: <message>`, and its output is kept small, because everything it
  prints lands in DMC's context. Source: `.docs/decisions.md` § One-line errors, and output kept small.

## Development Workflow

- Git and GitHub rules (branches, personal and copyrighted files, commits, pull requests, linking issues, one
  review per PR, merging) are in `CLAUDE.md` § Git and GitHub, summarised in `.docs/development.md` § Git and pull
  requests.
- Which changes go through Spec Kit, how a spec relates to its issue, and when a spec is frozen are in
  `.docs/development.md` § Spec-driven changes ("How a change goes through it").
- The toolkit tests check the prompts: commands, paths and skill names a prompt names must be real. Source:
  `.docs/development.md` § Writing prompts.

## Governance

This file points at the sources above and MUST NOT restate a rule or add one the repository does not already have.
To change a rule, change it in its home file and record the decision in `.docs/decisions.md`; change this file
only when a pointer moves, a principle's home is added or removed, or a source is renamed. An amendment goes
through a pull request under `CLAUDE.md` § Git and GitHub, and the PR description says why the constitution
changed. Its version follows Spec Kit's convention: MAJOR for a principle removed or redefined, MINOR for a
principle or section added, PATCH for clarifications. The constitution's origin is described in
`.docs/development.md` § Spec-driven changes.

**Version**: 1.0.1 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-10-23
