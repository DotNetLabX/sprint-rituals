# Skill-gap registry

Produced by `mine-skill-gaps` over this repo's delivery estate (`docs/specs/**/delivery/`,
including the `fokus/` and `reflekt/` sub-estates). v1 sources: plan.md + lessons.md only.
Discovery only — building/fixing skills is owner-routed to `improve-skills`; diagnosis of
existing skills is `evaluate-skill`.

**Estate swept (2026-07-18):** 77 plans, 74 lessons files (56 with a `## Skill Gaps` section);
~255 no-skill plan rows collected; 19 Tier-B clusters formed (the collector's stats line said 18 —
the skeptic counted 19 headings and ruled on all), 14 confirmed / 4 trimmed / 1 killed by a
fresh-context skeptic; 19/19 Tier-A citations resolved, 0 mismatches. Five slug folders carry no
v1 sources and contributed nothing: `adhoc-CoverageApparatus`, `adhoc-CoverHealthScore`,
`adhoc-ReflektEventCorrectness`, `adhoc-StrykerSmokeTest`, `BUG-1-masstransit-license-boot`.

Ranking: plan count first, row count as tiebreak. Tier-A candidates carry their native evidence
count (no 3-plan bar); Tier-B clusters required >= 3 distinct plans after skeptic trim.

## Candidates (open)

