# Create Domain Event

## Pattern

**Reference:** `src/Services/{Svc}/{Svc}.Domain/Events/`

### Variant A: With Action Tracking

```csharp
public record {EventName}({AggregateName} {AggregateName}, IAction Action) : DomainEvent<IAction>(Action);
```

Services that track which action triggered an event use this variant.

### Variant B: Aggregate Reference Only

```csharp
public record {EventName}({AggregateName} {AggregateName}) : IDomainEvent;
```

Services where events are simple notifications use this variant.

## Raising Events

In the aggregate behavior method (in `Behaviors/{Aggregate}.cs`):

### Variant A (with action):
```csharp
public void {Method}({Params}, IAction action)
{
    // validate + mutate
    AddDomainEvent(new {EventName}(this, action));
}
```

### Variant B (without action):
```csharp
public void {Method}({Params})
{
    // validate + mutate
    AddDomainEvent(new {EventName}(this));
}
```

## Handling Events

Domain event handlers bridge to side effects (integration events, emails, timeline entries).

### MediatR Variant
```csharp
public class {HandlerName}({Dependencies})
    : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // publish integration event, send email, etc.
    }
}
```

### FastEndpoints Variant
```csharp
public class {HandlerName}({Dependencies})
    : IEventHandler<{EventName}>
{
    public async Task HandleAsync({EventName} notification, CancellationToken ct)
    {
        // publish integration event, send email, etc.
    }
}
```

Note the method name difference: `Handle` (MediatR) vs `HandleAsync` (FastEndpoints). Check the service's CLAUDE.md for which variant to use.

## Location

- Events: `{Svc}.Domain/{AggregateName}/Events/{EventName}.cs` or co-located with the aggregate in `{Svc}.Domain/Events/`
- Handlers (MediatR): `{Svc}.Application/Features/{Domain}/{Feature}/{HandlerName}.cs` or `{Svc}.API/Features/{Domain}/{Feature}/{HandlerName}.cs`
- Handlers (FastEndpoints): `{Svc}.API/Features/{Domain}/{Feature}/{HandlerName}.cs`
