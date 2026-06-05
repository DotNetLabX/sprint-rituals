# Skill Domain Summary

Extracted and quoted findings from five SKILL.md files, answering five architecture questions.
Research-only — no code was modified.

---

## 1. domain-patterns/SKILL.md

### Q1 — Where should business computation/logic live?

SILENT on the question of which layer owns computation. The skill covers aggregate structure,
value objects, domain events, and event dispatch only. It does not sanction or prohibit
service classes in the API/Features folder.

### Q2 — May computations depend on API response DTOs/records?

SILENT — gap. No mention of response types, Info records, or any coupling rule between
domain and API layer types.

### Q3 — Value objects / embedded entities vs flat scalar properties

Directly addressed. From **"Value Objects"** section:

> "Three base types:
> - `ValueObject` — multi-property, implements `GetEqualityComponents()`
> - `StringValueObject` — single string `Value`, full equality (`Equals`, `==`/`!=`, `GetHashCode`)
> - `SingleValueObject<T>` — single struct `Value`, handles boxed `T` in `Equals(object?)`"

> "Pattern: static `Create()` factory with validation, internal constructor"

> "**Per-bounded-context duplication is intentional.** Each service defines its own VOs even if the shape
> is identical. This follows DDD — each BC owns its types. Only share VOs through BuildingBlocks when they
> are truly cross-cutting (e.g., `SingleValueObject<T>` base)."

From **"Partial Class Behavior Split"** section:

> "`{Aggregate}.cs` — properties, backing collections"
> "`Behaviors/{Aggregate}.cs` — domain methods that mutate state and raise events"

> "Backing collection pattern:
> ```csharp
> private readonly List<LineItem> _lineItems = new();
> public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();
> ```"

The skill prescribes value objects with factory + internal constructor, and backing collections
(embedded child entities) exposed as `IReadOnlyList`. Flat scalar properties are not explicitly
forbidden but the pattern clearly favors typed VOs.

### Q4 — Analytics computation service shape

SILENT — gap. This skill covers domain layer only.

### Q5 — Domain services spanning aggregates

SILENT — gap. The skill covers aggregates and events but says nothing about cross-aggregate
computation services or where they belong.

---

## 2. create-aggregate/SKILL.md

### Q1 — Where should business computation/logic live?

SILENT on computation layer placement. The skill is scaffolding-only (steps to create the
aggregate, VOs, events, EF config, repository, migration).

### Q2 — May computations depend on API response DTOs/records?

SILENT — gap.

### Q3 — Value objects / embedded entities vs flat scalar properties

Indirectly addressed by delegation: "Create value objects (if needed) — follow `workflows/ValueObject.md`"
and "Create domain events (if needed) — follow `workflows/DomainEvent.md`". The skill assumes the
domain-patterns pattern (VO factory, internal constructor) without restating it. No explicit guidance
on when to prefer a VO vs a flat property.

SKILL SMELL: The skill references `workflows/AggregateEfCore.md`, `workflows/AggregateRedis.md`,
`workflows/ValueObject.md`, and `workflows/DomainEvent.md` but these workflow files are not present in
the skill folder — the guidance is deferred to files that do not exist, leaving implementers without
the actual patterns.

### Q4 — Analytics computation service shape

SILENT — gap. This skill is domain-layer scaffolding only.

### Q5 — Domain services spanning aggregates

SILENT — gap.

---

## 3. analytics-computation-service/SKILL.md

### Q1 — Where should business computation/logic live?

Directly addressed. From **"Service structure"** section:

> "1. **Response records at the top of the file** — all records the service returns, using `sealed record`.
> 2. **Service class** — primary constructor with repository dependencies.
> 3. **Public method(s)** — typically `ComputeMultiSprint` and/or `ComputeSingleSprint`.
> 4. **Private helpers** — decomposed computation methods: `BuildMetricCards`, `BuildSparkline`, `ComputeDelta`, etc."

Service classes in the API/Features folder are explicitly sanctioned for analytics computation:

> "Reference files ... `Fokus.API/Features/Analytics/BugRatioService.cs`"
> (all reference files live under `Fokus.API/Features/Analytics/`)

From **"Endpoint wiring"**:

> "The endpoint creates the service (or receives it via DI), loads sprint data from repositories,
> and calls the service method. **The service never touches repositories directly — it receives
> pre-loaded data.**"

> "**Exception:** Some services accept repository references for targeted queries (e.g., loading test
> executions for a specific sprint set). This is acceptable when the query is tightly coupled to the
> computation."

SKILL SMELL: The "Exception" clause weakens the "never touches repositories directly" rule to
"sometimes acceptable", with a subjective criterion ("tightly coupled"). This creates ambiguity about
where the line is.

### Q2 — May computations depend on API response DTOs/records?

Directly addressed — and affirmatively yes. From **"Service structure"**:

> "**Response records at the top of the file** — all records the service returns, using `sealed record`."

The skill co-locates response records and the computation service in the same file. Computation
methods return these response records directly. There is no separation between the service's output
type and the API response type — they are the same.

SKILL SMELL: This directly couples computation logic to API response shapes. If a response record
changes (e.g., a new frontend field is added), the computation service's method signatures change too.
The skill provides no abstraction boundary between domain computation and API output. No guidance says
response records must be free of API-layer concerns.

### Q3 — Value objects / embedded entities vs flat scalar properties

SILENT — gap. This skill covers API-layer computation services, not domain model structure.

### Q4 — Analytics computation service shape (inputs, outputs, parameter count, method size, decomposition)

Fully prescribed. From **"Service structure"**:

