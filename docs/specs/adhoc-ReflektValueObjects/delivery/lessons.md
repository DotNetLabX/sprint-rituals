# RR-B — Reflekt Rich Value Objects — Lessons

> **Learner consolidation — 2026-06-29** (→ `docs/plugin-feedback/nexus-1.18.6-2026-06-29.md`)
> - `[ROUTED-TO-PLUGIN]` new skill `group-scalars-into-value-object` (the Improvement Proposal below) — promoted at recurrence 2x (adhoc-Pass3b-AppSettingsVOs + this run). Confirmed absent in live nexus-dotnet 1.2.0.

## Architect Lessons

- **Phase-1 read-site census missed three of six sites; the critic caught it (C1).** My analysis listed three flat-prop consumers (Mappings, GetRetroSummary, CreateRetro) but the real count was six — I missed both Report endpoints (`GetRetroReport`/`GetParticipantReport`, `BuildColumnSection` calls) and, critically, `BroadcastColumnConfigUpdatedHandler`, which feeds the SignalR `ColumnConfigUpdated` payload. The miss made the plan's "SignalR untouched" claim false. **Root cause:** I enumerated read sites from the files I happened to open in Phase 1 rather than from a single authoritative grep of the aggregate-property accesses. **Fix already internalized:** a "fix every consumer of symbol X" census must come from one grep (definition-or-usage as appropriate), and that same grep must be the step's acceptance check — the plan-skill already says this; I under-applied it for a *read-site* sweep (vs the more obvious removal/rename case). For RR-C/RR-D, run the consumer grep at plan-write time and paste its output into the plan, even when the sites "look" few.
- **A camelCase SignalR/JSON payload is a wire contract that a PascalCase grep silently skips.** The verification grep `\.(Column1Name|…)` is anchored to the C#-property casing and does not match the `column1Name` payload keys — which is correct (the keys must stay) but means the grep alone can't prove the SignalR source was repointed. The plan handled this by adding a separate diff-unchanged accept for the payload keys + a DO-NOT-CHANGE note; worth carrying as a pattern: when a refactor touches a serialized anonymous object, the done-check needs a *keys-unchanged* assertion distinct from the *source-repointed* grep.

## Skill Gaps

- **VO-grouping boundary read-map has no dedicated skill** (recurring — first logged in Fokus `adhoc-Pass3b-AppSettingsVOs` Step 4). RR-B Step 4 (group flat scalars → VO, then sweep every read site + add explicit Mapster flatten rules at the wire-flat boundary) was disposition `None` again. Two features now exhibit the same shape.

### Improvement Proposal `[ROUTED-TO-PLUGIN]` (2026-06-29, nexus-1.18.6 feedback)
**Target:** new skill `group-scalars-into-value-object` (nexus-dotnet, refactoring family)
**Change:** Codify the flat-scalars→VO refactor end to end: (1) extract the VO (compose existing single-value VOs, don't duplicate guards); (2) the `required init` → `private set` accessor flip when a `required` member becomes a VO member (CS9032 pre-empt); (3) EF `ComplexProperty` with the **rename-not-drop** migration (the `{Prop}_{Member}` column-name rule); (4) the read-site sweep driven by one authoritative grep that is also the acceptance check, explicitly including serialized payloads (SignalR/JSON) whose camelCase keys a PascalCase grep won't match; (5) explicit Mapster `.Map` rules at the wire-flat boundary because the regrouped source paths no longer line up with flat destination names.
**Evidence:** Fokus `adhoc-Pass3b-AppSettingsVOs` (Step 4 `None`, ComplexProperty rename migration, manual read-site sweep) and Reflekt `adhoc-ReflektValueObjects` (RR-B, Step 4 `None`, six read sites incl. a missed SignalR consumer caught by the critic). Same disposition gap, same hazards, two services.
**Priority:** medium
