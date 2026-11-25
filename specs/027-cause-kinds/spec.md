# Feature Specification: Feedback names the kind of its cause, and only one kind ends in words

**Feature Branch**: `claude/project-thread-bs1df4` (spec 027)

**Created**: 2026-11-25

**Status**: Draft

**Input**: Issue #266 (Slips 4.1, part of #243, plan #238): `review-feedback` names one of four kinds of cause for
each entry, with the file, line or log record that shows it; only the first kind ends in changed words. This makes
the hold of #248 (spec 023) a permanent rule of the skill.

## Background

`review-feedback` § 5 was "Find the cause in the prompts", so each cause was a prompt and each fix was words. Entries
marked fixed by words came back (#243). Spec 023 held word-only fixes until the slip test (#249) was done. That came
true when PR #275 merged, so the hold has ended.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A slip the model made with the right line in context gets no new sentence (Priority: P1)

The player reports that an NPC used a wrong name. The review reads the log and finds that the sheet's name line was
read two play turns before. It records kind 4, adds no sentence, and links the entry to the issue for a mechanism
(#253).

**Independent Test**: the toolkit test reads the skill. It checks for the four kinds, an example for each, the rule
that only kind 1 ends in words, and that the hold's end condition is gone.

### User Story 2 - A wrong instruction is fixed by replacing it (Priority: P1)

Two lines of the rules contradict each other after a correction. The review records kind 1 and proposes a
replacement that is no longer than the text it replaces.

## Requirements *(mandatory)*

- **FR-001**: § 5 MUST name one of four kinds for each entry, with the file and line or the log record that shows it:
  (1) the instructions say the wrong thing or contradict each other, (2) a mechanism is missing or cannot do its work,
  (3) the story files are wrong, (4) the model does not do it reliably.
- **FR-002**: Only kind 1 MAY end in changed words. Its fix removes or replaces text. A fix that makes a file longer
  (a new template field included) is proposed as a slip-test trial and changes the kit only if it passes.
- **FR-003**: Kind 2 MUST become an issue for a mechanism in the form of a trial (#241). Kind 3 MUST become a
  correction of the data with the player's ruling, and a note on #262 when a step wrote the line. Kind 4 MUST be
  recorded as "the model does not do this reliably", get no sentence, and count as evidence for a mechanism.
- **FR-004**: The hold of spec 023 MUST be replaced. An entry noted `held until the slip test` is reviewed again
  under the new rule.
- **FR-005**: The skill MUST give a worked example for each kind.
- **FR-006**: The documents MUST say the same: `.docs/development.md` (§ Writing prompts, and § Spec-driven changes
  where it describes feedback tuning), `.docs/decisions.md` and `.docs/roadmap.md`.

## Success Criteria *(mandatory)*

- **SC-001**: The next feedback review names a kind for every cause it reports, and proposes no new sentence for a
  cause of kind 2, 3 or 4.
- **SC-002**: Every present test passes, and the new toolkit test replaces spec 023's.

## Assumptions

- No new entry status. Kind 4 is `filed #N` for the mechanism's issue. Kind 3 is `filed #262` when a step wrote the line,
  `filed #N` for a mechanism when DMC wrote it with no step at fault, and `not a toolkit problem: the story file was
  wrong, corrected with the player's ruling` only when someone other than DMC wrote it. So the feedback template does not change and
  no migration is needed (Development 2).
- Cost (Development 1): 0 bytes to `kit/dm-rules.md` and to every player skill. `review-feedback` is a developer
  skill: +1,365 bytes, read only when it runs.

## Out of Scope

- A condition for "fixed" (#267) and a slip-test result in a PR that changes play (#268).
