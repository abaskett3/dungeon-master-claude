# Quickstart: check the change

## Q1. The toolkit tests

```
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"
```

Expected: `ReviewFeedbackHoldsWordOnlyFixesUntilTheSlipTest` passes, and every other toolkit test passes.

Result (2026-11-24, Linux, .NET SDK 10.0.115 with `global.json` set aside, since the container had no 10.0.300):
114 passed, 3 skipped (the hook tests that need a published `dmc`), 0 failed.

## Q2. A review (by the maintainer)

Run `review-feedback` on the DMC folder. Expected: an entry whose best fix is a new sentence is listed under "Held
until the slip test" with the Worker note written and its status unchanged; no kit file changes for it.
