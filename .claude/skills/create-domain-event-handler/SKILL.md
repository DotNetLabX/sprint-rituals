---
name: create-domain-event-handler
description: Creates a domain event handler following the vertical slice pattern. Places the handler in the feature folder it serves. Names the handler by its effect. Use when adding a handler that reacts to a domain event (SignalR broadcast, background job trigger, etc.).
---

# Create Domain Event Handler

Creates a domain event handler co-located with the feature slice it serves, named by its effect.

## Steps

1. **Determine the service and event** — identify which service owns the domain event and which aggregate raises it.

2. **Determine the target feature folder** — follow the location rule in `workflows/Handler.md`.

3. **Determine the handler name** — follow the naming rule in `workflows/Handler.md` (effect-based naming).

4. **Create the handler file** — follow the pattern in `workflows/Handler.md` (FastEndpoints or MediatR variant).

5. **Verify the build:** `dotnet build`

## Arguments

Pass the event name: `/create-domain-event-handler CardCreatedEvent`
