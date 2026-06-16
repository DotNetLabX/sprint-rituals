# adhoc-SprintContextPanel — Developer Lessons

## L1 — Extract assembly logic when the endpoint uses `Send.*`

FastEndpoints endpoints that call `Send.OkAsync` / `Send.NotFoundAsync` require the full HTTP pipeline to run `HandleAsync`. Rather than spinning up a test host, extract the non-trivial assembly logic into a scoped service (`*Assembler`) and test that directly. The endpoint becomes a thin shell: load the aggregate, delegate, send. This is the right split when the assembly logic has meaningful branching (fallback states, ordering, Rule 6 gate) and the infrastructure binding (HTTP context) adds no value to the test.

## L2 — `[DataMember]` is append-only; document the boundary explicitly

Adding fields to a `[DataContract]` gRPC class is safe because protobuf-net uses `Order` for wire encoding — existing clients receive zeros/nulls for new fields and ignore them. The risk is silently breaking old consumers if you re-use or reorder existing Order numbers. Add a block comment marking the addition boundary ("Orders 9–15 — RA4 additive") so the next developer knows what's safe to touch.

## L3 — Rate vs. RAG provenance must be documented at the mapping site

`DisruptionRate` on `AnonymizedSprintSummary` is Scope + Bug combined. `DisruptionRag` is computed by the Fokus analyzer against `ScopeDisruptionRate` only. This disconnect is not obvious from the field names. A brief comment at the mapping site (`SprintAnalyticsGrpcService.cs`) and at the assembler callsite prevents a future developer from "fixing" the RAG by recomputing it against the summed rate — which would be wrong.

## L4 — Hand-rolled fakes are sufficient when the interface is small and stable

`ISprintAnalyticsService` has 3 methods. `StubSprintAnalytics`, `ThrowingSprintAnalytics`, and `SelectiveThrowingSprintAnalytics` were written in ~40 lines. No mocking framework was needed. This keeps the test project's dependency footprint small and makes the test intent explicit (no `.Setup(x => x.Method(...)).Returns(...)` noise).

## L5 — Per-sprint gRPC isolation: catch at the right granularity

The `catch (Exception)` wraps a single sprint's gRPC call, not the entire loop. Catching at loop-iteration level is the correct granularity here — one sprint's Fokus outage must not degrade the entire panel. Catching too wide (wrapping the full loop) would short-circuit remaining sprints on first failure.

## Reviewer Lessons

### RL1 — `catch (Exception)` for gRPC isolation still needs cancellation exclusion

When a broad `catch (Exception)` is justified for per-item isolation (e.g. per-sprint gRPC failure isolation), it still must exclude `OperationCanceledException`. Missing the filter causes cancellations to be treated as gRPC failures — unnecessary snapshot lookups proceed after the client has disconnected. Pattern to apply: `when (ex is not OperationCanceledException)`. Established in this codebase at `OpenGraphFetcher.cs:40,61`.

### RL2 — `AsNoTracking` absence is a pre-existing project-wide gap, not a per-feature finding

Neither Fokus nor Reflekt repositories use `.AsNoTracking()` on read-only queries. Flagging it on newly-added methods only (while ignoring the same gap in adjacent methods) creates false asymmetry. Rate it LOW and note the pre-existing context; avoid escalating to MEDIUM as if introduced by the feature under review.
