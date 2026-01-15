using FinLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinLedger.Infrastructure.Configurations;

// EVENT STORE CONFIGURATION
/// <summary>
/// Immutable event log - Append-only
/// </summary>
public sealed class EventStoreConfiguration : IEntityTypeConfiguration<EventStore>
{
    public void Configure(EntityTypeBuilder<EventStore> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Data)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes for querying events
        builder.HasIndex(e => e.AggregateId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(new[] { nameof(EventStore.AggregateId), nameof(EventStore.CreatedAt) });

        builder.ToTable("Events");

        
    }
}