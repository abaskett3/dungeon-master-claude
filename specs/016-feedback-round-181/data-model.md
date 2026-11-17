# Data model: The feedback round of #181

No new campaign file. What changes is what existing files hold, and what `dmc` reads from them (Core 1: the files
named here hold all new state).

| Entity | File | Change | Read by |
|---|---|---|---|
| Standing line | `character-sheets/*.md`: Looks or Appearance, Usually found, Speed (or the card's AC line), Traits or Standing traits, Items used in play | Rewritten when play settles what someone can do, wears, owns or where they live (FR-001); checked at each full save against the session's History entries (FR-005) | the resume; the hook (FR-008) |
| Present line, scene | `game-state.md` Scene block (`kit/templates/campaign/game-state.md:14`) | No shape change; DMC reads a sheet before adding its person (FR-002) | the resume; the hook |
| Present line, log | `story/sessions/session-NNN.md`, each scene block | New `- Present:` line (FR-003, research R7) | the "who could know" check |
| Quoted reply | same scene blocks | A reply that matters to a relationship is quoted, never summarised (FR-007) | the resume (last log) |
| Companion | `game-state.md` Party status | Defined as travelling as one of the party's own, or directed by the player in a fight (FR-032); the full save moves a misfiled one (research R6) | the resume (play card) |
| Arc plan stage | `story/dm-notes.md` Arc plan | No shape change (#228's template); `dmc size` flags a missing `current since sN`, a missing `about N sessions`, an overrun and a dead name ahead (FR-026) | the full save |
| Antagonist's next move | `story/dm-notes.md` Antagonists | `dmc size` flags one without `by sN` (FR-026) | the full save |
| Token aura, map view | `maps/*.json` | Optional `aura` and `view` (FR-025) | `dmc map` |
| Feedback entry | DMC folder `feedback/*.md` | `status:` set to `fixed in PR #N` when the PR merges (FR-030) | `review-feedback` |

## State: a person's lines in the hook

```
not shown ──(on the Present line, a message arrives)──▶ shown at token T
shown at T ──(context passes T + 100,000, or a compaction)──▶ not shown
not shown ──(a Read of their sheet in the log at T)──▶ shown at T
```