> "Primary constructor with repository dependencies."
> "Public method(s) — typically `ComputeMultiSprint` and/or `ComputeSingleSprint`."
> "Private helpers — decomposed computation methods: `BuildMetricCards`, `BuildSparkline`, `ComputeDelta`, etc."

From **"Multi-sprint vs single-sprint"**:

> "**Multi-sprint:** Accepts a list of target sprints + full closed sprint history. Iterates targets,
> computes per-sprint metrics, returns a list."
> "**Single-sprint:** Accepts one sprint + prior sprint (nullable). Computes current values + deltas
> against prior. Returns a single response."

From **"Delta / Direction / Polarity triplet"** (output shape rule):

> "Each numeric column that shows change has three fields:
> - `{Column}Delta` (`decimal?`) — difference from prior sprint (null if no prior)
> - `{Column}Direction` (`string?`) — `"up"`, `"down"`, `"flat"`, or null
> - `{Column}Polarity` (`string?`) — `"positive"`, `"negative"`, `"neutral"`, or null"

From **"Rolling average"**:

> "Uses `TakeLast` AFTER any exclusion guards — not before (ordering matters to avoid returning fewer
> entries than requested)."

No explicit limit on method size or parameter count, but decomposition into private helpers is
mandated by example and structure.

### Q5 — Domain services spanning aggregates

SILENT — gap. The skill does not address cross-aggregate computation or the concept of a domain
service. All prescribed services live in the API layer (`Fokus.API/Features/Analytics/`).

---

## 4. extract-feature-service/SKILL.md

### Q1 — Where should business computation/logic live?

Directly addressed. From **"Steps"**, step 4:

> "**Create the service file** — place it in the feature area root (parent of the endpoint folders):
> ```
> Features/{Area}/{ServiceName}.cs    ← service + result records
> Features/{Area}/{Sub}/Endpoint.cs   ← endpoints that consume it
> ```"

From **"Guardrails"**:

> "**Feature-scoped** — the service lives in the feature area, not in a `Services/` god folder."
> "**Not an entity wrapper** — `SprintService` that accumulates all sprint logic is a guardrail violation.
> Each service covers one operation."
> "**Single-purpose** — if the service grows beyond one cohesive operation area, split it."

From **"What This Skill Does NOT Do"**:

> "Move logic to a separate project — services stay in the API project."

The skill explicitly sanctions service classes in the API/Features folder, with constraints: named after
operation (not entity), single-purpose, feature-scoped.

### Q2 — May computations depend on API response DTOs/records?

Indirectly yes. From **"Steps"**, step 3:

> "**Define result record(s)** — each public method returns a result record. Define them in the same
> file, above the service class:
> ```csharp
> public record {Operation}Result({properties});
> ```"

The pattern co-locates result records with the service. The skill does not address whether these
records may be the same as endpoint response types, but the naming `{Operation}Result` (not
`{Operation}Response`) implies a mild separation. No explicit prohibition on using response
types as service output.

### Q3 — Value objects / embedded entities vs flat scalar properties

SILENT — gap. This skill covers API-layer feature services only.

### Q4 — Analytics computation service shape

SILENT — gap. This skill is for general feature service extraction, not analytics-specific patterns.
(The analytics-computation-service skill covers this.)

### Q5 — Domain services spanning aggregates

SILENT — gap. The skill is scoped to the API/Features layer and does not address cross-aggregate
domain computation.

---

## 5. cqrs-patterns/SKILL.md

### Q1 — Where should business computation/logic live?

Implicitly addressed via the handler pattern. From **"Handler Pattern (Load-Mutate-Save)"**:

> "Pattern: load aggregate → call domain method → save → return response. Domain events dispatch via
> interceptor on `SaveChangesAsync()`."

The prescribed handler calls domain methods on the aggregate — computation lives in the domain layer,
not in the handler. The handler is an orchestrator (load, dispatch, save).

> "`entity.{DomainMethod}({params}, command, _stateMachineFactory);`"

No explicit statement about service classes in the API layer, but the pattern implies the handler
should be thin.

### Q2 — May computations depend on API response DTOs/records?

SILENT — gap. The skill shows handlers returning `new {ResponseType}(entity.Id)` but says nothing
about whether computation logic may depend on response types.

### Q3 — Value objects / embedded entities vs flat scalar properties

SILENT — gap. CQRS skill covers command/query dispatch only.

### Q4 — Analytics computation service shape

SILENT — gap.

### Q5 — Domain services spanning aggregates

SILENT — gap.

---

## Cross-Skill Synthesis

| Question | Skills that answer | Key finding |
|----------|--------------------|-------------|
| Q1: Where does computation live? | analytics-computation-service, extract-feature-service, cqrs-patterns (implicit) | API/Features layer services are sanctioned for analytics/operation logic. CQRS implies domain methods own mutation logic. The two are not reconciled into a single rule. |
| Q2: May computation depend on response DTOs? | analytics-computation-service (yes, by co-location) | SKILL SMELL — response records and computation co-located with no abstraction boundary. Other skills silent. |
| Q3: VO / embedded entity structure | domain-patterns, create-aggregate (by delegation) | VOs prescribed with factory + internal constructor. Backing collections for child entities. When to prefer VO vs scalar: SILENT — gap. |
| Q4: Analytics service shape | analytics-computation-service | Fully prescribed: primary constructor, ComputeMultiSprint/ComputeSingleSprint, private helpers, delta/direction/polarity triplet, sparkline pattern. |
| Q5: Domain services spanning aggregates | All five skills | SILENT — gap. No skill addresses cross-aggregate computation services or where they belong. |
