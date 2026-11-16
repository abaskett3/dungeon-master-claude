# Contract: the jump offer in `kit/dm-rules.md` § Narration

Meets FR-007, FR-008; research R8.

## Where

The bullet at line 195 of `kit/dm-rules.md` (§ Narration, "End each beat on the next decision that matters…"). The
new sentence goes right after "…and the scene does not skip ahead until the player closes it (a running clock or
threat still acts)." and before "A beat with no decision in it…". No line is added.

## The sentence

```text
 When a scene's point has landed and nothing is half done (the break test, § Compaction and long sessions), offer the jump in one line ("Skip to the morning?"), stopping at any clock due inside it; if they decline, offer again only once the next scene's point lands.
```

(The leading space joins it to the sentence before.)

## Budget

- 267 bytes (UTF-8), about 67 tokens per turn by bytes / 4. FR-008 allows at most 300.
- `kit/dm-rules.md` stays at 212 lines. `DmRulesBytesAllowed` in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` rises from
  30,631 to the file's size after the change (30,898 if nothing else changes the file first), and its comment names
  spec 015 FR-008.

## What it must and must not do

| Case | Result |
|---|---|
| A scene's point has landed; no roll asked for, no NPC waiting on the character's reply, no choice open beyond what to do next | one line offering the jump, naming where to |
| An exchange the character is still in | no offer (the open-exchange rule before it) |
| Mid-scene, a fight, or an initiative order | no offer |
| The player declines | play goes on; no new offer until the next scene's point lands |
| A clock comes due inside the jump | the jump stops there and the clock acts |
| The offer itself | a question, never a menu of options; in or out of character as the moment suits |

A toolkit test checks the sentence is in § Narration (it names "Skip to the morning?" and "the break test").
