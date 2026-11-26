# Dungeon Master Claude Toolkit Constitution

This constitution governs changes to the toolkit: `dmc`, the launchers, the skills, the templates and the prompts.
It states principles of its own. Where a rule already has a home elsewhere, a principle points at it instead of
copying it. Each principle gives its rule, its check and a one-line reason, so `/speckit-plan` and
`/speckit-analyze` have something to check a feature against.

## Core Principles

### Core 1. Consistency
Game state and story MUST stay accurate across scenes, sessions and months, and a session with no memory MUST
resume exactly from the files alone.
Check: the plan names which files hold any new state.
Reason: the files are DMC's only memory, so anything they do not hold is lost. See `kit/dm-rules.md`, the two
things that matter above everything else.

### Core 2. Fun
Rules and bookkeeping MUST serve the story, and the story serves the player. Where Core 1 and Core 2 pull
against each other, neither is sacrificed.
Check: the plan says what the player gains.
Reason: a rule that only makes play slower or duller has no reason to exist. See `kit/dm-rules.md`, the two
things that matter above everything else.

### Core 3. Low context usage and token cost
Play MUST keep its context usage and token cost low, so that, for example, a session is playable without a
top-tier subscription.
Check: the plan counts its cost as Development 1 says.
Reason: every token play reads is paid by the player, in every exchange of every game.

### Development 1. Count the cost
Every plan MUST count its token and context cost in bytes: the bytes it adds to or removes from `kit/dm-rules.md`
(paid every turn) and each skill (paid when it loads), and the tokens where `dmc size` measures them. Small
additions add up.
Check: the plan lists the bytes it adds to or removes from `kit/dm-rules.md` and each skill, and, where it changes
what a resume reads, the `dmc size` result in tokens.
Reason: what play reads is paid in every exchange of every game, and a line can grow without being added. See
`.docs/development.md` § Writing prompts.

### Development 2. No change breaks a campaign
A change MUST NOT break a player's campaign, whether it is to `dmc`'s code, the kit or the shape of campaign
files: every change that would leave a campaign behind MUST offer a migration path. A change to what campaign
files should hold (a template, or a skill's rule for a file's shape) MUST ship a migration, or say in the PR why
none is needed, as `.docs/development.md` § Changing a campaign-file standard says.
Check: the PR names its migration, or says why no campaign is left behind.
Reason: a player's campaign holds months of play.

### Development 3. One step to install, one to upgrade
Install and in-place upgrade MUST stay one step each. Players install Claude Code and `dmc`, and nothing else.
Check: the plan says whether install or `dmc update` gains a step or a dependency.
Reason: players are not developers. See `.docs/decisions.md` § Players install `dmc`, which writes and updates a
DMC folder.

### Development 4. Grounded in the code
Plans and reviews MUST rest on the code. Every claim cites a file, or is marked unverified. Nothing is assumed or
made up. Reviewer agents get the same rule.
Check: every claim in the plan or review has a path, a line or a command, or says "unverified".
Reason: a plan built on a guess breaks what it promised to fix.

### Development 6. Each rule has one home
A rule MUST be written in the one place it belongs, and other places point to it.
Check: the plan names the home of each rule it adds or changes, and copies none.
Reason: two copies drift apart. See `.docs/development.md` § Writing prompts.

### Development 7. Tests skip locally and fail on CI
A test whose tool is missing MUST skip locally and fail on CI, and CI MUST pass before the user is asked to merge.
Check: a test that needs a tool calls `TestEnv.Need` (`tests/Dmc.Tests/Support/TestEnv.cs`).
Reason: a test skipped on CI looks like a pass. See `.docs/development.md` § Skip locally, fail on CI;
`CLAUDE.md` § Git and GitHub (Merging).

### Dungeon Master 1. An arc
A homebrew campaign, one DMC creates, MUST have an arc built on at least the Five Commandments of Storytelling or
the Hero's Journey. The `dmc-new-campaign` skill MUST carry researched guidance for designing one. A module
campaign follows its module's arc as closely as play allows instead (Dungeon Master 3).
Check: `dmc-new-campaign` and the Arc plan template name the structure and its beats.
Reason: an arc gives the player a story worth finishing.

