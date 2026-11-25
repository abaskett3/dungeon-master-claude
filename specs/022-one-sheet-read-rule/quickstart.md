# Quickstart: check the change

## Q1. The toolkit tests

```
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"
```

Expected: `ThePresentLineReadsASheetAsTheResumeDoes` and `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps` pass, and
every other toolkit test passes.

Result (2026-11-24, Linux, .NET SDK 10.0.115 with `global.json` set aside, since the container had no 10.0.300):
114 passed, 3 skipped (the hook tests that need a published `dmc`), 0 failed.

## Q2. Three resumes (SC-004, by the maintainer, after the release)

On the released kit, resume the long campaign three times (three play sessions). In each Claude Code log, note for each
companion sheet the `offset` and `limit` of its Read calls, and post the three rows on #247 in the issue's table form.
Expected: each companion read through its play card; a read past the card at resume is a slip against step 2.5.
