using FastEndpoints;

namespace Blocks.Domain.Events;

/// <summary>
/// Marker interface for domain events. Extends FastEndpoints IEvent for in-process dispatch
/// via IEventHandler&lt;T&gt;.
/// </summary>
public interface IDomainEvent : IEvent;
