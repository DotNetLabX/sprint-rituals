# {Concept Name}

## Rules
<!-- Business invariants — one bullet per rule. Each must be falsifiable (a test can encode it). -->
<!-- Format: BR{N}: {rule statement} -->
- BR1: {rule statement}

## Key Files
<!-- Paths relative to repo root -->
- `src/Services/Fokus/Fokus.Domain/Analytics/{Calculator}.cs` — production implementation
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/{Calculator}Tests.cs` — example-based tests
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/{Calculator}PropertyTests.cs` — property-based tests

## Edge Cases
<!-- Boundary conditions and their handling. Don't mix with Rules. -->
- {condition}: {what happens and why}

## Relationships
<!-- How this concept connects to other domain concepts. -->
- {ConceptA} → {ConceptB}: {nature of relationship}

## Source
<!-- Where these rules come from: spec section, original Fokus plan, KB rule ID. -->
<!-- Example: Original Fokus fokus-spec.md §Health Score; plan step 3c-A. -->
{source reference}

---
<!-- Mutation gate tracking (filled by Step 3 / Cover agent) -->
<!-- mutation-gated: false | true -->
<!-- last-stryker-run: {date} — killed: {N} / {total} mutants -->
