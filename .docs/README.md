# Product documents

These documents are for anyone who wants to understand Dungeon Master Claude as a product: players, contributors and the maintainer. DMC's own operating rules are in [kit/dm-rules.md](../kit/dm-rules.md) and the skills under `kit/skills/` (written into a player's DMC folder by `dmc`); the repository's [CLAUDE.md](../CLAUDE.md) is for toolkit work. These documents explain the product around them and point there rather than repeat them.

| Document | What it covers |
|---|---|
| [product-overview.md](product-overview.md) | What DMC is, who it is for, its two principles, what it does and what it deliberately does not do |
| [user-guide.md](user-guide.md) | For the player: getting started, everyday play, saving and breaks, rewinds, player options, modules, the rules reference, troubleshooting |
| [architecture.md](architecture.md) | The parts and how they fit (the repository, `kit/` and the DMC folder), with a diagram, and what happens when a folder is made and during a turn of play and a save |
| [dmc-reference.md](dmc-reference.md) | Every `dmc` subcommand and option, with output, exit codes and examples from real runs |
| [campaign-files.md](campaign-files.md) | The campaign folder, what each file holds, which file wins, the map format and module folders |
| [development.md](development.md) | Building, tests, CI, the git and PR process, and the conventions for writing prompts |
| [releases.md](releases.md) | How a release is cut and pinned, how `get-dmc` installs it, the platforms, and what to do when a release fails |
| [decisions.md](decisions.md) | A log of the main decisions, with dates, reasons and sources |
| [roadmap.md](roadmap.md) | What is done, what comes next and what has been set aside |

Every change to the toolkit updates the documents it affects in the same pull request, as `CLAUDE.md` § Git and GitHub requires.
