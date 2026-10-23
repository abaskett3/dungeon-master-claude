# Specification Quality Checklist: Campaign Export and Import

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-21
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- The spec names `dmc`, `adventures/` and campaign file names because they are the product's user-facing surface (the commands and folders the player sees), not implementation choices. The checksum algorithm and command syntax are left to `/speckit-plan`. The archive format (ZIP) and the default file name are named because the player asked for them (refinement 2026-10-22), so they are requirements, not implementation leaks.
- Refinement 2026-10-22: exports are ZIP files named `<campaign>-<YYYYMMDDHHmm>.zip` in local time, with `-2`, `-3` on a clash (US1 scenarios 1 and 5, FR-001, FR-006, FR-007, Key Entities, Assumptions). Import takes the campaign name from the file's record, not its file name. Re-validated: all items still pass.
- Clarification settled 2026-10-21 (option B): the general `adventures/player-options.md` travels with the export and is written on import only when none exists (FR-004a, FR-005a, User Story 2 scenario 4).
- Constitution check: one-line errors and small output (FR-013, SC-005); documentation in the same PR (FR-015); never deletes or overwrites campaign files (FR-005, FR-008, Assumptions).
