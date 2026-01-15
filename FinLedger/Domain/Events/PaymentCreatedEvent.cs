namespace FinLedger.Domain.Events;

public sealed record PaymentCreatedEvent(
    Guid AggregateId,
    Guid PaymentId,
    Guid MerchantId,
    int Amount,
    string Currency,
    string Reference,
    DateTime OccurredAt
) : DomainEvent(AggregateId, OccurredAt);

public sealed record PaymentCompletedEvent(
    Guid AggregateId,
    Guid PaymentId,
    DateTime OccurredAt
) : DomainEvent(AggregateId, OccurredAt);

public sealed record FundsReservedEvent(
    Guid AggregateId,
    Guid PaymentId,
    int Amount,
    DateTime OccurredAt
) : DomainEvent(AggregateId, OccurredAt);

public sealed record FundsSettledEvent(
    Guid AggregateId,
    Guid PaymentId,
    int Amount,
    DateTime OccurredAt
) : DomainEvent(AggregateId, OccurredAt);

public sealed record PaymentFailedEvent(
    Guid AggregateId,
    Guid PaymentId,
    string Reason,
    DateTime OccurredAt
) : DomainEvent(AggregateId, OccurredAt);
