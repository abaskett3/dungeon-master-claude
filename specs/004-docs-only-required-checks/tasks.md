# Tasks: Documentation-Only Pull Requests Can Merge

**Input**: Design documents from `/specs/004-docs-only-required-checks/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [quickstart.md](quickstart.md)

**Tests**: The spec asks for them (FR-007): one data-driven toolkit test of the rule.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 docs-only merges, US2 code still tested, US3 one tested rule
- Every changed script or workflow gets its header comment updated (`CLAUDE.md` § Git and GitHub).

## Phase 1: Setup

- [X] T001 Confirm the branch is `004-docs-only-required-checks` from the tip of `origin/main`, and open one issue (#167)
  for the change that links to `specs/004-docs-only-required-checks/`.

## Phase 2: User Story 3 - The rule has one home, and it is tested (Priority: P2, but the others need it)

- [X] T002 [US3] Write `.github/ci/needs-tests.sh` to the interface in [plan.md](plan.md), with a header comment;
  mark it executable in git (`git update-index --chmod=+x`).
- [X] T003 [P] [US3] Add a data-driven test to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` that runs the script with
  bash on: a `.docs/` file (false), a `specs/` Markdown file and a `.txt` file (false), `CLAUDE.md`, `README.md`, a
  `.claude/skills/` file and a `kit/` file (true), a `.cs` file and a workflow (true), a nested `docs/README.md`
  (false), a mixed list (true), an empty list (false), a deleted `.md` and a renamed `.txt` (true). Add the script
  to `ShellScriptsHaveUnixLineEndingsAndAreExecutable`, and add a test that `tests.yml` has no `paths` filter, no
  job-level `if`, and a condition on `steps.changes.outputs.code` on every step after the decision (both added by
  the code review).

## Phase 3: User Stories 1 and 2 - Docs-only merges; code is still tested (Priority: P1)

- [X] T004 [US1] [US2] In `.github/workflows/tests.yml`: drop both `paths` filters (keep `push` on `main`); check out
  with `fetch-depth` 2 on a pull request and 0 on a push; add the step `changes` (bash) that diffs `HEAD^1` on a
  pull request or `github.event.before` on a push, with `--name-status --no-renames`, pipes the names to the script and writes `code=` to
  `GITHUB_OUTPUT`, or `code=true` if the diff fails; put `if: steps.changes.outputs.code == 'true'` on every later
  step (joined with the Linux condition); add a last step that says the tests were skipped when it is `false`;
  rewrite the header comment.

## Phase 4: Polish - Documentation

- [X] T005 [P] `.docs/development.md` § CI: the workflow starts on everything, the required checks (the ruleset),
  why the job always runs and skips its steps, the rule's home and its test; remove "no required status checks"
  and "the `paths` filter is not tested".
- [X] T006 [P] `.docs/decisions.md`: a new entry for this decision (date, decision, reason, alternatives, limits,
  source); mark "CI does not run for changes that are not code" as amended by it.
- [X] T007 [P] `.docs/roadmap.md`: update the row "CI skips changes that are not code".
- [X] T008 Build and run the tests (`dotnet test tests/Dmc.Tests -c Release`), run the quickstart's local steps,
  then open the pull request with `Closes #N` for the issue of T001 (the user's call: the PR finishes the change,
  and T009 is its check after merge) and check its description lands.
- [X] T009 After merge, update PR #166 from `main` and confirm both `dotnet` checks pass without building
  (quickstart step 4, SC-001, SC-002); if they do not, reopen #167.

## Dependencies & Execution Order

T001 → T002 → (T003, T004) → (T005, T006, T007) → T008 → T009. T003 and T004 touch different files.

## Implementation Strategy

One pull request. T009 is the only check of the docs-only path on real runners, and it can happen only after merge.
