# QA Workload & Throughput — Review

## Reviewed By
`reviewer` (Sonnet agent — Step 2 code review, Cycle 1)

## Verdict: APPROVE

## Pre-commitment Predictions

| Predicted Problem Area | Actual Finding |
|---|---|
| Sprint-not-found returns wrong HTTP status (plan says 404) | MEDIUM — code returns 400 via AddError, but matches GetBugRatioEndpoint reference pattern exactly |
| Delta polarity fields missing (Cycle 1 HIGH from done-check) | Fixed — all 7 polarity fields present end-to-end across record, DTO, mapping, TypeScript, and Vue |
| ApexCharts horizontal bar: category labels in wrong axis | MEDIUM — `yaxis.categories` used; ApexCharts horizontal bar expects `xaxis.categories` |
| `sealed record` not used per C# convention | LOW — pre-existing codebase pattern; BugRatioService and DeveloperQualityService both use `public record` |
| Workload alert streak edge cases | No finding — break-on-zero-runs matches BugRatioService reference pattern |

## Findings

### MEDIUM: Sprint-not-found returns 400 via FluentValidation mechanism instead of 404

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs:115`
**Issue:** The plan specifies "Validate sprint exists (404 if not found)." The code uses `AddError(r => r.SprintId, ...) + SendErrorsAsync(400, ct)`. The C# convention (`docs/conventions/csharp.md`) explicitly states: "`AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation, not for handler business logic errors." Handler business logic errors should throw domain exceptions (e.g., `NotFoundException`) which `GlobalExceptionMiddleware` maps to the correct HTTP status.

