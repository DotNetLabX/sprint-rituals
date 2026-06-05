# diagnose — Changelog

## [Unreleased]
- Added Required Reading, Anti-patterns, and Downstream Consumers sections (adhoc-PipelineCraftImprovements)

## [1.0.0] — 2026-05-24
- Baseline version (reconstructed from git history — last known modification: 88d2159 [Claude-Code] Improvements)
- 6-phase debugging loop: Build Feedback Loop → Reproduce → Hypothesize → Instrument → Fix → Cleanup
- Debug instrumentation tagging pattern ([DEBUG-{4-char-hex}])
- Circuit breaker integration guidance
- Phase gates enforced — cannot skip phases
