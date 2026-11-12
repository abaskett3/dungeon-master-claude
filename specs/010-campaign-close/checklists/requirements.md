# Specification Quality Checklist: Close a Campaign for Good

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-10
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

- The three markers (FR-003, FR-009, FR-018) were answered by the user on 2026-11-11, Q2 from an evidence-based
  answer the user asked for (spec § Clarifications). The user asked that nothing be guessed: a search of issue #180,
  `.docs/`, the code, the constitution and every earlier conversation found no decision that settled them first.
- Settled points are cited in the spec's Background: the user's own words (closed forever, sequels only; a full save
  and a backup at the end), the judgment/exact split from the migrations decision, `.standard`'s reasons for a
  `dmc`-written mark, the decision that modules are never exported, and `exports/` as the existing place outside the
  campaign. Two readings are derived and stated as assumptions: verify on a closed campaign reports and writes nothing,
  and the mark is set before the backup so the backup holds the campaign "as closed".
- Named commands and file names (`dmc campaign standard`, `canon.md`, `exports/`) are the product's own vocabulary, kept
  as in earlier specs of this repository; how the close command, the mark file and the refusals are built is left to
  the plan.
