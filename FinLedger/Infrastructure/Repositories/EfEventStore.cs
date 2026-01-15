using FinLedger.Application.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Events;
using FinLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinLedger.Infrastructure.Repositories;

/// <summary>
/// Event Store Implementation - Immutable append-only log
/// </summary>
public sealed class EfEventStore : IEventStore
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<EfEventStore> _logger;

    public EfEventStore(PaymentsDbContext context, ILogger<EfEventStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AppendAsync(Guid aggregateId, DomainEvent @event)
    {
        var stored = new EventStore
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            EventType = @event.GetType().Name,
            Data = System.Text.Json.JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(stored);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Domain event appended - Type: {EventType}, Aggregate: {AggregateId}",
            stored.EventType, aggregateId);
    }

    public async Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        var events = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        return events
            .Select(DeserializeEvent)
            .Where(e => e != null)
            .Cast<DomainEvent>()
            .ToList();
    }

    public async Task<IEnumerable<DomainEvent>> GetEventsByTypeAsync(string eventType)
    {
        var events = await _context.Events
            .Where(e => e.EventType == eventType)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        return events
            .Select(DeserializeEvent)
            .Where(e => e != null)
            .Cast<DomainEvent>()
            .ToList();
    }

    private static DomainEvent? DeserializeEvent(EventStore stored)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return stored.EventType switch
        {
            nameof(PaymentCreatedEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<PaymentCreatedEvent>(stored.Data, options),
            nameof(PaymentCompletedEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<PaymentCompletedEvent>(stored.Data, options),
            nameof(FundsReservedEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<FundsReservedEvent>(stored.Data, options),
            nameof(FundsSettledEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<FundsSettledEvent>(stored.Data, options),
            nameof(PaymentFailedEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<PaymentFailedEvent>(stored.Data, options),
            _ => null
        };
    }
}
