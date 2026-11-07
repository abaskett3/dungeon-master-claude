# Research: Documentation-Only Pull Requests Can Merge

## R1. Why PR #166 is blocked

- **Finding**: The ruleset on `main` (id 25800996, created 2026-10-24) has a `required_status_checks` rule for
  `dotnet (ubuntu-latest)`, `dotnet (windows-latest)` and `claude-review`. `tests.yml` has a `paths` filter on
  `pull_request` that excludes `**.md` and `**.txt`. PR #166 changes two Markdown files, so `tests.yml` never
  starts, and the two `dotnet` checks stay "expected" for ever (`mergeStateStatus: BLOCKED`, `claude-review`
  passed).
- **Source**: `gh api repos/abaskett3/dungeon-master-claude/rules/branches/main`; `gh pr view 166`; GitHub Docs,
  [Troubleshooting required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/troubleshooting-required-status-checks):
  a workflow skipped by path filtering leaves its checks pending, and a pull request that requires them is blocked.

## R2. Skip the steps, not the job

- **Decision**: The `dotnet` matrix job always runs. Its first step decides whether the change needs the tests;
  every later step has `if:` on that answer.
- **Rationale**: GitHub counts a skipped job as success for a required check, but a job-level `if` is evaluated
  before `strategy.matrix` is applied
  ([Using conditions to control job execution](https://docs.github.com/en/actions/using-jobs/using-conditions-to-control-job-execution)),
  so a skipped matrix job is reported once, under its unexpanded name, and matches neither required name. Step-level
  conditions keep the matrix, and so the two names.
- **Alternatives rejected**: An always-passing twin workflow with the same job names for the ignored paths (GitHub's
  own suggestion): two files must list the same paths inverted, and a mixed change runs both. A separate gate job
  with `needs:` plus a job-level `if`: the matrix-name problem above. A third-party paths-filter action: a new
  dependency for a shell `case`.
- **Cost**: A docs-only run starts two runners for checkout and one shell step, well under a minute each.

## R3. Which files changed

- **Decision**: Check out with `fetch-depth: 2` on a pull request and `0` on a push, then
  `git diff --name-status --no-renames <base> HEAD`. On a pull
  request `HEAD` is GitHub's merge commit (`refs/pull/N/merge`) and `<base>` is `HEAD^1`, the base branch it was
  merged onto, so the diff is what the pull request brings. On a push `<base>` is `github.event.before`. If the diff
  fails (an all-zero `before`, a missing commit), the answer is "needs the tests".
- **Rationale**: `--no-renames` lists a renamed file as a deletion and an addition, and `--name-status` gives the
  script the `D`, so a deletion or rename of any file starts the tests: `ToolkitTests` checks that the `.docs/` files
  a prompt names exist, and `MapPngTests` that the fonts' `.txt` licences do (found by the code review of the pull
  request; the old `paths` filter had the same gap). No API call and no token permission is needed.

## R4. Where the rule lives

- **Decision**: `.github/ci/needs-tests.sh` reads changed paths on standard input, one per line, and prints `true`
  or `false`. The workflow pipes the diff into it; `ToolkitTests` runs it with bash on example lists. The `paths`
  filter on `push` goes too, so push and pull request use the one rule (FR-006).
- **Rationale**: `.docs/development.md` § Writing prompts ("each rule has one home"), and the old filter's stated
  limit, "untested until a documentation-only PR shows the skip".
- **Empty list**: prints `false` (nothing changed, nothing to test). A failed diff never reaches the script (R3).
