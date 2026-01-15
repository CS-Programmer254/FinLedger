namespace FinLedger.Domain.Events;
/// <summary>
/// Base class for all domain events - immutable record of what happened
/// </summary>
public abstract record DomainEvent(Guid AggregateId, DateTime OccurredAt, string EventVersion = "1.0");
