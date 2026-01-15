using FinLedger.Domain.Events;

namespace FinLedger.Application.Interfaces;

/// <summary>
/// Event Store - Persists domain events
/// </summary>
public interface IEventStore
{
    Task AppendAsync(Guid aggregateId, DomainEvent @event);
    Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId);
    Task<IEnumerable<DomainEvent>> GetEventsByTypeAsync(string eventType);
}
