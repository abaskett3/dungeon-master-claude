# Quickstart: checking the change

1. Locally, the rule: `printf 'M\t.docs/roadmap.md\n' | bash .github/ci/needs-tests.sh` prints `false`;
   `printf 'M\tkit/dm-rules.md\n' | bash .github/ci/needs-tests.sh` and
   `printf 'D\t.docs/roadmap.md\n' | bash .github/ci/needs-tests.sh` print `true`.
2. Locally, the tests: `dotnet test tests/Dmc.Tests -c Release --filter "FullyQualifiedName~ToolkitTests"`.
3. On this feature's own pull request (it changes a workflow, so it is code): both `dotnet` checks run every step.
4. On a documentation-only pull request after merging (PR #166, updated from `main`): both `dotnet` checks pass with
   only the checkout and "Decide" steps run, and the merge box is not blocked (SC-001, SC-002).
