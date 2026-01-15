using FinLedger.Domain.Events;

namespace FinLedger.Domain.Shared;

/// <summary>
/// Base class for aggregate roots - manages transaction boundaries and invariants
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}
