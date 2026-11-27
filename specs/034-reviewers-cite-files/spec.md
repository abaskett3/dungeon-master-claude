# Feature Specification: Reviewers cite a file or mark a claim unverified

**Feature Branch**: `claude/project-thread-oncuxm` (spec 034)

**Created**: 2026-11-27

**Status**: Draft

**Input**: Issue #218. Constitution Development 4 says plans and reviews rest on the code, every claim cites a file or
is marked unverified, and reviewer agents get the same rule. No reviewer prompt carries it
(`.docs/decisions.md` § "The constitution states principles of its own (version 3.0.0)", Limits).

## Background

Reviews run in three places:

1. **The CI review.** `.github/workflows/claude-code-review.yml` runs `/code-review:code-review --comment` from the
   `code-review@claude-code-plugins` plugin, with no prompt of ours. The plugin's prompt is upstream and not ours to
   change.
2. **The built-in code-review command**, run once on each finished PR (`CLAUDE.md` § Git and GitHub, Review). Its
   prompt is part of Claude Code and not ours to change either; the session that runs it checks its findings.
3. **Reviewer subagents** a session launches for a PR or a plan. Their prompt is written by that session, which reads
   `CLAUDE.md`. `/speckit-plan` and `/speckit-analyze` check a plan against the constitution already, Development 4
   included (`.specify/memory/constitution.md` § Governance).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The CI review cites what it claims (Priority: P1)

A PR opens. The CI review posts a finding that a test is missing. The finding names the file and line it rests on, or
the command that shows it; a claim the reviewer could not check says "unverified".

**Independent Test**: `ToolkitTests.CiReviewCitesItsClaims` reads the workflow's appended prompt; the CI review of
this spec's own PR runs with it (a `pull_request` run uses the PR's workflow file).

### User Story 2 - A session checks an uncited finding and briefs its reviewers (Priority: P2)

A session runs the built-in code-review command and gets a finding with no file or line. It checks the finding
against the code before acting on it. When it launches a reviewer subagent for a PR or a plan, the subagent's prompt
carries Development 4.

**Independent Test**: read `CLAUDE.md` § Git and GitHub, Review.

## Requirements *(mandatory)*

- **FR-001**: `claude-code-review.yml` MUST append a system prompt (`claude_args`, `--append-system-prompt`) that
  applies Development 4 to the review: every claim in every comment cites a file and line or a command, or says
  "unverified", nothing is assumed, and every subagent it launches gets the rule in its prompt. It MUST point at the
  constitution as the rule's home.
- **FR-002**: `CLAUDE.md` § Git and GitHub, Review, MUST have a finding that cites no file, line or command checked
  against the code before it is acted on, and every reviewer subagent launched for a PR or a plan given
  Development 4 in its prompt.
- **FR-003**: A toolkit test MUST fail when the workflow's appended prompt is gone or no longer names Development 4,
  the constitution, a path and line, "unverified" and subagents.
- **FR-004**: The documents MUST say the same: `.docs/development.md` (§ CI and § Git and pull requests),
  `.docs/decisions.md` (a new entry, and the 3.0.0 entry's Development 4 limit marked met), `.docs/roadmap.md`.

## Success Criteria *(mandatory)*

- **SC-001**: Every finding the CI review posts on this spec's PR cites a file and line or a command, or says
  "unverified". Checked by reading the review on the PR.
- **SC-002**: Every present test passes, and the new toolkit test passes.

## Assumptions

- The plugin's subagents get the rule only if the main review agent passes it on, or through the final check before
  posting, which the main agent does. Whether the plugin's subagents receive it is unverified until a CI run shows it;
  the posted comments are what SC-001 checks.
- No slip test: the change is to CI, `CLAUDE.md` for development and the documents, none of which play reads
  (`CLAUDE.md` § Git and GitHub, Slip test).
- No migration (Development 2): no campaign file changes.
- Cost (Development 1): `kit/dm-rules.md` 0 bytes, no kit skill changes; the appended prompt (about 420 bytes) is paid
  by the CI review only.

## Out of Scope

- The plugin's and the built-in command's own prompts, which are upstream.
- The `review-feedback` and `review-transcript` skills, which already cite a file, line or log record for each cause
  or entry (`.docs/development.md` § Writing prompts, "Feedback is recorded first").
