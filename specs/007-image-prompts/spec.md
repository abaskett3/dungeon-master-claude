# Feature Specification: Image Prompts of the Story, in Saved Styles

**Feature Branch**: `007-image-prompts`

**Created**: 2026-11-10

**Status**: Draft

**Input**: Tracked by issue #178, opened when this spec was written. User description: "in the rorns landing playthrough I started asking for image prompts to send to an image
generation LLM, for funsies. Lets spec out something to formalize this process through a skill.
'/dmc-generate-image-prompt'. There are two parts to the skill must address the content of the image and the style.
The content needs to be consistent with what is in the playthrough. Often I would generate an image and it would have
Shale just as tall as the humans, kibil's sized ranged greatly, wennas pet often changed from a dog to a cat, some
folks would be half elfs for some reason, lastly sometimes spoilers would happen because of it. (hidden enemies, etc.)
We need to make sure the content is correct and there are no spoilers. As far as style goes I liked to have styles
saved so I can jump between them or use both of them to generate two different prompts. We should support saving
styles in the player options. The player can figure out what style they like by prompting the dmc and then when they
find something they like they can save. if the player provides no prompt we default to the last saved style used. If
the player doesn't have a style we should default to the high fantasy storybook style. If the player provides any
style description at all in the prompt that should be what we use and only save that style as an option if the player
asks for it to be saved."

## Background

In the maintainer's long campaign, Rorn's Landing (in the DMC folder `D:\Claude\dmc`), the player asks for image
prompts of scenes, often: a family photo at the Landing, the reunion as a wide landscape, Kibil's pose as a private of
the militia, Shale as Master Sergeant at dusk. They paste each prompt into an image-generation model, outside DMC.
Nothing in the toolkit knows about this; DMC improvised it, and the campaign's files show the result:

- **Styles live as dated notes.** The campaign's `player-options.md`, under Other requests, holds two style
  descriptions written as notes: "Image prompts, default changed to Looney Tunes style" (s19.1) and "Concept-art
  style (previous default, kept for switching back)" (s9.5). `story/dm-notes.md` repeats, in two places, that the
  player likes image prompts and which style is the default. The player switches between the two by asking for "the
  other art style", and has asked for both for one scene (s25).
- **The pictures came back wrong** where the prompt left the model to guess, and a model guesses toward its own
  defaults: Shale, a dwarf, as tall as the humans; Kibil, a silver dragon wyrmling, a different size in each picture;
  Wenna's pet changing between a dog and a cat (the files say Pepper is Wenna's cat); people drawn as half-elves who
  are not; and spoilers, such as a hidden enemy shown in a scene where the party had not seen it.

The campaign files already hold what a correct picture needs: each sheet's species, age, appearance and gear, the
minor-NPC table's one-line descriptions (Pepper: "Grey, silent, unblinking"), sizes written as comparisons ("Hound-sized
silver wyrmling"), and `game-state.md`'s location, time and who is present. They also hold what must not be in the
picture: secrets, hidden enemies and true identities in `story/dm-notes.md` and the sheets. The rule "Say only what the
listener could know" (`kit/dm-rules.md`, from the second feedback review) already covers what DMC tells the player out
of character, and an image prompt is something DMC tells the player.

This feature makes the process a player skill, `dmc-generate-image-prompt`: one place that builds the picture's content
from the campaign files, checked and free of spoilers, and dresses it in a style the player chose, with styles the
player can name, save and switch between.

## Clarifications

### Session 2026-11-10

- Q: Where are saved image styles, and the last one used, kept? → A: In the general `adventures/player-options.md`,
  shared by every campaign, so a style saved in one campaign is there in the next (FR-013).
- Q: Is a prompt in several styles at once a feature of its own? → A: No. The player can ask DMC for it in words and
  DMC will do it, with no request form of its own. (Changed by PR #185's code review: the reply gives one label and
  block per style; it had first allowed one prompt per request.)
- Q: What if the player wants a different style but does not describe one ("try a different style", "surprise me")? →
  A: DMC picks a style itself, unlike the one just used and the ones it has offered this session, and treats it as a
  style the player described: used for that prompt, not saved unless asked (FR-011, FR-014).
- Q: When a prompt needs a visual detail the files do not hold, does DMC choose one and record it? → A: No. DMC never
  chooses a fact the story has not established: "consistency rooted in fact", not assumption. The prompt leaves the
  detail out, and DMC names what is missing in a line; a detail the player then gives is a fact of the game and is
  written to the sheet (FR-009).
