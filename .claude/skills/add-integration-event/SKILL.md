---
name: add-integration-event
description: Adds a MassTransit integration event — contract, publisher (domain event handler), and consumer. Variant-aware for MediatR and FastEndpoints handlers. Use when a business event needs to propagate across service boundaries.
---

# Add Integration Event

Creates a complete MassTransit integration event with contract, publisher, and consumer(s).

## Steps

1. **Create the event contract** — follow `workflows/EventContract.md`
2. **Create the publisher** (domain event handler) — follow `workflows/Publisher.md` (variant-aware)
3. **Create consumer(s)** in target services — follow `workflows/Consumer.md`
4. **Verify the build:** `dotnet build`

## Arguments

Pass the event name: `/add-integration-event OrderApproved`

## Existing Integration Events

| Event | Publisher | Consumers | Contract Location |
|-------|-----------|-----------|-------------------|
| (check `src/BuildingBlocks/{ProjectName}.Integration.Contracts/`) | | | |
