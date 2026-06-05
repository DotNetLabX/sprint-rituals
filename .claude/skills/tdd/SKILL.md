---
name: tdd
description: Test-driven development — red-green-refactor loop, one vertical slice at a time. Loaded by developer agent during implementation steps that have testable behavior.
---

# TDD

Test-driven development using red-green-refactor, one vertical slice at a time. Each cycle: write ONE test, see it fail (red), write minimal code to pass (green), refactor only when green.

## When to Use

- Plan step specifies "with tests" or "test coverage"
- Implementing domain logic with clear input/output contracts
- Adding a new endpoint with defined request/response behavior
- Fixing a bug (write the regression test FIRST — see `diagnose` skill Phase 5)
- User requests TDD explicitly

## When NOT to Use

- Scaffolding/wiring (DI registration, project setup, EF config) — no behavior to test
- UI layout/styling — visual, not behavioral
- One-shot scripts or migrations

## Anti-Pattern: Horizontal Slicing

**Never** write all tests first, then all implementation. This produces tests that verify shape (structure) rather than behavior (what happens when).

```
BAD:  Write 5 tests → implement all → all green
GOOD: Write 1 test → implement → green → write next test → implement → green
```

## Workflow

### Step 0: Bootstrap (first time only)

If no test project exists, set one up following `docs/conventions/testing.md` (project structure, frameworks, dependencies). If no testing convention file exists, create a minimal test project using the stack's standard test runner and register it in the build.

### Step 1: Plan the Slice

Before writing code, identify the **behavior** to test — not the implementation:
- What does the caller send in?
- What comes back (or what side effect occurs)?
- What's the one edge case that matters most?

Write it as a sentence: "When {input}, it should {behavior}."

### Step 2: Red — Write One Failing Test

Write one test using the naming convention from `docs/conventions/testing.md`. Structure: Arrange (set up inputs), Act (call the unit), Assert (verify ONE behavior).

**Run** the test runner — confirm it fails for the RIGHT reason (not a compile error, not a wrong assertion — the actual behavior is missing).

If it fails for the wrong reason: fix the test setup, not the production code.

### Step 3: Green — Minimal Implementation

Write the **minimum** code to make the test pass. No more. Hardcoding is acceptable if only one test exists — the next test will force generalization.

**Run** the test runner — green.

### Step 4: Refactor (only when green)

With all tests passing, improve the code:
- Remove duplication introduced by the minimal implementation
- Extract methods if a block is doing two things
- Rename for clarity

**Rules:**
- Never refactor while red
- Run tests after each refactoring move — stay green
- Don't anticipate future tests during refactoring

### Step 5: Next Slice

Return to Step 1 with the next behavior. Each cycle should take 5–15 minutes.

## What to Test, Mocking Rules, Test Naming, Test Organization

Read `docs/conventions/testing.md` for all stack-specific details: test layers and what to mock at each level, mocking rules (mock at system boundaries only, never mock internal classes or pure logic), test naming conventions, test file organization, and assertion style.

The universal principle: mock only at system boundaries (external APIs, message buses, time, file system). Never mock internal classes or pure domain logic.

## Integration with Developer Workflow

TDD is HOW the developer implements plan steps, not a separate phase:

1. Developer reads plan step
2. Developer identifies the testable behavior in that step
3. TDD loop (red-green-refactor) until the step's acceptance criteria are met
4. Move to next plan step

**Not every plan step needs TDD.** Steps that are pure wiring (DI, config, EF migration) skip directly to implementation. Steps with business logic or request/response contracts use TDD.

## Guardrails

- **One test at a time** — never write the next test until the current one is green
- **Test behavior, not implementation** — if refactoring internals breaks tests, the tests are wrong
- **No test-only abstractions** — don't introduce interfaces solely for testability when you can use WebApplicationFactory with real dependencies
- **Delete tests that test nothing** — a test that passes regardless of implementation is worse than no test
- **Integration over unit for endpoints** — prefer one `WebApplicationFactory` test over mocking 5 internal collaborators

## Required Reading

Before invoking this skill, ensure you have:
- The plan step's acceptance criteria — what behavior needs to be verified
- The test project path (check if a test project exists; if not, run Step 0 bootstrap)
- An existing test file in the same project — for naming conventions and fixture setup patterns

## Anti-patterns

- **Writing all tests first, then all implementation (horizontal slicing).** This produces tests that verify shape rather than behavior. Write one test → implement → green → next test. Never batch tests.
- **Writing a test that passes before any implementation.** If the test is green before you write the implementation code, the test is testing nothing. Confirm the test fails for the right reason (missing behavior, not a compile error) before implementing.
- **Using implementation details as test assertions.** If refactoring internals breaks tests, the tests are wrong. Assert on observable behavior (return values, side effects, state changes) — not on how the code is structured internally.

## What This Skill Does NOT Do

- Set up CI/CD test pipelines — that's infrastructure
- Guide load/performance testing — different concern
- Handle flaky test investigation — see `diagnose` skill
- Make architectural decisions about test boundaries — that's the architect's job
