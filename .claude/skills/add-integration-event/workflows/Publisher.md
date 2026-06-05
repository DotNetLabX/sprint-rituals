# Create Integration Event Publisher

A domain event handler that bridges to MassTransit `IPublishEndpoint.Publish()`.

## MediatR Variant

**Reference:** `src/Services/{Svc}/{Svc}.Application/Features/{Domain}/{Feature}/PublishIntegrationEventOn{DomainEvent}Handler.cs`

**Important:** Re-load the aggregate with full includes before mapping to DTO. The domain event's aggregate may have lazy-loaded collections that the DTO needs.

```csharp
public class PublishIntegrationEventOn{DomainEvent}Handler({AggregateRepository} _repository, IPublishEndpoint _publishEndpoint)
    : INotificationHandler<{DomainEvent}>
{
    public async Task Handle({DomainEvent} notification, CancellationToken ct)
    {
        // Re-load with full includes for complete DTO mapping
        var entity = await _repository.GetFullByIdAsync(notification.{Entity}.Id);

        var dto = entity.Adapt<{DtoType}>();
        await _publishEndpoint.Publish(new {EventName}Event(dto), ct);
    }
}
```

## FastEndpoints Variant

**Reference:** `src/Services/{Svc}/{Svc}.API/Features/{Domain}/{Feature}/PublishIntegrationEventOn{DomainEvent}Handler.cs`

```csharp
public class PublishIntegrationEventOn{DomainEvent}Handler(Repository<{Entity}> _repository, IPublishEndpoint _publishEndpoint)
    : IEventHandler<{DomainEvent}>
{
    public async Task HandleAsync({DomainEvent} notification, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(notification.{Entity}.Id);
        var dto = entity.Adapt<{DtoType}>();
        await _publishEndpoint.Publish(new {EventName}Event(dto), ct);
    }
}
```

Note: `Handle` (MediatR) vs `HandleAsync` (FastEndpoints). Check the service's CLAUDE.md for which variant to use.

## Naming Convention

`PublishIntegrationEventOn{DomainEvent}Handler` — literal "IntegrationEvent" in the name, then the domain event that triggers it.

## Location

- MediatR: `{Svc}.Application/Features/{Domain}/{Feature}/`
- FastEndpoints: `{Svc}.API/Features/{Domain}/{Feature}/`