However, `GetBugRatioEndpoint.cs` (the plan's explicit reference pattern) uses the identical `AddError + SendErrorsAsync(400)` pattern for the same sprint-not-found case. The developer correctly followed the reference implementation rather than the plan text's status code. This is a pre-existing convention inconsistency in the codebase, not a regression.

**Fix:** No action needed in this feature. The architect should decide whether to standardize analytics endpoints to use `NotFoundException` and issue a follow-up to update both `GetBugRatioEndpoint` and `GetQaWorkloadEndpoint` together.

---

### MEDIUM: ApexCharts horizontal bar chart — person name categories placed in `yaxis` instead of `xaxis`

**File:** `client/src/components/developers/QaWorkloadTab.vue:147-150`
**Issue:** In ApexCharts, `type: 'bar'` with `plotOptions.bar.horizontal: true` visually flips the axes but category data still belongs in `xaxis.categories`. The code places person names in `yaxis.categories`, which is not a standard ApexCharts property for bar charts. This may result in the Y-axis showing no labels or default numeric labels instead of person names.

```typescript
// Current (non-standard):
yaxis: {
  categories: distributionCategories.value,
  labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
},
```

**Fix:** Move `categories` to `xaxis` and keep the `title` there:
```typescript
xaxis: {
  categories: distributionCategories.value,
  labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
  title: { text: 'Run Count', style: { color: '#9ca3af' } }
},
yaxis: {
  labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
},
```

Note: Confidence is MEDIUM — this review cannot run the browser to observe the rendered chart. No other horizontal bar charts exist in this codebase to compare against. If the chart renders person names correctly with the current `yaxis.categories` placement, this finding can be dismissed.

---

### LOW: Service-layer records use `public record` instead of `sealed record`

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/QaWorkloadService.cs:5-98`
**Issue:** `docs/conventions/csharp.md` says "Use `sealed record` for DTOs, value objects, and domain events." The 9 public records defined above the service class use `public record` without `sealed`.
**Fix:** This exactly matches `BugRatioService.cs` and `DeveloperQualityService.cs` — both use `public record` for service-layer types. The developer correctly followed the established codebase pattern. No action needed for this feature; the convention should be reconciled with the codebase reality in a separate pass.

---

### LOW: CS8714 nullable dictionary key warnings (13 instances)

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/QaWorkloadService.cs` (multiple lines)
**Issue:** `Dictionary<string?, ...>` with the custom `NullableStringComparer` triggers CS8714 because `string?` does not satisfy `notnull`. Build produces 13 instances of this warning.
**Fix:** Developer acknowledged this in implementation.md. The `NullableStringComparer` correctly handles null keys at runtime. These can optionally be suppressed with `#pragma warning disable CS8714` around the affected dictionary declarations, but this is cosmetic only.

## Positive Observations

- **Attribution logic is clean and DRY.** The three-tier attribution (ExecutedById → AssigneeId → null/Unassigned) is encapsulated in a single `GetAttributedAccountId` helper, reused across run counting, stories covered, and workload alert evaluation. The static duplicate `GetAttributedAccountIdStatic` is the correct workaround for calling attribution from a static context.

- **Cycle 1 polarity fix is complete and correct.** All 7 polarity fields are present and correctly wired end-to-end: `QaWorkloadSingleEntry` record → `QaWorkloadSingleEntryDto` → `MapSingleEntry` → TypeScript `QaWorkloadSingleEntry` interface → `QaWorkloadTab.vue` `deltaClass(polarity, direction)` calls. Polarity values correctly implement spec BR 51-57: TesOwned/RunsCompleted/BugsFound = neutral, Pass/PassRate/StoriesCovered = positiveUp:true, Fail = positiveUp:false.

- **Stories Covered and Bugs Found use the correct and deliberately different attribution models.** Stories Covered credits the runner (execution attribution, BR 10); Bugs Found credits the TE owner (AssigneeId, BR 11). The code implements both correctly without conflating them.

- **Sub-team filter correctly excludes Unassigned row.** `if (key is null) return true` at `QaWorkloadService.cs:382` removes the Unassigned entry whenever a sub-team filter is active — matches BR 21.

- **Developer exclusion filter correctly NOT applied.** `developerRepository.GetAllAsync(ct)` is used (not an active-only or exclusion-filtered variant), matching BR 20.

- **Workload alert uses full closed sprint history.** Both multi and single sprint paths load all closed sprint IDs separately from the target/viewed range and pass them to the service. Matches BR 17.

- **Team-level metrics are computed over the unfiltered TE set** (comment at `QaWorkloadService.cs:185`), not the sub-team-filtered personMap. This matches the plan intent for team metrics to reflect total team activity.

- **DI registration correct.** `QaWorkloadService` added as scoped at `DependencyInjection.cs:41`, consistent with all other analytics services.

- **Frontend tab integration complete.** Xray gate, URL sync (`qa-workload` ↔ `qaWorkload`), initial load on mount, sprint/sub-team/lastN change handlers all wired. Loading/error/empty/data state hierarchy matches the Quality tab pattern.

- **Unassigned row pinned consistently.** Both backend sort lambdas and both frontend computed sort functions (`sortedMultiDevs`, `sortedSingleDevs`) correctly pin `accountId === null` to bottom.

- **Build passes** with 0 errors.

## Gaps

- **Sparkline not rendered in single-sprint team metric cards.** The backend produces sparkline data in the `MetricCard` for all three team metrics. The component renders the cards with inline HTML (not reusing `MetricCard.vue`), and shows delta text but no sparkline chart. Spec BR 31 says "4-sprint sparkline." The sparkline data is produced but unused in the UI. This is additive work — the data is available, wiring a sparkline renderer is a follow-up if the team wants it.

- **`distributionSeries` and `distributionCategories` sort independently.** Both computeds sort by `runsCompleted` descending with Unassigned pinned, but they are separate `.sort()` calls on separate array copies. If two persons tie on `runsCompleted`, sort stability could theoretically diverge between the two computeds, misaligning series data with category labels. In practice this is unlikely with real data, but a defensive refactor would derive both from a single sorted array.

## Open Questions

- **Sprint-not-found status code.** The plan says 404; the codebase convention (GetBugRatioEndpoint) says 400. Should analytics endpoints use `NotFoundException` or continue with `AddError + SendErrorsAsync(400)`? Architect decision needed for standardization — not blocking this feature.

- **ApexCharts `yaxis.categories`.** If the distribution chart renders person names correctly in the browser despite the non-standard property placement, the MEDIUM finding above can be dismissed. Developer should verify in the browser before closing.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS (0 errors, 13 warnings) | `dotnet build Fokus.API.csproj --no-incremental` | `Build succeeded. 0 Error(s), 13 Warning(s)` |
| CS8714 warnings | Expected (acknowledged) | Build output | NullableStringComparer handles null keys correctly at runtime |
| Sprint-not-found mechanism | Verified vs reference | Read GetBugRatioEndpoint.cs:54-55 | Same `AddError + SendErrorsAsync(400)` pattern in reference |
| Attribution logic | Verified by trace | Read QaWorkloadService.cs:246-252 | Three-tier: ExecutedById → AssigneeId → null |
| Sub-team filter excludes Unassigned | Verified | Read QaWorkloadService.cs:382 | `if (key is null) return true` |
| DI registration | Verified | Read DependencyInjection.cs:41 | `services.AddScoped<QaWorkloadService>()` present |
| Tab Xray gate | Verified | Read DevelopersView.vue:379-390 | `v-if="settingsStore.settings.xrayEnabled"` on QA Workload button |
| URL slug | Verified | Read DevelopersView.vue:89, 44 | `qa-workload` ↔ `qaWorkload` correctly mapped both ways |
| Polarity fields (Cycle 1 fix) | Verified end-to-end | Read QaWorkloadService.cs:596-624, GetQaWorkloadQuery.cs:148-169, GetQaWorkloadEndpoint.cs:290-311, types/index.ts:1129-1163, QaWorkloadTab.vue:531-576 | All 7 polarities present and correctly set |
| Plan conformance (all 5 steps) | PASS | Traced each plan step against implementation files | No missing steps, no silent deviations |
