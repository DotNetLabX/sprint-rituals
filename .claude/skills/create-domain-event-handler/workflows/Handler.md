# Handler Workflow

## Location Rule

The handler lives in `{Svc}.API/Features/{Domain}/{Feature}/` — the feature folder it *serves*, not necessarily the one that raised the event.

- **Single handler for an event:** Place in the feature folder that triggers the event (cause and effect are the same feature).
- **Multiple handlers for the same event:** Each handler goes in the feature folder its *effect* belongs to.

## Naming Rule

Handler names describe their effect, not their trigger.

- **Effect matches event name (e.g., SignalR broadcast):** `Broadcast{EventName}Handler.cs` (short form).
  - Example: `OrderCreatedEvent` -> `BroadcastOrderCreatedHandler.cs`
- **Effect differs from event name:** `{EffectDescription}On{EventName}Handler.cs` (full form).
  - Example: `LineItemRemovedEvent` broadcasts "OrderUpdated" -> `BroadcastOrderUpdatedOnLineItemRemovedHandler.cs`
- **Multiple effects:** `{Effect1}And{Effect2}On{EventName}Handler.cs`.
  - Example: `LineItemAddedEvent` broadcasts and enqueues a calculation -> `BroadcastAndRecalculateOnLineItemAddedHandler.cs`

## Namespace

Matches the folder path exactly.

```
{Svc}.API.Features.{Domain}.{Feature}
```

## Pattern — FastEndpoints Variant

```csharp
public sealed class {HandlerName}({Dependencies})
    : IEventHandler<{EventName}>
{
    public async Task HandleAsync({EventName} ev, CancellationToken ct)
    {
        // side effect: SignalR broadcast, background job trigger, etc.
    }
}
```

## Pattern — MediatR Variant

```csharp
public sealed class {HandlerName}({Dependencies})
    : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // side effect
    }
}
```

Check the service's CLAUDE.md for which variant to use.

## Reference

Canonical example:

```
src/Services/{Svc}/{Svc}.API/Features/{Domain}/{Feature}/Broadcast{EventName}Handler.cs
```
