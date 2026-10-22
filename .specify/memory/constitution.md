# Dungeon Master Claude Toolkit Constitution

This constitution governs changes to the toolkit: `dmc`, the launchers, the skills, the templates and the prompts.
It names no rule of its own. Each principle points at the file where the rule lives, so a rule has one home and
a change to it is made there.

## Core Principles

### I. The story stays consistent and the player has fun
A toolkit change MUST NOT make the story or the game state less accurate, and MUST NOT make the game less
enjoyable for the player. Where the two pull against each other, neither is sacrificed.
Source: `CLAUDE.md`, the two things that matter above everything else.

### II. Play pays nothing for toolkit work
`CLAUDE.md` is loaded into every session and every line in it is paid for in every exchange, so it stays lean:
procedures go in skills, and rarely needed reference goes in a file a skill points to. Toolkit-only tooling adds
no description, hook or instruction to a play session. Players install nothing but Claude Code.
Sources: `.docs/development.md` § Writing prompts; `.docs/decisions.md` § .NET 10 Native AOT on every platform;
`.docs/decisions.md` § Spec Kit installed in `.claude/skills/` and blocked in play.

### III. Each rule has one home
A rule is written in the one place it belongs and other places point to it. Preferences that belong to one
player are player options, not toolkit rules. Write for a reader with none of the history.
Source: `.docs/development.md` § Writing prompts.

### IV. Documentation goes with every change
Every change updates its documentation in the same PR, and a PR that leaves it stale is not finished: an XML doc
comment on every type and member added or changed, a header comment on every script and workflow, the `.docs/`
documents that describe the change (including `roadmap.md` and `decisions.md`), and a PR description that says
which documents changed or why none needed to.
Sources: `CLAUDE.md` § Git and GitHub (Documentation, Pull requests); `.docs/development.md` § Documentation goes
with every change; `.docs/decisions.md` § Every change updates its documentation.

### V. Tests skip locally and fail on CI
A test whose tool is missing is skipped on a developer's machine and fails when the `CI` environment variable is
set, so nothing is skipped on CI. CI MUST pass before the user is asked to merge.
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

**Version**: 1.0.0 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-10-22
