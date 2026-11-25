# Specification Quality Checklist: Real tokens in `dmc size`

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-24
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

- This is a developer tool spec: it names `dmc` commands, `Budgets.cs` members and test files because they are the
  subject of the change (constitution Development 4 asks for file citations). No language, library or algorithm is
  chosen; how text tokens are estimated and calibrated is left to the plan.
- SC-002 compares the total with the share of the resume's growth that came from reading the files, not the whole
  growth, because the whole growth also holds skill text, command outputs and replies, and resumes read different
  lines (about 25k apart). The remaining growth must be measured and stated.