| name | kind | recurrence | citations | repo | skeptic excerpt | last_verified | status |
|------|------|-----------|-----------|------|-----------------|---------------|--------|
| documentation-kb-update | gap | 22 rows / 20 plans | plan rows across 20 plans (e.g. Pass3b s6, F22 s10, Pass4 s12) | sprint-rituals | "Update KB entries \| Documentation" — F22 s10; "Fokus/CLAUDE.md AppSettings VO-list doc correction" — Pass3b s6 | 2026-07-18 | rejected (governed-by-convention: kb-maintenance rule + kb-entry-schema exist — cross-repo §B) |
| build-verification-sweep | gap | 19 rows / 18 plans | terminal plan steps across 18 plans (e.g. F32(reflekt) s5, Pass2 s8, F14 s10) | sprint-rituals | "Verification sweep (grep-checkable acceptance) + build (backend + client)" — Pass2 s8 | 2026-07-18 | rejected (governed-by-convention: build-verification is a convention-mandated ritual step — cross-repo §B) |
| extend-existing-analytics-service | gap | 12 rows / 8 plans | fokus F14 s4, F19 s1, F20 s1-4, F26 s10, F30 s3, F31 s2-3, F33 s1-2 + lessons: F14 `metric-refactor`, F19 `extend-service-response`, F26 `extend-existing-service`, adhoc-ActiveSprintAnalytics `widen-analytics-endpoint` | sprint-rituals | "Extend existing DeveloperSummary record and ComputeLeaderboard method — modifications to an existing service, not creation" — F19 s1; skeptic: two candidates, not one (vs cross-cutting) | 2026-07-18 | candidate |
| extend-external-integration-module | gap | 8 rows / 6 plans | fokus adhoc-SettingsDropdowns s1-3, adhoc-SprintEdit s3, F25 s5, adhoc-XraySyncDiscovery s1, adhoc-TeamManagedSync s2, Pass4 s10 + lessons: adhoc-SprintEdit `jira-write-operation`, F25 `extend-module-contract` | sprint-rituals | "Extend Xray.Contracts DTOs + interface method \| No skill for extending module contracts" — adhoc-XraySyncDiscovery s1 | 2026-07-18 | candidate |
| grep-guarded-deletion | gap | 7 rows / 6 plans | fokus F15 s4+s10, adhoc-XraySyncDiscovery s5, ReflektValueObjects s6, Pass3c-B s9, Pass3c-C s10, Pass3c-ComputationToDomain s7 | sprint-rituals | "Remove SetSubTeam endpoint folder and repository method \| Inline: deletion, no skill needed" — F15 s4 | 2026-07-18 | candidate |
| cross-cutting-rule-across-analytics-services | gap | 30 rows / 5 plans | fokus F16 s2+s5-10, F21 s1-4, F22 s1-9, F23 s1+s3-11, F24 s1-5 + lessons: F23 `metric-query-patterns`, F16 `apply-effective-sp`, F24 `update-analytics-computation`, F22 (param-type migration) | sprint-rituals | "Extract CompletionChecker utility... \| Cross-cutting analytics utility — no skill" — F22 s1; plans self-cross-reference "Same pattern as TransitionBasedSprintScope" | 2026-07-18 | candidate |
| relocate-extract-code-unit | gap | 13 rows / 4 plans | Pass3-DomainEnrichment s2-4, Pass3c-ComputationToDomain s1, fokus adhoc-ExtractJiraModule s3-6, adhoc-JiraModuleRefactor s1-5 + lessons: Pass3 (relocate DTO-mapping factory, ADR-006) | sprint-rituals | "Relocate 4 static helpers to Fokus.Domain/Analytics/ (namespace change + using update)" — Pass3c-ComputationToDomain s1; skeptic excluded Pass3c-B/C s1 (new-primitive creation, not relocation) | 2026-07-18 | candidate |
| tooltip-wiring | gap | 12 rows / 4 plans | fokus adhoc-TooltipWiring s1-8, F12 s12, F24 s6 + lessons: adhoc-TooltipWiring `wire-tooltips`, F14 `tooltip-wiring` | sprint-rituals | "Tooltip wiring from help.tooltips.md \| Inline: follows help-tooltips rule" — F12 s12 | 2026-07-18 | candidate |
| mechanical-rename-suffix-sweep | gap | 10 rows / 4 plans | Pass6 s1-2, ReflektSprintOwnership s1+s5, Pass2 s5-6, fokus adhoc-ActiveSprintAnalytics s1-4 + lessons: Pass6 ("No skill for mechanical renames"), Pass2 (gRPC/Refit wire-safe rename) | sprint-rituals | "**Skill:** None (mechanical rename/type-move — no skill teaches a project-wide rename)" — ReflektSprintOwnership s1 | 2026-07-18 | candidate |
| sync-pipeline-orchestration | gap | 6 rows / 4 plans | fokus F25 s12+s14, adhoc-XraySyncDiscovery s3, adhoc-SyncRefactoring s4+s6, Pass4 s11 + lessons: F25 `cross-api-sync-service` + `extend-sync-pipeline`, adhoc-XraySyncDiscovery (caller-site sweep) | sprint-rituals | "XrayIssueSyncService: dual-API orchestration (Jira links + Xray GraphQL), upsert logic... no skill for cross-API sync orchestration" — F25 s12; skeptic excluded TeamManagedSync s3/s4 (its gap is `refactor-for-inheritance`) | 2026-07-18 | candidate |
| compose-host | gap | 5 rows / 4 plans | fokus adhoc-SuiteInfrastructure s5-6, F25 s7, adhoc-ExtractJiraModule s5 + lessons: adhoc-SuiteScaffolding `compose-host` | sprint-rituals | "**Skill:** None — Host composition. No skill covers Kestrel h2c configuration or MapGrpcService wiring" — adhoc-SuiteInfrastructure s5; carved out of the killed-as-grab-bag di-host-registration cluster | 2026-07-18 | candidate |
| settingsview-ui-extension | gap | 4 rows / 4 plans | fokus F13 s9, F12 s11, adhoc-SettingsDropdowns s6 + lessons: F26 `vue-settings-panel-extension` | sprint-rituals | "Settings form extension — straightforward field additions \| Log: no 'settings-form' skill" — F13 s9; skeptic: frontend shape only, backend half is `add-settings-field` | 2026-07-18 | candidate |
| signalr-hub-patterns | gap | 4 rows / 3 plans | fokus F32 s3, F6-PlanningPoker s7, reflekt F29 s4-5 + lessons: F29 `add-hub-method`, F32 `signalr-patterns`, F6 (corroborating: "skill doesn't cover SignalR hub methods directly") | sprint-rituals | "SignalR hub creation, DI registration, hub context injection into sync endpoints \| Log: no skill for SignalR infrastructure" — F32 s3 | 2026-07-18 | candidate |
| vue-chart-components | gap | 3 entries / 3 plans | lessons: fokus F10 `create-apex-chart`, F11 `vue-analytics-components`, F12 `apexcharts-scatter` | sprint-rituals | "patterns for ApexCharts integration in Vue (series shapes, options structure, dark theme tokens...)" — F10 lessons; skeptic: F28 `inline-sparklines` is OUT (raw SVG, no chart lib) | 2026-07-18 | candidate |
| group-scalars-into-value-object | gap | 4 rows / 2 plans + routed proposal | Pass3b s3-4, ReflektValueObjects s4 + lessons: ReflektValueObjects `[ROUTED-TO-PLUGIN]` proposal (Evidence names both plans); see docs/plugin-feedback/nexus-1.18.6-2026-06-29.md (upstream status `[DEFERRED]`) | sprint-rituals | "Repoint 6 internal read sites... wire DTOs + SignalR payload keys stay flat \| boundary flatten/unflatten read-side map — same family as Pass-3b Step 4 gap" — ReflektValueObjects s4 | 2026-07-18 | confirmed (upstream `[DEFERRED]`; build local if the plugin doesn't ship it — cross-repo §C) |
| apply-mapster-mapping | gap | 2 rows / 2 plans | Pass2 s7 (+s2 Mapster DI bootstrap sub-case) + lessons: Pass2 (`apply-mapster-mapping`, ADR-010), Pass3 ("Same gap Pass 2 logged for Mapster-application generally") | sprint-rituals | "Apply Mapster IRegister + .Adapt<T>() to the trivial-map handlers... no Mapster-application skill — ADR-010 candidate" — Pass2 s7; skeptic pulled this row OUT of the VO-grouping cluster | 2026-07-18 | confirmed (build queued — cross-repo §C) |
| add-settings-field | gap | 2 entries / 2 plans | lessons: fokus adhoc-SettingsGapFill `add-settings-field`, F13 `extend-app-settings` (straddles backend+frontend; frontend half sits with settingsview-ui-extension) | sprint-rituals | "domain class + response DTO + command + validator + endpoint mapping + EF migration in lockstep" — adhoc-SettingsGapFill lessons | 2026-07-18 | candidate |
| promote-entity-to-aggregate | gap | 1 entry / 1 plan | lessons: Pass4-IdentityExtraction | sprint-rituals | "No skill covers promoting an existing Entity to an AggregateRoot in place... Candidate for a `promote-entity-to-aggregate` skill or a `create-aggregate` variant" — Pass4 lessons | 2026-07-18 | candidate |
| external-api-client-graphql | gap | 1 entry / 1 plan (+2-plan sub-family) | lessons: fokus F25 `external-api-client-graphql`; below-threshold pair with adhoc-XraySyncDiscovery s2 (Xray GraphQL query + parser) | sprint-rituals | "External API client with bearer token management and GraphQL... token cache pattern... rate limiter" — F25 lessons | 2026-07-18 | candidate |
| extract-view-to-tabs | gap | 1 entry / 1 plan | lessons: fokus adhoc-SettingsViewSplit | sprint-rituals | "splitting a large settings/dashboard view into tab components via provide/inject" — adhoc-SettingsViewSplit lessons | 2026-07-18 | candidate |
| refactor-for-inheritance | gap | 1 entry / 1 plan | lessons: fokus adhoc-TeamManagedSync | sprint-rituals | "No skill for 'inheritance refactoring of an existing class'... Suggested name: `refactor-for-inheritance`" — adhoc-TeamManagedSync lessons | 2026-07-18 | candidate |
| repository-query-method | gap | 4 rows / 4 plans | fokus F26 s5, F23 s2, F31 s1, F28 s3 + lessons: F26 `repository-cross-entity-query` | sprint-rituals | "sprint-scoped TE query joining through TestExecutionLink -> SprintMembership \| Log: no skill for complex cross-entity query methods" — F26 s5 (persistence-patterns exists but does not cover the complex-join pattern) | 2026-07-18 | candidate |
| cross-cutting-exclusion-filter | gap | 1 entry / 1 plan | lessons: fokus F15-TeamManagement (same lockstep family as cross-cutting-rule candidate — owner may fold) | sprint-rituals | "threading an optional exclusion set through multiple analytics service methods" — F15 lessons | 2026-07-18 | candidate |
| subteam-null-absent-detection | gap | 1 entry / 1 plan | lessons: fokus F15-TeamManagement | sprint-rituals | "detect JSON key presence to distinguish null-clear from not-provided" — F15 lessons | 2026-07-18 | candidate |
| add-sortable-table | gap | 1 entry / 1 plan | lessons: fokus adhoc-UIImprovements | sprint-rituals | "local sort state (sortColumn/sortDirection refs), toggle function, sort icon helper, sorted computed" — adhoc-UIImprovements lessons | 2026-07-18 | candidate |
| settings-tab-layout | gap | 1 entry / 1 plan | lessons: fokus adhoc-UIImprovements | sprint-rituals | "adding tab nav bar to a settings view with per-panel save buttons" — adhoc-UIImprovements lessons | 2026-07-18 | candidate |
| add-developers-tab | gap | 1 entry / 1 plan | lessons: fokus F29-QaWorkloadThroughput ("All five F2x features follow the same pattern") | sprint-rituals | "No skill for frontend developer tab integration (tab button, URL sync extension, store wiring)" — F29 lessons | 2026-07-18 | candidate |
| stage-time-accumulation | gap | 1 entry / 1 plan | lessons: fokus F12-CycleTime | sprint-rituals | "cycle time stage accumulation algorithm (transition-walk, sprint clamping, rework counting)" — F12 lessons | 2026-07-18 | candidate |
| streak-computation | gap | 1 entry / 1 plan | lessons: fokus F28-PerDeveloperStoryQuality | sprint-rituals | "Below-median streak computation... walk-backward streak algorithm for any metric" — F28 lessons | 2026-07-18 | candidate |
| inline-sparklines | gap | 1 entry / 1 plan | lessons: fokus F28-PerDeveloperStoryQuality | sprint-rituals | "normalizing a SparklinePoint[] array into an SVG path d attribute... rendering as svg + path" — F28 lessons | 2026-07-18 | candidate |
| shared-cross-slice-wire-record | gap | 1 entry / 1 plan | lessons: Pass3c-C-DevEpicAnalytics | sprint-rituals | "No skill covers a deliberately-shared cross-slice wire-record file (Option B)... duplicate (slice purity) or share (DRY)" — Pass3c-C lessons | 2026-07-18 | confirmed (build queued — cross-repo §C; convention-note-sized) |
| create-vue-feature-retro-variant | fix | 2 entries / 2 plans | lessons: adhoc-ReflektFrontendMerge (retro-app variant), F6-PlanningPoker (corroborating: apiFetch wrapper absent, adapted) | sprint-rituals | "create-vue-feature skill is dashboard-centric (apiFetch, PageLayout, PageToolbar, setup-store). The retro app has none of these primitives" — ReflektFrontendMerge lessons | 2026-07-18 | candidate |
| create-domain-event-handler-broken-ref | fix | 1 entry / 1 plan | lessons: reflekt F30-OpenActions (plugin-bound — route via docs/plugin-feedback/) | sprint-rituals | "create-domain-event-handler references workflows/Handler.md which does not exist in the skill directory" — F30(reflekt) lessons | 2026-07-18 | candidate |
| create-grpc-contract-client-prereqs | fix | 1 entry / 1 plan | lessons: Pass4-IdentityExtraction (plugin-bound) | sprint-rituals | "the skill gives no guidance for the Build step (porting the helper + CPM packages)... 'client infra prerequisites' note" — Pass4 lessons | 2026-07-18 | candidate |

## Historical candidates now covered (skill exists / gap closed)

| name | kind | recurrence | citations | repo | skeptic excerpt | last_verified | status |
|------|------|-----------|-----------|------|-----------------|---------------|--------|
| frontend-vue-feature-slice | gap | 41 rows / 12 plans | fokus F6-F11, F19, F29-F31, F33, F35 plan rows | sprint-rituals | "Frontend TypeScript types for all API response shapes \| Frontend patterns gap" — F8 s7 | 2026-07-18 | built (create-vue-feature, pinia-patterns, vue-component-architecture) |
| analytics-metric-computation-service | gap | 7 rows / 7 plans | fokus F26 s6, F27 s1, F28 s4, F29 s1, F30 s1, F32 s1, F34 s3 + lessons: F27 (multi-sprint metric computation) | sprint-rituals | "QaMetricsService: metric computation... \| Log: no skill for metric computation patterns" — F26 s6 | 2026-07-18 | built (analytics-computation-service, domain-service) |
| theming-dark-light-vue | gap | 6 rows / 3 plans | fokus F7 s1-2+s8, reflekt F31 s3, F32 s3-4 | sprint-rituals | "Design tokens via Tailwind v4 @theme directive \| Frontend patterns gap" — F7 s1 | 2026-07-18 | built (tailwind-theme; flash-prevention sub-shape still uncovered — see Capture leaks) |
| auth-oauth-setup | gap | 4 rows / 2 plans (below threshold) | fokus F18 s4+s8+s12, Pass4 s6 | sprint-rituals | "Google OAuth middleware setup, cookie auth config, custom callback handler \| no auth-setup skill" — F18 s4 | 2026-07-18 | built (authorization-patterns) |
| central-package-management | gap | 2 entries / 2 plans | lessons: Pass0, Pass1 | sprint-rituals | "No skill covers CPM... Create in Pass 1" — Pass0 lessons | 2026-07-18 | built |
| framework-currency | gap | 2 entries / 2 plans | lessons: Pass0, Pass1 | sprint-rituals | "No rule enforces 'latest stable + modern Send.* API'" — Pass0 lessons | 2026-07-18 | built |
| extract-endpoint-types + extract-feature-service | gap | 2 entries / 1 plan | lessons: fokus adhoc-SyncRefactoring (both `[APPLIED]`) | sprint-rituals | "[APPLIED] Missing skill: 'Extract response/DTO types from endpoint files into sibling command/query files'" — adhoc-SyncRefactoring lessons | 2026-07-18 | built |
| create-building-blocks-package | gap | 1 entry / 1 plan | lessons: fokus adhoc-SkillAlignment (`[APPLIED]`, plus two persistence-patterns fixes `[APPLIED]`) | sprint-rituals | "[APPLIED] Missing skill: create-building-blocks-package — Steps 1-6 each build a BuildingBlocks package from scratch" — adhoc-SkillAlignment lessons | 2026-07-18 | built |
| domain-service (+ Pass1 skill-repair batch) | gap | 1 entry / 1 plan | lessons: Pass1-SkillRepair (domain-service MISSING; analytics-computation-service SMELL; domain-patterns VO rule) | sprint-rituals | "domain-service — MISSING; largest gap; shape reserved for user review (Q2)" — Pass1 lessons | 2026-07-18 | built |
| vue-frontend-patterns | gap | 1 entry / 1 plan | lessons: fokus F7-AppShell (`[APPLIED]`) | sprint-rituals | "[APPLIED] Missing skill: Frontend Vue 3 component patterns... Suggested name: `vue-frontend-patterns`" — F7 lessons | 2026-07-18 | built (vue-patterns family) |
| create-standalone-service | gap | 1 entry / 1 plan | lessons: fokus F1-Scaffolding (`[APPLIED]`) | sprint-rituals | "[APPLIED] Missing skill: create-standalone-service — the existing create-service skill assumes shared BuildingBlocks exist" — F1 lessons | 2026-07-18 | built |
| add-integration-event-consumer-side | gap | 1 entry / 1 plan | lessons: adhoc-ReflektSprintOwnership | sprint-rituals | "add-integration-event is publisher-oriented; its consumer-side + 'second module joins an existing single in-memory bus' coverage is unverified" — ReflektSprintOwnership lessons | 2026-07-18 | built (consumer-patterns; shared-bus Host topology belongs to compose-host candidate) |
| frontend-data-layer / create-vue-page | gap | 2 entries / 2 plans | lessons: fokus F30-TestExecutionTimeline, F10-ScopeChangeDisruption | sprint-rituals | "a skill for adding TypeScript interfaces... API functions... store refs/fetches" — F30 lessons | 2026-07-18 | built (create-vue-feature) |

## Killed at verify

- **grpc-server-impl-adaptation** (3-plan Tier-B cluster) — KILLED by the skeptic: only
  adhoc-SprintContextPanel s1 modifies an existing gRPC contract; Pass3c-B s8 and
  Pass3c-ComputationToDomain s5 are mechanical ctor/call-site rewiring after calculator
  relocation. The surviving field-addition flag has no lessons entry → recorded under
  Capture leaks, not as a counted candidate.
- **di-host-registration** (8-plan Tier-B cluster) — TRIMMED to death as a grab-bag: mechanical
  one-line DI swaps (not skill-worthy), Host composition (→ `compose-host` candidate), and
  Mapster DI bootstrap (→ `apply-mapster-mapping`).

## Capture leaks

Gap flags the estate noticed in a plan's Gap?/justification cell but never logged as a lessons
`## Skill Gaps` entry — a standing to-do to log the gap, not counted candidates:

- `adhoc-SprintContextPanel` step 1 — "additive field-addition to an existing contract...
  `create-grpc-contract` creates new contracts, doesn't teach this"
- `adhoc-Pass3c-B-DeliveryMetrics` step 1 — "Gap candidate: a 'shared-domain-primitive
  extraction' pattern" (its lessons say "No gaps discovered")
- `reflekt/F31-DarkLightTheme` step 3 — "Flash prevention pattern could be a skill" (no
  Skill Gaps entry in its lessons)
- `adhoc-Pass5-EventCorrectness` step 4 — RepositoryBase.UpsertAsync private-backing-field copy
  flagged as a persistence-patterns gap in the plan; no lessons entry

## Notes

- **Skill-tool registration failure** (adhoc-ReflektFrontendMerge lessons, Skill Gaps item 1):
  5 nexus-dotnet Vue skills present in the plugin cache but not invocable via the Skill tool at
  the time — a plugin/runtime issue, not a skill gap; belongs in the communication-log/plugin-
  feedback channel, not this registry.
- **Learner fence:** `group-scalars-into-value-object` is already learner-routed
  (`[ROUTED-TO-PLUGIN]`, docs/plugin-feedback/nexus-1.18.6-2026-06-29.md, upstream `[DEFERRED]`).
  Its row links that record and does not re-own it. No other candidate overlaps a `[TRACKED]`
  learner item.
- **Below-threshold clusters (2 plans, for the record):** repo-suite-scaffolding
  (adhoc-SuiteScaffolding + adhoc-SuiteScaffoldingV2); external-api-client GraphQL sub-family
  (F25 s6 + adhoc-XraySyncDiscovery s2). pipeline-agent-governance-meta-doc is a single-plan
  meta shape (adhoc-PipelineCraftImprovements) — not counted.
- Verification scratch artifacts (orchestrator-side, outside the estate): tierb-collection.md,
  skeptic-verdicts.md in the session scratchpad.
- **Cross-repo cross-check applied (2026-07-18):** `<private-repo>\docs\skill-gaps\cross-repo.md`
  compared the four suite registries. Consequences here: the two §B ritual-step rows flipped to
  `rejected`; the three sprint-rituals-own §C rows flipped to `confirmed` (build queue below).
  Rows evidenced by `fokus/*` / `reflekt/*` plans (tooltip-wiring, vue-chart-components,
  settingsview-ui-extension, extend-external-integration-module, the 1-plan fokus tail…) stay
  `candidate` here pending the §D.2 subsumption decision — their builds are queued in the fokus
  home registry; do not count them as independent recurrence in future cross-checks. The §A
  plugin-grade themes (extension-not-creation, compose-host, signalr-hub-patterns, mechanical-sweep
  checklist…) route via `docs/plugin-feedback/`, not local builds.

## Build queue (owner-confirmed 2026-07-18, sprint-rituals-own)

1. `apply-mapster-mapping` — 2 plans, ADR-010 backing; project-local skill.
2. `shared-cross-slice-wire-record` — 1 plan; likely a convention note rather than a full skill.
3. `group-scalars-into-value-object` — hold for upstream (`[DEFERRED]` in plugin-feedback
   nexus-1.18.6); build local only if the plugin pass doesn't ship it.

## Changelog

- 2026-07-18 — Initial registry from full-estate sweep (77 plans / 74 lessons). 33 open
  candidates (30 gaps, 3 fixes), 13 covered/built rows, 1 killed cluster, 4 capture leaks.
  Skeptic: fresh-context subagent; verdicts in session scratchpad `skeptic-verdicts.md`.
- 2026-07-18 — Owner triage per cross-repo cross-check (knowledge-gateway
  `docs/skill-gaps/cross-repo.md` §B/§C): `build-verification-sweep` + `documentation-kb-update`
  → rejected (governed-by-convention); `apply-mapster-mapping` + `shared-cross-slice-wire-record`
  → confirmed (build queued); `group-scalars-into-value-object` → confirmed (upstream hold).
  Fokus/reflekt-evidenced rows left `candidate` pending the §D.2 subsumption decision.
