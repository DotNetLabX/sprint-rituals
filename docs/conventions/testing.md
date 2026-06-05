# Testing Conventions

Stack-specific testing standards. For TDD workflow (red-green-refactor loop), see the `tdd` skill.

## Backend (.NET)

### Project Structure

```
{Svc}.Tests/
  Features/{Area}/
    {ServiceName}Tests.cs        # one class per service under test
  _Fixtures/
    TestData.cs                  # static factory methods for domain entities
```

Mirror the main project's `Features/` folder structure.

### Naming

- Test class: `{ClassUnderTest}Tests`
- Test method: `Should_{ExpectedBehavior}_When_{Condition}`
- SUT variable: `_sut`

### Slice Organization

Group tests by behavior slice with numbered comment headers:

```csharp
// --- Slice 1: No active sprint ---
[Fact]
public void Should_ReturnHasActiveSprintFalse_When_NoActiveSprint() { ... }

// --- Slice 2: Basic pace and daily breakdown ---
[Fact]
public void Should_ComputePaceAndBreakdown_When_DeveloperCompletesTicketOnDay3() { ... }
```

Number slices sequentially. Group related assertions in one test per slice — don't split a single behavior across multiple tests.

### Test Data

Use static factory methods in `TestData.cs` — not builders, not object mothers:

```csharp
TestData.ActiveSprint(startDate: ..., endDate: ...)
TestData.ActiveDeveloper("dev-1", "Dev One", subTeam: "Alpha")
TestData.FeatureTicket("FOK-1", assigneeId: "dev-1")
TestData.Membership(sprintId, ticketId, storyPoints: 5m, ticket: ticket)
TestData.Transition(ticketId, "Done", timestamp, "In Progress")
```

Each factory returns a valid default. Optional parameters override specific fields. Keep factories in one file per test project.

### Assertions

FluentAssertions exclusively. No `Assert.Equal`.

### Mocking

Direct instantiation for pure computation services (`new()`). No mocking for stateless logic. Reserve `NSubstitute` for services with external dependencies (repositories, HTTP clients). Reserve `WebApplicationFactory` for endpoint integration tests.

## Frontend (Vue/TypeScript)

### File Location

Co-located with source: `client/src/**/*.spec.ts`

### Framework

Vitest + `@vue/test-utils`. MSW for API mocking when needed.

### Naming

Same `should ... when ...` pattern as backend, adapted to `describe`/`it`:

```typescript
describe('DeveloperProgressStore', () => {
  it('should return empty developers when no active sprint', () => { ... })
})
```

## Coverage Expectations

Not every feature needs tests. Prioritize:
- Computation services with business logic (analytics, metrics, deltas)
- Domain aggregate behavior methods
- Edge cases flagged in plan steps or KB entries

Skip tests for:
- Pure wiring (DI registration, EF config, endpoint routing)
- UI layout and styling
- Simple CRUD with no business rules