- Q: Is framing part of the content or the style? → A: Content. Who and what is in the picture, and how wide the shot
  is, come from the request; where the request does not say, the picture shows everyone present in a wide shot of the
  whole scene. The style holds the look (medium, palette, light, rendering) and the aspect ratio. Switching style never
  changes who is in the picture (FR-005, FR-011a).
- Q: A request varies a saved style ("the same style, but more abstract"): is the saved style changed, and is it the
  last used? → A: The prompt is written as the player asks, and the saved style is NOT changed. DMC says in a line that
  it has not changed the saved style and that the player can say so if they want the change saved into it. The saved
  style it varied is the last used (FR-011, FR-015a). Scene details in such a request (rain, night) are content, not
  style.
- Q: Does the feature migrate existing campaigns' style notes? → A: No. Image styles are not story lore or canon, so a
  change to where they live needs no migration. The feature is image prompts; a migration (and
  the `dmc` changes it would need to reach the general file) is out of scope. The styles section is optional, so an
  older general file simply has none until the first save (FR-017), and the player moves Rorn's Landing's two styles
  once by asking DMC to save them (Assumptions).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A prompt whose picture matches the story (Priority: P1)

Mid-session, the player types `/dmc-generate-image-prompt` (or asks "give me an image prompt of this"). DMC writes one
prompt for the current scene, ready to paste into an image model. Everyone in it appears as the campaign's files describe
them: their species, how big they are next to everyone else, their colours, gear and marks. Nothing appears that the
player's character has not perceived.

**Why this priority**: A prompt that produces the wrong picture is the problem the player reported. A correct, spoiler-free
picture is the whole point; the style is worth nothing without it.

**Independent Test**: In a copy of Rorn's Landing, ask for a prompt of a scene with Shale, Kibil, a human NPC and Wenna
with Pepper, while an enemy the party has not spotted is hidden nearby. Check every figure against its sheet or
minor-NPC row, and check that the hidden enemy, and every hint of it, is absent.

**Acceptance Scenarios**:

1. **Given** a scene with Shale (a dwarf) and human NPCs, **When** the player asks for a prompt, **Then** the prompt
   names Shale's species and states his height next to the humans in plain comparative terms (for example "a stocky
   dwarf who stands about chest-high to the humans beside him").
2. **Given** Kibil in the scene, **When** prompts are made in several sessions, **Then** each states the same size for him
   in the same comparative terms (the size his sheet gives, against a named point of reference), so his size does not
   drift between pictures.
3. **Given** Wenna and Pepper in the scene, **When** the player asks for a prompt, **Then** Pepper is named as a cat
   with the details of her row (grey), never only as "her pet".
4. **Given** a character whose sheet says human, **When** the prompt describes them, **Then** it says "human" and adds
   no trait of another species (no pointed ears, for example). A character is a half-elf in the prompt only if their
   sheet says half-elf.
5. **Given** an enemy hidden in the scene that the party has not detected, or an NPC whose true form is secret,
   **When** the player asks for a prompt, **Then** the hidden enemy is absent (no figure, shadow, eyes or hint) and the
   disguised NPC appears only as the party sees them.
6. **Given** the player asks for a prompt of a past moment ("the reunion at the Landing"), **When** DMC writes it,
   **Then** the content comes from that moment's session log and the files as they stood then (who was there, injuries,
   the time of day), and reveals nothing the character learned only later or has not learned.
