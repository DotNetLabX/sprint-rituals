# Pipeline Craft Improvements — Summary

## Status: COMPLETE

## What Was Built
Comprehensive hardening of the agent pipeline: extracted convention files from agents-workflow.md, added governance sections (message size contract, checkpoint report format), overhauled KB maintenance rules, hardened all 4 agent files (developer, reviewer, architect, team-lead) with anti-patterns mined from real lessons, idempotency guards, conformance matrices, and structured state tracking. Enhanced 7 skills with required reading, anti-patterns, I/O contracts, downstream consumers, and changelogs. Created a known-deviations catalog seeded with 6 recurring patterns from F28-F32.

## Key Outcomes
- 5 new convention files, 5 new skill changelogs, 1 known-deviations catalog created
- 4 agent files hardened, 5 rule files updated, 7 skills enhanced
- Review verdict: APPROVED (3 LOW findings, all resolved)
- Review cycles: 1/3 (LOW fixes only)

## Deviations from Plan
- agents-workflow.md at 367 lines vs ≤350 target — valid deviation due to mandatory Step 3 governance additions

## Notes
- Documentation-only changes — no source code or build impact
- All anti-patterns and known deviations are seeded from real lessons (F28-F32, adhoc-SuiteScaffolding), not hypothetical
