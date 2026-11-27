# Implementation Plan: Reviewers cite a file or mark a claim unverified

**Branch**: `claude/project-thread-oncuxm` (spec 034) | **Date**: 2026-11-27 | **Spec**: [spec.md](spec.md)

## Summary

The CI review gets the rule through an appended system prompt in `claude-code-review.yml`'s `claude_args`, the one
input of `anthropics/claude-code-action@v1` that reaches the run without changing the plugin (its usage document
shows `--append-system-prompt` under `claude_args`). `CLAUDE.md` § Git and GitHub, Review, covers the built-in
command and reviewer subagents, because the sessions that run them read it. A toolkit test holds the workflow's
prompt in place.

## What can be changed, and where

| Review | Its prompt | What this change does |
|---|---|---|
| CI review (`code-review@claude-code-plugins`) | Upstream, not ours | Appends a system prompt in `claude-code-review.yml` |
| Built-in code-review command | Part of Claude Code, not ours | `CLAUDE.md` Review: check an uncited finding against the code before acting on it |
| Reviewer subagents for a PR or a plan | Written by the session | `CLAUDE.md` Review: the prompt carries Development 4 |
| `/speckit-plan`, `/speckit-analyze` | Spec Kit's | None: they already check against every principle (constitution § Governance) |

## Technical Context

**Language/Version**: YAML (a workflow), Markdown (`CLAUDE.md`, `.docs/`), C# (one MSTest)

**Testing**: MSTest, `ToolkitTests.CiReviewCitesItsClaims`; the CI review of this PR

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency, Core 2 Fun | Pass. Nothing in play changes |
| Core 3, Development 1 Cost | Pass. `kit/dm-rules.md` 0 bytes, no kit skill changes; about 420 bytes paid by the CI review only |
| Development 2 Migration | Pass. None needed: no campaign file changes |
| Development 3 One step | Pass. Install and update are unchanged |
| Development 4 Grounded | Pass. The places a review runs are `.github/workflows/claude-code-review.yml:54`, `CLAUDE.md:16` and constitution § Governance; whether the plugin's subagents get the rule is marked unverified in the spec |
| Development 6 One home | Pass. The rule's home stays the constitution; the workflow and `CLAUDE.md` point at it and say only how a review meets it |
| Development 7 | Pass. The test reads files only |
| Documentation 4 | Planned: development, decisions, roadmap |