7. **Given** the prompt would need a detail the files do not hold (Shale's hair colour, say), **When** DMC writes it,
   **Then** the prompt leaves that detail out (no colour, no guess), and DMC names the missing detail in a line after
   the block, so the player can settle it.
8. **Given** the player then gives the detail ("his hair is copper-red"), **When** DMC takes it, **Then** it is written
   to that character's sheet or row and confirmed in a line, and every later prompt uses it.

---

### User Story 2 - The style the player chose, without asking every time (Priority: P2)

The player asks for a prompt and says nothing about style. DMC uses the saved style the player used most recently. A
player who has never saved a style gets the built-in default, a high-fantasy storybook illustration.

**Why this priority**: The player asks for prompts often. Repeating the style every time is what the dated notes were
working around, and a stable default is what makes the prompts feel like one album.

**Independent Test**: With no saved styles, ask for a prompt and check it is in the storybook style. Save two styles, use
the second, then ask for a prompt naming no style, and check it is in the second style.

**Acceptance Scenarios**:

1. **Given** the player has no saved style, **When** they ask for a prompt naming no style, **Then** the prompt is in the
   built-in high-fantasy storybook style, and DMC says in a line which style it used and that styles can be saved.
2. **Given** the player has saved "Looney Tunes" and "Concept art" and last used "Concept art", **When** they ask for a
   prompt naming no style, **Then** the prompt is in "Concept art".
3. **Given** a style the player described in the request and did not save, **When** they next ask for a prompt naming no
   style, **Then** DMC uses the last *saved* style used, not the unsaved one.
4. **Given** each prompt, **When** DMC shows it, **Then** it is out of character, alone in a code block the player
   can copy whole, under a line naming the style, and nothing in the game changes: no time passes and no game state is touched.

---

### User Story 3 - Saving, switching and comparing styles (Priority: P2)

The player experiments ("make it look like an oil painting", "more muted, like a woodcut"), and when a look works they
say "save that as Woodcut". When they have no idea what they want, they ask DMC for something different and let it
choose. Later they switch by name ("use Looney Tunes").

**Why this priority**: Equal to the default: the player uses several styles and moves between them, and the saved styles
are what make the default in User Story 2 possible.

**Independent Test**: Describe a new style in a request, refine it twice, save it under a name, start a new session,
and ask for a prompt naming that style.

**Acceptance Scenarios**:

1. **Given** a request with any style description ("as a charcoal sketch"), **When** DMC writes the prompt, **Then** that
   description is the style, whatever the saved styles and the default are, and it is not saved.
2. **Given** a request that varies a saved style ("the same style, but more abstract"), **When** DMC writes the prompt,
   **Then** it uses the saved style with the change, the saved style is unchanged, and DMC says so in a line ("I haven't
   changed Concept art; say if you want this change saved into it"). If the player says yes, the saved style is
   changed and DMC confirms in a line.
3. **Given** the player has just refined a style over a few prompts, **When** they say "save that as Woodcut", **Then**
   the style as last used (the refinements included) is saved under that name, is the last style used from then on, and
   DMC confirms in a line.
4. **Given** saved styles, **When** the player asks to list, rename, change or delete one, **Then** DMC does so and
   confirms in a line; deleting the last style used makes the next default the built-in storybook style.
5. **Given** a request for a different style that describes none ("not this one, try something else", "surprise
   me"), **When** DMC writes the prompt, **Then** DMC chooses a style itself, clearly unlike the style of the last
   prompt and any style it chose earlier in the session, describes it in full in the prompt and names it in the label,
   so the player can save it with "save that"; it is not saved otherwise, and the last saved style used is unchanged.
6. **Given** a style name that matches no saved style, **When** the player asks for it, **Then** DMC says so in a line,
   lists the saved names, and writes nothing until the player picks or describes a style.

---

### Edge Cases

- **No campaign loaded**: the player asks for a prompt outside a campaign. DMC asks which campaign, or writes a prompt
  only from what the player describes, and says it is not checked against any campaign.
- **The player describes the scene's content** ("Shale riding Kibil over the river"): DMC uses that content, but still
  states every figure as the files describe them, and says in a line if the scene contradicts the story ("Kibil cannot
  carry Shale yet: he is hound-sized"), offering the closest picture that agrees.
- **The player asks for something the character has not seen** (the villain's lair, the hidden enemy): DMC declines that
  part in a line that reveals nothing more ("Shale hasn't seen that yet"), and offers the scene as the character knows it.
- **A spoiler by implication**: a hidden figure's shadow, eyes in the trees, a disguised NPC's true colours or a cursed
  item's glow. Each counts as showing what is hidden, and none appears.
- **A size the files give in game terms only, or not at all** ("Small", "Medium"): the prompt compares the size
  categories of the figures present: the same size is drawn the same (a human and an elf), a smaller one smaller (a
  gnome beside the humans), a larger one bigger (a dragon over the humans), never as a bare category.
- **Many people present** (a crowd at the Landing): the named characters are stated in full; the crowd is described as
  a crowd, consistent with the place's people (no species the setting's files do not put there).
- **Two styles saved under similar names**: DMC asks which, in a line.
- **A style that names a living artist or a studio**: DMC uses the player's words as given; the toolkit adds no style
  rules of its own beyond the built-in default.
- **The player asks for the prompt in another language or a set length**: DMC follows it for that prompt.

## Requirements *(mandatory)*

### Functional Requirements

**The skill**

- **FR-001**: The kit MUST provide a player skill `dmc-generate-image-prompt` that writes a prompt for an image-generation
  model of a moment in the campaign. DMC MUST use it whenever the player asks for an image prompt, by the slash command or
  in words.
- **FR-002**: A prompt MUST be shown out of character, alone in a fenced code block so that a copy takes the prompt and
  nothing more, under a line naming the style used (for an unsaved style, a short name of what it is, marked unsaved),
  with nothing else but the one-line notes of FR-009 and FR-010 and the one-line nudge after a variation or a first
  Storybook prompt, when they apply. A request for the same picture in several styles gets one such label and block
  per style, with the notes once, after the last block.
- **FR-003**: Writing a prompt MUST NOT change the game: no time passes, no roll is made, and no game state, canon or
  map changes. The only writes allowed are a detail the player gives under FR-009, the saved styles under FR-012 to FR-015, and
  the one line the session log already gives an out-of-character request.
- **FR-004**: The rules for image prompts MUST live in the skill. The rules loaded into every session MUST grow by no more
  than a pointer to it, so that play pays nothing for the feature until it is used.

**Content**

- **FR-005**: The scene MUST default to the current moment of play: where the party is, the time, light and weather,
  and who is present, from `game-state.md` and the latest session log. Where the request does not say who is in the picture
  or how wide the shot is, it MUST show everyone present in a wide shot of the whole scene; the player may ask for fewer
  figures, a closer shot or a different subject. A past moment the player names MUST be built from
  its session log and the files as they stood then: injuries from that scene, not from `game-state.md`, and a past look
  from the sheet's history or the `-history.md` file the full save moves older history to.
- **FR-006**: Every named figure in the prompt MUST be described from their sheet or minor-NPC row: species, sex where
  the files give it, age, build, colouring, clothing and gear, marks and injuries at that moment. The prompt MUST NOT add
  a trait the files contradict, and MUST NOT give a figure a species other than the one the files give.
- **FR-007**: Each figure's size MUST be stated as a comparison with someone or something in the scene (for example
  "chest-high to the humans", "the size of a big hound"), taken from the files, so that sizes hold within a picture and
  from one picture to the next. A creature's kind MUST be named explicitly (a cat, a dog, a wyrmling), never left to a
  word like "pet". Where the files give no comparison, the figures' size categories in the campaign's rules MUST decide
  it: figures of the same size are drawn the same size, and one of a smaller or larger size smaller or bigger than
  them. A bare category or a height MUST NOT appear in the prompt.
- **FR-008**: The prompt MUST include only what the player's character has perceived at that moment, as the rule "Say
  only what the listener could know" in the DM rules already requires of everything DMC tells the player: no hidden or
  undetected enemy, no true form of a disguised being, no secret from `story/dm-notes.md` or a sheet, no future event,
  and no hint of any of these (a shadow, eyes, a glow). A part of the request that would show one MUST be declined in a
  line that reveals nothing more.
- **FR-009**: DMC MUST NOT choose any visual detail the files do not hold: what was never established in play or by the
  player is not a fact of the game, and assuming it is not consistency. The prompt MUST leave such a detail out, and DMC
  MUST name the missing details in one line after the block. A detail the player gives in answer MUST be written to that
  figure's sheet (Appearance) or minor-NPC row and confirmed in a line, so later prompts use it.
- **FR-010**: When the player supplies the content, DMC MUST keep their content and still apply FR-006 to FR-008, and
  MUST say in a line where the requested scene contradicts the story, offering the closest version that agrees.

**Style**

- **FR-011**: The style MUST be chosen in this order: (1) any style description in the request, used as given, alone or
  as a variation of a saved style it names or the last one used ("the same style, but more abstract"), or, when the request asks for a different style without describing one, a
  style DMC chooses, unlike the style of the last prompt and any it chose earlier in the session; (2) a saved style the request names; (3) the saved style used most recently;
  (4) the built-in default, a high-fantasy storybook illustration, whose description ships with the kit.
- **FR-011a**: Content MUST decide who and what is in the picture and how wide the shot is (FR-005); the style MUST
  decide how it looks: medium, palette, light, rendering and aspect ratio. Where a style's words touch content (a saved
  style that says "a wide shot showing everyone present", or "key details from the sheets"), the content rules of
  FR-005 to FR-010 win, so a change of style never adds, drops or changes a figure. Scene details given with a style
  (rain, night) are content.
- **FR-012**: The player MUST be able to save the style of the last prompt (as given or as refined over several prompts)
  under a name, and to list, rename, change and delete saved styles, each confirmed in a line. A save or rename to a
  name another saved style has, ignoring case, MUST ask in a line whether to replace that style; a replaced style that
  was the last used gives its place to the new one. After a reply in several styles, "save that" MUST ask which.
- **FR-013**: Saved styles and which one was used last MUST be kept in the general `adventures/player-options.md`,
  shared by every campaign, in a section of their own with a fixed shape, each style a name and its description. A
  campaign's `player-options.md` holds no styles.
- **FR-014**: A style described in a request, a variation of a saved style, or a style chosen by DMC, MUST NOT be saved unless the player asks for it to be saved, and using an
  unsaved style (other than a variation, FR-015) MUST NOT change which saved style was used last.
- **FR-015**: Using a saved style (named, as the default, or varied under FR-015a) MUST record it as the last used, only
  when that changes. A reply in several styles MUST NOT change the last used.
- **FR-015a**: A variation of a saved style MUST NOT change the saved style. DMC MUST say in a line that the saved style
  is unchanged and that the player can have the change saved into it; on the player's word it is saved and confirmed.
- **FR-016**: A style name that matches no saved style MUST be answered with the saved names, and no prompt until the
  player picks or describes one.

**Template and documents**

- **FR-017**: `templates/player-options.md` (the general file's template) MUST gain the image-styles section, empty but
  for a comment that explains it and names the built-in default. The section is optional: a general file without it
  has no saved styles and no last used style, and the skill writes the section when the player first saves a style. No
  campaign file changes shape, so no migration is needed.
- **FR-018**: The player documents (the README's skill list, the user guide), `.docs/campaign-files.md` (the
  player-options rows), `.docs/architecture.md` (the skills row), `.docs/roadmap.md` and `.docs/decisions.md` MUST
  describe the skill and the styles section.

### Key Entities

- **Image prompt**: text for an image model: the content (the scene, each figure as the files describe them, sizes as
  comparisons, nothing the character has not perceived) and the style, labelled with the style's name.
- **Saved style**: a name the player chose and a description of a look (medium, palette, light, rendering, aspect
  ratio), as words to give an image model. It never decides who is in the picture or how wide the shot is.
- **Last style used**: which saved style the player used most recently; the default when a request names no style.
- **Built-in default style**: a high-fantasy storybook illustration, described in the kit, used when no saved style is
  the last used.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a test set of at least 10 prompts made from a copy of Rorn's Landing, every named figure's species,
  relative size and creature kind in the prompt agree with the files: 0 contradictions.
- **SC-002**: In the same test set, with hidden enemies and a disguised NPC in at least 3 scenes, 0 prompts name or hint
  at anything the character has not perceived.
- **SC-003**: Kibil's size is stated in the same comparative terms in every prompt that shows him, across at least 3
  sessions.
- **SC-004**: A player who saves a style gets it, unasked, in the next prompt that names no style, in a later session as
  well: no style needs repeating to stay the default.
- **SC-005**: The rules read at every session grow by no more than one line.

## Assumptions

- The player pastes prompts into an image model outside DMC; DMC generates no images itself and calls no image service.
- One prompt is written in prose that most image models accept. No model-specific syntax (weights, negative-prompt
  fields, aspect-ratio flags) is added unless the style or the request asks for it; an aspect ratio, if any, is part of
  the style's words.
- "No prompt" in the request means no style direction: a request naming only content still takes the default style.
- The built-in default is described once, in the kit, and is not written into the player's options unless they save it.
- A detail the player gives under FR-009 is an appearance fact like any other, so it belongs in the sheet, where the next
  prompt and the story both find it.
- Stating a size as a comparison where the files give only a size category (Small, Medium) or none is not a choice: it
  follows from comparing the size categories in the campaign's rules, so FR-009 does not apply to it. D&D gives most
  creatures a size category rather than a height, so the categories are the one firm measure (the user's ruling on
  PR #186's review); a figure the files make taller or shorter than its category, such as a dwarf "chest-high to the
  humans", keeps the files' comparison.
- The skill reads the campaign's files and the session log; it relies on the existing rule that the files are the truth,
  and on the knowledge-boundary rule in the DM rules for spoilers, and does not restate either.
- Rorn's Landing's two styles (Looney Tunes, the current default, and Concept art) move once, in play: the player asks
  DMC to save them from the campaign's Other requests and to drop the old notes there and in `story/dm-notes.md`. The
  skill's save does it; no other campaign has improvised styles.
- Remote play needs nothing extra: the prompt is text, sent in the reply like any other.