### Dungeon Master 2. Fair fights
Combat MUST challenge without dragging or overwhelming, within the lethality the player chose.
Check: a fight is sized and ended as `kit/dm-rules.md` § Sizing fights says.
Reason: fights are where a session stalls or a campaign ends by bad luck.

### Dungeon Master 3. The module is the source
While a module runs, the party MUST stay in the module's world, and the campaign follows its adventure. When the
player tries to leave, DMC brings them back with an in-world reason, never an out-of-character no. Monsters and
encounters MUST come from the module; NPCs, races, spells, skills and abilities MUST come from it first. After
the module ends, the world MUST keep the character the module and play gave it.
Check: a change to how modules are run keeps the party in the module's world while it runs, and keeps that
world's character after it ends.
Reason: the module is the adventure's setting, and its world is what the player chose.

### Dungeon Master 4. The player shapes the story
The player's actions, choices and goals MAY change the story, with real consequences, and DMC MUST weave what the
character wants into the main plot. DMC never refuses out of character. It answers "Yes, and…" to a choice that
fits the fiction, and "No, but…" in the fiction to one that would break the rules, the lore or the story, always
offering a way forward. In a module campaign, the story adapts within the module's world (Dungeon Master 3).
Check: a change to how arcs or modules are run says how a player's choice still changes the story, and how a
refusal stays in the fiction.
Reason: a story the player cannot change is not theirs, and one that bends to every choice loses its shape.

### Documentation 1. Concise
Documentation MUST be concise, simple English, with no padding.
Check: a review finds no sentence that could go without losing meaning.
Reason: every extra sentence costs a reader's time, and in a prompt it costs tokens.

### Documentation 2. Player documentation
Player documentation MUST give non-technical steps, directions and descriptions.
Check: a player can follow it without knowing a terminal or git.
Reason: players are not developers.

### Documentation 3. Developer documentation
Developer documentation MUST be simple technical English.
Check: a developer with none of the history can act on it.
Reason: the next reader has none of the session that wrote it.

### Documentation 4. Documentation goes with every change
Every change MUST update its documentation in the same PR; a PR that leaves it stale is not finished.
Check: the PR description names the documents it changed, or says why none needed to.
Reason: stale documentation misleads the next reader. See `CLAUDE.md` § Git and GitHub (Documentation, Pull
requests); `.docs/development.md` § Documentation goes with every change; `.docs/decisions.md` § Every change
updates its documentation.

## Build and Output Constraints

- `dmc` is compiled with Native AOT for each platform, and warnings are errors in `src/Dmc.Cli`. Source:
  `.docs/decisions.md` § .NET 10 Native AOT on every platform; `src/Dmc.Cli/Dmc.Cli.csproj`;
  `.docs/development.md` § What the build checks.
- Every `dmc` failure is one line, `dmc <command>: <message>`, and its output is kept small, because everything it
  prints lands in DMC's context. Source: `.docs/decisions.md` § One-line errors, and output kept small.

## Development Workflow

- Git and GitHub rules (branches, personal and copyrighted files, commits, pull requests, linking issues, one
  review per PR, merging) are in `CLAUDE.md` § Git and GitHub, summarised in `.docs/development.md` § Git and pull
  requests.
- Which changes go through Spec Kit, how a spec relates to its issue, and when a spec is frozen are in
  `.docs/development.md` § Spec-driven changes ("How a change goes through it").
- The toolkit tests check the prompts: commands, paths and skill names a prompt names must be real. Source:
  `.docs/development.md` § Writing prompts.
- A PR that changes how DMC plays states its slip-test result, or why it has none. The rule's home is `CLAUDE.md`
  § Git and GitHub ("Slip test").

## Governance

The constitution may state principles of its own. A rule with a home elsewhere is pointed at, not copied, and a
change to that rule is made in its home file. Each principle keeps to its rule, its check and a one-line reason.
An amendment goes through a pull request under `CLAUDE.md` § Git and GitHub; its PR description says why the
constitution changed, and `.docs/decisions.md` records the decision. Its version follows Spec Kit's convention:
MAJOR for a principle removed or redefined, MINOR for a principle or section added, PATCH for clarifications.
A removed principle's number is not reused, so the numbers older specs cite keep their meaning.
`/speckit-plan` and `/speckit-analyze` check each feature against every principle, and a plan that breaks one says
why. The constitution's origin is described in `.docs/development.md` § Spec-driven changes.

**Version**: 5.1.0 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-11-26
