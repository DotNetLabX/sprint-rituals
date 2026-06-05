# tdd — Changelog

## [Unreleased]
- Added Required Reading and Anti-patterns sections (adhoc-PipelineCraftImprovements)

## [1.0.0] — 2026-05-24
- Baseline version (reconstructed from git history — last known modification: 88d2159 [Claude-Code] Improvements)
- Red-green-refactor loop with one-vertical-slice-at-a-time constraint
- Step 0 bootstrap: xUnit + FluentAssertions + NSubstitute test project scaffold
- Test organization pattern: Domain/, Features/{Area}/, _Fixtures/
- Mocking rules: mock only at system boundaries
- Integration over unit guidance for endpoints (WebApplicationFactory)
- Anti-pattern: horizontal slicing documented
