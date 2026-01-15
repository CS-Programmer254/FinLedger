using FinLedger.Domain.Aggregates;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Events;
using FinLedger.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FinLedger.Infrastructure.Persistence;

public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }


    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookAggregate> WebhookAggregates => Set<WebhookAggregate>();
    public DbSet<ReconciliationSnapshot> ReconciliationSnapshots => Set<ReconciliationSnapshot>();
    public DbSet<EventStore> Events => Set<EventStore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookAggregateConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new EventStoreConfiguration());
        // Ignore domain events
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Ignore<PaymentCreatedEvent>();
        modelBuilder.Ignore<PaymentCompletedEvent>();
        modelBuilder.Ignore<FundsReservedEvent>();
        modelBuilder.Ignore<FundsSettledEvent>();
        modelBuilder.Ignore<PaymentFailedEvent>();
    }
}
