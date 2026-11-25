# Implementation Plan: Feedback names the kind of its cause, and only one kind ends in words

**Branch**: `claude/project-thread-bs1df4` (spec 027) | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

## Summary

`review-feedback` § 5 gains the four kinds with an example each. § 6's hold paragraph is replaced by the rule that
only kind 1 ends in words. § 7's held entries are reviewed again, and the Worker notes record the kind and its
status.

## Technical Context

**Language/Version**: Markdown (a developer skill)

**Testing**: MSTest, `ToolkitTests.ReviewFeedbackNamesTheKindOfEachCause`, which replaces
`ReviewFeedbackHoldsWordOnlyFixesUntilTheSlipTest`

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. A wrong story file is corrected with the player's ruling (kind 3) |
| Core 2 Fun | Pass. Fixes aim at the cause, so fewer slips come back |
| Core 3, Development 1 Cost | Pass. 0 bytes to `kit/dm-rules.md` and every player skill; `review-feedback` +1,365 bytes, developer-only |
| Development 2 Migration | Pass. None needed: no template or campaign file changes |
| Development 3 One step | Pass. A developer skill, not released |
| Development 4 Grounded | Pass. Every kind needs the file, line or log record that shows it |
| Development 6 One home | Pass. The kinds' home is `review-feedback` § 5; `development.md` summarises and points to it |
| Development 7 | Pass. The test reads the skill only |
| Documentation 4 | Planned: development.md, decisions.md, roadmap.md |
