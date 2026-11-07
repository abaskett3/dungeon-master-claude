# Feature Specification: Documentation-Only Pull Requests Can Merge

**Feature Branch**: `004-docs-only-required-checks`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "PR is still stuck" (PR #166, which changes only `.docs/roadmap.md` and
`specs/003-separate-player-install/tasks.md`). The user chose option 3 of three: change the tests workflow so the
required checks always report, instead of bypassing the rules for one PR or dropping the checks from the rules.

## Background

Since 2026-10-24 a repository ruleset on `main` requires three checks to pass before a pull request can merge:
`dotnet (ubuntu-latest)`, `dotnet (windows-latest)` and `claude-review`. Since 2026-10-17 the tests workflow does
not start at all for a change that touches only Markdown and text files (other than the prompts the tests read).
Together they block every documentation-only pull request for good: the two `dotnet` checks never report, and
GitHub waits for them. `.docs/development.md` still says the repository has no required checks.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A documentation-only pull request can merge (Priority: P1)

The maintainer opens a pull request that changes only documentation (the roadmap, a spec's task list, a decision
entry). The required checks report within a few minutes, the pull request shows as ready to merge, and no build or
test run is spent on it.

**Why this priority**: This is the bug. Every documentation-only pull request is blocked until it is fixed, and
the rules say every change must update its documentation, so these pull requests are common.

**Independent Test**: Open a pull request that changes only a `.docs/` file. Both `dotnet` checks report success
without building or testing, and the pull request can be merged without a bypass.

**Acceptance Scenarios**:

1. **Given** a pull request that changes only `*.md` or `*.txt` files outside the prompts, **When** its checks
   finish, **Then** `dotnet (ubuntu-latest)` and `dotnet (windows-latest)` have both reported success, and neither
   built or tested anything.
2. **Given** the same pull request, **When** the maintainer looks at its merge box, **Then** it is not blocked by
   a missing check.

---

### User Story 2 - A change to code or to the prompts is still tested (Priority: P1)

A pull request that changes code, a workflow, a project file, or one of the prompts the tests read (`CLAUDE.md`,
`README.md`, `.claude/skills/`, `kit/`) builds and runs the full test suite on Linux and Windows, as today, and
cannot merge until it passes.

**Why this priority**: The fix must not open a hole: the required checks exist so untested code cannot merge.

**Independent Test**: Open a pull request that changes one prompt or one source file. Both `dotnet` checks build
and run the tests.

**Acceptance Scenarios**:

1. **Given** a pull request that changes any file that is not Markdown or text, **When** the checks run, **Then**
   both `dotnet` checks build and run all the tests.
2. **Given** a pull request that changes only `CLAUDE.md`, `README.md`, a file under `.claude/skills/` or a file
   under `kit/`, **When** the checks run, **Then** both `dotnet` checks build and run all the tests.
3. **Given** a pull request that mixes documentation with one code file, **When** the checks run, **Then** the
   tests run.

---

### User Story 3 - The rule for "code" has one home, and it is tested (Priority: P2)

A developer who wants to know, or change, which files start the tests finds the rule in one place, and the toolkit
tests check it against example file lists.

**Why this priority**: The old rule was written out twice (for push and for pull requests) and was never tested
("check it on the next documentation-only PR"); this pull request is that check, and it failed.

**Independent Test**: The toolkit tests feed the rule lists of changed files and check its answer for each case
in User Stories 1 and 2.

**Acceptance Scenarios**:

1. **Given** the toolkit tests, **When** they run, **Then** they check the rule's answer for documentation-only,
   prompt, code and mixed file lists, and for an empty list.
2. **Given** the development and decision documents, **When** a reader looks up when CI runs, **Then** they find
   the required checks, the rule and why the checks always report.

---

### Edge Cases

- A pull request whose list of changed files cannot be worked out (for example, a history too shallow to compare)
  is treated as code, so the tests run: failing safe costs runner time, never an untested merge.
- A push to `main` follows the same rule as a pull request, so the push and pull request rules cannot drift apart.
- A push to `main` with no earlier commit to compare against (a new branch, a forced push) is treated as code.
- A pull request that deletes or renames any file, even a Markdown or text one, starts the tests: some tests check
  that Markdown and text files exist (the `.docs/` files a prompt names, the fonts' licences). Added by the code
  review of the pull request.
- A file named like a prompt in a subfolder (`docs/README.md`) is documentation, as today; only the root
  `CLAUDE.md` and `README.md` are prompts.
- `claude-review` is a required check from a different workflow and is not changed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tests workflow MUST start on every pull request and every push to `main`, whatever files it
  changes.
- **FR-002**: On every run, both required checks, `dotnet (ubuntu-latest)` and `dotnet (windows-latest)`, MUST
  report a result under exactly those names.
- **FR-003**: When every changed file is a Markdown (`*.md`) or text (`*.txt`) file, none is deleted, and none is
  the root `CLAUDE.md`, the root `README.md`, or under `.claude/skills/` or `kit/`, the checks MUST succeed without
  building or testing.
- **FR-004**: Otherwise the checks MUST build and run the full test suite, as before this change.
- **FR-005**: When the changed files cannot be determined, the checks MUST build and run the tests.
- **FR-006**: The rule in FR-003 MUST be written once and used for both pull requests and pushes.
- **FR-007**: The toolkit tests MUST check the rule against documentation-only, prompt-only, code, mixed, deleted
  and empty file lists, and MUST check that the workflow has no path filter and guards every step after the
  decision.
- **FR-008**: The repository's rules and the other workflows MUST NOT change.
- **FR-009**: `.docs/development.md` § CI and the decision log MUST describe the required checks, the rule and the
  reason; the earlier decision "CI does not run for changes that are not code" MUST be marked as amended; the
  roadmap MUST record the change.

### Key Entities

- **Required check**: A check name the repository's ruleset on `main` waits for before a pull request can merge.
- **Code change**: A set of changed files that, by the rule of FR-003, needs the tests.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A documentation-only pull request shows both `dotnet` checks as passed within 5 minutes of being
  opened, and can merge without a bypass.
- **SC-002**: On a documentation-only pull request, no build or test step runs.
- **SC-003**: On a pull request that changes code or a prompt, the full test suite runs on both systems, as before.
- **SC-004**: The rule is defined in exactly one file, and the toolkit tests check at least five cases of it.

## Assumptions

- The ruleset stays as it is (the user chose not to change it); the fix is in the workflow only.
- A required check that GitHub sees as skipped counts as passed, but a job skipped as a whole reports before its
  matrix is applied, so its name would not match the required ones. The job therefore always runs and skips its
  own steps.
- A runner that starts and skips its steps costs well under a minute, which the user accepts in place of a full
  build.
- PR #166 is unblocked once this merges and #166 is updated from `main`, or by the user's bypass in the meantime.
