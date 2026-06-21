# Cross-Sprint QA Trends — Summary

## Status: COMPLETE

## What Was Built
Dedicated QA sidebar page with cross-sprint trend charts: Quality Trends (coverage, pass rate, execution rate as percentage lines with RAG markers), Testing Volume (TE count + bugs found grouped bars), and Defect Correlation (N+1 lag model with Pearson r badge). Sub-team filterable, default last 10 QA-synced sprints, gated behind Xray feature flag.

## Key Outcomes
- 7 files created (QaTrendsService, GetQaTrendsEndpoint, GetQaTrendsQuery, qaTrendsStore, QualityTrendsChart, TestingVolumeChart, DefectCorrelationSection, QaTrendsView)
- 3 files modified (router.ts, AppSidebar.vue, DependencyInjection.cs)
- Build passes (0 errors backend, 0 errors TypeScript)
- Review verdict: APPROVE (cycle 0/3) — 3 LOW findings only

## Deviations from Plan
- Validator uses `last >= 0` instead of `last >= 1` (0 means "all" — documented convention)
- Sidebar fetches settings on mount to check xrayEnabled gate

## Notes
- Pearson r requires >= 6 data points, returns null otherwise
- N+1 lag model pairs coverage in sprint N with bug ratio in sprint N+1
- Help tooltips wired from help.tooltips.md
