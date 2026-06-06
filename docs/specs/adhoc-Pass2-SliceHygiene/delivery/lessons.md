# Pass 2 — SliceHygiene — Lessons

## Architect Lessons

- **Enumerate fat files with a definition-line grep, not a usage grep.** My first Step-3 list (14) was built from a broad `class .*(Request|Response|Validator|Dto)` scan that conflates *definitions* with *usages* (e.g. `EndpointWithoutRequest<MeResponse>` is a usage, not an inline definition) and also undercounted because I enumerated by memory of the files I'd read rather than a single authoritative grep. The correct count was 16. The precise pattern is `^\s*(public\s+)?(class|record)\s+\w+(Request|Response|Validator|Dto)\b`. The critic caught the 2-file gap (`GetDevelopers`, `GetCycleTimeBoundaries`). Lesson: for "split every file of class X" passes, derive the file list from the exact acceptance grep and make the two identical — never hand-curate the list separately from the check.

- **`EndpointWithoutRequest` features are still ADR-008 split targets even with no request type.** Three fat files (`GetTeamRoster`, `TestConnection`, `GetDevelopers`, `GetCycleTimeBoundaries`) are request-less reads that still define a response/DTO inline. The split moves the response/DTO to a `*Query.cs` sibling. Easy to miss when scanning for `*Request` types.

- **Mapster eligibility is narrower than "any hand-assignment."** ADR-010 says trivial mapping → Mapster, but the real boundary is *single-source object→object with name-aligned fields*. Method-dispatch handlers (`SetCapacity` calls `UpsertCapacityAsync(...)`), multi-source assembly (`UserResponse` from User + Person), and conditional logic (`SaveXraySettings` secret-preservation, `SaveCycleTimeBoundaries` stage-order) are NOT Mapster targets. A plan that says "replace hand-mapping with Mapster" without this carve-out will push the developer to force-fit `.Adapt` and break behavior. The strongest future-skill candidate is an `apply-mapster-mapping` skill encoding this eligibility test.

- **Wire-stability is a first-class plan concern when renaming contract types.** The MAXIMAL Q1 scope (rename gRPC/Jira `*Request`) is only safe because protobuf-net.Grpc keys the wire on `[DataMember(Order)]` and Refit serializes by property name — both decouple the wire from the C# type name. The plan must state the invariant ("don't touch Order numbers / property names") as a binding rule with a diff-based acceptance check, not assume the rename is mechanically safe.

- **Path-A field renames have a camelCase translation step the plan must spell out.** Renaming C# `AlertThreshold`→`BugRatioAlertThreshold` changes the wire JSON key to `bugRatioAlertThreshold`; the hand-written TS client payload must change to match. And the false-positive collisions (`workloadAlert.consecutiveSprintCount`, the cycle-time *response* read paths) must be explicitly fenced or a find/replace breaks unrelated code.

### Improvement Proposal (optional, for systemic issues)
**Target:** `.claude/plugins/.../skills/create-implementation-plan` (or a new `apply-mapster-mapping` skill)
**Change:** Add an explicit "Mapster eligibility test" (single-source + name-aligned ⇒ Follow; method-dispatch / multi-source / conditional ⇒ leave explicit). And add a planning rule: "when a step says 'fix every file matching pattern P', the step's file list MUST be generated from the same grep used in its acceptance criterion."
**Evidence:** Pass 2 — Step 3 undercount (14 vs 16) caught by critic; Step 7 Mapster eligibility required a hand-written carve-out with no skill backing.
**Priority:** medium

## Skill Gaps
- **No skill for applying Mapster to handlers (ADR-010 application).** `extract-endpoint-types` covers the split + naming but stops at the mapping mechanism. Steps 6/7 are `None`. Candidate: `apply-mapster-mapping`.
- **No skill for renaming gRPC code-first / Refit contract types wire-safely.** `extract-endpoint-types` is FastEndpoints-scoped. Step 5 is `None`. Lower priority (rare operation).
- **No skill for core-Mapster DI bootstrap** (`TypeAdapterConfig.GlobalSettings.Scan` without `Mapster.DependencyInjection`). Step 2 is `None`.
