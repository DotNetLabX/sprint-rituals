# Cross-Sprint QA Trends — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/QaTrendsService.cs` — Records (QaTrendsSprintInfo, QualityTrendEntry, TestingVolumeEntry, DefectCorrelationDataPoint, DefectCorrelationResult, QaTrendsResponse) and QaTrendsService with ComputeTrends, Pearson r, RAG coloring, sub-team filtering
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsQuery.cs` — GetQaTrendsRequest and validator
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaTrends/GetQaTrendsEndpoint.cs` — GET /api/analytics/qa-trends endpoint with full data loading pipeline
- `client/src/stores/qaTrendsStore.ts` — Pinia setup store with initialize, selectLastN, selectSubTeam, fetchTrends
- `client/src/components/qa/QualityTrendsChart.vue` — Multi-line ApexChart with RAG-colored discrete markers per data point
- `client/src/components/qa/TestingVolumeChart.vue` — Grouped bar ApexChart for TE count and bugs found
- `client/src/components/qa/DefectCorrelationSection.vue` — Dual-panel line charts with Pearson r badge and empty states
- `client/src/views/QaTrendsView.vue` — Page assembly with sprint range selector and sub-team filter

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Added `services.AddScoped<QaTrendsService>()`
- `client/src/types/index.ts` — Added QaTrendsSprintInfo, QualityTrendEntry, TestingVolumeEntry, DefectCorrelationDataPoint, DefectCorrelationResult, QaTrendsResponse interfaces
- `client/src/api/analytics.ts` — Added QaTrendsResponse import and getQaTrends function
- `client/src/router.ts` — Added `/qa` route before `/settings`
- `client/src/components/AppSidebar.vue` — Converted navItems to computed property with conditional QA entry gated on settingsStore.settings.xrayEnabled; added settingsStore import and fetchSettings call on mount
- `docs/kb/frontend-map.md` — Added QaTrendsView row to View -> Store -> API Mapping table

## Key Decisions

- **Endpoint: extended sprint range for N+1 correlation** — BugRatioService.ComputeMultiSprint is called with target sprints plus one extra sprint beyond the selected range so the correlation has the next-sprint bug ratio for the last QA sprint. The extra sprint's data is included in extendedTargetSprints.
- **AppSidebar fetchSettings on mount** — The sidebar needs xrayEnabled to gate the QA entry. Settings are fetched on sidebar mount so the value is populated even if the user hasn't visited the Settings page. This is low-cost (settings is a tiny payload) and avoids a race condition.
- **Pearson r denominator guard** — Returns null when denominator is zero (all coverage or bug ratio values identical), preventing division by zero per plan spec.
- **QaTrendsView toolbar** — Used inline BaseSelect components rather than PageToolbar's sprint selector since the QA page has a fixed-range selector (Last 3/5/10/All) rather than a per-sprint dropdown. PageLayout's #toolbar slot is used for placement.
- **DefectCorrelationSection visual connector** — Implemented as a dashed divider with "N+1 lag" label between the two panels. Simpler and more readable than annotation arrows; conveys the offset clearly.

## Deviations from Plan

- **Step 2 validator allows `last=0`** — The plan states "last=0 means all". The validator allows `>= 0` rather than `>= 1` to permit this. GetBugRatioRequestValidator requires `>= 1`, but the QA endpoint semantics explicitly use 0 for "all".
- **AppSidebar fetchSettings** — Plan says "import useSettingsStore and access settings.xrayEnabled". The store's settings ref is already populated if the user visited the Settings page; fetchSettings is called defensively on mount to ensure it's loaded on first navigation. This is minimal extra work (one GET on app load).
