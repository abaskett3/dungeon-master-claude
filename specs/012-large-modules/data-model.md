# Data Model: Large Modules Cost One Area, Not One Chapter

Phase 1 of [plan.md](plan.md). Every entity is a markdown file or a value computed from one in memory; nothing is
stored (spec FR-009).

## Module

`modules/<name>/`. Its checked files are `SourceFiles.Module(folder)`, the files lookup searches: `index.md`, every
other top-level `*.md`, and `chapters/**/*.md`; never `source/` or a folder starting with a dot.

## Section (for the check)

| Field | Source |
|---|---|
| File | the module file, relative path with `/` |
| Heading | `MarkdownFile.Headings[i]`: level, text, line |
| Parent | the nearest earlier heading of a lower level, or none |
| Own tokens | bytes of its lines (heading to the next heading of any level, line ends included) ÷ 4, as `dmc size` counts |
| Stat blocks | armor-class lines each followed within eight lines by a hit-points line, outside fences (research R4) |

## Module problem

One line each, in this order: files, then sections in file order, then repeats.

| Kind | Raised when | Line |
|---|---|---|
| File at the mark | file tokens ≥ `Budgets.NearCapTokens` or lines ≥ `Budgets.NearCapLines` | `modules/<m>/<file> <t> tokens, <n> lines: at the Read cap mark (<mark>): split it by heading into smaller files beside it` |
| Section over the limit | own tokens > `Budgets.ModuleSection` | `modules/<m>/<file>:<line> "<heading>" <t> tokens, over the <limit> section limit: give each area or entry its own heading` |
| Several stat blocks | stat blocks ≥ 2 | `modules/<m>/<file>:<line> "<heading>" holds <k> stat blocks: give each its own heading` |
| Repeated heading | same key, same level, same parent, second and later | `modules/<m>/<file>:<line> "<heading>" repeats line <first>: make each heading unique` |

A section that is both over the limit and holds several stat blocks gets the stat-block line only (the fix covers
both).

## Pointer

`module-progress.md`, `- **Digest to read before continuing**:` (research R7).

| Form | Meaning |
|---|---|
| `chapters/NN-title.md § H1; § H2` | read each heading's section (new) |
| `chapters/NN-title.md, section H` or `chapters/NN-title.md H` | read section H (old, unchanged) |
| `chapters/NN-title.md` | read the whole chapter (unchanged) |
| `none`, or no file named | read no digest (unchanged) |

A heading is resolved by `Lookup.FindSection` on that one file. One that matches nothing: the whole chapter is
measured and a flag line names it with the nearest headings.

## New budget

`Budgets.ModuleSection = 3000` (research R2), with an XML comment giving its reason.
