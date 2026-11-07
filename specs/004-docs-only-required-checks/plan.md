# Implementation Plan: Documentation-Only Pull Requests Can Merge

**Branch**: `004-docs-only-required-checks` | **Date**: 2026-11-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-docs-only-required-checks/spec.md`

## Summary

`tests.yml` loses its `paths` filters and starts on every pull request and push to `main`. The `dotnet` matrix job
always runs, so both required checks report under their names; its first step runs a new script,
`.github/ci/needs-tests.sh`, on the changed files, and every build and test step runs only when the script says
`true`. The script is the rule's one home and the toolkit tests check it. The docs say what the required checks
are and why the job always runs. See [research.md](research.md).

## Technical Context

**Language/Version**: GitHub Actions YAML; bash (Git Bash on the Windows runner); C# for the test

**Primary Dependencies**: None added

**Storage**: N/A

**Testing**: MSTest, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, running the script with `Bootstrap.Shell("bash")`

**Target Platform**: GitHub-hosted `ubuntu-latest` and `windows-latest` runners

**Project Type**: CI for a CLI toolkit

**Performance Goals**: A docs-only run finishes in under 5 minutes (SC-001); in practice about a minute

**Constraints**: The two check names must stay exactly `dotnet (ubuntu-latest)` and `dotnet (windows-latest)`; the
ruleset is not changed (FR-008); `.sh` files are LF (`.gitattributes`) and executable in git

**Scale/Scope**: One workflow, one new script, one test, four documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|-----------|-------|--------|
| I. Story and fun | No play behaviour changes | Pass |
| II. Play pays nothing | Nothing under `kit/` or the prompts changes | Pass |
| III. One home per rule | The path rule moves from two copies in YAML to one script | Pass |
| IV. Documentation | `development.md` § CI, `decisions.md`, `roadmap.md`, the workflow and script headers | Pass |
| V. Skip locally, fail on CI | The test needs bash; `Bootstrap.Shell` skips locally without it and fails on CI | Pass |

Re-checked after design: no change.

## Project Structure

### Documentation (this feature)

```text
specs/004-docs-only-required-checks/
├── spec.md
├── research.md
├── plan.md
├── quickstart.md
├── tasks.md
└── checklists/requirements.md
```

No `data-model.md` (no data) and no `contracts/`: the script's one interface is below.

### Source Code (repository root)

```text
.github/
├── ci/needs-tests.sh          # new: the rule; paths on stdin, prints true or false
└── workflows/tests.yml        # no paths filters; a "changes" step; every later step gated on it
tests/Dmc.Tests/Toolkit/ToolkitTests.cs   # the rule's cases; the script in the LF and executable check
.docs/development.md           # § CI
.docs/decisions.md             # new entry; the 2026-10-17 entry marked amended
.docs/roadmap.md               # one row
```

### The script's interface

- **Input**: changed paths relative to the repository root, `/` separators, one per line on stdin. Blank lines are
  ignored.
- **Output**: `true` if any path is not `*.md` or `*.txt`, or is `CLAUDE.md`, `README.md`, under `.claude/skills/` or
  under `kit/`; otherwise `false` (including no paths). Exit 0.

## Complexity Tracking

None.
