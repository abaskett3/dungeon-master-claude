# Implementation Plan: A condition for "fixed"

**Branch**: `claude/project-thread-bs1df4` (spec 028) | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

## Summary

There are new status words in `kit/templates/feedback.md`. `review-feedback` § 1, § 2 and § 7 move an entry from
filed to released to fixed. `dmc-record-feedback` § 2 and `review-transcript` § 6 search the released and fixed
entries and open one again when its fault comes back.

## Technical Context

**Language/Version**: Markdown (a kit template, a kit skill, two developer skills)

**Testing**: MSTest, `ToolkitTests.FixedMeansTheSlipStopped`

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. The entry's status says what play has shown |
| Core 2 Fun | Pass. A failed fix is known, so the same slip is not "fixed" again by words |
| Core 3, Development 1 Cost | Pass. `kit/dm-rules.md` 0 bytes; `dmc-record-feedback` +172 bytes and the template +125 bytes, read only when an entry is recorded; the resume's reads do not change |
| Development 2 Migration | Pass. None needed: `feedback/` is not a campaign file, and the old status is read |
| Development 3 One step | Pass. The kit files reach a DMC folder with `dmc update` |
| Development 4 Grounded | Pass. The statuses' homes are `kit/templates/feedback.md:11`, `review-feedback` § 2 and § 7, `dmc-record-feedback` § 2 and `review-transcript` § 6 |
| Development 6 One home | Pass. The condition's home is `review-feedback` § 2; the template lists the words |
| Development 7 | Pass. The test reads the files only |
| Documentation 4 | Planned: campaign-files, user-guide, decisions, roadmap |
